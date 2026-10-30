# ApiFileStorage

A `ApiFileStorage` é a API que concentra o acesso ao sistema de arquivos da solução Dtudo2026. Ela tem dois papéis: (1) um **motor de ciclo de vida de arquivos** (resolução segura, quarentena, varredura e promoção) que hoje não tem endpoint de escrita ativo, e (2) o **monitoramento de coleções locais** (inventário somente leitura, sessões, eventos e histórico), consumido pelo [WinAppDtudo](11_WinAppDtudo.md). O princípio central é nunca confiar em caminhos informados por clientes: o cliente usa identificadores lógicos e chaves de raiz autorizadas.

Segurança geral: [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md) · operação: [02_Solucao_Operacao.md](02_Solucao_Operacao.md) · identidade: [21_ApiIdentity.md](21_ApiIdentity.md) · contratos: [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md).

## Estrutura do projeto

| Pasta | Conteúdo |
| --- | --- |
| `Controllers\` | `FileStorageController`, `MonitoringController`, `FileStorageHealthController`, `FileStorageStartupController` |
| `Services\` | `FileStorageLifecycleService`, `FileStorageScanner` (Defender + AMSI), `SecureStoragePathResolver`, `StorageObjectId`, reconciliação em *hosted service* |
| `Monitoring\` | Monitoramento somente leitura: `MonitoringReader`, `MonitoringSessions`, `MonitoringRoots`, `MonitoringDirectoryLease`, `CollectionSyncDiscoveryService`, `AnimeFolderScanService` (+ `AnimeFolderScanOptions`); cobre reader, sessões, descoberta, varredura de pasta de anime e troca de chave de raiz |
| `Monitoring\Data\` | `MonitoringDbContext`, `MonitoringInventoryStore`, `MonitoringPersistence`, migrações |
| `Monitoring\Relocation\` | `MonitoringRootKeyMigration`/`MonitoringRootKeyPlan` (regrava chaves de raiz legadas) |
| `Configuration\`, `Contracts\`, `Infrastructure\` | opções, DTOs, middleware de correlação |

Pipeline

## Autorização

- Validação JWT do [ApiIdentity](21_ApiIdentity.md): `Authentication:Issuer` em HTTPS, `Audience = urn:dtudo:api-file-storage`, `MapInboundClaims = false`, tolerância de relógio de 1 min.
- Policies: `permission:filesystem.command` (permission **e** scope `filesystem.command`) para resolução, reconciliação e todo o monitoramento; `permission:health.read` para o health.
- O único endpoint anônimo é `GET api/file-storage/startup` (contrato de inicialização, sem dados sensíveis).
- O WinApp solicita o recurso `urn:dtudo:api-file-storage` e o escopo `filesystem.command` no login.
- Sessões de monitoramento pertencem ao usuário (claim `sub`): outro usuário não lê nem encerra a sessão.

## Endpoints

| Método e rota | Policy | Descrição |
| --- | --- | --- |
| `GET api/file-storage/startup` | anônimo | `{ Service: "ApiFileStorage", ContractVersion: 2 }`; usado pelo `ApiFileStorageStartupService` do WinApp |
| `GET api/file-storage/health` | `health.read` | saúde (raízes, scanner, monitoramento) |
| `POST api/file-storage/resolve` | `filesystem.command` | resolve `ObjectId` lógico em tipo/tamanho/data, sem devolver caminho físico; 400/403/404 em ProblemDetails (`storage-path-rejected`) |
| `POST api/file-storage/reconcile` | `filesystem.command` | reconcilia o diário de operações pendentes |
| `GET api/file-storage/monitoring/roots` | `filesystem.command` | raízes de coleção autorizadas (`Key`, `Path`) |
| `POST .../monitoring/synchronizations/discovery` | `filesystem.command` | descobre pastas de 1º nível por raiz e IDs numéricos de capas |
| `POST .../monitoring/anime-folders/scan` | `filesystem.command` | conta, sob demanda e sem sessão, os vídeos de episódio da pasta de um anime (ver [Contagem de vídeos da pasta do anime](#contagem-de-vídeos-da-pasta-do-anime)) |
| `POST .../monitoring/anime-folders/discovery` | `filesystem.command` | lista as subpastas diretas de uma pasta de coleção com os `MalId` das capas de cada uma (ver [Descoberta das pastas de anime](#descoberta-das-pastas-de-anime)) |
| `POST .../monitoring/sessions` | `filesystem.command` | abre sessão
| `POST .../monitoring/sessions/{id}/heartbeat` | `filesystem.command` | mantém a sessão viva |
| `POST .../monitoring/sessions/{id}/refresh` | `filesystem.command` | solicita novo inventário |
| `DELETE .../monitoring/sessions/{id}` | `filesystem.command` | encerra a sessão |
| `GET .../monitoring/sessions/{id}/events` | `filesystem.command` | eventos (`afterId`, `take` 1–1000, `latest`) |
| `GET .../monitoring/sessions/{id}/entries/{locationId}` | `filesystem.command` | entradas do inventário (`snapshotId`, `afterId`, `take` 1–1000) |

Códigos: 503 (monitoramento desativado ou banco indisponível), 409 (escopo sobreposto ou limite de sessões), 403, 404 e 400 (caminho relativo inválido). Os endpoints `import`, `delete*` e `export/*` foram removidos.

## Armazenamento, quarentena e ciclo de vida

O motor de ciclo de vida (`FileStorageLifecycleService`) foi desenhado para escrita segura em uma raiz de armazenamento própria, **diferente** das coleções monitoradas:

| Tópico | Comportamento |
| --- | --- |
| Raiz | `FileStorage:Roots` (vazio por padrão, fail-closed: a API não inicia sem raiz, allowlist e scanner). O caminho real vem de User Secrets/ambiente, nunca do repositório. Coleções de mídia **não** são adicionadas a `FileStorage:Roots` |
| Identificador | `StorageObjectId` v1 (lógico); `SecureStoragePathResolver` valida com handles Windows, bloqueia reparse points, hard links, traversal e nomes reservados |
| Quarentena | `.dtudo-quarantine/operations/<hash>/payload.bin` até a varredura terminar |
| Varredura | `CompositeFileScanner`: Microsoft Defender (`MpCmdRun.exe`) + AMSI; `RequireDefender`/`RequireAmsi` verdadeiros; timeout 60 s |
| Validação | allowlist `.jpg/.jpeg/.png/.gif` com *magic bytes*/MIME; tamanho máximo 52.428.800 bytes; nome até 255; espaço livre mínimo 104.857.600 bytes |
| Promoção | `File.Move` atômico, sem sobrescrever |
| Lixeira | `.dtudo-trash/...` com expurgo em 7 dias (`PurgeAtUtc`) |
| Diário | JSON com estados: staging, awaiting-scan, scanning, ready-to-promote, promoting, awaiting-promotion, completed, rejected |
| Idempotência | chave limitada a 128 caracteres, armazenada como SHA-256 |
| Reconciliação | no startup (`FileStorageReconciliationHostedService`) e por `POST reconcile` |

