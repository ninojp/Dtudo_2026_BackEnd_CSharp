# ApiIdentity

A **ApiIdentity** é o provedor central de identidade da solução Dtudo2026: servidor **OpenID Connect / OAuth 2.0** baseado em **OpenIddict** e ASP.NET Core Identity, com banco próprio (SQL Server), MFA, governança de roles/permissões, sessões revogáveis e recursos pessoais por proprietário com suporte a LGPD. Substitui integralmente o antigo login local da ApiMyAnimes.

Relacionados: [20_DtudoGateway.md](20_DtudoGateway.md) (BFF do site), [10_DtudoSite.md](10_DtudoSite.md), [11_WinAppDtudo.md](11_WinAppDtudo.md) (cliente PKCE e administração), [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md), [00_Solucao.md](00_Solucao.md), [30_LibDtudo.Shared.md](30_LibDtudo.Shared.md).

## Stack

| Item | Valor |
|---|---|
| Projeto | `ApiIdentity` (net10.0, minimal API + páginas HTML server-rendered) |
| Pacotes | OpenIddict 7.7.x (AspNetCore, EntityFrameworkCore, Validation), ASP.NET Core Identity, EF Core SqlServer 10.0.x, Fido2 4.3.x, `LibDtudo.Shared` |
| URLs locais | `https://localhost:7243` e `http://localhost:5243` |
| User Secrets | `dtudo2026-apiidentity` |
| Health | `GET /health/live`; `GET /health/ready` (verifica o banco) |
| Restrição atual | O `Program` lança exceção se não estiver em **Development no Windows** (certificados OpenIddict reais e proteção de chaves ainda não configurados para outros ambientes) |

## Persistência

- **Banco**: SQL Server, `IdentityDbContext` separado dos demais contextos. Em Development usa LocalDB (`DtudoIdentity.Development`, autenticação integrada). `ConnectionStrings:IdentityDb` é validada contra `IdentityDatabase:DatabaseName`; ausência → falha de startup.
- **Migrations**: `InitialIdentityFoundation`, `AddIdentityGovernance`, `AddAccountProvisioning`, `AddIdentitySecurity`, `AddSessionTokens`, `AddPersonalDataPrivacy`, `AddPersonalPrivacyPermissions` (todas com rollback testado).
- **Política de senha**: mínimo 12 caracteres, dígito, minúscula, maiúscula, símbolo e ≥ 4 caracteres únicos; e-mail único; lockout após 5 tentativas por 15 min. Dados sensíveis protegidos por `ProtectedIdentityUserStore`.
- **Chaves**: DataProtection em `%LOCALAPPDATA%\Dtudo2026\ApiIdentity\DataProtectionKeys` (DPAPI). Tokens e códigos de recuperação são persistidos apenas como hash (SHA-256); token em claro nunca é gravado.

## OpenIddict

| Item | Valor |
|---|---|
| Endpoints | `/.well-known/openid-configuration`, `/.well-known/jwks`, `/connect/authorize`, `/connect/token`, `/connect/revocation`, `/connect/logout` |
| Fluxos | Authorization code (**PKCE obrigatório**), refresh token, client credentials (contas de serviço) |
| Access token | JWT assinado, sem criptografia, vida de 300 s |
| Scopes registrados | `profile`, `identity.login`, `identity.provision` (+ demais criados pelo Seeder) |
| Recursos (audiences) | `urn:dtudo:api-my-animes`, `urn:dtudo:api-my-animelist`, `urn:dtudo:api-file-storage`, `urn:dtudo:api-musicx`, `urn:dtudo:api-discogs` |
| Cookie de login | `__Host-DtudoIdentity` (`Secure`, `SameSite=Lax`, 8 h, sem sliding) |

**Login no navegador**: `GET/POST /account/login` (HTML server-rendered, antiforgery, mensagem de erro genérica, `returnUrl` sanitizado). O tema visual ("winapp" ou "site") vem do `client_id` do `returnUrl`. Em `/connect/authorize`, sem cookie → challenge para o login; com cookie, `OpenIddictAuthorizationPrincipalFactory` exige conta ativada e não bloqueada e emite claims `sub`, `name`, `email`, `role` e `permission` (access e id token). Não existe registro público (`404`).

## Clientes registrados

O `OpenIddictConfigurationSeeder` cria/atualiza os clientes a cada startup.

