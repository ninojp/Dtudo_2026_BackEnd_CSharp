# DtudoSite

O **DtudoSite** é o frontend web da solução Dtudo2026: uma SPA **React 19 + Vite (JavaScript)** com React Router, que consome exclusivamente o [DtudoGateway](20_DtudoGateway.md) (BFF/YARP). O login é OIDC via BFF — o navegador só carrega um cookie de sessão `HttpOnly`; nenhum token fica em JavaScript. Este documento é um **inventário fiel do código atual**, escrito para servir de base à futura migração para **Next.js + TypeScript** mantendo lógica e visual idênticos.

Relacionados: [00_Solucao.md](00_Solucao.md), [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md), [20_DtudoGateway.md](20_DtudoGateway.md), [21_ApiIdentity.md](21_ApiIdentity.md), [22_ApiMyAnimes.md](22_ApiMyAnimes.md), [23_ApiMyAnimeList.md](23_ApiMyAnimeList.md), [24_ApiFileStorage.md](24_ApiFileStorage.md), [25_ApiMusicX.md](25_ApiMusicX.md), [26_ApiDiscogs.md](26_ApiDiscogs.md), [11_WinAppDtudo.md](11_WinAppDtudo.md), [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md).

## 1. Stack e scripts

| Item | Valor |
|---|---|
| Pacote | `dtudo` 3.0.0 (ESM, `"type": "module"`) |
| Runtime | `react`/`react-dom` ^19.3, `react-router-dom` ^7.18 (`BrowserRouter`), `axios`, `react-icons` |
| Build/dev | `vite` ^8 + `@vitejs/plugin-react` ^6 com **`babel-plugin-react-compiler`** (React Compiler ativo) |
| Lint | `eslint` ^10 (`react-hooks`, `react-refresh`) |
| Tipagem | **Nenhuma** (sem TypeScript; `jsconfig.json` com `checkJs:false` e paths `@/*`, `@components/*`, `@context/*`) |
| Orquestração | `concurrently` |
| Porta dev | `http://localhost:5173/` |

Scripts (`package.json`):

| Script | Função |
|---|---|
| `dev` | `vite` |
| `dev:ensure` | `run-if-down` do Vite (só inicia se `http://localhost:5173/` estiver fora) |
| `api:identity` / `api:identity:run` | ApiIdentity (health `https://localhost:7243/health/live`, profile https) |
| `api:myanimes` | ApiMyAnimes (health `https://localhost:63980/apiLocal/Health`) |
| `api:myanimelist` | ApiMyAnimeList (health `https://localhost:7146/ApiMyAnimeList/health`) |
| `api:musicx` | ApiMusicX (health `https://localhost:63982/apiLocal/Health`) |
| `gateway:catalog` | DtudoGateway (health `https://localhost:51376/health/live`) |
| `serv` | `concurrently` de todos os acima + `dev:ensure` |
| `build` / `preview` / `lint` | Vite build, preview, ESLint |
| `test:musicx` / `test:discogs` | `node --test tests/apiMusicX.test.js` / `tests/apiDiscogs.test.js` |

`scripts\run-if-down.js` (na raiz do DtudoSite): verifica health HTTP(S) (`rejectUnauthorized:false`) ou porta TCP; **só executa o comando se o serviço estiver fora**, tornando `serv` idempotente. Timeout por `DTUDO_HEALTH_TIMEOUT_MS` (padrão 2500 ms). Não há proxy Node: o antigo proxy foi removido; tudo passa pelo Gateway.

## 2. Vite, aliases e proxy

`vite.config.js`:

- Aliases: `@dtudo-app` → `src/app/FullApp.jsx`; `@dtudo-anime-content` → `src/utils/animeContentUtils.js`. `jsconfig.json` define `@/*`, `@components/*`, `@context/*`.
- Header `X-Robots-Tag: noindex, nofollow, noarchive` em `server` e `preview`.
- **Proxy de dev** → `https://localhost:51376` (`secure:false`) para: `/api/catalog`, `/api/external`, `/bff`, `/identity`, `/signin-oidc`, `/signout-callback-oidc`. Assim o navegador fala sempre com a origem do Vite (`:5173`) e o gateway aparece como same-origin.

