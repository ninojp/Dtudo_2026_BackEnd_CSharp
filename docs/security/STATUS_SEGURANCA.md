# Status da Implementacao de Seguranca

## Estado geral

- Etapa atual: 30 (Reprovada no gate; controles locais reexecutados)
- Ultima etapa concluida: 28 no Development local
- Proxima etapa permitida: desbloquear e concluir a Etapa 27; depois concluir a validacao protegida da Etapa 29 e reexecutar o Gate 30; nao publicar nem iniciar etapa seguinte.
- Bloqueios globais: o Gate 30 foi reprovado porque a Etapa 27 ainda requer dominio real, IIS/WebAdministration, certificado ACME, contas de servico, listeners internos e regras de firewall no host de homologacao, e a Etapa 29 ainda requer Windows SDK com `makeappx`/`signtool`, certificado interno confiavel, runner dedicado, environment com aprovacao e teste real de instalacao/rollback. Development foi o unico ambiente tocado; Production permaneceu intocada. O repositorio e pessoal, somente o proprietario faz commits, e regras administrativas do GitHub continuam diferidas.

## Etapas

| Etapa | Estado | Evidencia principal | Data UTC |
| --- | --- | --- | --- |
| 01 | Concluida | Quatro documentos de seguranca consistentes e validacao executada | 2026-08-06 |
| 02 | Concluida | Varredura de conteudo/historico, fontes externas e falha fechada validadas | 2026-08-06 |
| 03 | Concluida | Workflow versionado, build/testes/lint e auditorias locais aprovados; controles administrativos do GitHub diferidos por decisao do proprietario | 2026-08-06 |
| 04 | Concluida | Serilog/Seq, correlacao, redacao e testes das duas APIs implementados | 2026-08-06 |
| 05 | Concluida | SecurityAuditEvents append-only, API interna, migration e 8 testes aprovados | 2026-08-06 |
| 06 | Concluida | Runner idempotente, snapshot real de 23 itens, restauracao isolada com `DBCC CHECKDB` e retencao de 30 dias validados | 2026-08-06 |
| 07 | Concluida | Development workstation aplicado e reaplicado sem elevacao: 18 Passed, 4 NotChecked, 0 Blocked, 0 Failed; baseline de promocao e rollback declarados sem segredos | 2026-08-06 |
| 08 | Concluida | Amostras locais de configuracao, pipeline, correlacao/redacao, auditoria, backup/restauracao e negativos aprovadas; escopo pessoal sem regras administrativas do GitHub aceito | 2026-08-06 |
| 09 | Concluida | ApiIdentity isolada com Identity/OpenIddict, migration e rollback LocalDB comprovados | 2026-08-06 |
| 10 | Concluida | Catalogo central de permissoes/papeis, maioridade, termos versionados, migration e 8 testes LocalDB aprovados | 2026-08-06 |
| 11 | Concluida | Bootstrap local unico, provisionamento auditavel, segredo inicial com hash/expiracao/uso unico/revogacao e rate limiting validados | 2026-08-06 |
| 12 | Concluida | Passkeys/Fido2, TOTP, recovery codes, step-up, sessoes/dispositivos, recovery local, snapshots protegidos, restauracao unica e 33 testes ApiIdentity aprovados | 2026-08-06 |
| 13 | Concluida | Tokens opacos hash-only, refresh rotation/reuse detection, introspecao, expiracao de 30 dias, bloqueio e revogacao imediata comprovados em 6 cenarios focados e suite completa | 2026-08-06 |
| 14 | Concluida | Bearer issuer/audience, fallback deny-by-default, politicas de escopo/permissao, classificacao de endpoints, Swagger/OpenAPI restritos e testes positivos/negativos das duas APIs | 2026-08-06 |
| 15 | Concluida | Client Credentials + mTLS, binding por client ID/certificado/escopo, Certificate Store, ACL com snapshot/rollback, overlap de rotacao e negativos; suites 102/102 | 2026-08-07 |
| 16 | Concluida | `DtudoGateway` com YARP, OIDC Code + PKCE, cookie server-side, antiforgery, allowlist, rotas explicitas e 10 testes focados aprovados | 2026-08-07 |
| 17 | Concluida | `docs/security/ETAPA_17_DTUDOSITE_BFF.md`; lint/build do frontend; scan de tokens; gateway 10/10 | 2026-08-07 |
| 18 | Concluida | `docs/security/ETAPA_18_RECURSOS_PESSOAIS_LGPD.md`; owner authorization, privacidade LGPD, migrations e suite ApiIdentity 55/55 | 2026-08-07 |
| 19 | Implementada; homologacao externa pendente | `docs/security/ETAPA_19_IDENTIDADE_WINAPP.md`; `IdentitySecurityServiceTests` `19/19` (incluindo binding OIDC e administracao) e WinApp `10/10` aprovados; os bancos fixos ausentes na execucao original foram preparados e revalidados no gate 20 | 2026-08-07 |
| 20 | Concluida no Development local | `docs/security/ETAPA_20_GATE_IDENTIDADE.md`; remocao do login legado; `ApiIdentity.Tests` `57/57`; mTLS `10/10`; APIs, Gateway, Site, LGPD e WinApp revalidados | 2026-08-07 |
| 21 | Concluida | `docs/security/ETAPA_21_API_FILE_STORAGE.md`; suite `ApiFileStorage.Tests` 29 total, 29 aprovados, 0 ignorados, 0 falhas, incluindo symlink real e ACL | 2026-08-07 |
| 22 | Concluida | `docs/security/ETAPA_22_QUARENTENA_ARQUIVOS.md`; quarentena, limites, hash, scanner fail-closed, promocao, idempotencia, lixeira/reconciliacao; ciclo focado 9/9 e suite 31/31 | 2026-08-07 |
| 23 | Concluida | matriz de migracao, contratos minimos, autorizacao e testes de ApiMyAnimes/ApiFileStorage | 2026-08-07 |
| 24 | Concluida no Development local | `docs/security/ETAPA_24_REMOCAO_SQL_WINAPP.md`; cliente autenticado, comandos idempotentes, remocao do LocalDB e varredura SQL/EF negativa | 2026-08-07 |
| 25 | Concluida no Development local | `docs/security/ETAPA_25_REMOCAO_ARQUIVOS_WINAPP.md`; exportacao por ObjectId, feedback, previa/step-up/lixeira, suites 33/33 e 16/16, varredura negativa de caminhos/ACL | 2026-08-07 |
| 26 | Concluida no Development local | `docs/security/ETAPA_26_RESILIENCIA_MAL.md`; pipeline oficial com timeout/retry jitter/circuit breaker, allowlist egress/SSRF, correlacao e simulacoes 21/21 | 2026-08-07 |
| 27 | Bloqueada | `docs/security/ETAPA_27_IIS_TLS.md`; gateway catalog-only, build sem superficies proibidas e negativos locais aprovados; IIS/TLS/firewall/renovacao externos indisponiveis | 2026-08-07 |
| 28 | Concluida no Development local | `docs/security/ETAPA_28_PAINEL_SAUDE_ALERTAS.md`; monitoramento autenticado, endpoint operacional de storage, estados/timeout, pouco espaco critico, notificacao e DPI; suites 23/23 e 36/36 | 2026-08-07 |
| 29 | Bloqueada | `docs/security/ETAPA_29_MSIX_RUNNER.md`; workflow, empacotador, estado de update/rollback e hardening local; suite 10/10 e build Release aprovados; pacote/assinatura/runner reais indisponiveis | 2026-08-07 |
| 30 | Reprovada no gate | `docs/security/ETAPA_30_GATE_FINAL.md`; negativos locais, restauracao, revogacao, isolamento, 504, portas e pacote sintetico reexecutados; IIS/TLS/ACME/firewall e MSIX real bloqueados | 2026-08-07 |

