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

- `ApiMusicX`: `C:\2026MeusProjetos\Dtudo2026\ApiMusicX`
- `ApiMusicX`: `https://localhost:63982`

- `DtudoGateway`: `https://localhost:51376`

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

## Inicializacao pelo Visual Studio

O perfil de varios projetos `IniciaTudo`, definido em `Dtudo2026.slnLaunch.user`, inicia o conjunto necessario para login, consulta local de animes, capas e exportacao segura:

- `ApiIdentity`
- `ApiMyAnimes`
- `ApiMyAnimeList`
- `ApiFileStorage`
- `WinAppDtudo`

`LibDtudo.Shared` e uma biblioteca referenciada e nao deve ser iniciada. Para trabalhar no fluxo de musicas, inicie tambem `ApiMusicX` e `ApiDiscogs`. `DtudoGateway` e `DtudoSite` sao necessarios somente para o site e podem ser iniciados quando esse fluxo for usado.

A ApiFileStorage deve ser iniciada pelo perfil durante a depuracao. Se o WinApp precisar inicia-la como fallback, ele passa a encerrar somente o processo que ele proprio criou ao fechar, evitando que um binario Debug antigo continue bloqueado.

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

### Busca unificada de animes no WinApp

Na janela MyAnimes, o menu **Buscar Anime** abre uma unica aba de busca. Clicar novamente no
menu seleciona a aba existente e preserva seu texto, resultados e consulta atual.

- **Busca DB Local** consulta somente a `ApiMyAnimes`, por titulo. Titulos numericos como
  `86` continuam sendo nomes, nao IDs. A normalizacao de caracteres especiais, a prioridade
  dos titulos e a ordenacao continuam no mecanismo local existente. O limite continua em
  100 resultados, com 20 animes por pagina.
- **Busca ApiMyAnimeList** consulta exclusivamente a `ApiMyAnimeList`. Nomes usam a busca
  paginada externa; uma entrada composta somente por digitos representa um ID entre
  1 e 100000, com resultado unico e sem paginacao.

Os dois botoes grandes ficam centralizados acima do label e do input, sem titulo adicional,
com 100 px de espacamento entre eles. O label mantem suas dimensoes; o campo mantem sua
fonte e altura, mas sua largura responsiva foi reduzida em 300 px em relacao a primeira
interface unificada, permanecendo centralizado. O texto digitado nunca e limpo ou reescrito, inclusive apos
validacao, ausencia de resultados ou erros. Apenas a consulta enviada tem espacos externos
removidos. Enter no input direciona o foco para os botoes, sem executar uma busca
automaticamente; a fonte deve ser escolhida explicitamente.

Cada nova busca com entrada valida substitui os cards exibidos, sem combinar resultados locais e
externos. O botao da fonte atual recebe destaque e o status identifica a origem. O status
mantem sua fonte e fica centralizado no rodape compacto, entre **Anterior** e **Proximo**,
no lugar do numero de paginas. Os botoes de paginacao medem 104 x 30 px e exibem texto
cinza-claro quando inativos; as medidas sao escaladas automaticamente pelo DPI do Windows.
A navegacao usa a fonte e o texto da consulta enviada, mesmo se o
input for editado depois; uma nova busca sempre comeca na primeira pagina.

As consultas permanecem em adaptadores separados, reutilizando os servicos HTTP existentes.
Cards locais usam suas imagens locais e abrem detalhes/colecoes pelo DB_Local e MyAnimeId,
sem fallback para relacoes ou capas externas. Cards externos usam as imagens e detalhes da
ApiMyAnimeList. Um mesmo MalId encontrado nas duas fontes continua abrindo detalhes distintos.

Durante uma consulta, ambos os botoes de busca, o input e a paginacao ficam bloqueados para
evitar concorrencia. Falhas de conexao, timeout e 504 mantem feedback explicito, preservam
o input e liberam os botoes novamente, sem trocar de fonte automaticamente. Fechar a aba
cancela sua consulta e descarta respostas tardias; reabrir cria uma nova aba vazia.

Testes focados de layout, fontes, navegacao e comportamento:

Os projetos de teste continuam somente locais, conforme a convencao de [`.gitignore`](.gitignore).

```powershell
dotnet test .\tests\WinAppDtudo.Tests\WinAppDtudo.Tests.csproj --no-restore --filter "FullyQualifiedName~AnimeSearchLayoutTests|FullyQualifiedName~AnimeSearchSourceTests|FullyQualifiedName~AnimeSearchNavigationTests|FullyQualifiedName~UnifiedAnimeSearchTests|FullyQualifiedName~ApiMyAnimesServiceTests"
```

## MyMusicX no DtudoSite

As consultas locais de Colecoes, artistas, releases e faixas usam a fachada autenticada do `DtudoGateway`:

- `VITE_API_MUSICX_BASE_URL`: origem configuravel da fachada; por padrao usa `VITE_BFF_BASE_URL` ou a origem atual do site.
- `VITE_API_MUSICX_PATH_PREFIX`: prefixo configuravel; por padrao, `/api/catalog/music`.

O gateway encaminha somente requisicoes GET para a `ApiMusicX` e repassa o token no servidor com `catalog.read`; o React nao recebe nem envia bearer token. O proxy Node legado permanece apenas para a rota de busca externa Discogs enquanto a migracao da Fase 2 nao for concluida.

## Identidade e autenticacao

- `ApiIdentity` e o proprietario de contas, provisionamento, MFA, sessoes, tokens e revogacao.
- `DtudoGateway` atende o site com OIDC Code + PKCE e sessao por cookie server-side; o React nao recebe tokens.
- `WinAppDtudo` usa o navegador do sistema com PKCE, callback loopback e armazenamento local protegido por DPAPI.
- `ApiMyAnimes` nao expoe mais cadastro, login ou consulta de usuarios locais; o catalogo usa as politicas e identidades da arquitetura nova.

Finalmente após anos de estudo e dedicação contínua. Agora vou começar a colocar em prática um projeto que já teve varias caras e dessa vez está ficando do jeito que quero e tenho capacidade de executar.  
Depois eu crio uma descrição descente...
