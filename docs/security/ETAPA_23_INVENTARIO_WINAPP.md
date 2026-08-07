# Etapa 23 - Inventario de acesso direto do WinApp

## Estado e escopo

Esta etapa foi executada somente no recorte de inventario, contratos e endpoints
minimos. Nenhum acesso do `WinAppDtudo` foi removido ou redirecionado e a Etapa 24
nao foi iniciada.

As Etapas 21 e 22 foram concluidas no Development local. A liberacao formal da
migracao continua condicionada somente aos controles externos ja registrados
nessas etapas, como ACL do host e exercicio real de Defender/AMSI.

## Evidencia do inventario

As buscas foram limitadas ao `WinAppDtudo` e aos termos previstos na etapa:

- `DbContext`, `DbSet`, `SqlConnection`, `SqlCommand`, `SqlDataReader`,
  `EntityFramework`, `FromSql`, `ExecuteSql`, `SaveChanges` e `UseSqlServer`:
  nenhuma ocorrencia no cliente.
- `System.IO`, `File`, `Directory`, `Path`, streams e enumeracao de arquivos:
  ocorrencias relevantes em `AnalizadorDeEstruturas`, `CriadorDeEstruturas`,
  `ImportadorAnimesMyAnimeService`, `AppConfigurationService`,
  `ProtectedTokenStore` e servicos de inicializacao.
- `sqllocaldb.exe`: uma operacao em `DtudoSiteStartupService`, usada somente
  para iniciar a instancia local antes de abrir o site.

Nao foram tratados como acesso a banco ou a raiz protegida os arquivos usados
para configuracao da aplicacao, DPAPI da sessao, descoberta de Chrome/npm,
logs locais da operacao e selecao explicita de uma pasta pelo operador. Eles
continuam registrados como superficies locais e possuem destino proprio na
matriz abaixo.

## Matriz de migracao

