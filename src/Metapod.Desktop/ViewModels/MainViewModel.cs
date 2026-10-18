using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Markdig;
using Metapod.Core;
using Metapod.Claude;
using Metapod.Infrastructure;

namespace Metapod.Desktop.ViewModels;

public sealed class MainViewModel : Observable
{
    private readonly ClaudeAdapter adapter;
    private readonly FileChanges changes;
    private readonly GitHubCatalog catalog;
    private readonly IDesktopDialogs dialogs;
    private CancellationTokenSource? running;
    private Snapshot? snapshot;
    private ChangePlan? pending;
    private (string Project,string Action,string Name,string Scope)? pluginPlan;
    private string approvedRoot="";
    private string? capturedQuestion;
    private readonly List<string> conversation=[];
    private static readonly Markdig.MarkdownPipeline MarkdownPipeline = new Markdig.MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    public bool WebViewAvailable { get; }
    public MainViewModel(ClaudeAdapter adapter,FileChanges changes,GitHubCatalog catalog,IDesktopDialogs dialogs,bool webViewAvailable=true)
    {
        this.adapter=adapter; this.changes=changes; this.catalog=catalog; this.dialogs=dialogs; WebViewAvailable=webViewAvailable;
        foreach(var item in catalog.Curated) CatalogItems.Add(item);
        NavigateCommand=new ActionCommand(p=> { if(!IsBusy) Page=p?.ToString()??"Overview"; });
        ChooseProjectCommand=Async(async()=> { if(!ConfirmDiscard()) return; var path=dialogs.ChooseFolder(); if(path is not null) { ProjectPath=path; snapshot=null; pending=null; pluginPlan=null; EditorText=""; conversation.Clear(); ChatText="Conversa vinculada ao projeto. Prepare e confira o contexto antes de enviar."; ContextPreview=""; CodeContext=""; await Refresh(); Page="Overview"; } });
        RefreshCommand=Async(Refresh);
        EditCommand=Async(()=> { OpenSelected(); return Task.CompletedTask; });
        NewCommand=Async(()=> { CreateResource(); return Task.CompletedTask; });
        PrepareCommand=Async(()=> { PrepareChange(); return Task.CompletedTask; });
        ApplyCommand=Async(Apply);
        RestoreCommand=Async(()=> { Restore(); return Task.CompletedTask; });
        ContextCommand=Async(()=> { PrepareContext(); return Task.CompletedTask; });
        AddCodeCommand=Async(()=> { AddCode(); return Task.CompletedTask; });
        SendCommand=Async(Send);
        CancelCommand=new ActionCommand(_=>running?.Cancel());
        ListPluginsCommand=Async(async()=> { RequireProject(); PluginOutput=await adapter.PluginsAsync(ProjectPath,"list","","project",Token); });
        PlanPluginCommand=Async(()=> { PreparePlugin(); return Task.CompletedTask; });
        SearchCommand=Async(async()=> { var result=await catalog.Search(SearchQuery,Token); Repositories.Clear(); foreach(var item in result) Repositories.Add(item); SearchStatus=$"{result.Count} resultados · {DateTime.Now:dd/MM/yyyy HH:mm} · ordenados por estrelas; compatibilidade não validada."; });
        OpenRepositoryCommand=new ActionCommand(p=> { if(p is RepositoryResult r) dialogs.OpenUrl(r.Url); });
        SelectProjectCommand=Async(async()=> { if(SelectedProject is null || !SelectedProject.Exists) throw new InvalidOperationException("O projeto detectado não está disponível. Escolha uma pasta manualmente."); ProjectPath=SelectedProject.Path; await Refresh(); Page="Overview"; });
        LoadCatalogCommand=new ActionCommand(p=> _=Run(async()=> { if(p is CatalogItem item) SelectedCatalog=item; if(SelectedCatalog is null) return; CatalogStatus="Carregando README do GitHub…"; CatalogDocument=await catalog.LoadAsync(SelectedCatalog,Token); CatalogReadme=CatalogDocument.Readme; CatalogManifest=CatalogDocument.Manifest; CatalogStatus=CatalogDocument.Status; IsCatalogModalOpen=true; }));
        CloseCatalogModalCommand=new ActionCommand(_=>IsCatalogModalOpen=false);
        OpenAddCatalogModalCommand=new ActionCommand(_=> { NewCatalogName=""; NewCatalogRepository=""; NewCatalogSummary=""; NewCatalogWhenToUse=""; NewCatalogInstallHint=""; NewCatalogCompatibility=""; NewCatalogKind=CatalogItemKind.Skill; NewCatalogStatus="Preencha os campos ou use \"Obter informações\" para tentar preencher automaticamente."; IsAddCatalogModalOpen=true; });
        CloseAddCatalogModalCommand=new ActionCommand(_=>IsAddCatalogModalOpen=false);
        FetchCatalogInfoCommand=Async(FetchCatalogInfo);
        SaveCatalogItemCommand=new ActionCommand(_=>SaveCatalogItem());
        OpenCatalogRepositoryCommand=new ActionCommand(_=> { if(SelectedCatalog is not null) dialogs.OpenUrl(SelectedCatalog.Url); });
        SelectProfileCommand=new ActionCommand(p=> { if(p is ExecutionProfile profile) { SelectedProfile=profile; Changed(nameof(SelectedProfile)); ProfileStatus=$"Perfil ativo: {profile.Name}. {profile.Notes}"; } });
        PromptBlockCommand=new ActionCommand(p=> { Question += (Question.Length>0?"\n\n":"") + (p?.ToString() switch {
            "skill"=>"Ajude-me a criar uma skill. Objetivo: __. Quando usar: __. Entradas: __. Passos: __. Saída esperada: __. Explique quando não usar.",
            "agent"=>"Avalie se preciso de um agente especializado ou de uma skill. Tarefa: __. Contexto necessário: __. Ferramentas mínimas: __. Critérios de sucesso: __.",
            "tokens"=>"Analise o contexto selecionado e sugira formas de reduzir conteúdo desnecessário. Mostre evidências, riscos e como medir o resultado, sem prometer economia percentual.",
            _=>"Explique quando Jev/TypeSafe ajudaria nesta tarefa: __. Distinga skill orientadora de API/ferramenta executora. Inclua custo adicional, dados enviados, limitações e como comparar resultados." }); Page="Assistant"; });
        CopyCommand=new ActionCommand(_=>dialogs.Copy(ChatText));
        UseResponseCommand=Async(()=> { if(string.IsNullOrWhiteSpace(LastResponse)) throw new InvalidOperationException("Ainda não há resposta."); if(snapshot is null) throw new InvalidOperationException("Abra ou crie um recurso no editor primeiro."); if(!dialogs.Confirm("Substituir rascunho?","Copiar a resposta para o editor? Revise e remova explicações ou cercas de código antes de preparar a alteração.")) return Task.CompletedTask; EditorText=LastResponse; Page="Editor"; return Task.CompletedTask; });
        ClearContextCommand=new ActionCommand(_=> { CodeContext=""; ContextPreview=""; ContextState="Contexto limpo. Prepare novamente antes de enviar."; });
        foreach(var item in changes.List()) History.Add(item);
    }
    private ActionCommand Async(Func<Task> action) => new(_=>_ = Run(action));
    private async Task Run(Func<Task> action)
    {
        if(IsBusy) return;
        IsBusy=true; Status="Processando…"; running=new();
        try { await action(); Status="Pronto."; }
        catch(OperationCanceledException) { Status="Operação cancelada ou tempo esgotado. Nenhuma resposta parcial foi aplicada."; }
        catch(Exception e) { Status=ContentRules.Redact(e.Message); }
        finally { running.Dispose(); running=null; IsBusy=false; }
    }
    private CancellationToken Token=>running?.Token??CancellationToken.None;
    public Task Initialize()=>Run(async()=> { var cli=await adapter.DetectAsync(Token); CliStatus=$"{cli.Version}"; ChatAvailable=cli.SupportsSafeChat; await Refresh(); Projects.Clear(); foreach(var project in adapter.DiscoverProjects()) Projects.Add(project); });
    public void Stop()=>running?.Cancel();
    private void RequireProject() { if(!Directory.Exists(ProjectPath)) throw new InvalidOperationException("Selecione uma pasta de projeto primeiro."); }
    private bool ConfirmDiscard()=>snapshot is null || EditorText==snapshot.Text || dialogs.Confirm("Rascunho não salvo","Descartar o rascunho atual e continuar?");
    private async Task Refresh()
    {
        var inventory=await Task.Run(()=>adapter.Scan(Directory.Exists(ProjectPath) ? ProjectPath : null),Token);
        Resources.Clear(); foreach(var r in inventory.Resources) Resources.Add(r);
        Findings.Clear(); foreach(var f in inventory.Findings) Findings.Add(f);
        Changed(nameof(ResourceCount)); Changed(nameof(FindingCount)); Changed(nameof(ProjectName)); Changed(nameof(FilteredResources));
    }
    private void OpenSelected()
    {
        if(SelectedResource is null) throw new InvalidOperationException("Selecione um recurso na lista.");
        if(!ConfirmDiscard()) return;
        snapshot=FileChanges.Read(SelectedResource.Path);
        EditorPath=snapshot.Path; EditorScope=SelectedResource.ScopeLabel; EditorText=snapshot.Text; Page="Editor";
    }
    private string RootFor(string path)
    {
        var personal=Path.GetFullPath(adapter.PersonalRoot)+Path.DirectorySeparatorChar;
        if(Path.GetFullPath(path).StartsWith(personal,StringComparison.OrdinalIgnoreCase)) return adapter.PersonalRoot;
        RequireProject();
        var project=Path.GetFullPath(ProjectPath)+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(path).StartsWith(project,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Este recurso é somente leitura neste projeto.");
        var relative=Path.GetRelativePath(ProjectPath,path).Replace('\\','/');
        if(!relative.StartsWith(".claude/",StringComparison.OrdinalIgnoreCase) && !new[]{"CLAUDE.md","CLAUDE.local.md","AGENTS.md",".mcp.json"}.Contains(relative,StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Destino fora das configurações reconhecidas.");
        return ProjectPath;
    }
    private void CreateResource()
    {
        RequireProject(); if(!ConfirmDiscard()) return;
        var name=ContentRules.NewName(NewName);
        var root=NewScope=="Pessoal"?adapter.PersonalRoot:Path.Combine(ProjectPath,".claude");
        var relative=NewKind switch { "Skill"=>Path.Combine("skills",name,"SKILL.md"),"Instruções"=>"CLAUDE.md","Configuração"=>"settings.json",_=>Path.Combine("agents",name+".md") };
        var path=Path.Combine(root,relative);
        snapshot=FileChanges.Read(path);
        if(snapshot.Exists) { snapshot=null; throw new IOException("Já existe um recurso nesse destino. Abra-o no inventário para editar."); }
        EditorPath=path; EditorScope=NewScope;
        EditorText=NewKind switch {
            "Skill"=>$"---\nname: {name}\ndescription: Use quando precisar executar o procedimento descrito abaixo.\n---\n\n# Objetivo\nDescreva o resultado esperado.\n\n## Quando usar\nDefina os gatilhos e quando não usar.\n\n## Passos\n1. Entenda as entradas.\n2. Execute o procedimento.\n3. Verifique o resultado.\n\n## Saída\nDefina o formato esperado.\n",
            "Instruções"=>"# Contexto do projeto\n\n## Objetivo\nDescreva o domínio do projeto.\n\n## Convenções\nRegistre apenas regras essenciais.\n\n## Verificação\nDocumente os comandos reais de validação.\n",
            "Configuração"=>"{\n}\n",
            _=>$"---\nname: {name}\ndescription: Use para revisar a tarefa especializada descrita abaixo.\ntools: Read, Grep, Glob\n---\n\n# Responsabilidade\nDefina o papel e os limites do agente.\n\n# Critérios de qualidade\nApresente evidências, riscos e recomendações.\n"};
        Page="Editor";
    }
    private void PrepareChange()
    {
        if(snapshot is null) throw new InvalidOperationException("Abra ou crie um recurso primeiro.");
        ContentRules.Validate(snapshot.Path,EditorText);
        approvedRoot=RootFor(snapshot.Path);
        if(EditorText==snapshot.Text) throw new InvalidOperationException("Nenhuma alteração para revisar.");
        pending=new(snapshot,EditorText,EditorScope,snapshot.Exists?"Editar recurso":"Criar recurso"); pluginPlan=null;
        ReviewTitle="Revisar alteração de arquivo"; ReviewPath=$"{EditorScope} · {snapshot.Path}";
        ReviewDiff=Diff.Create(snapshot.Text,EditorText); ReviewNotes="O conteúdo aprovado será gravado. Um arquivo existente terá backup local; alterações externas interrompem a gravação. Backups podem conter dados privados."; Page="Review";
    }
    private async Task Apply()
    {
        if(pluginPlan is { } p)
        {
            try
            {
                PluginOutput=await adapter.PluginsAsync(p.Project,p.Action,p.Name,p.Scope,Token);
                History.Insert(0,changes.Record(p.Project,p.Scope,$"Plugin {p.Action}: {p.Name}","CLI concluiu; confira a saída"));
            }
            catch(Exception e)
            {
                History.Insert(0,changes.Record(p.Project,p.Scope,$"Plugin {p.Action}: {p.Name}","Não concluída; confira estado pelo CLI"));
                PluginOutput=ContentRules.Redact(e.Message); throw;
            }
            finally { pluginPlan=null; Page="Plugins"; }
            await Refresh(); return;
        }
        if(pending is null) throw new InvalidOperationException("Prepare uma alteração antes de aprovar.");
        var plan=pending;
        var entry=await Task.Run(()=>changes.Apply(plan,approvedRoot));
        pending=null; History.Insert(0,entry); snapshot=FileChanges.Read(plan.Before.Path); EditorText=snapshot.Text;
        await Refresh(); Page="History";
    }
    private void Restore()
    {
        if(SelectedHistory is null) throw new InvalidOperationException("Selecione uma entrada do histórico.");
        pending=changes.Restore(SelectedHistory); approvedRoot=RootFor(pending.Before.Path); pluginPlan=null;
        ReviewTitle="Revisar restauração"; ReviewPath=pending.Before.Path;
        ReviewDiff=Diff.Create(pending.Before.Text,pending.Content);
        ReviewNotes="A restauração substitui o conteúdo atual pelo backup selecionado. O conteúdo atual também terá backup. Confira possíveis edições posteriores."; Page="Review";
    }
    private void PreparePlugin()
    {
        RequireProject(); if(string.IsNullOrWhiteSpace(PluginName) || PluginName.StartsWith('-')) throw new InvalidOperationException("Informe nome@marketplace.");
        pluginPlan=(ProjectPath,PluginAction,PluginName,PluginScope); pending=null;
        ReviewTitle="Revisar operação de plugin"; ReviewPath=$"{PluginScope} · {ProjectPath}";
        ReviewDiff=$"claude plugin {PluginAction} {PluginName} --scope {PluginScope} --json";
        ReviewNotes="O CLI poderá baixar pacotes e alterar configurações do escopo escolhido. Não há rollback universal. Comandos externos de marketplace que exigirem confirmação serão recusados; revise-os no CLI. A origem e os componentes precisam ser conhecidos antes de aprovar."; Page="Review";
    }
    private async Task FetchCatalogInfo()
    {
        if(string.IsNullOrWhiteSpace(NewCatalogRepository)) throw new InvalidOperationException("Informe o repositório (owner/repo) ou a URL da ferramenta.");
        var slug=ExtractGitHubSlug(NewCatalogRepository);
        if(slug is not null)
        {
            var doc=await catalog.LoadAsync(new("",slug.Split('/')[1],slug,NewCatalogKind,"","","",""),Token);
            if(string.IsNullOrWhiteSpace(NewCatalogName)) NewCatalogName=slug.Split('/')[1];
            NewCatalogSummary=FirstParagraph(doc.Readme);
            NewCatalogStatus=doc.Status;
            return;
        }
        if(!ChatAvailable) { NewCatalogStatus="A origem não é um repositório do GitHub e o Claude instalado não está disponível para resumi-la. Preencha os campos manualmente."; return; }
        var raw=await catalog.FetchRawText(NormalizeUrl(NewCatalogRepository),Token) ?? throw new IOException("Não foi possível acessar a origem informada.");
        var prompt="Extraia desta página as informações da ferramenta em JSON compacto, com as chaves name, summary e whenToUse. Responda somente o JSON, sem comentários nem cercas de código.";
        var json=await adapter.AskAsync(ContentRules.Redact(Truncate(raw,40000)),prompt,SelectedProfile,Token);
        ApplyExtractedInfo(json);
        NewCatalogStatus="Informações obtidas com ajuda do Claude instalado; revise os campos antes de cadastrar.";
    }
    private void ApplyExtractedInfo(string json)
    {
        try
        {
            using var doc=JsonDocument.Parse(json);
            var root=doc.RootElement;
            if(string.IsNullOrWhiteSpace(NewCatalogName) && root.TryGetProperty("name",out var n)) NewCatalogName=n.GetString()??NewCatalogName;
            if(root.TryGetProperty("summary",out var s)) NewCatalogSummary=s.GetString()??NewCatalogSummary;
            if(root.TryGetProperty("whenToUse",out var w)) NewCatalogWhenToUse=w.GetString()??NewCatalogWhenToUse;
        }
        catch(JsonException) { NewCatalogStatus="O Claude respondeu em um formato inesperado; preencha os campos manualmente."; }
    }
    private void SaveCatalogItem()
    {
        if(string.IsNullOrWhiteSpace(NewCatalogName) || string.IsNullOrWhiteSpace(NewCatalogRepository)) throw new InvalidOperationException("Informe ao menos nome e repositório/URL.");
        var id="custom-"+Regex.Replace(NewCatalogName.ToLowerInvariant(),"[^a-z0-9]+","-").Trim('-');
        var repository=ExtractGitHubSlug(NewCatalogRepository) ?? NewCatalogRepository;
        CatalogItems.Add(new(id,NewCatalogName,repository,NewCatalogKind,
            string.IsNullOrWhiteSpace(NewCatalogSummary)?"Aguardando revisão do README.":NewCatalogSummary,
            string.IsNullOrWhiteSpace(NewCatalogWhenToUse)?"Defina quando usar após revisar a ficha.":NewCatalogWhenToUse,
            string.IsNullOrWhiteSpace(NewCatalogInstallHint)?"Revise o repositório antes de instalar.":NewCatalogInstallHint,
            string.IsNullOrWhiteSpace(NewCatalogCompatibility)?"Compatibilidade ainda não verificada.":NewCatalogCompatibility));
        IsAddCatalogModalOpen=false; CatalogStatus="Item cadastrado. Abra o card para carregar README e revisar.";
    }
    private static string? ExtractGitHubSlug(string input)
    {
        var match=Regex.Match(input.Trim(),@"^(?:https?://github\.com/)?([A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+?)(?:\.git)?/?$");
        return match.Success ? match.Groups[1].Value : null;
    }
    private static string NormalizeUrl(string input)=>input.Contains("://",StringComparison.Ordinal) ? input : "https://"+input;
    private static string Truncate(string text,int max)=>text.Length<=max?text:text[..max];
    private static string FirstParagraph(string markdown)
    {
        foreach(var block in markdown.Replace("\r\n","\n").Split("\n\n"))
        {
            var line=block.Trim();
            if(line.Length>0 && !line.StartsWith('#') && !line.StartsWith('<') && !line.StartsWith('!') && !line.StartsWith('[')) return line.Length>280 ? line[..280]+"…" : line;
        }
        return "Descrição não encontrada automaticamente; preencha manualmente.";
    }
    private void AddCode()
    {
        RequireProject(); var files=dialogs.ChooseCodeFiles(); var result=new StringBuilder(CodeContext);
        foreach(var path in files)
        {
            var relative=Path.GetRelativePath(ProjectPath,path);
            if(relative.StartsWith("..")||Path.IsPathRooted(relative)) throw new IOException("Selecione arquivos dentro do projeto.");
            if(Path.GetFileName(path).StartsWith(".env",StringComparison.OrdinalIgnoreCase)||path.Contains("credential",StringComparison.OrdinalIgnoreCase)) throw new IOException("Este arquivo pode conter credenciais e não será incluído.");
            var content=FileChanges.Read(path).Text;
            if(result.Length+content.Length>48000) throw new IOException("Contexto de código acima de 48 mil caracteres. Selecione menos arquivos.");
            result.AppendLine($"\nARQUIVO SELECIONADO: {relative}\n{ContentRules.Redact(content)}");
        }
        CodeContext=result.ToString(); PrepareContext();
    }
    private void PrepareContext()
    {
        RequireProject();
        var b=new StringBuilder($"PROJETO: {ProjectName}\nINVENTÁRIO (metadados):\n");
        foreach(var r in Resources.Take(100)) b.AppendLine($"{r.KindLabel} | {r.ScopeLabel} | {r.Name} | {r.Summary}");
        b.AppendLine("\nESTRUTURA IMEDIATA (somente nomes):");
        foreach(var path in Directory.EnumerateFileSystemEntries(ProjectPath).Take(80)) b.AppendLine(Path.GetFileName(path));
        if(IncludeResource && SelectedResource is not null) b.AppendLine("\nRECURSO SELECIONADO:\n"+SelectedResource.Name+"\n"+FileChanges.Read(SelectedResource.Path).Text);
        if(CodeContext.Length>0) b.AppendLine(CodeContext);
        b.AppendLine("\nCONVERSA RECENTE:"); foreach(var turn in conversation.TakeLast(6)) b.AppendLine(turn);
        b.AppendLine("\nPEDIDO ATUAL:\n"+Question);
        if(b.Length>100000) throw new InvalidOperationException("Contexto muito grande. Remova arquivos ou inicie outro projeto antes de enviar.");
        ContextPreview=ContentRules.Redact(b.ToString()); capturedQuestion=ContentRules.Redact(Question);
        ContextState=$"{ContextPreview.Length:N0} caracteres preparados (não é contagem de tokens). Revise o texto abaixo; nada foi enviado.";
        Page="Context";
    }
    private async Task Send()
    {
        RequireProject();
        if(string.IsNullOrWhiteSpace(ContextPreview)||string.IsNullOrWhiteSpace(capturedQuestion)) throw new InvalidOperationException("Escreva uma pergunta e prepare o contexto antes de enviar.");
        if(!ChatAvailable) throw new InvalidOperationException("CLI indisponível ou sem suporte ao modo de conversa necessário.");
        Page="Assistant"; ChatText+="\n\nVocê\n"+capturedQuestion+"\n\nClaude está respondendo…";
        LastResponse=await adapter.AskAsync(ContentRules.Redact(ContextPreview),"Responda ao PEDIDO ATUAL incluído no contexto.",SelectedProfile,Token);
        conversation.Add("Você: "+capturedQuestion); conversation.Add("Claude: "+LastResponse);
        ChatText=string.Join("\n\n",conversation); ContextPreview=""; capturedQuestion=null;
    }
    public ObservableCollection<Resource> Resources {get;}=[];
    public ObservableCollection<Finding> Findings {get;}=[];
    public ObservableCollection<HistoryEntry> History {get;}=[];
    public ObservableCollection<RepositoryResult> Repositories {get;}=[];
    public ObservableCollection<ClaudeProject> Projects {get;}=[];
    public ObservableCollection<CatalogItem> CatalogItems {get;}=[];
    public IEnumerable<CatalogItem> FilteredCatalogItems=>CatalogItems.Where(x=>(x.Name+" "+x.Summary+" "+x.WhenToUse).Contains(CatalogFilter,StringComparison.OrdinalIgnoreCase));
    public IEnumerable<Resource> FilteredResources=>Resources
        .Where(x=>KindFilter=="Todos"||x.KindLabel==KindFilter)
        .Where(x=>ScopeFilterOption=="Todos"||x.Scope.ToString()==ScopeFilterOption)
        .Where(x=>(x.Name+" "+x.KindLabel+" "+x.ScopeLabel).Contains(Filter,StringComparison.OrdinalIgnoreCase));
    public string ResourceCount=>Resources.Count.ToString(); public string FindingCount=>Findings.Count.ToString();
    public string ProjectName=>string.IsNullOrEmpty(ProjectPath)?"Nenhum projeto selecionado":new DirectoryInfo(ProjectPath).Name;
    private string page="Overview",projectPath="",status="Escolha um projeto para começar.",cliStatus="Detectando Claude Code…",filter="";
    private bool isBusy,chatAvailable,includeResource;
    public string Page {get=>page;set=>Set(ref page,value);}
    public string ProjectPath {get=>projectPath;private set {Set(ref projectPath,value);Changed(nameof(ProjectName));}}
    public string Status {get=>status;set=>Set(ref status,value);}
    public string CliStatus {get=>cliStatus;set=>Set(ref cliStatus,value);}
    public bool IsBusy {get=>isBusy;set {Set(ref isBusy,value);Changed(nameof(IsIdle));}}
    public bool IsIdle=>!IsBusy;
    public bool ChatAvailable {get=>chatAvailable;set=>Set(ref chatAvailable,value);}
    public string Filter {get=>filter;set {Set(ref filter,value);Changed(nameof(FilteredResources));}}
    private string kindFilter="Todos",scopeFilterOption="Todos";
    public string KindFilter {get=>kindFilter;set {Set(ref kindFilter,value);Changed(nameof(FilteredResources));}}
    public string ScopeFilterOption {get=>scopeFilterOption;set {Set(ref scopeFilterOption,value);Changed(nameof(FilteredResources));}}
    public string[] KindFilterOptions {get;}=["Todos","Agente","Skill","Instruções","Configuração","MCP","Regra"];
    public string[] ScopeFilterOptions {get;}=["Todos","Global","Projeto","Local","Gerenciado"];
    public bool IncludeResource {get=>includeResource;set=>Set(ref includeResource,value);}
    private Resource? selectedResource;
    public Resource? SelectedResource {get=>selectedResource;set=>Set(ref selectedResource,value);}
    private HistoryEntry? selectedHistory;
    public HistoryEntry? SelectedHistory {get=>selectedHistory;set=>Set(ref selectedHistory,value);}
    private string editorText="",editorPath="Abra ou crie um recurso",editorScope="",newName="code-reviewer",newKind="Agente",newScope="Projeto";
    public string EditorText {get=>editorText;set=>Set(ref editorText,value);}
    public string EditorPath {get=>editorPath;set=>Set(ref editorPath,value);}
    public string EditorScope {get=>editorScope;set=>Set(ref editorScope,value);}
    public string NewName {get=>newName;set=>Set(ref newName,value);}
    public string NewKind {get=>newKind;set=>Set(ref newKind,value);}
    public string NewScope {get=>newScope;set=>Set(ref newScope,value);}
    public string[] ResourceKinds {get;}=["Agente","Skill","Instruções","Configuração"];
    public string[] Scopes {get;}=["Projeto","Pessoal"];
    private string reviewTitle="Nenhuma alteração preparada",reviewPath="",reviewDiff="",reviewNotes="";
    public string ReviewTitle {get=>reviewTitle;set=>Set(ref reviewTitle,value);}
    public string ReviewPath {get=>reviewPath;set=>Set(ref reviewPath,value);}
    public string ReviewDiff {get=>reviewDiff;set=>Set(ref reviewDiff,value);}
    public string ReviewNotes {get=>reviewNotes;set=>Set(ref reviewNotes,value);}
    private string question="",chatText="Converse sobre seu projeto. Nenhum conteúdo é enviado automaticamente.",contextPreview="",contextState="Prepare o contexto antes de enviar.",codeContext="",lastResponse="";
    public string Question {get=>question;set=>Set(ref question,value);}
    public string ChatText {get=>chatText;set=>Set(ref chatText,value);}
    public string ContextPreview {get=>contextPreview;set=>Set(ref contextPreview,value);}
    public string ContextState {get=>contextState;set=>Set(ref contextState,value);}
    public string CodeContext {get=>codeContext;set=>Set(ref codeContext,value);}
    public string LastResponse {get=>lastResponse;set=>Set(ref lastResponse,value);}
    private string pluginName="",pluginAction="install",pluginScope="project",pluginOutput="Consulte a lista pelo CLI. Instalado, habilitado e carregado são estados diferentes.";
    public string PluginName {get=>pluginName;set=>Set(ref pluginName,value);}
    public string PluginAction {get=>pluginAction;set=>Set(ref pluginAction,value);}
    public string PluginScope {get=>pluginScope;set=>Set(ref pluginScope,value);}
    public string PluginOutput {get=>pluginOutput;set=>Set(ref pluginOutput,value);}
    public string[] PluginActions {get;}=["install","enable","disable","update","uninstall"];
    public string[] PluginScopes {get;}=["project","user","local"];
    private string searchQuery="claude code skills",searchStatus="A busca só ocorre ao clicar. Estrelas não comprovam qualidade, segurança ou compatibilidade.";
    public string SearchQuery {get=>searchQuery;set=>Set(ref searchQuery,value);}
    public string SearchStatus {get=>searchStatus;set=>Set(ref searchStatus,value);}
    private ClaudeProject? selectedProject;
    public ClaudeProject? SelectedProject {get=>selectedProject;set=>Set(ref selectedProject,value);}
    private CatalogItem? selectedCatalog;
    public CatalogItem? SelectedCatalog {get=>selectedCatalog;set {Set(ref selectedCatalog,value);Changed(nameof(CatalogInstallTutorial));}}
    private CatalogDocument? catalogDocument;
    public CatalogDocument? CatalogDocument {get=>catalogDocument;set=>Set(ref catalogDocument,value);}
    private string catalogReadme="Selecione uma ferramenta para ler o README.",catalogManifest="Selecione uma ferramenta para ver o manifesto.",catalogStatus="Catálogo curado com limites de compatibilidade.";
    public string CatalogReadme {get=>catalogReadme;set {Set(ref catalogReadme,value);Changed(nameof(CatalogReadmeHtml));}}
    public string CatalogManifest {get=>catalogManifest;set=>Set(ref catalogManifest,value);}
    public string CatalogStatus {get=>catalogStatus;set=>Set(ref catalogStatus,value);}
    public string CatalogReadmeHtml=>"<html><head><meta charset='utf-8'><style>body{font-family:'Segoe UI',sans-serif;padding:16px;color:#172B3A;} img{max-width:100%;} pre{background:#F3F5F7;padding:10px;border-radius:6px;overflow:auto;} code{background:#F3F5F7;padding:2px 4px;border-radius:4px;} a{color:#167D80;}</style></head><body>"+Markdig.Markdown.ToHtml(CatalogReadme,MarkdownPipeline)+"</body></html>";
    public string CatalogInstallTutorial=>SelectedCatalog is null ? "" :
        $"Como instalar:\n1. Revise o repositório: {SelectedCatalog.Url}\n2. No Claude Code: claude plugin marketplace add {SelectedCatalog.Repository}\n3. Depois: claude plugin install {SelectedCatalog.Name}@{SelectedCatalog.Repository.Split('/')[0]}\n\nComo usar:\n{SelectedCatalog.WhenToUse}\n\nDica de instalação do autor:\n{SelectedCatalog.InstallHint}";
    private string catalogFilter="",newCatalogRepository="",newCatalogName="",newCatalogSummary="",newCatalogWhenToUse="",newCatalogInstallHint="",newCatalogCompatibility="",newCatalogStatus="Preencha os campos ou use \"Obter informações\" para tentar preencher automaticamente.";
    private CatalogItemKind newCatalogKind=CatalogItemKind.Skill;
    private bool isCatalogModalOpen,isAddCatalogModalOpen;
    public string CatalogFilter {get=>catalogFilter;set {Set(ref catalogFilter,value);Changed(nameof(FilteredCatalogItems));}}
    public string NewCatalogRepository {get=>newCatalogRepository;set=>Set(ref newCatalogRepository,value);}
    public string NewCatalogName {get=>newCatalogName;set=>Set(ref newCatalogName,value);}
    public string NewCatalogSummary {get=>newCatalogSummary;set=>Set(ref newCatalogSummary,value);}
    public string NewCatalogWhenToUse {get=>newCatalogWhenToUse;set=>Set(ref newCatalogWhenToUse,value);}
    public string NewCatalogInstallHint {get=>newCatalogInstallHint;set=>Set(ref newCatalogInstallHint,value);}
    public string NewCatalogCompatibility {get=>newCatalogCompatibility;set=>Set(ref newCatalogCompatibility,value);}
    public string NewCatalogStatus {get=>newCatalogStatus;set=>Set(ref newCatalogStatus,value);}
    public CatalogItemKind NewCatalogKind {get=>newCatalogKind;set=>Set(ref newCatalogKind,value);}
    public CatalogItemKind[] CatalogItemKinds {get;}=[CatalogItemKind.Skill,CatalogItemKind.Plugin,CatalogItemKind.Tool,CatalogItemKind.Agent];
    public bool IsCatalogModalOpen {get=>isCatalogModalOpen;set=>Set(ref isCatalogModalOpen,value);}
    public bool IsAddCatalogModalOpen {get=>isAddCatalogModalOpen;set=>Set(ref isAddCatalogModalOpen,value);}
    public ExecutionProfile SelectedProfile {get; private set;} = new("Equilíbrio","sonnet","haiku","opus",false,"Orquestração geral","Sonnet conduz a tarefa; Haiku atende subagentes. Opus é uma recomendação para consulta deliberada, não um argumento --advisor do Claude Code.");
    private string profileStatus="Escolha um perfil. A alteração vale para as próximas conversas do Metapod.";
    public string ProfileStatus {get=>profileStatus;set=>Set(ref profileStatus,value);}
    public ExecutionProfile[] Profiles {get;} =
    [
        new("Economia","sonnet","haiku","opus",true,"Custo previsível","Sonnet no fluxo principal e Haiku forçado para subagentes."),
        new("Equilíbrio","sonnet","haiku","opus",false,"Orquestração geral","Sonnet conduz a tarefa; Haiku atende subagentes. Opus é uma recomendação para consulta deliberada, não um argumento --advisor do Claude Code."),
        new("Qualidade","opus","sonnet","opus",false,"Decisões complexas","Opus no fluxo principal; Sonnet para subagentes quando adequado."),
        new("Exploração","sonnet","haiku","opus",false,"Pesquisa e alternativas","Mantenha tarefas pequenas e compare hipóteses antes de aumentar o modelo.")
    ];
    public ICommand NavigateCommand {get;} public ICommand ChooseProjectCommand {get;} public ICommand RefreshCommand {get;}
    public ICommand EditCommand {get;} public ICommand NewCommand {get;} public ICommand PrepareCommand {get;} public ICommand ApplyCommand {get;}
    public ICommand RestoreCommand {get;} public ICommand ContextCommand {get;} public ICommand AddCodeCommand {get;} public ICommand SendCommand {get;}
    public ICommand CancelCommand {get;} public ICommand ListPluginsCommand {get;} public ICommand PlanPluginCommand {get;} public ICommand SearchCommand {get;} public ICommand SelectProjectCommand {get;} public ICommand LoadCatalogCommand {get;} public ICommand CloseCatalogModalCommand {get;} public ICommand SelectProfileCommand {get;}
    public ICommand OpenAddCatalogModalCommand {get;} public ICommand CloseAddCatalogModalCommand {get;} public ICommand FetchCatalogInfoCommand {get;} public ICommand SaveCatalogItemCommand {get;} public ICommand OpenCatalogRepositoryCommand {get;}
    public ICommand OpenRepositoryCommand {get;} public ICommand PromptBlockCommand {get;} public ICommand CopyCommand {get;} public ICommand UseResponseCommand {get;}
    public ICommand ClearContextCommand {get;}
}
