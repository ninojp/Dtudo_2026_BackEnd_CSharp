
🎯 Próximas Ações Recomendadas

1. Criar estrutura de 2 projetos APIs + 1 Shared
2. Implementar o módulo FileStorage logo (enquanto estrutura)
3. Adicionar logging centralizado (Serilog)
4. Documentação OpenAPI (Swagger) para cada API
5. Docker Compose para orquestrar ambos os serviços
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

Agora no meu projeto WinAppDtudo/Forms/Frm_MyAnimes.cs. Na ABA, WinAppDtudo/FormsUC/FUC_BuscarPorNome.cs quero implementar uma funcionalidade que permita ao usuário buscar animes por nome utilizando a API externa Jikan. 
A ideia é que o usuário digite o nome do anime em um campo de texto, e ao clicar em um botão de busca, a aplicação faça uma requisição à API Jikan e exiba os resultados.
Os resultados (animes encontrados) devem ser exibidos em forma de cards, contendo o Titulo e suas variações, ingles, sinonimos (quando disponível).
Uma Imagem de capa do anime e seu ano de lançamento devem ser exibidos em cada card.
Obviamente o retorno deve ser paginado.
Os Cards devem ser clicáveis, e ao clicar em um card, a aplicação deve abrir uma nova ABA (WinAppDtudo/FormsUC/FUC_DetalhesAnime.cs) que exibirá informações detalhadas sobre o anime selecionado.
Os detalhes devem ser todos (propriedades da classe Anime) que já estão disponíveis na API Jikan (Anime), e devem ser exibidos na pagina (aba) de detalhes, apenas os que estiverem disponíveis (não nulos).

Quero uma implementação COMPLETA, ROBUSTA, E 100% ATUALIZADA, utilizando boas práticas de programação, incluindo tratamento de erros, validação de entrada do usuário, e uma interface amigável e responsiva.
