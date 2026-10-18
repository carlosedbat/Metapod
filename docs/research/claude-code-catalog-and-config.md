# Claude Code: configuração, catálogo e implicações para o Harness Helper

Pesquisa em 17 de outubro de 2026. As afirmações sobre Claude Code apontam para a documentação oficial da Anthropic; as ferramentas de terceiros são descritas a partir de seus próprios repositórios.

## Descoberta local e escopos

No Windows, a raiz pessoal padrão é `%USERPROFILE%\\.claude` (isto é, `~/.claude`). `CLAUDE_CONFIG_DIR` pode substituí-la; nesse caso, configurações, histórico de sessões e plugins passam a ser gravados nessa nova raiz. O aplicativo deve descobrir essa variável antes de assumir o caminho padrão, e apresentar ao usuário a raiz efetiva, nunca tratar uma pasta inexistente como erro.

| Escopo | Caminho principal | Uso e visibilidade |
|---|---|---|
| Pessoal | `~/.claude/settings.json` | Preferências do usuário em todos os projetos: inclusive modelo padrão e regras pessoais. |
| Projeto compartilhado | `<projeto>/.claude/settings.json` | Configuração versionável para a equipe. |
| Projeto local | `<projeto>/.claude/settings.local.json` | Sobrescritas pessoais para um projeto; Claude a mantém fora do Git quando a cria. |
| Estado de Claude | `~/.claude.json` | Sessão de login, MCP, decisões de confiança e estado por projeto. É arquivo escrito pelo Claude Code; a tela deve expor leitura/diagnóstico e só propor alterações explícitas. |
| Skills pessoais | `~/.claude/skills/<nome>/SKILL.md` | Carregadas em todos os projetos locais. |
| Skills do projeto | `<projeto>/.claude/skills/<nome>/SKILL.md` | Carregadas para sessões do repositório. |
| Subagentes pessoais | `~/.claude/agents/*.md` | Configurações reutilizáveis do usuário. |
| Subagentes do projeto | `<projeto>/.claude/agents/*.md` | Configurações específicas do repositório. |

