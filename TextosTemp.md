
🎯 Próximas Ações Recomendadas

1. Criar estrutura de 2 projetos APIs + 1 Shared
2. Implementar o módulo FileStorage logo (enquanto estrutura)
3. Adicionar logging centralizado (Serilog)
4. Documentação OpenAPI (Swagger) para cada API
5. Docker Compose para orquestrar ambos os serviços
========================================================

Projeto LibDtudo.Shared - Biblioteca para compartilhar Dtos, Modelos, Utils... entre os meus projetos

Projeto ApiMyAnimes - Api Local MyAnimes - https://localhost:63980
Esta é uma Api Local que manipula (CRUD completo) um Banco de dados Relacional local que contém informações relacionadas as minhas coleções de animes.  
(/apiLocal/MyAnime) MyAnime (DBtabela) representa as coleções nomeadas que agrupam APENAS os IDs dos animes relacionados.
(/apiLocal/Anime) Anime (DBtabela) contém informações detalhadas sobre cada anime.

Projeto ApiJikan - Api Jikan Consulta Externa - https://localhost:63982
Esta é uma Api de consulta à API externa Jikan (MyAnimeList). Fornece endpoints para buscar (por nome ou ID) informações detalhadas sobre animes e seus relacionamentos.
ApiJikan  
GET/ApiJikan/search  
End-Point da minha Api Local que faz uma busca na Api externa Jikan, por nome do anime.
/ApiJikan/{id}  
Busca um anime específico por ID do MyAnimeList.
/ApiJikan/{id}/relations
Busca os animes relacionados a um anime específico pelo ID do MyAnimeList. Utiliza o endpoint dedicado /anime/{id}/relations da Jikan e retorna as imagens hidratadas de cada entrada.

Projeto 
