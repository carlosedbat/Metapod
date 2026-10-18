# Pesquisa — gerenciador WPF de configurações de harnesses

Consulta: **17/10/2026**, America/Sao_Paulo. Fontes primárias consultadas ao vivo. Este documento distingue fatos documentados de propostas de produto; não é uma especificação aprovada nem contém implementação.

## Conclusão para o produto

**Recomendação:** começar como gerenciador local de configurações com explicação de origem, diagnóstico e criação assistida. Seu diferencial deve ser responder “o que está ativo neste projeto, por quê, e como mudar com segurança?”. Catálogo, educação e execução de prompts devem apoiar esse fluxo. “Gerenciar tudo de todos os agentes” seria um escopo inicial grande demais.

Não se deve criar uma única hierarquia “global < projeto < local” e reutilizá-la para todos os recursos. As regras verificadas abaixo são distintas. Recomenda-se registrar versão do CLI, diretório de execução, ambiente e proveniência de cada resultado; uma leitura estática do disco não prova o estado de uma sessão em execução.

## 1. Configurações e políticas verificadas

| Recurso | Localização / regra documentada |
|---|---|
| Settings de usuário | `~/.claude/settings.json` |
| Settings compartilhados | `.claude/settings.json` |
| Settings pessoais do projeto | `.claude/settings.local.json` |
| Precedência básica | Managed > JSON de `--settings` > local > projeto > usuário |
| Merge | Listas geralmente se combinam; algumas têm regras próprias. Variáveis de ambiente e flags específicas não formam uma camada universal |
| Windows | `~` corresponde a `%USERPROFILE%`; `CLAUDE_CONFIG_DIR` altera a raiz pessoal |
| Formato | JSON estrito; schema publicado pode atrasar em relação ao CLI |

