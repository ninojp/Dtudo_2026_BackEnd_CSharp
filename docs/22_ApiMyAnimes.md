# ApiMyAnimes (DB_Local)

A **ApiMyAnimes** é a API ASP.NET Core (.NET 10) dona do catálogo local de animes, o **DB_Local** (banco SQL Server `Dtudo2026Db`). Ela persiste animes e coleções internas (MyAnime), serve a leitura pública e administrativa do catálogo, sincroniza coleções com estruturas de pastas autorizadas (somente vínculo em banco, nunca altera arquivos) e importa dados de anime a partir da [ApiMyAnimeList](23_ApiMyAnimeList.md).

Contexto da solução: [00_Solucao.md](00_Solucao.md) · Segurança: [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md) · Gateway: [20_DtudoGateway.md](20_DtudoGateway.md) · Identidade: [21_ApiIdentity.md](21_ApiIdentity.md) · Biblioteca compartilhada: [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md).

## Visão geral

| Item | Valor |
|---|---|
| Projeto / testes | `ApiMyAnimes` / `tests\ApiMyAnimes.Tests` |
| Stack | .NET 10, EF Core 10 (SQL Server), JWT Bearer, Serilog (Console + Seq opcional), Swashbuckle |
| Prefixo das rotas | `apiLocal` |
| URLs de desenvolvimento | `https://localhost:63980` / `http://localhost:63981` |
| Audience do token | `urn:dtudo:api-my-animes` (Issuer: ApiIdentity) |
| Consumidores | `DtudoGateway` (leitura do site), `WinAppDtudo` (leitura, escrita, migração, monitoramento) |
| Depende de | `LibDtudo.Shared`, ApiMyAnimeList (apenas na importação por `malId`), ApiIdentity (tokens) |

## Modelo de dados

O contexto é `MyAnimesContext`. Tabelas e papéis:

| Tabela | Chave | Conteúdo essencial |
|---|---|---|
| `Animes` | `MalId` (informado, `ValueGeneratedNever`) | Título (`Titulo`), `SubTitulos`, `Episodios`, `MalUrl`, `ImagensUrlMal` (lista de URLs em JSON), `MyAnimeID`, `AnimesRelacionadosIds` e os campos detalhados vindos do MAL (`Title`, `TitleEnglish`, `TitleJapanese`, `TitleSynonyms`, `Type`, `Source`, `Status`, `Aired`, `Duration`, `Rating`, `Score`, `Synopsis`, `Season`, `Year`, `Studios`, `Genres`, `ExplicitGenres`, `Themes`, `Demographics`, `Trailer` etc.) |
| `MyAnimes` | `Id` (identity) | `Titulo` e `AnimesMalId` (lista de `MalId` em JSON) |
| `MyAnimeMonitoringLocations` | `MyAnimeId` (1:1 com `MyAnimes`, cascade) | `RootKey` (≤ 100) e `RelativePath` — vínculo da coleção com a pasta local |
| `MyAnimeMonitoringSyncRuns` | `Id` (Guid) | Execução de sincronização: início/fim, `Status`, contadores, `Error` (≤ 2000); índice por início |
| `MyAnimeMonitoringSyncEvents` | `Id` (long) | Ocorrências da execução (`Severity`, `Code`, `RootKey`, `RelativePath`, `MyAnimeId`, chaves anterior/nova, `Detail`); **append-only** (o `SaveChanges` rejeita alteração e exclusão) |
| `SecurityAuditEvents` | `Id` | Trilha de auditoria append-only com retenção de 12 meses (Actor, Action, Target, Result, DeviceId, CorrelationId, Reason) |
| `AnimeEpisodeWatches` | `(AccountId, MalId, EpisodeNumber)` | Episódios vistos **por usuário** (`AccountId` ≤ 128 = `sub` do token, `WatchedAtUtc`); check constraints `MalId > 0` e `EpisodeNumber >= 1`; sem FK para `Animes`. O crescimento por usuário é limitado a 10 000 linhas por anime |
| `AnimeFolderStatus` | `MalId` (informado) | **Resultado persistido** do status da pasta de cada anime (Fase 5): `MyAnimeId`, `RootKey` (≤ 100), `RelativePath`, `VideoCount`, `ExpectedEpisodes` (total usado na verificação), `State` (`none`/`partial`/`complete`), `Truncated` e `CheckedAtUtc` (`datetime2(7)`). Check constraints `MalId > 0`, `VideoCount >= 0` e `State IN ('none','partial','complete')`; índice por `MyAnimeId`. **Sem FK para `Animes`** (como `AnimeEpisodeWatches`). `RootKey`/`RelativePath` **nunca saem da API** (só servem para reverificar) |
| `AnimeFolderRefreshRuns` / `AnimeFolderRefreshItems` | `Id` (Guid) / `Id` (long, FK cascade para a execução) | Execução da atualização do status de pasta (início/fim, `Status`, `MyAnimeId?`, `RequestedBy` = `sub`, contadores `TotalAnimes`/`ProcessedAnimes`/`Updated`/`NotFound`/`Unavailable`/`Failed`, `Error` ≤ 2000) e o relatório por anime (`MalId`, `Outcome`, `State`, `VideoCount`, `Detail` ≤ 500, sem caminho físico); índices `(StartedAtUtc, Id)` e `(RunId, Id)` |

