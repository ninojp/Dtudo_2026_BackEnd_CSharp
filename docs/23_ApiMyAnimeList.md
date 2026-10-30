# ApiMyAnimeList

A **ApiMyAnimeList** é a fonte externa **independente e exclusiva** de busca, capas, detalhes e relações oficiais de animes da solução. Ela encapsula a API oficial do MyAnimeList (v2), normaliza o payload em contratos próprios e aplica cache, resiliência (retry, circuit breaker, timeouts) e uma allowlist de egress contra SSRF. Substitui integralmente o fluxo antigo de consulta externa de animes; não deve surgir nenhum novo fluxo para a API externa antiga.

Contexto: [00_Solucao.md](00_Solucao.md) · Segurança: [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md) · Catálogo local: [22_ApiMyAnimes.md](22_ApiMyAnimes.md) · Identidade: [21_ApiIdentity.md](21_ApiIdentity.md) · Cliente desktop: [11_WinAppDtudo.md](11_WinAppDtudo.md) · Contratos: [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md).

## Visão geral

| Item | Valor |
|---|---|
| Projeto / testes | `ApiMyAnimeList` / `tests\ApiMyAnimeList.Tests` |
| Stack | .NET 10, JWT Bearer, `Microsoft.Extensions.Http.Resilience` 10.11.0, `IMemoryCache`, Serilog |
| Rota base | `ApiMyAnimeList` |
| URL de desenvolvimento | `https://localhost:7146` |
| Audience | `urn:dtudo:api-my-animelist` |
| Fonte externa | `https://api.myanimelist.net/v2/` (cabeçalho `X-MAL-CLIENT-ID`, `User-Agent` `Dtudo-ApiMyAnimeList/1.0`) |
| Persistência | nenhuma (sem banco) |
| Consumidores | `WinAppDtudo` (chamada **direta**: busca, detalhe, relações, health) e `ApiMyAnimes` (importação `POST Anime?malId=`) |

O `DtudoGateway` **não** possui rota para esta API ([20_DtudoGateway.md](20_DtudoGateway.md)); o site não a consome.

## Responsabilidade e fronteiras

- É a única fonte de busca, capas, detalhes e relações **oficiais** do MAL. As relações do DB_Local (`MyAnimeId`) são independentes e vivem em [22_ApiMyAnimes.md](22_ApiMyAnimes.md).
- Não grava dados: quem persiste (importação) é a ApiMyAnimes, orquestrada pelo WinAppDtudo.
- Os DTOs de saída ficam em `LibDtudo.Shared\Dtos\MyAnimeList\MyAnimeListContracts.cs` (`AnimeSearchResult`, `AnimeSearchCard`, `AnimeDetails`, `AnimeRelationGroup`, `AnimeRelationEntry`, `AnimeImages`…).

## Endpoints

| Método e rota | Política | Resultado |
|---|---|---|
| `GET ApiMyAnimeList/health` | `health.read` | saúde local (não chama o MAL) |
| `GET ApiMyAnimeList/search?q=&page=` | `catalog.external.read` | `AnimeSearchResult` (limite fixo de 20 por página, `nsfw=true`) |
| `GET ApiMyAnimeList/{id:int}` | `catalog.external.read` | `AnimeDetails` |
| `GET ApiMyAnimeList/{id:int}/relations` | `catalog.external.read` | grupos de relações (por `relation_type`) |

`catalog.external.read` = (`catalog.read` com escopo `catalog.read`) **ou** (`service.mal.read` com escopo `service.mal.read`). A política de fallback exige autenticação; OpenAPI (`MapOpenApi`) só em Development e restrito a `health.read`.

### Códigos de erro

| Situação | HTTP | Observação |
|---|---|---|
| `q` vazio, `page < 1`, `id ≤ 0` | 400 | validação de entrada |
| MAL responde "não encontrado" | 404 | — |
| Circuito aberto ou falha de rede | 503 | `Retry-After: 30` |
| Timeout / upstream demorou (inclui 504) | 504 | `Retry-After: 5` |
| Outro `HttpRequestException` | 503 (status nulo, 429 ou ≥ 500) ou 502 (outros 4xx) | — |
| Cancelamento do chamador | — | não vira erro HTTP |

### Mapeamento (`MyAnimeListMapper`)

`Aired` como `"início to fim"` (texto em inglês, `?` quando ausente)

## Cache

`IMemoryCache` apenas para **detalhes** (chave `mal-anime-{id}`), TTL `CacheMinutes` (padrão 15; faixa 1–1440). Detalhes e relações compartilham a mesma chamada e o mesmo cache. A busca não é cacheada.

## Resiliência

Pipeline do `HttpClient` (ordem de fora para dentro): timeout total → circuit breaker → retry → timeout por tentativa. `HttpClient.Timeout` é infinito; os prazos vêm do pipeline.

