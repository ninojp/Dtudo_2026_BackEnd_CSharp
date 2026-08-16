# Integracao da ApiMusicX com o WinAppDtudo

**Status:** implementada na Parte 1.5
**Escopo:** cliente autenticado, health monitoring, inicializacao local e feedback de operacoes

## Fronteiras

- O `WinAppDtudo` e o orquestrador e chama a `ApiMusicX` somente pelo `ApiMusicXService`.
- A `ApiMusicX` continua sendo a unica camada que acessa o SQL Server da Colecao.
- Nenhum form abre conexao SQL ou monta chamadas HTTP diretamente.
- O `ApiMusicXService` cobre leitura, criacao, atualizacao, exclusao e importacao normalizada.
- O token JWT e anexado somente por `WinAppAuthenticationService.SendAuthenticatedAsync`; ele nao e incluido em logs, DTOs ou payloads.

## Configuracao

O WinApp usa:

- `ApiMusicX:BaseUrl`: `https://localhost:63982` por padrao;
- `ApiMusicX:AutoStartUrl`: URL usada somente quando o processo local precisa ser iniciado;
- `DTUDO_API_MUSICX_BASE_URL` e `DTUDO_API_MUSICX_AUTOSTART_URL`: sobrescritas por ambiente.

O recurso `urn:dtudo:api-musicx` foi adicionado ao cliente publico do WinApp no seeder do `ApiIdentity`. As permissoes existentes `catalog.read`, `catalog.write`, `catalog.delete` e `health.read` continuam sendo usadas pela API.

## Inicializacao e saude

`ApiMusicXStartupService` consulta `apiLocal/Health`, usa um `SemaphoreSlim` compartilhado no processo e confirma novamente a disponibilidade antes de iniciar `dotnet run`. O monitor global inclui a ApiMusicX nos probes autenticados e na verificacao de certificados.

Respostas `401` e `403` no health confirmam que o processo esta acessivel; `503` continua indicando banco local indisponivel. A inicializacao nao abre conexao SQL e nao inicia uma segunda instancia quando um processo existente ja esta ativo.

## Feedback e testes

As operacoes do cliente recebem `IProgress<string>` e relatam disponibilidade, envio autenticado, status HTTP, cancelamento, falha registrada e resumo da importacao.

Os testes de `WinAppDtudo.Tests` usam `HttpMessageHandler` fake e `ProtectedTokenStore` temporario, sem criar janela real ou depender da API em execucao.
