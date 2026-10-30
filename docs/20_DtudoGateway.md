# DtudoGateway

O **DtudoGateway** é o BFF (Backend for Frontend) do [DtudoSite](10_DtudoSite.md): conduz o login OpenID Connect (authorization code + PKCE) com a [ApiIdentity](21_ApiIdentity.md), mantém a sessão em cookie e atua como **proxy reverso YARP** somente-leitura para as APIs de catálogo (exceções: `POST /api/catalog/animes/status`, leitura em lote, e as rotas de progresso de episódios `.../animes/{id}/watch*`; as mutações exigem CSRF). O navegador nunca recebe access/refresh token.

Contexto geral: [00_Solucao.md](00_Solucao.md) e [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md).

## Stack

| Item | Valor |
|---|---|
| Projeto | `DtudoGateway` (net10.0, ASP.NET Core minimal API) |
| Pacotes | `Microsoft.AspNetCore.Authentication.OpenIdConnect` 10.0.x, `Yarp.ReverseProxy` 2.3.0 |
| URLs locais | `https://localhost:51376` e `http://localhost:51377` |
| User Secrets | `dtudo2026-gateway` |
| Health | `GET /health/live` (anônimo, `{"status":"ok"}`) |

## Autenticação e sessão

- **Esquemas**: `BffCookie` (sessão do site) e `oidc` (OpenID Connect, authorization code + **PKCE**, `response_mode=query`, `SaveTokens`, `MapInboundClaims=false`).
- **Cookie de sessão** `__Host-dtudo-bff`: `HttpOnly`, `Secure`, `SameSite=Lax`, sliding; em `/bff/*` não autenticado responde `401` (sem redirect).
- **Ticket server-side** (`ServerSideTicketStore`): o cookie guarda apenas uma chave aleatória (32 bytes); o ticket (com tokens) fica em `IDistributedCache`. Há índice por `sub` (hash SHA-256): um novo login do mesmo sujeito **substitui** a sessão anterior. Expiração = mínimo entre idle e vida absoluta.
- **Cookies de correlation/nonce** OIDC: `SameSite=None; Secure; HttpOnly`.
Scopes `openid profile catalog.read personal.read personal.write offline_access` (os `personal.*` exigem novo login após a atualização);
- **Callbacks**: `/signin-oidc` e `/signout-callback-oidc`. O endereço de authorize/logout é reescrito para `{PublicOrigin}/identity/connect/authorize` e `/identity/connect/logout` (o navegador chega à ApiIdentity pelo próprio gateway).
- Falha remota OIDC → `400 authentication_failed`.
- **DataProtection**: chaves em `%LOCALAPPDATA%\Dtudo2026\DtudoGateway\DataProtectionKeys`, protegidas por DPAPI (Windows).
- **CSRF** (antiforgery): header `X-CSRF-TOKEN`, cookie `__Host-dtudo-xsrf` (`Secure`, `SameSite=Strict`, legível pelo JS); no logout também aceita o campo de formulário `__RequestVerificationToken`. O token é vinculado ao usuário autenticado: o site deve buscá-lo em `/bff/antiforgery` **depois** do login.

### Renovação de tokens e revogação

O access token da ApiIdentity dura 300 s; sem renovação, as rotas com Bearer passariam a responder `401` após 5 minutos de sessão.

`POST /api/catalog/animes/status` e `GET/PUT/DELETE /api/catalog/animes/{id}/watch[/episodes|/all]`) e apenas se
- Se o `access_token` expira em até 60 s (ou `expires_at` não existe), o gateway chama o token endpoint (`grant_type=refresh_token`, `client_id`/`client_secret`, mesmo `Backchannel` do OIDC) e grava `access_token`, `refresh_token`, `id_token` e `expires_at` no ticket server-side (`ShouldRenew`).
- O refresh token da ApiIdentity é **rotativo**: requisições paralelas da mesma sessão compartilham uma única renovação (`GatewayTokenRefresher`, dedupe em memória por hash SHA-256 do token, 60 s).
- `invalid_grant` (400) → tokens removidos da sessão (as APIs passam a responder `401` e o site pede novo login). Falha transitória (rede/5xx) → sessão intacta, nova tentativa na próxima requisição.
- `POST /bff/logout` revoga o refresh token (`revocation_endpoint`, melhor esforço: falha não impede o logout) antes de encerrar o cookie.
- Sessões criadas antes desta mudança não têm refresh token (não havia `offline_access`) e exigem novo login.

## Rotas BFF

| Rota | Proteção | Função |
|---|---|---|
| `GET /bff/login?returnUrl=` | anônima; `returnUrl` na allowlist (`400 invalid_redirect`) | Inicia o challenge OIDC |
| `GET /bff/antiforgery` | anônima | Devolve `{token}` para uso no logout |
| `GET /bff/me` | `BffPolicy` (sessão) | `{authenticated, user:{subject,name,email,roles}}` — **sem tokens** |
| `POST /bff/logout?returnUrl=` | sessão + allowlist + antiforgery (`400 invalid_csrf`) | Revoga o refresh token (melhor esforço) e encerra cookie e sessão OIDC |
| `GET /bff/logout` | anônima | Apenas redireciona ao front `/auth/logout` (nunca faz logout por GET) |
| `GET /health/live` | anônima, fora do rate limit | Liveness |