| Parâmetro (`MyAnimeList:*`) | Padrão | Faixa validada |
|---|---:|---|
| `TimeoutSeconds` (por tentativa) | 20 | 1–300 |
| `TotalTimeoutSeconds` | 90 | 1–900 (≥ por tentativa) |
| `MaxRetries` | 8 | 0–10 (0 omite o retry) |
| `RetryDelayMilliseconds` | 1000 | 1–5000 (backoff exponencial com jitter, teto 8 s, respeita `Retry-After`) |
| `CircuitBreakerFailureRatio` | 0,5 | > 0 e ≤ 1 |
| `CircuitBreakerMinimumThroughput` | 5 | 2–1000 |
| `CircuitBreakerSamplingSeconds` / `BreakSeconds` | 30 / 30 | 1–3600 |
| `CacheMinutes` | 15 | 1–1440 |

Regras: retry e breaker só para métodos idempotentes (GET/HEAD/OPTIONS/PUT/DELETE) e resultados transitórios (408, 429, 5xx, timeout, `HttpRequestException` exceto `MyAnimeListEgressException`, e `SocketError`s específicos). O circuito é **em memória, por processo**. Opções são validadas no startup (`ValidateOnStart`).

Tratamento de 504: o MAL costuma devolver 504 sob carga; o pipeline repete com backoff e, esgotadas as tentativas, a API responde 504 com `Retry-After: 5`. O WinAppDtudo deve tratar 503/504 como falha temporária e permitir nova tentativa (ver [11_WinAppDtudo.md](11_WinAppDtudo.md)).

## Egress e proteção contra SSRF

`MyAnimeListEgressHandler` + `SocketsHttpHandler`:

- `AllowedHosts` (padrão `api.myanimelist.net`) e `AllowedPathPrefix` (`/v2/`); somente HTTPS na porta 443, sem *userinfo*;
- sem redirecionamento automático, sem proxy, `MaxConnectionsPerServer` 20, `PooledConnectionLifetime` 5 min;
- `ConnectCallback` resolve DNS e **rejeita endereços não roteáveis globalmente** (loopback, privados, link-local, CGNAT, documentação, multicast, ULA); falha de DNS vira erro de resolução;
- nenhuma rota aceita URL fornecida pelo cliente.

## Autenticação e autorização

- JWT Bearer (Issuer `https://localhost:7243/` em desenvolvimento, Audience `urn:dtudo:api-my-animelist`).
- Políticas: `catalog.read`, `catalog.write`, `catalog.delete`, `health.read`, `service.mal.read`, `catalog.external.read`.
- **Principal de serviço:** `ServiceClientCertificateMiddleware`, quando `ServiceAuthentication:Enabled` e o token traz `service.mal.read` (permissão + escopo), exige **certificado de cliente** (mTLS) validado por `ServiceCertificateValidator` contra os vínculos de `ServiceTokenIssuerOptions.Clients`; falha → 401. O Kestrel usa `ClientCertificateMode=AllowCertificate`. Em Development, `ServiceAuthentication` fica `Enabled: false`. Ver [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md).
- CORS: política com `Cors:AllowedOrigins` (mesmo conjunto da ApiMyAnimes).

## Configuração

| Chave | Descrição |
|---|---|
| `MyAnimeList:ClientId` | **Segredo** do MAL (User Secrets/ambiente; nunca versionar nem logar); validado no startup |
| `MyAnimeList:BaseUrl` / `AllowedHosts` / `AllowedPathPrefix` | Destino e allowlist de egress |
| `MyAnimeList:*` de resiliência e cache | Tabela acima |
| `Authentication:Issuer` / `Audience` | Validação do JWT |
| `ServiceAuthentication:*` | mTLS de principais de serviço |
| `Cors:AllowedOrigins`, `Seq:Url` | CORS e observabilidade opcional |

Logs: Serilog com `SensitiveDataRedactionEnricher`, correlação `X-Correlation-ID` e `CorrelationIdDelegatingHandler` na saída.

## Testes

`tests\ApiMyAnimeList.Tests` (~22 métodos): `MyAnimeListHealthTests` (11: autenticação, health e startup), `MyAnimeListLoggingTests` (1), `MyAnimeListMapperTests` (2), `MyAnimeListResilienceTests` (8: retry, 429/5xx/504, circuito, egress).

```powershell
dotnet test .\tests\ApiMyAnimeList.Tests\ApiMyAnimeList.Tests.csproj
```

## Pendências/Divergências

- **Circuito em memória:** o estado do breaker não é compartilhado entre instâncias.
- **Homologação ainda por provar:** DNS/firewall/TLS e comportamento real de `Retry-After` com o MAL; ajustar parâmetros com as métricas do Seq.
- **Sem rota no gateway:** acesso só direto (WinApp) e serviço-a-serviço (ApiMyAnimes); decidir se deve haver fachada no gateway.