### Ultima execucao

- Objetivo: executar exclusivamente o Gate Final da Etapa 30, sem implementar funcionalidades.
- Evidencia principal: `docs/security/ETAPA_30_GATE_FINAL.md`, `docs/security/ETAPA_27_IIS_TLS.md`, `docs/security/ETAPA_29_MSIX_RUNNER.md`, runbook de backup e suites focadas de identidade, arquivos, MAL e gateway.
- Arquivos alterados neste checkpoint: `docs/security/ETAPA_30_GATE_FINAL.md` e este status. Nenhum codigo, banco ativo, Production, certificado, chave, runner ou environment foi alterado.
- Testes: `ApiIdentity.Tests` filtrado `51/51`; cenarios de comprometimento/revogacao `3/3`; WinApp autenticacao/protecao `10/10`; ApiFileStorage focado `35/35`; MAL resiliencia `8/8`; Gateway/catalogo `17/17`; `RestoreVerify` real com 22 arquivos e 1 banco temporario; negativos de manifesto/payload adulterado rejeitados; `Test-DtudoEtapa29.ps1` `10/10`; varredura local redigida de conteudo/historico sem padrao detectado.
- Resultado: controles locais aprovados no escopo exercitado, mas Gate 30 `Reprovado para publicacao`. A validacao da Etapa 27 marcou build catalog-only como `Passed` e hostname/IIS/bindings/certificado/ACME/firewall como `Blocked`; a Etapa 29 nao possui `makeappx`/`signtool` nem pacote, instalacao, update ou rollback reais.
- Riscos residuais: scanner Defender/AMSI real, OIDC/mTLS live, dominio/TLS/firewall, runner/environment GitHub, secret scanning operacional e copia de backup fora do host continuam sem evidencia neste workstation; ver documento do gate.
- Rollback: este gate somente acrescentou evidencia documental; remover o documento e restaurar este bloco/linha de status. Nao remover snapshots, relatorios, bancos, certificados, chaves ou estados operacionais.
- Acoes manuais: concluir Etapa 27 em homologacao real, concluir validacao protegida da Etapa 29, habilitar/reexecutar controles administrativos de secret scanning e repetir o Gate 30 antes de qualquer publicacao.
- Proxima etapa: desbloquear e concluir a Etapa 27; depois concluir a validacao protegida da Etapa 29 e reexecutar o Gate 30; nao iniciar etapa seguinte.

