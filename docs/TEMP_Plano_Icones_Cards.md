# TEMP — Plano: ícones de status nos cards de anime

> Arquivo temporário. Apagar quando as 8 fases terminarem (a exclusão do arquivo é o último passo da Fase 8).
> Como usar: abra um **chat novo** para cada fase, cole **apenas** o bloco "Prompt" da fase e espere terminar antes da próxima. Cada prompt manda a I.A. ler este arquivo (seção "Contrato comum") em vez de repetir o contexto, para gastar menos.

## Ordem e dependências

| Fase | Entrega | Depende de | Risco |
|---|---|---|---|
| 1 | Ícones no card (visual, dados falsos) + olho/pasta/gostei/play | — | baixo |
| 2 | Ponte de autenticação por usuário (site → gateway → ApiMyAnimes) + endpoint em lote | 1 | **alto** (segurança) |
| 3 | Ícone **olho**: progresso de episódios por usuário | 2 | médio |
| 4 | **Pasta A**: contagem de vídeos na ApiFileStorage + regras compartilhadas | — (pode ser feita antes da 2) | médio |
| 5 | **Pasta B**: anime → subpasta, persistência e status na ApiMyAnimes | 2, 4 | alto |
| 6 | **Pasta C**: WinAppDtudo dispara e mostra o progresso da atualização | 5 | médio |
| 7 | Revisão final ponta a ponta + documentação | 1–6 | baixo |
| 8 | **LGPD**: worker de exclusão + exportação/exclusão do progresso (e dados de pasta) via ApiIdentity → ApiMyAnimes (client credentials + mTLS) | 3 (e 5 para os dados de pasta); ambiente com `ServiceAuthentication` habilitado | **alto** (dado pessoal, exclusão irreversível) |

Ícone **gostei** e botão **play**: nesta etapa são só o ícone (fase 1). Os detalhes entram depois, em prompts separados.

## Contrato comum (todas as fases leem esta seção)

