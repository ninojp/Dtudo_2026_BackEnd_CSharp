# ApiDiscogs

A **ApiDiscogs** é uma API **somente leitura** que encapsula a API pública da Discogs para a solução. Ela esconde o token e as URLs externas do cliente, valida entradas, aplica allowlist de egress (anti-SSRF), cache, limites e resiliência, e devolve contratos próprios e estáveis de artistas, releases e masters. Não tem banco, não grava dados e **não chama a [ApiMusicX](25_ApiMusicX.md)**: quem orquestra busca → confirmação → importação é o [WinAppDtudo](11_WinAppDtudo.md).

Contexto: [00_Solucao.md](00_Solucao.md) · Segurança: [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md) · Gateway: [20_DtudoGateway.md](20_DtudoGateway.md) · Identidade: [21_ApiIdentity.md](21_ApiIdentity.md) · Site: [10_DtudoSite.md](10_DtudoSite.md) · Biblioteca: [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md).

## Visão geral

| Item | Valor |
|---|---|
| Projeto / testes | `ApiDiscogs` / `tests\ApiDiscogs.Tests` |
| Stack | .NET 10, JWT Bearer, `Microsoft.Extensions.Http.Resilience` 10.11.0, `IMemoryCache`, Serilog |
| Rota base | `ApiDiscogs` |
| URLs de desenvolvimento | `https://localhost:7147` / `http://localhost:7148` |
| Audience | `urn:dtudo:api-discogs` |
| Fonte externa | `https://api.discogs.com` (HTTPS, porta 443) |
| Fluxo | WinAppDtudo / DtudoSite → (DtudoGateway) → ApiDiscogs → Discogs |

## Contrato externo de leitura

Todos os endpoints são `GET` e exigem JWT. Parâmetros inválidos retornam 400 **antes** de qualquer chamada externa.

| Rota | Política | Parâmetros e limites |
|---|---|---|
| `ApiDiscogs/health` | `health.read` | sem chamada externa; `{status:"ok", service:"ApiDiscogs"}` |
| `ApiDiscogs/artists/search` | `catalog.external.read` | `q` (2–120 caracteres após *trim*), `page` ≥ 1, `perPage` 1–20 (padrão 10) |
| `ApiDiscogs/artists/{discogsArtistId}` | `catalog.external.read` | ID inteiro positivo |
| `ApiDiscogs/artists/{discogsArtistId}/releases` | `catalog.external.read` | `page`, `perPage` 1–100 (padrão 50), `expand` = `none` \| `master` |
| `ApiDiscogs/releases/{discogsReleaseId}` | `catalog.external.read` | ID inteiro positivo |
| `ApiDiscogs/masters/{discogsMasterId}` | `catalog.external.read` | ID inteiro positivo |

IDs aceitam apenas dígitos decimais positivos (sinais, espaços e notação alternativa são rejeitados).

### Contratos de saída (resumo)

- Paginação: `DiscogsPagedResponse` / `DiscogsPagination`; origem rastreável em `DiscogsSourceReference`.
- Busca: `DiscogsArtistSearchItem`; detalhe: `DiscogsArtistDetails`.
- Discografia: `DiscogsArtistReleasesResponse` com `DiscogsReleaseSummary` — `CanonicalId` (`master:{id}` ou `release:{id}`), `Category` (`album`, `singleEp`, `compilation`, `video`, `unknown`), formatos e papéis.
- Detalhes: `DiscogsReleaseDetails`, `DiscogsMasterDetails` (com `DiscogsTrack`, `DiscogsCredit`, `DiscogsLabel`, `DiscogsImage`).
- O mapper descarta releases com formato "unofficial", **deduplica por `CanonicalId`** agregando formatos e papéis, e só entrega URLs de imagem HTTPS. Envelope ou paginação ausente → resposta inválida (502).
- Os DTOs ficam no próprio projeto (domínio musical específico), não na LibDtudo.Shared.

## Política de acesso

- Fallback: usuário autenticado. Políticas `permission:{x}` exigem claim `permission` **e** o mesmo valor em `scope`/`scp`.
- Na ApiDiscogs, `catalog.external.read` exige **apenas** `catalog.read` (diferente da [ApiMyAnimeList](23_ApiMyAnimeList.md), que também aceita `service.mal.read`). Não existe `service.discogs.read`.
- Gateway (cluster `api-discogs-external`, repassa o `Authorization`): `/api/external/discogs/artists/search`, `artists/{id:int}`, `artists/{id:int}/releases`, `releases/{id:int}` e `masters/{id:int}` → `/ApiDiscogs/...`. O `health` não é publicado. A audience `urn:dtudo:api-discogs` é provisionada no [ApiIdentity](21_ApiIdentity.md).
- Swagger só em Development, restrito a `health.read`. CORS por `Cors:AllowedOrigins`.

## Erros

`DiscogsExceptionHandler` devolve `ProblemDetails` (tipos `https://dtudo.local/problems/discogs-dependency` e `…/validation`) com extensões `code`, `traceId` e, quando aplicável, `retryAfterSeconds`; o cabeçalho `Retry-After` só acompanha o 429. Nunca expõe URL externa, token ou corpo bruto da Discogs.