## Rotas proxy (YARP)

**Somente `GET`**, rotas exatas (sem proxy genérico), com **exceções**: `POST /api/catalog/animes/status` e as 5 rotas de progresso de episódios (`GET`/`PUT`/`DELETE` em `watch`). Todas exigem sessão (`gateway-authenticated-catalog`; as exceções usam `gateway-authenticated-mutation`, que responde `401` em vez de redirecionar, inclusive no `GET` de `watch`, que é sempre XHR)

| Rota pública | Destino | Cluster | Token ao backend |
|---|---|---|---|
| `/api/catalog/animes` | `/apiLocal/Anime` | [ApiMyAnimes](22_ApiMyAnimes.md) | nenhum (remove `Authorization`/`Cookie`) |
| `/api/catalog/animes/search` | `/apiLocal/Anime/buscar` | ApiMyAnimes | idem |
| `/api/catalog/animes/relacionados` | `/apiLocal/Anime/relacionados` | ApiMyAnimes | idem |
| `/api/catalog/animes/{id:int}` | `/apiLocal/Anime/{id}` | ApiMyAnimes | idem |
| `/api/catalog/collections` | `/apiLocal/MyAnime` | ApiMyAnimes | idem |
| `/api/catalog/collections/{id:int}` | `/apiLocal/MyAnime/{id}` | ApiMyAnimes | idem |
| `POST /api/catalog/animes/status` | `POST /apiLocal/Anime/status` | ApiMyAnimes | **access_token da sessão** (só esta rota de anime); remove `Cookie` e `X-CSRF-TOKEN`; exige CSRF, `Content-Type: application/json` e corpo ≤ 16 KiB |
| `GET /api/catalog/animes/{id:int}/watch` (`anime-watch-get`) | `GET /apiLocal/Anime/{id}/watch` | ApiMyAnimes | **access_token da sessão**; remove `Cookie`; sem CSRF (leitura) |
| `DELETE /api/catalog/animes/{id:int}/watch` (`anime-watch-clear`) | `DELETE /apiLocal/Anime/{id}/watch` | ApiMyAnimes | idem; exige CSRF; remove `X-CSRF-TOKEN`; corpo ≤ 1 KiB (sem corpo) |
| `PUT /api/catalog/animes/{id:int}/watch/all` (`anime-watch-mark-all`) | `PUT /apiLocal/Anime/{id}/watch/all` | ApiMyAnimes | idem; exige CSRF; sem corpo; corpo ≤ 1 KiB |
| `PUT /api/catalog/animes/{id:int}/watch/episodes` (`anime-watch-mark-episodes`) | `PUT /apiLocal/Anime/{id}/watch/episodes` | ApiMyAnimes | idem; exige CSRF e `application/json`; corpo ≤ 64 KiB |
| `DELETE /api/catalog/animes/{id:int}/watch/episodes` (`anime-watch-unmark-episodes`) | `DELETE /apiLocal/Anime/{id}/watch/episodes` | ApiMyAnimes | idem; exige CSRF; corpo opcional (se houver, `application/json`), ≤ 64 KiB |
| `/api/catalog/music/collections`, `/collections/{id:long}`
| `/api/external/discogs/artists/search`, `/artists/{id:int}`, `/artists/{id:int}/releases`, `/releases/{id:int}`, `/masters/{id:int}` | `/ApiDiscogs/...` | [ApiDiscogs](26_ApiDiscogs.md) | **Bearer do access_token da sessão** |
| `/identity/connect/authorize` | `/connect/authorize` | ApiIdentity | anônima |
| `/identity/connect/logout` | `/connect/logout` | ApiIdentity | anônima |

e qualquer mutação além do status em lote e do progresso de episódios (`watch`).

## Controles de segurança

| Controle | Comportamento |
|---|---|
O `POST` de status e as mutações de `watch` (`PUT`/`DELETE`) são same-origin (proxy do Vite em dev), por isso não há CORS para eles
corpo `application/json` (`415 unsupported_media_type`; dispensado quando a rota tem `Dtudo.AllowEmptyBody` e a requisição não traz corpo, isto é, sem `Content-Length` > 0 nem `Transfer-Encoding`);
| Rate limit | Fixed-window por IP (60 req/60 s; partições `authentication:` e `general:`), `429` + `Retry-After`; `/health` isento. **Aplicado apenas fora de Development** |
| Redirect allowlist | `returnUrl` só relativo ou de origem permitida; rejeita userinfo e esquemas inválidos |
| Headers | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `X-Robots-Tag: noindex`, `Permissions-Policy`, CSP restritiva (`default-src 'self'`, `frame-ancestors 'none'`, `object-src 'none'`, `img-src 'self' https: data:`), remove `Server` |
| HTTPS | Redirecionamento; HSTS fora de Development |
| Forwarded headers | Somente `TrustedProxyAddresses` (loopback por padrão) |
| Corpo | `MaxRequestBodySize` Kestrel = 1 MiB |
| Hosts | `AllowedHosts: localhost` |

