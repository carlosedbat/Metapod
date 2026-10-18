using System.Net.Http;
using System.Text.Json;
using Metapod.Core;

namespace Metapod.Infrastructure;

public sealed class GitHubCatalog
{
    private readonly HttpClient client = new() { Timeout=TimeSpan.FromSeconds(30) };
    public GitHubCatalog() { client.DefaultRequestHeaders.UserAgent.ParseAdd("Metapod/0.1"); }
    public IReadOnlyList<CatalogItem> Curated { get; } =
    [
        new("cartographer","Cartographer","miltonian/cartographer",CatalogItemKind.Plugin,"Gera um mapa persistente e navegável do código para dar ao Claude contexto estrutural.","Use antes de tarefas grandes em repositórios pouco conhecidos.","Consulte o README e instale somente pela origem verificada.","Requer avaliação do marketplace e do runtime antes da instalação."),
        new("claude-code-setup","claude-code-setup","b33eep/claude-code-setup",CatalogItemKind.Plugin,"Coleção de comandos e configuração inicial para Claude Code.","Use como referência de setup, revisando cada arquivo proposto.","Não execute scripts remotos sem ler o conteúdo.","Repositório arquivado: avalie manutenção e compatibilidade."),
        new("claude-mem","claude-mem","thedotmack/claude-mem",CatalogItemKind.Plugin,"Mantém memória entre sessões por meio de um plugin e armazenamento local.","Use quando a continuidade entre sessões for mais importante que o contexto manual.","Leia permissões, dados retidos e dependências antes de instalar.","Exige Node.js; dados persistidos precisam de política de retenção."),
        new("headroom","Headroom","dittofleet/headroom",CatalogItemKind.Tool,"Aplicativo de barra de menus para acompanhar uso e limites do Claude.","Use para observabilidade de consumo no macOS.","Abra o repositório para confirmar a plataforma.","Não é um compressor de contexto nem uma solução para Windows."),
        new("omniroute","OmniRoute","ourines/omniroute",CatalogItemKind.Tool,"Gateway que encaminha chamadas compatíveis a provedores e modelos diferentes.","Use quando há uma política explícita de roteamento e credenciais externas.","Configure a URL e o segredo pelo provedor; não grave tokens em arquivos.","Avalie custo, privacidade, suporte e compatibilidade da API antes de usar.")
    ];
    public async Task<IReadOnlyList<RepositoryResult>> Search(string query,CancellationToken token)
    {
        if(string.IsNullOrWhiteSpace(query)) throw new InvalidOperationException("Informe a necessidade ou os termos de busca.");
        var url="https://api.github.com/search/repositories?q="+Uri.EscapeDataString(query+" archived:false")+"&sort=stars&order=desc&per_page=20";
        using var response=await client.GetAsync(url,token);
        if(!response.IsSuccessStatusCode) throw new IOException($"GitHub retornou {(int)response.StatusCode}. Aguarde se o limite de consultas foi atingido.");
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        if(doc.RootElement.TryGetProperty("incomplete_results",out var incomplete) && incomplete.GetBoolean()) throw new IOException("O GitHub retornou resultados incompletos. Refine a busca e tente novamente.");
        return doc.RootElement.GetProperty("items").EnumerateArray().Select(x=>new RepositoryResult(
            x.GetProperty("full_name").GetString()!,x.GetProperty("html_url").GetString()!,
            x.GetProperty("description").GetString()??"Sem descrição",x.GetProperty("stargazers_count").GetInt32(),
            x.GetProperty("pushed_at").GetString()??"Não informado",
            x.TryGetProperty("license",out var l) && l.ValueKind==JsonValueKind.Object ? l.GetProperty("spdx_id").GetString()??"Não informada" : "Não informada")).ToArray();
    }
    public async Task<CatalogDocument> LoadAsync(CatalogItem item,CancellationToken token)
    {
        var readme=await ReadContent(item.Repository,"README.md",token);
        var manifest=await ReadContent(item.Repository,".claude-plugin/plugin.json",token);
        var status=readme is null ? "README não encontrado na raiz padrão ou indisponível no GitHub." : "README carregado do repositório público; revise a origem antes de instalar.";
        return new(item,readme ?? "# README indisponível\n\nAbra o repositório para consultar seus arquivos.",manifest ?? "Manifesto de plugin não encontrado neste caminho.",status);
    }
    public async Task<string?> FetchRawText(string url,CancellationToken token)
    {
        try
        {
            using var response=await client.GetAsync(url,token);
            if(!response.IsSuccessStatusCode) return null;
            var bytes=await response.Content.ReadAsByteArrayAsync(token);
            return bytes.Length>ContentRules.MaxBytes ? null : System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch(Exception e) when(e is HttpRequestException or TaskCanceledException) { return null; }
    }
    private async Task<string?> ReadContent(string repository,string path,CancellationToken token)
    {
        using var response=await client.GetAsync("https://api.github.com/repos/"+repository+"/contents/"+path,token);
        if(!response.IsSuccessStatusCode) return null;
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        if(!doc.RootElement.TryGetProperty("content",out var content)) return null;
        var encoded=content.GetString()?.Replace("\n","") ?? "";
        var bytes=Convert.FromBase64String(encoded);
        return bytes.Length>ContentRules.MaxBytes ? "# README muito grande\n\nO arquivo excede o limite local de visualização." : System.Text.Encoding.UTF8.GetString(bytes);
    }
}
