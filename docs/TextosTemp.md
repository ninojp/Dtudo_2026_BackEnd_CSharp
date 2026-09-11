# PRIMEIRAMENTE, NÃO QUERO QUE LEIA TODA MINHA SOLUÇÃO, POIS ELA É GRANDE E COMPLEXA

Estou DESENVOLVENDO e EXECUTANDO (modo Debug) TODA parte de back-end (C#) da minha solução via Visual Studio 2016, e apenas o front-end (React.js) é feito via VS Code (inclusive uso o Chat e agents I.A no VS Code, para auxiliar no desenvolvimento).
Minha SOLUÇÃO: C:\2026MeusProjetos\Dtudo2026\ (conjunto de projetos) chamada "Dtudo2026" atualmente é um projeto pessoal e que roda 100% local, mas futuramente (após o termino do básico, atual 50%) será disponibilizada para uso externo (internet), via site DtudoSite (deve apenas acessar as informações do banco de dados, via ApiMyAnimes).  
O ponto central é o projeto WinAppDtudo, que é o aplicativo desktop que manipula, faz consultas externas e DEVE CONTROLAR o Banco de dados local (via ApiMyAnimes) e arquivos em disco local (via ApiFileStore).  
Lembrando que este meu projeto (é pessoal e apenas eu trabalho nele, portanto não necessita de controle de versão avançado ou integração contínua) está em desenvolvimento e não tem uma versão de produção/deployment ainda.

Projeto LibDtudo.Shared - Biblioteca para compartilhar Dtos, Modelos, Utils... entre os projetos dentro da solução Dtudo2026.

Projeto ApiMyAnimes - Api Local MyAnimes (CRUD completo, documentada com Swagger) - <https://localhost:63980>
Esta é uma Api Local que manipula Meu Banco de dados, Relacional (SQL Server) que contém minhas coleções, MyAnimes e seus Animes relacionados.  
(/apiLocal/MyAnime) MyAnime (tabela_db) representa as coleções completas, nomeadas MyAnime por titulo e uma lista de IDs de animes relacionados.
(/apiLocal/Anime) Anime (tabela_db) contém informações detalhadas sobre cada anime.

Projeto ApiMyAnimeList - Api local ApiMyAnimeList - <https://localhost:7146>
Esta é uma Api de consulta à API externa, Oficial MyAnimeList. Fornece endpoints para buscar (por nome ou ID) informações detalhadas sobre animes e seus relacionamentos.
GET/ApiMyAnimeList/search  
End-Point da minha Api Local que faz uma busca na Api externa ApiMyAnimeList, por nome do anime.
/ApiMyAnimeList/{id}  
Busca um anime específico por ID do MyAnimeList.
/ApiMyAnimeList/{id}/relations
Busca os animes relacionados a um anime específico pelo ID do MyAnimeList. Utiliza o endpoint dedicado /anime/{id}/relations da ApiMyAnimeList e retorna as imagens hidratadas de cada entrada.

Projeto WinAppDtudo - Aplicativo Desktop para consulta, cadastro e manipulação de dados (Lê e grava no DB_Local e em disco local, pastas e arquivos).

Projeto ApiDiscogs - Api para consulta externa de informações sobre músicas, artistas e álbuns.
Projeto ApiMyMusicX - Api para gestão do Banco de dados local de músicas, artistas e álbuns. (CRUD completo, documentada com Swagger).

Atualmente (03/09/2026) foram adicionados (através de I.A) diversos novos projetos e funcionalidades, ApiIdentity, ApiFileStorage, ApiDiscogs, ApiMusicX, DtudoGateway e as Apis de Testes.

ANOTE, MARQUE, REGISTRE!!!!
EU SEMPRE EXECUTO TUDO VIA VS 2026!
ESTOU TRABALHANDO LOCALMENTE, SOZINHO NO PROJETO.
MEU PORJETO NÃO TEM VERSÃO DE PRODUÇÃO (DEPLOY) AINDA (FUTURAMENTE TERÁ).
MEU PROJETO PRINCIPAL É WINAPPDTUDO (C:\2026MeusProjetos\Dtudo2026\WinAppDtudo\), ATRAVÉS DELE EU EXECUTO TODAS AS MINHAS AÇÕES PRINCIPAIS RELACIONADAS À CONSULTA, CADASTRO, MANIPULAÇÃO DE DADOS E EXECUÇÃO DOS DEMAIS PROJETOS (DTUDOSITE).

------------------------------------------------------------------------------------------------------------------



Neste meu projeto C:\2026MeusProjetos\Dtudo2026\WinAppDtudo\
Após logar no WinAppDtudo, e acessar a Form MyAnimes, na aba "Busca Externa - ApiMyAnimeList", depois de digitar o nome de um anime e clicar no botão "Buscar", a ApiMyAnimeList é chamada, e retorna os resultados (Cards) da busca. Ao clicar em um Card, abre uma nova ABA (UC) de detalhes do anime clicado, nessa Aba "detalhes do anime" temos o botão "Salvar Como MyAnime" que ao ser clicado deve salvar (via ApiMyAnimes) o anime atual como MyAnime (MyAnime, representa a coleção completa a qual o anime vai pertencer, deve salvar nome e IDs dos animes relacionados ao atual), também deve salvar este mesmo anime como um Anime (com todos os detalhes do anime atual e SEUS ANIMES RELACIONADOS A ELE).

Descobri OUTRA anomalia ao salvar animes relacionados ao anime atual, cada anime tem seus próprios animes relacionados a ele, que atualmente não estão sendo salvos corretamente em cada anime na tabela Animes.
Corrija isso.

Testei e continua sem salvar corretamente os animes relacionados dos animes relacionados ao anime atual.
vou colocar um exemplo detalhado de como os animes relacionados deveriam ser salvos corretamente:  
MyAnime id 2494, Cardcaptor Sakura: mal-id 232  
tem 4 animes relacionados diretamente a ele: [371, 2424, 372, 35320]  
anime 371: [232]  
anime 2424: [232]  
anime 372: [232, 1078, 33354, 35320] aqui não salvou o 1078  
anime 35320: [232, 372, 33354] aqui não salvou o 33354  
O salvamento pode ser feito em etapas e de forma lenta (tempo não é problema)  
Se possivel gostaria que o salvamento fosse feito de forma recursiva, garantindo que todos os animes relacionados, e os animes relacionados a eles, fossem salvos corretamente. E se possivel, poderia salvar em mais um nivel, ou seja, Anime 372 [232, 1078, 33354, 35320] salvar também os animes relacionados a cada um desses animes (apenas neste terceiro nivel e não além dele).
Gostaria de exbir status do processo e relatorio de erros caso ocorra.
Me questione se não entendeu algo.

COMO SEMPRE! VOU REPETIR!
Quero uma implementação (correção, modificação) PROFISSIONAL, COMPLETA E ROBUSTA.
Se tiver qualquer duvida me pergunte antes de começar a implementar.

As proximas etapas serão, gerenciamento, monitoramento e estatisticas das minhas estruturas de dados locais (via ApiFileStorage e WinAppDtudo para exibição das estruturas salvas e seus conteúdos e suas estatisticas).
 
==========================================================================================================
TODOS OS TEXTOS ABAIXO SÃO APENAS PARA USO PESSOAL E NÃO DEVEM SER USADOS POR NENHUMA I.A OU AGENTE DE I.A

Documentação official da api MyAnimeList:  <https://myanimelist.net/apiconfig/references/api/v2#section/Common-parameters>  
C:\Users\comer\AppData\Local\Dtudo2026\ApiFileStorage\media\my-animes
O CÓDIGO ABAIXO NÃO É SEGREDO E PODE SER EXIBIDO NO GITHUB SEM PROBLEMAS.
eumemosem@nadaSENHAatual123@)NinoJPDtudoDev!2026LfZDp9ftZLgbpd1f

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
5.571 animes no catálogo completo.
1.358 animes reconhecidos pelo filtro Hentai +18.