### Execucao anterior (Etapa 28)

- Objetivo: executar exclusivamente a Etapa 28 no Development local, sem iniciar a Etapa 29.
- Evidencia principal: `docs/security/ETAPA_28_PAINEL_SAUDE_ALERTAS.md`, `WinAppDtudo/Services/WinAppHealthMonitoringService.cs`, `WinAppDtudo/Forms/Frm_HealthDashboard.cs` e `ApiFileStorage/Controllers/FileStorageHealthController.cs`.
- Arquivos alterados neste checkpoint: probes autenticados e estados do WinApp; configuracao do escopo `health.read`; endpoint operacional restrito da ApiFileStorage; painel Dark Mode, menu, polling, NotifyIcon e testes. Nenhum banco, migration, Production, certificado real ou segredo foi alterado.
- Consultas: Identity `/health/ready`, ApiMyAnimes `/apiLocal/Health`, ApiMyAnimeList `/ApiMyAnimeList/health` e ApiFileStorage `/api/file-storage/health` usam bearer da sessao WinApp e timeout por fonte. O storage devolve somente estado, bytes, scanner e contagens; caminhos fisicos nao sao serializados.
- Alertas: o painel exibe servicos, sessao/scanner, certificados TLS, espaco, backup diario e quarentena. A notificacao Windows deduplica somente estados criticos e usa texto generico; timeout ou fonte indisponivel vira estado local e nao encerra o WinApp.
- Testes: `WinAppDtudo.Tests` passou `23/23`, incluindo probes/bearer, timeout, backup, sessao em aviso, pouco espaco critico, politica de notificacao e formulario DPI; `ApiFileStorage.Tests` passou `36/36`, incluindo `401`, `403`, resposta autorizada e ausencia de caminho; `dotnet build .\\ApiIdentity\\ApiIdentity.csproj --no-restore` passou.
- Resultado: a implementacao local da Etapa 28 esta concluida. A homologacao de certificados reais, notificacao visual no Explorer, portas e dependencias externas continua condicionada ao desbloqueio da Etapa 27; nenhum resultado externo foi afirmado.
- Riscos residuais: `DTUDO_BACKUP_ROOT` precisa ser configurado fora do repositorio para que o painel valide backups; a leitura de scanner/espaco/quarentena depende da ApiFileStorage autenticada; o alerta local nao existe se o WinApp ou o host estiver indisponivel.
- Rollback: parar o WinApp e remover o menu/polling/NotifyIcon, restaurar os servicos/configuracoes/testes e remover o endpoint/controlador de health da ApiFileStorage; nenhuma raiz, quarentena, lixeira, banco ou segredo deve ser apagado automaticamente.
- Acoes manuais: apos concluir a Etapa 27, autenticar o WinApp em Development controlado, configurar `DTUDO_BACKUP_ROOT` sem versionar caminho sensivel, abrir o painel em DPI alto e confirmar uma notificacao critica sintetica; em homologacao, repetir com certificados, scanner e raiz reais.
- Proxima etapa: desbloquear e concluir a Etapa 27; nao iniciar a Etapa 29.

### Pendencia local fora da Etapa 27

- O `WinAppDtudo` compilava, mas o perfil de depuracao `WinAppControlStore` apontava para um alvo antigo inexistente. A referencia obsoleta foi removida de `WinAppDtudo/WinAppDtudo.csproj.user`.
- Foi adicionado diagnostico local em `WinAppDtudo/Services/StartupDiagnostics.cs`, com fases e excecoes sem segredos em `%LOCALAPPDATA%\Dtudo2026\WinAppDtudo\startup.log`. O teste automatizado confirmou construcao do formulario e chegada a `Application.Run`; a sessao de terminal nao criou janela grafica principal, portanto a confirmacao visual deve ser feita no VS Code.
- Build do WinApp passou com dois avisos preexistentes (`NU1510` e conflito `WindowsBase`); testes focados anteriores passaram `14/14`. Esta pendencia permanece separada e nao altera o bloqueio externo da Etapa 27.

### Execucao anterior (Etapa 25)

