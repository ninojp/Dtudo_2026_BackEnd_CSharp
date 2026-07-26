Abaixo vou descrever minha SOLUÇÃO (conjunto de projetos) chamada "Dtudo2026" e seus projetos internos relacionados. 

PRIMEIRAMENTE, NÃO QUERO QUE LEIA TODA MINHA SOLUÇÃO, POIS ELA É GRANDE E COMPLEXA.
EU CRIEI TODOS OS PROJETOS, ME PERGUNTE SE PRECISAR DE ALGUMA INFORMAÇÃO SOBRE ALGUM PROJETO.

Após as descrições, vou detalhar o que quero que seja feito.
Caso você, I.A, não entenda algum termo ou conceito, por favor, me pergunte antes de prosseguir.

Front-End: DtudoSite, WinAppDtudo e Back-End: ApiMyAnimes, ApiJikan, LibDtudo.Shared.

O projeto ApiNode (deve ser ignorado) está sendo gradativamente sendo substituído pelos projetos ApiMyAnimes e ApiJikan.

Projeto LibDtudo.Shared - Biblioteca para compartilhar Dtos, Modelos, Utils... entre os projetos dentro da solução Dtudo2026.

Projeto ApiMyAnimes - Api Local MyAnimes (CRUD completo, documentada com Swagger) - https://localhost:63980
Esta é uma Api Local que manipula Meu Banco de dados, Relacional (SQL Server) que contém minhas coleções, MyAnimes e seus Animes.  
(/apiLocal/MyAnime) MyAnime (tabela_db) representa as coleções nomeadas MyAnime por titulo e uma lista de IDs de animes relacionados.
(/apiLocal/Anime) Anime (tabela_db) contém informações detalhadas sobre cada anime.

Projeto ApiJikan - Api Jikan Consulta Externa (documentada com Swagger) - https://localhost:63982
Esta é uma Api de consulta à API externa Jikan (MyAnimeList). Fornece endpoints para buscar (por nome ou ID) informações detalhadas sobre animes e seus relacionamentos.
GET/ApiJikan/search  
End-Point da minha Api Local que faz uma busca na Api externa Jikan, por nome do anime.
/ApiJikan/{id}  
Busca um anime específico por ID do MyAnimeList.
/ApiJikan/{id}/relations
Busca os animes relacionados a um anime específico pelo ID do MyAnimeList. Utiliza o endpoint dedicado /anime/{id}/relations da Jikan e retorna as imagens hidratadas de cada entrada.

Projeto WinAppDtudo - Aplicativo Desktop para consulta, cadastro e manipulação de dados (Lê e grava no DB_Local e em disco local, pastas e arquivos).

-------------------------------------------------------------------------------------------------------------------------------------
Proximo passo:
DBLocalBuscar - É meu banco de dados, controlado pela ApiMyAnimes.
ApiMyAnimeList - Api Atual que fornece os dados e detalhes dos animes buscados por nome.



O Projeto, C:\2026MeusProjetos\Dtudo2026\ApiJikan (DEVE SER TOTALMENTE IGNORADO POIS NÃO ESTÁ MAIS EM USO)  
O Projeto, C:\2026MeusProjetos\Dtudo2026\ApiMyAnimeList (DEVE SER IGNORADO, no contexto atual da pergunta)
O Projeto, C:\2026MeusProjetos\Dtudo2026\ApiNode (PODE SER IGNORADO, lido apenas se necessário no contexto atual da pergunta)

Agora neste meu projeto C:\2026MeusProjetos\Dtudo2026\DtudoSite, na rota: http://localhost:5173/animes, temos uma página que exibe uma lista de animes (Cards) atualmente os dados dos animes estão vindo da api json-server: http://localhost:3666/animesDetalhes.  
Agora quero os dados dos animes venham da minha NOVA API: Projeto ApiMyAnimes - Api DB Local MyAnimes (CRUD completo, documentada com Swagger) - https://localhost:63980/apiLocal/. Quero que analize como os dados (já paginados) estão vindo da nova API e faça TODAS as alterações necessárias no front-end para que os dados dos animes venham da nova API, substituindo TOTALMENTE a antiga fonte de dados.  
Esta nova API retorna conteudo adulto (hentai), então vamos precisar de um filtro, Se a pessoa estiver logada (usar o metodo de login atual mesmo), devemos implementar (mostrar) na página um botão (pode ficar ao lado do filtro por Ano) "Hentai +18", que ao ser clicado, deve exibir SOMEMENTE os animes com conteúdo adulto (hentai). Caso a pessoa não esteja logada, o botão não deve aparecer e os animes com conteúdo adulto não devem ser exibidos.  

