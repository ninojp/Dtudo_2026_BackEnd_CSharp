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

### Janela principal com abas

O WinApp usa uma unica janela principal (`Frm_WinAppDtudo`). As funcionalidades abrem como abas do
`Tbc_Principal` (`DarkTabControl`), cada uma com seu proprio `UserControl`, em vez de janelas separadas.

- **Inicio** e uma aba de boas-vindas (`PinnedTabPage`, fundo preto, sem botao de fechar e nao arrastavel).
  Aparece somente enquanto nao ha outra aba aberta: abrir qualquer aba a remove e fechar a ultima aba
  (ou desconectar) a exibe novamente (`TabWorkspace.SetHomePage`). Contem o fundo e os atalhos
  (`FUC_Inicio`: DtudoSite, MyAnimes, MyMusicX e Nino T.I). Antes de conectar, as imagens ficam em
  preto e branco e nao podem ser clicadas; apos conectar os atalhos sao liberados e voltam a ser coloridos
  (`FUC_Inicio.DefinirAcessoLiberado`).
- **Janela maximizada** respeita a area de trabalho do monitor (nao cobre a barra de tarefas do
  Windows 11) e o clique no icone da barra de tarefas minimiza/restaura a janela
  (`CustomFormNoBorder`: estilos `WS_MINIMIZEBOX`/`WS_SYSMENU` e tratamento de `WM_GETMINMAXINFO`).
  Ao restaurar a janela minimizada, o estado e o tamanho anteriores voltam no mesmo monitor
  (enquanto minimizada, usa-se o ultimo monitor em que ela esteve visivel).
- **MyAnimes** e um menu com os submenus **Buscar Anime** e **Analizar Pastas**. Buscar Anime abre a
  aba de busca; Analizar Pastas executa a analise com progresso no rodape (`Ssb_StatusRodape`) e e
  bloqueada enquanto estiver em andamento. Detalhes, edicao, MyAnime (`My #id`) e estrutura de pastas
  tambem sao abas (lista plana, sem abas aninhadas), orquestradas por
  [`MyAnimesTabController`](WinAppDtudo/Services/MyAnimesTabController.cs).
- **Menu principal**: `Arquivo`, `MyAnimes`, `DtudoSite`, `MyMusicX` e `NinoTI`.
  - **Arquivo** (submenu): Monitoramento local, Cadastrar Usuario, Painel de Saude, Conectar, Desconectar e Sair.
  - **DtudoSite** (submenu): **DtudoWebView2**. O atalho DtudoSite da aba Inicio continua abrindo o Google Chrome.
- **Monitoramento local**, **Nino T.I**, **MyMusicX**, **Painel de Saude** e **Administracao de Usuarios**
  abrem como abas unicas (`FUC_Monitoramento`, `FUC_NinoTI`, `FUC_MyMusicX`, `FUC_PainelSaude`,
  `FUC_IdentityAdministration`); clicar de novo no menu reativa a aba existente. O Painel de Saude recebe o
  ultimo estado e as atualizacoes periodicas enquanto estiver aberto.
- **DtudoWebView2** abre cada pagina web em uma aba propria (`FUC_DtudoWebView`, WebView2 com Voltar, Avancar,
  Atualizar e Nova guia). O titulo da aba acompanha o titulo do documento; **Nova guia** e links que abrem
  nova janela criam outra aba da janela principal. Fechar a aba descarta o WebView2.
- O fechamento de abas fica somente no `X` da aba, no clique do botao do meio e no
  menu de contexto (botao direito): Fechar aba, Fechar outras abas, Fechar abas a direita, a esquerda
  e Fechar todas as abas (a aba Inicio nao possui comandos de fechamento).
- `TabWorkspace` ([`Controls/TabWorkspace.cs`](WinAppDtudo/Controls/TabWorkspace.cs)) e o helper
  reutilizavel: `OpenOrActivate(chave, fabrica)` reaproveita a aba pela chave (`Name`), e fechar uma aba
  descarta o controle e cancela suas operacoes. Novas funcionalidades devem usar esse helper.
