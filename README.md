# Metapod

Aplicativo WPF local para inventariar e evoluir configurações do Claude Code. A primeira versão lê recursos convencionais, cria e edita agentes/skills/instruções/configurações, mostra um diff antes de gravar, preserva backups e histórico, conversa com o Claude instalado usando um processo restrito e pesquisa repositórios no GitHub somente quando solicitada.

## Executar

```powershell
dotnet run --project .\src\Metapod.Desktop
```

O aplicativo guarda seu histórico em `%LOCALAPPDATA%\Metapod\history`. Ele não lê código automaticamente para a conversa; a pessoa seleciona arquivos individualmente e revisa o contexto preparado antes de enviá-lo.

## Limites da primeira versão

- O inventário é estático: não inicia hooks, MCP, plugins ou comandos descobertos. Ele não representa integralmente políticas de Registro, servidor, diretórios extras, recursos provenientes de plugins ou o estado de uma sessão Claude em execução.
- A conversa exige um Claude Code instalado que aceite os controles de isolamento usados pela aplicação. Ela recebe contexto explícito, com ferramentas, skills, hooks e MCP desabilitados, e não altera arquivos.
- Operações de plugin sempre passam pela tela de revisão. O CLI pode recusar instalações que exijam confirmação de comando externo; a aplicação não a contorna. Efeitos externos de plugins não têm rollback automático.
- Backups cobrem arquivos existentes editados pelo aplicativo. Restaurações também exigem revisão.

O protótipo visual editável permanece em [design/Harness-Helper.figcraft](design/Harness-Helper.figcraft). A arquitetura está documentada em [docs/architecture.md](docs/architecture.md).
