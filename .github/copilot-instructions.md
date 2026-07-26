# Copilot Instructions

## Working conventions

- Prefer reusing existing components, contexts, hooks, services, and routes before creating new ones.
- Keep file and folder names consistent with the current project structure.
- Preserve the current architecture unless the user explicitly asks for a refactor.
- When refactoring controllers in this repository, preserve and correct existing XML/Swagger comments instead of removing them.
- Always use modern and secure coding practices, ensuring consistent standards across controllers.
- Os projetos ativos (`ApiMyAnimes`, `ApiMyAnimeList`, `LibDtudo.Shared`, `WinAppDtudo` e `DtudoSite`) fazem parte da mesma solu��o `Dtudo2026`.
- Preferir sempre modularização e reaproveitamento de código, evitando duplicação de layout e lógica.
- Tratar `ApiMyAnimeList` como substituta do fluxo antigo de consulta externa de animes; nao criar novos fluxos para API externa antiga.
- Validar todos os fluxos de imagens na aplicação, pois a correção anterior foi insuficiente e ainda faltam muitas imagens em diversos pontos.

## User Interaction Guidelines

- No fluxo de importação do WinAppDtudo, forneça feedback visual em tempo real com texto detalhado durante análise e salvamento em banco, com confirmação por etapas, evitando apenas a exibição de porcentagens.
- Implementar tratamento resiliente para falhas 504 da ApiMyAnimeList.
- A ApiMyAnimeList deve ser a fonte de dados independente e exclusiva para busca, capas, detalhes e relacoes.
- Para diálogos do WinAppDtudo, o usuário prefere dimensões maiores e a mensagem do MyAnime existente no formato: "MyAnime ID: Nome do anime", quebra de linha, "O MyAnime já foi cadastrado!".
