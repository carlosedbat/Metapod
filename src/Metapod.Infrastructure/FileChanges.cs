using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Metapod.Core;

namespace Metapod.Infrastructure;

public static class SafePaths
{
    public static void NoLinks(string path)
    {
        var userRoot=Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))+Path.DirectorySeparatorChar;
        var current = Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        {
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                FileSystemInfo info=Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
                var target=info.ResolveLinkTarget(true)?.FullName;
                if(target is null || !Path.GetFullPath(target).StartsWith(userRoot,StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Link externo não é editável automaticamente: " + current);
            }
            current = Path.GetDirectoryName(current);
        }
    }
    public static void Configuration(string path, string root)
    {
        var full = Path.GetFullPath(path);
        var basePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        if (!full.StartsWith(basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Destino fora do escopo aprovado.");
        var relative = Path.GetRelativePath(basePath, full).Replace('\\', '/');
        if (relative.Contains(':') || !(relative.EndsWith(".md",StringComparison.OrdinalIgnoreCase) || relative.EndsWith(".json",StringComparison.OrdinalIgnoreCase))) throw new IOException("Tipo de arquivo não suportado.");
        if (relative.Split('/').Any(s => s.StartsWith('.') && s != ".claude" && s != ".mcp.json")) throw new IOException("Arquivo interno não editável.");
        if (relative.Contains("credentials",StringComparison.OrdinalIgnoreCase)) throw new IOException("Credenciais não são editáveis.");
        NoLinks(full);
    }
}

public sealed class FileChanges(string storage)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static Snapshot Read(string path)
    {
        SafePaths.NoLinks(path);
        if (!File.Exists(path)) return new(path,null);
        using var file = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if (file.Length > ContentRules.MaxBytes) throw new IOException("Arquivo acima de 256 KB; use um editor externo.");
        byte[] data = new byte[(int)file.Length]; file.ReadExactly(data);
        var result = new Snapshot(Path.GetFullPath(path),data);
        _ = result.Text;
        return result;
    }
    public HistoryEntry Apply(ChangePlan plan, string allowedRoot)
    {
        ContentRules.Validate(plan.Before.Path,plan.Content);
        SafePaths.Configuration(plan.Before.Path,allowedRoot);
        Directory.CreateDirectory(storage);
        var id = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N");
        var backup = plan.Before.Exists ? Path.Combine(storage,id+".bak") : null;
        var bytes = new UTF8Encoding(plan.Before.Bytes?.Take(3).SequenceEqual(new byte[] {239,187,191}) == true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(plan.Content)).ToArray();
        var entry = new HistoryEntry(id,DateTimeOffset.Now,Path.GetFullPath(plan.Before.Path),plan.Scope,plan.Reason,"Preparada",backup,Convert.ToHexString(SHA256.HashData(bytes)));
        // Hold an exclusive handle through validation and write so another editor cannot race the update.
        Directory.CreateDirectory(Path.GetDirectoryName(plan.Before.Path)!);
        SafePaths.NoLinks(plan.Before.Path);
        using var file = new FileStream(plan.Before.Path,plan.Before.Exists ? FileMode.Open : FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None);
        if (file.Length > ContentRules.MaxBytes) throw new IOException("O arquivo mudou: recarregue antes de aplicar.");
        byte[] current = new byte[(int)file.Length]; file.ReadExactly(current);
        if (plan.Before.Exists && !current.SequenceEqual(plan.Before.Bytes!)) throw new IOException("Conflito: o arquivo mudou externamente. Recarregue e revise novamente.");
        try
        {
            if (backup is not null) File.WriteAllBytes(backup,current);
            Save(entry);
            file.Position = 0; file.Write(bytes); file.SetLength(bytes.Length); file.Flush(true);
            file.Position = 0; byte[] check = new byte[bytes.Length]; file.ReadExactly(check);
            if (!bytes.SequenceEqual(check)) throw new IOException("A verificação da gravação falhou.");
            entry = entry with { Status = "Concluída" }; Save(entry);
            return entry;
        }
        catch (Exception error)
        {
            try { file.Position=0; file.Write(current); file.SetLength(current.Length); file.Flush(true); }
            catch (IOException) { /* The prepared receipt and original backup remain available for recovery. */ }
            try { Save(entry with { Status = "Falhou: " + error.Message }); } catch (IOException) { }
            throw;
        }
    }
    public HistoryEntry Record(string path,string scope,string action,string status)
    {
        Directory.CreateDirectory(storage);
        var entry = new HistoryEntry(Guid.NewGuid().ToString("N"),DateTimeOffset.Now,path,scope,action,status,null,null);
        Save(entry); return entry;
    }
    private void Save(HistoryEntry entry) => File.WriteAllText(Path.Combine(storage,entry.Id+".json"),JsonSerializer.Serialize(entry,Json));
    public IReadOnlyList<HistoryEntry> List()
    {
        if (!Directory.Exists(storage)) return [];
        var items = new List<HistoryEntry>();
        foreach (var path in Directory.EnumerateFiles(storage,"*.json"))
            try { var e=JsonSerializer.Deserialize<HistoryEntry>(File.ReadAllText(path)); if(e is not null) items.Add(e); }
            catch (Exception e) when(e is IOException or JsonException) { }
        return items.OrderByDescending(x=>x.When).ToArray();
    }
    public ChangePlan Restore(HistoryEntry entry)
    {
        if (entry.Backup is null || !File.Exists(entry.Backup)) throw new IOException("Não há backup de conteúdo para esta operação.");
        var expected = Path.GetFullPath(Path.Combine(storage,entry.Id+".bak"));
        if (!string.Equals(expected,Path.GetFullPath(entry.Backup),StringComparison.OrdinalIgnoreCase)) throw new IOException("Backup inválido.");
        return new(Read(entry.Path), Read(entry.Backup).Text,entry.Scope,"Restaurar backup");
    }
}
