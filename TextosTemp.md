PRIMEIRAMENTE, NÃO QUERO QUE LEIA TODA MINHA SOLUÇÃO, POIS ELA É GRANDE E COMPLEXA.
EU CRIEI TODOS OS PROJETOS, ME PERGUNTE SE PRECISAR DE ALGUMA INFORMAÇÃO SOBRE ALGUM PROJETO.

Abaixo vou descrever minha SOLUÇÃO: C:\2026MeusProjetos\Dtudo2026\ (conjunto de projetos) chamada "Dtudo2026" e seus projetos internos relacionados

Após as descrições, vou detalhar o que quero que seja feito.

Aplicação Desktop: WinAppDtudo, Front-End: DtudoSite e Back-End: ApiMyAnimes, ApiMyAnimeList, LibDtudo.Shared.

O projeto ApiNode (deve ser ignorado) está sendo gradativamente sendo substituído pelos projetos ApiMyAnimes e ApiMyAnimeList.

Projeto LibDtudo.Shared - Biblioteca para compartilhar Dtos, Modelos, Utils... entre os projetos dentro da solução Dtudo2026.

Projeto ApiMyAnimes - Api Local MyAnimes (CRUD completo, documentada com Swagger) - https://localhost:63980
Esta é uma Api Local que manipula Meu Banco de dados, Relacional (SQL Server) que contém minhas coleções, MyAnimes e seus Animes relacionados.  
(/apiLocal/MyAnime) MyAnime (tabela_db) representa as coleções nomeadas MyAnime por titulo e uma lista de IDs de animes relacionados.
(/apiLocal/Anime) Anime (tabela_db) contém informações detalhadas sobre cada anime.

Projeto ApiMyAnimeList - Api local ApiMyAnimeList - https://localhost:7146
Esta é uma Api de consulta à API externa, Oficial MyAnimeList. Fornece endpoints para buscar (por nome ou ID) informações detalhadas sobre animes e seus relacionamentos.
GET/ApiMyAnimeList/search  
End-Point da minha Api Local que faz uma busca na Api externa ApiMyAnimeList, por nome do anime.
/ApiMyAnimeList/{id}  
Busca um anime específico por ID do MyAnimeList.
/ApiMyAnimeList/{id}/relations
Busca os animes relacionados a um anime específico pelo ID do MyAnimeList. Utiliza o endpoint dedicado /anime/{id}/relations da ApiMyAnimeList e retorna as imagens hidratadas de cada entrada.

Projeto WinAppDtudo - Aplicativo Desktop para consulta, cadastro e manipulação de dados (Lê e grava no DB_Local e em disco local, pastas e arquivos).

Agora quero que entenda que havia um projeto chamado API externa antiga, que foi descontinuado e removido e está sendo substituído pelo projeto ApiMyAnimeList.
Ainda existem muitas referências (códigos, arquivos de configuração, comentários, nomes legados da API externa antiga) ao projeto API externa antiga, em WinAppDtudo, LibDtudo.Shared, ApiMyAnimes, ApiMyAnimeList, e DtudoSite. Quero que você me ajude a localizar e substituir todas essas referências ao projeto API externa antiga, para que tudo fique consistente com o novo projeto ApiMyAnimeList.
Aproveite que tera que ler a sulução quase toda, para localizar e substituir todas as Falhas, Erros, inconsistências, problemas de nomenclatura, problemas de arquitetura, problemas de modularização, problemas de performance, problemas de segurança, problemas de usabilidade, problemas de acessibilidade, problemas de responsividade, problemas de compatibilidade, problemas de manutenção, problemas de documentação, problemas de testes, problemas de integração, problemas de deploy, problemas de versionamento, problemas de licenciamento, problemas legais e quaisquer outros problemas que você encontrar. E crie uma lista em arquivo.md, detalhada de tudo que você encontrou, com sugestões de melhorias, correções e implementações necessárias.




-------------------------------------------------------------------------------------------------------------------------------------
Projeto API externa antiga
O Projeto, C:\2026MeusProjetos\Dtudo2026\ApiMyAnimeList (DEVE SER IGNORADO, no contexto atual da pergunta)
O Projeto, C:\2026MeusProjetos\Dtudo2026\ApiNode (PODE SER IGNORADO, lido apenas se necessário no contexto atual da pergunta)


Agora neste meu projeto C:\2026MeusProjetos\Dtudo2026\WinAppDtudo, em \Forms_UC\FUC_DBLocalBuscarNome.cs


Quero uma implementação, modularizada, sem arquivos únicos muito extensos, COMPLETA e ROBUSTA, com todos os detalhes necessários. Me pergunte se precisar de mais informações sobre o que já existe, ou se precisar de detalhes sobre o que deve ser implementado.






===================================================================================================

Documentação official da api MyAnimeList:  https://myanimelist.net/apiconfig/references/api/v2#section/Common-parameters  

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