- Objetivo: executar exclusivamente a Etapa 25, migrando exportacao e exclusao de midia do WinApp para a ApiFileStorage por IDs/comandos.
- Evidencia principal: `docs/security/ETAPA_25_REMOCAO_ARQUIVOS_WINAPP.md`, `ApiFileStorage/Controllers/FileStorageController.cs`, `WinAppDtudo/Services/FileStorageApiClient.cs` e `WinAppDtudo/Services/CriadorDeEstruturas.cs`.
- Arquivos alterados neste checkpoint: contratos, options, controller e servicos da ApiFileStorage; cliente/configuracao/tela do WinApp; configuracao de escopo da ApiIdentity; testes focados e documentos de seguranca. Nenhum banco, migration, raiz operacional ou ACL foi alterado.
- Migracao: `export/plan` recebe `MyAnimeId`/`MalIds` e devolve `ObjectId`; upload usa multipart, bearer, sessao/dispositivo e `Idempotency-Key`; o WinApp nao envia caminho nem grava imagem local.
- Feedback e exclusao: a tela informa preparacao/download/envio/replay; exclusao em massa mostra previa, exige confirmacao e TOTP, valida o grant `filesystem.command` na ApiIdentity e move itens para a lixeira por sete dias.
- Testes: `ApiFileStorage.Tests` passou `33/33`; `WinAppDtudo.Tests` passou `16/16`; `ApiIdentity.Tests` passou `57/57` apos o novo escopo; builds de ApiFileStorage e WinApp aprovados; testes focados de comandos 2/2, cliente 2/2 e criador 1/1.
- Varredura: zero `Directory`, `File`, `Path`, `FolderBrowserDialog` ou APIs de ACL nos arquivos migrados; nenhum `FileSystemAccessRule`, `DirectorySecurity`, `FileSecurity`, `GetAccessControl` ou `SetAccessControl` no WinApp.
- Resultado: a Etapa 25 esta concluida no Development local. A analise da origem, configuracao, DPAPI, descoberta de ferramentas e log diagnostico permanecem locais conforme a matriz; a raiz protegida de exportacao nao e acessada pelo WinApp.
- Riscos residuais: homologacao ainda precisa configurar raiz/ACL minima da ApiFileStorage, Defender/AMSI real, audience/issuer, client/scopes e exercicio integrado de TOTP/step-up; catalogo e arquivos continuam sem transacao distribuida.
- Rollback: parar ApiFileStorage, preservar diarios de quarentena/lixeira, restaurar codigo/configuracao/testes/documentos e nao apagar raiz, payload, banco ou ACL automaticamente.
- Acoes manuais: em homologacao, configurar `FileStorage:Roots`, identidade do processo, scanner real, client `dtudo-winapp` com `filesystem.command` e executar exportacao, previa, TOTP, lote, reconciliacao e purge controlados.
- Proxima etapa: Etapa 26; nao iniciar neste chat.

### Execucao anterior (Etapa 18)

- Objetivo: concluir exclusivamente a Etapa 18 com owner authorization para favoritos, preferencias e listas, maioridade minimizada, termos versionados, exportacao e exclusao com retencao/auditoria.
- Arquivos alterados/atualizados: modelos e `IdentityDbContext` de recursos pessoais, `ApiIdentity/Privacy/IdentityPrivacyContracts.cs`, `ApiIdentity/Privacy/IdentityPrivacyService.cs`, `ApiIdentity/Authorization/AuthorizationCatalog.cs`, `ApiIdentity/Program.cs`, `ApiIdentity/ApiIdentity.csproj`, as migrations `20260807051638_AddPersonalDataPrivacy` e `20260807051857_AddPersonalPrivacyPermissions`, `tests/ApiIdentity.Tests/IdentityPrivacyServiceTests.cs`, `tests/ApiIdentity.Tests/ApiIdentityStartupTests.cs`, `docs/security/ETAPA_18_RECURSOS_PESSOAIS_LGPD.md`, `docs/security/MATRIZ_ACESSO.md` e este status.
- Testes executados: `dotnet test .\tests\ApiIdentity.Tests\ApiIdentity.Tests.csproj --no-restore --filter FullyQualifiedName~IdentityPrivacyServiceTests`, teste de startup filtrado para `StartsAndPublishesOpenIdDiscoveryWithoutPublicRegistration`, `dotnet test .\tests\ApiIdentity.Tests\ApiIdentity.Tests.csproj --no-restore`, `dotnet build .\ApiIdentity\ApiIdentity.csproj --no-restore` e `get_errors` nos arquivos tocados.
- Resultado: `IdentityPrivacyServiceTests` passou `6/6`; startup passou `1/1`; a suite completa `ApiIdentity.Tests` passou `55/55`, com `0` falhas e `0` ignorados; o build explicito da `ApiIdentity` passou.
- Evidencia de autorizacao: o `AccountId` e derivado de `NameIdentifier`/`sub`; os endpoints `/identity/me` nao aceitam proprietario no payload. O OpenIddict Validation local e o esquema padrao de autenticacao/desafio, e rota pessoal anonima retorna `401`.
- Evidencia de minimizacao: maioridade usa somente booleano e instante UTC; termos usam documento, versao, conteudo e SHA-256; allowlists rejeitam payloads de recurso/preferencia fora do contrato.
- Evidencia de privacidade: exportacao inclui os dados pessoais da conta e omite `PasswordHash`, `SecretHash`, `ProtectedPayload`, `TokenHash`, sessoes, dispositivos, challenges, grants, recovery tickets e tokens.
- Evidencia de exclusao: pedido idempotente com janela de sete dias; processamento remove dados pessoais e material de autenticacao, conserva pedido concluido e auditoria minima por doze meses; rollback da migration para `20260807020126_AddSessionTokens` passou.
- Decisoes: a conta inicial de bootstrap nao pode ser excluida pelo fluxo self-service; nenhum nascimento completo ou segredo real foi criado; a retencao e marcada por timestamps UTC e deve ser aplicada pelo worker operacional antes da publicacao.
- Riscos residuais: OIDC live, provisionamento de certificados/contas de servico e scheduler de processamento/purge continuam dependentes do ambiente externo; a implementacao validada permanece local Development.
- Acoes manuais: antes de homologacao/producao, configurar o provider OIDC, iniciar um worker autorizado para pedidos devidos e purge apos `RetentionUntilUtc`, e exercitar login, expiracao, revogacao e exclusao com dados de ambiente controlado.
- Rollback: migrar para `20260807020126_AddSessionTokens`, remover as migrations/rotas/modelos/permissoes da Etapa 18, retirar os testes especificos e restaurar as evidencias/status correspondentes. O rollback foi testado em LocalDB temporario.
- Proxima etapa registrada naquele momento: a Etapa 19 era a unica proxima etapa permitida. Ela nao foi iniciada naquele registro historico.