`index.html`: `lang="pt-br"`, `<meta name="robots" content="noindex">`, título "Dtudo", `<div id="root">`, `<script type="module" src="/src/main.jsx">`. `public\robots.txt` = `Disallow: /`.

## 3. Variáveis de ambiente e configuração

Não há `.env` versionado (`.env` e `.env.*.local` estão no `.gitignore`). Todas são opcionais (`VITE_*`, lidas via `import.meta.env`):

| Variável | Uso | Padrão |
|---|---|---|
| `VITE_BFF_BASE_URL` | Base do BFF (`bffClient`, `conectApiCatalog`) | `window.location.origin` (fallback SSR/testes `https://localhost:7120`) |
| `VITE_API_MUSICX_BASE_URL` | Base do catálogo musical | origem do BFF (fallback `https://localhost:51376`) |
| `VITE_API_MUSICX_PATH_PREFIX` | Prefixo MusicX | `/api/catalog/music` |
| `VITE_API_DISCOGS_BASE_URL` | Base do Discogs | origem do BFF |
| `VITE_API_DISCOGS_PATH_PREFIX` | Prefixo Discogs | `/api/external/discogs` |
| `DTUDO_HEALTH_TIMEOUT_MS` | Timeout do `run-if-down` (Node, fora do bundle) | 2500 |

Nenhum segredo é necessário no frontend; o client secret OIDC vive apenas no Gateway e na ApiIdentity.

### Configuração OIDC local

O site não fala com a ApiIdentity diretamente; o client OIDC é o **`dtudo-gateway`** (confidencial, code + PKCE). Para funcionar localmente:

1. O mesmo segredo forte (48 bytes aleatórios em Base64Url) deve ser definido em **User Secrets** de duas aplicações: `DtudoGateway` → `OpenIdConnect:ClientSecret` e `ApiIdentity` → `OpenIddict:Gateway:ClientSecret` (valor nunca versionado nem documentado). Os arquivos ficam em `%APPDATA%\Microsoft\UserSecrets\dtudo2026-gateway\` e `...\dtudo2026-apiidentity\`.
2. A ApiIdentity registra o client e os redirects (`https://localhost:51376/signin-oidc` e `/signout-callback-oidc`) no startup, **somente se** o segredo estiver definido; o Gateway falha no startup sem ele.
3. `npm run serv` sobe ApiIdentity, ApiMyAnimes, ApiMyAnimeList, ApiMusicX, Gateway e Vite (health checks idempotentes). O WinApp também pode gerenciar esses processos ([11_WinAppDtudo.md](11_WinAppDtudo.md)).
4. Fluxo: `http://localhost:5173/` → `/bff/login` (gateway) → `https://localhost:7243/account/login` (tema "site") → callback no gateway → retorno ao Vite.
5. Cookies `__Host-*` não separam por porta em `localhost`; use sempre a mesma origem para evitar sessões inesperadas.

## 4. Estrutura de `src\`

```
src/
  main.jsx            StrictMode + <FullApp/> (via alias @dtudo-app)
  main.css            Variáveis CSS globais, reset, tema
  api_conect/         conectApiCatalog.js (axios withCredentials)
  app/FullApp.jsx     AuthProvider > AnimesProvider > DtudoRouter
  components/         Componentes reutilizáveis (ver abaixo)
  context_api/        AnimesContext, AuthContext, MyMusicXDetalhesContext, MyMusicxObjsListContext
                      (cada um: <Nome>Context.js + <Nome>Provider.jsx)
  useAnimeStatus.js (status dos cards), useEpisodiosVistos.js (progresso de episódios)
  layouts/
  pages/              Animes, Login, Logout, MyMusicX, NinoTI, NotFound
  router/DtudoRouter.jsx
  services/           apiMyAnimes, apiMusicX, apiDiscogs, bffClient
  utils/animeContentUtils.js
public/               logos, mymusicx/, ninoti/, robots.txt
```

