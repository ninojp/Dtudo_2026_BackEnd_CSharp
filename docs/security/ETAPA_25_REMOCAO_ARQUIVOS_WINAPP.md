# Etapa 25 - Remocao do acesso direto a arquivos do WinApp

## Estado

Concluida no Development local em 2026-08-07. A Etapa 26 nao foi iniciada.

## Escopo executado

- A `ApiFileStorage` passou a emitir planos de exportacao usando somente
  `MyAnimeId` e `MalId`. O plano devolve `ObjectId` logico e nunca devolve raiz,
  caminho fisico ou caminho relativo ao cliente.
- O `WinAppDtudo` usa cliente autenticado com bearer, IDs de sessao/dispositivo,
  multipart recriavel para retry e `Idempotency-Key`. O criador baixa a capa em
  memoria e envia os bytes para a API; nao cria diretorios nem grava imagens na
  raiz protegida.
- O feedback da exportacao foi preservado e ampliado: preparacao, download,
  envio, reconciliacao, percentual e falhas aparecem durante a operacao.
- A `ApiFileStorage` possui previa de exclusao limitada, vinculada ao usuario,
  sessao, dispositivo e expiracao curta. O lote aceita apenas o `PreviewId`,
  exige step-up para a acao `filesystem.command` por consulta autenticada a
  `ApiIdentity` e executa cada item com chave idempotente.
- A exclusao em lote reutiliza o ciclo existente de lixeira, com `PurgeAtUtc`
  de sete dias e reconciliacao. A tela de detalhes exige confirmacao da previa,
  codigo TOTP e mostra o resumo de itens movidos ou falhos.
- A configuracao do WinApp recebeu somente a URL da API e o escopo
  `filesystem.command`. Nenhuma raiz fisica, ACL, connection string ou segredo
  foi adicionado ao cliente.

## Arquivos principais

- `ApiFileStorage/Configuration/FileStorageOptions.cs`
- `ApiFileStorage/Contracts/FileStorageContracts.cs`
- `ApiFileStorage/Controllers/FileStorageController.cs`
- `ApiFileStorage/Services/FileStorageCommandServices.cs`
- `ApiFileStorage/Program.cs`
- `ApiFileStorage/appsettings.json`
- `WinAppDtudo/Services/FileStorageApiClient.cs`
- `WinAppDtudo/Services/CriadorDeEstruturas.cs`
- `WinAppDtudo/Services/ImageLoaderService.cs`
- `WinAppDtudo/Services/AppConfigurationService.cs`
- `WinAppDtudo/Forms/Frm_MyAnimes.cs`
- `WinAppDtudo/FormsUC/FUC_MyAnimeDetalhes.cs`
- `WinAppDtudo/appsettings.json`
- `ApiIdentity/appsettings.json`
- `tests/ApiFileStorage.Tests/FileStorageCommandTests.cs`
- `tests/WinAppDtudo.Tests/CriadorDeEstruturasTests.cs`
- `tests/WinAppDtudo.Tests/FileStorageApiClientTests.cs`

## Validacao

- `dotnet build .\ApiFileStorage\ApiFileStorage.csproj --no-restore`: aprovado.
- `dotnet test .\tests\ApiFileStorage.Tests\ApiFileStorage.Tests.csproj --no-restore`: **33/33**, 0 falhas e 0 ignorados.
- `dotnet build .\WinAppDtudo\WinAppDtudo.csproj --no-restore`: aprovado, com os dois avisos conhecidos de `ProtectedData` e `WindowsBase`.
- `dotnet test .\tests\WinAppDtudo.Tests\WinAppDtudo.Tests.csproj --no-restore`: **16/16**, 0 falhas e 0 ignorados.
- `dotnet test .\tests\ApiIdentity.Tests\ApiIdentity.Tests.csproj --no-restore`: **57/57**, 0 falhas e 0 ignorados, apos o novo escopo do client.
- Testes focados: comandos da API **2/2**, cliente HTTP **2/2** e criador sem escrita local **1/1**.
- Varredura dos arquivos migrados: zero ocorrencias de `Directory`, `File`, `Path`, `FolderBrowserDialog` ou APIs de ACL em `CriadorDeEstruturas` e `FUC_MyAnimeDetalhes`.
- Varredura do WinApp: nenhum `FileSystemAccessRule`, `DirectorySecurity`, `FileSecurity`, `GetAccessControl` ou `SetAccessControl`; os acessos restantes estao restritos a analise de origem, configuracao, DPAPI, ferramentas locais e log diagnostico.

## Riscos residuais e acoes manuais

- As raizes de Development continuam vazias por falha fechada. Homologacao deve
  configurar uma raiz local absoluta, ACL minima da conta da `ApiFileStorage`,
  allowlist, scanner Defender/AMSI real e a URL da `ApiIdentity`.
- O step-up foi testado com validator controlado no controller e com transporte
  do cliente. O exercicio integrado deve confirmar TOTP, grant, sessao,
  dispositivo, audience e certificado no host de homologacao.
- Catalogo e arquivos continuam sem transacao distribuida. Falha parcial deve
  ser recuperada por replay da chave idempotente e reconciliacao da API.
- A analise da pasta escolhida pelo operador e a escrita de `LogsImportacao`
  permanecem locais por decisao da Etapa 23; nao sao a raiz protegida de midia.

## Rollback

Parar a `ApiFileStorage`, preservar diarios de quarentena/lixeira e restaurar
os arquivos de codigo, configuracao, testes e documentos desta etapa. Nao
apagar raiz, payload, banco ou ACL automaticamente. O rollback do cliente
restaura a exportacao anterior apenas para Development controlado; antes de
qualquer retorno operacional deve ser revalidada a permissao da conta.

## Proxima etapa

A Etapa 26 e a proxima etapa permitida. Ela nao foi iniciada neste chat.