| Acesso atual no WinApp | Proprietario atual | Substituto alvo | Permissao | Idempotencia | Consistencia e ordem | Teste de migracao |
| --- | --- | --- | --- | --- | --- | --- |
| `ApiMyAnimesService` faz leituras de `Anime` e `MyAnime` por HTTP | `ApiMyAnimes` | `GET /apiLocal/Anime`, `GET /apiLocal/Anime/{malId}`, `GET /apiLocal/MyAnime` e `GET /apiLocal/MyAnime/{id}` | `catalog.read` quando protegido; leituras publicas do catalogo permanecem publicas conforme Etapa 14 | Leitura sem efeito; pagina e ID sao estaveis | Ler antes de qualquer decisao de edicao; `MyAnimeId` continua sendo a relacao interna do DB_Local | 401/403 em rotas protegidas, 200 nas leituras publicas e preservacao da normalizacao de busca |
| `AdicionarMyAnimeAsync` usa consulta previa, `POST /apiLocal/MyAnime` e trata conflito | `ApiMyAnimes` | `PUT /apiLocal/catalog-migration/my-animes/by-title` | `catalog.write` | Chave natural e o titulo normalizado; replay sequencial mescla IDs sem criar outra colecao | Garantir a colecao antes de associar animes; resposta devolve o ID persistido | Criacao seguida de replay, titulo com espacos, IDs repetidos e duas colecoes distintas |
| `AdicionarAnimeAsync` envia `AdicionaAnimeDto` | `ApiMyAnimes` | `POST /apiLocal/Anime`, ja existente e protegido; em `409`, consultar o anime e usar o comando de associacao abaixo | `catalog.write` | O `POST` existente nao deve ser repetido cegamente; `409` e resultado recuperavel por `MalId` | Criar o anime antes da associacao; nao substituir detalhes existentes somente por causa de replay | Criacao, `409`, consulta posterior, cancelamento e associacao sem perda de detalhes |
| `AssociarAnimeAoMyAnimeAsync` le o anime e faz `PUT` do objeto inteiro | `ApiMyAnimes` | `PUT /apiLocal/catalog-migration/animes/{malId}/my-anime` | `catalog.write` | Define o mesmo `MyAnimeId` e adiciona o `MalId` uma unica vez na colecao | Uma chamada `SaveChanges` atualiza o vinculo e a lista da colecao; exige que ambos os registros existam | Primeira associacao, replay sem alteracao, reassociacao e IDs inexistentes |
| `AtualizarMyAnimeAsync` / `AtualizarAnimeAsync` | `ApiMyAnimes` | `PUT` existente por ID; `PATCH` existente quando a alteracao for parcial | `catalog.write` | Repeticao depende do corpo completo; nao usar como comando de importacao | Executar depois da leitura e validar conflito de titulo antes de salvar | Equivalencia do formulario, campos imutaveis e resposta 404/409 |
| `RemoverMyAnimeAsync` / `RemoverAnimeAsync` | `ApiMyAnimes` | `DELETE` existente por ID | `catalog.delete` e step-up quando aplicavel | Nao e parte do import; retry deve tratar 404 conforme politica de exclusao | Excluir somente em fluxo explicito e auditado; nao faz parte da ordem de importacao | 401/403, step-up, 404 e auditoria |
| `AnalizadorDeEstruturas.AnalisarDiretorio` enumera a pasta escolhida pelo operador | WinApp, como entrada local de analise | Nenhum endpoint nesta etapa; e uma leitura da origem escolhida, nao da raiz protegida do servidor | Nenhuma permissao de servico; operador controla a selecao local | Leitura sem mutacao; repetir a analise deve produzir o mesmo conjunto para a mesma origem estavel | Executar antes de persistir catalogo; nao enviar caminho absoluto ao servidor | pasta vazia, subpasta inacessivel, extensao invalida, IDs repetidos e nome nao numerico |
| `CriadorDeEstruturas.CriarEstruturaAsync` cria diretorios e salva capas no destino escolhido | WinApp, acesso direto a arquivos | `POST /api/file-storage/import` por arquivo, usando `ObjectId` logico e multipart | `filesystem.command` | `Idempotency-Key` obrigatoria; diario do servico evita duplicidade e suporta reconciliacao | Catalogo deve estar consolidado antes da exportacao; nao existe transacao distribuida entre catalogo e arquivos | magic bytes/MIME, scanner indisponivel, concorrencia, falta de espaco, promocao e ACL |
| `ImportadorAnimesMyAnimeService.SalvarLogErros` cria `LogsImportacao` no diretorio da aplicacao | WinApp, log operacional local | Seq/log estruturado e feedback da operacao; nao e objeto de midia da `ApiFileStorage` | Sem `filesystem.command` | Nomes locais sao apenas diagnosticos e nao sao chave de negocio | Registrar falha sem impedir a reconciliacao do catalogo; nunca registrar segredo, token ou caminho protegido | falha de escrita, redacao de dados sensiveis e continuidade apos erro |
| `DtudoSiteStartupService.StartLocalDbAsync` executa `sqllocaldb.exe start` | Processo do host/ambiente Development | Ciclo de vida do servico fora do cliente; `GET /apiLocal/Health` somente para readiness autenticado | `health.read` para health; `db.owner` fica exclusivo da `ApiMyAnimes` | Nao e operacao de catalogo; iniciar o mesmo servico deve ser responsabilidade do host | Deve ser retirado somente na Etapa 24, depois de provar que o WinApp nao depende de SQL local | verificacao negativa de `sqllocaldb`, health 200/503 e inicializacao sem permissao SQL |

## Contratos adicionados na ApiMyAnimes

Os DTOs compartilhados estao em `LibDtudo.Shared/Dtos/CatalogMigrationDtos.cs`.

### Garantir colecao por titulo

```http
PUT /apiLocal/catalog-migration/my-animes/by-title
Authorization: Bearer <sessao do WinApp>
Content-Type: application/json
```

```json
{
  "titulo": "Colecao A",
  "animesMalId": [1, 2, 2]
}
```

O servidor normaliza o titulo, remove IDs invalidos/duplicados e retorna
`201 Created` na primeira criacao ou `200 OK` em replay/mesclagem. O comando
nao aceita caminho, connection string, `accountId` ou qualquer identificador
de banco. A autorizacao e `permission:catalog.write` com escopo
`catalog.write`.

### Garantir associacao por MalId

```http
PUT /apiLocal/catalog-migration/animes/42/my-anime
Authorization: Bearer <sessao do WinApp>
Content-Type: application/json
```

```json
{
  "myAnimeId": 7
}
```

O servidor exige que o anime e a colecao existam, define `Anime.MyAnimeID` e
garante que `42` esteja na lista `MyAnime.AnimesMalId`. Em uma reassociacao,
remove o mesmo `MalId` da colecao anterior na mesma persistencia. A repeticao
retorna `200 OK` com `changed: false` e nao regrava o objeto inteiro do anime.

## Contratos existentes da ApiFileStorage

Nenhum endpoint novo de caminho ou diretorio foi criado nesta etapa. Isso
preserva a regra de que o cliente envia IDs logicos e comandos, nunca raiz
fisica, caminho absoluto, UNC ou caminho relativo livre.