`src\assets` não existe: todos os assets estáticos estão em `public\` e são referenciados por caminho absoluto (`/mymusicx/...`).

### Componentes (`src\components\<Nome>\`)

Cada componente tem pasta própria com `.jsx` e, em geral, `.module.css`.

| Grupo | Componentes |
|---|---|
| Layout | `HeaderPage`, `NavBarPage`, `FooterPage`, `ScrollToTop` (**não montado em nenhum lugar**) |
| Texto/formulário | `H1TituloPage`, `H2SubTitulo`, `ParagrafoPage`, `LabelPadrao`, `InputPadrao`, `FieldsetPadrao`, `ButtonPadrao`, `CampoBuscar` |
| Navegação de listas | `Pagination`, `PaginationButtons`, `QtdExibirPorPage` |
| Filtros | `FiltrarPorAno`, `FiltrarPorGenero`, `FiltrarPorLetra`, `FiltrarPorTipo` (+ `tiposAnime.js`) |
| Feedback | `Spinner`, `ModalDialog` (+ `ModalDialog.css` global; adiciona `body.modal-open`) |
| Segurança | `ProtetorDeRota` |
`componentsAnimes/AnimeStatusIcons`, `componentsAnimes/EpisodiosVistos` |
| MyMusicX | `componentsMyMusicx/CardRelease`, `CardReleaseDetalhes`, `CardsMyMusicxList` |
| NinoTI | `componentsNinoTI/` — `areasTI` (logos por área), `AsideNinoTIPage`, `BadgesTI` e componentes de tecnologia: `HTML`, `CSS`, `JavaScript`, `TypeScript`, `NodeJS`, `React`, `Vite`, `NextJS`, `Git`, `GitHub`, `Figma`, `WordPress` |
| Ícones | `Icons` (componentes JSX + SVG próprios); demais ícones vêm de `react-icons` (`fa`, `fa6`, `md`) |

### Layouts

- `IndexLayout`: `NavBarPage` + `<Outlet/>` + `FooterPage`.
- `NinoTIPageLayout`: `AsideNinoTIPage` + `<Outlet/>`.

## 5. Rotas (`router\DtudoRouter.jsx`)

`BrowserRouter`; a raiz `/` usa `IndexLayout`. **Proteção** = envolvida por `ProtetorDeRota` (exige sessão do BFF).

| Caminho | Página/Componente | Proteção |
|---|---|---|
| `/` (index) | `Login` | Pública |
| `/auth/login` | `Login` | Pública |
| `/auth/logout` | `Logout` | Pública |
| `/animes` | `Animes` | **Login** |
| `/animes/animes-detalhes/:malId` | `AnimesDetalhes` | **Login** |
| `/animes/myanimes-colecao/:myAnimeId` | `AnimesRelacionados` (coleção MyAnime) | **Login** |
| `/ninoti` (layout `NinoTIPageLayout`) | index e `front-end` → `NinoTIFrontEnd` | Pública |
| `/ninoti/front-end/{html5,css3,javascript,typescript,nodejs,react,vite,nextjs,git,github,figma,wordpress}` | Páginas de tecnologia (filhos de FrontEnd) | Pública |
| `/ninoti/{programacao,cyber-security,blockchain,ia,hardware,os,ciencia-computacao,data-science,design-ux}` | Páginas de área de TI | Pública |
| `/mymusicx` (envolto em `MyMusicxObjsListProvider`) | index → `MyMusicX` | Pública no router* |
| `/mymusicx/mymusicx-buscar` | `MyMusicXBuscar` (Discogs) | Pública no router* |
| `/mymusicx/mymusicx-detalhes/:id` (envolto em `MyMusicXDetalhesProvider`) | `MyMusicXDetalhes` | Pública no router* |
| `*` | `NotFound` | Pública |

\* As rotas MyMusicX não usam `ProtetorDeRota`, mas **os endpoints do gateway exigem sessão**; sem login, as chamadas retornam 401/403 e o evento `dtudo:bff-session-expired` zera o usuário (ver §6). Não existem rotas de cadastro/registro.

`ProtetorDeRota`: usa `AuthContext`; enquanto `isLoading` mostra `Spinner`; se não autenticado, `navigate('/auth/login', { replace: true, state: { returnUrl: pathname + search } })`.

## 6. Autenticação OIDC/BFF no cliente

O que o cliente guarda: **somente em memória React** (`user`, `isLoading`, `isLoggingOut`, `error`) e um token antiforgery transitório. **Nada** em `localStorage`/`sessionStorage`; nenhum token OIDC.

Fluxo (`hooks\useAuth.js` + `services\bffClient.js`):

1. `AuthProvider` chama `refreshSession()` ao montar: `GET /bff/me` com `credentials: 'include'`. `200` → `user` = `{subject, name, email, roles}`; `401/403` → `user = null` sem erro; outros → mensagem de erro. Enquanto `isLoading`, o provider renderiza `Spinner` global.
2. `Login.jsx` não coleta credenciais. Se autenticado, navega ao `returnUrl` (padrão `/animes`); senão chama `login(returnUrl)` → `window.location.assign('/bff/login?returnUrl=…')`. `getSafeReturnPath` só aceita caminhos iniciados por `/`, rejeita `//` e `\`, e remove `#`.
3. O Gateway conduz o fluxo OIDC (code + PKCE) com a ApiIdentity, grava o cookie `__Host-dtudo-bff` (`HttpOnly; Secure; SameSite=Lax`) e redireciona de volta ao site.
4. Logout: `Logout.jsx` chama `logout('/')` → `GET /bff/antiforgery` (`{token}`) → `submitBffPostNavigation` cria um `<form method="post">` oculto com `__RequestVerificationToken` e submete para `/bff/logout?returnUrl=` (navegação top-level, precisa de DOM). O cookie antiforgery `__Host-dtudo-xsrf` é gerenciado pelo gateway.
5. Expiração: `requestBff` e os services MusicX/Discogs disparam `window.dispatchEvent(new Event('dtudo:bff-session-expired'))` em 401/403; `useAuth` escuta, zera `user` e define a mensagem.
6. `NavBarPage`: links Animes / NinoT.I / MyMusicX; ícone de login ou menu do usuário (nome ou e-mail; "Sair" → `/auth/logout`); fecha por `pointerdown` fora e `Escape` (usa `document`).

