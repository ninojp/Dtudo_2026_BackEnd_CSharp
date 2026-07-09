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

Erro ao buscar na ApiJikan:
Response status code does not indicate sucess: 504 (Gateway Timeout)

Proximo passo:
Minha Api Local (ApiJikan) está com serios problemas de timeout nos ultimos dias. Não quero removela, podemos deixala como está (para uso posterior, se necessário).
No momento quero implementar uma nova API, original MyAnimeList (já fiz o cadastro e tenho o: App Name e Client ID)





Agora neste meu projeto C:\2026MeusProjetos\Dtudo2026\WinAppDtudo, em: \FormsUC\FUC_DetalhesAnime.cs.
Quando estamos exibindo os detalhes do anime atual, tem um botão para salvar o ANIME atual como um MYANIME (coleção de animes, relacionados) no banco de dados local.
Ao criar um MYANIME, ele automaticamente já adiciona o anime atual (mal_id) na lista de animes relacionados a este MYANIME (corretamente).
Mas faltou criar este mesmo ANIME (agora na lista de animes relacionados) no banco de dados local. 
Pois todo ANIME que é usado (mal_id) para criar um MYANIME deve também ser criado como ANIME (logo após a criação do MYANIME, pois vai precisar do ID dele para cadastar um novo Anime (MyAnimeId=<ID_DO_MYANIME>)), com seus dados completos na TABELA ANIME do banco de dados local.
(já existe o serviço que cria o ANIME, mas ele pergunta ao usuário o MyAnimeId (que deve ser mantido), agora vamos fazer OUTRO serviço para criar o ANIME automaticamente (lembrei que no \Services\AnalizadorDeEstruturas.cs já faz algo semelhante também) após a criação do MYANIME (modularize e reaproveite o que der))
Me pergunte se precisar de mais informações sobre o que já existe, ou se precisar de detalhes sobre o que deve ser implementado.


SALVAR COMO MYANIME 



(a paginação também não está funcionando corretamente)

Quero uma implementação completa e ROBUSTA, com todos os detalhes necessários. Me pergunte se precisar de mais informações sobre o que já existe, ou se precisar de detalhes sobre o que deve ser implementado.



===================================================================================================
```
Dragon Ball/                        (usaremos este nome da pasta como myAnime.titulo)
|
├── 📁 1986 Dragon Ball - TV/   
│   ├── 54321.jpg               (usaremos os numeros como myAnime.List<Anime>54321.id)
├── 📁 1996 Dragon Ball Z - Filme/
│   ├── 54322.jpg 
```



🎯 Próximas Ações Recomendadas para solução "Dtudo2026":

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

======================================================================================================
Estou recebendo este aviso (Este projeto está definido para abrir o Designer WinForms no modo sem Reconhecimento de DPI.)(Agora mudou o aviso) ao abrir meus Forms em modo visual. Estou trabalhando (meu hardware) com uma tv 50" (escala 200%) com RESOLUÇÃO de 3840x2160. Pergunto se isso pode estar causando problemas visuais (por exemplo, itens (textos) dentro da aba animes detalhes estão se sobrepondo).
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
