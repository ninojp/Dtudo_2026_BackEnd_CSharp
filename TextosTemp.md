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

Projeto WinAppDtudo - Aplicativo Desktop para consulta, cadastro e manipulação de dados (Lê e grava no DB_Local, pastas e arquivos).

-------------------------------------------------------------------------------------------------------------------------------------

Agora neste meu projeto WinAppDtudo, no meu arquivo UC: \WinAppDtudo\FUC_DetalhesAnime.cs (que está funcionando, e já exibe os detalhes do anime).
Agora quero implementar dois botões. Um, SALVAR COMO MYANIME, para salvar as informações (anime atual) na minha (\Dtudo2026\ApiMyAnimes -> https://localhost:63980/apiLocal/MyAnime) MyAnime (tabela_db) que representa as coleções nomeadas MyAnime por titulo, usaremos o titulo como MyAnime.Titulo e os IDs (mal_id) dos animes relacionados (exibidos no anime atual) devem ser colocados dentro da lista MyAnimes.AnimesMalId.
No segundo botão, SALVAR COMO ANIME, usando minha (\Dtudo2026\ApiMyAnimes -> https://localhost:63980/apiLocal/Anime) Anime (tabela_db), para salvar TODAS as informaçoes do anime atual.
Nesta mesma tabela, Anime vamos usar o campo, MyAnimeId, para fazer o relacionamento (de um para muitos) entre as tabelas MyAnime(id) e Anime(MyAnimeId).
Quando clicado no botão SALVAR COMO ANIME, exigir (solicitar do usuario) o ID de um MyAnime (préviamente criada).
Após inserir este ID (MyAnimeId), deve ser feito automaticamente a relação, adicionando na tabela MyAnime (verificar se já existe antes), na propriedade MyAnimes.AnimesMalId (que é uma lista de mal_id, dos animes relacionados entre sí, desta coleção MyAnime, agora relacionadas)
Quero uma implementação ROBUSTA e ATUALIZADA. Me pergunte o que for necessário.


Mas está com algumas falhas na parte visual, os textos (titulo, tipo, exibição) estão se sobrepondo e tem partes (exemplo: titulo da Aba, parte superior da imagem de capa) que estão transparentes e ao clicar, clica na janela que estiver a atrás, como se o meu aplicativo tive-se partes tranparentes.

===================================================================================================
🎯 Próximas Ações Recomendadas para solução "Dtudo2026":

0. Implementar o serviço local para salvar os dados (MyAnime) em forma de uma estrutura de pastas e arquivos.
1. Adicionar logging centralizado (Serilog)
2. Docker Compose para orquestrar ambos os serviços
3. 

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