## Configuração

Seção `Gateway` (`appsettings.json`; valores locais): `PublicOrigin`, `FrontendOrigin` (Development: `http://localhost:5173/`), `AllowedRedirectOrigins`, `AllowedCorsOrigins`, `TrustedProxyAddresses`, `ApiMyAnimesBaseUrl` (:63980), `ApiMusicXBaseUrl` (:63982), `ApiDiscogsBaseUrl` (:7147), `ApiIdentityBaseUrl` (:7243), `SessionIdleTimeoutMinutes` (120), `SessionAbsoluteLifetimeHours` (24), `MaxRequestBodyBytes`, `RateLimitPermitLimit`/`RateLimitWindowSeconds`.

`Scopes` (inclui `personal.read`, `personal.write` e `offline_access`)

`GatewayOptionsValidator` valida no startup (falha fechada): origens e URLs HTTPS, `PublicOrigin` ∈ redirect origins, limites numéricos, `ClientSecret` obrigatório e scope `openid`.

## Testes

`tests\DtudoGateway.Tests\DtudoGatewayStatusAndRefreshTests.cs` (15 testes, mesma classe parcial): rota de status (`401` sem sessão, `400` sem CSRF, Bearer da sessão no lugar do `Authorization` do navegador, `415`/`413`, outras rotas de anime sem Bearer, `GET` não aceito), refresh único para requisições paralelas e persistido no ticket, token válido não renovado, `invalid_grant` remove tokens, falha transitória preserva, logout revoga e validador de `Resources`.

`tests\DtudoGateway.Tests\DtudoGatewayAnimeWatchTests.cs` (mesma classe parcial): metadados das 5 rotas `watch` (método único, CSRF só nas mutações, limites de corpo, remoção de `X-CSRF-TOKEN`/`Cookie`), injeção do Bearer apenas nos caminhos `watch` (incluindo caixa, ids inválidos e métodos errados), `401` sem sessão (sem redirecionar nem chamar o backend), `400 invalid_csrf` (ausente, forjado, sem cookie), `GET` sem CSRF repassando só o Bearer da sessão (ignora o `Authorization` do navegador), mutações repassando caminho/corpo corretos e **sem** `Cookie`/CSRF, `DELETE`/`PUT` sem corpo aceitos, `415` para corpo não JSON, `413` acima do limite, métodos/ids inválidos rejeitados e refresh do token antes de uma requisição `watch`.

`tests\DtudoGateway.Tests\DtudoGatewayTests.cs` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`; 18 `[Fact]/[Theory]`; total do projeto: 77): allowlist

## Pendências/Divergências

- Todas as rotas de catálogo exigem sessão (não existe catálogo anônimo); isso inclui `/animes/relacionados`, MusicX e Discogs (com Bearer injetado).
- `AddDistributedMemoryCache` ainda é o armazenamento dos tickets (apenas Development; sessões se perdem ao reiniciar e não escalam). Para homologação/produção usar cache distribuído persistente (a decidir).
- Rate limit desligado em Development (por desenho); precisa ser validado em Homologation.
- `ClientSecret` vazio no `appsettings.json` causa falha de startup até ser configurado via User Secrets.
- O gateway não proxifica `/identity/me/*` (recursos pessoais/LGPD da ApiIdentity); portanto o site ainda não os consome.
- **Sessões antigas**: criadas antes do `offline_access`/refresh não têm refresh token; as rotas com Bearer voltam a responder `401` após 5 min e exigem novo login.
- **Refresh rejeitado** (`invalid_grant`): a sessão do cookie permanece, mas sem tokens; as APIs respondem `401` até novo login (escolha restritiva; o gateway não finge sessão válida).
- **Revogação no logout** é melhor esforço (falha de rede não bloqueia o logout; o refresh token expira por conta própria).
- **Dedupe do refresh** é em memória do processo (instância única, projeto pessoal); com várias instâncias, a rotação do refresh token exigiria coordenação distribuída.
o `POST` de status e as mutações `PUT`/`DELETE` de `watch` só funcionam same-origin
o status em lote só é chamado pelo site com sessão autenticada.
- **Progresso de episódios (`watch`):** exige que o client `dtudo-gateway` receba `personal.read`/`personal.write`; após atualizar `OpenIddict:Gateway:Scopes` (ApiIdentity) e `OpenIdConnect:Scopes` (gateway), reiniciar a ApiIdentity (o seeder atualiza o cliente) e fazer **novo login**; sem isso a ApiMyAnimes responde `403` e o site orienta o usuário a entrar de novo. O `GET` de `watch` responde `401` (não redireciona) sem sessão.