| Client | Tipo | Redirect / fluxo | Scopes / recursos |
|---|---|---|---|
| `dtudo-winapp` | Público (PKCE) | Loopback `http://127.0.0.1:49173/callback/` (validado), code + refresh | `openid profile offline_access identity.login identity.provision catalog.read catalog.write catalog.delete filesystem.command health.read`; todos os 5 recursos. **Só aceita role Superadministrador** (senão `403` com página) |
| `dtudo-gateway` | Confidencial (code + PKCE, refresh) | `https://localhost:51376/signin-oidc`; logout `/signout-callback-oidc`; criado só se `OpenIddict:Gateway:ClientSecret` estiver configurado | `openid profile catalog.read personal.read personal.write offline_access` (`OpenIddict:Gateway:Scopes`; as permissões `personal.*` já existiam no catálogo, sem nova permissão); recursos `api-my-animes`, `api-musicx` e `api-discogs` (sem `api-file-storage`) |
| `api-my-animes` | Conta de serviço (client credentials + mTLS) | Sem redirect; certificado de cliente; configurado em `ServiceAuthentication:Clients` | `service.mal.read` para `urn:dtudo:api-my-animelist` (consulta externa) e `filesystem.command` para `urn:dtudo:api-file-storage` (atualização do status de pasta; mesmo client e mesmo certificado, com uma segunda concessão em `AllowedGrants` — em Development nos user-secrets) |

### Fluxos

1. **Site (BFF)**: o `DtudoGateway` faz authorization code + PKCE com o client `dtudo-gateway` (o navegador nunca recebe tokens) e pede os recursos `api-my-animes`, `api-musicx` e `api-discogs`. O scope `catalog.read` autoriza `POST /apiLocal/Anime/status` na [ApiMyAnimes](22_ApiMyAnimes.md), e `personal.read`/`personal.write` autorizam as rotas de progresso de episódios `apiLocal/Anime/{malId}/watch*` (o `sub` do token é o dono do dado). Ao ampliar os scopes do client, reinicie a ApiIdentity (o seeder atualiza o `dtudo-gateway` existente) e faça **novo login** no site; sessões antigas não recebem os novos scopes e as rotas `watch` respondem `403`.
2. **WinApp (PKCE)**: aplicação desktop abre o navegador, recebe o code no loopback e troca por tokens; refresh com `offline_access`. Detalhes em [11_WinAppDtudo.md](11_WinAppDtudo.md).
3. **Serviço → serviço (client credentials + mTLS)**: `/connect/token` exige certificado de cliente do Windows Certificate Store (nenhum segredo compartilhado), client ID coerente, EKU de cliente, validade e *thumbprint* registrado (ativo ou anterior, para rotação), além de escopos e audiences em allowlist **amarrados por concessão** (`AllowedGrants`: audiência → escopos; um escopo concedido para uma audiência não vale para outra; a forma legada `AllowedScopes`/`AllowedAudiences` só vale com uma audiência e não pode ser misturada, senão a API não inicia). Exige exatamente um `resource` e no máximo um `scope`; o escopo vira o claim `permission` do token. *Thumbprints* são únicos entre clients (um certificado, um client). Detalhes em [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md).

## Autorização: roles e permissões

Catálogo central em `AuthorizationCatalog`; cada permissão vira uma policy `permission:<chave>`.

| Permissão | Superadministrador | Usuario do Site |
|---|:-:|:-:|
| `catalog.read` | ✔ | ✔ |
| `catalog.write`, `catalog.delete` | ✔ | — |
| `identity.provision` | ✔ | — |
| `identity.login` | — (escopo de login) | — (escopo de login) |
| `identity.self.read` | ✔ | ✔ |
| `personal.read`, `personal.write` | ✔ | ✔ |
| `privacy.export`, `privacy.delete` | ✔ | ✔ |
| `health.read`, `filesystem.command` | ✔ | — |
| `service.mal.read` | só conta de serviço | — |
| `service.privacy.myanimes` | só conta de serviço (client `api-identity`, apenas para a audiência `urn:dtudo:api-my-animes`) | — |

Matriz de acesso em [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md).

## Rotas

### Provisionamento local (somente loopback)

Guardadas por `LocalProvisioningRequestGuard` com `LocalProvisioning:AdministrationSecret` (Base64Url ≥ 32 bytes, via User Secrets); não autorizado → `404`; rate limit 10/min.