Os quatro primeiros escopos e sua precedência estão documentados em [Settings files and precedence](https://code.claude.com/docs/en/settings). Em especial, a Anthropic especifica o caminho Windows e o efeito de `CLAUDE_CONFIG_DIR` no mesmo documento. As localizações de skills estão em [Extend Claude with skills](https://code.claude.com/docs/en/skills), e o monitoramento de `~/.claude/agents/` e `.claude/agents/` está em [Create custom subagents](https://code.claude.com/docs/en/sub-agents).

### Como a tela de projetos deve funcionar

O seletor deve ter duas fontes distintas:

1. **Projeto atual**: a pasta escolhida pelo usuário, mostrada com sua árvore `.claude`, seus arquivos `CLAUDE.md`/`AGENTS.md`, settings e recursos encontrados.
2. **Ambiente Claude**: raiz pessoal efetiva, `~/.claude.json` e inventário de skills, agentes, plugins e marketplaces globais. Esses itens não desaparecem quando se troca de projeto.

Para explicar por que algo está ou não ativo, o aplicativo pode instruir a execução de `/status` e `claude doctor`: a documentação diz que `/status` lista as fontes de settings carregadas e `doctor` lista entradas rejeitadas. Uma sessão pode ser retomada por ID com `claude --resume <session-id>` e a mais recente no diretório atual com `claude --continue`; o gerenciador deve oferecer essas ações como comandos revisáveis, sem reconstruir por conta própria o formato interno de transcrições. Consulte a [referência CLI](https://docs.anthropic.com/en/docs/claude-code/cli-usage).

## Agentes, subagentes e skills

Uma skill é uma instrução/fluxo reutilizável em `SKILL.md`; uma descrição pode ficar disponível no início da sessão e o conteúdo completo só é carregado quando usado. Um subagente é um trabalhador isolado, definido em Markdown com frontmatter YAML e um prompt de sistema no corpo. Para cada arquivo de agente, a página deve mostrar nome, descrição, escopo, origem, modelo, ferramentas permitidas e negadas, skills pré-carregadas, MCP, memória, hooks e isolamento.

O modelo por subagente pode vir de quatro lugares, nesta ordem: parâmetro da invocação, frontmatter `model`, variável `CLAUDE_CODE_SUBAGENT_MODEL`, e modelo da conversa principal. `CLAUDE_CODE_SUBAGENT_MODEL` é apenas o padrão. Para impor um modelo a todos, a Anthropic exige também `CLAUDE_CODE_SUBAGENT_MODEL_FORCE=1`; isto ignora o campo `model` nas definições. Portanto, a tela deve distinguir claramente **padrão** de **forçar todos**, antes de sugerir algo equivalente a `claude --model opus` com subagentes Haiku. A referência oficial está em [Choose a model](https://code.claude.com/docs/en/sub-agents#choose-a-model) e [Run every subagent on one model](https://code.claude.com/docs/en/sub-agents#run-every-subagent-on-one-model).

Para a conversa principal, `--model` define o modelo da sessão; `/model` pode persistir o padrão do usuário para novas sessões. A sintaxe confirmada pela Anthropic é `claude --model opus`, não `--advisor`. O campo `advisor` não aparece na referência atual de flags, então o aplicativo deve oferecer uma validação contra `claude --help` no computador do usuário em vez de presumir que ele é suportado. Fontes: [CLI reference](https://docs.anthropic.com/en/docs/claude-code/cli-usage) e [Settings files and precedence](https://code.claude.com/docs/en/settings).

Subagentes de plugin aparecem com identificador com escopo, por exemplo `plugin:agente`, e têm restrições: hooks, MCP embutido e `permissionMode` no frontmatter são ignorados quando vêm de um plugin. O catálogo deve tornar esta limitação visível. Veja [Create custom subagents](https://code.claude.com/docs/en/sub-agents).

## Plugins, marketplaces e README

Um marketplace é um repositório ou diretório que possui `.claude-plugin/marketplace.json`; ele lista plugins e suas fontes, não é uma loja hospedada. Um plugin instalado pode carregar skills, agentes, hooks, MCP e outros componentes. Como um plugin pode executar código com os privilégios do usuário, o Harness Helper deve abrir a fonte, manifest e README antes de qualquer instalação e conservar a aprovação por escopo.

Para a página de detalhes proposta, a leitura do README deve usar a URL bruta de `README.md` do repositório/ref escolhido, renderizada como Markdown local, com uma aba separada para manifesto/arquivos de instalação. O aplicativo não deve inferir segurança a partir de estrelas ou de descrição comercial. A Anthropic recomenda abrir o plugin em `/plugin` antes de instalar e documenta estimativa de custo de contexto em plugins do marketplace oficial. Fontes: [Plugins overview](https://code.claude.com/docs/en/plugins) e [Plugin security and trust](https://code.claude.com/docs/en/plugins/security).

## Catálogo inicial de ferramentas citadas

| Ferramenta | Repositório e alegação do autor | Instalação declarada | Limites relevantes para o produto |
|---|---|---|---|
| Cartographer | [miltonian/cartographer](https://github.com/miltonian/cartographer) — plugin que constrói um modelo persistente do código ligado a evidências no fonte; inclui MCP/serviço local e UI. | `/plugin marketplace add miltonian/cartographer`, depois `/plugin install cartographer@cartographer`; há modo de desenvolvimento com `--plugin-dir`. | Exige Node 20+ e Claude Code CLI. O roadmap ainda cita detecção de dados obsoletos, otimização de bases grandes e suporte multiprojeto; não apresentar o mapa como sempre atualizado. |
| claude-code-setup | [b33eep/claude-code-setup](https://github.com/b33eep/claude-code-setup) — instalador modular de comandos, hooks, MCP, skills e templates, com `/init-project`. | `quick-install.sh` baixa um checkout temporário e chama `install.sh`. | Repositório arquivado pelo autor em 05/10/2026. O fluxo usa script remoto por pipe; o aplicativo deve abrir e revisar os scripts em vez de instalá-los automaticamente. |
| claude-mem | [thedotmack/claude-mem](https://github.com/thedotmack/claude-mem) — memória persistente que observa ferramentas, cria resumos e recupera contexto semântico entre sessões. | `npx claude-mem install`, ou marketplace `thedotmack/claude-mem` seguido da instalação de `claude-mem`. | Requer Node 20+ e suporte a plugins. O instalador pode abrir login e selecionar provedor; `npm install -g` sozinho não registra hooks/worker. Em Windows, o README pede reiniciar terminal/IDE após instalar. |
| Headroom | [dittofleet/headroom](https://github.com/dittofleet/headroom) — aplicativo de barra de menu que mostra uso e reset de limites do Claude e Codex. | Instalador para macOS; skill opcional pode ser copiada para `~/.claude/skills/headroom`. | Não é solução para Windows nem um compressor de contexto. Usa endpoints não oficiais e lê tokens de sessão; a tela deve classificá-lo como incompatível com a plataforma atual e sensível a credenciais. |
| OmniRoute | [ourines/omniroute](https://github.com/ourines/omniroute) — gateway para rotear CLIs para provedores/modelos e aplicar fallback. | `omniroute setup-claude`; iniciar por `omniroute launch --profile <nome>`. | Para Claude Code, usa `ANTHROPIC_BASE_URL`, token e modelo no início da sessão; não existe `--base-url` no CLI. Perfis não devem gravar token e podem exigir descoberta de modelo habilitada. |

As informações da tabela vêm dos READMEs/guias dos repositórios indicados. Os nomes Cartographer e Headroom são ambíguos no GitHub; os links acima são os projetos cuja descrição coincide com a solicitada.

## Implicação para memória e contexto

A área de “Memórias” deve separar três coisas que têm risco e implementação diferentes:

1. **Memória nativa por subagente**: frontmatter `memory: user`, `project` ou `local`, quando suportado pela versão instalada.
2. **Instruções persistentes**: `CLAUDE.md`/`AGENTS.md` e skills, que são arquivos de texto inspecionáveis e versionáveis.
3. **Provedores externos**: Notion, Hermes ou outro MCP/plugin. Cada conexão deve mostrar escopos, origem, dados que saem da máquina, manifesto/README e comando de instalação antes de habilitar.

Essa separação evita chamar qualquer integração externa de “memória” sem revelar se ela lê arquivos locais, inicia processos, armazena dados ou envia contexto a um serviço. A documentação de subagentes lista `memory` entre os campos de frontmatter e a de extensões diferencia CLAUDE.md, skill, subagente, MCP, hook e plugin: [Create custom subagents](https://code.claude.com/docs/en/sub-agents) e [Extend Claude](https://code.claude.com/docs/en/features-overview).