## Correcao local posterior ao Gate 30

- Data UTC: 2026-08-08.
- Objetivo: restaurar a operacao de autenticacao do WinAppDtudo em Development quando o access token expira, o Identity fica temporariamente indisponivel ou o logout remoto nao pode ser confirmado.
- Arquivos alterados: `WinAppDtudo/Services/WinAppAuthenticationService.cs`, `WinAppDtudo/Services/WinAppHealthMonitoringService.cs`, `WinAppDtudo/Frm_WinAppDtudo.cs` e testes focados correspondentes.
- Conta local: `NinoJP` ja existia, estava ativa e sem bloqueio; a credencial foi redefinida pelo endpoint local de Development. O login HTTP com antiforgery e cookie retornou `302`; nenhuma credencial foi registrada neste documento.
- Testes: `/health/ready` do ApiIdentity retornou `200`; login local retornou `302`; `WinAppDtudo.Tests` passou `25/25`; `get_errors` nao encontrou erros nos arquivos tocados.
- Resultado: logout limpa o estado local mesmo com conexao recusada e informa quando a revogacao remota nao foi confirmada; access token expirado com refresh valido fica em aviso; sessao realmente expirada continua critica e desabilita a UI privilegiada.
- Riscos residuais: a validacao continua restrita ao Development local. O Gate 30 permanece reprovado para publicacao por IIS/TLS/firewall, scanner operacional, MSIX assinado, runner e rollback real indisponiveis.
- Rollback: restaurar os tres arquivos de producao e os dois arquivos de teste desta correcao; nao remover banco, chaves, tokens protegidos ou a conta local automaticamente.
- Acoes: reiniciar o WinAppDtudo pelo perfil composto do Visual Studio para carregar os assemblies atualizados; manter `ApiIdentity` iniciado pelo mesmo perfil durante os testes.

## Correcao local de inicializacao do Identity e encerramento

- Data UTC: 2026-08-08.
- Diagnostico: o `WinAppDtudo` estava em execucao sem processo `ApiIdentity`; a porta `7243` recusava conexoes. O log local tambem registrou `ObjectDisposedException` do `SemaphoreSlim` quando o aplicativo foi fechado durante uma operacao de logout.
- Arquivos: `WinAppDtudo/Services/ApiIdentityStartupService.cs`, `WinAppDtudo/Services/WinAppAuthenticationService.cs` e `tests/WinAppDtudo.Tests/WinAppAuthenticationServiceTests.cs`.
- Resultado: em Development, o WinApp verifica `/health/ready` e inicia o projeto local `ApiIdentity` quando necessario; o logout remoto possui prazo total de oito segundos, sempre limpa o estado local e trata cancelamento no fechamento; o semaforo permanece valido ate a finalizacao das continuations.
- Evidencia: `ApiIdentity` respondeu `200`; `WinAppDtudo.Tests` passou `26/26`; build Release do WinApp passou; `get_errors` nao encontrou erros nos arquivos alterados.
- Riscos residuais: a autoinicializacao e compilada somente para `DEBUG` e depende do SDK .NET, certificado HTTPS e banco local de Development. Nenhum controle de homologacao ou producao foi alterado.
- Rollback: remover `ApiIdentityStartupService`, restaurar o construtor e o descarte anteriores do servico de autenticacao, remover o teste de concorrencia e recompilar; nao apagar o banco ou a sessao protegida automaticamente.
- Acao manual: fechar a instancia atual do `WinAppDtudo` e iniciar novamente o perfil `IniciaTudo` pelo Visual Studio para carregar o binario atualizado. O perfil deve iniciar o `ApiIdentity` em `https://localhost:7243`; se ele nao for iniciado, o WinApp tentara iniciar o projeto local em Development.

