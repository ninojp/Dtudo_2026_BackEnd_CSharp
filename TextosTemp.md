# PRIMEIRAMENTE, NÃO QUERO QUE LEIA TODA MINHA SOLUÇÃO, POIS ELA É GRANDE E COMPLEXA

Abaixo vou descrever minha SOLUÇÃO: C:\2026MeusProjetos\Dtudo2026\ (conjunto de projetos) chamada "Dtudo2026" e seus projetos internos relacionados

O projeto ApiNode (deve ser ignorado) está sendo gradativamente sendo substituído pelos projetos ApiMyAnimes e ApiMyAnimeList.

Projeto LibDtudo.Shared - Biblioteca para compartilhar Dtos, Modelos, Utils... entre os projetos dentro da solução Dtudo2026.

Projeto ApiMyAnimes - Api Local MyAnimes (CRUD completo, documentada com Swagger) - <https://localhost:63980>
Esta é uma Api Local que manipula Meu Banco de dados, Relacional (SQL Server) que contém minhas coleções, MyAnimes e seus Animes relacionados.  
(/apiLocal/MyAnime) MyAnime (tabela_db) representa as coleções nomeadas MyAnime por titulo e uma lista de IDs de animes relacionados.
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

Projetos FUTUROS, projetos que serão criados futuramente, mas que já estão planejados:
Projeto ApiDiscogs - Api para consulta externa de informações sobre músicas, artistas e álbuns.
Projeto ApiMyMusicX - Api para gestão do Banco de dados local de músicas, artistas e álbuns. (CRUD completo, documentada com Swagger).

Minha solução (conjunto de projetos) atualmente é projeto pessoal e que roda 100% local, mas futuramente será disponibilizada para uso externo (internet), via site DtudoSite (deve apenas acessar as informações do banco de dados, via ApiMyAnimes).
O ponto central é o projeto WinAppDtudo, que é o aplicativo desktop que manipula, faz consultas externas e DEVE CONTROLAR o Banco de dados local e arquivos em disco local.

-------------------------------------------------------------------------------------------------------------------------------------
C:\Users\comer\AppData\Roaming\Microsoft\UserSecrets
`eumemosem@nadaSENHAatual123@)`NinoJPDtudoDev!2026LfZDp9ftZLgbpd1f