| Rota | Função |
|---|---|
| `POST /internal/identity/bootstrap` | Cria o primeiro administrador (executa uma única vez) |
| `POST /internal/identity/accounts` | Provisiona conta com segredo de ativação inicial |
| `POST /internal/identity/activation-secrets/{id}/revoke` | Revoga segredo de ativação |
| `POST /internal/identity/development/reset-password` | Somente Development |
| `POST /identity/activate-initial-account` | Ativação pelo titular (genérica contra enumeração; 5/min; consumo único) |
| `POST /identity/recover/local` | Resgate de ticket de recuperação local (5/min; hash, expira, consumo único) |
| `/internal/identity/security/tokens/introspect`, `recovery-tickets`, `recovery-tickets/{id}/revoke` | Introspecção e tickets (`identity.provision`; 5/min) |

### Segurança de sessão e MFA (`/identity/security/*`)

Sessões (criar, listar, revogar, revogar todas), binding de token por sessão/dispositivo (`sessions/{id}/token`, `sessions/tokens`), `POST /identity/security/tokens/refresh` (rotação com detecção de replay: reuso revoga a família e a sessão), dispositivos, **TOTP** (setup, confirm, step-up, disable), **códigos de recuperação** (step-up, regenerate, 10 códigos), **passkeys/WebAuthn** (opções de registro/autenticação, complete, delete), `step-up/{action}` e revogação de step-up, **snapshots** de segurança (create/restore/revoke; 24 h; restauração única).

Um middleware extra, para `/identity/security|admin|me` com Bearer, introspecta o access token (binding de sessão/dispositivo) e responde `401` se não estiver vinculado (exceto criação de sessão/binding).

### Administração (`/identity/admin/*`, policy `identity.provision`)

Contas (GET/POST), roles, permissões, dispositivos, sessões, `accounts/{id}/roles`, `accounts/{id}/lock`, remoção de sessões/dispositivos. As **mutações exigem sessão e dispositivo ativos + step-up MFA** com a permissão exata. A UI administrativa vive no WinApp ([11_WinAppDtudo.md](11_WinAppDtudo.md)).

### Opções de sessão e MFA

`IdentitySessions`: sessão 30 dias (máximo), access 300 s, refresh 30 dias, entropia 32 bytes. `IdentityMfa`: RP `localhost`, origins HTTPS, timeouts Fido2 validados, challenge 120 s, step-up 300 s, ticket de recuperação 15 min, snapshot 24 h. Valores fora da faixa → falha de startup.

## Recursos pessoais e LGPD (`/identity/me`)

O proprietário é sempre derivado do claim `sub`/`NameIdentifier`; **nenhum `accountId` do cliente é aceito**, logo conhecer o ID de outro registro não amplia acesso. Tipos de recurso: `anime`, `my-anime`.

| Rota | Permissão |
|---|---|
| `GET/POST /identity/me/age-confirmation` | `personal.read` / `personal.write` |
| `GET /identity/me/terms/{documentType}/current`; `POST .../terms/{id}/accept` | `personal.read` / `personal.write` |
| `GET/POST/DELETE /identity/me/favorites` | read / write |
| `GET/PUT/DELETE /identity/me/preferences` (chaves: `catalog-sort`, `language`, `notifications`, `theme`) | read / write |
| `GET/POST/DELETE /identity/me/lists` e itens (`POST/DELETE`) | read / write |
| `POST /identity/me/data-export` (JSON **v2**, sem segredos de autenticação; inclui a seção `myAnimes` com o progresso de episódios; `503` + `Retry-After` se a ApiMyAnimes estiver indisponível) | `privacy.export` |
| `POST /identity/me/deletion-request` (carência 7 dias, retenção de auditoria 12 meses, idempotente, bloqueada para a conta bootstrap) | `privacy.delete` |

Maioridade é guardada só como `HasConfirmedAdultAge` + `AdultAgeConfirmedAtUtc` (sem data de nascimento). Termos são versionados com hash. A exclusão é processada pelo **worker** descrito em "Integração LGPD com a ApiMyAnimes e worker de exclusão" (desabilitado por padrão). Essas rotas **não são expostas pelo Gateway** nem consumidas pelo DtudoSite hoje.

### Integração LGPD com a ApiMyAnimes e worker de exclusão

**Formato do export (`exportVersion`).** `"1"` = dados da ApiIdentity; `"2"` (atual, `PersonalDataExportVersions.Current`) = `"1"` + `myAnimes` (`schemaVersion` próprio, hoje `"1"`): `episodeProgress` (por `malId`, episódios e `watchedAtUtc`) e `folderRefreshRuns` (execuções de atualização de pasta iniciadas pela conta, sem caminhos). A exportação **nunca é parcial**: se a ApiMyAnimes não responder (indisponível, timeout, rejeição, não configurada), o endpoint responde `503` com `Retry-After`, e o evento de auditoria `privacy.data-export` fica `failed` com um código curto (`http-503`, `timeout`, `network`, `not-configured`…), sem dado pessoal.

