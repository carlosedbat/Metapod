# Harness Helper — proposta de arquitetura e UI

17/10/2026. Proposta para implementação; os arquivos de design já existem, o aplicativo WPF ainda não foi implementado.

## Base das decisões

A [pesquisa](research/claude-code-manager.md) fundamenta as diferenças entre configurações, agentes, skills, plugins, MCP e instruções. As [decisões de produto](product-decisions.md) registram as escolhas do usuário. MVVM, organização dos projetos e aparência abaixo são propostas de engenharia e design, não resultados obrigatórios da pesquisa.

## WPF com MVVM

Views em XAML cuidam de apresentação, bindings e acessibilidade. ViewModels mantêm estado de tela e comandos assíncronos com cancelamento. Diagnóstico, execução do Claude, parsing e escrita de arquivos ficam fora dos ViewModels. Code-behind pode cuidar exclusivamente de comportamento visual, como foco e seleção.

Começar com quatro projetos, organizados internamente por funcionalidade:

```text
src/
  HarnessHelper.Desktop/
    Features/{Projects,Inventory,Editor,Assistant,Plugins,Discover,History}/
    Shell/
    Resources/{Colors,Typography,Controls}/
    Composition/
  HarnessHelper.Core/
    Projects/
    Inventory/
    Diagnostics/
    Conversations/
    ChangePlans/
    Recommendations/
  HarnessHelper.Claude/
    Discovery/
    Configuration/
    Artifacts/
    Cli/
    Plugins/
  HarnessHelper.Infrastructure/
    FileChanges/
    History/
    Processes/
    GitHub/
tests/
  HarnessHelper.Core.Tests/
  HarnessHelper.Claude.IntegrationTests/
```

Core define os casos de uso, seus resultados e os contratos necessários para acessar recursos externos. Não depende de WPF, disco ou Claude. Claude e Infrastructure implementam esses contratos. Desktop conecta as implementações no ponto de composição; as telas consomem casos de uso do Core.

O adaptador Claude concentra caminhos, versões, precedência por tipo de recurso e operações oficiais do CLI. Infrastructure concentra os mecanismos gerais de processo, arquivos e consultas. Não criar um projeto ou interface para cada classe; criar fronteiras onde existe uma responsabilidade externa ou regra que precisa variar.

## Fluxo de alteração

1. Ler o projeto e registrar origem, escopo e falhas de leitura de cada recurso.
2. Produzir diagnóstico local ou preparar o contexto selecionado para a conversa.
3. Receber uma proposta estruturada: destinos, motivo e conteúdo esperado.
4. Validar a proposta, mostrar diff e pedir aprovação dentro do aplicativo.
5. Verificar se os arquivos mudaram desde a leitura; em conflito, exigir nova revisão.
6. Preservar versões anteriores, gravar e reler para verificar o resultado.
7. Registrar sucesso, falha ou resultado parcial no histórico.

Uma sequência de alterações em vários arquivos não é uma transação atômica universal. Cada etapa precisa de registro e recuperação. Restaurar backup também passa por comparação e revisão. Operações de plugin têm plano próprio; restaurar arquivos não desfaz necessariamente seus efeitos externos.

## Integração do Claude instalado

Detectar a instalação e suas capacidades antes de construir argumentos. Executar o processo sem bloquear a UI, consumir saída progressivamente, tratar timeout, cancelamento, autenticação ausente e término inesperado. A resposta do modelo é uma proposta, nunca autorização de escrita.

O contexto padrão contém configurações e estrutura. Código só entra mediante seleção. Isso exige controlar também ferramentas, MCP e hooks disponíveis ao processo: filtrar apenas o texto do prompt não garante o limite. Um experimento técnico com a versão instalada deve demonstrar esse isolamento antes de habilitar a conversa em projetos reais. Quando a capacidade necessária não puder ser garantida, manter a edição local disponível e explicar a limitação.

Campos desconhecidos devem ser preservados nas edições. Políticas organizacionais são consultadas, não editadas no MVP. Segredos identificados são omitidos de contexto e logs; não há promessa de detecção perfeita.

## UI proposta

Sidebar fixa para Visão geral, Agentes e skills, Assistente, Plugins, Descobrir e Histórico. Cabeçalho com projeto e harness. Área central para a tarefa; painel lateral com orientação contextual quando útil. Escopo e origem acompanham os recursos e as propostas.

O protótipo usa Segoe UI, sidebar azul escura, superfícies claras e ações em verde azulado. Na implementação, cores e medidas ficam em recursos XAML reutilizáveis. Grids, limites mínimos e rolagem devem substituir posicionamento absoluto. Testar escalas de DPI, navegação por teclado, foco visível e contraste; o desenho estático não comprova acessibilidade nem adaptação a todos os tamanhos.

Os 12 quadros cobrem visão geral, inventário, editor, conversa, seleção de contexto, diff, plugins, aprovação de plugin, recomendações, preparação da pesquisa, histórico e mapa de fluxos. Os estados de erro estão descritos no mapa; ainda não são telas detalhadas. Seleção inicial da pasta, resultados de busca e edição de todos os tipos de recurso precisam de detalhamento na implementação.

## Evolução

Configuração do zero reutiliza inventário, modelos, proposta e aplicação. Biblioteca pessoal acrescenta persistência e versionamento de modelos. Um futuro HarnessHelper.Codex implementa capacidades próprias: não reutiliza cegamente os caminhos ou as precedências do Claude. A separação reduz retrabalho, mas não elimina o trabalho específico de cada integração.

## Validação antes da primeira versão

Testes de resolução de escopos por tipo; parsing com campos desconhecidos; preservação de conteúdo; conflitos externos e recuperação após falha parcial; processos simulados com cancelamento e saída inválida. Integração em diretórios temporários, sem modificar configurações pessoais. Smoke test da UI com teclado e DPI. Testes de privacidade devem demonstrar que o processo não lê arquivos não selecionados.