## 7. Contextos e providers

Todos seguem o padrão `<Nome>Context.js` (`createContext`) + `<Nome>Provider.jsx`. O consumo usa `use(Context)` (React 19) ou `useContext`.

| Contexto | Montado em | Valor | Observações |
|---|---|---|---|
| `AuthContext` | `FullApp` (raiz) | `user, isLoading, isLoggingOut, error, isAuthenticated, login, logout, refreshSession` | Sessão via `/bff/me`; spinner global |
| `AnimesContext` | `FullApp` (dentro de Auth) | `listObjsDetalhesAnimes, isLoading, error, recarregarAnimes` | Só carrega se autenticado; paginação `skip/take` de 500 até esgotar; limpa ao deslogar |
| `MyMusicxObjsListContext` | rota `/mymusicx` | `listObjsMyMusicx, isLoading, error, errorMessage, totalCount, fetchAllObjsMyMusicx` | Usa `listAllMusicCollections` |
| `MyMusicXDetalhesContext` | rota `mymusicx-detalhes/:id` | `myMusicXDetalhes, currentDisplayId, setCurrentDisplayId, isLoading, error, errorMessage, fetchCollectionDetails` | Lê `useParams().id`; usa `getMusicCollection` |

## 8. Serviços e endpoints consumidos

