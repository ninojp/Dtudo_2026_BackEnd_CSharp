# Dtudo2026 — Visão geral da solução

Este documento descreve a arquitetura geral da solução Dtudo2026: o que o produto faz, quais projetos o compõem, como se comunicam, como a autenticação funciona de ponta a ponta, como rodar e compilar, e quais regras de arquitetura devem ser preservadas. Detalhes de cada projeto ficam nos documentos específicos listados no [mapa de documentação](#mapa-de-documentação).

## Visão do produto

Dtudo2026 é uma plataforma pessoal de catálogo e organização de mídia (animes, músicas e arquivos associados), composta por um aplicativo desktop Windows, um site React e um conjunto de APIs .NET 10. É um projeto pessoal, mantido por uma única pessoa, em desenvolvimento **local (Development)**, sem versão de produção nem implantação (ver [02_Solucao_Operacao.md](02_Solucao_Operacao.md) e [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md)).

> **O projeto central é o `WinAppDtudo`.** É por ele que o proprietário executa as ações principais: consulta, cadastro e manipulação de dados, consultas externas (ApiMyAnimeList, Discogs) e execução dos demais projetos (por exemplo, o DtudoSite). Ele deve **controlar o banco local** (via ApiMyAnimes) e os **arquivos em disco local** (via ApiFileStorage), sem SQL nem acesso direto a dados. As APIs e o site existem para servir a esse fluxo.

- Catálogo local de animes (DB_Local) com coleções internas (`MyAnime`), busca com normalização de caracteres especiais e consulta externa à ApiMyAnimeList.
- Catálogo musical (MyMusicX) com integração de busca externa ao Discogs.
- Armazenamento seguro de arquivos e monitoramento de pastas de coleção.
- Identidade própria (OIDC/OpenIddict), com contas locais, MFA/passkeys e recursos LGPD.

## Projetos da solução

A solução é `Dtudo2026.slnx` (backend .NET 10) mais `DtudoSite` (React 19 + Vite), tratados como um único produto full-stack.

| Projeto | Tipo | Papel | Portas locais (https/http) | Documento |
|---|---|---|---|---|
| **ApiIdentity** | ASP.NET Core + OpenIddict | Provedor OIDC, contas, papéis, permissões, segurança de conta, LGPD; banco próprio | 7243 / 5243 | [21_ApiIdentity.md](21_ApiIdentity.md) |
| **ApiMyAnimes** | ASP.NET Core + EF Core | Catálogo local (DB_Local) de animes e coleções `MyAnime`; rotas `/apiLocal/*` | 63980 / 63981 | [22_ApiMyAnimes.md](22_ApiMyAnimes.md) |
| **ApiMyAnimeList** | ASP.NET Core | Fonte **exclusiva** de busca, capas, detalhes e relações externas (API MyAnimeList) | 7146 | [23_ApiMyAnimeList.md](23_ApiMyAnimeList.md) |
| **ApiFileStorage** | ASP.NET Core (serviço interno) | Resolução segura de IDs lógicos de arquivos, reconciliação e monitoramento de pastas | 7244 / 5244 | [24_ApiFileStorage.md](24_ApiFileStorage.md) |
| **ApiMusicX** | ASP.NET Core + EF Core | Catálogo musical (coleções, artistas, lançamentos) | 63982 / 63983 | [25_ApiMusicX.md](25_ApiMusicX.md) |
| **ApiDiscogs** | ASP.NET Core | Consulta externa ao Discogs (artistas, lançamentos, masters) | 7147 / 7148 | [26_ApiDiscogs.md](26_ApiDiscogs.md) |
| **DtudoGateway** | ASP.NET Core + YARP | BFF do site: login OIDC (code + PKCE), sessão em cookie, proxy autenticado para as APIs | 51376 / 51377 | [20_DtudoGateway.md](20_DtudoGateway.md) |
| **LibDtudo.Shared** | Biblioteca .NET | Modelos, logging (Serilog + redação de dados sensíveis), correlação de requisições, validação de certificado de serviço | — | [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md) |
| **WinAppDtudo** | WinForms (net10.0-windows10.0.26100.0) | Aplicativo desktop (Dark Mode): importação, edição, painel de saúde; cliente OIDC público com PKCE | callback loopback `127.0.0.1:49173` | [11_WinAppDtudo.md](11_WinAppDtudo.md) |
| **DtudoSite** | React 19 + Vite 8 | Site (MyAnimes, Animes, Animex, MyMusicX, NinoTI, auth) | Vite 5173 | [10_DtudoSite.md](10_DtudoSite.md) |

Observações:

- `DtudoGateway` **não** referencia `LibDtudo.Shared`; as APIs e o WinApp sim.
- Todos os backends usam EF Core 10 onde há banco; o `ApiNode` foi removido (o proxy Discogs foi substituído por `ApiDiscogs` + `DtudoGateway`). Não recriar nem referenciar `ApiNode`.
- O Seq (logs estruturados) escuta em `5341` no Development.

### Projetos de teste

Existem nove projetos de teste na pasta `tests\` (agrupados na pasta de solução `/tests/` do `.slnx`): `ApiDiscogs.Tests`, `ApiFileStorage.Tests`, `ApiIdentity.Tests`, `ApiMusicX.Tests`, `ApiMyAnimeList.Tests`, `ApiMyAnimes.Tests`, `DtudoGateway.Tests`, `LibDtudo.Shared.Tests` e `WinAppDtudo.Tests`. A pasta `**/tests/` consta no `.gitignore`: hoje os testes são **somente locais** (nenhum arquivo versionado). Veja a divergência correspondente na seção de pendências.

## Topologia e portas

```text
Navegador ──► DtudoSite (Vite 5173) ──► DtudoGateway (BFF, 51376/51377)
                                              │  cookie de sessão (sem token no navegador)
                                              ├──► ApiIdentity  (OIDC: authorize/logout)
                                              ├──► ApiMyAnimes   /apiLocal/*     (catálogo)
                                              ├──► ApiMusicX     /apiLocal/*     (música, Bearer da sessão)
                                              └──► ApiDiscogs    /ApiDiscogs/*   (externo, Bearer da sessão)

WinAppDtudo ──(OIDC code + PKCE, navegador do sistema, loopback 49173)──► ApiIdentity
WinAppDtudo ──(Bearer)──► ApiMyAnimes / ApiMyAnimeList / ApiFileStorage (health e operações)
ApiMyAnimes ──(client_credentials + mTLS)──► ApiIdentity ──► token ──► ApiMyAnimeList ──► api.myanimelist.net
ApiDiscogs ──► api.discogs.com
Todas as APIs ──► JWT validado contra o emissor ApiIdentity
```

| Serviço | Health | Política |
|---|---|---|
| ApiIdentity | `/health/live`, `/health/ready` | — |
| ApiMyAnimes | `GET /apiLocal/Health` | `health.read` |
| ApiMyAnimeList | `/ApiMyAnimeList/health` | `health.read` |
| ApiMusicX | `/apiLocal/Health` | `health.read` |
| ApiDiscogs | `/ApiDiscogs/health` | `health.read` |
| ApiFileStorage | `/api/file-storage/health` (`/startup` anônimo) | `health.read` |
| DtudoGateway | `/health/live` | anônimo |

## Autenticação ponta a ponta

1. **Site → Gateway (BFF).** O navegador nunca recebe tokens. O gateway executa OIDC (authorization code + PKCE) com o client `dtudo-gateway`, redirect `/signin-oidc` e logout `/signout-callback-oidc`, escopos `openid profile catalog.read`. A sessão fica em ticket store no servidor, referenciada pelo cookie `__Host-dtudo-bff` (HttpOnly, Secure, SameSite Lax); inatividade 120 min, duração absoluta 24 h. CSRF: cookie `__Host-dtudo-xsrf` + cabeçalho `X-CSRF-TOKEN`. Chaves de Data Protection persistidas com DPAPI.
2. **Endpoints `/bff/*`.** `login` (com allowlist de redirecionamento), `antiforgery`, `logout` (GET/POST) e `me`. O gateway também faz proxy de `/identity/connect/authorize` e `/identity/connect/logout` para a ApiIdentity.
3. **Gateway → APIs.** Rotas YARP somente-GET sob `/api/catalog/*` (animes, coleções, música) e `/api/external/discogs/*`, todas exigindo usuário autenticado. Para música e Discogs o gateway injeta o Bearer `access_token` da sessão; nas rotas de anime o cabeçalho `Authorization` é removido. Rate limit de 60 req/60 s por IP fora do Development, corpo ≤ 1 MiB, CORS apenas para origens HTTPS exatas.
4. **ApiIdentity (OpenIddict).** Fluxos: authorization code (PKCE obrigatório), refresh token e client credentials. Access token de 300 s, refresh de 30 dias. Papéis `Superadministrador` e `Usuario do Site` mapeados para permissões (`catalog.read/write/delete`, `identity.provision`, `health.read`, `filesystem.command` etc.). Detalhes em [21_ApiIdentity.md](21_ApiIdentity.md).
5. **APIs.** Validam JWT (emissor HTTPS obrigatório, audiência própria, `ValidateOnStart`) e exigem a *permission* e o mesmo valor em `scope`/`scp`. Audiências: `urn:dtudo:api-my-animes`, `-api-my-animelist`, `-api-file-storage`, `-api-musicx`, `-api-discogs`.
6. **WinApp.** Cliente público `dtudo-winapp` com PKCE, navegador do sistema, callback loopback `http://127.0.0.1:49173/callback/` e armazenamento de tokens protegido por DPAPI. O WinApp não acessa SQL nem grava arquivos via ApiFileStorage (escreve a estrutura direto na pasta local escolhida pelo operador).
7. **Serviço → serviço.** `ApiMyAnimes` obtém token da ApiIdentity por `client_credentials` (client `api-my-animes`, escopo `service.mal.read`, recurso `urn:dtudo:api-my-animelist`) apresentando certificado de cliente (mTLS). Desabilitado por padrão (`ServiceAuthentication.Enabled=false`). Ver [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md).

## Ambientes

| Ambiente | Situação | Características |
|---|---|---|
| **Development** | Ativo | Perfis de lançamento padrão, LocalDB, loopback, Seq 5341, segredos em User Secrets, Swagger/OpenAPI somente aqui |
| **Homologation** | Não utilizado | Sem ambiente de homologação (a borda IIS/TLS foi removida); a linha de base de hardening ainda prevê o perfil |
| **Production** | Não iniciado | Somente o gateway em 443; portas internas 17080/17081/25341; contas de serviço dedicadas |

Linha de base de hardening: [02_Solucao_Operacao.md](02_Solucao_Operacao.md).

## Como rodar e compilar

| Objetivo | Comando / ação |
|---|---|
| Subir o ambiente de desenvolvimento | `npm run serv` (raiz) — delega para `npm --prefix DtudoSite run serv` |
| Perfil do Visual Studio | `IniciaTudo` (ApiIdentity, ApiMyAnimes, ApiMyAnimeList, ApiFileStorage e WinAppDtudo) |
| Compilar backend | `dotnet build Dtudo2026.slnx -c Release` |
| Testar backend | `dotnet test Dtudo2026.slnx` (requer `tests\` local) |
| Compilar/lint do site | `npm --prefix DtudoSite run build` / `npm --prefix DtudoSite run lint` |
| Segredos de desenvolvimento | `dotnet user-secrets` (ex.: `MyAnimeList:ClientId`, `ConnectionStrings:LocalDbConnection`) |

`npm run serv` usa `concurrently` para iniciar ApiIdentity, ApiMyAnimes, ApiMyAnimeList, ApiMusicX, o DtudoGateway (perfil `DtudoGateway`) e o Vite. Cada processo passa por `scripts\run-if-down.js <urlDeHealth> -- <comando>`, que só inicia o processo se o health correspondente estiver indisponível. **Não** inicia `ApiFileStorage` nem `ApiDiscogs` (iniciar manualmente quando necessário).

## Regras de arquitetura

- Reutilizar componentes, contextos, hooks, serviços e rotas existentes; preservar a arquitetura salvo pedido explícito de refatoração; modularizar e evitar duplicação de layout/lógica.
- A **ApiMyAnimeList é a fonte exclusiva** de busca, capas, detalhes e relações externas de anime; não criar novos fluxos para a API externa antiga.
- No DB_Local, `MyAnime` é uma coleção interna: todos os animes com o mesmo `MyAnimeId` formam o conjunto. Relações e navegação do detalhe local usam `MyAnimeId`/DB_Local, **nunca** as relações da ApiMyAnimeList.
- Busca local preserva a normalização de caracteres especiais e a prioridade: título principal, título em inglês, demais títulos alternativos.
- O navegador não guarda tokens (BFF); nenhuma rota de login/registro legada (`/apiLocal/Auth/*` retorna 404).
- Mudanças de endpoint/payload exigem revisar controller/serviço/modelo no backend **e** consumidores no frontend; mudanças de rota exigem revisar `DtudoRouter.jsx` e os providers.
- `WinAppDtudo` é inteiramente Dark Mode: reutilizar `ThemeManager`, `DarkModeColors` e os padrões escuros existentes; sem painéis claros.
- Fluxos de imagem devem ser validados em toda a aplicação.
- Ao refatorar controllers, preservar e corrigir comentários XML/Swagger.
- Nunca versionar segredos, strings de conexão ou thumbprints reais.

## Mapa de documentação

| Documento | Conteúdo |
|---|---|
| [00_Solucao.md](00_Solucao.md) | Este documento: arquitetura geral |
| [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md) | Modelo de ameaças, matriz de acesso, mTLS, rotação de segredos, estado do gate final |
PowerShell, scripts, CI/CD, backup, hardening, saúde, release MSIX |
| [10_DtudoSite.md](10_DtudoSite.md) | Site React |
| [11_WinAppDtudo.md](11_WinAppDtudo.md) | Aplicativo desktop |
| [20_DtudoGateway.md](20_DtudoGateway.md) | Gateway/BFF |
| [21_ApiIdentity.md](21_ApiIdentity.md) | Identidade e OIDC |
| [22_ApiMyAnimes.md](22_ApiMyAnimes.md) | Catálogo local de animes |
| [23_ApiMyAnimeList.md](23_ApiMyAnimeList.md) | Integração MyAnimeList |
| [24_ApiFileStorage.md](24_ApiFileStorage.md) | Armazenamento de arquivos |
| [25_ApiMusicX.md](25_ApiMusicX.md) | Catálogo musical |
| [26_ApiDiscogs.md](26_ApiDiscogs.md) | Integração Discogs |
| [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md) | Biblioteca compartilhada |

## Pendências/Divergências

- **Testes não versionados × CI:** `tests\` está no `.gitignore`, mas o `ci.yml` restaura/compila/testa `Dtudo2026.slnx`, que referencia esses projetos; em checkout limpo o CI tende a falhar na restauração ou não executar testes.
- **README desatualizado:** ainda menciona o "proxy Node legado" para busca Discogs (substituído por `ApiDiscogs` + `DtudoGateway`) e omite ApiIdentity/ApiMusicX na lista de health.
- **`npm run serv`** não inicia `ApiFileStorage` nem `ApiDiscogs`; confirmar se é intencional.
- **ApiFileStorage:** `FileStorage:Roots` vazio no `appsettings.json` base, mas a validação de inicialização exige ao menos uma raiz; a raiz deve ser configurada por ambiente.
- **Gateway:** não existe perfil "somente catálogo público"; todas as rotas de catálogo exigem sessão (ver [02_Solucao_Operacao.md](02_Solucao_Operacao.md)).
- Nomes herdados de etapas ainda aparecem em artefatos (`Test-DtudoEtapa29.ps1`, tarefa `Dtudo2026-Etapa06-Backup`, regras de firewall `Dtudo2026-Etapa07*`).