Estado atual: o motor está implementado e testado (`FileStorageLifecycleTests`, `FileStorageScannerTests`, `StoragePathResolverTests`, `StorageAclTests`), mas nenhum endpoint de escrita o expõe; não há consumidor. Pastas `.dtudo-*` são ignoradas pelo monitoramento.

## Monitoramento de coleções locais

Monitoramento é **somente leitura**: a API nunca escreve, move ou apaga nas coleções; não lê conteúdo nem calcula hash.

### Raízes autorizadas

A política (`MonitoringPathPolicy.CollectionRoots`, em `LibDtudo.Shared\Dtos\FileMonitoring`) define 54 chaves, resolvidas no servidor:

| Grupo | Chaves |
| --- | --- |
| Unidade E | `E_.Dots` e `E_A` … `E_L` (13) |
| Unidade H | `H_M` … `H_Z` (14) |
| Animex (subpastas da raiz Animex) | `X_.Dots` e `X_A` … `X_Z` (27) |

- Exclusões: `.ImportanteX`, `$RECYCLE.BIN`, `System Volume Information` e nomes iniciados por `.dtudo-`.
- Caminho relativo: até 32.000 caracteres, segmentos de até 255, sem `/`, `:`, controle, `.`/`..`, nomes reservados, terminação em ponto ou espaço.
- `NormalizeRootKey` converte chaves legadas (por exemplo `H_#Dots`, `X_#Dots`, letras que mudaram de unidade) para as atuais; `MonitoringRootKeyMigration` regrava `RootKey` legado nos bancos, em transação e de forma idempotente (única exceção ao *append-only*).

### Bancos de dados

