# Etapa 21 - Fundacao da ApiFileStorage

## Estado do gate

**Concluida no Development local.** A implementacao da fundacao e os controles de autenticacao, IDs logicos, raizes permitidas, handles Windows, canonicalizacao, reparse points, hard links, ACL e TOCTOU foram validados. Os fixtures de symlink real foram executados nesta validacao, sem testes ignorados.

A Etapa 22 nao foi iniciada.

## Implementacao

- `ApiFileStorage` foi adicionado a solucao e usa ASP.NET Core com JWT Bearer, fallback deny-by-default e a policy `filesystem.command`, exigindo permissao e escopo.
- `FileStorage:Roots` aceita somente IDs logicos unicos e caminhos locais absolutos configurados no servidor. UNC, device path, raiz inexistente, raiz reparse e raiz sem ACL de leitura/execucao falham no startup.
- O endpoint `POST /api/file-storage/resolve` recebe somente `ObjectId` logico versionado. O cliente nao envia raiz fisica, caminho absoluto, UNC ou caminho relativo livre.
- `StorageObjectId` usa payload UTF-8/base64url canonico, valida o root ID, rejeita payload duplicado/malformado e devolve somente o ID logico na resposta. O ID nao e mecanismo de autorizacao; a policy continua obrigatoria.
- A resolucao interna rejeita vazio, absoluto, UNC/device, traversal, `%`/encoding, separadores duplicados, nomes reservados, ADS, caracteres invalidos e segmentos ambiguos.
- Cada componente e aberto por handle Windows com `FILE_OPEN_REPARSE_POINT`; o final path e obtido pelo handle, comparado com a raiz canonica e o `NumberOfLinks` precisa ser um. Handles mantem a operacao ancorada no objeto aberto durante a verificacao.
- Destinos de escrita validam por handle todos os componentes existentes, rejeitam os mesmos links/reparse e nao compartilham exclusao durante a verificacao, impedindo a troca de caminho enquanto a resolucao esta ativa.
- O controller nao inclui caminho fisico ou caminho relativo canonico no JSON, problem details ou logs de request.

## Arquivos

- `ApiFileStorage/ApiFileStorage.csproj`
- `ApiFileStorage/Program.cs`
- `ApiFileStorage/Configuration/FileStorageOptions.cs`
- `ApiFileStorage/Contracts/FileStorageContracts.cs`
- `ApiFileStorage/Controllers/FileStorageController.cs`
- `ApiFileStorage/Infrastructure/RequestCorrelationMiddleware.cs`
- `ApiFileStorage/Services/StoragePathExceptions.cs`
- `ApiFileStorage/Services/SecureStoragePathResolver.cs`
- `ApiFileStorage/appsettings.json`
- `ApiFileStorage/appsettings.Development.json`
- `tests/ApiFileStorage.Tests/ApiFileStorage.Tests.csproj`
- `tests/ApiFileStorage.Tests/ApiAuthorizationTests.cs`
- `tests/ApiFileStorage.Tests/StorageAclTests.cs`
- `tests/ApiFileStorage.Tests/StoragePathResolverTests.cs`
- `tests/ApiFileStorage.Tests/TestAuthentication.cs`
- `tests/ApiFileStorage.Tests/UncheckedStorageObjectId.cs`
- `Dtudo2026.slnx`

## Validacao executada

Comando:

```text
dotnet test .\tests\ApiFileStorage.Tests\ApiFileStorage.Tests.csproj --no-restore
```

Resultado: `29` testes descobertos, `29` aprovados, `0` ignorados e `0` falhas. O build de `ApiFileStorage`, `LibDtudo.Shared` e testes foi aprovado.

Cobertura executada:

- anonimo `401`, permissao/escopo ausente `403` e autorizacao valida `200`;
- raiz desconhecida e ID logico invalido;
- absoluto, UNC, device path, traversal, encoding simples/duplo, ADS, nomes reservados e sintaxe invalida;
- ausencia do objeto;
- junction/reparse point;
- hard link identificado pelo link count do handle;
- ACL sem leitura/execucao falhando fechado no startup;
- rename concorrente para fora da raiz sem retorno de metadado externo;
- resposta sem caminho relativo ou raiz fisica.

Os testes `SymbolicLink_IsRejectedBeforeResolution` e `RootConfiguration_RejectsReparsePoint` foram executados, assim como a cobertura de junction, reparse, hard link, ACL e TOCTOU. Os testes de link tambem verificam destinos de escrita.

## Decisoes e riscos residuais

- O servico falha fechado sem raiz configurada; os valores de Development continuam vazios e nenhum caminho local foi versionado como raiz operacional.
- A raiz e o ID logico sao configuracao do servidor; o cliente nao recebe caminho fisico.
- O ID logico e deterministico e nao e segredo. Ele nao substitui autenticacao, autorizacao, ACL ou futura propriedade por recurso.
- A etapa nao implementa upload, quarentena, magic bytes, scanner, promocao, lixeira ou ciclo destrutivo. Esses controles pertencem exclusivamente a Etapa 22.
- A homologacao real ainda precisa validar a conta do processo, ACLs de servico e privilegio de symlink no host escolhido.

## Rollback e acoes manuais

- Rollback: remover o projeto `ApiFileStorage`, o projeto de testes, a entrada correspondente de `Dtudo2026.slnx`, as configuracoes e o documento desta etapa. Nenhum banco, raiz operacional ou arquivo de usuario foi alterado.
- A homologacao real ainda deve validar a conta do processo, ACLs de servico e raizes do host escolhido; nenhuma permissao de producao foi elevada nesta execucao.
- A proxima etapa permitida e a Etapa 22. Ela permanece fora deste chat e nao foi iniciada.
