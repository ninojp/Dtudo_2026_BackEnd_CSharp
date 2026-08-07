# Gate da Etapa 20 - Identidade

## Decisao

**Etapa 20: Concluida no escopo local Development.**

O login local legado foi removido depois da confirmacao dos clientes novos. Os testes negativos, de rotacao, revogacao e rollback exigidos pelo gate foram reexecutados e passaram. A conclusao nao autoriza promocao: certificados reais, contas de servico, issuer/discovery, firewall e homologacao continuam dependentes do ambiente externo.

A Etapa 21 permanece pendente e nao foi iniciada.

## Clientes confirmados antes da remocao

| Cliente/superficie | Fluxo vigente confirmado | Evidencia |
| --- | --- | --- |
| `ApiIdentity` | Contas, bootstrap/provisionamento, MFA, sessoes, tokens opacos, revogacao, OpenIddict e privacidade | `ApiIdentity` e a fonte exclusiva de identidade; suite `ApiIdentity.Tests` `57/57` |
| `DtudoGateway` | BFF YARP, OIDC Code + PKCE, cookie `__Host-dtudo-bff`, ticket server-side e antiforgery | Testes focados do Gateway `10/10` |
| `DtudoSite` | Sessao BFF; nenhum access token ou refresh token em JavaScript, `localStorage` ou `sessionStorage` | Lint/build e scan de tokens da Etapa 17 aprovados |
| `WinAppDtudo` | Navegador do sistema, PKCE/loopback, DPAPI, refresh, logout, revogacao e administracao Dark Mode com step-up | Testes focados do WinApp `10/10` |
| `ApiMyAnimes` | Catalogo protegido por policies; chamada interna para MAL com Client Credentials + mTLS | Suite completa `ApiMyAnimes.Tests` `18/18`; autorizacao/startup/auditoria `10/10` e mTLS `10/10` |
| `ApiMyAnimeList` | API interna protegida por issuer, audience, permissao e escopo `service.mal.read` | Suite completa `ApiMyAnimeList.Tests` `13/13`; `LibDtudo.Shared.Tests` `24/24` |

## Remocao executada

Foram removidos os seguintes caminhos de implementacao, configuracao, dados, contrato e teste:

- `ApiMyAnimes/Services/LocalAuthService.cs`
- `ApiMyAnimes/Controllers/AuthController.cs`
- `ApiMyAnimes/Configuration/AuthOptions.cs`
- `ApiMyAnimes/App_Data/auth-users.Development.json`
- `LibDtudo.Shared/Dtos/Auth/AuthDtos.cs`
- `LibDtudo.Shared/Utils/ValidaSenhaLogin.cs`
- `tests/ApiMyAnimes.Tests/AuthControllerTests.cs`

Tambem foram removidos o registro DI de `LocalAuthService`, a validacao/binding de `Auth:UsersFilePath` e as configuracoes `Auth` dos dois `appsettings` da `ApiMyAnimes`. O teste `ApiAuthorizationTests.LegacyAuthenticationEndpoints_AreNotMapped` confirma `404` para:

- `/apiLocal/Auth/register`
- `/apiLocal/Auth/login`
- `/apiLocal/Auth/me/legacy`

## Revisao por dominio

| Dominio | Resultado do gate |
| --- | --- |
| Provisionamento de contas | Aprovado; bootstrap de uso unico, segredo inicial com hash, expiracao, revogacao, consumo unico, rate limiting e rollback de migration cobertos |
| MFA | Aprovado; passkey/FIDO2, TOTP, recovery codes, desafios, step-up, expiracao e replay cobertos |
| Sessoes e tokens | Aprovado; tokens opacos hash-only, access token curto, refresh rotation, reuse detection, expiracao e vinculo sessao/dispositivo cobertos |
| Revogacao | Aprovado; familia de refresh, sessao, dispositivo, grants, challenges e binding OIDC sao invalidados conforme os cenarios testados |
| APIs | Aprovado; issuer/audience, escopo/permissao, fallback deny-by-default, rotas publicas enumeradas e rotas legadas 404 |
| mTLS | Aprovado; client ID, certificado, EKU, audience, escopo, certificado incorreto e overlap de rotacao cobertos |
| BFF e site | Aprovado no escopo local; PKCE, cookie server-side, antiforgery, allowlist e ausencia de tokens no React cobertos |
| LGPD | Aprovado; owner authorization, maioridade minimizada, termos, exportacao sem segredos, exclusao, retencao e auditoria cobertos |
| WinApp | Aprovado no escopo local; navegador do sistema, DPAPI, refresh/revogacao, administracao, permissao e step-up cobertos |

## Validacao executada

| Bloco | Resultado |
| --- | ---: |
| `tests/ApiIdentity.Tests` completo | `57/57` |
| `ServiceTokenEndpointTests` | `10/10` |
| `ApiMyAnimes` auth/startup/auditoria | `10/10` |
| `tests/ApiMyAnimes.Tests` completo | `18/18` |
| `DtudoGateway.Tests` + `WinAppDtudo.Tests` | `20/20` |
| `ApiMyAnimeList.Tests` + `LibDtudo.Shared.Tests` de seguranca/logging | `21/21` |
| `tests/ApiMyAnimeList.Tests` completo | `13/13` |
| `tests/LibDtudo.Shared.Tests` completo | `24/24` |
| Builds dos projetos tocados e consumidores de `LibDtudo.Shared` | Aprovados |
| Varredura de fonte fora de `bin/`, `obj/` e `.vs/` | Sem implementacao legada ativa; ocorrencias restantes sao testes negativos ou historico documental |

Os testes de startup e mTLS usam bancos fixos LocalDB. A primeira execucao falhou antes da validacao funcional porque `DtudoIdentity.ApiIdentityTests` e `DtudoIdentity.ServiceTokenEndpointTests` nao existiam ou nao tinham schema para o login `MEUPC\\comer`. Os bancos foram criados somente na instancia local `MSSQLLocalDB` e atualizados pelas migrations oficiais; a reexecucao passou integralmente. Nenhuma base de dados de aplicacao ou dado real foi alterado.

## Rollback e limites

- Rollback de migrations, incluindo o retorno para `20260807020126_AddSessionTokens`, passou nos testes de banco da identidade.
- O rollback funcional do gate e remover os caminhos novos somente com uma decisao operacional explicita; nao se deve reintroduzir login por senha local, JSON ou token acessivel ao navegador.
- Em caso de falha de promocao, desativar os clientes OpenIddict/mTLS no ambiente, retirar a configuracao de certificado nova e restaurar o snapshot de banco aprovado, preservando auditoria.
- A conclusao e limitada ao Development local. Homologacao/producao ainda exigem certificados reais, contas de servico, ACLs, issuer/discovery, worker LGPD e exercicio browser/processo real.