| Item | Valor |
| --- | --- |
| Banco | `DtudoFileMonitoring` (SQL Server), dedicado ao monitoramento; esquema `monitoring` |
| Connection string | `ConnectionStrings:FileMonitoring` via User Secrets/ambiente (nunca versionada); a validação rejeita outro banco e `AttachDBFilename` |
| Tabelas | `Locations`, `Snapshots`, `Entries`, `Observations` |
| Integridade | *append-only* (`EnsureAppendOnly`), FKs `Restrict`, checks de contagem/tempo/`Completion`/`Source`; caminhos `nvarchar(max)` |
| Migrações | `20260918001315_InitialFileMonitoring`, `20260918001316_OptimizeObservationCursor` (índice `IX_Observations_LocationId_Id`) |
| Aplicação | **sem migração automática**; aplicar com `dotnet ef database update` (fábrica design-time aceita `--offline` para gerar scripts) |
| Ativação | `Monitoring:Enabled` (padrão `true`); desativado → 503 |

Relação com o [ApiMyAnimes](22_ApiMyAnimes.md): o banco do catálogo guarda `MyAnimeMonitoringLocations` (`MyAnimeId`, `RootKey`, `RelativePath`; **uma** localização por MyAnime), além de `MyAnimeMonitoringSyncRuns`/`MyAnimeMonitoringSyncEvents` (execuções de sincronização). Os dois bancos não têm chave estrangeira entre si: o vínculo é lógico, por `(RootKey, RelativePath)`. A localização aponta para a **pasta da coleção** (1º nível sob a letra), não para a pasta de cada anime. Endpoints no ApiMyAnimes: `GET/PUT apiLocal/MyAnime/{id}/monitoring-location` e `POST apiLocal/MyAnime/monitoring-synchronizations` (+ `GET .../{runId}`), ambos com policy `catalog.write`.

### Sessões

- Máximo de 4 sessões simultâneas; escopos sobrepostos são recusados (409).
- Sessão sem heartbeat por 45 s expira (verificação a cada 5 s). A coleta só existe com a tela do WinApp aberta; banco indisponível encerra a sessão, sem *spool*.
- Ao abrir, um inventário inicial é solicitado automaticamente; `refresh` pede outro.
- `FileSystemWatcher` com fila limitada de 4096 eventos; estouro gera `EVENT_GAP`. Disponibilidade da raiz é conferida a cada 15 s (`UNAVAILABLE`, `AVAILABLE`, `WATCH_FAILED`).

### Inventário, snapshots e observações

- `MonitoringReader` enumera iterativamente cada diretório (`MonitoringDirectoryLease`), bloqueia reparse points e itens de sistema (inventário `Partial`) e marca falha na raiz como `Unavailable`.
- Cada leitura grava um **snapshot** (`Completion`: Complete, Partial ou Unavailable) com totais de arquivos, diretórios e bytes e, em `Entries`, cada item (`RelativePath`, `IsDirectory`, `Bytes`, `ModifiedAtUtc`).
- A comparação do novo inventário com o último `Complete` gera eventos `ADDED`, `CHANGED` e `REMOVED` (fonte `InventoryComparison`); notificações do watcher geram `CREATED`, `CHANGED`, `DELETED`, `RENAMED` (fonte `LiveNotification`); ciclo da sessão gera `SESSION_STARTED`/`SESSION_STOPPED`.
- Observações de qualidade: `ENTRY_SKIPPED`, `ZERO_BYTES`, `LONG_PATH` (≥ 260), `EMPTY_DIRECTORY`, `READ_FAILED`.
- `MonitoringInventoryStore` valida totais e caminhos ao gravar e pagina entradas por cursor de `Id` (1–1000 por página).

### Como consultar

1. `GET monitoring/roots` para as chaves válidas.
2. `POST monitoring/sessions` com `{ RootKey, RelativePath }`; a resposta traz `MonitoringSessionDto` (estado, erro, `InventoryOutdated` e as `MonitoringLocationDto` com `SnapshotId`, `Completion`, `Files`, `Directories`, `Bytes`, `InventoriedAtUtc`).
3. Manter `POST .../heartbeat` a cada poucos segundos (o WinApp usa 3 s).
4. Paginar `GET .../entries/{locationId}?snapshotId=...&afterId=...&take=1000` e `GET .../events?afterId=...`.
5. Encerrar com `DELETE`.

O WinApp (`MonitoringApiClient`) faz exatamente esse fluxo; a classificação visual de imagem/vídeo é feita no cliente.

### Sincronização de coleções