- Continuam como dialogos modais (nao sao funcionalidades): importacao Discogs, progresso de operacao,
  confirmacao de desconexao (`Frm_Questao`) e login.

### Busca unificada de animes no WinApp

Na janela principal, o menu **MyAnimes > Buscar Anime** abre uma unica aba de busca. Clicar novamente no
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

#### Identidade visual e layout dos detalhes

Os detalhes compartilham o mesmo controle, com uma identidade por fonte:

| Fonte | Cor padrao |
| --- | --- |
| DB Local / ApiMyAnimes | Verde `#10B070`, reutilizando a paleta escura |
| ApiMyAnimeList | Azul VS Code `#007ACC` |

O cabecalho da aba exibe somente o icone da fonte, `#MalId` e o botao de fechar,
sem o prefixo visual `DB`. Icone, texto e fechamento usam a cor da fonte em
abas ativas e inativas; a borda da aba ativa tambem acompanha sua fonte.
A recoloracao dos icones preserva transparencia e contraste interno dos logos.
O nome acessivel identifica a fonte sem recolocar o prefixo no titulo visual. Os nomes internos
das abas continuam distinguindo as fontes, evitando misturar animes com o mesmo ID.
A largura do cabecalho acompanha o texto, a fonte, o icone, o fechamento e o DPI;
titulos maiores, como o da busca, nao ficam presos a uma largura unica.
Essa configuracao e idempotente: somente valores diferentes sao aplicados.
Ajustes que podem recriar o controle nativo sao adiados para fora de
`HandleCreated`, evitando reentrada e travamento ao abrir MyAnimes com o
Dark Mode nativo do Windows. As etapas de construcao do formulario tambem
sao registradas pelo mecanismo existente de diagnostico de inicializacao.

Clique e segure no cabecalho fora do `X`, arraste ate a posicao indicada e solte
para reorganizar as abas. O limite de movimento do Windows evita iniciar arraste
num clique simples. A movimentacao permanece dentro da mesma janela e preserva
os controles, a selecao e os dados carregados; o `X` e o menu de contexto de fechamento
continuam funcionando sobre a ordem atual.

Os dados do anime (titulos, estatisticas, valores e descricoes) seguem a cor da
fonte da origem, mesmo apos reaplicar o tema ou adicionar conteudo dinamico.
Legendas, titulos de secao, separadores e icones usam Gold. Os icones das
estatisticas têm tamanho uniforme de 9 pt; o texto ao lado continua com a cor da fonte.
Botoes, links e cards clicaveis mantem Gold. Fundos permanecem em Dark Mode.

Ha apenas uma rolagem vertical para a pagina inteira, quando necessaria.
Informacoes, estatisticas e cards relacionados nao possuem rolagem interna.
Os cards quebram em novas linhas sem limite artificial de altura; metadados
alternam entre duas e uma coluna, e textos longos ajustam a altura a largura
disponivel. Em janelas estreitas, a coluna de informacoes fica abaixo da capa
e das estatisticas. Redimensionar ou alternar abas reorganiza os mesmos
controles, sem baixar novamente as capas nem refazer a consulta.
As relacoes locais continuam usando exclusivamente DB_Local/MyAnimeId; as
externas permanecem na ApiMyAnimeList.

Nas duas fontes, a coluna esquerda e compacta e segue a mesma ordem: capa
reduzida (ate 240 x 340 px, escalados pelo DPI), botao principal, informacoes
(ano, tipo e score na mesma linha; episodios; duracao; generos) com pouco espaco
entre as linhas e botao final. No DB Local, os botoes sao **Exibir MyAnime** e
**Editar Anime**; na ApiMyAnimeList, **Salvar Como MyAnime** fica antes das
informacoes e **Salvar Anime** depois delas. A capa e centralizada, preserva a
proporcao da imagem carregada e o layout e refeito quando o download termina.
Enquanto a imagem carrega, o espaco de uma capa vertical fica reservado para
evitar saltos; se o anime nao tiver capa ou o download falhar, a capa e recolhida
e os botoes sobem para o topo da coluna. Linhas sem dado e o botao Exibir MyAnime,
quando o anime nao pertence a um MyAnime, nao deixam espacos vazios.
O limite da capa e a largura compartilhada com os botoes ficam nas constantes
`LarguraConteudoColuna` e `AlturaMaximaCapa` de
[`FUC_DetalhesAnime.Layout.cs`](WinAppDtudo/FormsUC/FUC_DetalhesAnime.Layout.cs).

