# Status da Implementacao de Seguranca

## Estado geral

- Etapa atual: 16 (Concluida no escopo de implementacao e validacao local Development)
- Ultima etapa concluida: 16
- Proxima etapa permitida: 17 (abrir novo chat para executar exclusivamente a Etapa 17). A Etapa 17 nao foi iniciada neste chat.
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
| 17 | Pendente | - | - |
| 18 | Pendente | - | - |
| 19 | Pendente | - | - |
| 20 | Pendente | - | - |
| 21 | Pendente | - | - |
| 22 | Pendente | - | - |
| 23 | Pendente | - | - |
| 24 | Pendente | - | - |
| 25 | Pendente | - | - |
| 26 | Pendente | - | - |
| 27 | Pendente | - | - |
| 28 | Pendente | - | - |
| 29 | Pendente | - | - |
| 30 | Pendente | - | - |

## Ultima execucao

- Objetivo: concluir exclusivamente a Etapa 16 com `DtudoGateway`, YARP, OIDC Authorization Code + PKCE, cookie BFF server-side, antiforgery, allowlist de redirects e exposicao minima de rotas.
- Arquivos alterados/atualizados: `DtudoGateway/DtudoGateway.csproj`, `DtudoGateway/Program.cs`, `DtudoGateway/appsettings.json`, `DtudoGateway/Configuration/GatewayOptions.cs`, `DtudoGateway/Configuration/RedirectAllowlist.cs`, `DtudoGateway/Infrastructure/GatewayRouteConfiguration.cs`, `DtudoGateway/Infrastructure/ServerSideTicketStore.cs`, `tests/DtudoGateway.Tests/DtudoGateway.Tests.csproj`, `tests/DtudoGateway.Tests/DtudoGatewayTests.cs`, `Dtudo2026.slnx`, `docs/security/ETAPA_16_GATEWAY_BFF.md`, `docs/security/MATRIZ_ACESSO.md` e este status.
- Testes executados: `dotnet test .\tests\DtudoGateway.Tests\DtudoGateway.Tests.csproj --no-restore`.
- Resultado: `10/10` testes aprovados, `0` falhas e `0` ignorados; `get_errors` nao encontrou erros no gateway ou nos testes.
- Evidencia: redirects externos, userinfo e rotas nao allowlisted sao recusados; origem configurada, code + PKCE/S256 e callbacks no host do gateway sao aceitos.
- Evidencia: cookie `__Host-dtudo-bff` e `HttpOnly`/`Secure`/`SameSite=Lax`; correlation/nonce sao seguros; antiforgery usa `__Host-dtudo-xsrf`, `SameSite=Strict` e header `X-CSRF-TOKEN`.
- Evidencia: tokens salvos pelo handler ficam no `IDistributedCache` server-side; `/bff/me` nao retorna access token ou refresh token; catalogo remove headers de sessao e autorizacao no proxy.
- Evidencia: YARP possui somente cinco leituras de catalogo e os dois endpoints OIDC publicos necessarios; mutacao retorna `405`, API interna, token endpoint e Swagger retornam `404`.
- Decisoes: o browser nao e redirecionado diretamente para a porta da `ApiIdentity`; authorize/logout sao alcancados pelo gateway, enquanto discovery/token permanecem server-side. O secret do client OIDC e obrigatorio, externo e ausente do `appsettings.json`.
- Riscos residuais: o provider live, registro do client, handler de login/logout da `ApiIdentity`, certificados/issuer/discovery reais e substituicao do cache de memoria por armazenamento distribuido persistente ainda exigem validacao manual antes da promocao.
- Acoes manuais: registrar `dtudo-gateway` com redirect `/signin-oidc`, callback de logout, PKCE e segredo fora do repositorio; configurar `OpenIdConnect:ClientSecret` e destinos HTTPS por User Secrets/ambiente; executar fluxo live e revisar a protecao das chaves.
- Rollback: remover o projeto/testes, entradas da solucao e rotas do gateway, retirar `docs/security/ETAPA_16_GATEWAY_BFF.md` e restaurar o status/matriz para a pendencia anterior. Nao ha migration nem alteracao de banco.
- Proxima etapa: a Etapa 17 e a unica proxima etapa permitida. Ela nao foi iniciada neste chat.

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