| Rota | Contrato minimo | Resultado |
| --- | --- | --- |
| `POST /api/file-storage/export/plan` | `{ "myAnimeId": 7, "malIds": [42] }` | `ObjectId` logico por anime, sem raiz ou caminho fisico |
| `POST /api/file-storage/resolve` | `{ "objectId": "v1..." }` | Metadados logicos sem raiz ou caminho fisico |
| `POST /api/file-storage/import` | multipart `objectId` + `file` + `Idempotency-Key` | Hash, tamanho, promocao e indicacao de replay |
| `POST /api/file-storage/delete` | `{ "objectId": "v1..." }` + `Idempotency-Key` | Movimento para lixeira e `PurgeAtUtc` de sete dias |
| `POST /api/file-storage/delete/preview` | `{ "objectIds": ["v1..."] }` + sessao/dispositivo | Previa limitada e vinculada ao contexto autenticado |
| `POST /api/file-storage/delete/batch` | `{ "previewId": "guid" }` + step-up valido | Exclusao idempotente item a item para a lixeira |
| `POST /api/file-storage/reconcile` | Nenhum corpo | Retomada de diarios, scanner e purge autorizado |

Todos exigem `permission:filesystem.command` e escopo
`filesystem.command`. O import cria os diretorios pais de forma segura durante
o ciclo de vida; nao foi criado um endpoint generico de `mkdir`. A Etapa 25
implementou o plano por IDs e retirou a resolucao de destinos do WinApp.

## Ordem e consistencia

1. Analisar a origem local em modo somente leitura.
2. Garantir cada colecao por titulo e guardar o ID retornado.
3. Consultar/importar os detalhes da ApiMyAnimeList.
4. Criar cada anime pela API proprietaria; em conflito, consultar por `MalId`.
5. Garantir a associacao pelo endpoint dedicado, mantendo `MyAnimeId` e a lista da colecao coerentes.
6. Preparar o plano por `MyAnimeId`/`MalId` e guardar os `ObjectId` devolvidos.
7. Em fluxo de exportacao, enviar cada arquivo para `ApiFileStorage` somente por `ObjectId`, chave de idempotencia e multipart.
8. Para exclusao em massa, criar previa, confirmar no cliente, obter step-up e enviar somente o `PreviewId`.
9. Reconsultar o estado e reconciliar operacoes de arquivo incompletas.

Catalogo e arquivos nao compartilham transacao distribuida. Uma falha depois
do catalogo e antes da promocao do arquivo deve ser retomada por consulta e
replay idempotente, nunca por acesso direto do WinApp ao banco ou a raiz.

## Testes e criterios para a proxima migracao

Implementados nesta etapa:

- `CatalogMigrationControllerTests` prova criacao/replay sem duplicidade,
  reassociacao consistente e autorizacao, com `6/6` testes focados incluindo
  401, 403 e alcance da validacao do controller.
- `dotnet build .\ApiMyAnimes\ApiMyAnimes.csproj --no-restore` aprovado.
- A suite completa `ApiMyAnimes.Tests` passou `24/24` apos o novo controller.
- A suite existente da `ApiFileStorage` permanece com `29/29`, incluindo
  caminhos maliciosos, reparse/hard link, quarentena, idempotencia e
  reconciliacao.

Obrigatorios antes de remover qualquer acesso do WinApp:

- busca negativa de SQL/EF, connection string, `sqllocaldb.exe`, `File`,
  `Directory` e `Path` nos fluxos migrados;
- replay concorrente dos comandos de catalogo e tratamento de `409` do POST
  de anime sem duplicacao;
- teste sem permissao `db.owner` e sem ACL nas raizes protegidas;
- importacao de arquivo com MIME/extensao/magic bytes validos e invalidos,
  scanner indisponivel, falta de espaco, falha parcial e purge;
- verificacao de que nenhum erro devolve caminho fisico, token, segredo ou
  connection string;
- teste de rollback/reconciliacao para falha entre catalogo e arquivo.

## Riscos residuais e rollback

- As Etapas 21/22 foram concluidas no Development local; ACL do host e
  exercicio real de Defender/AMSI continuam como homologacao externa.
- O `PUT` de colecao fornece idempotencia por chave natural em replay
  sequencial; a restricao de unicidade/concorrencia de titulo deve ser
  comprovada no banco alvo antes da remocao de acessos legados.
- A analise local da origem e a escrita local de logs ainda existem no WinApp
  por decisao de escopo; a exportacao de midia nao usa mais esses caminhos.
- Nenhuma migration, raiz operacional, ACL, arquivo de usuario ou permissao
  do WinApp foi removida. Rollback do trabalho desta etapa consiste em remover
  o controller/DTOs/testes adicionados e o documento, mantendo os endpoints
  anteriores e o estado de banco intactos.