Quero uma implementação completa e ROBUSTA, com todos os detalhes necessários. Me pergunte se precisar de mais informações sobre o que já existe, ou se precisar de detalhes sobre o que deve ser implementado.






PROBLEMAS:
Aqui está o site da documentação official da api MyAnimeList:  https://myanimelist.net/apiconfig/references/api/v2#section/Common-parameters  
===================================================================================================

09/07/2026 NUMEROS DEPOIS DE POPULAR O DB LOCAL
SQL Server, new query:
```SQL
SELECT COUNT(*) AS Total
FROM Animes;
```
1064 MyAnimes(coleções) Adicionados
3815 Animes Adicionados
564 AmineXs Adicionados
4379 Total Adicionado





🎯 Próximas Ações Recomendadas para solução "Dtudo2026":

1. Adicionar logging centralizado (Serilog)
2. Docker Compose para orquestrar ambos os serviços
3. 

======================================================================================================
Estou recebendo este aviso (Este projeto está definido para abrir o Designer WinForms no modo sem Reconhecimento de DPI.)(Agora mudou o aviso) ao abrir meus Forms em modo visual. Estou trabalhando (meu hardware) com uma tv 50" (escala 200%) com RESOLUÇÃO de 3840x2160. Pergunto se isso pode estar causando problemas visuais (por exemplo, itens (textos) dentro da aba animes detalhes estão se sobrepondo).
<ApplicationHighDpiMode>SystemAware</ApplicationHighDpiMode>
<ForceDesignerDpiUnaware>true</ForceDesignerDpiUnaware>
<ApplicationVisualStyles>true</ApplicationVisualStyles>
<ApplicationUseCompatibleTextRendering>false</ApplicationUseCompatibleTextRendering>
<ApplicationHighDpiMode>SystemAware</ApplicationHighDpiMode>
<ApplicationDefaultFont>Microsoft Sans Serif, 8.25pt</ApplicationDefaultFont>

```
Dragon Ball/                        (usaremos este nome da pasta como myAnime.titulo)
|
├── 📁 1986 Dragon Ball - TV/   
│   ├── 54321.jpg               (usaremos os numeros como myAnime.List<Anime>54321.id)
├── 📁 1996 Dragon Ball Z - Filme/
│   ├── 54322.jpg 
```
```
WinAppDtudo/
│
├── 📁 Services/
│   ├── DarkModeColors.cs          ⭐ Paleta de cores do tema
│   ├── ThemeManager.cs            ⭐ Gerenciador centralizado
│   ├── ImageLoaderService.cs      (existente - sem mudanças)
│   ├── JikanApiService.cs         (existente - sem mudanças)
│   └── JikanModels.cs             (existente - sem mudanças)
│
├── 📁 Helpers/
│   └── FormHelpers.cs             ⭐ Classes base para formulários
│
├── 📁 Forms/
│   ├── Frm_WinAppDtudo.cs         ✏️ MODIFICADO - Tema aplicado
│   ├── Frm_Login.cs               ✏️ MODIFICADO - Exemplo
│   ├── Frm_CadastrarUsuario.cs    (próximo a migrar)
│   ├── Frm_MyAnimes.cs            (próximo a migrar)
│   └── ... (resto dos formulários)
│
├── 📁 FormsUC/
│   ├── FUC_BuscarPorID.cs         (próximo a migrar)
│   ├── FUC_DetalhesAnime.cs       (próximo a migrar)
│   └── ... (rest dos UserControls)
│
├── 📁 Controls/
│   ├── UC_AnimeCard.cs            (próximo a migrar)
│   └── UC_MiniAnimeCard.cs        (próximo a migrar)
│
├── 📄 Program.cs                  ✏️ MODIFICADO - Inicializa tema
│
├── 📖 DARK_MODE_GUIDE.md          ⭐ Documentação completa
├── 📖 SETUP_DARK_MODE_RESUMO.md   ⭐ Resumo rápido
└── ... (pasta de projeto)
```
#### Forms principais
- [ ] `Frm_CadastrarUsuario.cs`
- [ ] `Frm_FormTest.cs`
- [ ] `Frm_HelloWorld.cs`
- [ ] `Frm_MyAnimes.cs`
- [ ] `Frm_MyMusicX.cs`
- [ ] `Frm_Questao.cs`

#### UserControls
- [ ] `FUC_BuscarPorID.cs`
- [ ] `FUC_BuscarPorNome.cs`
- [ ] `FUC_CadastrarUsuario.cs`
- [ ] `FUC_DetalhesAnime.cs`
- [ ] `FUC_Mascaras.cs`
- [ ] `UC_AnimeCard.cs`
- [ ] `UC_MiniAnimeCard.cs`