Settings podem recarregar durante a sessão, mas determinadas opções exigem tratamento específico. `/status` identifica fontes carregadas, não a origem de cada chave. Arquivos locais criados pelo aplicativo precisam de exclusão Git explícita quando ainda não estão ignorados. [Fonte: settings](https://code.claude.com/docs/en/settings).

Políticas no Windows podem vir de servidor, Registro (`HKLM`/`HKCU\SOFTWARE\Policies\ClaudeCode`, valor `Settings`) e arquivos em `C:\Program Files\ClaudeCode\`, incluindo `managed-settings.json`, `managed-settings.d/` e `managed-mcp.json`. Existem regras internas de combinação e fontes ausentes no disco do projeto. [Fonte: managed settings](https://code.claude.com/docs/en/managed-settings).

**Recomendações:** políticas organizacionais somente para consulta no MVP; distinguir “não encontrado”, “não acessível” e “não determinável offline”. Preservar chaves desconhecidas, apresentar diff e origem, salvar com backup e detectar edição concorrente antes de sobrescrever. Não editar arquivos internos de credenciais ou caches como se fossem configuração pública.

## 2. Artefatos têm identidades e precedências próprias

### Instruções e memória

`CLAUDE.md`, regras em `.claude/rules/` e instruções pessoais compõem contexto; instruções conflitantes não têm a mesma garantia determinística de override de settings. Regras pessoais e de projeto podem coexistir e o modelo pode seguir qualquer uma quando conflitam. Instruções não substituem controles de permissão. A documentação atual descreve leitura direta de `AGENTS.md` a partir de v2.1.277, condicionada à presença de arquivos Claude e opções da sessão. No Windows há também `C:\Program Files\ClaudeCode\CLAUDE.md` organizacional. [Fonte: memory](https://code.claude.com/docs/en/memory).

**Recomendação:** mostrar composição e potenciais contradições, sem afirmar que uma instrução “vence” outra. Separar memória gerada automaticamente de instruções deliberadamente mantidas pelo usuário.

### Skills

Skills pessoais usam `~/.claude/skills/<nome>/SKILL.md`; de projeto, `.claude/skills/<nome>/SKILL.md`. Há fontes enterprise, diretórios aninhados/adicionais, plugins e conta sincronizada. Em colisões entre enterprise, pessoal e projeto, a ordem é **enterprise > pessoal > projeto**. Skills de plugins usam namespace. Arquivos antigos de `.claude/commands/` continuam reconhecidos. Frontmatter controla descoberta/invocação; `disable-model-invocation` e `user-invocable` têm efeitos diferentes. [Fonte: skills](https://code.claude.com/docs/en/skills).

**Recomendação:** assistente de criação com objetivo, gatilhos, situações em que não usar, entradas, passos e saída; preview do Markdown real. Distinguir invocação manual, descoberta automática e desativação. Validar nomes reservados e compatibilidade da versão antes de salvar.

### Agentes

Definições são Markdown com frontmatter em `.claude/agents/` ou `~/.claude/agents/`. Prioridade documentada: **managed > `--agents` > projeto > usuário > plugin**. Em projetos aninhados, proximidade do diretório de execução importa. Dentro da mesma árvore, nomes duplicados podem depender da ordem do filesystem. Os campos incluem descrição, modelo, ferramentas e permissões. [Fonte: subagents](https://code.claude.com/docs/en/sub-agents).

**Recomendação:** tratar agente como contexto especializado com capacidades explícitas, e skill como procedimento reutilizável. O wizard deve explicar quando cada um resolve o problema e detectar colisões, sem recomendar um agente para toda tarefa.

### Plugins

Plugin agrupa componentes; `.claude-plugin/plugin.json` descreve identidade e metadados, enquanto o marketplace possui `.claude-plugin/marketplace.json`. Skills, agentes, hooks e servidores MCP podem funcionar separadamente ou ser distribuídos juntos. Componentes habilitados podem acrescentar contexto e processos mesmo sem uma invocação explícita do usuário. [Fonte: plugins overview](https://code.claude.com/docs/en/plugins).

A habilitação é registrada em `enabledPlugins` no escopo escolhido. Compartilhar essa entrada não instala automaticamente o pacote nas máquinas dos colegas. O CLI oferece `claude plugin list`, `install`, `enable`, `disable` e `uninstall`; comandos têm defaults de escopo diferentes. `/plugin` interativo não funciona em `-p`. [Fonte: install and manage plugins](https://code.claude.com/docs/en/discover-plugins).

**Recomendação:** representar separadamente disponível, instalado, habilitado e carregado. Preferir operações oficiais do CLI a mutações nos caches de instalação. Sempre fornecer escopo explícito; mostrar fonte, versão e componentes antes de aplicar mudanças.

### MCP e hooks

MCP local e de usuário ficam em `~/.claude.json`; MCP de projeto fica em `.mcp.json`. Portanto, “local” não significa `.claude/settings.local.json` para MCP. Na hierarquia normal: local > projeto > usuário > plugin > conectores, substituindo a entrada completa; managed pode prevalecer. Em execução `-p`, servidores de projeto podem ser carregados sem o diálogo interativo de aprovação. [Fonte: MCP](https://code.claude.com/docs/en/mcp).

Hooks são ações ligadas a eventos; podem executar comandos, HTTP, ferramentas MCP, prompts ou agentes. Comandos recebem dados de evento por stdin e comunicam resultado por stdout/exit code. [Fonte: hooks](https://code.claude.com/docs/en/hooks).

**Recomendação:** o scanner deve apenas ler. Não iniciar servidor, executar hook ou instalar dependência para descobrir configuração. Testar conexão é uma operação separada e explícita. Segredos devem aparecer mascarados e ser omitidos de prompts, backups compartilháveis e logs.

## 3. Integração de IA e prompts do próprio harness

**Decisão já confirmada pelo usuário durante a análise:** o aplicativo executará o Claude Code instalado. As alternativas abaixo servem para definir limites dessa execução, não para reabrir a escolha de integração principal.

Há integração programática por `claude -p`, saídas estruturadas e streaming, além de pacotes Agent SDK em Python e TypeScript. A página oficial consultada não documenta um pacote Agent SDK C#. `-p` normalmente descobre customizações; `--bare` evita grande parte dessa descoberta e altera a experiência. Erros podem aparecer em stdout, além de stderr e exit code. [Fonte: headless](https://code.claude.com/docs/en/headless).

`--json-schema`, `--max-turns`, `--max-budget-usd` e `--no-session-persistence` existem com restrições de modo/versão. O limite em dólares usa estimativa do cliente e pode ser excedido; não constitui teto financeiro absoluto. [Fonte: CLI reference](https://code.claude.com/docs/en/cli-reference).

A autenticação pode usar assinatura, API ou provedores. No Windows o CLI gerencia `.credentials.json`; `CLAUDE_CONFIG_DIR` também afeta esse arquivo. Em `-p`, `ANTHROPIC_API_KEY` presente é usada diretamente, podendo mudar a forma de cobrança em relação à assinatura esperada. [Fonte: authentication](https://code.claude.com/docs/en/authentication).

**Recomendação para a primeira integração:** processo CLI supervisionado, com executável e versão conhecidos, argumentos estruturados, diretório explícito, timeout, cancelamento e captura limitada/redigida de saída. Não usar shell interpolation para prompts. Não contornar permissões com `--dangerously-skip-permissions`.

Oferecer três operações distintas:

1. **Compor:** biblioteca local de blocos, termos, exemplos e variáveis, sem chamada paga.
2. **Sugerir com IA:** enviar apenas contexto selecionado, receber proposta estruturada e gerar diff; o modelo não grava a configuração.
3. **Testar no harness:** execução explícita no contexto escolhido, mostrando quais recursos podem ser carregados e qual autenticação será usada.

Usar o próprio harness significa aproveitar sua execução autenticada e suas customizações, não extrair um suposto prompt interno completo. Escolher entre isolamento (`--bare`) e fidelidade à configuração do projeto é uma decisão de produto por operação, a validar em protótipo. Se callbacks interativos completos forem indispensáveis, avaliar um processo auxiliar TypeScript/Python, sem assumir equivalência automática com o CLI simples.

## 4. Educação e economia de tokens

A documentação recomenda controlar contexto, limpar sessões entre tarefas distintas, escolher modelos/effort adequados, reduzir integrações ociosas e mover procedimentos especializados de instruções permanentes para skills. Subagentes isolam saída volumosa, mas suas próprias chamadas também consomem uso. Cache e compactação influenciam o custo; compactar também envolve processamento. [Fonte: costs](https://code.claude.com/docs/en/costs).

**Proposta:** cada diagnóstico deve trazer evidência local, impacto provável, sugestão e preview. Exemplos: instruções repetidas; skill com descrição pouco específica; agente com ferramentas além do necessário; plugin habilitado sem utilidade atual. Sem prometer percentual de economia ou usar comprimento de texto como medição exata de tokens.

Uma trilha útil: iniciar projeto → instruções essenciais → primeiro procedimento → agente especializado → ferramentas/permissões → distribuição e avaliação. Terminologia deve ser pesquisável, com exemplos ligados à tarefa. Validar utilidade por tarefas reais: comparar qualidade da saída, tempo, tokens observados e retrabalho; “mais agentes” não é indicador de maturidade.

## 5. Descoberta de ferramentas no GitHub

A REST Search API permite ordenar repositórios por estrelas (`sort=stars&order=desc`), com até 100 itens por página e 1.000 resultados por consulta. Há limites de busca próprios: normalmente 30 requisições/minuto autenticadas e 10 sem autenticação; resultados podem ter `incomplete_results=true`. Busca pública pode funcionar sem autenticação. [Fonte: GitHub Search API](https://docs.github.com/en/rest/search/search#search-repositories).

**Proposta:** catálogo por tarefa e tecnologia, com cache, data da consulta e filtros. Exibir relevância, manutenção, licença, compatibilidade Windows, dependências e proveniência junto às estrelas. “Mais estrelado” deve ser uma ordenação transparente, não um selo de segurança ou qualidade. Não há ranking factual de ferramentas neste relatório: seria necessária uma consulta específica e datada por categoria. Instalação automática baseada apenas em popularidade fica fora do MVP.

### Caso de orientação contextual: Jev

**Necessidade confirmada pelo usuário:** orientar quando, onde e por que uma ferramenta ajuda o projeto, seus agentes e skills. O catálogo deve explicar a aplicação concreta, além de apresentar ferramentas populares.

Segundo a documentação do fornecedor, Jev/TypeSafe recebe estado e perguntas tipadas e retorna escolhas, pontuações e probabilidades; as perguntas são avaliadas de forma independente sobre o mesmo estado. É uma primitiva de decisão, não um gerador de explicações textuais. [Fonte: TypeSafe introduction](https://docs.typesafe.ai/introduction).

O repositório do fornecedor `typesafe-ai/skills` distribui uma skill para projetar workflows e consultar documentação/cookbooks; oferece instalação como plugin Claude. Isso ensina o agente a construir com TypeSafe e não demonstra, por si só, que toda tarefa do agente é automaticamente roteada por Jev. [Fonte: TypeSafe skills](https://github.com/typesafe-ai/skills).

Uma integração diferente, comunitária, `FrancoisChastel/jev-code`, declara ferramentas de classificação, verificação, pontuação e ranking; para Claude combina skill e servidor MCP, com opção de plugin. Seu setup requer Node.js e autenticação de provedor. Essas são características declaradas pelo mantenedor, não integração validada neste aplicativo. [Fonte: jev-code](https://github.com/FrancoisChastel/jev-code).

**Exemplo de recomendação proposta:** ao identificar uma rotina que classifica muitos tickets em categorias fixas, explicar que Jev pode ser avaliado para essa etapa; indicar onde a skill orientadora entra e onde uma ferramenta/API executaria a decisão. Exigir exemplos rotulados e comparar custo total, latência, erros e casos incertos com a implementação atual. Não recomendar Jev apenas porque o projeto possui agentes, nem prometer economia a partir de estrelas ou publicidade. Se a tarefa depende de contexto ausente ou raciocínio aberto, explicar essa limitação.

**Contrato proposto para cada sugestão:** evidência no projeto → problema atendido → recurso recomendado e origem → integração necessária → dados enviados/custo adicional → quando não usar → teste de sucesso → ação opcional com preview. Separar as ações “aprender”, “gerar configuração”, “instalar” e “testar”. Jev é estudo de caso do mecanismo de orientação; sua instalação ou uso automático não foi decidido. Preços e desempenho não foram validados neste levantamento.

## 6. WPF e arquitetura para outros harnesses

WPF oferece XAML e data binding e executa somente no Windows, mesmo usando .NET moderno. [Fonte: WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/).

.NET suporta carregamento de extensões com `AssemblyLoadContext` e resolução de dependências, mas código não confiável não pode ser isolado com segurança dentro de um processo confiável apenas por esse mecanismo. [Fonte: plugins em .NET](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support).

**Proposta arquitetural, ainda não implementada:**

- WPF/MVVM apresenta inventário, editor, diagnóstico e assistentes.
- Núcleo modela artefato, origem, escopo nativo, capacidades, diagnóstico e plano de alteração.
- Adaptador Claude implementa descoberta, resolução específica, validação, exportação e integração CLI.
- Serviço de escrita cuida de diff, backup, alteração concorrente e recuperação.
- Serviço de sugestões aceita um provedor de execução sem misturar cobrança/autenticação com o editor.
- Catálogo e material educativo têm origem, versão/data e cache próprios.

Inicialmente, adaptadores compilados junto ao aplicativo são suficientes. Não criar sistema de DLLs externas antes de existir necessidade. Separar “plugin Claude” de “extensão do gerenciador”. Um futuro adaptador Codex pode reutilizar fluxos de UI e transações, mas precisa de suas próprias regras, formatos e capacidades. Não prometer conversão sem perda.

## 7. Recorte recomendado e decisões abertas

**MVP proposto:** Windows nativo; selecionar projetos; descobrir CLI e raízes; inventário com origem; editor de instruções, skills e agentes; diagnóstico estático; diff/backup; biblioteca de prompts; uma operação explícita de sugestão via CLI. Plugins entram inicialmente como inventário e ações oficiais de instalação/habilitação, se priorizados pelo usuário. MCP/hooks podem começar em leitura/diagnóstico para evitar ampliar demais a superfície do editor.

**Critérios de aceitação propostos:** uma alteração informa exatamente qual arquivo/escopo muda; edição externa não é perdida; JSON desconhecido é preservado; arquivos inválidos não são sobrescritos; origem e sombra de recursos são explicadas; varredura não executa conteúdo; falha do CLI gera erro legível e não aplica sugestão parcial.

**Decisões para entrevista:**

1. Qual tarefa recorrente deve ficar completa primeiro e para qual usuário?
2. Gerenciar projetos existentes, configurar do zero ou ambos? Windows nativo apenas ou WSL obrigatório?
3. IA embutida deve usar assinatura do CLI, API própria ou ambos? Pode enviar código/contexto do projeto?
4. Qual parte da criação precisa de IA, e qual deve continuar disponível offline?
5. O usuário quer terminal/conversa contínua no aplicativo ou sugestões pontuais e abertura do harness externo?
6. Plugins precisam de instalação/atualização no MVP ou inventário e diagnóstico bastam?
7. Quais alterações podem ser aplicadas diretamente e quais exigem preview? Onde guardar histórico local?
8. Haverá distribuição para equipes, políticas corporativas e importação/exportação de perfis?
9. Como medir sucesso: tempo de setup, redução de erro, qualidade das respostas ou consumo observado?

## Limites desta pesquisa

O compartilhamento ChatGPT fornecido não pôde ser recuperado no ambiente; seu conteúdo não foi tratado como fonte nem validado. Documentação online representa o estado consultado, não prova que todos os recursos existem no CLI instalado. Ainda faltam uma matriz de versões suportadas, teste controlado em Windows/WSL, validação das modalidades de conta previstas e pesquisa equivalente para Codex. Nenhum arquivo pessoal de configuração ou credencial foi alterado.
