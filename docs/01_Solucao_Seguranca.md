# Dtudo2026 — Segurança da solução

Este documento consolida o modelo de segurança transversal da solução Dtudo2026: modelo de ameaças, fronteiras de confiança, matriz de acesso vigente, autenticação entre serviços (client credentials + mTLS), política de rotação de segredos, LGPD, o estado final do gate de segurança e os riscos residuais ainda abertos. Não contém segredos, thumbprints reais, strings de conexão ou caminhos de usuário. Arquitetura geral em [00_Solucao.md](00_Solucao.md); operação (backup, hardening, CI/CD) em [02_Solucao_Operacao.md](02_Solucao_Operacao.md).

> **Estado final do gate: REPROVADO para publicação** (2026-08-07). O escopo validado é **somente Development local**; o catálogo e o WinApp **não devem ser publicados** até que os bloqueios abaixo sejam resolvidos.

## Modelo de ameaças

### Fronteiras de confiança

| Fronteira | Descrição | Controles principais |
|---|---|---|
| Navegador / público | Origem não confiável | CORS por origem HTTPS exata, CSRF (antiforgery), rate limit, cabeçalhos de segurança/CSP/HSTS |
| DtudoSite (React) | Código no navegador; sem segredos | Não recebe nem armazena tokens; sessão apenas por cookie `__Host-` |
| [DtudoGateway](20_DtudoGateway.md) (BFF) | Termina a sessão do usuário e injeta Bearer | OIDC code + PKCE, ticket store no servidor, allowlist de redirecionamento, proxy somente-GET |
| [ApiIdentity](21_ApiIdentity.md) | Autoridade de identidade | OpenIddict, lockout, step-up, auditoria append-only, proteção de endpoints internos |
| APIs de domínio | [ApiMyAnimes](22_ApiMyAnimes.md), [ApiMyAnimeList](23_ApiMyAnimeList.md), [ApiMusicX](25_ApiMusicX.md), [ApiDiscogs](26_ApiDiscogs.md), [ApiFileStorage](24_ApiFileStorage.md) | JWT com emissor/audiência, permissão + escopo, política de fallback autenticada |
| SQL / arquivos | Dados persistentes | Autenticação Windows, ACLs mínimas, backup verificado |
| [WinAppDtudo](11_WinAppDtudo.md) / operador | Cliente público de confiança limitada | PKCE com navegador do sistema, loopback, DPAPI; sem acesso direto a SQL |
| APIs externas | MyAnimeList e Discogs | Allowlist de egress, resiliência, sem seguir redirects |
| Runner de release | Assina e distribui o MSIX | Environment com aprovação, runner dedicado, assinatura pelo Certificate Store |

### Ameaças e mitigações (estado vigente)

| Ameaça | Mitigação |
|---|---|
| Mutação anônima do catálogo | Autenticação JWT + políticas `catalog.write`/`catalog.delete`; GETs públicos limitados e filtrados |
| Token de sessão em `localStorage` / login JSON legado | BFF com cookie `__Host-dtudo-bff`; rotas `/apiLocal/Auth/*` removidas (404) |
| CSRF | Cookie antiforgery `__Host-dtudo-xsrf` + cabeçalho `X-CSRF-TOKEN`; no gateway, rotas mutáveis (hoje só o status em lote) exigem CSRF, `application/json` e corpo limitado por metadados da rota |
| Open redirect | Allowlist de redirecionamento em `/bff/login` |
| SSRF em integrações externas | Allowlists: `api.myanimelist.net` (prefixo `/v2/`) e `api.discogs.com`; bloqueio de DNS não público; sem redirects/proxy |
| Indisponibilidade (504/DoS) | Resiliência com retry, timeouts e circuit breaker na ApiMyAnimeList; rate limit no gateway; limites de corpo |
| Path traversal / reparse point / TOCTOU em arquivos | Resolução segura de caminhos na ApiFileStorage (testada) |
| Malware em arquivos | Quarentena, scanner (Defender/AMSI), tipos e magic bytes permitidos |
| Roubo/replay de refresh token | Tokens opacos com hash, rotação, detecção de reuso revogando a família e a sessão |
| Escalada via SQL no desktop | Acesso SQL removido do WinApp |
| Vazamento em logs | `SensitiveDataRedactionEnricher`; correlação por `RequestCorrelationMiddleware` |
| Cadeia de suprimentos / CI | Actions fixadas por SHA, CodeQL, dependency review/audit, gitleaks, permissões mínimas, sem `pull_request_target` |