**Cliente tipado** (`IMyAnimesPrivacyClient`/`MyAnimesPrivacyClient`, `AddHttpClient`): handler primário com certificado de cliente (mTLS), `CorrelationIdDelegatingHandler` e `ServiceAccessTokenHandler` (token de client credentials do client `api-identity`, escopo `service.privacy.myanimes`, recurso `urn:dtudo:api-my-animes`). Timeout por tentativa e retry limitado com backoff exponencial **somente** para falhas transitórias (rede, timeout, `408`, `429`, `5xx`); `4xx` definitivo, resposta inválida e autenticação de serviço desabilitada/sem certificado não são repetidos. Rotas `POST` com a conta no corpo (nunca na URL): `apiLocal/internal/privacy/export` e `.../erase`.

**Exclusão (`ProcessDueDeletionAsync`)** devolve `DeletionProcessingOutcome` (`Completed`, `NotPending`, `NotDue`, `Protected`, `DownstreamFailed`):

1. valida pedido pendente, carência vencida e conta não-bootstrap (a conta bootstrap nunca é excluída);
2. chama a exclusão idempotente na ApiMyAnimes **sem transação de banco aberta**;
3. se falhar: o pedido continua `Pending`, `AttemptCount` e `LastAttemptAtUtc` sobem, `LastError` guarda o código curto (≤ 200), auditoria `privacy.deletion-failed`, nada local é removido;
4. se tiver sucesso: transação local remove os dados da conta, marca `Completed` (retenção de 12 meses) e audita `privacy.deletion-completed`. Nunca há `Completed` com dado remanescente na ApiMyAnimes. Pedido cuja conta local já não existe é concluído após a exclusão remota idempotente.

**Worker** (`PrivacyDeletionWorker`, `BackgroundService`): **desabilitado por padrão**. A cada `PollIntervalSeconds` seleciona até `BatchSize` pedidos `Pending` vencidos (do mais antigo ao mais novo; ignora contas bootstrap e pedidos que falharam há menos de `RetryDelayMinutes`) e os processa **um por vez**, cada um em um escopo DI próprio (um `DbContext` por pedido). Cancelamento cooperativo no shutdown, `InitialDelaySeconds` antes da primeira varredura (a ApiIdentity precisa estar atendendo o próprio `/connect/token`), falhas inesperadas não derrubam o loop (o pedido continua pendente). Logs sem identificador de conta/e-mail/mensagem de exceção: apenas id do pedido e tipo da exceção. Auditoria em `IdentityProvisioningAuditEvents` (ações `privacy.deletion-failed`/`privacy.deletion-completed`, `deviceId` `privacy-worker`).

**Permissão de serviço.** `service.privacy.myanimes` está no `AuthorizationCatalog` (e na policy `permission:...`), mas **fora de `AllRoles`**: nenhum usuário a recebe. Validações de startup (`ValidateOnStart`): o escopo só pode constar em `AllowedGrants` para a audiência `urn:dtudo:api-my-animes`; `ServiceAuthentication:ApiMyAnimes` (credenciais de saída) exige, quando habilitado, endpoint HTTPS, client ID, thumbprints válidos, audiência e escopo fixos.

**Migração** `AddDeletionRetryTrackingAndPrivacyServicePermission` (gerada, **não aplicada**): colunas `AttemptCount` (`>= 0`), `LastAttemptAtUtc` (offset UTC) e `LastError` (≤ 200) em `IdentityPersonalDataDeletionRequests`, índice `(Status, ScheduledForUtc)` e o registro da permissão de serviço. Aplicar com `dotnet ef database update --project ApiIdentity\ApiIdentity.csproj`.

## Remoção do login legado

`LocalAuthService`, `AuthController`, `AuthOptions`, `auth-users.Development.json` e DTOs relacionados foram removidos da ApiMyAnimes/LibDtudo.Shared. `/apiLocal/Auth/{register,login,me/legacy}` retornam `404` (teste `LegacyAuthenticationEndpoints_AreNotMapped`). Toda autenticação passa pela ApiIdentity.

## Configuração (sem valores sensíveis)

