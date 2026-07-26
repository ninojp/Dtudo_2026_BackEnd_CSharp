# HTML CSS JavaScript React + Vite

Finalmente após anos de estudo e dedicação contínua. Agora vou começar a colocar em prática um projeto que já teve varias caras e dessa vez está ficando do jeito que quero e tenho capacidade de executar.

Depois eu crio uma descrição descente...

/api_backend
/public
/src
/index.html - NavBarLinks - Page      -  Page
inicia com -> MyAnimes    -> myanimes -> myanimes-detalhes
myanimes    - AnimeX      -> animex   -> animex-detalhes
myanimes    - NinoJP      - Page
myanimes    - MyMusicX    - Page

## Desenvolvimento Local

Stack atual:

- `ApiMyAnimes`: `https://localhost:63980`
- `ApiMyAnimeList`: `https://localhost:7146`
- `DtudoSite`: `http://localhost:5173`

Comando principal:

```powershell
npm run serv
```

Os scripts das APIs verificam os respectivos health checks antes de executar `dotnet run`.
Se uma API ja estiver aberta, por exemplo pelo Visual Studio ou pelo WinApp, o script reaproveita a instancia existente e evita erro de porta ocupada.

Scripts antigos baseados em `ApiNode` foram mantidos com prefixo `legacy:*`.

Health checks locais:

- `GET https://localhost:63980/apiLocal/Health`
- `GET https://localhost:7146/ApiMyAnimeList/health`

## Segredos Locais

O `ClientId` da MyAnimeList nao fica versionado. Configure com user-secrets:

```powershell
dotnet user-secrets set "MyAnimeList:ClientId" "SEU_CLIENT_ID" --project ApiMyAnimeList/ApiMyAnimeList.csproj
```

Ou use variavel de ambiente:

```powershell
$env:MyAnimeList__ClientId="SEU_CLIENT_ID"
```

## Configuracao Do WinApp

O WinApp le `WinAppDtudo/appsettings.json` e tambem aceita variaveis:

- `DTUDO_API_MYANIMES_BASE_URL`
- `DTUDO_API_MYANIMELIST_BASE_URL`
- `DTUDO_API_MYANIMELIST_AUTOSTART_URL`
- `DTUDO_ALLOW_INVALID_CERTIFICATES`

## Autenticacao Local

`ApiMyAnimes` expoe:

- `POST /apiLocal/Auth/register`
- `POST /apiLocal/Auth/login`
- `GET /apiLocal/Auth/me/{id}`

Usuarios locais ficam em `ApiMyAnimes/App_Data/*.json`, ignorado pelo Git. Senhas sao armazenadas com PBKDF2.
