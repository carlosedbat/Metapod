# Protótipo editável — Harness Helper

## Preservação e biblioteca de componentes

Ao adicionar conteúdo novo, preservar todas as páginas, telas e componentes existentes. Não excluir ou substituir conteúdo anterior sem solicitação explícita do usuário.

A página `01 · Produto / protótipo` mantém os 12 quadros originais. A nova página `02 · Componentes reutilizáveis` contém três quadros: fundamentos, controles e estados, e blocos compostos. São 21 componentes nativos, com propriedades de texto e exemplos de instâncias vinculadas. Cores usam variáveis compartilhadas; os estados são componentes separados, não um conjunto de variantes. As telas antigas ainda não foram migradas para instâncias dessa biblioteca.

Backup anterior à adição: `Harness-Helper.before-components.figcraft`. Exportações conferidas: `components-01.png`, `components-02.png` e `components-03.png`.

### Correspondência proposta com WPF

| Figcraft | Implementação WPF |
| --- | --- |
| Cores e medidas | ResourceDictionary com brushes e medidas compartilhadas |
| Button e seus estados | Style e ControlTemplate; triggers ou VisualStateManager |
| TextField | TextBox com label, binding e apresentação de validação |
| Badge | DataTemplate ou controle leve de apresentação |
| NavigationItem | Estilo de ListBoxItem, seleção e foco independentes |
| Card, Alert, ContextSummary e EmptyState | UserControl ou ContentControl composto; dados por binding e ações por ICommand |

A biblioteca é referência de implementação, não conversão automática para XAML. Layout adaptável, acessibilidade, foco e comportamento precisam ser implementados e verificados no WPF.

Abra `Harness-Helper.figcraft` no Figcraft. O documento contém 12 quadros com camadas editáveis e dois pontos de entrada de protótipo: fluxo principal e plugins. Use o botão de apresentação do Figcraft para navegar pelos links.

As imagens `screen-01.png` a `screen-12.png` foram exportadas pelo renderizador do próprio Figcraft e conferidas visualmente. Dados de projeto, conversa e operações são demonstrativos; nenhum comando do Claude, instalação de plugin ou pesquisa no GitHub é executado pelo protótipo. Links representam transições entre quadros, não formulários funcionais.

O arquivo foi salvo localmente. Isso não confirma publicação no servidor de colaboração da porta 47900. A edição foi realizada pela API de controle da aplicação na porta 47901.

## Quadros

1. Visão geral e diagnóstico.
2. Inventário de recursos e origens.
3. Editor de agente com preview.
4. Conversa do projeto.
5. Seleção de contexto.
6. Revisão do diff.
7. Plugins.
8. Aprovação de operação de plugin.
9. Recomendações — exemplo Jev / TypeSafe.
10. Preparação da busca de ferramentas.
11. Histórico e recuperação.
12. Mapa de fluxos e situações de erro.

## Manutenção

`build_prototype.py` gera os comandos em `build.jsonl`. Executá-los cria outro documento; não reproduza o lote para uma alteração pontual. `figcraft-control.ps1` transmite solicitações NDJSON ao processo existente, sem abrir novas instâncias. Os arquivos de resultado registram as respostas da aplicação.

Ver `../docs/architecture.md` para a proposta WPF/MVVM e limites do protótipo.
