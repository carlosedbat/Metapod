using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Metapod.Core;

public enum ResourceKind { Agent, Skill, Instructions, Settings, Mcp, Rule }
public enum ResourceScope { Global, Projeto, Local, Gerenciado }
public sealed record Resource(string Name, ResourceKind Kind, ResourceScope Scope, string Path, string Summary,
    string Details = "", string Model = "", string Tools = "", string Memory = "")
{
    public string KindLabel => Kind switch { ResourceKind.Agent => "Agente", ResourceKind.Skill => "Skill", ResourceKind.Instructions => "Instruções", ResourceKind.Settings => "Configuração", ResourceKind.Mcp => "MCP", _ => "Regra" };
    public string ScopeLabel => Scope switch { ResourceScope.Global => "Global (usuário)", ResourceScope.Projeto => "Projeto", ResourceScope.Local => "Local", _ => "Gerenciado · leitura" };
}
public sealed record Finding(string Title, string Detail, string Path = "");
public sealed record Inventory(IReadOnlyList<Resource> Resources, IReadOnlyList<Finding> Findings);
public sealed record Snapshot(string Path, byte[]? Bytes)
{
    public bool Exists => Bytes is not null;
    public string Hash => Bytes is null ? "missing" : Convert.ToHexString(SHA256.HashData(Bytes));
    public string Text => Bytes is null ? "" : new UTF8Encoding(false, true).GetString(Bytes).TrimStart('\uFEFF');
}
public sealed record ChangePlan(Snapshot Before, string Content, string Scope, string Reason);
public sealed record HistoryEntry(string Id, DateTimeOffset When, string Path, string Scope, string Action, string Status, string? Backup, string? AfterHash)
{
    public string Display => $"{When.ToLocalTime():dd/MM HH:mm} · {Action} · {Status}\n{Path}";
}
public sealed record ProcessResult(int ExitCode, string Output, string Error);
public sealed record CliStatus(string? Executable, string Version, bool SupportsSafeChat);
public sealed record RepositoryResult(string Name, string Url, string Description, int Stars, string Updated, string License)
{
    public string Detail => $"★ {Stars:N0} · Atualização: {Updated} · Licença: {License}";
}
public sealed record ClaudeProject(string Name, string Path, string Source, bool Exists, bool Favorite = false)
{
    public string Detail => Exists ? $"{Source} · {Path}" : $"{Source} · localização não disponível";
}
public sealed record ExecutionProfile(string Name, string MainModel, string SubagentModel, string AdvisorModel,
    bool ForceSubagentModel, string Goal, string Notes, string Scope = "Usuário");
public enum CatalogItemKind { Skill, Plugin, Tool, Agent }
public sealed record CatalogItem(string Id, string Name, string Repository, CatalogItemKind Kind, string Summary, string WhenToUse,
    string InstallHint, string Compatibility)
{
    public string Url => "https://github.com/" + Repository;
    public string KindLabel => Kind switch { CatalogItemKind.Skill => "Skill", CatalogItemKind.Plugin => "Plugin", CatalogItemKind.Agent => "Agente", _ => "Ferramenta" };
}
public sealed record CatalogDocument(CatalogItem Item, string Readme, string Manifest, string Status);
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, string directory, string? input, TimeSpan timeout, CancellationToken token, IReadOnlyDictionary<string,string?>? environment = null);
}
public interface IHarnessAdapter
{
    string Name { get; }
    Inventory Scan(string? project);
    Task<CliStatus> DetectAsync(CancellationToken token);
    Task<string> AskAsync(string context, string question, ExecutionProfile profile, CancellationToken token);
    Task<string> PluginsAsync(string project, string action, string name, string scope, CancellationToken token);
}

public static partial class ContentRules
{
    public const int MaxBytes = 256 * 1024;
    public static void Validate(string path, string text)
    {
        if (Encoding.UTF8.GetByteCount(text) > MaxBytes) throw new InvalidOperationException("Conteúdo acima do limite de 256 KB.");
        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("A configuração deve ser um objeto JSON.");
        }
        else if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("O recurso não pode ficar vazio.");
    }
    public static string NewName(string name)
    {
        if (!NameRegex().IsMatch(name)) throw new InvalidOperationException("Use 1 a 64 letras minúsculas, números e hífens; comece com uma letra.");
        if (new[] { "con", "prn", "aux", "nul", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9" }.Contains(name)) throw new InvalidOperationException("Nome reservado pelo Windows.");
        return name;
    }
    public static string Redact(string text) => SecretValue().Replace(SecretLine().Replace(text, "[linha sensível omitida]"), "[segredo omitido]");
    [GeneratedRegex("^[a-z][a-z0-9-]{0,63}$")] private static partial Regex NameRegex();
    [GeneratedRegex("(?im)^.*(?:api[_-]?key|token|secret|password|authorization|credential|senha|bearer).*$")]
    private static partial Regex SecretLine();
    [GeneratedRegex(@"\b(?:sk-[A-Za-z0-9_-]{16,}|gh[pousr]_[A-Za-z0-9_]{20,})\b")]
    private static partial Regex SecretValue();
}

public static class Diff
{
    public static string Create(string before, string after)
    {
        var a = before.Replace("\r\n", "\n").Split('\n');
        var b = after.Replace("\r\n", "\n").Split('\n');
        int start = 0, end = 0;
        while (start < Math.Min(a.Length,b.Length) && a[start] == b[start]) start++;
        if (start == a.Length && start == b.Length) return "Nenhuma alteração.";
        while (end < Math.Min(a.Length,b.Length) - start && a[^(end+1)] == b[^(end+1)]) end++;
        var result = new StringBuilder($"@@ linha {start+1} · trecho substituído @@\n");
        foreach (var line in a.Skip(start).Take(a.Length-start-end)) result.AppendLine("− " + line);
        foreach (var line in b.Skip(start).Take(b.Length-start-end)) result.AppendLine("+ " + line);
        return result.ToString();
    }
}