## Correcao local do ApiFileStorage no painel de saude

- Data UTC: 2026-08-08.
- Diagnostico: o processo `ApiFileStorage` nao estava ativo, a porta `7244` nao escutava e o perfil antigo usava `51378`; alem disso, `FileStorage:Roots` estava vazio no Development, fazendo o startup falhar fechado.
- Configuracao local: foi criada a raiz `%LOCALAPPDATA%\Dtudo2026\ApiFileStorage\media` e seus valores foram gravados somente no User Secrets `dtudo2026-apifilestorage`; nenhum caminho fisico foi versionado.
- Arquivos: `ApiFileStorage/Properties/launchSettings.json`, `ApiFileStorage/appsettings.json`, `Dtudo2026.slnLaunch.user`, `WinAppDtudo/appsettings.json`, `WinAppDtudo/Services/ApiFileStorageStartupService.cs`, `WinAppDtudo/Frm_WinAppDtudo.cs`, `ApiIdentity/Program.cs` e `ApiIdentity/Authorization/OpenIddictConfigurationSeeder.cs`.
- Resultado: o perfil usa `7244`; o `IniciaTudo` inclui o storage; o WinApp inicia o servico em Debug quando necessario; a API recebeu a audiencia `urn:dtudo:api-file-storage` e o token do WinApp passou a solicitar esse recurso.
- Evidencia: `ApiFileStorage` iniciou com raiz valida e respondeu `401` sem token; `/api/file-storage/health` protegido respondeu `401` sem token; `ApiIdentityStartupTests` passou `9/9`; `WinAppDtudo.Tests` passou `26/26`; builds de ApiIdentity, ApiFileStorage e WinApp Release passaram; testes completos de Identity e FileStorage terminaram sem falhas.
- Sessao existente: o JWT DPAPI atual continha somente as audiencias antigas de anime; a chamada real ao health com essa sessao respondeu `401`. O WinApp agora detecta esse token legado, limpa o estado persistido e exige novo login para emitir a audiencia do storage.
- Riscos residuais: a chamada autenticada `200` com o novo login deve ser confirmada pelo usuario no WinApp; a configuracao de raiz e autostart permanecem exclusivas de Development. O Gate 30 continua sem alteracao.
- Rollback: restaurar os arquivos listados, remover a entrada do storage do perfil composto e apagar somente os dois valores de User Secrets/root local se desejado; nao apagar arquivos de midia ou banco automaticamente.
- Acao manual: fechar qualquer WinApp/servico iniciado antes desta correcao, iniciar `IniciaTudo`, fazer login novamente e abrir o Painel de saude. A sessao antiga nao deve ser reutilizada.

## Correcao local do backup diario no painel de saude

- Data UTC: 2026-08-08.
- Diagnostico: `DTUDO_BACKUP_ROOT` nao estava definida e nenhuma raiz de backup conhecida existia para o processo do WinApp; o painel reportava "Nenhuma raiz de backup foi configurada".
- Configuracao: `DTUDO_BACKUP_ROOT` foi persistida como variavel de usuario apontando para `D:\Dtudo2026-Backups`, fora do repositorio e no volume separado `D:`. O valor nao foi colocado em `appsettings.json`.
- Execucao: `Invoke-DtudoBackup.ps1 -Mode Backup` concluiu o snapshot UTC `20260808` com 21 itens, manifesto e `manifest.sha256`, em 4,921 segundos; a tarefa `Dtudo2026-Etapa06-Backup` estava `Ready` e aponta para a mesma raiz.
- Testes: probe de manifesto valido `1/1`, probe de manifesto adulterado `1/1`, `WinAppDtudo.Tests` completo `27/27`, sem falhas; builds anteriores do WinApp e runner aprovados.
- Resultado esperado: apos reiniciar o Visual Studio para que o novo processo herde a variavel de usuario, o item `Backup / Backup diario` deve aparecer como `Healthy` com "Backup recente e manifesto verificado.".
- Riscos residuais: a raiz esta no mesmo host, embora em volume separado; a tarefa usa a identidade local de Development conforme o runbook. Copia externa/offline continua pendente para homologacao/producao.
- Rollback: remover a variavel de usuario `DTUDO_BACKUP_ROOT` e preservar o snapshot valido; nao apagar backups automaticamente. A tarefa diaria deve ser removida somente por acao operacional explicita.
- Acao manual: fechar e reabrir o `WinAppDtudo` pelo perfil `IniciaTudo`, autenticar novamente se solicitado e atualizar o Painel de saude.

