# Status da Implementacao de Seguranca

## Estado geral

- Etapa atual: 25 (Concluida no Development local)
- Ultima etapa concluida: 25
- Proxima etapa permitida: 26; ela nao foi iniciada neste chat.
- Bloqueios globais: a implementacao local esta validada, mas a publicacao continua bloqueada ate provisionar certificados reais, contas de servico e issuer/discovery no host alvo. O repositorio e pessoal, somente o proprietario faz commits, e regras administrativas do GitHub, data/dominio e promocao para servidor Windows proprio continuam diferidas.

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
| 26 | Pendente | - | - |
| 27 | Pendente | - | - |
| 28 | Pendente | - | - |
| 29 | Pendente | - | - |
| 30 | Pendente | - | - |

### Ultima execucao

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
