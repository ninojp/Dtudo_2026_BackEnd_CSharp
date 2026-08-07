# Etapa 16 - DtudoGateway e BFF

## Estado

- Estado: `Concluida no escopo de implementacao e validacao local Development`.
- O `DtudoGateway` e um projeto ASP.NET Core separado e a unica entrada configurada para o catalogo e para os callbacks OIDC.
- O navegador recebe somente uma sessao protegida por cookie e dados de usuario minimizados; access token e refresh token ficam no ticket server-side.
- Nenhum segredo, token, cookie de sessao ou connection string real foi registrado no repositorio.

## Rotas expostas

| Rota | Metodo | Controle | Destino |
|---|---|---|---|
| `/bff/login` | GET | allowlist de redirect | challenge OIDC |
| `/bff/antiforgery` | GET | token de requisicao CSRF | gateway |
| `/bff/me` | GET | cookie BFF autenticado | gateway |
| `/bff/logout` | POST | cookie BFF + antiforgery + allowlist | logout OIDC |
| `/api/catalog/animes` | GET | rota YARP explicita | `ApiMyAnimes` |
| `/api/catalog/animes/search` | GET | rota YARP explicita | `ApiMyAnimes` |
| `/api/catalog/animes/{id}` | GET | rota YARP explicita | `ApiMyAnimes` |
| `/api/catalog/collections` | GET | rota YARP explicita | `ApiMyAnimes` |
| `/api/catalog/collections/{id}` | GET | rota YARP explicita | `ApiMyAnimes` |
| `/identity/connect/authorize` | GET | rota YARP explicita | `ApiIdentity` |
| `/identity/connect/logout` | GET | rota YARP explicita | `ApiIdentity` |

`/signin-oidc` e `/signout-callback-oidc` sao callbacks tratados pelo middleware OIDC. Nao existe rota generica de proxy. Token endpoint, discovery, health detalhado, Swagger, `ApiMyAnimeList`, `Auth` legado e mutacoes do catalogo nao sao expostos.

## Controles

- OIDC usa Authorization Code, `response_type=code`, PKCE obrigatorio e `S256`.
- O redirect OIDC e reescrito para o host publico do gateway; o navegador nao recebe URL publica da `ApiIdentity`.
- Redirects locais e absolutos sao aceitos somente para as origens configuradas. URI com `userinfo`, fragmento, barra dupla, barra invertida ou origem externa e recusada.
- O cookie `__Host-dtudo-bff` e `HttpOnly`, `Secure`, `SameSite=Lax`, sem dominio e com expiracao maxima de 30 dias.
- Correlation e nonce cookies OIDC sao `HttpOnly`, `Secure` e `SameSite=None` para o retorno de autenticacao.
- O cookie `__Host-dtudo-xsrf` e `Secure`, `SameSite=Strict` e nao `HttpOnly`; o token de requisicao e exigido no header `X-CSRF-TOKEN` para logout.
- O ticket OIDC, incluindo tokens salvos pelo handler, e gravado no `IDistributedCache`; o cookie contem somente o identificador protegido da sessao server-side.
- Catalogo remove `Authorization`, `Cookie` e `Set-Cookie` no proxy. Os callbacks OIDC removem somente os cookies do gateway e preservam cookies proprios do provedor.
- Rotas nao cadastradas retornam `404`; mutacoes nos caminhos de catalogo retornam `405` e nao alcancam a API.
- Configuracao obrigatoria falha no startup quando authority, client ID, client secret externo, destinos HTTPS ou allowlist sao invalidos.

## Evidencias

Comando executado:

```text
dotnet test .\tests\DtudoGateway.Tests\DtudoGateway.Tests.csproj --no-restore
```

Resultado: `10/10` testes aprovados, `0` falhas e `0` ignorados.

Os testes cobrem rejeicao de open redirect, origem absoluta permitida, `userinfo`, code + PKCE/S256, CSRF ausente, logout com CSRF e exclusao do cookie, atributos de cookie, ausencia de tokens na resposta de `/bff/me`, mutacoes/rotas internas negadas e enumeracao das rotas YARP.

`get_errors` nao encontrou erros nos arquivos do gateway ou dos testes.

## Riscos residuais e acoes manuais

- O provider live ainda precisa registrar o client `dtudo-gateway` com redirect `/signin-oidc`, callback de logout, PKCE e segredo mantido fora do repositorio. O handler de autorizacao/login e logout da `ApiIdentity` nao foi exercitado neste teste local.
- `AddDistributedMemoryCache` atende Development local. Antes de homologacao ou producao, substituir por armazenamento distribuido persistente e protegido, mantendo o ticket fora do navegador.
- `OpenIdConnect:ClientSecret` permanece vazio no `appsettings.json` de Development; fornecer o valor somente por User Secrets, variavel de ambiente ou cofre aprovado. Nao copiar o segredo para o React.
- A destinacao HTTPS, certificado, issuer/discovery e mapeamento de portas precisam de exercicio live antes da Etapa 17.

## Rollback

Remover o projeto `DtudoGateway`, o projeto de testes, as duas entradas da solucao, este documento e a linha da Etapa 16 no status. Nao ha migration nem alteracao de banco. Em caso de integracao parcial, retirar as rotas publicas do gateway e manter as APIs internas sem exposicao.

## Proxima etapa

A Etapa 17 e a unica proxima etapa permitida. Ela nao foi iniciada neste chat.
