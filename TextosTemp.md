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

-------------------------------------------------------------------------------------------------------------------------------------

Neste meu projeto C:\2026MeusProjetos\Dtudo2026\WinAppDtudo\




Quero uma implementação Profissional Completa, Robusta e que contemple todos os casos que um Form WinForms pode ter.


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

======================================================================================================
🎯 Próximas Ações Recomendadas para solução "Dtudo2026":

1. Adicionar logging centralizado (Serilog)
2. Docker Compose para orquestrar ambos os serviços
3.

======================================================================================================
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