Todas as chamadas são **relativas ao BFF** (mesma origem em dev via proxy Vite) e exigem cookie de sessão (`withCredentials`/`credentials:'include'`). Não há chamada direta às APIs internas.

### `services\apiMyAnimes.js` (axios `axiosHttpBffCatalog` de `api_conect\conectApiCatalog.js`)

| Função | Endpoint | Notas |
|---|---|---|
| lista de animes | `GET /api/catalog/animes?skip&take` | Páginas de 500 |
| detalhe | `GET /api/catalog/animes/{malId}` | |
| busca | `GET /api/catalog/animes/search?termo&take=100` | |
| relacionados | `GET /api/catalog/animes/relacionados?ids=…` | Lotes de 200 ids; `paramsSerializer` com `indexes:null` |
| coleções | `GET /api/catalog/collections?skip&take` | |
| coleção por id | `GET /api/catalog/collections/{myAnimeId}` | |
`limparTokenCsrfCatalogo()` descarta o token |
| progresso de episódios (`buscarProgressoAnime`, `marcarEpisodiosVistos`, `desmarcarEpisodiosVistos`, `marcarTodosEpisodiosVistos`, `limparProgressoAnime`) | `GET /api/catalog/animes/{malId}/watch`; `PUT`/`DELETE .../watch/episodes` `{ episodes }`; `PUT .../watch/all`; `DELETE .../watch` | As mutações usam o mesmo helper CSRF (`enviarComCsrf`, com a repetição única em `400 invalid_csrf`) e aceitam corpo vazio. Lista vazia em marcar/desmarcar **não chama a API**. Exigem login feito após os scopes `personal.read`/`personal.write` (senão `403`). Falhas **não** derrubam a sessão na UI |

Backend: [22_ApiMyAnimes.md](22_ApiMyAnimes.md). A [ApiMyAnimeList](23_ApiMyAnimeList.md) **não** é consumida pelo site (o gateway não a expõe).

### `services\apiMusicX.js` (`fetch`, `credentials:'include'`)

Prefixo `/api/catalog/music`: `GET /collections?page&pageSize&search`, `/collections/{id}`, `/collections/{id}/releases`, `/artists?search…`, `/artists/{id}`, `/releases/{id}`. Erros 401/403/404/5xx mapeados para mensagens em português. Backend: [25_ApiMusicX.md](25_ApiMusicX.md).

### `services\apiDiscogs.js`

Prefixo `/api/external/discogs`: `GET /artists/search?q&page&perPage`, `/artists/{id}`, `/artists/{id}/releases?page&perPage&expand`, `/releases/{id}`, `/masters/{id}`. Mapeia 401/403/404/429/502/503/504; `getDiscogsImageUrl` só aceita URLs `https`. Backend: [26_ApiDiscogs.md](26_ApiDiscogs.md).

### `services\bffClient.js`

`getBffMe`/`refreshSession`, `getAntiforgeryToken`, `requestBff` (wrapper de `fetch` com `credentials:'include'` e evento de sessão expirada), `getSafeReturnPath`, `submitBffPostNavigation`, `buildBffUrl` (usa `VITE_BFF_BASE_URL`).

### Testes (Node)

`tests\apiDiscogs.test.js` (4 testes) e `tests\apiMusicX.test.js` (2 testes), via `node --test`. Não há testes de componentes/rotas.

## 9. Áreas de feature

### auth

Descrito em §6. Páginas `Login` e `Logout` (spinner, mensagem de erro e botão de nova tentativa).

### Animes / MyAnimes

