using Metapod.Core;
using Metapod.Infrastructure;
using Metapod.Claude;

var failures = new List<string>();
void Test(string name, Action action)
{
    try { action(); Console.WriteLine("PASS " + name); }
    catch (Exception error) { failures.Add(name + ": " + error.Message); Console.Error.WriteLine("FAIL " + name + ": " + error); }
}
var temp = Path.Combine(Path.GetTempPath(), "Metapod.Tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    Test("diff mostra adição e remoção", () =>
    {
        var diff = Diff.Create("um\ndois", "um\ntrês");
        if (!diff.Contains("− dois") || !diff.Contains("+ três")) throw new Exception("Diff incompleto.");
    });
    Test("nome de recurso bloqueia espaços e reservados", () =>
    {
        try { ContentRules.NewName("arquivo ruim"); throw new Exception("Aceitou espaço."); } catch (InvalidOperationException) { }
        try { ContentRules.NewName("con"); throw new Exception("Aceitou nome reservado."); } catch (InvalidOperationException) { }
        if (ContentRules.NewName("code-reviewer") != "code-reviewer") throw new Exception("Nome válido recusado.");
    });
    Test("gravação preserva backup e histórico", () =>
    {
        var project = Path.Combine(temp, "project"); var claude = Path.Combine(project, ".claude", "agents"); Directory.CreateDirectory(claude);
        var path = Path.Combine(claude, "reviewer.md"); File.WriteAllText(path, "antigo");
        var files = new FileChanges(Path.Combine(temp, "history")); var before = FileChanges.Read(path);
        var entry = files.Apply(new ChangePlan(before, "novo", "Projeto", "Teste"), project);
        if (File.ReadAllText(path) != "novo" || entry.Status != "Concluída" || entry.Backup is null || File.ReadAllText(entry.Backup) != "antigo") throw new Exception("Backup ou gravação incorretos.");
        var restore = files.Restore(entry);
        if (restore.Content != "antigo") throw new Exception("Restauração não leu backup.");
    });
    Test("conflito externo bloqueia escrita", () =>
    {
        var project = Path.Combine(temp, "conflict"); var dir = Path.Combine(project, ".claude", "agents"); Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "reviewer.md"); File.WriteAllText(path, "v1"); var before = FileChanges.Read(path); File.WriteAllText(path, "v2");
        try { new FileChanges(Path.Combine(temp, "history2")).Apply(new ChangePlan(before, "v3", "Projeto", "Teste"), project); throw new Exception("Conflito não foi detectado."); }
        catch (IOException e) when (e.Message.Contains("Conflito")) { }
        if (File.ReadAllText(path) != "v2") throw new Exception("Arquivo externo foi substituído.");
    });
    Test("inventário não executa conteúdo e encontra recursos", () =>
    {
        var project = Path.Combine(temp, "scan"); var agent = Path.Combine(project, ".claude", "agents"); Directory.CreateDirectory(agent);
        File.WriteAllText(Path.Combine(agent,"reviewer.md"), "---\ndescription: Revisa alterações\n---\n# Não execute isto\n");
        File.WriteAllText(Path.Combine(project,"CLAUDE.md"), "# projeto");
        var inventory = new ClaudeAdapter(new ThrowingRunner(), Path.Combine(temp,"personal-empty")).Scan(project);
        if (inventory.Resources.Count != 2 || inventory.Resources.All(x => x.Name != "reviewer.md")) throw new Exception("Inventário não encontrou arquivos esperados.");
    });
    Test("inventário global funciona sem projeto", () =>
    {
        var personal = Path.Combine(temp,"personal"); var skill = Path.Combine(personal,"skills","release-review"); Directory.CreateDirectory(skill);
        File.WriteAllText(Path.Combine(skill,"SKILL.md"),"---\ndescription: Revisa uma release\n---\n");
        var inventory = new ClaudeAdapter(new ThrowingRunner(), personal).Scan(null);
        var found = inventory.Resources.SingleOrDefault(x => x.Name == "release-review");
        if(found is null || found.Scope != ResourceScope.Global) throw new Exception("Skill global não apareceu sem projeto.");
    });
}
finally { try { Directory.Delete(temp, true); } catch { } }
if (failures.Count > 0) { Console.Error.WriteLine(string.Join(Environment.NewLine, failures)); return 1; }
return 0;

sealed class ThrowingRunner : IProcessRunner
{
    public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, string directory, string? input, TimeSpan timeout, CancellationToken token, IReadOnlyDictionary<string,string?>? environment = null) => throw new Exception("O scanner não deve iniciar processos.");
}
