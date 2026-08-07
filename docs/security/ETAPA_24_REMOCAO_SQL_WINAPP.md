# Etapa 24 - Remocao do SQL direto do WinApp

## Estado

Concluida no Development local em 2026-08-07. A Etapa 25 nao foi iniciada.

## Escopo executado

- O inventario da Etapa 23 nao encontrou `DbContext`, EF ou consultas SQL no
  codigo-fonte do `WinAppDtudo`; a unica superficie de ciclo de vida era
  `sqllocaldb.exe start`.
- O startup do WinApp deixou de iniciar LocalDB e de carregar
  `DTUDO_LOCALDB_INSTANCE`/`LocalDbInstanceName`. O host e a `ApiMyAnimes`
  continuam proprietarios do banco.
- O cliente `ApiMyAnimesService` passou a aceitar a sessao compartilhada do
  WinApp. Mutacoes adicionam bearer automaticamente e repetem uma chamada
  apos `401` usando a renovacao existente.
- Colecoes usam `PUT /apiLocal/catalog-migration/my-animes/by-title`, com
  mesclagem idempotente por titulo e IDs. O importador cria ou reconhece o
  anime pelo `POST` autorizado e sempre confirma a associacao com
  `PUT /apiLocal/catalog-migration/animes/{malId}/my-anime`.
- A associacao usa o `MyAnimeId` interno e atualiza a lista `AnimesMalId` no
  servidor, sem enviar um objeto de anime inteiro para alterar o vinculo.
- A analise local e o feedback percentual/textual da importacao foram
  preservados. Arquivos e logs locais permanecem fora do escopo desta etapa.

## Autorizacao e ausencia de SQL

- Os escopos `catalog.write` e `catalog.delete` foram adicionados ao client
  publico do WinApp para as operacoes de catalogo. A sessao continua protegida
  por PKCE/DPAPI; nenhum segredo foi colocado no React ou no codigo.
- Uma mutacao sem sessao falha antes de enviar request. O WinApp nao possui
  permissao `db.owner`, connection string, `DbContext`, referencia EF ou
  cliente SQL no codigo/configuracao fonte.
- A verificacao negativa excluiu somente `bin/` e `obj/`, que sao artefatos
  gerados e nao superficies de runtime versionadas pelo cliente.

## Validacao

- `dotnet build .\WinAppDtudo\WinAppDtudo.csproj --no-restore`: aprovado;
  permanecem somente os avisos conhecidos de `ProtectedData` e conflito de
  `WindowsBase` do WebView2.
- `dotnet test .\tests\WinAppDtudo.Tests\WinAppDtudo.Tests.csproj --no-restore`:
  `13/13` aprovados.
- `dotnet test .\tests\ApiMyAnimes.Tests\ApiMyAnimes.Tests.csproj --no-restore`:
  `24/24` aprovados, incluindo autorizacao e consistencia dos comandos de
  migracao.
- Teste focado `ApiMyAnimesServiceTests`: `3/3` aprovados, cobrindo bearer,
  PUT de colecao, PUT de associacao e falha fechada sem sessao.
- Varredura PowerShell em `.cs`, `.csproj` e `appsettings*.json` do WinApp,
  excluindo `bin/obj`: zero ocorrencias de SQL/EF/LocalDB/connection string ou
  `db.owner`.

## Riscos residuais

- A validade do client e dos escopos precisa ser confirmada no banco/issuer de
  homologacao com a identidade real do administrador.
- Catalogo e arquivos continuam sem transacao distribuida. O fluxo de
  arquivos, ACLs e raizes protegidas permanece para a Etapa 25.
- Os avisos de build existentes nao foram alterados por esta etapa.

## Rollback

Restaurar as alteracoes do cliente, configuracao e testes desta etapa remove
o transporte autenticado/migrado e o documento, mantendo os endpoints de
migracao da ApiMyAnimes e sem executar migration destrutiva, apagar banco,
raiz ou arquivo operacional. A Etapa 25 nao deve ser iniciada como parte do
rollback.
