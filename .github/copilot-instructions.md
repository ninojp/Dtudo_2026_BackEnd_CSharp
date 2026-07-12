# Copilot Instructions

## Working conventions

- Prefer reusing existing components, contexts, hooks, services, and routes before creating new ones.
- Keep file and folder names consistent with the current project structure.
- Preserve the current architecture unless the user explicitly asks for a refactor.
- When refactoring controllers in this repository, preserve and correct existing XML/Swagger comments instead of removing them.
- Always use modern and secure coding practices, ensuring consistent standards across controllers.
- Os novos projetos (`ApiJikan`, `ApiMyAnimes`, `LibDtudo.Shared`, `WinAppDtudo`, `ApiMyAnimeList`) foram criados dentro de `Dtudo2026`.
- Preferir sempre modularização e reaproveitamento de código, evitando duplicação de layout e lógica.
- Manter implementações de ApiJikan e ApiMyAnimeList separadas no WinAppDtudo, com UserControls e itens de menu independentes, preservando o fluxo antigo sem quebra e criando novos componentes para a nova API.

## User Interaction Guidelines

- No fluxo de importação do WinAppDtudo, forneça feedback visual em tempo real com texto detalhado durante análise e salvamento em banco, com confirmação por etapas, evitando apenas a exibição de porcentagens.
- Implementar tratamento resiliente para falhas 504 da ApiJikan.
- A ApiMyAnimeList deve ser a fonte de dados independente e exclusiva para busca, capas, detalhes e relações; a ApiJikan não deve ser usada como fallback nesse fluxo.