### Estado atual confirmado no código
- Site: `CardAnime.jsx` (em `DtudoSite\src\components\componentsAnimes\CardAnime\`) é renderizado dentro de um `<Link>` em `CardsAnimesList.jsx`. O site carrega o catálogo inteiro (`AnimesProvider.jsx` → `buscarTodosAnimesDaApiLocal`) e pagina no cliente (20/50/100 por página). Já usa `react-icons/fa` e CSS Modules. Acesso à API sempre por `withCredentials` via gateway.
- Identificador do anime: `MalId` (`LibDtudo.Shared\Models\Anime.cs`). Total de episódios: `Episodes` (`int?`, vindo do MAL) e `Episodios` (`int`, padrão 1). **Regra: `total = Episodes` se > 0, senão `Episodios` se > 1, senão desconhecido.**
- Gateway (`DtudoGateway`): rotas `/api/catalog/animes*` removem o header `Authorization` e a ApiMyAnimes atende essas rotas como anônimas. O cliente OIDC `dtudo-gateway` só tem os recursos `urn:dtudo:api-musicx` e `urn:dtudo:api-discogs`; **não** tem `urn:dtudo:api-my-animes`.
- ApiMyAnimes: autorização por política de permissão + escopo (`ApiAuthorizationPolicies`). Vínculo de pasta existente: `MyAnimeMonitoringLocation(MyAnimeId, RootKey, RelativePath)` é **por coleção MyAnime**, não por anime.
- ApiFileStorage: monitoramento só tem inventário com sessão aberta; classificação de vídeo existe só no WinApp (`FUC_Monitoramento`). Descoberta de sincronização (`synchronizations/discovery`) já lê as subpastas de 1º nível e os IDs numéricos de capas (`<MalId>.jpg`).
- Documentação de referência: `docs\00_Solucao.md`, `10_DtudoSite.md`, `20_DtudoGateway.md`, `21_ApiIdentity.md`, `22_ApiMyAnimes.md`, `24_ApiFileStorage.md` (seção final "Base para o indicador de pasta do anime"), `11_WinAppDtudo.md`.

### Aparência (site)
Quatro ícones em **coluna vertical**, no **meio da lateral esquerda** do card, **flutuando sobre a imagem** de capa (posição absoluta dentro do `figure`), na ordem: olho, pasta, gostei, play. Visual escuro e discreto (fundo semitransparente) para não esconder a capa.

| Ícone | Cinza | Azul | Verde |
|---|---|---|---|
| Olho (visto) | nenhum episódio visto | parcialmente visto | todos os episódios vistos |
| Pasta | pasta não existe / não encontrada / nunca verificada / sem nenhum vídeo de episódio | pasta tem vídeos, mas vídeos < episódios (ou episódios desconhecidos) | pasta com vídeos ≥ episódios |

Gostei (polegar) e play: neutros por enquanto. Usar as cores já existentes no projeto; se não houver variável adequada, definir três custom properties no CSS Module do componente. Todo ícone tem `title` e `aria-label` em português.

### Contrato da API (único endpoint em lote; sem N requisições por card)
- Site → gateway: `POST /api/catalog/animes/status`
- Gateway → ApiMyAnimes: `POST /apiLocal/Anime/status`
- Corpo: `{ "malIds": [1, 5, 20] }` (máximo **200**; ids repetidos ou inválidos são descartados; lista vazia retorna `items: []`).
- Resposta:

```json
{
  "items": [
    {
      "malId": 1,
      "watch":  { "state": "none|partial|complete", "watched": 0, "total": 26 },
      "folder": { "state": "none|partial|complete", "videos": 0, "expected": 26, "checkedAtUtc": null },
      "like":   null
    }
  ]
}
```
- `total`/`expected` pode ser `null` (desconhecido). `folder.checkedAtUtc = null` significa "nunca verificado" e vale como `none` (cinza).
- A identidade do usuário vem **exclusivamente** do token (claim `sub`); nunca do corpo ou de query string.

### Regras de negócio
**Olho:** `none` se vistos = 0; `complete` se `total` conhecido e vistos ≥ total; senão `partial`.
**Pasta:** `none` se a pasta do anime não existe, não foi encontrada, nunca foi verificada **ou não tem nenhum vídeo de episódio (0 vídeos)**; `complete` se vídeos ≥ `expected` (e `expected` conhecido, leitura não truncada); `partial` se há pelo menos 1 vídeo e (vídeos < `expected` **ou** `expected` desconhecido **ou** leitura truncada).
**O que conta como vídeo de episódio:** as 23 extensões de vídeo reconhecidas (`.3g2 .3gp .asf .avi .divx .flv .m2ts .m2v .m4v .mkv .mov .mp4 .mpe .mpeg .mpg .mts .ogv .rm .rmvb .ts .vob .webm .wmv`; lista única em `LibDtudo.Shared`, `EpisodeVideoPolicy`, usada por API e WinApp)

### Segurança e qualidade (valem em todas as fases)
- Dados por usuário isolados por `sub`; teste automatizado provando que um usuário não lê nem altera o dado de outro.
- Rotas de escrita seguem o padrão de CSRF e as permissões do gateway/ApiIdentity já existentes (ver `docs\20_DtudoGateway.md`); não criar atalhos anônimos.
- Nenhuma resposta ao site devolve caminho físico de disco.
- Não copiar segredos nem caminhos de usuário em código ou docs.
- Preservar o visual existente e o Dark Mode do WinApp; preservar comentários XML/Swagger existentes.
- O site continua em JavaScript/React 19 + CSS Modules (será migrado para Next.js/TypeScript depois): manter o componente novo desacoplado (props simples, sem acesso direto a contextos globais) para facilitar o porte.
- Ao terminar: `dotnet build`/testes focados dos projetos tocados, `npm run build` no site quando tocado, e atualizar **apenas** os docs afetados (`docs\*.md`). Sem commit nem PR.

---

## Fase 1 — Ícones no card (visual com dados falsos)

```text
Leia docs\TEMP_Plano_Icones_Cards.md, seções "Contrato comum" e "Fase 1", e execute SOMENTE a Fase 1.

Objetivo: criar o componente AnimeStatusIcons e exibi-lo em todo CardAnime.

Tarefas:
1. Criar DtudoSite\src\components\componentsAnimes\AnimeStatusIcons\AnimeStatusIcons.jsx + .module.css com 4 ícones (react-icons/fa: FaEye, FaFolder, FaThumbsUp, FaPlay) em coluna vertical, meio da lateral esquerda, flutuando sobre a imagem, conforme "Aparência". Props: watchState, folderState ('none'|'partial'|'complete'), like (null por enquanto), onPlay (opcional). Cores cinza/azul/verde por estado, title e aria-label em português (ex.: "Visto: parcialmente", "Pasta: encontrada, faltam episódios").
2. Integrar no CardAnime.jsx dentro do <figure> da capa (posição relativa no figure). Os ícones NÃO podem acionar a navegação do <Link> pai: botões com type="button" e preventDefault + stopPropagation no clique. Gostei e play ficam sem ação (play com title "Em breve").
3. Criar o hook DtudoSite\src\hooks\useAnimeStatus.js que recebe a lista de malIds visíveis e devolve um mapa malId -> { watch, folder, like }. Nesta fase ele retorna sempre o padrão (watch none, folder none, like null), mas com a assinatura final (ele será ligado à API na Fase 2). Usá-lo em CardsAnimesList.jsx (passar o status para cada CardAnime pelo ID da página atual) sem alterar filtros, busca ou paginação.
4. Não alterar o layout do card além de acomodar os ícones; conferir o visual em telas estreitas e no card sem imagem ("Imagem indisponivel").
5. Teste manual descrito no resumo final (npm run serv) e `npm run build` sem erros/avisos novos. Atualize docs\10_DtudoSite.md (componente, hook e contrato de estados).

Não toque em backend nem em outros projetos.
```

## Fase 2 — Ponte de autenticação por usuário e endpoint em lote

```text
Leia docs\TEMP_Plano_Icones_Cards.md, seções "Contrato comum" e "Fase 2", e execute SOMENTE a Fase 2. Leia também docs\20_DtudoGateway.md, docs\21_ApiIdentity.md e docs\22_ApiMyAnimes.md.

Objetivo: o site autenticado conseguir chamar a ApiMyAnimes com a identidade do usuário, e existir o endpoint em lote que devolve status por anime (ainda com valores neutros).

Tarefas:
1. ApiIdentity: incluir o recurso urn:dtudo:api-my-animes nos recursos do cliente dtudo-gateway e garantir o escopo/permissão necessários; revisar o handler OIDC do gateway (hoje ele define `resource` duas vezes — o token precisa conter os recursos necessários sem sobrescrever). Se existir `sub`/audience mismatch, corrigir.
2. DtudoGateway: nova rota POST /api/catalog/animes/status → ApiMyAnimes POST /apiLocal/Anime/status, exigindo sessão, protegida contra CSRF no mesmo padrão das demais mutações do gateway, injetando o Bearer da sessão (como já faz com /api/catalog/music) e SEM abrir as demais rotas de anime. Corpo limitado (tamanho máximo adequado a 200 ids).
3. ApiMyAnimes: endpoint POST apiLocal/Anime/status com política de permissão + escopo (padrão ApiAuthorizationPolicies), validação do corpo (máx. 200, deduplicação), identidade só pelo claim sub, resposta no formato do contrato com valores neutros (watch none/0/total, folder none, like null). Calcular `total` pela regra do contrato. Sem acesso a disco e sem tabela nova nesta fase.
4. DtudoSite: implementar a chamada em services\apiMyAnimes.js (buscarStatusAnimes) e ligar o hook useAnimeStatus a ela: uma requisição por página/mudança de filtro, com AbortController, cache em memória por sessão e falha silenciosa (ícones cinza + sem quebrar o card; um console.error discreto).
5. Testes: ApiMyAnimes (autorização: anônimo 401, usuário sem permissão 403, validação do corpo, limite 200), DtudoGateway (rota exige sessão e CSRF; não repassa o Bearer nas outras rotas de anime) e ApiIdentity se o catálogo de recursos tiver teste. Rodar os testes focados dos 3 projetos e `npm run build`.
6. Atualizar docs: 20_DtudoGateway.md, 21_ApiIdentity.md, 22_ApiMyAnimes.md, 10_DtudoSite.md e a matriz de acesso em 01_Solucao_Seguranca.md.

Se algo exigir decisão de segurança que não esteja no contrato, escolha a opção mais restritiva e registre em "Pendências/Divergências" do doc do projeto.
```

## Fase 3 — Ícone olho (episódios vistos por usuário)

```text
Leia docs\TEMP_Plano_Icones_Cards.md, seções "Contrato comum" e "Fase 3", e execute SOMENTE a Fase 3. Leia também docs\22_ApiMyAnimes.md e docs\10_DtudoSite.md. A Fase 2 já está pronta (endpoint POST /apiLocal/Anime/status e hook useAnimeStatus).

Objetivo: persistir e exibir o progresso de episódios vistos por usuário.

Modelo (ApiMyAnimes, mesmo DbContext e padrões de migração do projeto): tabela AnimeEpisodeWatch com chave única (AccountId, MalId, EpisodeNumber), AccountId = sub do token (string), WatchedAtUtc. EpisodeNumber ≥ 1 e, quando o total é conhecido, ≤ total. Índice por (AccountId, MalId).

Tarefas:
1. Entidade, configuração EF, migração (gerar com dotnet ef; NÃO aplicar em banco sem avisar; descrever o comando de aplicação no resumo).
2. Endpoints na ApiMyAnimes (política de permissão/escopo para escrita pessoal, seguindo o padrão das permissões pessoais da ApiIdentity; criar a permissão no catálogo da ApiIdentity se necessário):
   - PUT apiLocal/Anime/{malId}/watch/episodes  corpo { "episodes": [1,2,3] } marca como vistos (idempotente);
   - DELETE apiLocal/Anime/{malId}/watch/episodes  corpo { "episodes": [..] } desmarca; sem corpo/lista vazia não apaga nada;
   - PUT apiLocal/Anime/{malId}/watch/all  marca todos (exige total conhecido);
   - DELETE apiLocal/Anime/{malId}/watch  limpa o anime;
   - GET apiLocal/Anime/{malId}/watch  lista os episódios vistos.
   Rotas equivalentes no gateway com sessão + CSRF (mesmo padrão da Fase 2). Anime inexistente = 404.
3. Preencher `watch` no endpoint em lote usando uma única consulta agregada (GROUP BY MalId) — sem N+1.
4. Site: em DtudoSite\src\pages\Animes\AnimesDetalhes\AnimesDetalhes.jsx adicionar controle mínimo e sóbrio (mesmo estilo da página): lista/grade de episódios 1..total com marcar/desmarcar, botões "Marcar todos" e "Limpar". Ao alterar, invalidar o cache do hook useAnimeStatus para o ícone do card atualizar. Se total desconhecido, mostrar aviso e não oferecer "Marcar todos".
5. Testes: isolamento entre dois usuários, idempotência, limites de episódio, status none/partial/complete, anime sem total. Rodar testes focados e `npm run build`.
6. Atualizar docs 22_ApiMyAnimes.md, 20_DtudoGateway.md, 21_ApiIdentity.md (se criou permissão) e 10_DtudoSite.md. Registrar em 01_Solucao_Seguranca.md que esse dado pessoal ainda NÃO entra na exportação/exclusão LGPD da ApiIdentity (pendência).
```

## Fase 4 — Pasta A: contagem de vídeos na ApiFileStorage

```text
Leia docs\TEMP_Plano_Icones_Cards.md, seções "Contrato comum" e "Fase 4", e execute SOMENTE a Fase 4. Leia também docs\24_ApiFileStorage.md (inclusive "Base para o indicador de pasta do anime") e o código de MonitoringReader / MonitoringPathPolicy / CollectionSyncDiscoveryService.

Objetivo: a ApiFileStorage conseguir dizer, sob demanda e sem sessão de monitoramento, se uma pasta de anime existe e quantos vídeos de episódio ela contém.

Tarefas:
1. LibDtudo.Shared: criar a política única de vídeo de episódio (lista de extensões, nomes/pastas de extras ignorados, ignorar 0 bytes) conforme "Regras de negócio", com testes. Fazer o WinApp (`FUC_Monitoramento`) reutilizar a lista de extensões em vez da cópia local, sem mudar seu comportamento visível.
2. ApiFileStorage: novo endpoint interno POST monitoring/anime-folders/scan, corpo { "rootKey", "relativePath", "expectedEpisodes": int? }, exigindo a mesma permissão/escopo do restante do monitoramento (filesystem.command). Valida rootKey e relativePath com MonitoringPathPolicy (sem traversal, sem reparse points, sem caminho absoluto). Resposta: { exists, videoCount, directoryCount, state: none|partial|complete, truncated }. Não devolve caminho físico.
3. A varredura é leitura iterativa, com limites (profundidade máxima, máximo de arquivos, timeout/cancelamento) e `truncated = true` quando um limite for atingido (nesse caso o estado nunca é `complete`). Pasta inexistente ou raiz indisponível = exists false, state none. Não reutilizar a sessão de monitoramento nem gravar histórico.
4. Testes com sistema de arquivos temporário: pasta inexistente, vazia, só estrutura sem vídeo, menos vídeos que episódios, vídeos suficientes, extras ignorados, 0 bytes, traversal rejeitado, limite atingido, expectedEpisodes nulo.
5. Atualizar docs\24_ApiFileStorage.md (endpoint, regras, limites), 30_LibDtudo.Shared.md e 11_WinAppDtudo.md (reuso da lista de extensões); remover/atualizar a seção "O que falta" que esta fase resolver. Corrigir de passagem a inconsistência de DI do CollectionSyncDiscoveryService apenas se ela atrapalhar esta fase.
```

## Fase 5 — Pasta B: anime → subpasta, persistência e status

```text
Leia docs\TEMP_Plano_Icones_Cards.md, seções "Contrato comum" e "Fase 5", e execute SOMENTE a Fase 5. Leia docs\22_ApiMyAnimes.md e docs\24_ApiFileStorage.md. As Fases 2 e 4 já estão prontas.

Objetivo: o status da pasta de cada anime ficar calculado e persistido na ApiMyAnimes, e o endpoint em lote devolvê-lo.

Decisões já tomadas: o cálculo NÃO acontece a cada exibição do card; ele é disparado sob demanda (Fase 6 faz o botão no WinApp) e o resultado fica gravado. O site só lê o resultado.

Tarefas:
1. Mapeamento anime → subpasta: a partir do vínculo MyAnimeMonitoringLocation da coleção (MyAnimeId → RootKey/RelativePath) e da descoberta da ApiFileStorage (subpastas de 1º nível e capas `<MalId>.jpg`), resolver a subpasta de cada anime da coleção por MalId. Fallback: convenção de nome `<ano> <título> - <tipo>` já usada pelo sincronizador (reaproveitar o código existente, não duplicar). Anime sem subpasta encontrada = state none.
2. Tabela AnimeFolderStatus (MalId PK, MyAnimeId, RootKey, RelativePath, VideoCount, ExpectedEpisodes?, State, Truncated, CheckedAtUtc) + migração (gerar, não aplicar sem avisar).
3. Serviço de atualização: para uma coleção (MyAnimeId) ou para todas, chama o endpoint POST monitoring/anime-folders/scan da ApiFileStorage (chamada serviço-a-serviço como a descoberta já faz), com concorrência limitada, cancelamento, e relatório de progresso por anime. Falha de um anime não derruba o lote.
4. Endpoints (permissão administrativa/escopo de monitoramento, não acessíveis ao usuário comum): POST apiLocal/MyAnime/{myAnimeId}/folder-status/refresh e POST apiLocal/MyAnime/folder-status/refresh (todas), com resposta de execução/progresso consultável (padrão de MyAnimeMonitoringSyncRun, se servir). Sem caminho físico nas respostas ao site.
5. Endpoint em lote (Fase 2): preencher `folder` a partir da tabela (uma consulta por lote). `checkedAtUtc` nulo = nunca verificado = none.
6. Testes: mapeamento por capa, por convenção e não encontrado; atualização parcial com falha; resposta em lote; autorização do refresh; ausência de caminho físico no payload. Rodar testes focados da ApiMyAnimes.
7. Atualizar docs 22_ApiMyAnimes.md, 24_ApiFileStorage.md e a seção "Base para o indicador de pasta" (marcar o que ficou resolvido).
```

## Fase 6 — Pasta C: WinAppDtudo atualiza e acompanha o status

```text
Leia docs\TEMP_Plano_Icones_Cards.md, seções "Contrato comum" e "Fase 6", e execute SOMENTE a Fase 6. Leia docs\11_WinAppDtudo.md. As Fases 4 e 5 já estão prontas (endpoints de refresh em ApiMyAnimes).

Objetivo: no WinAppDtudo, o proprietário disparar a verificação das pastas e ver o resultado, com feedback detalhado.

Regras de UI: Dark Mode obrigatório (reutilizar ThemeManager, DarkModeColors e padrões existentes; sem fundos claros); feedback textual por etapa em tempo real (ex.: "Coleção X: verificando 'Nome do anime' (12/87)…", "Concluído: 70 completos, 10 parciais, 7 sem pasta"), não apenas porcentagem; diálogos maiores; cancelamento; respeitar DPI alto como o restante do app.

Tarefas:
1. Cliente na camada de serviços existente do WinApp (mesmo padrão e autenticação PKCE dos outros clientes de API; sem SQL e sem acesso direto a disco) para os endpoints de refresh da ApiMyAnimes e consulta de progresso.
2. Ações na UI existente onde já se gerencia coleções/monitoramento (preferir FUC_DetalhesAnime/FUC_Monitoramento e o padrão de Frm_ProgressoOperacao): "Atualizar status das pastas" (uma coleção MyAnime) e "Atualizar todas" com confirmação.
3. Exibir o resultado por anime (estado, vídeos/episódios, última verificação) com as mesmas cores cinza/azul/verde do site, usando o padrão visual escuro.
4. Tratar erros (API indisponível, 401/403, ApiFileStorage fora do ar, raiz indisponível) com mensagens claras.
5. Testes em WinAppDtudo.Tests para o cliente/serviço (sem UI). Build do WinApp sem novos avisos.
6. Atualizar docs\11_WinAppDtudo.md.
```

## Fase 7 — Revisão final e documentação

```text
Leia docs\TEMP_Plano_Icones_Cards.md, seção "Contrato comum", e execute a Fase 7: revisão SOMENTE LEITURA do que as Fases 1–6 implementaram, mais correções pequenas e seguras.

1. Verificar ponta a ponta: WinApp dispara a atualização → ApiMyAnimes → ApiFileStorage → tabela → endpoint em lote → site mostra os 4 ícones corretos (olho cinza/azul/verde; pasta cinza/azul/verde; gostei e play neutros).
2. Revisão de segurança dirigida: isolamento por `sub`; nenhuma rota anônima nova; CSRF em escritas; limite de 200 ids; nenhum caminho físico exposto; varredura com limites; permissões mínimas. Listar achados por severidade e corrigir os de severidade média ou maior sem mudar o contrato.
3. Rodar todos os testes dos projetos tocados (ApiMyAnimes, ApiFileStorage, ApiIdentity, DtudoGateway, LibDtudo.Shared, WinAppDtudo) e `npm run build`.
4. Conferir que os docs de 10, 11, 20, 21, 22, 24, 30 e 01_Solucao_Seguranca estão coerentes com o código e sem links quebrados.
5. **Não** apagar docs\TEMP_Plano_Icones_Cards.md (a Fase 8 ainda o usa) e listar pendências (ex.: ícone gostei, playlist/play). A LGPD do progresso é a Fase 8.
```

## Fase 8 — LGPD: worker de exclusão e exportação/exclusão service-to-service

Decisão de arquitetura (já tomada, registrada em `docs\01_Solucao_Seguranca.md`): a **ApiIdentity** é dona do pedido de exclusão, da carência de 7 dias, da auditoria e da retenção; ela chama a **ApiMyAnimes** por **client credentials + mTLS** (`ServiceAccessTokenProvider`/`ServiceAccessTokenHandler` de `LibDtudo.Shared`) em endpoints internos por `AccountId`. Não usa token de usuário, pois a exclusão roda em worker após a carência. Um endpoint self-service na ApiMyAnimes não resolve (a rota "Limpar" da Fase 3 já cobre o uso imediato pelo titular).

### Pré-requisitos (marcar antes de iniciar)

- [x] **Fase 3 pronta:** tabela `AnimeEpisodeWatch` (chave `AccountId` = `sub`) já existe e a migração foi aplicada.
- [x] **Fase 5 pronta:** o status de pasta (`AnimeFolderStatus`) é **dado global do anime, não pessoal** (sem `AccountId`); **não entra** na exportação/exclusão LGPD da Fase 8. Revisar o `like` (fase futura) quando existir.
- [ ] **Ambiente:** `ServiceAuthentication:Enabled=true` (padrão `false`), com certificados de cliente mTLS válidos e thumbprints/segredos fora do repositório (ver `docs\01_Solucao_Seguranca.md`, seção de mTLS e rotação). **A Fase 8 não pode ser validada ao vivo sem isso**; os testes automatizados usam substitutos.
- [ ] **Cliente de serviço** da ApiIdentity em `ServiceAuthentication:Clients` (audience `urn:dtudo:api-my-animes`, escopo mínimo novo).
- [ ] **Permissão de serviço nova** no `AuthorizationCatalog` (ex.: `service.privacy.myanimes`), concedida **somente** ao cliente de serviço — nunca a usuários.
- [ ] **Worker de processamento da exclusão:** hoje `IdentityPrivacyService.ProcessDueDeletionAsync` **não tem worker** (pendência já listada em `docs\21_ApiIdentity.md`); faz parte desta fase.
- [ ] **Decisão do dono** sobre exportação quando a ApiMyAnimes estiver indisponível (recomendação abaixo).

```text
Leia docs\TEMP_Plano_Icones_Cards.md, seções "Contrato comum" e "Fase 8", e execute SOMENTE a Fase 8. Leia docs\21_ApiIdentity.md, docs\22_ApiMyAnimes.md, docs\01_Solucao_Seguranca.md e docs\02_Solucao_Operacao.md. Confirme os pré-requisitos da seção "Fase 8" antes de codar; se algum item de ambiente faltar, implemente e teste com substitutos e me avise o que falta para validar ao vivo.

Objetivo: o progresso de episódios (e demais dados pessoais da ApiMyAnimes) entrar na exportação e na exclusão LGPD da ApiIdentity, com worker operacional.

Tarefas:
1. ApiMyAnimes: endpoints internos por AccountId (exportar o progresso; apagar o progresso — idempotente, sem 404 se não houver dado), sob política de permissão de serviço dedicada (JWT de client credentials; sem aceitar token de usuário, sem rota no gateway, sem CORS). Corpo/parâmetros validados (AccountId não vazio, tamanho máximo). Nada de caminho físico na resposta.
2. ApiIdentity: cliente HTTP tipado com `ServiceAccessTokenHandler` e timeout/retry limitado; `ExportAsync` inclui uma seção nova do progresso (incrementar a versão do formato do export e documentar); `ProcessDueDeletionAsync` chama primeiro a exclusão na ApiMyAnimes (idempotente) e só depois remove os dados locais; se a chamada falhar, o pedido continua pendente e é repetido (nunca marcar Completed com dado remanescente; não manter transação de banco aberta durante a chamada HTTP). Recomendação: exportação com a ApiMyAnimes indisponível falha com 503 (sem export silenciosamente incompleto).
3. Worker (`BackgroundService`) na ApiIdentity que processa pedidos vencidos: intervalo configurável, lote limitado, um pedido por vez, escopo DI por iteração, cancelamento no shutdown, log sem dado pessoal, auditoria em `SecurityAuditEvents`, contador de tentativas/último erro (migração EF se necessário — GERAR com dotnet ef, NÃO aplicar, e descrever o comando), desabilitado por configuração por padrão até o ambiente estar pronto. Conta bootstrap continua protegida.
4. Segurança: permissão de serviço no AuthorizationCatalog (não concedida a roles de usuário), escopo/audience em allowlist, mTLS obrigatório; testes provando que token de usuário comum recebe 403 e que um AccountId não afeta outro.
5. Testes: ApiMyAnimes (export/delete por conta, isolamento, idempotência, 401/403), ApiIdentity (export com a seção nova, exclusão chamando o cliente antes de remover, falha do cliente mantém pendente, retry, worker respeita carência e a conta bootstrap) usando substituto do cliente HTTP. Rodar testes focados dos projetos tocados.
6. Docs: 21_ApiIdentity.md (export v2, worker, config, permissão de serviço), 22_ApiMyAnimes.md (endpoints internos), 01_Solucao_Seguranca.md (remover a pendência LGPD do progresso; manter "worker não validado em ambiente real" se não houver validação ao vivo), 02_Solucao_Operacao.md (como habilitar o worker e o service-to-service).
7. Ao final, apagar docs\TEMP_Plano_Icones_Cards.md e listar pendências (ícone gostei, playlist/play, retenção do monitoramento, validação ao vivo do worker).
```

## Pendências para depois (fora das 8 fases)
- **Gostei:** modelo (like/dislike por usuário? por anime ou episódio?), endpoint e interação. O ícone e o campo `like` do contrato já ficam reservados.
- **Play/playlist:** reprodução de episódios (depende de streaming seguro via ApiFileStorage).
- **LGPD:** progresso de episódios na exportação/exclusão → **Fase 8**. Futuro: incluir o `like` quando existir.
- **Retenção do histórico de monitoramento** na ApiFileStorage.