Neste meu projeto C:\2026MeusProjetos\Dtudo2026\DtudoSite\
Após logar, ao acessar a página MyMusicX, ela não está carregando. Preciso que ela volte a funcionar, mesmo que temporariamente, pois vou criar a ApiMyMusicX na seguencia, mas neste momento preciso que a página MyMusicX volte a funcionar, mesmo que temporariamente, para poder acessar as informações das músicas, artistas e álbuns.
Temos esse erro no console:
[0] (node:7876) [MODULE_TYPELESS_PACKAGE_JSON] Warning: Module type of file:///C:/2026MeusProjetos/Dtudo2026/scripts/run-if-down.js is not specified and it doesn't parse as CommonJS.
[0] Reparsing as ES module because module syntax was detected. This incurs a performance overhead.
[0] To eliminate this warning, add "type": "module" to \\?\C:\2026MeusProjetos\Dtudo2026\package.json.
[0] (Use `node --trace-warnings ...` to show where the warning was created)
[2] (node:21716) [MODULE_TYPELESS_PACKAGE_JSON] Warning: Module type of file:///C:/2026MeusProjetos/Dtudo2026/scripts/run-if-down.js is not specified and it doesn't parse as CommonJS.
[2] Reparsing as ES module because module syntax was detected. This incurs a performance overhead.
[2] To eliminate this warning, add "type": "module" to \\?\C:\2026MeusProjetos\Dtudo2026\package.json.
[2] (Use `node --trace-warnings ...` to show where the warning was created)
[3] (node:22536) [MODULE_TYPELESS_PACKAGE_JSON] Warning: Module type of file:///C:/2026MeusProjetos/Dtudo2026/scripts/run-if-down.js is not specified and it doesn't parse as CommonJS.
[3] Reparsing as ES module because module syntax was detected. This incurs a performance overhead.
[3] To eliminate this warning, add "type": "module" to \\?\C:\2026MeusProjetos\Dtudo2026\package.json.
[3] (Use `node --trace-warnings ...` to show where the warning was created)
[1] (node:14472) [MODULE_TYPELESS_PACKAGE_JSON] Warning: Module type of file:///C:/2026MeusProjetos/Dtudo2026/scripts/run-if-down.js is not specified and it doesn't parse as CommonJS.
[1] Reparsing as ES module because module syntax was detected. This incurs a performance overhead.
[1] To eliminate this warning, add "type": "module" to \\?\C:\2026MeusProjetos\Dtudo2026\package.json.
[1] (Use `node --trace-warnings ...` to show where the warning was created)
[4] (node:17336) [MODULE_TYPELESS_PACKAGE_JSON] Warning: Module type of file:///C:/2026MeusProjetos/Dtudo2026/scripts/run-if-down.js is not specified and it doesn't parse as CommonJS.
[4] Reparsing as ES module because module syntax was detected. This incurs a performance overhead.
[4] To eliminate this warning, add "type": "module" to \\?\C:\2026MeusProjetos\Dtudo2026\package.json.
[4] (Use `node --trace-warnings ...` to show where the warning was created)
[3] [dtudo] https://localhost:51376/health/live ja esta acessivel (HTTP 200). Mantendo este processo ativo para o concurrently.
[0] [dtudo] https://localhost:7243/health/live ja esta acessivel (HTTP 200). Mantendo este processo ativo para o concurrently.
[2] [dtudo] https://localhost:7146/ApiMyAnimeList/health ja esta acessivel (HTTP 401). Mantendo este processo ativo para o concurrently.
[1] [dtudo] https://localhost:63980/apiLocal/Health ja esta acessivel (HTTP 401). Mantendo este processo ativo para o concurrently.
[5] (node:21352) [MODULE_TYPELESS_PACKAGE_JSON] Warning: Module type of file:///C:/2026MeusProjetos/Dtudo2026/scripts/run-if-down.js is not specified and it doesn't parse as CommonJS.
[5] Reparsing as ES module because module syntax was detected. This incurs a performance overhead.
[5] To eliminate this warning, add "type": "module" to \\?\C:\2026MeusProjetos\Dtudo2026\package.json.
[5] (Use `node --trace-warnings ...` to show where the warning was created)
[4] [dtudo] http://localhost:4010/health/live fora do ar. Iniciando: npm run proxy
[5] [dtudo] http://localhost:5173/ fora do ar. Iniciando: npm run dev
[4]
[4] > dtudo@3.0.0 proxy
[4] > node ../ApiNode/mymusicx/discogsProxy.js
[4]
[5]
[5] > dtudo@3.0.0 dev
[5] > vite
[5]
[4] (node:14220) [MODULE_TYPELESS_PACKAGE_JSON] Warning: Module type of file:///C:/2026MeusProjetos/Dtudo2026/ApiNode/mymusicx/discogsProxy.js is not specified and it doesn't parse as CommonJS.
[4] Reparsing as ES module because module syntax was detected. This incurs a performance overhead.
[4] To eliminate this warning, add "type": "module" to \\?\C:\2026MeusProjetos\Dtudo2026\package.json.
[4] (Use `node --trace-warnings ...` to show where the warning was created)
[4] discogsProxy.js listening on http://localhost:4010



===================================================================================================

Documentação official da api MyAnimeList:  <https://myanimelist.net/apiconfig/references/api/v2#section/Common-parameters>  

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

======================================================================================================
O aviso NU1510 e o conflito de WindowsBase continuam sendo avisos preexistentes.

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
