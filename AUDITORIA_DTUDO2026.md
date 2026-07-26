# Auditoria Dtudo2026 - ApiJikan para ApiMyAnimeList

Data: 2026-07-26

## Limpeza aplicada

- Removido o fluxo antigo `ApiJikan` do WinAppDtudo:
  - `WinAppDtudo/Services/JikanApiService.cs` removido.
  - `WinAppDtudo/FormsUC/FUC_ApiJikanBuscarNome.cs` removido.
  - Menu `ApiJikan` removido de `Frm_MyAnimes`.
- Renomeados modelos do WinAppDtudo para nomes neutros:
  - `JikanModels.cs` virou `AnimeApiModels.cs`.
  - `JikanBuscaResult` virou `AnimeSearchResult`.
  - `JikanAnimeCard` virou `AnimeSearchCard`.
  - `JikanAnimeDetalhes` virou `AnimeDetails`.
  - `JikanAnimeRelacaoGroup` virou `AnimeRelationGroup`.
  - `JikanRelacaoEntry` virou `AnimeRelationEntry`.
- Renomeado o cliente da ApiMyAnimes:
  - `ApiJikanClient` virou `MyAnimeListImportClient`.
  - A importacao por query string agora usa `malId`, nao `jikanId`.
- Alinhadas rotas locais da ApiMyAnimeList:
  - Controller agora responde em `/ApiMyAnimeList`.
  - WinAppDtudo e arquivo `.http` foram atualizados para `/ApiMyAnimeList/search`, `/ApiMyAnimeList/{id}` e `/ApiMyAnimeList/{id}/relations`.
- Atualizados `AGENTS.md` e `.github/copilot-instructions.md` para nao preservarem instrucoes antigas que tratavam ApiJikan como projeto ativo.

## Referencias ApiJikan restantes

- Restam apenas migrations EF historicas em `ApiMyAnimes/Migrations/20260620051838_RecebendoDadosDetalhadoApiJikan*`.
- Recomendacao: nao renomear migrations ja aplicadas diretamente, pois isso pode quebrar o historico do banco. Se a limpeza visual for obrigatoria, criar uma migration nova e documentar a transicao, ou validar antes em banco descartavel.

## Achados principais

1. Segredo da MyAnimeList em arquivo versionado
   - `ApiMyAnimeList/appsettings.json` contem `MyAnimeList:ClientId`.
   - Sugestao: mover para user-secrets, variavel de ambiente ou cofre de segredos, e deixar no repo apenas placeholder/documentacao.

2. Senha hardcoded no shared
   - `LibDtudo.Shared/Utils/ValidaSenhaLogin.cs` aceita login/senha fixos.
   - Sugestao: substituir por autenticacao real, hash de senha, storage seguro e testes de login.

3. Frontend ainda usa autenticacao local
   - `DtudoSite/src/hooks/useAuth.js` mantem usuarios/senhas no cliente.
   - Sugestao: integrar com backend de auth ou remover esse fluxo em telas que parecam producao.

4. Scripts npm ainda dependem de ApiNode
   - `package.json` roda `json-server` e proxy dentro de `ApiNode`.
   - Sugestao: separar scripts legados dos scripts atuais e criar comando dedicado para stack C# (`ApiMyAnimes`, `ApiMyAnimeList`, `DtudoSite`).

5. Configuracoes locais hardcoded
   - Corrigido: WinAppDtudo agora le `WinAppDtudo/appsettings.json` e variaveis `DTUDO_*`.

6. Certificados HTTPS ignorados no WinAppDtudo
   - `DangerousAcceptAnyServerCertificateValidator` aparece em servicos HTTP.
   - Sugestao: limitar a Debug/Development ou configurar certificados locais confiaveis.

7. Swagger/XML pode gerar muito aviso de documentacao em rebuild completo
   - A primeira compilacao completa expos muitos `CS1591` em DTOs/modelos publicos.
   - Sugestao: completar XML docs dos DTOs/modelos publicos ou desabilitar `NoWarn`/XML docs onde nao forem requisito.

8. ApiMyAnimeList tem CORS fixo
   - `Program.cs` permite somente `http://localhost:5173`.
   - Sugestao: espelhar o padrao configuravel usado em `ApiMyAnimes`.

9. Possivel divergencia de porta
   - Corrigido: ApiMyAnimeList foi padronizada em `https://localhost:7146` no WinApp, README, `.http` e `launchSettings`.

10. Testes automatizados ausentes ou nao evidentes
    - Nao encontrei projetos de teste dedicados para APIs/WinApp.
    - Sugestao: criar testes unitarios para mappers, services HTTP com handlers fake e endpoints principais.

## Melhorias recomendadas

- Criar camada compartilhada de contratos MyAnimeList em `LibDtudo.Shared`, evitando DTOs compatibilidade duplicados entre API e WinApp.
- Padronizar nomes `MalId` em payloads, query strings e documentacao.
- Adicionar health endpoint em `ApiMyAnimeList` para o auto-start do WinApp, em vez de usar uma busca real por `test`.
- Adicionar retry/backoff configuravel no `MyAnimeListClient`, respeitando `MaxRetries` que ja existe em configuracao.
- Revisar `TextosTemp.md` periodicamente, pois ele parece ser anotacao temporaria e pode ficar defasado em relacao ao runtime.

## Verificacao

- `dotnet build Dtudo2026.slnx --no-restore`: passou com 0 avisos e 0 erros na verificacao final.
- `dotnet test Dtudo2026.slnx --no-restore`: passou com 7 testes.
- `npm run build`: passou com build Vite de producao.
- A busca por `Jikan`, `jikan` e `ApiJikan` em arquivos de fonte ficou restrita as migrations historicas do EF.

## Implementado Depois Da Auditoria

- `MyAnimeList:ClientId` saiu de `appsettings.json`; usar user-secrets ou variavel `MyAnimeList__ClientId`.
- Criada autenticacao local em `ApiMyAnimes` com PBKDF2 e usuarios em `App_Data/*.json`.
- Frontend passou a usar `/apiLocal/Auth/register` e `/apiLocal/Auth/login`.
- WinApp passou a autenticar e cadastrar usuarios via `ApiMyAnimes`.
- Scripts npm foram separados entre stack atual (`serv`) e legado (`legacy:*`).
- URLs do WinApp foram centralizadas em `WinAppDtudo/appsettings.json` e variaveis `DTUDO_*`.
- Aceite de certificado invalido no WinApp ficou limitado a compilacao `DEBUG`.
- CORS da `ApiMyAnimeList` passou a ser configuravel.
- `ApiMyAnimeList/health` foi criado e o auto-start do WinApp passou a usa-lo.
- Contratos MyAnimeList compartilhados foram movidos para `LibDtudo.Shared`.
- Projetos de testes foram adicionados em `tests/`.