## Correcao local da corrida de inicializacao do ApiFileStorage

- Data UTC: 2026-08-08.
- Diagnostico: o perfil `IniciaTudo` ja iniciava `ApiFileStorage.exe` em `7244`, enquanto o `WinAppDtudo` tentava iniciar uma segunda instancia durante a janela em que o primeiro processo ainda nao respondia; isso causava `Failed to bind to address https://127.0.0.1:7244`.
- Arquivo: `WinAppDtudo/Services/ApiFileStorageStartupService.cs`.
- Resultado: o WinApp agora detecta um processo `ApiFileStorage` existente e aguarda sua disponibilidade; somente inicia uma instancia propria quando nao ha processo em inicializacao. A verificacao ocorre antes e depois de uma pequena janela de corrida.
- Evidencia: com `ApiFileStorage.exe` ocupando `7244`, a suite `WinAppDtudo.Tests` passou `27/27` e o build Debug no destino normal passou; os avisos `NU1510` e `WindowsBase` continuam preexistentes.
- Risco residual: se um processo nao relacionado ocupar `7244`, o health endpoint continua sendo a verificacao definitiva e o WinApp registrara a indisponibilidade sem iniciar uma instancia conflitante.
- Rollback: restaurar `ApiFileStorageStartupService.cs` para a versao anterior; nao encerrar o processo existente nem remover a configuracao do perfil composto automaticamente.

## Correcao adicional da autoridade unica do ApiFileStorage

- Data UTC: 2026-08-08.
- Diagnostico: a correcao anterior ainda mantinha duas autoridades de startup: `Dtudo2026.slnLaunch.user` iniciava o storage e `Frm_WinAppDtudo.Shown` tambem podia inicia-lo antes de o primeiro processo ser detectado.
- Arquivo: `Dtudo2026.slnLaunch.user`.
- Resultado: `ApiFileStorage` foi removido do perfil composto `IniciaTudo`; em Development, o `WinAppDtudo` e o unico iniciador automatico, enquanto o servico continua aguardando uma instancia manual ja existente.
- Evidencia: JSON do perfil valido com quatro projetos; processo residual que ocupava `7244` foi encerrado; `WinAppDtudo.Tests` passou `27/27`; build Debug normal passou.
- Rollback: recolocar `ApiFileStorage\\ApiFileStorage.csproj` no perfil composto somente se o autostart do WinApp for removido simultaneamente; nao habilitar as duas formas ao mesmo tempo.

## Decisoes posteriores ao plano