| Seção | Observações |
|---|---|
| `ConnectionStrings:IdentityDb`, `IdentityDatabase` | LocalDB em Development; nome do banco validado |
| `OpenIddict:Gateway:ClientSecret` | **User Secrets** — igual a `OpenIdConnect:ClientSecret` do gateway |
| `LocalProvisioning:AdministrationSecret` | **User Secrets** (Base64Url ≥ 32 bytes) |
| `IdentitySessions`, `IdentityMfa` | Faixas validadas no startup |
| `ServiceAuthentication` | `Enabled=false`, `Clients: []`; certificados/thumbprints só em segredo/ambiente |
| `ServiceAuthentication:ApiMyAnimes` | Credenciais **de saída** para a ApiMyAnimes (`Enabled=false`; `TokenEndpoint`, `ClientId` `api-identity`, `Audience` `urn:dtudo:api-my-animes`, `Scopes` `[service.privacy.myanimes]`, thumbprints, store); valores reais só em segredo/ambiente |
| `PrivacyIntegration:MyAnimes` | `BaseUrl` HTTPS da ApiMyAnimes (vazia = não configurado: export `503`, exclusão pendente), `RequestTimeoutSeconds` 1–60 (10), `MaxAttempts` 1–5 (3), `RetryBaseDelayMilliseconds` 0–5000 (250) |
| `PrivacyDeletionWorker` | `Enabled=false` (padrão), `PollIntervalSeconds` 10–86400 (300), `InitialDelaySeconds` 0–3600 (30), `BatchSize` 1–100 (10), `RetryDelayMinutes` 1–1440 (15); fora da faixa → falha de startup |
| Certificados OpenIddict | Dev: certificados de desenvolvimento (assinatura/criptografia); real: pendente |

## Testes

`tests\ApiIdentity.Tests` (xUnit; 87 `[Fact]/[Theory]` em 8 arquivos): `AccountProvisioningServiceTests` (bootstrap único, replay, expiração, concorrência), `ApiIdentityStartupTests` (discovery sem registro público, end-session, falha fechada de configuração, policy por permissão, recursos musicx/discogs, **cliente `dtudo-gateway` semeado com os recursos my-animes/musicx/discogs, refresh, revogação e `offline_access`, sem file-storage**, worker desabilitado por padrão e cliente tipado registrado, falha fechada com config do worker fora da faixa e com escopo de privacidade para outra audiência), `IdentityGovernanceDatabaseTests`, `IdentitySecurityServiceTests`, `LocalProvisioningEndpointTests`, `ServiceTokenEndpointTests`, `IdentityPrivacyServiceTests` (export v2 e falha sem export parcial; exclusão chama a ApiMyAnimes antes de remover; falha do cliente mantém `Pending` com `AttemptCount`/`LastError` e o retry conclui; seleção de vencidos respeitando carência, `RetryDelayMinutes`, lote e conta bootstrap; worker um por vez, lote limitado e cancelamento; permissão de serviço fora das roles; usa SQL Server LocalDB com migrações e um `FakeMyAnimesPrivacyClient`) e `MyAnimesPrivacyClientTests` (retry limitado, `4xx` sem retry, rede/timeout, autenticação de serviço indisponível, resposta inválida, não configurado, conta inválida, cancelamento, validação de options e `PrivacyServiceGrantRules`). Não há teste HTTP do mapeamento `503` do `/identity/me/data-export` (sem infraestrutura de token de usuário nesse projeto); a propagação da exceção é coberta no serviço.

## Pendências/Divergências

- Worker de exclusão LGPD e integração com a ApiMyAnimes implementados e testados com substitutos, mas **não validados ao vivo** (exigem `ServiceAuthentication` real, certificados, client `api-identity` e `PrivacyDeletionWorker:Enabled=true`; ver [02_Solucao_Operacao.md](02_Solucao_Operacao.md)). A migração `AddDeletionRetryTrackingAndPrivacyServicePermission` precisa ser aplicada.
- Execução limitada a Development/Windows; certificados reais, issuer público, ACLs e `ServiceAuthentication` real ficam para homologação.
- `RegisterScopes` registra só 3 escopos; os demais são criados pelo Seeder no banco.
- `/identity/me/*` sem exposição no Gateway/Site (decisão de produto pendente).
- O cliente `dtudo-gateway` só é criado quando o segredo está configurado.
- `offline_access` do gateway é adicionado pelo Seeder como permissão (não fica em `OpenIddict:Gateway:Scopes`); o gateway o solicita em `OpenIdConnect:Scopes`. Sessões anteriores sem refresh token precisam de novo login.
- Revisado `sub`/audience: o token do gateway carrega `sub` do usuário e `aud` com os recursos solicitados; não foi encontrada divergência (a perda do recurso `api-my-animes` era do handler do gateway, ver [20_DtudoGateway.md](20_DtudoGateway.md)).