### MyAnime é uma coleção interna

- Cada anime tem `MyAnimeID`; **todos os animes com o mesmo `MyAnimeID` pertencem ao mesmo conjunto**. `MyAnimeID = 0` significa "sem coleção".
- `MyAnimes.AnimesMalId` guarda a lista inversa. Não há chave estrangeira formal: a consistência dos dois lados é mantida pelos controllers e pode divergir (ver [Pendências](#pendênciasdivergências)).
- `Animes.AnimesRelacionadosIds` guarda relações **locais** escolhidas no cadastro.
- **Relações locais usam `MyAnimeId`/DB_Local e são independentes das relações oficiais da ApiMyAnimeList** (`/ApiMyAnimeList/{id}/relations`). O detalhe local nunca deve consumir as relações externas.
- `Titulo` de MyAnime é único apenas por regra do controller (comparação sem diferença de caixa, após `Trim`); não há índice único no banco.

## Autorização

- Autenticação JWT Bearer (`MapInboundClaims=false`, `RequireHttpsMetadata=true`, tolerância de relógio de 1 min). `Authentication:Issuer` (HTTPS absoluto) e `Authentication:Audience` são validados na inicialização.
- **Política de fallback exige usuário autenticado**; rotas anônimas são marcadas explicitamente com `[AllowAnonymous]`.
- Política `permission:{x}` exige a claim `permission` **e** o mesmo valor em `scope`/`scp`.

| Política | Uso |
|---|---|
| `catalog.read` | `POST apiLocal/Anime/status` (status por usuário em lote); leitura de catálogo anônima não a exige |
| `personal.read` | `GET apiLocal/Anime/{malId}/watch` (progresso pessoal de episódios) |
| `personal.write` | `PUT`/`DELETE apiLocal/Anime/{malId}/watch...` (marcar, desmarcar, marcar todos, limpar) |
| `catalog.write` | criar/alterar animes e MyAnimes, migração, monitoramento e sincronização, **atualização do status de pasta** (`folder-status/*`; não é acessível ao usuário comum do site) |
| `catalog.delete` | exclusões |
| `health.read` | `Health` e Swagger (somente em Development) |
| `service.mal.read`, `identity.self.read` | definidas na configuração de autorização (uso por serviço/identidade; ver [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md)) |
| `service.privacy.myanimes` | `POST apiLocal/internal/privacy/export` e `.../erase` (LGPD por conta; **somente** conta de serviço da [ApiIdentity](21_ApiIdentity.md), com mTLS; ver "Endpoints internos de privacidade") |

CORS usa a política `AllowFrontend` com `Cors:AllowedOrigins` (em desenvolvimento, `http://localhost:5173` e `http://localhost:5178`).

## Endpoints

### `AnimeController` — `apiLocal/Anime`

| Método e rota | Acesso | Observação |
|---|---|---|
| `GET public` | anônimo | `skip`/`take` (take limitado a 500); **filtra conteúdo adulto** |
| `GET public/buscar?termo=&take=` | anônimo | busca filtrada (take ≤ 500) |
| `GET public/{id}` | anônimo | detalhe filtrado |
| `GET /` | anônimo | listagem **sem** filtro adulto e **sem** teto de `take` |
| `GET relacionados?ids=` | anônimo | até 200 IDs positivos distintos; preserva a ordem pedida |
| `GET buscar?termo=&take=100` | anônimo | busca completa (o serviço limita `take` a 1–100) |
| `GET {id}` | anônimo | detalhe por `MalId` |
| `POST` | `catalog.write` | corpo direto **ou** `?malId=` (importa da ApiMyAnimeList). 400/404/409/502; 409 se o `MalId` já existe ou já pertence a outro MyAnime |
| `POST conflito-titulo` | `catalog.write` | 200 com `ConflitoTituloAnimeDto` ou 204 (sem conflito) |
| `PUT {id}` / `PATCH {id}` | `catalog.write` | atualização total / JSON Patch (Newtonsoft) |
| `DELETE {id}` | `catalog.delete` | — |

Normalização no cadastro: `AnimesRelacionadosIds` mantém só IDs positivos e distintos; `Episodes ≤ 0` vira nulo; `Year` é extraído de `Aired` quando ausente.

### `AnimeStatusController` — `POST apiLocal/Anime/status` (`catalog.read`)

Status por anime **do usuário autenticado**, em lote, para os ícones dos cards do [DtudoSite](10_DtudoSite.md). Chega pelo [Gateway](20_DtudoGateway.md) (`POST /api/catalog/animes/status`) com o access token da sessão; o contrato do lote está descrito nesta seção e no [20_DtudoGateway.md](20_DtudoGateway.md).

- **Autorização:** policy `permission:catalog.read` (claim `permission` **e** `scope`). Anônimo → `401`; sem permissão/escopo → `403`. A identidade vem **somente** do claim `sub` (`MapInboundClaims=false`); token sem `sub` → `403`. Campos de identidade no corpo são ignorados.
- **Corpo:** `{ "malIds": [1, 5, 20] }` (`[RequestSizeLimit]` de 16 KiB). Corpo ausente, `malIds` ausente/nulo, tipos inválidos (inclusive inteiros fora do intervalo) → `400`. Mais de **200** itens no corpo → `400` (a contagem considera a lista bruta, antes de descartar repetidos/inválidos). Ids ≤ 0 e repetidos são descartados mantendo a ordem da primeira ocorrência; lista vazia → `{ "items": [] }`.
**`watch` é real (Fase 3)** e vem de uma **única consulta agregada** (`GROUP BY MalId` com join em `Animes`, sem N+1; ignora episódios acima do total atual) filtrada pelo `sub`; `none` (0 vistos), `partial` ou `complete` (vistos ≥ total conhecido; com total desconhecido nunca é `complete`). **`folder` é real (Fase 5)** e vem do último resultado **persistido** em `AnimeFolderStatus` (uma consulta `AsNoTracking` por lote, `MalId IN (...)`; o status é global, não por usuário): `{ state, videos, expected, checkedAtUtc }`. Anime sem linha, linha sem `CheckedAtUtc` ou estado desconhecido = nunca verificado → `none`/0/`checkedAtUtc: null`. O site **não** dispara cálculo nem lê disco; a resposta não traz caminho, chave de raiz nem `Truncated`. `like` = `null` até a próxima fase. Ids inexistentes no banco local são **omitidos** de `items`.
- **Total de episódios** (`AnimeStatusRules.ResolveTotalEpisodes`): `Episodes` se > 0; senão `Episodios` se > 1; senão `null`. `watch.total` usa o total **atual** do catálogo; `folder.expected` usa o total **da verificação** (o que originou o `state`) e, para anime nunca verificado, o total atual. Duas consultas `AsNoTracking` por lote (catálogo e `AnimeFolderStatus`), mais a agregada do `watch`; sem acesso a disco.
- Código: `Controllers\AnimeStatusController.cs`, `Services\AnimeStatusContract.cs` (estados e regras puras, incluindo `AnimeStatusRules.ToFolderStatus`); os DTOs do status em lote ficam em `LibDtudo.Shared\Dtos\AnimeFolderStatusContracts.cs` (compartilhados com o WinApp).

### Status de pasta dos animes (Fase 5)

`AnimeFolderStatusController` — `apiLocal/MyAnime`, policy `permission:catalog.write` (permissão **e** escopo; anônimo `401`, sem permissão/escopo — inclusive o usuário comum do site com só `catalog.read` — `403`, sem `sub` `403`). **Não há rotas no [Gateway](20_DtudoGateway.md)**: só o WinApp (Superadministrador) chama. O cálculo é **sob demanda e gravado**; nunca roda na exibição dos cards.

| Rota | Efeito |
|---|---|
| `POST {myAnimeId}/folder-status/refresh` | atualiza os animes de **uma coleção**; `404` se o MyAnime não existe |
| `POST folder-status/refresh` | atualiza **todas** as coleções |
| `GET folder-status/runs?take=` | execuções recentes (1–50), sem o relatório por anime |
| `GET folder-status/runs/{runId}?includeItems=&afterId=&take=` | progresso da execução e, opcionalmente, o relatório por anime paginado por cursor (`take` 1–500) |
| `POST folder-status/runs/{runId}/cancel` | pede o cancelamento (`202`; `404` se não há execução ativa com o id) |

Os `POST ... refresh` respondem **`202 Accepted`** com `AnimeFolderRefreshRunDto` (`id`, `status`, `total`, `processed`, `updated`, `notFound`, `unavailable`, `failed`, `items`...) e `Location` da execução; **`409`** se já há uma execução ativa (uma por vez no processo; `Location` aponta para ela); **`503`** se `ApiFileStorage:BaseUrl` não está configurada. O trabalho roda em segundo plano (`AnimeFolderRefreshRunner`, escopo próprio, cancelado ao encerrar a aplicação). Status da execução: `Running`, `Completed`, `CompletedWithErrors` (algum anime `Failed`/`Unavailable`), `Failed` (erro inesperado, mensagem genérica + log), `Cancelled` e `Interrupted` (o processo que a executava foi encerrado; marcado ao criar a próxima execução ou ao consultá-la — não há tarefa de inicialização tocando o banco, pois as migrações não são automáticas).

**Mapeamento anime → subpasta** (`AnimeFolderMapper`, por coleção, a partir do vínculo `MyAnimeMonitoringLocations` e da descoberta `anime-folders/discovery` da [ApiFileStorage](24_ApiFileStorage.md#descoberta-das-pastas-de-anime)):

1. **Capa:** subpasta cujas capas `<MalId>.jpg` contêm o `MalId` (várias candidatas: a que casa pelo nome; senão a primeira em ordem alfabética).
2. **Convenção de nome** `<ano> <título> - <tipo>` (reaproveita `FolderNameMatcher`, extraído do sincronizador de coleções: normalização sem acento/caixa; ignora o sufixo ` (n)` de duplicados, o ano inicial e o tipo final): primeiro **igualdade exata** com `Titulo`/`Title`/`TitleEnglish` (ano compatível) e depois **as quatro primeiras palavras com o mesmo ano**. Só vale se o anime casa com **uma única pasta** e a pasta com **um único anime** (empates não são resolvidos).
3. **Capa na própria pasta da coleção**, só se a coleção tem um único anime.

Sem correspondência → `NotFound` (grava `none`). Os animes de uma coleção são os de `MyAnimeID` igual ou listados em `AnimesMalId`; um anime em duas coleções é processado só na primeira (menor `Id`).

**Resultado por anime** (`AnimeFolderRefreshOutcomes`): `Updated` (pasta contada; grava `State`, `VideoCount`, `RootKey`, `RelativePath`, `ExpectedEpisodes`, `CheckedAtUtc`), `NotFound`/`NoLocation` (sem pasta mapeada ou coleção sem vínculo: grava `none`), `Unavailable` (disco/pasta indisponível ou listagem truncada) e `Failed` (falha de rede/autenticação/tempo limite). **`Unavailable` e `Failed` preservam o status anterior**: um disco offline não apaga um resultado bom. Falha de um anime (ou de uma coleção) não derruba o lote; cada resultado é gravado e contabilizado na hora (`SaveChanges` por anime), então o progresso é consultável e o cancelamento mantém o que já foi resolvido.

**Chamada serviço-a-serviço** (`AnimeFolderScanClient`, `IAnimeFolderScanClient`): HttpClient tipado com o mesmo padrão do cliente da ApiMyAnimeList (Client Credentials + **mTLS**, audience `urn:dtudo:api-file-storage`, escopo `filesystem.command`; `CorrelationIdDelegatingHandler`). Duas fases com `MaxConcurrency` (`FolderStatus:Refresh`, padrão 4, 1–16): uma descoberta por coleção e depois uma contagem (`anime-folders/scan`) por anime encontrado. Erros viram `AnimeFolderClientException` sem caminho físico. **A conta de serviço precisa existir na [ApiIdentity](21_ApiIdentity.md)** (`ServiceAuthentication:Clients`) e o certificado ser configurado pelo dono; enquanto `ServiceAuthentication:ApiFileStorage:Enabled=false` (padrão em Development), a atualização falha por anime (`Failed`) com o status anterior preservado.

- **Sem caminho físico:** nenhuma resposta (execução, relatório, lote) traz caminho, `RootKey`, `RelativePath` ou chave de raiz; `Detail` é texto fixo/genérico. Coberto por teste.
- Código: `Controllers\AnimeFolderStatusController.cs`, `Services\AnimeFolderRefreshService.cs` (plano, fases, gravação serializada), `AnimeFolderRefreshRunner.cs`, `AnimeFolderMapper.cs`, `FolderNameMatcher.cs`, `AnimeFolderScanClient.cs`, `AnimeFolderRefreshMappings.cs` (entidades → DTOs compartilhados de `LibDtudo.Shared\Dtos\AnimeFolderStatusContracts.cs`), `Data\AnimeFolderStatus.cs`, `Configuration\ApiFileStorageOptions.cs` e `AnimeFolderScanServiceCollectionExtensions.cs` (DI).

### `AnimeWatchController` — `apiLocal/Anime/{malId}/watch` (`personal.read` / `personal.write`)

Progresso de episódios vistos **do usuário autenticado** (tabela `AnimeEpisodeWatches`), consumido pelo controle de episódios do [DtudoSite](10_DtudoSite.md) via [Gateway](20_DtudoGateway.md).

| Método e rota | Policy | Efeito |
|---|---|---|
| `GET apiLocal/Anime/{malId}/watch` | `personal.read` | lista os episódios vistos |
| `PUT .../watch/episodes` | `personal.write` | corpo `{ "episodes": [1,2,3] }`; marca como vistos (**idempotente**: repetir não duplica nem altera `WatchedAtUtc`) |
| `DELETE .../watch/episodes` | `personal.write` | corpo `{ "episodes": [..] }`; desmarca. **Sem corpo, `{}` ou lista vazia não apaga nada** (devolve o estado atual) |
| `PUT .../watch/all` | `personal.write` | marca todos (1..total); exige total conhecido |
| `DELETE .../watch` | `personal.write` | limpa o progresso do anime para o usuário |

- **Rota:** `{malId:int:min(1)}`; fora disso a rota nem casa (404). Anime inexistente no banco local → `404`.
- **Autorização:** policies `permission:personal.read` / `permission:personal.write` (claim `permission` **e** `scope`), criadas em `Program.cs` com `ApiAuthorizationPolicies`. As permissões `personal.read`/`personal.write` **já existiam** no catálogo da [ApiIdentity](21_ApiIdentity.md) (perfis `SiteUser` e `Superadministrator`); nenhuma permissão nova foi criada. Anônimo → `401`; sem permissão/escopo → `403`. A identidade vem **somente** do claim `sub`; ausente ou com mais de 128 caracteres → `403`. Identidade no corpo/query é ignorada: um usuário nunca lê nem altera dados de outro.
- **Resposta (200):** `{ "malId", "total", "watched", "state", "episodes": [..] }` (`state`: `none`/`partial`/`complete`; `total` é `null` quando desconhecido; `episodes` ordenados e só até o total atual). `Cache-Control: no-store`.
- **Limites (`AnimeWatchRules`):** episódio ≥ 1; ≤ total quando conhecido; ≤ **10 000** quando o total é desconhecido; até **5 000** itens por corpo e corpo ≤ 64 KiB (`[RequestSizeLimit]`). Valor fora dos limites, corpo inválido ou tipos errados → `400`. A marcação é **atômica**: um número inválido rejeita a requisição inteira (nada é salvo). A desmarcação valida apenas `1..10 000`, para permitir remover episódios antigos quando o total diminuiu depois. `PUT watch/all` com total desconhecido → `409`; total acima de 10 000 → `400`.
- **Concorrência:** colisões de chave primária por requisições simultâneas (idempotência) são tratadas com até 3 tentativas (`ChangeTracker.Clear()` entre elas). Verificado durante o desenvolvimento contra um SQL Server LocalDB descartável (marcações/desmarcações paralelas, `GROUP BY` e check constraint); não há teste automatizado equivalente, pois o EF InMemory não reproduz a colisão.
- **Total de episódios:** mesma regra do status em lote (`AnimeStatusRules.ResolveTotalEpisodes`); `AnimeStatusRules.ResolveWatchState(watched, total)` calcula o estado.
- Código: `Controllers\AnimeWatchController.cs`, `Services\AnimeWatchService.cs` (escopo; usa `TimeProvider`), `Services\AnimeWatchContract.cs` (regras, DTOs e resultados), `Data\AnimeEpisodeWatch.cs`.
- **LGPD:** o progresso entra na exportação e na exclusão de dados da ApiIdentity pelos endpoints internos descritos abaixo.

### `InternalPrivacyController` — `POST apiLocal/internal/privacy/export` e `.../erase` (`service.privacy.myanimes`)

Endpoints **internos** de serviço, chamados apenas pela [ApiIdentity](21_ApiIdentity.md) (exportação e exclusão LGPD por conta). **Sem rota no Gateway e sem CORS.**

- **Acesso:** política `permission:service.privacy.myanimes` com falha fechada. Exige `InternalServiceAuthentication:Enabled=true`; permissão **e** scope no token; `sub == client_id` (token de usuário, mesmo com a claim, é `403`); `client_id` na allowlist; e **certificado mTLS da conexão** registrado para esse client (thumbprint atual ou anterior ainda dentro de `PreviousCertificateAcceptedUntilUtc`). Sem token: `401`; qualquer falha das demais condições: `403`. A permissão não pertence a nenhuma role de usuário.
- **Contrato:** `POST` com corpo `{ "accountId": "..." }` (conta nunca na URL nem em log). Validação: não vazio/branco, ≤ 128 caracteres, corpo ≤ 1 KiB; inválido → `400` ProblemDetails. Respostas com `Cache-Control: no-store`; outros verbos não são mapeados. Contratos em `LibDtudo.Shared\Dtos\MyAnimesPrivacyContracts.cs`.
- **`export`:** devolve `schemaVersion` `"1"`, `episodeProgress` (por `malId`: episódios e `watchedAtUtc`) e `folderRefreshRuns` (execução iniciada pela conta: id, estado, datas e contadores). **Nunca devolve caminho físico**, chave de raiz ou dados de outra conta.
- **`erase`:** apaga todo o progresso da conta (lotes de 1 000) e **anonimiza** `AnimeFolderRefreshRuns.RequestedBy` para `deleted-account` (a execução é dado global do catálogo). É **idempotente**: conta sem dados responde `200` com contadores zerados (nunca `404`). `AnimeFolderStatus` é dado global e não entra.
- **Auditoria:** `SecurityAuditEvents` (append-only, 12 meses) com a ação, o resultado e apenas um hash curto da conta (`account-hash:<sha256[..16]>`), `deviceId` `internal-service`; nunca o `AccountId`.
- **Configuração:** `InternalServiceAuthentication` (`Enabled`, `Clients[]` com `ClientId`, `CertificateThumbprints`, `PreviousCertificateAcceptedUntilUtc`); em Development vem `Enabled=false` e `Clients: []`. O Kestrel pede certificado de cliente opcional e o valida contra essa allowlist.
- Código: `Controllers\InternalPrivacyController.cs`, `Services\AccountPrivacyService.cs`, `Infrastructure\InternalServiceAuthorization.cs`, `Configuration\InternalServiceAuthenticationOptions.cs`.

### `MyAnimeController` — `apiLocal/MyAnime`

| Método e rota | Acesso | Observação |
|---|---|---|
| `GET public`, `GET public/{id}` | anônimo | `take` padrão 5 (máx. 500); remove dos conjuntos os animes adultos |
| `GET /`, `GET {id}` | anônimo | sem filtro adulto; `take` padrão 5 |
| `POST` | `catalog.write` | 409 se o título já existir (sem diferenciar caixa) |
| `PUT {id}`, `PATCH {id}` | `catalog.write` | título obrigatório; IDs distintos e positivos |
| `DELETE {id}` | `catalog.delete` | zera `MyAnimeID` dos animes relacionados |

### `CatalogMigrationController` — `apiLocal/catalog-migration` (`catalog.write`)

Fluxos idempotentes de migração/reparo usados pelo WinAppDtudo (DTOs em `CatalogMigrationDtos`, na [LibDtudo.Shared](30_LibDtudo.Shared.md)):

- `PUT my-animes/by-title` — cria/mescla MyAnime por título; 201 na criação, 409 se todos os `MalId` já pertencem a outras coleções.
- `PUT animes/{malId}/my-anime` — associa um anime; **nunca move** anime de outra coleção (retorna `IgnoradaPorOutraColecao`).
- `POST animes/orphan-associations/repair` — simulação por padrão; com `Aplicar=true` zera `MyAnimeID` órfãos.

### Monitoramento e sincronização (`catalog.write`)

- `apiLocal/MyAnime/{myAnimeId}/monitoring-location` — `GET`/`PUT`. Valida `RootKey` contra `MonitoringPathPolicy.CollectionRoots()` e `RelativePath` com `IsValidRelativePath`. **Altera apenas o vínculo no banco, nunca arquivos.**
- `apiLocal/MyAnime/monitoring-synchronizations` — `POST` (corpo `CollectionSyncRequest(Discovery)`, resultado da descoberta feita pelo WinApp) e `GET {runId}`.
  - Uma execução por vez (semáforo estático; concorrência → 409), transação serializável.
  - Status: `Running`, `Blocked`, `Completed`, `Partial`, `Failed`, `Cancelled`.
  - Eventos típicos: `ROOT_UNAVAILABLE`, `UNAUTHORIZED_ROOT`, `DUPLICATE_FOLDER`, `INVALID_RELATIVE_PATH`, `MAPPING_ADDED/UPDATED/UNCHANGED`, `AMBIGUOUS_FOLDER`, `MISSING_CATALOG`, `MAPPING_CONFLICT`, `EXISTING_MAPPING_NOT_DISCOVERED`, `SYNC_FAILED`.
  - Respostas: 400 (argumento inválido), 409 (operação inválida/concorrência), 503 (falha de banco).
[24_ApiFileStorage.md](24_ApiFileStorage.md)

### `HealthController` — `apiLocal/Health` (`health.read`)

200 `{status, service, database}` ou 503 `unavailable`.

## Busca local

Implementada em `AnimeBuscaLocalService` com `AnimeSearchTextNormalizer` (compartilhado):

1. **Normalização de caracteres especiais:** decodifica HTML, minúsculas, remove diacríticos, pontuação vira espaço e compacta o texto em tokens.
2. **Casamento:** substring, forma compacta ou tokens aproximados (distância de Levenshtein ≤ 1; ≤ 2 para tokens com 8+ caracteres; aproximação só para tokens com 4+ caracteres).
3. **Prioridade por campo (peso):** título principal `Titulo` (100) → `Title` (95) → inglês `TitleEnglish` e japonês `TitleJapanese` (90) → sinônimos `TitleSynonyms` (80) → `SubTitulos` (70). Bônus: +50 igualdade exata, +25 prefixo, +20 igualdade compacta. Ordena por pontuação decrescente e depois `Titulo`.
4. **Coleções como complemento:** somente se houver menos que `take` resultados, busca MyAnimes por título e acrescenta os animes de `AnimesMalId`. Resultado distinto por `MalId`; `take` 1–100.
5. `AnimeTitleConflictService` usa `AnimeTitleEquivalence` (igualdade exata após normalização) para o endpoint `conflito-titulo`.

> A busca carrega a tabela em memória (`ToListAsync`) e filtra no processo. Adequado ao volume atual; reavaliar se o catálogo crescer.

### Conteúdo adulto

`PublicCatalogPolicy.IsAdult` considera `Rating`, `Genres`, `ExplicitGenres`, `Themes` e `Demographics` (contém `hentai`, começa com `rx` ou `r+`, ou contém "adult"/"pornographic"). Os endpoints `/public` aplicam o filtro.

## Paginação

`skip`/`take` nas listagens de anime e `take` nas de MyAnime. Tetos: 500 nas rotas `/public`; 100 na busca completa (clamp do serviço); `relacionados` aceita até 200 IDs. Rotas não públicas de listagem (`GET /`) **não** impõem teto (ver Pendências).

## Imagens e capas

Não há arquivo de imagem local no banco: `ImagensUrlMal` guarda URLs do MyAnimeList (JPG pequeno/grande, distintas). Capas locais e arquivos pertencem ao [24_ApiFileStorage.md](24_ApiFileStorage.md); o consumo visual está em [10_DtudoSite.md](10_DtudoSite.md) e [11_WinAppDtudo.md](11_WinAppDtudo.md).

## Importação da ApiMyAnimeList

`POST apiLocal/Anime?malId=` usa `MyAnimeListImportClient` (HttpClient tipado, timeout de 30 s) para `GET ApiMyAnimeList/{malId}`.

- Handlers: `CorrelationIdDelegatingHandler` e `ServiceAccessTokenHandler` (Client Credentials com mTLS; token com audience `urn:dtudo:api-my-animelist`).
- Mapeamento para `AnimeImportData`: título = `Title` ?? inglês ?? japonês; imagens JPG distintas; subtítulos = inglês + japonês + sinônimos (exceto o título principal); `Episodios` padrão 1.
- Falhas upstream retornam 404/502 ao chamador.

## Configuração

| Chave | Descrição |
|---|---|
| `ConnectionStrings:LocalDbConnection` | **Segredo** (User Secrets, id `dtudo2026-apimyanimes`); validada na inicialização. Nunca versionar |
| `Authentication:Issuer` / `Authentication:Audience` | Issuer HTTPS absoluto e audience da API |
| `ApiMyAnimeList:BaseUrl` | HTTPS; destino da importação |
| `ServiceAuthentication:ApiMyAnimeList` | `Enabled` (falso em Development), `TokenEndpoint`, `ClientId`, `Audience`, `Scopes`, `CertificateThumbprints` (1–2, 40 hex), `CertificateStore` |
| `ApiFileStorage:BaseUrl` / `RequestTimeoutSeconds` | HTTPS da [ApiFileStorage](24_ApiFileStorage.md) (Development: `https://localhost:7244/`); vazia = atualização de pasta responde `503`. Timeout de cada chamada (1–300 s, padrão 30) |
| `ServiceAuthentication:ApiFileStorage` | mesma forma da chave acima; `Audience` `urn:dtudo:api-file-storage`, `Scopes` `["filesystem.command"]`; `Enabled=false` em Development; `ClientId` e certificado a configurar pelo dono (ver [21_ApiIdentity.md](21_ApiIdentity.md)) |
| `FolderStatus:Refresh:MaxConcurrency` | Chamadas simultâneas à ApiFileStorage na atualização (1–16, padrão 4) |
| `Cors:AllowedOrigins` | Origens do front |
| `InternalServiceAuthentication` | Allowlist de contas de serviço dos endpoints internos de privacidade: `Enabled` (falso em Development), `Clients[]` (`ClientId` `api-identity`, `CertificateThumbprints`, `PreviousCertificateAcceptedUntilUtc`); valores reais só em segredo/ambiente. Habilitação em [02_Solucao_Operacao.md](02_Solucao_Operacao.md) |
| `Seq:Url` | Opcional (`http://localhost:5341` em desenvolvimento) |

Observabilidade: Serilog com `SensitiveDataRedactionEnricher` e `RequestCorrelationMiddleware` (`X-Correlation-ID`, compatível com W3C Activity). Swagger apenas em Development, restrito a `health.read`; comentários XML são publicados.

## Migrações

`Migrate()` **não** roda na inicialização: aplicar manualmente (`dotnet ef database update`). Ordem:

1. `20260620051838_RecebendoDadosDetalhadoApiJikan` — cria `Animes`/`MyAnimes` (nome histórico)
2. `20260621054103_CampoMyAnimeIdAdicionado`
3. `20260806183450_SecurityAuditTrail`
4. `20260816012701_RemoveMyAnimeTitleLengthLimit` (`Titulo` → `nvarchar(max)`)
5. `20260816014801_AnimesRelacionadosIds`
6. `20260819220229_DesativarCacheIdentityMyAnimes` (`IDENTITY_CACHE=OFF`)
7. `20260918002634_AddMyAnimeMonitoringLocation`
8. `20260925025748_AddMyAnimeMonitoringSynchronization`
9. `20261030005434_AddAnimeEpisodeWatch` (tabela `AnimeEpisodeWatches`; **aplicada em 30/10/2026**)
10. `20261030033256_AddAnimeFolderStatus` (tabelas `AnimeFolderStatus`, `AnimeFolderRefreshRuns` e `AnimeFolderRefreshItems`; **gerada e aplicada em 30/10/2026**)

Ambas foram aplicadas no ambiente local de desenvolvimento. Em outro banco, aplicar, a partir da raiz do repositório: `dotnet ef database update --project ApiMyAnimes\ApiMyAnimes.csproj`

## Testes

`InternalPrivacyEndpointTests` (22 casos, Fase 8): exportação/exclusão por conta, isolamento entre contas, idempotência, anonimização de `RequestedBy`, ausência de caminhos nas respostas, `401` sem token, `403` para token de usuário, `sub ≠ client_id`, sem scope, outra permissão, sem certificado/certificado errado, client fora da allowlist e `Enabled=false`, `400` para conta vazia/longa/corpo vazio, só `POST` e auditoria sem o `AccountId` (usa certificado de cliente injetado por header apenas no host de teste).

`ApiMyAnimesTestHost` (helper compartilhado; `CreateAppWith` injeta o `FakeAnimeFolderScanClient` e carga extra), `AnimeFolderMapperTests` (mapeamento por capa, por convenção exata/aproximada, duplicado, ano divergente, ambiguidade, capa na pasta da coleção e não encontrado), `AnimeFolderRefreshServiceTests` (atualização parcial com falha de um anime, disco offline/pasta ausente/listagem truncada preservando o status, `NoLocation`, falha de descoberta isolada por coleção, concorrência máxima, cancelamento, execução órfã/interrompida e paginação do relatório), `AnimeFolderStatusEndpointTests` (autorização 401/403 em todas as rotas, `404`/`503`/`409`, `202` com execução consultável, cancelamento, **lote lendo `folder` da tabela** com não verificado = `none` e `checkedAtUtc` nulo, e **ausência de caminho físico/chave de raiz** nas respostas), `ApiAuthorizationTests`,

```powershell
dotnet test .\tests\ApiMyAnimes.Tests\ApiMyAnimes.Tests.csproj
```

## Pendências/Divergências

- **Documentação de código × comportamento:** o XML doc de `AnimeController.BuscarAnimes` diz que "prioriza coleções MyAnime", mas o código busca primeiro os títulos dos animes e usa coleções só como complemento.
- **Rotas anônimas sem filtro/teto:** `GET apiLocal/Anime` e `GET apiLocal/MyAnime` são `[AllowAnonymous]`, sem filtro adulto e sem teto de `take`; apenas as rotas `/public` filtram.
Não há rotas de homologação para `/public` no gateway.
- **Auditoria sem uso:** `ISecurityAuditWriter`/`SecurityAuditWriter` estão registrados, mas nenhum controller grava eventos (apenas testes/infraestrutura).
- **Integridade lógica:** `MyAnimeID` e `AnimesMalId` não têm FK; unicidade de `MyAnimes.Titulo` só no controller.
- **Nome histórico:** a migração "…ApiJikan" não reflete a fonte atual (ApiMyAnimeList); o fluxo Jikan não existe mais.
- **Higiene:** `MyAnimeController` usa `Console.WriteLine` em algumas ações (preferir `ILogger`).
- **Escala:** a busca local carrega todo o catálogo em memória.
- **Fora do escopo desta API:** `PasswordHasher`/`ChecaForcaSenha`/`ValidaCPF` na LibDtudo.Shared não são usados aqui (ver [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md)).
- **Status em lote (Fase 2):** `like` segue neutro até a próxima fase; `folder` passou a ser real na Fase 5; ids inexistentes são omitidos (não há 404 por item); lista bruta acima de 200 é rejeitada com `400` (opção restritiva, em vez de truncar). Usa a policy `catalog.read` existente (não foi criada permissão nova).
(uso administrativo direto, pelo WinAppDtudo via `Frm_StatusPastas`, Fase 6);
- **Progresso de episódios (Fase 3):** (1) em outro banco, a migração `AddAnimeEpisodeWatch` precisa ser aplicada manualmente (no local já foi); (2) os tokens do gateway só carregam `personal.read`/`personal.write` depois que a ApiIdentity for reiniciada (o seeder atualiza o cliente) e o usuário **entrar novamente**, senão as rotas respondem `403`; (3) sem FK para `Animes`; exportação e exclusão LGPD pelos endpoints internos (Fase 8), **ainda não validadas ao vivo** (exigem mTLS, `InternalServiceAuthentication` e o worker da ApiIdentity habilitado; ver [02_Solucao_Operacao.md](02_Solucao_Operacao.md)); (4) o limite por requisição (5 000 itens) e o teto por anime (10 000) são decisões de proteção do projeto, não do contrato original.