Nas duas fontes, a coluna esquerda e estreita (300 px logicos) para dar espaco aos
cards relacionados. No topo, titulo principal e sinonimos ficam centralizados sobre
a capa e ingles/japones comecam no inicio da coluna direita; em janelas estreitas,
quando as colunas ficam empilhadas, os quatro titulos ficam centralizados.
Legendas como Mal ID: e Fonte:, titulos e mensagens de Animes relacionados, titulos
de secao e separadores usam o Gold padrao do WinAppDtudo. Como a captura mostrou
os emojis tintados pela cor dos dados, os icones usam o fallback Gold
(`IconTextLabel.IconColor` igual a `DarkModeColors.TextColor`) e tamanho uniforme de 9 pt;
titulos, valores e sinopse seguem a cor da fonte, e links continuam Gold. Os mini cards das duas fontes usam
moldura proporcional de poster, em grade alinhada a esquerda que preenche a coluna
(cards de 150 a 200 px logicos com 12 px de espaco). Eles posicionam tudo em pixels
do DPI atual e nao usam o autoescalonamento do WinForms, que fazia a imagem estourar
a moldura em DPI alto.

Durante uma consulta, ambos os botoes de busca, o input e a paginacao ficam bloqueados para
evitar concorrencia. Falhas de conexao, timeout e 504 mantem feedback explicito, preservam
o input e liberam os botoes novamente, sem trocar de fonte automaticamente. Fechar a aba
cancela sua consulta e descarta respostas tardias; reabrir cria uma nova aba vazia.

Testes focados de layout, fontes, navegacao e comportamento:

Os projetos de teste continuam somente locais, conforme a convencao de [`.gitignore`](.gitignore).

```powershell
dotnet test .\tests\WinAppDtudo.Tests\WinAppDtudo.Tests.csproj --no-restore --filter "FullyQualifiedName~AnimeSearchLayoutTests|FullyQualifiedName~AnimeSearchSourceTests|FullyQualifiedName~AnimeSearchNavigationTests|FullyQualifiedName~UnifiedAnimeSearchTests|FullyQualifiedName~ApiMyAnimesServiceTests"
dotnet test .\tests\WinAppDtudo.Tests\WinAppDtudo.Tests.csproj --no-restore --filter "FullyQualifiedName~AnimeDetailLayoutTests|FullyQualifiedName~DarkTabControlTests|FullyQualifiedName~MyAnimesFormStartupTests|FullyQualifiedName~TabWorkspaceTests|FullyQualifiedName~MainWindowTabsTests"
```

Os testes de detalhes verificam as duas fontes, persistencia das cores, icones
Gold e de tamanho uniforme, alinhamento do cabecalho em telas largas e estreitas, titulos longos,
campos opcionais, muitas relacoes, redimensionamento, abas ocultas, preservacao
das capas e ausencia de barras internas/horizontais. Tambem cobrem a coluna
coluna esquerda compartilhada (ordem capa, botao principal, informacoes compactas
e botao final), a capa proporcional, reservada ou recolhida e seu carregamento
assincrono, os cards em formato poster, a grade responsiva, a centralizacao do texto
selecionavel e o AutoScaleMode.None dos mini cards.
Os testes de abas verificam largura real do conteudo, icones coloridos, arraste,
limite de movimento, fechamento e preservacao dos controles e da selecao.
Os testes de inicializacao abrem e fecham a aba de busca repetidamente num
`DarkTabControl` com handle real e Dark Mode nativo, verificam callbacks da UI
e a estabilidade do handle apos reaplicacoes de tema e configuracao. Os testes de
`TabWorkspace` e da janela principal cobrem reaproveitamento por chave, aba Inicio fixa,
comandos de fechamento, selecao apos fechar, icones do cabecalho, submenus de MyAnimes e
os atalhos de `FUC_Inicio`.

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