`POST monitoring/synchronizations/discovery` lista pastas de 1º nível por raiz e IDs numéricos de capas (`.jpg/.jpeg/.png/.webp/.gif/.bmp`, recursivo). O ApiMyAnimes cruza esses dados com o catálogo (maioria de IDs presentes; desempate pelas quatro primeiras palavras normalizadas) e grava o vínculo MyAnime ↔ pasta com estados Completed, Partial, Blocked, Failed ou Cancelled.

### Contagem de vídeos da pasta do anime

`POST api/file-storage/monitoring/anime-folders/scan` responde, sob demanda, se a pasta de **um** anime existe e quantos vídeos de episódio ela contém. É leitura iterativa direta do disco (`AnimeFolderScanService`): **não** abre sessão, **não** usa o banco de monitoramento, **não** grava snapshot/histórico e funciona mesmo com `Monitoring:Enabled=false`. Exige a mesma policy do restante do monitoramento (`filesystem.command`: permission **e** scope).

Pedido (JSON, camelCase):

```json
{ "rootKey": "E_A", "relativePath": "2020 Nome do Anime - TV", "expectedEpisodes": 12 }
```

Resposta (nunca contém caminho físico):

```json
{ "exists": true, "videoCount": 11, "directoryCount": 2, "state": "partial", "truncated": false }
```

| Campo | Regra |
| --- | --- |
| `rootKey` | uma das chaves de `GET monitoring/roots`; chaves legadas são normalizadas por `MonitoringPathPolicy.NormalizeRootKey` |
| `relativePath` | **obrigatório** (não vazio), validado por `MonitoringPathPolicy.IsValidRelativePath` (sem `..`, `/`, `:`, caminho absoluto/UNC, nomes reservados, pastas excluídas) e resolvido por `MonitoringRoots.Resolve` (sem escapar da raiz) |
| `expectedEpisodes` | `int?`; nulo ou `0` = desconhecido; negativo ou acima de 100.000 = 400 |
| `exists` | `false` para pasta inexistente, raiz indisponível, caminho que não é pasta, pasta ou ancestral que seja link/junção (reparse point) ou pasta ilegível; nesses casos `state = none` |
| `videoCount` | vídeos de episódio, recursivo (regras abaixo) |
| `directoryCount` | subpastas percorridas (extras, links, itens de sistema e pastas excluídas não contam) |
| `truncated` | `true` quando um limite foi atingido ou uma subpasta não pôde ser lida: as contagens podem estar abaixo do real |

**O que conta como vídeo de episódio** (política única `EpisodeVideoPolicy`, em `LibDtudo.Shared`, também usada pelo WinApp — ver [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md)):

- extensões (sem diferenciar maiúsculas): `.3g2 .3gp .asf .avi .divx .flv .m2ts .m2v .m4v .mkv .mov .mp4 .mpe .mpeg .mpg .mts .ogv .rm .rmvb .ts .vob .webm .wmv`;
- arquivos de 0 byte são ignorados;
- extras são ignorados: pastas (não são percorridas) e nomes de arquivo que contenham, como **palavra inteira**, `NCOP`, `NCED`, `Creditless`, `PV`, `CM`, `Trailer`, `Extras` ou `Featurettes` (comparação sem maiúsculas; número final como `NCED2`/`PV1` e plural simples como `Trailers` são aceitos; `CMYK` ou `PVR` não são extras). Efeito colateral aceito: um título que contenha uma dessas palavras inteiras (por exemplo "Trailer Park") seria ignorado;
- contagem por arquivo, sem detectar arquivos que agrupam vários episódios, versões duplicadas ou temporadas; legendas e imagens não contam; links/junções e itens de sistema são pulados (não seguidos) sem marcar `truncated`.

**Estado** (`AnimeFolderState.Evaluate`): `none` se a pasta não existe **ou** não tem nenhum vídeo de episódio (0 vídeos: só estrutura, extras, legendas ou arquivos de 0 byte); `complete` somente se a leitura foi integral (`truncated = false`), `expectedEpisodes` é conhecido (> 0) e `videoCount ≥ expectedEpisodes`; `partial` quando há pelo menos 1 vídeo, mas `videoCount < expectedEpisodes`, `expectedEpisodes` é desconhecido ou a leitura foi truncada. `truncated` nunca resulta em `complete`; com 0 vídeos encontrados continua `none` (confira o campo `truncated`).

**Limites** (`Monitoring:AnimeFolderScan`, validados na inicialização):

