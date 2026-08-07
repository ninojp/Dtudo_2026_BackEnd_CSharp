# Etapa 19 - Identidade do WinApp

## Escopo entregue

Esta etapa implementa exclusivamente a autenticacao e a administracao de identidade do `WinAppDtudo`:

- navegador do sistema com Authorization Code + PKCE usando `S256`;
- callback loopback fixo em `http://127.0.0.1:49173/callback/`, registrado no OpenIddict;
- `state`, `code_verifier`, `code_challenge` e validacao do callback antes da troca do codigo;
- cliente publico `dtudo-winapp`, sem segredo no desktop;
- refresh token e rebind do novo access token na sessao de seguranca;
- persistencia local do conjunto de tokens protegida por DPAPI `CurrentUser`;
- logout com revogacao da sessao no servidor e limpeza do armazenamento local;
- introspeccao dos bearer tokens OIDC vinculados a `SecuritySession`/`SecurityDevice`;
- administracao Dark Mode de contas, papeis, permissoes, dispositivos e sessoes;
- step-up MFA `identity.provision` para mutacoes administrativas;
- remocao do login/cadastro por senha do caminho ativo do WinApp.

## Fluxo de autenticacao

1. O WinApp gera valores aleatorios para `state` e `code_verifier`, calcula o desafio SHA-256 e inicia o listener loopback na porta registrada.
2. O navegador abre `/connect/authorize`. Sem cookie, a ApiIdentity redireciona para `/account/login`, que usa o cookie de aplicacao do ASP.NET Core Identity.
3. O OpenIddict valida cliente, redirect exato, escopos, `authorization_code` e PKCE. O callback aceita somente requisicoes GET de loopback com caminho exato, `state` correspondente e codigo presente.
4. O WinApp troca o codigo em `/connect/token`, cria uma sessao segura e envia o hash do access token ao servidor. O token bruto nao e persistido pela ApiIdentity.
5. Rotas protegidas exigem bearer valido e binding ativo. Revogar sessao ou dispositivo invalida a introspeccao do access token OIDC e o refresh correspondente.
6. No logout, um access token expirado pode ser renovado primeiro para permitir a revogacao remota; o refresh token atual e revogado pelo endpoint OpenIddict `/connect/revocation` e, ao final, o estado local e apagado mesmo quando alguma revogacao nao pode ser concluida.

## Administracao e MFA

O grupo `/identity/admin` exige a permissao `identity.provision`. Cada mutacao tambem exige:

- conta autenticada com a permissao efetiva;
- sessao e dispositivo ativos;
- grant de step-up para `identity.provision` no mesmo contexto.

O formulario `Frm_IdentityAdministration` reutiliza a infraestrutura Dark Mode existente e oferece leitura de contas, papeis, permissoes, dispositivos e sessoes, alem de provisionamento, atribuicao de papel, bloqueio e revogacao.

## Persistencia e revogacao

O WinApp serializa o conjunto de sessao em JSON somente antes da protecao DPAPI. O arquivo final contem bytes protegidos por `DataProtectionScope.CurrentUser`, usa escrita temporaria atomica e e removido no logout. A ApiIdentity armazena somente hashes SHA-256 dos tokens vinculados, com expiracao limitada pela sessao.

O OpenIddict continua dono do codigo de autorizacao e dos refresh tokens OIDC. O cliente publico possui permissao para o endpoint de revogacao e o logout envia `client_id`, o refresh token e `token_type_hint=refresh_token` para `/connect/revocation`. A tabela `IdentitySecurityToken` funciona como ponte de binding para que a revogacao de sessao/dispositivo tambem invalide chamadas OIDC ja emitidas.

## Evidencias de validacao

- `dotnet build .\ApiIdentity\ApiIdentity.csproj --no-restore`: aprovado.
- `dotnet build .\WinAppDtudo\WinAppDtudo.csproj --no-restore`: aprovado; permanecem os avisos conhecidos de `System.Security.Cryptography.ProtectedData` e conflito `WindowsBase` do WebView2.
- `dotnet test .\tests\ApiIdentity.Tests\ApiIdentity.Tests.csproj --no-restore --filter "FullyQualifiedName~IdentitySecurityServiceTests"`: `19/19` aprovados.
- Testes focados de binding OIDC e administracao: `2/2` aprovados.
- `dotnet test .\tests\WinAppDtudo.Tests\WinAppDtudo.Tests.csproj --no-restore`: `10/10` aprovados, cobrindo DPAPI, refresh OAuth `snake_case`, rebind, logout, ausencia de tokens claros, validacao de redirect/state/callback, PKCE e colisao da porta loopback.
- O teste do binding OIDC confirma introspeccao antes da revogacao, ausencia do token claro no hash persistido e introspeccao nula depois da revogacao da sessao.
- O teste administrativo confirma negacao sem permissao, negacao sem step-up, sucesso com step-up e nova negacao depois da revogacao da sessao.

## Validacao ainda dependente do ambiente

A rodada completa de `ApiIdentity.Tests` foi executada, mas terminou com `38` aprovados e `19` falhas de infraestrutura: os testes de startup usam bancos fixos que nao existiam na instancia/configuracao SQL Server disponivel. A instancia `MSSQLLocalDB` estava executando, mas a consulta de bancos `DtudoIdentity*` retornou zero resultados. Isso impede afirmar a validacao completa do startup/seeding neste ambiente.

Tambem permanece pendente a verificacao manual ou automatizada end-to-end com banco migrado e `ApiIdentity` executando em `https://localhost:7243`:

- login no navegador, retorno do codigo e troca PKCE;
- rejeicao de `state`, verifier ou redirect incorretos;
- renovacao real do refresh token OpenIddict;
- revogacao real pelo formulario administrativo e logout;
- porta `49173` livre no host do WinApp.

## Pre-requisitos e rollback

Antes de homologacao, aplicar as migrations da ApiIdentity, configurar `ConnectionStrings:IdentityDb`, `LocalProvisioning:AdministrationSecret`, certificados de Development e a porta loopback registrada. Segredos e certificados reais devem permanecer fora do repositorio.

O rollback da etapa deve remover o cliente/scopes OpenIddict do ambiente, desativar as rotas e o formulario de administracao do WinApp, restaurar o caminho de autenticacao anterior somente se houver decisao explicita, e migrar o banco conforme o ponto de rollback definido para as tabelas de tokens/sessoes. Nao remover dados de auditoria sem uma politica de retencao aprovada.

## Limites

Esta etapa nao inicia nem antecipa a Etapa 20. A Etapa 20 permanece pendente.