- Etapa 02: `ApiMyAnimeList` e `ApiMyAnimes` falham no startup sem configuracao obrigatoria; valores de ambiente nao ficam em `appsettings` versionado.
- Escopo operacional atual: a solucao permanece em Development e nao sera promovida para producao antes da conclusao integral; homologacao/producao ficaram deliberadamente fora desta execucao.
- Etapa 05: a trilha de auditoria de seguranca e separada dos logs tecnicos do Seq e deve ser gravada exclusivamente pelo contrato interno `ISecurityAuditWriter`.
- Etapa 06: backups validos ficam em snapshots UTC de `yyyyMMdd` no volume separado `D:` do ambiente de validacao; a retencao e de 30 dias e a restauracao deve ocorrer em banco/nome e diretorio isolados.
- Etapa 06: o agendamento criado e `Dtudo2026-Etapa06-Backup`; a identidade interativa atual e provisoria e deve ser substituida antes de homologacao/producao.
- Etapa 07: a baseline operacional fica em `scripts/DtudoInfrastructureBaseline.psd1`; o runner seleciona Development por padrao, e o perfil local foi aplicado sem publicar servicos; Homologation/Production permanecem diferidos e a Etapa 08 nao foi iniciada.
- Realinhamento 2026-08-06: a solucao permanece em desenvolvimento local e sem data de publicacao. A Etapa 07 e concluida no escopo Development; controles de hospedagem permanecem declarados ate a promocao. A fundacao prioriza identidade com contas pre-criadas, declaracao de maioridade, autenticacao/autorizacao no servidor, protecao proporcional de dados, Seq/auditoria e monitoramento local. O primeiro lancamento sera somente catalogo publico, sem login ou conteudo adulto, e nenhuma credencial de servico podera ser compartilhada com o React.
- Realinhamento Etapa 03 2026-08-06: por decisao do proprietario, o repositorio publico permanece de uso pessoal e sem colaboradores, runner proprio ou publicacao. O workflow versionado e as validacoes locais comprovadas atendem esta fase; controles administrativos avancados do GitHub ficam diferidos e devem ser reavaliados antes de mudar esse contexto. Segredos reais continuam proibidos no repositorio.
- Etapa 09: ApiIdentity possui banco, DbContext e conta proprios. A configuracao de banco e obrigatoria, externa ao repositorio e validada contra o nome do banco esperado; Development usa somente DPAPI e certificados OpenIddict de desenvolvimento fora do diretorio da aplicacao.
- Etapa 10: o catalogo de autorizacao possui somente `Superadministrador` e `Usuario do Site`. Permissoes humanas sao atribuicoes papel-permissao persistidas e permissao de servico permanece sem papel humano; todas as politicas usam o formato `permission:{chave}` e a claim `permission`.
- Etapa 10: maioridade e declarada por booleano e instante UTC, sem nascimento completo. Termos sao versionados por documento com tipo, versao, conteudo, hash e publicacao; aceite e unico por conta/documento e referencia o documento exato.
- Etapa 11: bootstrap e provisionamento usam apenas procedimentos locais. A primeira conta e protegida por estado singleton transacional; a ativacao nao cria contas e consome um segredo inicial aleatorio, com hash nativo, expiracao, revogacao, `rowversion`, resposta generica e rate limiting. Nenhum convite, e-mail, cadastro publico ou segredo versionado foi criado.
- Etapa 13: o lifecycle usa tokens opacos de referencia com hash-only, access token padrao de 5 minutos e sessao/refresh limitados a 30 dias. O access token somente e aceito enquanto a introspecao encontrar conta, sessao e dispositivo ativos.
- Etapa 13: refresh rotation e protegida por update condicional atomico; replay detectado revoga a familia e a sessao inteira, bloqueando imediatamente o contexto privilegiado. Revogacoes manuais propagam para tokens, challenges e grants.
- Etapa 13: a migration `AddSessionTokens` faz backfill de expiracao/confianca a partir de `CreatedAtUtc` antes de adicionar constraints, evitando defaults de data minima em bancos existentes.
- Etapa 15: Client Credentials de servico usa endpoint separado do authorization code, sem segredo compartilhado; o binding combina `api-my-animes`, certificado de cliente, EKU `1.3.6.1.5.5.7.3.2`, `service.mal.read` e `urn:dtudo:api-my-animelist`. A rotacao usa certificado ativo + anterior com prazo UTC explicito e os tres componentes compartilham a mesma politica de overlap.
- Etapa 15: o Certificate Store e `My`, com `CurrentUser` no Development e `LocalMachine` em servidor; a chave privada recebe ACL explicita de leitura para o principal do processo, com snapshot DACL, Apply idempotente e rollback nativo sem regravar SACL.
- Etapa 16: o `DtudoGateway` usa YARP com allowlist de rotas, nao possui proxy generico e encaminha somente catalogo publico e authorize/logout OIDC; token endpoint, discovery e APIs internas nao sao expostos.
- Etapa 16: o cookie de sessao e `__Host-dtudo-bff`, com ticket OIDC no cache server-side; o React nao recebe tokens e toda mutacao BFF exige antiforgery. O segredo do client OIDC permanece externo e a validacao de startup falha fechada sem ele.
- Etapa 26: a `ApiMyAnimeList` usa `Microsoft.Extensions.Http.Resilience` com timeout total e por tentativa, retry exponencial com jitter e `Retry-After` limitado a oito segundos; `MaxRetries = 0` nao registra estrategia de retry.
- Etapa 26: retry e circuit breaker tratam somente metodos idempotentes e falhas transitorias; a excecao de egress nao e repetida nem contabilizada no circuito. O cancelamento do chamador propaga sem conversao para `504`.
- Etapa 26: a allowlist exige `https`, porta 443, host oficial e prefixo `/v2/`; o handler nao segue redirects, nao usa proxy e bloqueia enderecos DNS nao publicos antes da conexao.
- Etapa 26: a circuit breaker e local ao processo no Development; distribuicao entre replicas e qualquer ajuste de egress/proxy dependem da decisao de hospedagem e nao foram antecipados para a Etapa 27.
- Etapa 27: Homologation publica somente o gateway IIS/YARP em `16443`; APIs e Seq permanecem em loopback (`16080`, `16081`, `15341`) e Production nao participa desta execucao.
- Etapa 27: o primeiro build publico e catalog-only e usa somente rotas `/public` filtradas server-side; login, escrita, conteudo adulto, Swagger, Seq e health detalhado nao possuem rota publica.
- Etapa 29 2026-08-07: o MSIX e preparado com manifest versionado, hash SHA-256, assinatura exclusivamente pelo Certificate Store, estado externo de update/rollback e workflow separado sem PR; a conclusao depende de Windows SDK, certificado interno, runner restrito, environment aprovado e testes reais em host protegido.
- Gate 30 2026-08-07: reprovado para publicacao. Negativos locais, restauracao, revogacao, isolamento, 504, portas e pacote sintetico foram reexecutados; IIS/TLS/ACME/firewall, scanner operacional, MSIX assinado e rollback real permanecem sem evidencia por bloqueios do ambiente.