| Opção | Padrão | Efeito |
| --- | --- | --- |
| `MaxDepth` | 8 | níveis de subpasta abaixo da pasta do anime; mais fundo não é percorrido (`truncated`) |
| `MaxFiles` | 20000 | arquivos examinados; ao exceder, interrompe (`truncated`) |
| `MaxDirectories` | 2000 | subpastas percorridas (`truncated` ao exceder) |
| `Timeout` | `00:00:10` | tempo máximo de leitura; devolve o parcial com `truncated` (não é erro) |
| `MaxConcurrentScans` | 2 | varreduras simultâneas; as demais aguardam vaga |

Respostas: 200, 400 (pedido ou caminho inválido, ProblemDetails sem caminho físico), 401/403. O consumidor é a [ApiMyAnimes](22_ApiMyAnimes.md) (Fase 5: `AnimeFolderRefreshService`, chamada serviço-a-serviço); esta API não conhece anime nem catálogo.

### Descoberta das pastas de anime

`POST api/file-storage/monitoring/anime-folders/discovery` (mesma policy, mesma leitura direta sem sessão/banco) serve ao mapeamento anime → subpasta da ApiMyAnimes. Pedido `{ "rootKey": "E_A", "relativePath": "Colecoes\\Alpha" }` (pasta da coleção, validada como no `scan`). Resposta, sem caminho físico:

```json
{ "exists": true, "folders": [ { "relativePath": "Colecoes\\Alpha\\2020 Alpha - TV", "coverIds": [1] } ], "truncated": false }
```

- `folders`: cada **subpasta direta** da coleção (links/junções e itens de sistema são pulados) com os `MalId` das imagens numéricas `<MalId>.<ext>` do **1º nível** dela (extensões de imagem da sincronização de coleções), em ordem crescente; se a própria pasta da coleção tiver capas diretas, ela entra como mais um candidato (coleção de um único anime).
- `exists = false`: pasta inexistente, raiz indisponível, não é pasta, link/junção ou ilegível (como no `scan`).
- `truncated`: limites de `Monitoring:AnimeFolderScan` (`MaxDirectories`, `MaxFiles`, `Timeout`) atingidos ou subpasta ilegível — a lista pode estar incompleta; o consumidor não conclui "sem pasta" nesse caso.
- Compartilha o limite de varreduras simultâneas (`MaxConcurrentScans`) com o `scan`.

## Configuração

`appsettings.json` (sem segredos): `FileStorage` (`Roots` vazio, `Limits`, `AllowedFileTypes`, `Scanner`), `Monitoring:Enabled`, `Monitoring:AnimeFolderScan` (limites da contagem de vídeos),

## Limitações atuais

- Sem retenção/limpeza automática do histórico (`Snapshots`/`Entries`/`Observations` só crescem).
- Máximo de 4 sessões e sem sessões sobrepostas; sem coleta passiva (exige tela aberta).
- Inventário completo e recursivo por sessão: coleções grandes demandam tempo e espaço; leitura parcial vira `Partial`.
- Sem exportação de relatórios no servidor, sem raízes/limiares configuráveis, sem auditoria de autoria.
- Banco de monitoramento indisponível encerra a sessão e não há fila local.
- O motor de ciclo de vida não tem endpoint de escrita e nenhum consumidor.

## Pendências/Divergências

- **`GET monitoring/roots` devolve o caminho físico** (`MonitoringRootDto.Path`, e `CollectionSyncRootDto` também), usado pelo WinApp para abrir pastas no Explorer. O README e a documentação de segurança tratam o serviço como "sem caminho físico/sem enumeração"; isso só vale para `resolve`.
- O README da ApiFileStorage e a documentação histórica de quarentena/inventário ainda citam `import`, `delete*` e `export/*` (removidos) e não citam `synchronizations/discovery`.
- **DI (corrigido):** `MonitoringRoots` agora é registrado sempre (`Program.cs`, `TryAddSingleton`), pois só lê disco; antes ficava dentro de `AddMonitoringPersistence` e a descoberta (`CollectionSyncDiscoveryService`) falharia com `Monitoring:Enabled=false`. Consequência: descoberta de sincronização e varredura de pasta de anime funcionam sem o banco de monitoramento (cobertas por teste); sessões continuam exigindo `Monitoring:Enabled=true`.
- Documento histórico de monitoramento traz números agregados de inventário e contagem de testes desatualizados; não foram reproduzidos.
- Dúvida aberta: política de retenção do histórico de monitoramento.

