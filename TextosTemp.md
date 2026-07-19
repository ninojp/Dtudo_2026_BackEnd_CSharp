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

Projeto \ApiMyAnimeList (DEVE SER IGNORADO, no contexto atual da pergunta)


Projeto \ApiJikan (DEVE SER IGNORADO POIS NÃO ESTÁ MAIS EM USO)  

Neste meu projeto C:\2026MeusProjetos\Dtudo2026\WinAppDtudo, em \Controls\UC_AnimeCard.cs.
Quero fazer algumas modificações visuais, no Card temos, imagem, titulo, abaixo dele temos o titulo em Inglês.
Quando não existir titulo em inglês, quero que exiba um dos SINOMIMOS (se existir) do anime, caso não exista nenhum sinônimo, exibir o titulo em Japones, se não tiver nenhum deixe em branco.
os detalhes restantes do card, ano, tipo e score devem AGORA vir numa mesma linha, separados por icones.



Me pergunte se precisar de mais informações sobre o que já existe, ou se precisar de detalhes sobre o que deve ser implementado.  


PROBLEMAS:
Aqui está o site da documentação official da api MyAnimeList:  https://myanimelist.net/apiconfig/references/api/v2#section/Common-parameters  








em \FormsUC\FUC_MyAnimesDetalhes.cs.
Agora onde MOSTRA o ID, do MyAnime criado (e deixa-lo selecionavel para que eu possa copiar o id, se possível que já venha copiado (como se eu já tivese copiado o id))
Também preciso que este UC, se atualize (exiba os novos animes relacionados) automaticamente, pois quando eu adicionar um novo anime a esá coleção (MyAnime), preciso velo.
Agora antes de salvar Estrutura em Disco, tem que verificar se já existe uma pasta com mesmo nome antes, NÃO pode sobrescrever a pasta já existente. (apenas avise e interrompa a ação).







Quero uma implementação completa e ROBUSTA, com todos os detalhes necessários. Me pergunte se precisar de mais informações sobre o que já existe, ou se precisar de detalhes sobre o que deve ser implementado.

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