======================================================================================================
🎯 Próximas Ações Recomendadas para solução "Dtudo2026":

1. Adicionar logging centralizado (Serilog)
2. Docker Compose para orquestrar ambos os serviços
3.

Estou recebendo este aviso (Este projeto está definido para abrir o Designer WinForms no modo sem Reconhecimento de DPI.)
recebo o aviso: A escala na tela principal está definida como 200%. Considere abrir o WinForm Designer no modo DPI-Unaware.
Estou trabalhando (meu hardware) com uma tv 50" (escala 200%) com RESOLUÇÃO de 3840x2160. Pergunto se isso pode estar causando problemas visuais (por exemplo, itens (textos) dentro da aba animes detalhes estão se sobrepondo).

```csharp
<ApplicationHighDpiMode>SystemAware</ApplicationHighDpiMode>
<ForceDesignerDpiUnaware>true</ForceDesignerDpiUnaware>
<ApplicationVisualStyles>true</ApplicationVisualStyles>
<ApplicationUseCompatibleTextRendering>false</ApplicationUseCompatibleTextRendering>
<ApplicationDefaultFont>Microsoft Sans Serif, 8.25pt</ApplicationDefaultFont>
```

```prompt
Dragon Ball/                        (usaremos este nome da pasta como myAnime.titulo)
|
├── 📁 1986 Dragon Ball - TV/   
│   ├── 54321.jpg               (usaremos os numeros como myAnime.List<Anime>54321.id)
├── 📁 1996 Dragon Ball Z - Filme/
│   ├── 54322.jpg 
```