## Base para o indicador de pasta do anime

A **Fase 4** do plano dos ícones entregou a contagem sob demanda (ver [Contagem de vídeos da pasta do anime](#contagem-de-vídeos-da-pasta-do-anime)) e a **Fase 5** entregou o mapeamento anime → subpasta e a persistência do resultado na ApiMyAnimes (tabela `AnimeFolderStatus`, ver [22_ApiMyAnimes.md](22_ApiMyAnimes.md#status-de-pasta-dos-animes-fase-5)); o restante (botão e exibição) é das fases seguintes, no WinApp e no site.

### O que já existe

| Necessidade | Disponível hoje |
| --- | --- |
| Pasta da coleção | vínculo `MyAnimeId → (RootKey, RelativePath)` em `MyAnimeMonitoringLocations` |
| A pasta existe/está acessível | snapshot com `Completion = Unavailable` ou observação `READ_FAILED`/evento `UNAVAILABLE` indicam ausência ou inacessibilidade; `Complete`/`Partial` indicam que foi lida |
| Pasta vazia | observação `EMPTY_DIRECTORY` e totais `Files = 0` |
| Lista de arquivos | `Entries` do snapshot: `RelativePath`, `IsDirectory`, `Bytes`, `ModifiedAtUtc`, paginável por `SnapshotId` |
| Subpastas de anime | convenção `<ano> <titulo> - <tipo>` (sufixo ` (n)` para duplicados) com capa `<MalId>.jpg` dentro de cada subpasta de anime |
| Vídeos | `EpisodeVideoPolicy` (`LibDtudo.Shared`) é a lista única de extensões e extras, usada pela API (`anime-folders/scan`) e pelo `FUC_Monitoramento`; a contagem por pasta é feita no servidor |
| Frescor | `InventoriedAtUtc`, `InventoryOutdated` e eventos em tempo real |
| Episódios do anime | disponíveis no catálogo ([ApiMyAnimes](22_ApiMyAnimes.md)/[ApiMyAnimeList](23_ApiMyAnimeList.md)), não no monitoramento |

Ou seja, é possível saber se a **coleção** existe e contém arquivos e, para uma pasta de anime conhecida, se ela existe e quantos vídeos de episódio tem, sem abrir sessão.

### O que falta

Resolvido na Fase 4: ~~consulta passiva e leve~~ (`anime-folders/scan`), ~~contagem de vídeos no servidor e lista compartilhada de extensões~~ (`EpisodeVideoPolicy`), ~~regra do que é vídeo de episódio~~ (extras, 0 bytes, recursivo) e a regra de estado `none`/`partial`/`complete` com `expectedEpisodes` desconhecido (`AnimeFolderState`).

Resolvido na Fase 5: ~~mapeamento anime → subpasta~~ (`anime-folders/discovery` + `AnimeFolderMapper` na ApiMyAnimes: capa `<MalId>.jpg`, depois convenção de nome com ano, depois capa na pasta da coleção de um único anime; pares ambíguos ficam sem pasta), ~~política de atualização e persistência~~ (atualização sob demanda por coleção ou geral, gravada em `AnimeFolderStatus` com `CheckedAtUtc`; o site só lê) e ~~`exists = false` não distingue "não existe" de "disco offline"~~ (a ApiMyAnimes trata pasta/disco indisponível como "não verificável" e **preserva o status anterior**, em vez de gravar `none`). Continuam abertos:

1. **Limites conhecidos da contagem:** arquivos que agrupam vários episódios, versões duplicadas e temporadas não são tratados; só há tolerância por `expectedEpisodes` (≥).
2. **Limites do mapeamento por nome:** a convenção só casa por título exato (principal, inglês ou alternativos, sem acento/caixa) ou pelas quatro primeiras palavras com o **mesmo ano**; anime sem capa e sem esse padrão fica sem pasta (`NotFound`) até a pasta ser renomeada ou receber a capa `<MalId>.jpg`.
3. **Retenção:** sem limpeza, o histórico de `Entries` cresce a cada snapshot (não afeta o `scan`, que não grava histórico).

### Caminho entregue

A ApiMyAnimes chama `discovery` (uma vez por coleção) e `scan` (uma vez por anime encontrado), com concorrência limitada, e persiste o resultado comparando com `Episodes`/`Episodios` do catálogo. O `scan` continua sem conhecer anime nem catálogo.
