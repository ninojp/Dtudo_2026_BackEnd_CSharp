# WinAppDtudo

O `WinAppDtudo` é o aplicativo desktop (WinForms, .NET 10, Dark Mode) do Dtudo2026. Ele é o cliente administrativo do catálogo de animes (MyAnimes), da coleção musical (MyMusicX), do monitoramento local de coleções e do painel de saúde da solução. Todo dado de negócio passa por APIs autenticadas; o aplicativo não acessa banco de dados.

Visão geral da solução: [00_Solucao.md](00_Solucao.md) · segurança: [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md) · operação: [02_Solucao_Operacao.md](02_Solucao_Operacao.md).

## Arquitetura

| Item | Valor |
| --- | --- |
| Projeto | `WinAppDtudo\WinAppDtudo.csproj` (`net10.0-windows10.0.26100.0`, WinExe) |
| UI | WinForms, DPI `PerMonitorV2`, `Application.SetColorMode(Dark)` e `ThemeManager.Initialize()` no `Program.cs` |
| Pacotes | WebView2 (login e site embutido) e SkiaSharp |
| Compartilhado | referencia [LibDtudo.Shared](30_LibDtudo.Shared.md) (DTOs, contratos de monitoramento) |
| Testes | `tests\WinAppDtudo.Tests` |

Pastas principais:

