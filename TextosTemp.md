
🎯 Próximas Ações Recomendadas para solução "Dtudo2026":

1. Adicionar logging centralizado (Serilog)
2. Docker Compose para orquestrar ambos os serviços
3. 
========================================================

Abaixo vou descrever minha SOLUÇÃO chamada "Dtudo2026" e seus projetos relacionados. 
Após as descrições, vou detalhar o que quero que seja feito na solução.
Caso você, I.A, não entenda algum termo ou conceito, por favor, me pergunte antes de prosseguir.

Minha SOLUÇÃO chamada "Dtudo2026", é a pasta raiz de todos os meus Projetos.
Front-End: DtudoSite, WinAppDtudo e Back-End: ApiMyAnimes, ApiJikan, LibDtudo.Shared. 
O projeto ApiNode (deve ser ignorado) está sendo gradativamente substituído pelos projetos ApiMyAnimes e ApiJikan.

Projeto LibDtudo.Shared - Biblioteca para compartilhar Dtos, Modelos, Utils... entre os meus projetos

Projeto ApiMyAnimes - Api Local MyAnimes (documentada com Swagger) - https://localhost:63980
Esta é uma Api Local que manipula (CRUD completo) um Banco de dados Relacional local que contém informações relacionadas as minhas coleções de animes.  
(/apiLocal/MyAnime) MyAnime (DBtabela) representa as coleções nomeadas que agrupam APENAS os IDs dos animes relacionados.
(/apiLocal/Anime) Anime (DBtabela) contém informações detalhadas sobre cada anime.

Projeto ApiJikan - Api Jikan Consulta Externa (documentada com Swagger) - https://localhost:63982
Esta é uma Api de consulta à API externa Jikan (MyAnimeList). Fornece endpoints para buscar (por nome ou ID) informações detalhadas sobre animes e seus relacionamentos.
ApiJikan  
GET/ApiJikan/search  
End-Point da minha Api Local que faz uma busca na Api externa Jikan, por nome do anime.
/ApiJikan/{id}  
Busca um anime específico por ID do MyAnimeList.
/ApiJikan/{id}/relations
Busca os animes relacionados a um anime específico pelo ID do MyAnimeList. Utiliza o endpoint dedicado /anime/{id}/relations da Jikan e retorna as imagens hidratadas de cada entrada.

Projeto WinAppDtudo - Aplicativo Desktop para consulta e manipulação de dados

PRIMEIRAMENTE, NÃO QUERO QUE LEIA TODA MINHA SOLUÇÃO, POIS ELA É GRANDE E COMPLEXA.
EU CRIEI TODOS OS PROJETOS, ME PERGUNTE SE PRECISAR DE ALGUMA INFORMAÇÃO SOBRE ALGUM PROJETO.

Agora neste meu projeto WinAppDtudo, no meu Form: C:\2026MeusProjetos\Dtudo2026\WinAppDtudo\Frm_WinAppDtudo.cs. Quero retirar

===================================================================================================


MyAnimes Central (\WinAppDtudo\Frm_MyAnimes.cs)
Através do nome Encontramos o Anime - (ApiJikan)
Exibimos seus detalhes e Animes Relacionados - (ApiJikan)

Com o Mal_id do Anime podemos fazer C.R.U.D, no DB_Local: 
MyAnime (tabela_db) - Coleção de animes, agrupados por nome - (ApiMyAnimes)
Anime (tabela_db) - Anime e seus detalhes - (ApiMyAnimes)

Com o registro do Anime no DB_Local, podemos:
Exibir seus detalhes e Animes Relacionados - (DtudoSite)
E através de um serviço local (será criado), poderemos salvar os dados (MyAnime) em forma de uma estrutura de pastas e arquivos.
```
Dragon Ball/                 (nome da pasta = myAnime.titulo)
|
├── 📁 1986 Dragon Ball - TV/     (ano de lançamento = myAnime.List<Anime>[0].ano + myAnime.List<Anime>[0].titulo)
│   ├── 54321.jpg               (nome do arquivo = myAnime.List<Anime>[0].id + .jpg)
├── 📁 1996 Dragon Ball Z - Filme/
│   ├── 54322.jpg 
```
=========================================================================

1. Fazer o relacionamento (de um para muitos) entre as tabelas MyAnime e Anime, ApiMyAnimes e LibDtudo.Shared.
1.1 No end point, POST/apiLocal/Anime: Modificar o código (e o modelo Anime, para ter uma Forenkey MyAnimeId),
Para quando for criar o ANIME, exigir (solicitar do usuario) o ID de um MyAnime (préviamente criada).

2. Na tabela MyAnime, Fazer o relacionamento do campo List<Anime> Animes, com a tabela Anime, onde cada Anime terá uma ForeignKey MyAnimeId, para que seja possível recuperar todos os animes relacionados a um MyAnime específico. 

3. Na tabela MyAnime, criar um campo (object) chamado PastaLocalMyAnimes, que depois será preenchido com dados (ano, nomeAnime, tipo, id) vindos do serviço local (será criado).

=========================================================================

Neste meu projeto WinAppDtudo, neste meu UC: C:\2026MeusProjetos\Dtudo2026\WinAppDtudo\_.cs,









======================================================================================================
Estou recebendo este (Agora mudou o aviso) aviso (Este projeto está definido para abrir o Designer WinForms no modo sem Reconhecimento de DPI.) ao abrir meus Forms em modo visual. Estou trabalhando (meu hardware) com uma tv 50" (escala 200%) com RESOLUÇÃO de 3840x2160. Pergunto se isso pode estar causando problemas visuais (por exemplo, itens (textos) dentro da aba animes detalhes estão se sobrepondo).
<ApplicationHighDpiMode>SystemAware</ApplicationHighDpiMode>
<ForceDesignerDpiUnaware>true</ForceDesignerDpiUnaware>
<ApplicationVisualStyles>true</ApplicationVisualStyles>
<ApplicationUseCompatibleTextRendering>false</ApplicationUseCompatibleTextRendering>
<ApplicationHighDpiMode>SystemAware</ApplicationHighDpiMode>
<ApplicationDefaultFont>Microsoft Sans Serif, 8.25pt</ApplicationDefaultFont>

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
├── 📖 CHECKLIST_MIGRACAO_DARK_MODE.md  ⭐ Checklist de migração
├── 📖 EXEMPLOS_DARK_MODE.cs       ⭐ Exemplos de código
│
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
