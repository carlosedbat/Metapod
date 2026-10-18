# Harness Helper — decisões de produto

Atualizado em 17/10/2026. Duas rodadas de decisões concluídas; escopo consolidado aguardando confirmação de entendimento antes da implementação.

## Decisões confirmadas

- Aplicativo desktop WPF, inicialmente para Claude Code, com arquitetura preparada para outros harnesses, incluindo Codex.
- Público inicial: uso pessoal do usuário em projetos reais.
- Prioridade: analisar configurações existentes, sugerir melhorias e criar o que faltar.
- Evolução desejada: configuração guiada do zero e biblioteca reutilizável. Separar descoberta/diagnóstico, criação/aplicação e modelos reutilizáveis para apoiar essa evolução.
- Integração de IA: executar o Claude Code instalado pelo aplicativo.
- Alterações: apresentar proposta e diff, aplicar após aprovação e guardar backup.
- Orientação contextual: tanto em diagnósticos solicitados quanto em perguntas explícitas.
- Descoberta: catálogo selecionado e busca no GitHub solicitada pelo usuário; considerar compatibilidade, manutenção e estrelas.
- Exemplo de orientação: explicar quando e onde Jev e outras ferramentas ajudam, sua relação com agentes/skills, requisitos, custos e forma de validar o benefício. Isso não autoriza instalação automática nem define Jev como dependência do produto.
- Entrevista: uma pergunta por vez, mostrando progresso x de y.

## Fundamentação

Ver [pesquisa técnica](research/claude-code-manager.md), que separa fontes primárias, recomendações e limites. As propostas desse relatório não se tornam decisões aprovadas automaticamente.

## Decisões técnicas confirmadas

- Conversa vinculada ao projeto e ações rápidas, como analisar configurações e melhorar um agente.
- Windows nativo na primeira versão; integração com WSL fica para evolução posterior.
- Gerenciamento completo de plugins: listar, explicar, instalar, habilitar, desabilitar, atualizar e remover pelo CLI oficial. Toda mutação apresenta ação e escopo para aprovação.
- Contexto padrão da IA: configurações e estrutura do projeto. Conteúdo de código somente após seleção explícita do usuário. Omitir credenciais e segredos identificados; não prometer detecção perfeita.

## Fluxo principal consolidado

1. Selecionar uma pasta de projeto e detectar a instalação do Claude Code.
2. Ler configurações e inventariar instruções, agentes, skills e plugins, indicando origem e escopo. A varredura não executa conteúdo descoberto.
3. Solicitar diagnóstico ou conversar sobre uma necessidade. Preparar contexto respeitando as escolhas de compartilhamento.
4. Apresentar oportunidades justificadas, exemplos, blocos de prompts e ferramentas pertinentes. Consultar o GitHub somente mediante solicitação.
5. Gerar uma proposta de alteração com arquivos, escopos e diff; operações de plugin apresentam comando/operação e efeito esperado.
6. Aplicar após aprovação, preservar backup dos arquivos alterados e verificar o resultado. Registrar falhas e operações parciais sem declará-las concluídas.

## Critérios de aceitação propostos para implementação

- O usuário distingue configuração pessoal, de projeto e local, com limites explícitos quando não for possível determinar o estado efetivo.
- A resolução de recursos respeita regras específicas do adaptador Claude; não aplica uma hierarquia universal a todos os tipos.
- O diagnóstico e a conversa não alteram configurações por conta própria.
- Propostas de escrita mostram destino e diff, preservam campos desconhecidos e detectam alterações externas antes de gravar.
- Backups permitem restaurar arquivos editados. Isso não equivale a rollback garantido de instalações, atualizações ou efeitos externos de plugins.
- Execução do CLI tem cancelamento e erros legíveis; autenticação ausente não provoca instalação ou troca de credenciais automática.
- Código não é incluído automaticamente em prompts nem lido pela execução assistida sem seleção. A restrição deve ser aplicada também às ferramentas disponíveis ao processo Claude, não apenas à montagem do prompt.
- Recomendações explicam problema atendido, evidência, integração, limitações e como avaliar benefício; estrelas não equivalem a qualidade ou economia comprovada.
- Serviços de descoberta, propostas, aplicação, catálogo e execução ficam separados da interface WPF e das regras específicas de cada harness.

## Evoluções posteriores

Configuração guiada do zero; biblioteca reutilizável com versões; adaptador Codex; WSL. A arquitetura deve permitir essas evoluções sem afirmar que serão gratuitas ou conversões sem perda.
# Decisões adicionais — 17/10/2026

- O inventário inicia pelo diretório global do Claude Code (`~/.claude`, ou `CLAUDE_CONFIG_DIR`) e incorpora as configurações do projeto somente depois da seleção.
- A lista de projetos parte de `~/.claude/projects`; uma pasta manual continua sendo a alternativa para caminhos que não possam ser reconstruídos do identificador interno.
- Perfis de execução usam `--model` para o modelo principal e `CLAUDE_CODE_SUBAGENT_MODEL` para subagentes. “Advisor” é apresentado como estratégia explícita de consulta, pois não é uma opção oficial `--advisor` do CLI.
- Segredos de conectores autorizados pelo usuário devem usar o Gerenciador de Credenciais do Windows. O aplicativo não lê, importa ou mostra a autenticação própria do Claude Code.
- Toda ferramenta de catálogo deve mostrar adequação, compatibilidade e README antes de preparar instalação.