| Pasta | Conteúdo |
| --- | --- |
| `Forms\` | `Frm_WinAppLogin` (WebView2), `Frm_DiscogsImport`, `Frm_ProgressoOperacao`, `Frm_Questao`, `Frm_StatusPastas` |
| `FormsUC\` | telas hospedadas em abas (ver abaixo) |
| `Controls\` | `UC_AnimeCard`, `UC_MiniAnimeCard`, `AccentLabel`, `IconTextLabel`, `DarkTabControl`, `TabWorkspace` |
| `Services\` | clientes HTTP, autenticação, startup/health de APIs, importação, migração, diagnóstico |
| `Theme` / Dark Mode | `ThemeManager`, `DarkModeColors`, `DarkMessageBox`, `DarkInputDialog`, `DarkSelectionDialog`, `DarkToolStripRenderer`, `WindowsDarkMode` |

Regras visuais: o aplicativo é inteiramente escuro; novas telas devem reutilizar `ThemeManager` e `DarkModeColors` (nada de painéis claros). Rótulos fixos usam `AccentLabel` (Gold padrão) e os dados seguem a cor da fonte.

### Janela principal (`Frm_WinAppDtudo`)

- Menus: **Arquivo** (Monitoramento local, Cadastrar Usuário, Painel de Saúde, Conectar, Desconectar, Sair), **MyAnimes** (Buscar Anime, Analizar Pastas), **DtudoSite** (DtudoWebView2, Site no Navegador), **MyMusicX** e **NinoTI**.
- Abas planas gerenciadas por `TabWorkspace`/`DarkTabControl`, reaproveitadas por chave (`MyAnimesTabController` orquestra as abas de MyAnimes). A aba fixa "Início" é `FUC_Inicio`.
- Os menus ficam desabilitados até a autenticação; o rodapé (`UpdateStatusFooter`) mostra estado de conexão e saúde.

### Telas e controles principais

| Tela | Função |
| --- | --- |
| `FUC_BuscarAnime` | busca unificada: DB Local ([ApiMyAnimes](22_ApiMyAnimes.md)) e [ApiMyAnimeList](23_ApiMyAnimeList.md) (`IAnimeSearchSource`) |
| `FUC_DetalhesAnime` | detalhes do anime nas duas fontes (layout compartilhado), relações e mini cards (`UC_MiniAnimeCard.FormatoPoster`) |
| `FUC_EditarAnime` / `FUC_EditarMyAnime` | edição de Anime e MyAnime (coleção interna `MyAnimeId`) |
| `FUC_MyAnimeDetalhes` | detalhes da coleção local; botões "Monitorar Estrutura" e "Atualizar status das pastas" |
| `FUC_Monitoramento` | monitoramento local de coleções (geral e por MyAnime) — ver [24_ApiFileStorage.md](24_ApiFileStorage.md); botão "Atualizar status das pastas" (coleção) / "Atualizar todas" (geral) |
| `Frm_StatusPastas` | diálogo escuro com o progresso e o resultado da verificação de pastas (ver "Status das pastas") |
| `FUC_PainelSaude` | painel de saúde das APIs |
| `FUC_IdentityAdministration` | administração de usuários ([ApiIdentity](21_ApiIdentity.md)) |
| `FUC_MyMusicX` | coleção musical via [ApiMusicX](25_ApiMusicX.md) e importação Discogs ([ApiDiscogs](26_ApiDiscogs.md)) |
| `FUC_DtudoWebView` / `FUC_NinoTI` | site embutido ([DtudoSite](10_DtudoSite.md)) |

Regras de negócio de exibição: no DB Local, relações e navegação usam `MyAnimeId` (nunca as relações externas da ApiMyAnimeList); a ApiMyAnimeList é a fonte exclusiva para busca, capas, detalhes e relações externas.

## Autenticação

O WinApp é cliente público OAuth (`dtudo-winapp`, sem segredo) do [ApiIdentity](21_ApiIdentity.md), usando Authorization Code + PKCE (S256).

1. `WinAppPkceBrowserClient` gera `code_verifier`, `state` e `code_challenge`, abre um `TcpListener` em loopback na porta do `RedirectUri` e monta `connect/authorize` com `scope` e vários `resource`.
2. A tela de login é o `Frm_WinAppLogin` (WebView2 embutido); o callback loopback valida `state` em tempo constante e devolve o `code`.
3. O código é trocado em `connect/token`. Timeout de autenticação configurável (padrão 300 s, limitado entre 30 e 600 s); porta ocupada gera erro explícito.
4. `WinAppAuthenticationService` exige no access token a role `Superadministrador` e a permission `catalog.write`; sem elas o login é recusado.
5. Cria sessão de segurança em `identity/security/sessions` (com *rebind* do token em `/{id}/token`), renova por refresh token, revoga em `connect/revocation` e encerra com `DELETE identity/security/sessions/{id}`.
6. `SendAuthenticatedAsync` anexa o bearer e repete uma vez após 401 com refresh; um `SemaphoreSlim` serializa renovações.

Armazenamento: `ProtectedTokenStore` grava o conjunto de tokens protegido com DPAPI (`CurrentUser` + entropia), com escrita atômica (arquivo temporário + `File.Move`), em `%LocalAppData%\Dtudo2026\WinAppDtudo\` (sobrescrevível por `DTUDO_IDENTITY_SESSION_STORE`); é apagado no logout.

Escopos solicitados: `openid profile offline_access identity.login identity.provision catalog.read catalog.write catalog.delete filesystem.command health.read`. Recursos (audiences): `urn:dtudo:api-my-animes`, `api-my-animelist`, `api-file-storage`, `api-musicx` e `api-discogs`.

## Consumo de APIs

| API | Uso no WinApp | Cliente |
| --- | --- | --- |
| [ApiIdentity](21_ApiIdentity.md) | login, sessão, administração de usuários, health | `WinAppAuthenticationService`, `IdentityAdministrationApiClient` |
| [ApiMyAnimes](22_ApiMyAnimes.md) | catálogo (`apiLocal/Anime`, `apiLocal/MyAnime`, `apiLocal/catalog-migration/*`), vínculo de pasta, status das pastas (`folder-status/*`) | `ApiMyAnimesService`, `AnimeFolderStatusApiClient` |
| [ApiMyAnimeList](23_ApiMyAnimeList.md) | busca, detalhes, capas e relações externas | `MyAnimeListApiService` |
| [ApiMusicX](25_ApiMusicX.md) | coleções, artistas, álbuns | `ApiMusicXService` |
| [ApiDiscogs](26_ApiDiscogs.md) | busca/importação Discogs | `ApiDiscogsService`, `ApiDiscogsImportCoordinator` |
| [ApiFileStorage](24_ApiFileStorage.md) | monitoramento local de coleções | `MonitoringApiClient` |
| [DtudoGateway](20_DtudoGateway.md) | borda do site (startup/health) | `DtudoGatewayStartupService` |

Padrões comuns: bearer automático e retry após 401; mensagens de erro de rede tratadas globalmente em `Program.cs`; falhas 504 da ApiMyAnimeList tratadas com tentativas (3 tentativas com espera de 2 s na importação) e modo degradado.

## Integração com a ApiMusicX

- O WinApp só chama a ApiMusicX por `ApiMusicXService` (`apiLocal/collections`, `apiLocal/artists` etc., timeout de 120 s, `IProgress<string>` para feedback textual).
- Token do usuário com audience `urn:dtudo:api-musicx` e permissões `catalog.read/write/delete` e `health.read`.
- `ApiMusicXHealthCheckService` usa `apiLocal/Health`. `ApiMusicXStartupService` confirma a saúde, e, se a API não estiver de pé e o processo não existir, inicia `dotnet run` do projeto `ApiMusicX` (Development, URL de `AutoStartUrl`), com `SemaphoreSlim` compartilhado e nova confirmação antes de iniciar; timeout de 20 s.
- `LegacyMusicCollectionMigrationService` importa coleções legadas (JSON) enviando-as pela ApiMusicX, com feedback por etapas.
- Testes usam `HttpMessageHandler` fake e `ProtectedTokenStore` temporário (`ApiMusicXServiceTests`).

## Health monitoring e inicialização local

- `WinAppHealthMonitoringService` roda a cada 60 s, apenas após autenticar. Probes autenticados: ApiIdentity `health/ready`, ApiMyAnimes e ApiMusicX `apiLocal/Health`, ApiMyAnimeList `ApiMyAnimeList/health`, ApiDiscogs `ApiDiscogs/health`, ApiFileStorage `api/file-storage/health`. HTTP 401/403 conta como "processo acessível".
- Sondas adicionais: validade dos certificados TLS das seis APIs (`TlsCertificateHealthProbe`), backup diário (`WinAppBackupHealthProbe`: manifesto de no máximo 24 h, aviso após 20 h) e expiração da sessão/access token em até 5 min.
- Estados: Healthy, Warning, Critical, Unavailable. `WindowsHealthNotificationService` emite notificação do Windows para Critical; `FUC_PainelSaude` mostra o detalhe.
- Após o login, `EnsureMonitoredServicesReadyAsync` aciona em paralelo os `*StartupService` (ApiMyAnimes, ApiMusicX, ApiMyAnimeList, ApiDiscogs, ApiFileStorage). Cada serviço verifica o contrato/saúde, procura a raiz da solução (`Dtudo2026.slnx`) e só inicia o projeto se não houver processo. Falhas são registradas em `StartupDiagnostics` sem bloquear a UI. `ApiFileStorageStartupService` exige o contrato de startup v2 (`GET api/file-storage/startup`). `DtudoSiteStartupService` sobe o site Vite (npm) e o navegador, sem SQL.

## Fluxo de importação (feedback em tempo real)

`MnI_AnalizarPastas` → `MyAnimesTabController.AnalizarPastasAsync`:

1. O usuário escolhe a pasta raiz (`FolderBrowserDialog`); `AnalizadorDeEstruturas` lista as pastas MyAnime e seus animes (apenas leitura local).
2. `Frm_ProgressoOperacao` mostra mensagens detalhadas por etapa (análise 0–50 %, cadastro de MyAnime 50–100 %), não apenas porcentagem.
3. MyAnime criado por `PUT apiLocal/catalog-migration/my-animes/by-title` (idempotente por título normalizado); já existentes são ignorados e contabilizados.
4. Diálogo de confirmação pergunta se os animes da coleção devem ser salvos.
5. `ImportadorAnimesMyAnimeService` importa o escopo direto (anime e relações diretas, sem ampliar a coleção): detalhes e relações na ApiMyAnimeList (até 3 tentativas), `POST apiLocal/Anime` (409 = já existe), `PUT apiLocal/catalog-migration/animes/{malId}/my-anime` (associação) e atualização das relações. Falha persistente de detalhes grava registro mínimo em modo de degradação (`Source = "Fallback"`).
6. Resumo final (criados/ignorados/falhas) e log de erros em `LogsImportacao\` ao lado do executável; opção de abrir o MyAnime criado.

Mensagens de duplicidade seguem o formato "MyAnime ID: Nome do anime" + quebra de linha + "O MyAnime já foi cadastrado!".

## Monitoramento local

A classificação de vídeo (cor verde e unidade MB) reutiliza `EpisodeVideoPolicy.VideoExtensions` de [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md), a mesma lista usada pela ApiFileStorage para contar vídeos da pasta do anime; não há cópia local (a lista de 23 extensões e o comportamento visual não mudaram). O vínculo MyAnime ↔ pasta usa `ApiMyAnimesService`.

## Status das pastas (ícones dos cards)

O proprietário dispara, pelo WinApp, a verificação que a [ApiMyAnimes](22_ApiMyAnimes.md) executa com apoio da [ApiFileStorage](24_ApiFileStorage.md) e que alimenta os ícones de pasta (cinza/azul/verde) dos cards do site. O WinApp não acessa disco nem SQL para isso: tudo passa pela API.

**Pontos de entrada**

- `FUC_MyAnimeDetalhes` → "Atualizar status das pastas" (uma coleção MyAnime).
- `FUC_Monitoramento` → "Atualizar status das pastas" (modo coleção) e "Atualizar todas" (modo geral). "Atualizar todas" pede confirmação (padrão: Não), pois percorre todas as coleções.
- Ambos chamam `Frm_StatusPastas.ExecutarAsync`, com guarda contra execução dupla no WinApp.

**Camadas (sem UI nas duas primeiras, cobertas por testes)**

| Tipo | Papel |
| --- | --- |
| `AnimeFolderStatusApiClient` (`IAnimeFolderStatusApi`) | cliente HTTP autenticado (mesmo bearer/PKCE dos demais clientes; 401 faz refresh da sessão e repete uma vez). Rotas `apiLocal/MyAnime/{id}/folder-status/refresh`, `folder-status/refresh`, `folder-status/runs[/{runId}]` (cursor `afterId`, `take` 1–500) e `.../cancel`; consulta em lote `POST apiLocal/Anime/status` (até 200 ids por chamada) para `expected` e última verificação |
| `AnimeFolderRefreshCoordinator` | inicia (ou anexa à execução ativa em 409), consulta o progresso por cursor, tolera falhas transitórias (até 5 seguidas, "tentando novamente (n/5)"), enriquece cada item com título/`expected`/data, cancela e devolve resultado parcial |
| `AnimeFolderStatusPresenter` | textos, cores, filtro e resumo ("Concluído: 70 completos, 10 parciais, 7 sem pasta") |
| `Frm_StatusPastas` | diálogo `CustomFormNoBorder` escuro, redimensionável (até 96 % da área útil, DPI por monitor) |

**Feedback por etapa:** "Planejando…", "Contando os vídeos de N animes; aguardando o primeiro resultado…" e, por item, "Coleção X: 'Anime' verificado (12/87) — completo (26/26 vídeos)". O servidor só reporta itens concluídos, por isso as mensagens dizem "verificado". A barra fica indeterminada até haver total e depois passa a contínua.

**Resultado por anime:** grade (virtual) com Coleção, MAL ID, Anime, Estado (colorido), Vídeos/Episódios, Última verificação e Resultado; filtro Todos/Completo/Parcial/Sem pasta/Atenção. Cores (em `DarkModeColors`, iguais às do site): sem pasta `#8A94A6` (cinza), parcial `#4EB7E6` (azul), completo `#00C86F` (verde). Itens indisponíveis ou com falha mantêm o status anterior (exibido com a observação "status anterior mantido").

**Cancelamento:** "Cancelar verificação" (ou fechar o diálogo) chama `POST .../cancel` e aguarda o estado final (até 30 s), mostrando o resultado parcial ("Cancelado: …").

**Erros (mensagens claras no diálogo e em `DarkMessageBox`)**

| Situação | Tratamento |
| --- | --- |
| API/rede/timeout, 502–504 | "ApiMyAnimes indisponível"; durante o acompanhamento, novas tentativas antes de desistir |
| 401 / sem sessão | pede para reconectar (Arquivo > Conectar) |
| 403 | falta a permissão `catalog.write` |
| 404 | coleção inexistente ou execução que deixou de existir |
| 409 | já existe execução; o WinApp anexa a ela e acompanha |
| 503 | ApiFileStorage fora do ar ou raiz de coleções não configurada/indisponível |
| JSON inválido | resposta inesperada da API |

**Contratos:** os DTOs de refresh e do status em lote vêm de `LibDtudo.Shared\Dtos\AnimeFolderStatusContracts.cs` (o mesmo código usado pela ApiMyAnimes, sem cópia no WinApp; ver [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md)). Só o `AnimeFolderStatusInfo` (visão do WinApp com `MalId`) e as exceções/tipos de falha ficam em `AnimeFolderStatusModels.cs`.

## Empacotamento MSIX, atualização e rollback

| Artefato | Papel |
| --- | --- |
| `WinAppDtudo\Package.appxmanifest.template` | identidade `Dtudo.WinAppDtudo`, FullTrust, `win-x64`, Windows 10.0.26100 |
| `scripts\Invoke-DtudoMsixPackage.ps1` | `Prepare`/`Package`/`Validate`; assina só por Certificate Store com thumbprint explícito |
| `scripts\Invoke-DtudoMsixDeployment.ps1` | instala/atualiza (apenas versão maior) e faz rollback somente para pacote anterior, revalidando hash e assinatura; estado em `deployment-state.json` fora do repositório |
| `scripts\Invoke-DtudoReleaseRunnerHardening.ps1` | endurecimento do runner de release |
| `scripts\Test-DtudoEtapa29.ps1` | testes de segurança do fluxo MSIX |
| `.github\workflows\msix-release.yml` | tag `v*.*.*` ou dispatch em `main`; runner `[self-hosted, windows, dtudo-release]`; environment `dtudo-msix-release`; sem gatilho de PR; actions fixadas por SHA; artefatos sem sobrescrita |

Variáveis e secrets exigidos (valores nunca versionados): `DTUDO_MSIX_STATE_ROOT`, `DTUDO_RELEASE_RUNNER_ROOT`, `DTUDO_RELEASE_RUNNER_ACCOUNT`, `DTUDO_MSIX_PUBLISHER` e o secret `DTUDO_MSIX_SIGNING_THUMBPRINT`. Detalhes operacionais em [02_Solucao_Operacao.md](02_Solucao_Operacao.md).

## Configuração

`appsettings.json` (sem segredos) é procurado em `AppContext.BaseDirectory` e depois em `WinAppDtudo\` na raiz da solução. `AppConfigurationService` centraliza as leituras; variáveis de ambiente prevalecem.

| Seção | Conteúdo |
| --- | --- |
| `Apis` (BaseUrl) | ApiMyAnimes, ApiMusicX (+`AutoStartUrl`), ApiMyAnimeList, ApiDiscogs, ApiIdentity, DtudoGateway, ApiFileStorage — todas em `localhost` com portas dev |
| `Identity` | `ClientId`, `RedirectUri` (loopback `127.0.0.1`), `Scopes`, `Resources`, `AuthenticationTimeoutSeconds` |
| `DtudoSite` | `StartUrl`, `StartupTimeoutSeconds` |
| `Monitoring` | `HealthProbeTimeoutSeconds` (5) |
| `Http` | `AllowInvalidCertificates` — só tem efeito em build DEBUG |

Variáveis: `DTUDO_API_*_BASE_URL`, `DTUDO_API_MYANIMELIST/DISCOGS/MUSICX_AUTOSTART_URL`, `DTUDO_GATEWAY_BASE_URL`, `DTUDO_IDENTITY_CLIENT_ID`, `DTUDO_IDENTITY_SESSION_STORE`, `DTUDO_BACKUP_ROOT`, `DTUDO_SITE_START_URL`, `DTUDO_SITE_DIRECTORY`, `DTUDO_GOOGLE_CHROME_PATH`, `DTUDO_NPM_PATH`, `DTUDO_ALLOW_INVALID_CERTIFICATES`.

## Testes

Projeto `tests\WinAppDtudo.Tests` (executar com `dotnet test`). Áreas cobertas:

| Área | Classes |
| --- | --- |
| Autenticação | `WinAppPkceBrowserClientTests` (PKCE/state/callback/porta ocupada), `WinAppAuthenticationServiceTests` (refresh, rebind, logout), `ProtectedTokenStoreTests` (DPAPI) |
| APIs | `ApiMyAnimesServiceTests`, `AnimeFolderStatusApiClientTests`, `AnimeFolderRefreshCoordinatorTests` (inclui `AnimeFolderStatusPresenterTests`), `ApiMusicXServiceTests`, `ApiDiscogsServiceTests`, `ApiDiscogsImportCoordinatorTests`, `ConversorAnimeDtoServiceTests` |
| Saúde | `WinAppHealthMonitoringServiceTests` |
| Migração | `LegacyMusicCollectionMigrationServiceTests` |
| UI/layout | `AnimeDetailLayoutTests`, `AnimeSearch*Tests`, `UnifiedAnimeSearchTests`, `MainWindowTabsTests`, `TabWorkspaceTests`, `DarkTabControlTests`, `MigratedTabContentTests`, `ImageLoaderServiceTests` |

## Matriz: acesso direto → API

| Antes | Agora |
| --- | --- |
| SQL/EF/LocalDB direto no WinApp | removido; catálogo via [ApiMyAnimes](22_ApiMyAnimes.md), música via [ApiMusicX](25_ApiMusicX.md) |
| Consulta externa antiga de animes | [ApiMyAnimeList](23_ApiMyAnimeList.md) (fonte exclusiva) |
| Proxy Discogs antigo | [ApiDiscogs](26_ApiDiscogs.md) / [DtudoGateway](20_DtudoGateway.md) |
| Login local/credenciais no app | PKCE contra [ApiIdentity](21_ApiIdentity.md) |
| Inventário/monitoramento de pastas lido pelo app | [ApiFileStorage](24_ApiFileStorage.md) (`api/file-storage/monitoring`) |
| Envio/exclusão de arquivos via API de armazenamento | endpoints removidos; ver divergências |
| Inicialização do site com `sqllocaldb` | removida; só npm/Chrome |

## Pendências/Divergências

- **Login no navegador do sistema × WebView2:** a documentação de segurança antiga fala em navegador do sistema; o código usa `Frm_WinAppLogin` (WebView2 embutido), mantendo o callback loopback.
- **Acesso a arquivos não é zero:** o WinApp não usa SQL, mas ainda toca o disco local: `CriadorDeEstruturas` cria pastas e capas (`FileMode.CreateNew`, nunca sobrescreve) na pasta escolhida; `AnalizadorDeEstruturas` lê a pasta escolhida; `ImportadorAnimesMyAnimeService` grava `LogsImportacao`; `FUC_Monitoramento` abre pastas no Explorer e exporta TXT/CSV; DPAPI, configuração, backup (leitura de manifesto) e `StartupDiagnostics` também usam arquivos. A documentação da etapa de remoção de arquivos diz que esses fluxos foram migrados para a ApiFileStorage; os endpoints `import`, `delete*` e `export/*` foram removidos e `FileStorageApiClient` não existe mais.
- **Defaults de código:** `AppConfigurationService` (usado só sem `appsettings.json`) não inclui o escopo `filesystem.command` nem o recurso `urn:dtudo:api-file-storage`, e `DtudoSite.StartUrl` padrão é `/animes` (o `appsettings.json` usa `/`).
- **MSIX não validado ponta a ponta:** scripts, template e workflow existem, mas a emissão real depende de `makeappx`/`signtool` e de runner dedicado; não há evidência de release publicada.
- **Status das pastas sem teste de UI:** `Frm_StatusPastas` e os botões não têm teste automatizado (só cliente, coordenador e apresentador); o caminho 401 via HTTP não é exercitado (dispararia refresh real no ApiIdentity); o menu principal não ganhou item novo (as ações ficam nas telas de detalhe e monitoramento).
- **Contagens de testes** em documentos históricos estão desatualizadas e não foram repetidas aqui.
- **Dúvida aberta:** manter ou migrar para API a criação de estruturas de pastas e capas (`CriadorDeEstruturas`), dado o princípio de "somente APIs".
