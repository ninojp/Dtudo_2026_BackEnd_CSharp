# PRIMEIRAMENTE, NÃO QUERO QUE LEIA TODA MINHA SOLUÇÃO, POIS ELA É GRANDE E COMPLEXA

## ANOTE, MARQUE, REGISTRE!!!!

EU SEMPRE EXECUTO TUDO VIA VS 2026!  
Estou DESENVOLVENDO e EXECUTANDO (modo Debug) TODA parte de back-end (C#) da minha solução via Visual Studio 2016, e apenas o front-end (React.js) é feito via VS Code (inclusive uso o Chat e agents I.A no VS Code, para auxiliar no desenvolvimento).
Minha SOLUÇÃO: C:\2026MeusProjetos\Dtudo2026\ (conjunto de projetos) chamada "Dtudo2026" atualmente é um projeto pessoal e que roda 100% local, mas futuramente (após o termino do básico, atual 50%) será disponibilizada para uso externo (internet), via site DtudoSite (deve apenas acessar as informações do banco de dados, via ApiMyAnimes).  

MEU PROJETO PRINCIPAL É WINAPPDTUDO (C:\2026MeusProjetos\Dtudo2026\WinAppDtudo\), ATRAVÉS DELE EU EXECUTO TODAS AS MINHAS AÇÕES  PRINCIPAIS RELACIONADAS À CONSULTA, CADASTRO, MANIPULAÇÃO DE DADOS E EXECUÇÃO DOS DEMAIS PROJETOS (DTUDOSITE).  
O ponto central é o projeto WinAppDtudo, que é o aplicativo desktop que manipula, faz consultas externas e DEVE CONTROLAR o Banco de dados local (via ApiMyAnimes) e arquivos em disco local (via ApiFileStore).

ESTOU TRABALHANDO LOCALMENTE, SOZINHO NO PROJETO.  
MEU PORJETO NÃO TEM VERSÃO DE PRODUÇÃO (DEPLOY) AINDA (FUTURAMENTE TERÁ).  
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

------------------------------------------------------------------------------------------------------------------

TROCAR TODOS OS MINI CARDS POR CARDS

Neste meu projeto C:\2026MeusProjetos\Dtudo2026\WinAppDtudo\
Após logar no WinAppDtudo, e acessar o Form MyAnimes, Clicando no menu "Buscar Anime", será aberta a aba unificada de busca, onde o usuário poderá escolher entre buscar no DB Local ou na ApiMyAnimeList. Após fazer uma busca, temos os Cards exibindo os resultados correspondentes à fonte selecionada, após clicar em um Card, será aberta a aba de detalhes do anime clicado (cada aba tem seu próprio UC, mas um layout semelhante).  

Neste meu projeto C:\2026MeusProjetos\Dtudo2026\WinAppDtudo\
Tem uma parte importante que faz a gestão das coleções locais, MyAnimes (coleção completa de animes relacionados entre si), ou seja analiza e monitora meus arquivos locais (estruturas de pastas e arquivos, neste computador local), garantindo que todas as alterações e adições sejam devidamente registradas e refletidas na coleção (banco de dados local).
Atualmente minha estrutura local (no sistema) estava organizada assim:
Disco 2 (E:) ANIMEs (E:\.Dots,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R)
Disco 0 (H:) ANIMEs2 (H:\(Animex\.Dots,A,B,C,D,E,F... todas as letras),S,T,U,V,X,W,Y,Z)

Agora adicionei um novo HD e preciso modificar (já modifiquei as pastas, já organizei as letras nos discos), no meu sistema, os novos endereços (apontamentos) das minhas coleções locais para incluir este novo disco e letras modificadas de lugar (HD). Agora estão assim:
Disco 2 (E:) ANIMEs (E:\.Dots,A,B,C,D,E,F,G,H,I,J,K,L)
Disco 0 (H:) ANIMEs2 (H:\M,N,O,P,Q,R,S,T,U,V,X,W,Y,Z)
Disco 1 (G:) ANIMEsTemp (G:\Animex\.Dots,A,B,C... todas as letras)

Na pasta Docs estão armazenados os arquivos de documentação do projeto, "Monitoramento das colecoes" (atualize este arquivo conforme necessário) locais e demais informações relevantes para o desenvolvimento e manutenção do sistema.

Na verdade não deveria ter um arquivo Script "Sync-MyAnimeMonitoringLocations.ps1", rodanda numa pasta separada, fora de um projeto C#!
Este código deveria estar dentro de uma estrutura de pastas dentro de uma Api (no meu  ver seria dentro da ApiFileStorage)

RAFAÇA TUDO QUE FOR NECESSÁRIO!

COMO SEMPRE! VOU REPETIR!
Quero uma implementação PROFISSIONAL, COMPLETA E ROBUSTA (mesmo que precise de mais tempo ou reformular grandes partes do projeto, me avise sobre as alterações necessárias neste caso).
Implemente ou modifique a documentação do projeto conforme necessário.
Se tiver qualquer duvida me pergunte antes de começar a implementar.


Ao clicar em um Card, abre uma nova ABA (UC) de detalhes do anime clicado, nessa Aba "detalhes do anime" temos o botão "Savar Como MyAnime" que ao ser clicado Salvaos dados da coleção MyAnime (MyAnime, representa a coleção completa de IDs ou seja TODOS os animes relacionados entre si).

https://myanimelist.net/anime/64008 NÃO APARECE A IMAGEM CAPA

==========================================================
ESTÁ É A PARTE MAIS IMPORTANTE DE TUDO QUE ESTOU CRIANDO, LOGO JAMAIS, DE FORMA ALGUMA DEVE SER FEITO QUALQUER TIPO DE ALTERAÇÃO OU MODIFICAÇÃO NAS MINHAS ESTRUTURAS DE DADOS LOCAIS, DEVE SER APENAS LEITURA E MONITORAMENTO.

Agora adicionei um novo HD e preciso modificar os endereços (apontamentos) das minhas coleções locais para incluir este novo disco. Agora as ficaram em apenas dois HDs.
Disco 2 (E:) ANIMEs (E:\.Dots,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R)
Disco 0 (H:) ANIMEs2 (H:\(Animex\.Dots,A,B,C,D,E,F... todas as letras),S,T,U,V,X,W,Y,Z)

===========================================================
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

O problema continua, o FormMyAnimes, abre no monitor padrão (monitor 2, 3840x2160, escala 250%), mas quando tento executar no monitor 3 (monitor 3, 1920x1080, escala 175%)

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
