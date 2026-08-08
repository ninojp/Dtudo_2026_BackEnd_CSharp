# Projeto Solo, Dtudo: Animes, Musicas, Material sobre T.I

## Desenvolvimento Local - C#, SQL, HTML, CSS, JavaScript, React + Vite

Stack atual:

- `DtudoSite`: `C:\2026MeusProjetos\Dtudo2026\DtudoSite`
- `DtudoSite`: `http://localhost:5173`

- `WinAppDtudo`: `C:\2026MeusProjetos\Dtudo2026\WinAppDtudo`

- `ApiMyAnimes`: `C:\2026MeusProjetos\Dtudo2026\ApiMyAnimes`
- `ApiMyAnimes`: `https://localhost:63980`

- `ApiMyAnimeList`: `C:\2026MeusProjetos\Dtudo2026\ApiMyAnimeList`
- `ApiMyAnimeList`: `https://localhost:7146`

- `LibDtudo.Shared`: `C:\2026MeusProjetos\Dtudo2026\LibDtudo.Shared`

Comando principal, para iniciar a solução localmente (a partir da raiz do repositorio):

```powershell
Set-Location .\DtudoSite
npm run serv
```

Tambem e possivel executar `npm run serv` diretamente na raiz; o wrapper local encaminha o comando para `DtudoSite`.

Os scripts das APIs verificam os respectivos health checks antes de executar `dotnet run`.
Se uma API ja estiver aberta, por exemplo pelo Visual Studio ou pelo WinApp, o script reaproveita a instancia existente e evita erro de porta ocupada.

Scripts antigos baseados em `ApiNode` foram mantidos com prefixo `legacy:*`.

Health checks locais:

- `GET https://localhost:63980/apiLocal/Health`
- `GET https://localhost:7146/ApiMyAnimeList/health`
- `GET https://localhost:51376/health/live` (gateway catalog-only)

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

## Identidade e autenticacao

- `ApiIdentity` e o proprietario de contas, provisionamento, MFA, sessoes, tokens e revogacao.
- `DtudoGateway` atende o site com OIDC Code + PKCE e sessao por cookie server-side; o React nao recebe tokens.
- `WinAppDtudo` usa o navegador do sistema com PKCE, callback loopback e armazenamento local protegido por DPAPI.
- `ApiMyAnimes` nao expoe mais cadastro, login ou consulta de usuarios locais; o catalogo usa as politicas e identidades da arquitetura nova.

Finalmente após anos de estudo e dedicação contínua. Agora vou começar a colocar em prática um projeto que já teve varias caras e dessa vez está ficando do jeito que quero e tenho capacidade de executar.  
Depois eu crio uma descrição descente...