| `code` | HTTP | Origem |
|---|---:|---|
| `discogs_validation` | 400 | entrada inválida |
| `discogs_request_rejected` | 400 | Discogs 400/422 |
| `discogs_resource_not_found` | 404 | Discogs 404 |
| `discogs_rate_limited` | 429 | Discogs 429 (com `Retry-After` limitado a 0–3600 s) |
| `discogs_unavailable` | 503 | Discogs 503 ou circuito aberto |
| `discogs_timeout` / `discogs_gateway_timeout` | 504 | 408, timeout ou 504 |
| `discogs_configuration_error` | 502 | Discogs 401/403 (token inválido; não repassa 401 ao cliente) |
| `discogs_upstream_error` | 502 | Discogs 500/501/502 |
| `discogs_invalid_response` | 502 | JSON inválido, envelope ausente, resposta acima do limite |
| `discogs_connection_error` | 502 | demais falhas de conexão |

Cancelamento pelo chamador não é convertido em erro.

## Limites, cache e resiliência

| Parâmetro (`ApiDiscogs:*`) | Padrão | Faixa validada |
|---|---:|---|
| `TimeoutSeconds` (por tentativa) | 20 | 1–300 |
| `TotalTimeoutSeconds` | 90 | 1–900 (≥ por tentativa) |
| `MaxRetries` | 3 | 0–10 |
| `RetryDelayMilliseconds` | 250 | 1–5000 (exponencial + jitter, teto 8 s, respeita `Retry-After`) |
| `CircuitBreaker` (razão / throughput mín. / amostragem / pausa) | 0,5 / 5 / 30 s / 30 s | razão > 0 e ≤ 1; 2–1000; 1–3600; 1–3600 |
| `CacheMinutes` | 15 | 1–1440 |
| `MaxResponseBytes` | 2.000.000 | 1024–20.000.000 |

- Ordem do pipeline `discogs` (fora → dentro): timeout total → retry → circuit breaker → timeout por tentativa; `HttpClient.Timeout` é infinito.
- Retry e breaker só para métodos idempotentes e resultados transitórios (408, 429, 5xx, timeout, `HttpRequestException` sem status), **exceto** `DiscogsEgressException`.
- Cache em memória (`apidiscogs:v1:…`), chave com parâmetros normalizados (sem token), apenas respostas de sucesso.
- A API **nunca segue** `pagination.urls.next` nem aceita URL externa: monta apenas caminhos relativos internos.

## Segurança

- **Token Discogs** (`ApiDiscogs:Token`, obrigatório, ≤ 512): segredo em User Secrets/variável de ambiente (`ApiDiscogs__Token`); jamais em arquivo versionado, log ou resposta. O cabeçalho `Authorization: Discogs token=<token>` é injetado por `DiscogsAuthenticationHandler` **apenas** na requisição à Discogs. `UserAgent` configurável (padrão `Dtudo-ApiDiscogs/1.0`, sem CR/LF).
- **Egress** (`DiscogsEgressHandler`): `BaseUrl` HTTPS em `api.discogs.com:443` sem *userinfo*/query; `AllowedHosts` somente `api.discogs.com`; prefixos de caminho fixos em código (`/database/`, `/artists/`, `/releases/`, `/masters/`), sem *path traversal*; sem redirecionamento e sem proxy; `ConnectCallback` resolve DNS e rejeita endereços não públicos; `MaxConnectionsPerServer` 20 e `PooledConnectionLifetime` 5 min.
- Correlação (`X-Correlation-ID` propagado à saída) e `SensitiveDataRedactionEnricher` nos logs.
- Detalhes gerais de autenticação e mTLS: [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md).

## Configuração

| Chave | Descrição |
|---|---|
| `ApiDiscogs:Token` | **Segredo** (placeholder em docs: `<TOKEN_LOCAL_FORA_DO_REPOSITORIO>`) |
| `ApiDiscogs:BaseUrl` / `AllowedHosts` / `AllowedPathPrefix` | Destino e allowlist (`AllowedPathPrefix` = `/`) |
| `ApiDiscogs:*` de limites/resiliência | Tabela acima; validados no startup (falha ao iniciar se inválidos) |
| `Authentication:Issuer` / `Audience` | Em Development: `https://localhost:7243/` e `urn:dtudo:api-discogs` |
| `Cors:AllowedOrigins` | Em Development: `http://localhost:5173`, `http://localhost:5178` |
| `Seq:Url` | Vazio por padrão |

## Testes

`tests\ApiDiscogs.Tests` (20 métodos): `DiscogsClientTests` (12: correlação, sem retry em status permanentes, `Retry-After` do 429, limite de retries em 500, timeout, cancelamento, circuito, chaves de cache, entrada não altera host/caminho, allowlist de egress, token só na saída), `DiscogsExceptionHandlerTests` (2), `DiscogsMapperTests` (6).

```powershell
dotnet test .\tests\ApiDiscogs.Tests\ApiDiscogs.Tests.csproj
```

## Pendências/Divergências

- **Allowlist de caminhos:** o código usa `AllowedPathPrefix` único `/` com lista fixa que libera `/database/` inteiro (o desenho original previa só `/database/search`); cache de 15 min.
- **`expand=master` incompleto:** o valor é apenas repassado como parâmetro; não há enriquecimento com até 10 requisições complementares, `IsComplete` permanece `true`, `warnings` nunca é preenchido e não há orquestração com limite de 20 páginas/1000 itens prevista no contrato.
- **Retry-After:** o contrato prevê janela configurável; o código limita a 0–3600 s fixos.
- **Parâmetros divergentes do MAL:** o contrato pede alinhar limites ao MAL (8 tentativas/1000 ms); a ApiDiscogs usa 3/250 ms.
- **Permissão dedicada:** decidir se `service.discogs.read` deve existir (hoje só `catalog.read`).
- **IDs como `string` na rota:** o parsing é manual (a rota do gateway restringe `{id:int}`).
- O `README.md` do projeto ainda tem tom histórico ("Parte 2.x").
