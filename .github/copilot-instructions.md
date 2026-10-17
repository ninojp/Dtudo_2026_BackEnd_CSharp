# Copilot Instructions

## Working conventions

- Prefer reusing existing components, contexts, hooks, services, and routes before creating new ones.
- Keep file and folder names consistent with the current project structure.
- Preserve the current architecture unless the user explicitly asks for a refactor.
- When refactoring controllers in this repository, preserve and correct existing XML/Swagger comments instead of removing them.
- Always use modern and secure coding practices, ensuring consistent standards across controllers.
- Os projetos ativos (`ApiMyAnimes`, `ApiMyAnimeList`, `LibDtudo.Shared`, `WinAppDtudo` e `DtudoSite`) fazem parte da mesma solução `Dtudo2026`.
- Preferir sempre modularização e reaproveitamento de código, evitando duplicação de layout e lógica.
- Tratar `ApiMyAnimeList` como substituta do fluxo antigo de consulta externa de animes; nao criar novos fluxos para API externa antiga.
- Validar todos os fluxos de imagens na aplicação, pois a correção anterior foi insuficiente e ainda faltam muitas imagens em diversos pontos.
- Na busca de animes do DB_Local, preservar o padrão existente de normalização de caracteres especiais e a prioridade de busca: título principal, depois título em inglês e demais títulos alternativos conforme o mecanismo atual.
- No DB_Local, MyAnime é uma coleção interna da tabela MyAnimes: cada anime possui MyAnimeId e todos os animes com o mesmo MyAnimeId pertencem ao conjunto relacionado. Esse relacionamento interno é diferente e independente das relações oficiais da ApiMyAnimeList; no detalhe local, relações e navegação devem usar MyAnimeId/DB_Local, nunca as relações externas da ApiMyAnimeList.
- O `WinAppDtudo` está todo em Dark Mode. Não aplicar fundos claros, painéis claros ou layout light em telas WinForms; reutilizar `ThemeManager`, `DarkModeColors` e os padrões visuais escuros existentes.

## User Interaction Guidelines

- No fluxo de importação do WinAppDtudo, forneça feedback visual em tempo real com texto detalhado durante análise e salvamento em banco, com confirmação por etapas, evitando apenas a exibição de porcentagens.
- Implementar tratamento resiliente para falhas 504 da ApiMyAnimeList.
- A ApiMyAnimeList deve ser a fonte de dados independente e exclusiva para busca, capas, detalhes e relações.
- Para diálogos do WinAppDtudo, o usuário prefere dimensões maiores e a mensagem do MyAnime existente no formato: "MyAnime ID: Nome do anime", quebra de linha, "O MyAnime já foi cadastrado!".

## Anime Detail Display Guidelines

- No controle de detalhes de anime, exibir os títulos alternativos na ordem: título principal, título em inglês, sinônimos e título japonês; usar uma fonte um pouco maior para o título e alinhar os títulos secundários à linha superior da imagem de capa.
- Manter os três itens de estatísticas (ano, tipo e score) centralizados na mesma linha, seguidos verticalmente por episódios, duração e gêneros; o layout deve ser dinâmico porque apenas gêneros têm altura variável.
- Nas duas fontes de detalhes, a coluna esquerda segue a mesma ordem compacta: capa reduzida, botão principal, informações (ano, tipo e score, episódios, duração e gêneros, com espaçamento compacto entre as linhas) e botão final. No DB Local, os botões são Exibir MyAnime e Editar Anime; na ApiMyAnimeList, Salvar Como MyAnime fica antes das informações e Salvar Anime depois.
- Nas duas fontes, a coluna esquerda tem 300 px lógicos quando o layout está lado a lado; título principal e sinônimos ficam centralizados sobre a capa, enquanto inglês e japonês começam no início da coluna direita. Quando as colunas ficam empilhadas, os quatro títulos ficam centralizados.
- Nas duas fontes, elementos fixos (legendas como Mal ID:/Fonte:, título e mensagem de "Animes relacionados", títulos de seção e separadores) usam o Gold padrão do WinAppDtudo. Os dados seguem a cor da fonte. Se a renderização emoji nativa for tintada pela cor do texto, use Gold para os ícones (`IconTextLabel.IconColor = DarkModeColors.TextColor`) e padronize os ícones de informação em 9 pt. Use `AccentLabel` para rótulos fixos.
- Os mini cards relacionados das duas fontes usam `UC_MiniAnimeCard.FormatoPoster` (moldura proporcional em grade alinhada à esquerda que preenche a coluna direita). O `UC_MiniAnimeCard` posiciona tudo manualmente em pixels do DPI atual, por isso usa `AutoScaleMode.None` para não escalar os filhos duas vezes em DPI alto.