- **`Animes`**: lista com busca (termo via API), filtros por gênero, letra, ano e tipo (padrão TV/OVA/ONA/MOVIE), paginação com limite 100 por página, `QtdExibirPorPage`. Conteúdo adulto: o toggle só aparece para usuário autenticado; adultos (`ehAnimeAdulto`) ficam ocultos quando o toggle está desligado.
- **`AnimesDetalhes` (`:malId`)**: busca o anime na lista em memória (`AnimesContext`) ou via `GET /animes/{malId}`; anime adulto sem login redireciona para `/animes`; mostra relacionados a partir de `animesRelacionadosIds`.
- **Progresso de episódios (`EpisodiosVistos` + `useEpisodiosVistos`, Fase 3):** seção no fim de `AnimesDetalhes` (`key` = `malId`, para não herdar estado entre animes), exibida **só com sessão autenticada**. Mostra uma grade de botões `1..total` com `aria-pressed` (marca/desmarca o episódio), "Marcar todos" e "Limpar", e o contador de vistos. Total desconhecido: aviso sóbrio e **sem** "Marcar todos" (a API também recusa com `409`). O hook carrega `GET .../watch`, **serializa** as operações (uma por vez, sem corrida entre cliques rápidos), mapeia erros HTTP para mensagens em português (`401` sessão expirada, `403` novo login para obter as permissões pessoais, `404` anime fora do banco local, `409` total desconhecido, `413`/`400` pedido inválido) e chama `invalidarAnimeStatus([malId])` após cada mudança. Estilo no padrão da página (`EpisodiosVistos.module.css`, tema escuro).
- **`AnimesRelacionados` (`:myAnimeId`)**: página da coleção MyAnime (animes do mesmo `MyAnimeId` no DB_Local). Usa `useAnimeStatus` com os `malId` dos animes visíveis e repassa `status` a cada `CardAnime`, de modo que os ícones de olho e pasta mostram o estado real (o hook é chamado antes dos retornos antecipados, respeitando as regras dos hooks).
- **`utils\animeContentUtils.js`**: normaliza campos snake_case/camelCase, tipos canônicos, ícones por tipo, score em pt-BR, datas e `obterAnimesRelacionados`.

#### Ícones de status no card (`AnimeStatusIcons` + `useAnimeStatus`)

- **`AnimeStatusIcons`** (`props`: `watchState`, `folderState`, `like`, `onPlay`) é renderizado dentro do `<figure>` de todo `CardAnime` (`figure` com `position: relative`, 180 px, igual à capa; também aparece no card "Imagem indisponivel" e nos cards de `AnimesRelacionados`). Quatro botões em coluna vertical, no meio da lateral esquerda, sobre a capa: olho (visto), pasta, polegar (gostei) e play. É desacoplado (sem contextos globais) para facilitar o porte para Next.js.
- Como o card fica dentro de um `<Link>`, todo clique nos ícones faz `preventDefault` + `stopPropagation` (botões `type="button"`) e não navega. Gostei não tem ação; play só chama `onPlay` se informado (sem ele, `title` "Em breve").
por ora sempre neutro (reservado; o contrato de `like` ainda não existe na API). O olho (`watch`) reflete o progresso real de episódios e a pasta (`folder`) reflete o último status de pasta gravado pela ApiMyAnimes (`none`/`partial`/`complete`); gostei e play continuam neutros.
- **`hooks\useAnimeStatus.js`**: `useAnimeStatus(malIds)` recebe os `malId` visíveis (normalizados: inteiros positivos, sem repetição) e devolve o mapa `malId -> { watch, folder, like }`, mantendo a forma da Fase 1 (`STATUS_ANIME_PADRAO`: `watch: 'none'`, `folder: 'none'`, `like: null`). **Fase 2 — ligado à API:**
  - só consulta com sessão autenticada (`AuthContext.isAuthenticated`); visitante anônimo ou sessão encerrada recebe o padrão e o cache é limpo;
  - **uma requisição por página/mudança de filtro** (o cliente `buscarStatusAnimes` divide em lotes sequenciais de até 200 ids, o limite do contrato), com debounce de 200 ms e `AbortController` (a requisição anterior é cancelada quando a lista muda ou o componente desmonta);
  - cache em memória por `malId`; `limparCacheAnimeStatus` e `invalidarAnimeStatus(malIds)` (chamada pelo `useEpisodiosVistos` após cada alteração de progresso, para o ícone do card atualizar) estão exportados;
  - **falha silenciosa**: qualquer erro (rede, 401/403/5xx, resposta inválida) mantém os ícones cinza, não quebra o card, não é guardado em cache e gera apenas um `console.error` discreto; não há retry automático (nova tentativa ao mudar a lista).
  `CardsAnimesList` o chama com os ids da página atual e repassa `status` a cada `CardAnime`; filtros, busca e paginação não mudam. `CardAnime` sem `status` usa o padrão. Pré-requisito de ambiente: login feito **após** o gateway com `offline_access`/recurso `api-my-animes` (sessões antigas devolvem 401 e ficam com ícones cinza até novo login).