### Riscos residuais do modelo

Certificados e contas de serviço reais; validação OIDC/mTLS em processos reais; scanner Defender/AMSI real; backups no mesmo host; controles administrativos do GitHub; validação ao vivo do worker de exclusão LGPD e do fluxo ApiIdentity → ApiMyAnimes; e a própria homologação.

## Autenticação e autorização

- **Usuários do site:** BFF + OIDC, sem token no navegador (ver [00_Solucao.md](00_Solucao.md#autenticação-ponta-a-ponta)).
- **WinApp:** cliente público `dtudo-winapp`, PKCE, tokens protegidos por DPAPI.
- **APIs:** validam emissor HTTPS obrigatório (`ValidateOnStart`), audiência própria, `MapInboundClaims=false`, tolerância de relógio de 1 min. As políticas exigem a *permission* e o mesmo valor em `scope`/`scp`. Política de fallback: usuário autenticado.
- **Tokens:** access 300 s; refresh 30 dias; PKCE obrigatório; conta bloqueada por 15 min após 5 falhas.
- **Swagger/OpenAPI:** somente em Development e protegido por `health.read`.

## Matriz de acesso (estado vigente)

### Atores e permissões

| Ator | Permissões |
|---|---|
| Anônimo | Apenas leitura pública do catálogo local (rotas `public`), health de startup do FileStorage e `/health/live` do gateway |
| Usuario do Site | `catalog.read`, `identity.self.read`, `personal.read/write`, `privacy.export/delete` |
| Superadministrador | `catalog.read/write/delete`, `identity.provision`, `identity.self.read`, `personal.read/write`, `privacy.export/delete`, `health.read`, `filesystem.command` |
| WinApp (`dtudo-winapp`) | Escopos `openid profile offline_access identity.login identity.provision catalog.read/write/delete filesystem.command health.read`, limitados pelas permissões do usuário |
| Gateway (`dtudo-gateway`) | Escopos `openid profile catalog.read personal.read personal.write offline_access` (refresh/revogação; os `personal.*` exigem novo login); recursos `api-my-animes`, `api-musicx` e `api-discogs` |
| ApiMyAnimes (serviço) | `service.mal.read` por client credentials para `api-my-animelist`; `filesystem.command` por client credentials para `api-file-storage` (atualização do status de pasta; mesmo client `api-my-animes` e mesmo certificado) |
| ApiIdentity (serviço, client `api-identity`) | `service.privacy.myanimes` por client credentials **somente** para `api-my-animes` (exportação e exclusão LGPD do progresso); permissão **fora de qualquer role de usuário** |

### Atores × recursos

| Recurso | Anônimo | Usuário do Site | Superadmin | Serviço |
|---|---|---|---|---|
| ApiMyAnimes — GET `Anime`, `Anime/buscar`, `Anime/relacionados`, `Anime/{id}`, `MyAnime`, `MyAnime/{id}` e variantes `public` | Sim (conteúdo adulto filtrado no servidor pela `PublicCatalogPolicy` nas rotas `public`) | Sim | Sim | — |
| ApiMyAnimes — POST `Anime/status` (status por usuário em lote; `watch` real desde a Fase 3 e `folder` real desde a Fase 5 — lido do resultado persistido, global e sem caminho físico —, `like` ainda neutro; via gateway `POST /api/catalog/animes/status`, com sessão + CSRF) | Não (`401`) | `catalog.read` (dados só do próprio `sub`) | `catalog.read` | — |
| ApiMyAnimes — GET `Anime/{malId}/watch` (progresso de episódios do próprio usuário; via gateway `GET /api/catalog/animes/{id}/watch`, sessão sem CSRF) | Não (`401`) | `personal.read` (somente o próprio `sub`) | `personal.read` | — |
| ApiMyAnimes — PUT/DELETE `Anime/{malId}/watch/episodes`, PUT `watch/all`, DELETE `watch` (marcar/desmarcar/limpar; via gateway, sessão + CSRF) | Não (`401`) | `personal.write` (somente o próprio `sub`) | `personal.write` | — |
| ApiMyAnimes — POST/PUT/PATCH, `conflito-titulo`, migração de catálogo, monitoramento (locais/sincronizações) | Não | Não | `catalog.write` | — |
| ApiMyAnimes — `MyAnime/folder-status/*` e `MyAnime/{id}/folder-status/refresh` (atualizar/consultar/cancelar a atualização do status de pasta; **sem rota no gateway**) | Não | Não (`403`) | `catalog.write` | — |
| ApiMyAnimes — DELETE | Não | Não | `catalog.delete` | — |
| ApiMyAnimes — POST `apiLocal/internal/privacy/export` e `.../erase` (exportação/exclusão LGPD por conta; corpo com `accountId`; **sem rota no gateway e sem CORS**) | Não (`401`) | Não (`403`) | Não (`403`; token de usuário nunca é aceito) | `service.privacy.myanimes` (client `api-identity` + certificado mTLS registrado) |
| ApiMyAnimes — health | Não | Não | `health.read` | — |
| ApiMyAnimeList — busca, detalhes, relações | Não | `catalog.read` | `catalog.read` | `service.mal.read` |
| ApiMusicX — GET coleções/artistas/lançamentos | Não | `catalog.read` | Sim | — |
| ApiMusicX — POST/PUT/importação | Não | Não | `catalog.write` | — |
| ApiMusicX — DELETE | Não | Não | `catalog.delete` | — |
| ApiDiscogs — artistas/lançamentos/masters | Não | Política de leitura de catálogo externo | Sim | — |
| ApiFileStorage — `FileStorageController` e `MonitoringController` | Não | Não | `filesystem.command` | — |
| Health (todas as APIs) | Não (exceto startup do FileStorage e live do gateway/ApiIdentity) | Não | `health.read` | — |
| ApiIdentity — `/identity/admin/*` | Não | Não | `identity.provision` | — |
| ApiIdentity — `/identity/me/*`, `/identity/security/*` | Não | Próprio titular | Próprio titular | — |
| ApiIdentity — `/internal/identity/*` | Não | Não | Somente local (`LocalProvisioningRequestGuard`) | — |

Notas: `ApiMyAnimeList` usa a política `catalog.external.read`, que aceita (`catalog.read` com escopo) **ou** (`service.mal.read` com escopo). As rotas de anime do gateway removem o `Authorization` antes de chegar à API; as de música e Discogs recebem o Bearer da sessão. O endpoint de desenvolvimento `development/reset-password` existe apenas em Development.

Rota de status em lote (`POST /apiLocal/Anime/status`) e rotas `Anime/{malId}/watch*`: o gateway injeta o access token da sessão **somente** nelas dentre as rotas de anime (as demais continuam sem `Authorization`); a ApiMyAnimes valida `catalog.read` (status) ou `personal.read`/`personal.write` (watch) (permission + scope) e usa exclusivamente o claim `sub` como identidade, nunca o corpo.

### Rotas legadas removidas

`/apiLocal/Auth/*` (registro, login, `me/{id}`) retorna 404; `AuthController`, `LocalAuthService`, o arquivo `auth-users*.json` e tokens em `localStorage` foram removidos.

## Client credentials e mTLS entre serviços

Fluxo de `ApiMyAnimes` → `ApiMyAnimeList`:

1. `ApiMyAnimes` solicita token em `/connect/token` da ApiIdentity: client `api-my-animes`, grant `client_credentials`, escopo `service.mal.read`, recurso `urn:dtudo:api-my-animelist`.
2. Apresenta **certificado de cliente** do Windows Certificate Store (`My`; `CurrentUser` em Development, `LocalMachine` em servidor) com EKU Client Authentication (`1.3.6.1.5.5.7.3.2`). **Nenhum segredo compartilhado é aceito.**
3. A ApiIdentity (`ServiceTokenEndpoint`) valida certificado, client ID, EKU, validade, thumbprint ativo/anterior e a concessão (escopo válido **para a audiência pedida**, `AllowedGrants`) e emite o access token, com o escopo como claim `permission`.
4. A `ApiMyAnimeList` revalida a vinculação `client_id` ↔ certificado (`ServiceClientCertificateMiddleware`) e a política `catalog.external.read`.

Fluxo de `ApiMyAnimes` → `ApiFileStorage` (atualização do status de pasta dos animes, Fase 5): mesmo client `api-my-animes`, mesmo certificado e mesmo `/connect/token`, com escopo `filesystem.command` e recurso `urn:dtudo:api-file-storage`; na ApiIdentity o client ganhou a concessão `urn:dtudo:api-file-storage` → `filesystem.command` em `AllowedGrants`. **Escopo e audiência são amarrados por concessão** (`AllowedGrants`: audiência → escopos): `api-my-animes` só obtém `service.mal.read` para a audiência da MAL e `filesystem.command` para a da ApiFileStorage; o cruzamento (`service.mal.read` em `api-file-storage` ou o inverso) é recusado com `invalid_scope`. A forma legada `AllowedScopes`/`AllowedAudiences` só é aceita com **uma única** audiência e não pode ser combinada com `AllowedGrants`; com várias audiências a ApiIdentity e a ApiMyAnimeList **não iniciam** (validação no startup). Em Development isso está nos user-secrets das duas APIs (nada no repositório) e foi validado ao vivo: token emitido com o certificado e `anime-folders/discovery`/`scan` respondendo na ApiFileStorage; sem token a ApiFileStorage responde `401`. A ApiFileStorage valida apenas o JWT (audiência `urn:dtudo:api-file-storage` + permissão `filesystem.command`), sem middleware de certificado de cliente — o `filesystem.command` também é de usuários (superadmin/WinApp), então a ApiFileStorage não pode exigir certificado de cliente em todas as chamadas. Sem o registro, a atualização falha de forma isolada por anime e o status anterior é preservado.

Fluxo de `ApiIdentity` → `ApiMyAnimes` (exportação e exclusão LGPD do progresso, Fase 8): client `api-identity`, certificado próprio, escopo `service.privacy.myanimes` e recurso `urn:dtudo:api-my-animes`, em `AllowedGrants` do client na própria ApiIdentity (a ApiIdentity chama o próprio `/connect/token` por HTTPS). O escopo **só pode ser concedido para a audiência da ApiMyAnimes**: a ApiIdentity não inicia se qualquer client o receber para outra audiência, nem se as credenciais de saída (`ServiceAuthentication:ApiMyAnimes`) tiverem audiência/escopo diferentes dos fixos. Na ApiMyAnimes a política `permission:service.privacy.myanimes` falha fechada e só autoriza quando `InternalServiceAuthentication:Enabled=true`, o token tem a permissão **e** o escopo, `sub == client_id` (exclui qualquer token de usuário), o `client_id` está na allowlist e o **certificado mTLS da conexão** é o registrado para esse client (o Kestrel também exige certificado opcional e o valida contra a allowlist). A permissão está no `AuthorizationCatalog` mas **não** é concedida a nenhuma role. Timeout por tentativa (10 s) e retry limitado (3 tentativas, backoff exponencial, só falhas transitórias).

Rotação de certificado: registrar o novo thumbprint primeiro e manter o antigo como secundário até `PreviousCertificateAcceptedUntilUtc`; depois do prazo o antigo é rejeitado (`client-certificate-not-registered`). O acesso à chave privada é concedido por `Invoke-DtudoInfrastructureHardening.ps1 -ConfigureCertificateAcl` ([02_Solucao_Operacao.md](02_Solucao_Operacao.md)). Thumbprints ficam apenas em configuração protegida (`<THUMBPRINT_HEX_40>`).

**Estado:** `ServiceAuthentication.Enabled=false` por padrão em todos os `appsettings`; **nenhum teste mTLS ao vivo entre processos foi executado**.

## Rotação e gestão de segredos

| Tipo | Origem | Política |
|---|---|---|
| Desenvolvimento | .NET User Secrets (`MyAnimeList:ClientId`, `ConnectionStrings:LocalDbConnection`) | Nunca versionar; `ValidateOnStart` falha sem eles |
| Homologação/Produção | Variáveis de ambiente ou provedor de segredos do host (`MyAnimeList__ClientId`, `ConnectionStrings__LocalDbConnection`, `OpenIddict__Gateway__ClientSecret`, `OpenIdConnect__ClientSecret`) | Nunca usar `appsettings.Staging.json` versionado |
| Certificados mTLS | Certificate Store | Thumbprints apenas em configuração protegida; rotação com sobreposição |
| Segredo do client do gateway | Fonte externa | Vazio nos `appsettings`; o seeder só cria o client `dtudo-gateway` se `OpenIddict:Gateway:ClientSecret` for informado |
| Chaves do BFF | Data Protection com DPAPI | Persistidas fora do repositório |
| Material de recuperação do backup | Fora do repositório (`-RecoveryMaterialPath`) | Guardado separadamente, com ACL restrita |

Regras de rotação:

- Rotacionar após suspeita de exposição, troca de responsável ou periodicamente; registrar a rotação em log **sem valores**.
- Antes da primeira promoção: revogar qualquer `Client ID` antigo do MyAnimeList que não seja reconhecido e restringir as ACLs das fontes de segredo.
- Credenciais de contas locais nunca são registradas em documentação ou log.

## LGPD (ApiIdentity)

Autorização do titular sobre seus próprios dados; confirmação de idade sem guardar a data de nascimento completa; termos versionados com hash; exportação de dados sem material de autenticação; solicitação de exclusão com janela de 7 dias; retenção de 12 meses; auditoria append-only (`IdentityProvisioningAuditEvents` na ApiIdentity e `SecurityAuditEvents` na ApiMyAnimes, retenção de 12 meses).

**Escopo da exportação/exclusão (desde a Fase 8):** além dos dados da ApiIdentity, entram o **progresso de episódios** (`AnimeEpisodeWatches`) e o vínculo da conta com execuções de atualização de pasta (`AnimeFolderRefreshRuns.RequestedBy`) da ApiMyAnimes. A exportação (formato v2) falha com `503` se a ApiMyAnimes estiver indisponível (nunca entrega export parcial). A exclusão, executada pelo **worker** da ApiIdentity após a carência de 7 dias, apaga **primeiro** na ApiMyAnimes (idempotente) e só então remove os dados locais; se a chamada falhar, o pedido continua `Pending`, com contador de tentativas e último erro (código curto, sem dado pessoal), e é repetido. Na ApiMyAnimes o progresso é apagado e `RequestedBy` é **anonimizado** (`deleted-account`), pois a execução é dado global; a trilha `SecurityAuditEvents` guarda apenas um hash curto da conta. `AnimeFolderStatus` é dado global do catálogo e fica fora da LGPD. A conta bootstrap continua protegida (nunca é excluída).

O **worker vem desabilitado** (`PrivacyDeletionWorker:Enabled=false`) e **não foi validado em ambiente real** (sem mTLS/`ServiceAuthentication` ao vivo entre os processos); os testes usam substitutos do cliente HTTP e certificados efêmeros.

## Estado final do Gate 30

**Resultado: reprovado para publicação.** Escopo mantido em Development local.

### Bloqueios

| # | Bloqueio | Necessário para fechar |
|---|---|---|
| 1 | Homologação externa | Não aplicável no momento (projeto sem implantação; a borda de homologação foi removida) |
| 2 | MSIX real | `makeappx`/`signtool`, assinatura real, teste de instalação/atualização/rollback, runner protegido, environment com aprovação |
| 3 | GitHub | Ruleset, secret scanning/push protection, environments e runner group (configurações administrativas diferidas) |
| 4 | Antes da promoção | Defender/AMSI reais, OIDC/mTLS ao vivo, validação ao vivo do worker LGPD (exclusão e exportação ApiIdentity → ApiMyAnimes), restauração semestral fora do host |

### Evidências locais (Development)

ApiIdentity 51/51; ApiFileStorage focado 35/35; resiliência MAL 8/8; WinApp autenticação/proteção 10/10; suíte do pipeline MSIX 10/10; restauração de backup OK; backup adulterado rejeitado; varredura local redigida sem segredos em conteúdo/histórico.

### Riscos residuais abertos

- `npm audit` do DtudoSite com 3 vulnerabilidades conhecidas (1 baixa, 2 altas), exigindo atualização maior (React Router ou ferramenta de build); item pendente explícito — **não elevar o limiar do audit**.
- Backups no mesmo host (volume separado); sem cópia off-site/offline.
- Contas de serviço dedicadas, BitLocker e certificados reais ainda não provisionados.
- Catálogo e arquivos sem transação distribuída.
- Circuit breaker da ApiMyAnimeList é local ao processo; distribuição entre réplicas depende da decisão de hospedagem.
- Scanner/espaço/quarentena do painel de saúde dependem da ApiFileStorage autenticada; `DTUDO_BACKUP_ROOT` precisa ser configurado fora do repositório.
- O repositório é pessoal: somente o proprietário faz commits.

## Histórico resumido

| Mudança | Efeito |
|---|---|
| Remoção do login JSON legado e do token em `localStorage` | Autenticação única via ApiIdentity + BFF |
| Introdução do BFF (DtudoGateway) | Navegador sem tokens; CSRF, allowlists e rate limit no gateway |
| OpenIddict com refresh rotation e auditoria append-only | Revogação imediata e rastreabilidade |
| Allowlist de egress e resiliência na ApiMyAnimeList | Mitigação de SSRF e de falhas 504 |
| ApiFileStorage com quarentena e resolução segura | Mitigação de path traversal e malware |
| WinApp sem SQL e sem escrita via ApiFileStorage | Escreve estrutura direto na pasta local escolhida; endpoints de escrita da ApiFileStorage removidos |
| Remoção do `ApiNode` | Proxy Discogs substituído por `ApiDiscogs` + `DtudoGateway` |

Os documentos por etapa foram consolidados nesta série; nomes de etapa permanecem apenas em alguns artefatos operacionais.

## Pendências/Divergências

- **ApiIdentity:** o `appsettings.json` versionado contém uma string de conexão LocalDB (autenticação integrada, sem senha) e nome de banco de Development; a política antiga dizia que isso havia sido removido. Aceitável para Development, mas deve ser sobrescrito por configuração externa em Homologação/Produção.
- **ApiIdentity:** registra certificados de **assinatura e criptografia de desenvolvimento** de forma incondicional; certificados reais são obrigatórios antes de Homologação/Produção.
- **Gateway (resolvido na Fase 2):** o handler OIDC chamava `SetParameter("resource", ...)` duas vezes e o último valor sobrescrevia o primeiro; agora monta `resource=a&resource=b&resource=c` a partir de `OpenIdConnect:Resources` (my-animes, musicx, discogs) e o client `dtudo-gateway` recebeu permissão para `api-my-animes`. O gateway renova o access token (refresh rotativo, `offline_access`) e revoga o refresh token no logout; sessões antigas exigem novo login. O proxy continua somente-GET, com a única exceção `POST /api/catalog/animes/status` (sessão + CSRF + corpo ≤ 16 KiB + Bearer da sessão apenas nessa rota de anime). CORS segue `GET`-only (restritivo; o POST é same-origin).
- **Gateway/progresso de episódios (Fase 3):** além do status em lote, o proxy expõe 5 rotas `.../animes/{id}/watch*` (GET sem CSRF; PUT/DELETE com sessão + CSRF, corpo ≤ 64 KiB ou vazio nas mutações sem corpo). Nelas o gateway também injeta o access token da sessão; a ApiMyAnimes exige `personal.read`/`personal.write` (permission + scope) e usa apenas o `sub` como `AccountId`. O `GET` sem sessão responde `401` (sem redirecionamento). Sessões anteriores à ampliação dos scopes recebem `403` até novo login.
**LGPD do progresso (resolvido na Fase 8):** exportação e exclusão do progresso passam pela ApiIdentity por **client credentials + mTLS** em endpoints internos da ApiMyAnimes (ver "LGPD" e "Client credentials e mTLS" acima e [21_ApiIdentity.md](21_ApiIdentity.md)). **Pendente (ambiente):** `ServiceAuthentication:Enabled=true`, certificados, client `api-identity` com `AllowedGrants`, `InternalServiceAuthentication` na ApiMyAnimes e `PrivacyDeletionWorker:Enabled=true`; sem isso a exclusão permanece pendente e a exportação responde `503` (comportamento seguro). Passo a passo em [02_Solucao_Operacao.md](02_Solucao_Operacao.md).
Se houver publicação no futuro, a borda de homologação precisará ser refeita para esse modelo.
- **ApiMyAnimeList:** busca/detalhes/relações usam `catalog.external.read` (aceita `catalog.read` **ou** `service.mal.read`).
- **ApiFileStorage:** além de `resolve`, `reconcile`, `health` e `startup`, existe `MonitoringController` (raízes, sincronizações/descoberta, sessões, heartbeat, refresh, eventos, entradas), exigindo `filesystem.command`. O `appsettings.json` base declara `FileStorage:Roots` vazio, mas a validação de inicialização exige ao menos uma raiz; também define `Authentication:Issuer` sem barra final.
- **ApiMyAnimes:** `appsettings.json` não possui a seção `Authentication` (somente o de Development); Homologação/Produção precisam fornecer emissor e audiência externamente, senão a validação de inicialização falha.
- O gateway só expõe `/health/live` (não há `/health/ready` nem health detalhado).

