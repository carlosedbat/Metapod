# Claude Code: convenções, organização e descoberta de plugins

Pesquisa em 17 de outubro de 2026. Fontes consultadas ao vivo: documentação oficial em `code.claude.com/docs`, o blog oficial da Anthropic (`claude.com/blog`) e as páginas de referência de marketplace/plugin. Este documento não repete fatos já registrados em `docs/research/claude-code-catalog-and-config.md` e `docs/research/claude-code-manager.md` (localizações de arquivos, precedência básica de settings, tabela de ferramentas de terceiros); ele foca em convenções de nomenclatura, fluxos de organização e descoberta/instalação.

## 1. Guia oficial para organizar CLAUDE.md, skills, subagentes, hooks e plugins

A própria Anthropic documenta isso como "a camada de extensão" do Claude Code, com uma página dedicada a comparar as opções. Ela recomenda começar simples: "Start with CLAUDE.md for project conventions, then add other extensions as specific triggers come up." [Extend Claude Code](https://code.claude.com/docs/en/features-overview).

A página lista gatilhos concretos para adicionar cada recurso, na ordem em que a maioria das equipes os adota:

| Gatilho | Recurso a adicionar |
|---|---|
| Claude erra uma convenção ou comando duas vezes | Adicionar ao CLAUDE.md |
| Você digita o mesmo prompt repetidamente para iniciar uma tarefa | Salvar como skill invocável pelo usuário |
| Você cola o mesmo playbook/procedimento de várias etapas pela terceira vez | Capturar como skill |
| Uma tarefa paralela inunda a conversa com saída que não será referenciada depois | Rotear por um subagente |
| Você quer que algo aconteça sempre, sem perguntar | Escrever um hook |
| Um segundo repositório precisa da mesma configuração | Empacotar como plugin |

Fonte: [Build your setup over time](https://code.claude.com/docs/en/features-overview#build-your-setup-over-time).

### Regra prática explícita para skill vs. subagente vs. CLAUDE.md

A documentação dá uma tabela de comparação direta (seção "Compare similar features"):

- **CLAUDE.md vs. Skill**: "Put it in CLAUDE.md if Claude should always know it: coding conventions, build commands, project structure, 'never do X' rules. Put it in a skill if it's reference material Claude needs sometimes [...] or a workflow you trigger with `/<name>`." Regra de bolso explícita: **"Keep CLAUDE.md under 200 lines. If it's growing, move reference content to skills or split into `.claude/rules/` files."**
- **Skill vs. Subagent**: "Use a subagent when you need context isolation or when your context window is getting full [...] Custom subagents can have their own instructions and can preload skills." E mais adiante: "They can combine. A subagent can preload specific skills (`skills:` field). A skill can run in isolated context using `context: fork`."
- **Hook vs. Skill**: "Use a hook when the action must happen the same way every time and doesn't need Claude to think [...] Use a skill when Claude should decide how to apply the steps." E uma frase direta sobre garantias: **"An instruction like 'never edit `.env`' in CLAUDE.md or a skill is a request, not a guarantee. A `PreToolUse` hook that blocks the edit is enforcement."**

Fonte: [Compare similar features](https://code.claude.com/docs/en/features-overview#compare-similar-features).

A página também documenta explicitamente quando usar `.claude/rules/` em vez de inchar o CLAUDE.md: "Use rules to keep CLAUDE.md focused. Rules with `paths` frontmatter only load when Claude works with matching files, saving context." A documentação de skills confirma esse critério do outro lado: "Create a skill when you keep pasting the same instructions, checklist, or multi-step procedure into chat, or when a section of CLAUDE.md has grown into a procedure rather than a fact." [Create a skill](https://code.claude.com/docs/en/skills).

### Organização de arquivos de skill individuais

A documentação de skills recomenda um layout de projeto específico para skills que crescem: **"Keep `SKILL.md` under 500 lines. Move detailed reference material to separate files"** — sugerindo `SKILL.md` principal mais arquivos `reference.md`, `examples.md` e uma pasta `scripts/`, com o `SKILL.md` linkando para eles para que o Claude saiba quando carregar cada um. Também recomenda manter o corpo conciso porque, uma vez carregado, o conteúdo "stays in context across turns, so every line is a recurring token cost." [Skills](https://code.claude.com/docs/en/skills).

### Organização de múltiplos subagentes

A documentação de subagentes permite (mas não exige) subpastas dentro de `.claude/agents/` e `~/.claude/agents/`, varridas recursivamente: "you can organize definitions into subfolders such as `agents/review/` or `agents/research/`." Ela também alerta sobre custo agregado de contexto: **"When the combined descriptions of your subagents, except the built-in ones, exceed 15,000 tokens, Claude Code shows a warning at startup [...] Trim the `description` fields of your subagents, and move detail into each subagent's system prompt, which only loads when that subagent runs."** [Create custom subagents](https://code.claude.com/docs/en/sub-agents).

### Post oficial do blog da Anthropic sobre o tema

O blog da Anthropic publicou um artigo dedicado a este exato problema: **"Steering Claude Code: CLAUDE.md files, skills, hooks, rules, subagents and more"**, atribuído por um podcast de terceiros (Super Data Science, episódio 1022) a Michael Segner, funcionário da Anthropic — a autoria não foi confirmada por uma assinatura visível no próprio post. [Steering Claude Code](https://claude.com/blog/steering-claude-code-skills-hooks-rules-subagents-and-more); menção da autoria em [SDS 1022](https://superdatascience.com/1022).

O post compara sete mecanismos (CLAUDE.md, rules, skills, subagentes, hooks, output styles e anexos ao system prompt) por quando carregam, se sobrevivem à compactação e quanta autoridade carregam. Pontos centrais reportados:

- Comportamento que precisa acontecer de forma confiável pertence a um hook: "The model choosing to run a formatter is different from the formatter running automatically."
- Proibições rígidas precisam de aplicação via hooks/permissions/managed settings, não apenas de instrução em texto.
- Preferências pessoais vão em arquivos de nível de usuário; preferências de equipe/codebase vão em nível de projeto.
- Em monorepos: dar a cada time um CLAUDE.md de subdiretório e usar a opção `claudeMdExcludes` para pular arquivos de times que não interessam.
- Progressão recomendada ao final do artigo: **"Once you have a few of these working, you can bundle many of them (skills, subagents, hooks, output styles) as a plugin to share a coherent setup across teammates or projects."**

Essas informações vieram de uma busca na web mais um resumo de ferramenta sobre o conteúdo; não foi feita leitura linha a linha do HTML completo do post, então frases fora das citadas entre aspas podem ter nuances não capturadas aqui.

## 2. Convenções de nomenclatura e precedência exata por nome

### Skills

A documentação não define um formato genérico obrigatório (não exige kebab-case explicitamente para skills, diferente de plugins/marketplaces), mas define regras negativas específicas:

- Nome padrão vem do nome da pasta, se o campo `name` não for definido.
- **Nome reservado `synced`**: "don't name a skill folder `synced`, in any capitalization. Claude Code uses `~/.claude/skills/synced/` for skills downloaded from claude.ai and skips a skill you author at that name."
- **Reservado `anthropic-skills`**: "outside a plugin, a skill folder or command file whose name is `anthropic-skills` or starts with `anthropic-skills:` doesn't load."
- Caracteres inválidos: zero-width space, caracteres de controle, caracteres de uso privado/não atribuídos, caracteres que alteram direção do texto, espaços não convencionais, e `<`/`>`.
- Comparação de nomes ignora maiúsculas/minúsculas, espaçamento e caracteres invisíveis, e trata variantes de largura/hífen como equivalentes.

Fonte: [Skills](https://code.claude.com/docs/en/skills).

**Precedência exata entre escopos (quando o mesmo nome existe em mais de um lugar)**, citada textualmente da tabela de colisão da documentação de skills: "Two of enterprise, personal, and project: Enterprise over personal, and personal over project. With `deploy` in both `~/.claude/skills/` and the project's `.claude/skills/`, `/deploy` runs the personal one." Outras regras da mesma tabela: skill vs. arquivo em `.claude/commands/` → vence a skill; skill de plugin nunca colide porque é namespaced como `/plugin-name:skill-name`; skill de usuário substitui um comando embutido/bundled, mas não seus aliases. A página-mãe resume a regra geral assim: **"Skills and subagents override by name: when the same name exists at multiple levels, one definition wins based on priority (managed > user > project for skills; managed > CLI flag > project > user > plugin for subagents)."** [Understand how features layer](https://code.claude.com/docs/en/features-overview#understand-how-features-layer); detalhe de skills em [Resolve skills that share a name](https://code.claude.com/docs/en/skills#resolve-skills-that-share-a-name).

Nota: a tabela geral da página de visão geral cita a ordem "managed > user > project" para skills, enquanto a página específica de skills descreve "enterprise > pessoal > projeto" nos casos de colisão entre esses três — são a mesma hierarquia com nomenclatura diferente (enterprise = managed). Não há contradição, mas o app deve usar os termos exatos de cada página ao citar a fonte.

Campo de metadados livre: skills aceitam um campo `metadata` (mapa YAML livre, "such as entitlement or catalog fields") que o Claude Code não interpreta — é destinado a ferramentas externas, como um catálogo. Não existem campos nativos `tags`, `category` ou `author` em `SKILL.md`; fora do Claude Code, uploads para claude.ai só aceitam `name`, `description`, `license`, `compatibility`, `metadata` e `allowed-tools` — qualquer outra chave causa erro. [Skills](https://code.claude.com/docs/en/skills).

### Subagentes

Regras de nome, citadas da tabela de frontmatter: **"Unique identifier of at most 256 characters, such as `code-reviewer` or `reviewer-v2' [...] Names can't contain `:`, which is reserved for plugin-scoped identifiers such as `my-plugin:reviewer`."** A identidade de um subagente vem exclusivamente do campo `name`, não do caminho do arquivo: "The subdirectory path doesn't affect how a subagent is identified or invoked, because identity comes only from the `name` frontmatter field." Nomes inválidos (começando com `-`, contendo `:` ou com mais de 256 caracteres) fazem o Claude Code pular o arquivo e registrar erro no log de debug. Fonte: [Create custom subagents](https://code.claude.com/docs/en/sub-agents).

**Precedência exata entre escopos**, tabela citada literalmente da documentação:

| Local | Prioridade |
|---|---|
| Managed settings | 1 (mais alta) |
| Flag CLI `--agents` | 2 |
| `.claude/agents/` (projeto) | 3 |
| `~/.claude/agents/` (usuário) | 4 |
| Diretório `agents/` de um plugin | 5 (mais baixa) |

"When multiple subagents share the same name, Claude Code uses the one from the higher-priority location." Em projetos com diretórios aninhados, "Claude Code uses the definition closest to the working directory." Duplicatas dentro do mesmo diretório são resolvidas por ordem de leitura do filesystem, não documentada de forma determinística — o `/doctor` detecta e sugere renomear/remover. Fonte: [Choose the subagent scope](https://code.claude.com/docs/en/sub-agents#choose-the-subagent-scope).

### Plugins e marketplaces (regras mais estritas, com mensagens de erro exatas)

Diferente de skills, nomes de **marketplace** e de **entrada de plugin** em `marketplace.json` têm regra de caracteres explícita e validável por ferramenta: "Marketplace identifier: letters, digits, `.`, `_`, and `-`, starting with a letter or digit, and no `..`." A mensagem de erro de `claude plugin validate` para espaços é literal: `"Marketplace name cannot contain spaces. Use kebab-case (e.g., \"my-marketplace\")"` — ou seja, kebab-case é a convenção recomendada pela própria mensagem de erro oficial, embora tecnicamente `_` também seja aceito. A mesma regra e mensagem equivalente existem para nomes de plugin (`plugins[i].name`). Fonte: [Marketplace reference — Top-level fields](https://code.claude.com/docs/en/plugins/marketplace-reference#top-level-fields) e [Validation messages](https://code.claude.com/docs/en/plugins/marketplace-reference#validation-messages).

Nomes reservados de marketplace são uma lista fechada e específica, não um padrão genérico: `claude-plugins-official`, `claude-code-marketplace`, `claude-code-plugins`, `anthropic-marketplace`, `anthropic-plugins`, `agent-skills`, `anthropic-agent-skills`, `life-sciences`, `knowledge-work-plugins`, `claude-for-legal`, `claude-for-financial-services`, `financial-services-plugins`, `first-party-plugins`, `claude-tag-plugins` (tier oficial); `claude-community`, `claude-plugins-community`, `healthcare` (tier comunidade) — todos reservados apenas quando a fonte do marketplace **não** é um repositório sob `github.com/anthropics/`; third-party que tentar usar esses nomes recebe recusa explícita: `"Claude Code refuses the marketplace name "<name>""`. Há também bloqueio de "nomes que imitam" um nome oficial (ex.: `official-claude-plugins`, `claude-plugins-v2`) e de grafias alternativas do mesmo nome reservado (ex.: `claude.code.plugins` conta como `claude-code-plugins`). Nomes começando com `claudeai-` são reservados para marketplaces hospedados no claude.ai. Fonte: [Reserved names](https://code.claude.com/docs/en/plugins/marketplace-reference#reserved-names).

**Namespacing de skills de plugin**: confirmado tanto na visão geral quanto na referência — uma skill de plugin é sempre endereçada como `/<nome-do-plugin>:<nome-da-skill>` (ex.: `/commit-commands:commit`), o que é citado como o mecanismo que evita colisão de nome entre plugins diferentes: "Plugin skills are namespaced (like `/my-plugin:review`) so multiple plugins can coexist." [Extend Claude Code](https://code.claude.com/docs/en/features-overview). Para subagentes de plugin em subpastas, o caminho entra no nome namespaced: um arquivo em `agents/review/security.md` no plugin `my-plugin` é registrado como `my-plugin:review:security`. [Create custom subagents](https://code.claude.com/docs/en/sub-agents).

## 3. Fluxo real de descoberta/instalação de terceiros no GitHub

A documentação descreve um fluxo de duas etapas, documentado para terminal, app desktop, VS Code e execução via shell:

1. **Adicionar o marketplace** (só é necessário se o plugin não estiver no marketplace oficial, que já vem pré-registrado): `/plugin marketplace add <fonte>` dentro de uma sessão, ou `claude plugin marketplace add <fonte>` no shell. A fonte aceita repositório GitHub (`owner/repo`, com `#ref` opcional para fixar branch/tag), repositório git em qualquer host, diretório/arquivo local, ou uma URL `https://` apontando direto para um `marketplace.json` hospedado. Fonte: [Add a marketplace](https://code.claude.com/docs/en/discover-plugins#add-a-marketplace) *(nota: a URL efetivamente servida por essa página redireciona de `discover-plugins`; o conteúdo citado é da seção "Install and manage plugins" em `code.claude.com/docs/en/discover-plugins`)*.
2. **Instalar o plugin**: `/plugin install <plugin>@<marketplace>` numa sessão — isso **não instala direto**: abre o painel `/plugin` na tela de detalhes daquele plugin para revisão e escolha de escopo antes da instalação de fato. No shell, `claude plugin install <plugin>@<marketplace>` instala diretamente (escopo usuário por padrão, ajustável com `--scope project` ou `--scope local`).

Também existe um comando que combina as duas etapas, citando a fonte do marketplace direto no install: `/plugin install deploy-helper --marketplace your-org/plugins` (sessão) ou `claude plugin install deploy-helper --marketplace your-org/plugins` (shell) — pede confirmação do marketplace resolvido antes de registrar, exceto pelo shell sem sessão interativa.

A UI interativa `/plugin`, sem argumento, "opens the panel on the **Discover** tab, which lists the plugins from your marketplaces, and you can type to search, then press Enter on a plugin to open its details." Fonte: [Install a plugin](https://code.claude.com/docs/en/discover-plugins#install-a-plugin).

### Passos de revisão/confiança recomendados antes de instalar

A página dedicada **"Plugin security and trust"** declara o risco central sem ambiguidade: **"A Claude Code plugin you install can execute arbitrary code on your machine with your user privileges."** E esclarece que o nome do marketplace não é garantia: "A marketplace's name tells you who publishes the catalog, not what each plugin in it does, so review a plugin before you install it whichever marketplace it comes from." [Plugin security and trust](https://code.claude.com/docs/en/plugins/security).

A mesma página também afirma que hooks, monitors e servidores MCP/LSP de um plugin **rodam fora do sandbox** e com permissões totais do usuário: "Claude Code runs hooks, monitors, MCP servers, LSP servers, and the processes a mod starts outside the sandbox." Apenas chamadas de ferramenta feitas pelo próprio Claude (MCP tool calls, comandos Bash que usam o `bin/` do plugin) passam pelas regras de permissão normais.

A documentação lista um checklist de 4 passos explícitos para revisar um plugin antes de instalar:

1. **Checar a fonte do marketplace**: `claude plugin marketplace list` no shell, mostra de onde cada marketplace foi adicionado.
2. **Ler o painel de detalhes**: `/plugin`, selecionar o plugin — mostra seção "Will install" com comandos, agentes, skills, hooks e servidores MCP/LSP que serão adicionados. Para plugins sem dados publicados pela Anthropic, aparece `Components will be discovered at installation` ou `Component summary not available for remote plugin`.
3. **Ler o código-fonte do plugin**: abrir "View on GitHub" no painel, e ler especificamente `hooks/hooks.json` (comando que cada hook executa), `.mcp.json` (comando/URL de cada servidor) e todo o conteúdo de `bin/`.
4. **Listar o conteúdo com a própria ferramenta**: clonar o repositório e rodar `claude --plugin-dir <diretório> plugin details <nome>` no shell, que lê os arquivos sem iniciar sessão e imprime um "Component inventory".

Fonte: [Review a plugin before you install](https://code.claude.com/docs/en/plugins/security#review-a-plugin-before-you-install).

A ferramenta também mostra um aviso de confiança padrão, idêntico para qualquer marketplace, sempre que o painel de detalhes é aberto: **"Make sure you trust a plugin before installing, updating, or using it. Anthropic does not control what MCP servers, files, or other software are included in plugins and cannot verify that they will work as intended or that they won't change."** Organizações podem anexar texto próprio a esse aviso via `pluginTrustMessage` em managed settings. Fonte: [Trust warning before you install](https://code.claude.com/docs/en/plugins/security#trust-warning-before-you-install).

Claude Code **recusa** (não apenas avisa) instalar/carregar em dois casos de integridade verificável: marketplace com nome oficial/comunidade mas fonte fora de `github.com/anthropics/`, e fonte `archive` cujo hash `sha256` declarado não corresponde ao arquivo baixado. Fonte: [Untrusted marketplace sources and failed integrity checks](https://code.claude.com/docs/en/plugins/security#untrusted-marketplace-sources-and-failed-integrity-checks).

### Repositório oficial de exemplo de marketplace

A Anthropic mantém `anthropics/claude-plugins-official` como o marketplace oficial padrão (pré-registrado em toda sessão interativa nova), citado como exemplo na própria documentação ao instalar o plugin `commit-commands`, cujo diretório está em `github.com/anthropics/claude-plugins-official/tree/main/plugins/commit-commands`. Fonte: [Install a plugin](https://code.claude.com/docs/en/discover-plugins#install-a-plugin). Esta pesquisa não abriu o conteúdo bruto do arquivo `marketplace.json` desse repositório para confirmar sua estrutura exata linha a linha; a estrutura mínima documentada (campos `name`, `owner`, `plugins[]`, cada entrada com `name`/`source`/`description`) vem da página de referência oficial, não de uma leitura direta desse repositório.

## 4. Padrão de UX de descoberta/catálogo e esquema de metadados

### O que a UI do `/plugin` mostra

A aba **Discover** do painel `/plugin` lista os plugins de todos os marketplaces registrados, com busca por digitação e abertura de detalhes ao pressionar Enter. O painel de detalhes de um plugin pode mostrar, além da descrição:

- **Will install**: lista de comandos, agentes, skills, hooks e servidores MCP/LSP que o plugin adiciona.
- **Last updated**: exibido apenas para plugins do marketplace oficial da Anthropic.
- **Context cost**: duas estimativas de tokens — "Every turn" (custo em cada mensagem) e "When invoked" (custo quando skills/agentes carregam) — também exibidas apenas para o marketplace oficial, e só quando o plugin é aberto nomeando seu marketplace (não aparecem vindo da lista genérica da aba Discover).

Fonte: [Review what the plugin adds](https://code.claude.com/docs/en/discover-plugins#install-a-plugin).

A aba **Installed** permite filtrar por nome/descrição ao digitar, favoritar com `f`, habilitar/desabilitar com espaço, e mostra separadamente plugins "Not used recently" com uma linha "Last used" — ou seja, a própria Anthropic já implementa telemetria de uso como critério de organização do catálogo instalado, não apenas do catálogo de descoberta. Fonte: [Manage installed plugins](https://code.claude.com/docs/en/discover-plugins#manage-installed-plugins) e [Find plugins you no longer use](https://code.claude.com/docs/en/discover-plugins#find-plugins-you-no-longer-use).

### Esquema de metadados documentado para catalogar um plugin

A referência de `marketplace.json` lista campos de catálogo explícitos em cada entrada de plugin — este é o esquema mais próximo de uma "ficha de catálogo" oficial que a Anthropic documenta:

| Campo | Tipo | Descrição oficial |
|---|---|---|
| `category` | string | "Free-form category for organizing the catalog" |
| `tags` | array de strings | "Free-form tags for search" |
| `displayName` | string | Nome legível mostrado na UI, quando diferente do `name` técnico |
| `description` | string | Mostrado nas listagens/detalhes do `/plugin` |
| `author`, `homepage`, `repository`, `license`, `keywords` | — | Campos de exibição compartilhados entre a entrada do marketplace e o `plugin.json` do próprio plugin; o valor da entrada do marketplace tem precedência quando ambos definem o campo |
| `relevance` | object | "Signals that tell Claude Code when to suggest the plugin" — usado por organizações para sugestão automática de plugins relevantes a uma sessão |
| `metadata` | object | Campo livre não lido pelo Claude Code, destinado a ferramentas externas (ex.: um catálogo próprio) |
| `version` | string | Versão do plugin (quando `plugin.json` também define versão, `plugin.json` tem precedência) |

Fonte: [Plugin entries](https://code.claude.com/docs/en/plugins/marketplace-reference#plugin-entries) e [Display fields](https://code.claude.com/docs/en/plugins/marketplace-reference#entry-and-plugin-json).

Note que `category` e `tags` são descritos textualmente como **"free-form"** — não há uma taxonomia fixa de categorias nem de tags imposta pela Anthropic; cada mantenedor de marketplace escolhe seus próprios valores. Isso significa que um catálogo GUI como o do Harness Helper não pode assumir um conjunto fixo de categorias vindo da documentação oficial — teria que inferir/normalizar categorias observadas nos marketplaces reais, ou adotar sua própria taxonomia própria e mapear o campo livre para ela.

Não foi encontrada, nesta pesquisa, nenhuma página oficial que descreva "categorias" fixas como abas de navegação dentro do `/plugin` (diferente de "Discover", "Installed", "Marketplaces", "Errors", que são abas de *estado de instalação*, não de taxonomia de conteúdo). A navegação por categoria/tag dentro do próprio painel `/plugin`, se existir na UI real do CLI, não está descrita com detalhe suficiente nas páginas consultadas para confirmar seu comportamento exato (ex.: se há um filtro por `category` clicável). Isso deveria ser confirmado observando a ferramenta `/plugin` rodando, não apenas a documentação.

## Limites desta pesquisa

Esta pesquisa usou buscas na web e `WebFetch` sobre páginas HTML renderizadas da documentação oficial (via Mintlify/`code.claude.com`), não uma leitura do fonte Markdown bruto nem do repositório `anthropics/claude-code` em si; duas das páginas fetchadas (`skills` e `sub-agents`) foram truncadas pela ferramenta em ~100.000 caracteres e podem conter seções adicionais além das 100.000 primeiras não cobertas aqui. O resumo do post do blog "Steering Claude Code" veio de uma ferramenta de resumo sobre o HTML da página, não de leitura literal linha a linha, e a atribuição de autoria a Michael Segner vem de uma fonte secundária (podcast Super Data Science), não de uma assinatura visível confirmada no próprio post — trate essa atribuição como prevista, não confirmada. Não foi aberto o conteúdo bruto do repositório `anthropics/claude-plugins-official` nem de `anthropics/claude-code` para inspecionar arquivos reais linha a linha; os exemplos de `marketplace.json` citados vêm da documentação de referência, não de uma leitura direta desses repositórios. Não há, nesta pesquisa, confirmação visual da UI real do comando `/plugin` (screenshots ou execução), apenas a descrição textual da documentação — qualquer afirmação sobre navegação por categoria/tag na UI real do CLI precisaria ser validada rodando a ferramenta. Nenhum arquivo de configuração pessoal foi alterado.