### Animex

Não existe pasta ou rota `Animex` no código atual (referência apenas nominal em documentação anterior); o conteúdo de anime está integralmente em `Animes`. Ver Pendências.

### MyMusicX

- **`MyMusicX`**: lista local (coleções do catálogo) com busca, filtro por letra e ano, paginação (12–96 itens, padrão 48). Capas servidas de `public\mymusicx\<slug>.jpg` (slug = nome do artista em minúsculas, sem acentos, separado por `-`, ≈ 90 arquivos).
- **`MyMusicXDetalhes` (`:id`)**: detalhes da coleção e releases (`CardRelease`, `CardReleaseDetalhes`).
- **`MyMusicXBuscar`**: busca de artistas no Discogs com debounce de 250 ms, mínimo de 2 caracteres e `AbortController` para cancelar requisições; importa `/mymusicx/NotaMusica.png` como asset.

### NinoTI

Seção majoritariamente **estática**: `NinoTIPageLayout` (aside lateral), páginas por área (Programação, CyberSecurity, Blockchain, IA, Hardware, OS, Data Science, Design UX, Ciência da Computação) e por tecnologia de front-end, com `BadgesTI` e logos por área. Cores por área definidas em `main.css`. Imagens em `public\ninoti\` (`alura/`, `cursos/`, `TempImgs/`).

## 10. CSS, estilo e assets

- **CSS Modules** por componente/página (`*.module.css`), mais dois arquivos globais: `main.css` e `ModalDialog.css`.
- `main.css`: variáveis `:root` — gradientes (`--linearGradiente`, `--linearGradiente2`), cores por área de TI (`--corFrontEnd` `#4EB7E6`, `--corProgramacao` `#00C86F`, `--corCyberSecurity` `#FF4F54`, além de Blockchain, DataSciencia, IA, Hardware, Redes, OS, DesignEUX), `--pTextColor` `#646cff`, `--hTituloColor`, `--bordaPadrao`; reset `*`; `body` flex; `#root` com gradiente; classes utilitárias `.styleFrontEnd` etc.
- Modal: `ModalDialog` aplica a classe `body.modal-open` (bloqueio de scroll) definida em CSS global.
- Ícones: `react-icons` (`fa`, `fa6`, `md`) e SVGs em JSX próprios (`components\Icons`).
- Assets em `public\`: logos (`Logo_Dtudo_300p.png` e variantes), `mymusicx\`, `ninoti\`, formatos png/webp/gif/jpg, `robots.txt`.
- `lang="pt-br"` e `noindex` global (meta + header + robots).

## 11. Pontos de atenção para Next.js + TypeScript

| Tema | Situação atual | Cuidado na migração |
|---|---|---|
| Roteamento | `react-router-dom` com `BrowserRouter`, rotas aninhadas, `Outlet`, `useParams`, `navigate` com `state.returnUrl` | App Router: segmentos `animes/animes-detalhes/[malId]`, `animes/myanimes-colecao/[myAnimeId]`, `mymusicx/mymusicx-detalhes/[id]`, `ninoti/...`; grupo de rotas protegido; `returnUrl` por query ou estado |
| Proteção de rota | `ProtetorDeRota` no cliente | Pode virar `middleware`/layout de servidor consultando `/bff/me`; manter regra "MyMusicX pública no router, mas exige sessão nas chamadas" |
| Contextos | `use(Context)`, providers por rota | Providers viram `'use client'`; manter hierarquia Auth → Animes e providers MusicX por segmento |
| `window`/`document` | `useAuth` (eventos, `location.assign`), `bffClient` (`window.location.origin`, form oculto), `NavBarPage`, `ModalDialog`, `PaginationButtons` (`window.scrollTo`) | Só em client components/efeitos; evitar acesso em SSR |
| Env | `import.meta.env.VITE_*` | `process.env.NEXT_PUBLIC_*` (renomear) |
| Proxy/BFF | Proxy do Vite para `https://localhost:51376` em `/api/catalog`, `/api/external`, `/bff`, `/identity`, `/signin-oidc`, `/signout-callback-oidc` | `rewrites` em `next.config` ou implantar atrás do mesmo host do gateway; cookies `__Host-` exigem origem única, HTTPS e sem `Domain` |
| Cookies/CSRF | Cookie de sessão HttpOnly; antiforgery via `/bff/antiforgery` + POST de formulário no logout | Manter navegação top-level (não `fetch`) para login/logout; server components não devem repassar tokens |
| Aliases | `@dtudo-app`, `@dtudo-anime-content`, `@/*`, `@components/*`, `@context/*` | Mapear em `tsconfig.json` paths |
| Assets | `public\` e `import` de `/mymusicx/NotaMusica.png` estilo Vite | Usar caminho em `public` ou `next/image`; manter slugs de `mymusicx/<slug>.jpg` |
| CSS | CSS Modules + CSS global | Compatível; `main.css` em `app/layout`; `ModalDialog.css` global precisa ser importado em layout |
| React Compiler | Ativo via plugin Babel | Opção `experimental.reactCompiler` do Next |
| SEO | `noindex` global | Manter `robots` metadata e `X-Robots-Tag` |
| Dados | Catálogo inteiro de animes carregado no cliente (páginas de 500) | Candidato a paginação/consulta no servidor, preservando filtros e adultos |
| Testes | Apenas testes Node de services | Portar para o runner escolhido; adicionar testes de rotas |
| Tipagem | JS puro, formas inferidas (`snake`/`camel`) | Criar tipos para `Anime`, `MyAnime`, `MusicCollection`, `Release`, `BffUser` |

## 12. Pendências/Divergências

- `ScrollToTop` existe mas **não é montado** em lugar nenhum.
- Não há pasta/rota `Animex` no código, embora seja citada como área.
- O axios usa `withCredentials: true` e o gateway **exige sessão** em todas as rotas de catálogo (não há catálogo anônimo). O Login redireciona direto, sem aviso de maioridade.
- Fallbacks de porta inconsistentes em `bffClient` (`https://localhost:7120`) versus o restante (`https://localhost:51376`).
- Rotas MyMusicX não passam por `ProtetorDeRota` (proteção só ocorre via 401 do gateway).
- Recursos pessoais/LGPD da [ApiIdentity](21_ApiIdentity.md) (`/identity/me/*`: favoritos, listas, preferências, maioridade, termos, exportação/exclusão) **não são consumidos** pelo site nem expostos pelo gateway; a UI correspondente não existe.
- O frontend ainda não tem testes de componente/rota nem CI própria.
- **Progresso de episódios (Fase 3):** `EpisodiosVistos`, `useEpisodiosVistos` e as funções `watch` de `apiMyAnimes.js` não têm teste automatizado (mesma limitação do `import.meta.env`); a verificação foi lint + `npm run build`, e os contratos são cobertos pelos testes da ApiMyAnimes e do gateway. Exige novo login após a atualização dos scopes do gateway ([20_DtudoGateway.md](20_DtudoGateway.md)).
Pasta e gostei continuam neutros até as Fases 4–5 (o olho já usa o progresso real).
