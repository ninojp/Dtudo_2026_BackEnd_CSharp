# Matriz de Acesso

## 1. Objetivo e legenda

Esta matriz registra o acesso efetivo observado no estado atual, as permissoes ja implementadas e a permissao necessaria para a arquitetura-alvo. As superficies ainda nao migradas continuam classificadas como referencia de planejamento.

> Atualizacao pos-Etapa 20: as linhas marcadas como legadas preservam o baseline historico da Etapa 01. As rotas JSON removidas permanecem sem mapeamento e foram verificadas com `404`; os fluxos vigentes sao `ApiIdentity`, `DtudoGateway`, `ApiMyAnimes` protegido e o cliente WinApp com PKCE.

Legenda do acesso atual:

- `R`: leitura observada sem barreira de identidade no controller.
- `W`: escrita, atualizacao ou exclusao observada sem barreira de identidade no controller.
- `A`: operacao de autenticacao/registro observada sem sessao de API.
- `-`: nao ha chamada observada para o ator naquela superficie.
- `*`: a permissao depende de alcance de rede; CORS nao e uma autorizacao de servidor.

Permissoes de referencia para o alvo:

- `catalog.read`: leitura do catalogo publico, com escopo de rota e limite.
- `catalog.write`: criacao/atualizacao de catalogo pelo WinApp autorizado.
- `catalog.delete`: exclusao de catalogo, com step-up quando aplicavel.
- `identity.provision`: bootstrap e provisionamento administrativo de conta pre-criada; sem convite ou cadastro publico.
- `identity.login`: autenticacao pelo fluxo de identidade.
- `identity.self.read`: leitura do proprio usuario; consulta administrativa separada.
- `personal.read`: leitura dos recursos pessoais do proprio usuario.
- `personal.write`: criacao, alteracao e exclusao dos recursos pessoais do proprio usuario.
- `privacy.export`: exportacao dos dados pessoais do proprio usuario.
- `privacy.delete`: solicitacao de exclusao dos dados do proprio usuario.
- `health.read`: health minimo, com exposicao restrita conforme o ambiente.
- `service.mal.read`: chamada interna autorizada a dados da MAL.
- `filesystem.command`: operacao de arquivo por ID/comando no servico de arquivos.
- `db.owner`: acesso do servico proprietario ao banco, nunca do cliente final.

## 2. Atores

| Sigla | Ator | Identidade observada hoje | Papel necessario no alvo |
| --- | --- | --- | --- |
| ANON | Navegador/cliente anonimo | Apenas alcance HTTP; sem identidade de API | Leitor do catalogo publico |
| SITE | Usuario autenticado do site | `user` e token no `localStorage`; token nao e validado nos controllers lidos | Usuario do site pre-criado, com recursos proprios |
| WIN | Operador do `WinAppDtudo` | Processo desktop sem token/certificado de servico observado | Cliente administrativo autorizado |
| AMS | Processo `ApiMyAnimes` | Client Credentials + mTLS implementados; habilitacao depende de configuracao e certificado do ambiente | Servico proprietario de `MyAnimes`/`Anime` |
| MLS | Processo `ApiMyAnimeList` | `HttpClient` com Client ID configurado para API externa | Servico autorizado de egress MAL |
| OSDB | Conta/processo do host e LocalDB | Windows/LocalDB com autenticacao integrada observada | Conta de servico com ACL minima |
| MAL | API oficial externa | Identificada pela URL e credencial configurada fora desta matriz | Dependencia externa, somente resposta de dados |

## 3. Endpoints da ApiMyAnimes

| Endpoint/recurso | ANON atual | SITE atual | WIN atual | AMS atual | MLS atual | Permissao necessaria no alvo |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `GET /apiLocal/Health` | R* | R* | R | R | - | `health.read`; preferir rede interna para detalhes de banco |
| `GET /apiLocal/Anime` | R* | R* | R | R | - | `catalog.read` |
| `GET /apiLocal/Anime/buscar` | R* | R* | R | R | - | `catalog.read` |
| `GET /apiLocal/Anime/{malId}` | R* | R* | R | R | - | `catalog.read` |
| `POST /apiLocal/Anime/conflito-titulo` | W* | W* | W | W | - | `catalog.write` para validacao de importacao; limitar payload |
| `POST /apiLocal/Anime` | W* | W* | W | W | - | `catalog.write`; somente operador/servico autorizado |
| `PUT /apiLocal/Anime/{malId}` | W* | W* | W | W | - | `catalog.write`; verificar escopo do recurso |
| `PATCH /apiLocal/Anime/{malId}` | W* | W* | W | W | - | `catalog.write`; restringir campos e operacoes |
| `DELETE /apiLocal/Anime/{malId}` | W* | W* | W | W | - | `catalog.delete`; step-up e auditoria |
| `GET /apiLocal/MyAnime` | R* | R* | R | R | - | `catalog.read`; confirmar se toda colecao e publica |
| `GET /apiLocal/MyAnime/{id}` | R* | R* | R | R | - | `catalog.read` ou propriedade, conforme decisao de produto |
| `POST /apiLocal/MyAnime` | W* | W* | W | W | - | `catalog.write`; somente operador/servico autorizado |
| `PUT /apiLocal/MyAnime/{id}` | W* | W* | W | W | - | `catalog.write`; verificar colecao alvo |
| `PATCH /apiLocal/MyAnime/{id}` | W* | W* | W | W | - | `catalog.write`; restringir campos e operacoes |
| `DELETE /apiLocal/MyAnime/{id}` | W* | W* | W | W | - | `catalog.delete`; step-up e auditoria |
| `POST /apiLocal/Auth/register` (legado, removido) | - | - | - | - | - | `404`; usar bootstrap/provisionamento `identity.provision` em `ApiIdentity` |
| `POST /apiLocal/Auth/login` (legado, removido) | - | - | - | - | - | `404`; usar `identity.login` em `ApiIdentity`/BFF |
| `GET /apiLocal/Auth/me/{id}` (legado, removido) | - | - | - | - | - | `404`; usar `identity.self.read` no fluxo de identidade |

Observacao: `SITE` e `ANON` possuem o mesmo acesso efetivo observado porque a API nao demonstra validacao do token persistido pelo frontend. `AMS` e `WIN` tambem nao enviam uma identidade de servico observada nos clientes lidos.

## 4. Endpoints da ApiMyAnimeList

| Endpoint/recurso | ANON atual | SITE atual | WIN atual | AMS atual | MLS atual | Permissao necessaria no alvo |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `GET /ApiMyAnimeList/health` | R* | R* | R | R | R | `health.read`; detalhes internos somente para rede/servico autorizado |
| `GET /ApiMyAnimeList/search` | R* | R* | R | R | R | `catalog.read` publico via gateway; rota interna deve exigir `service.mal.read` |
| `GET /ApiMyAnimeList/{malId}` | R* | R* | R | R | R | `catalog.read` publico via gateway; chamada interna com `service.mal.read` |
| `GET /ApiMyAnimeList/{malId}/relations` | R* | R* | R | R | R | `catalog.read` publico via gateway; chamada interna com `service.mal.read` |

`MLS` representa o proprio processo ao acessar a API externa MAL, nao um chamador HTTP da API local. A fronteira HTTP interna agora usa o client ID `api-my-animes`, o audience `urn:dtudo:api-my-animelist`, o escopo `service.mal.read` e certificado de cliente com overlap controlado.

## 5. Endpoints da ApiIdentity - recursos pessoais e LGPD

| Endpoint/recurso | ANON | SITE | WIN | AMS | MLS | Permissao necessaria |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `GET /identity/me/age-confirmation` | - | R | - | - | - | `personal.read` |
| `POST /identity/me/age-confirmation` | - | W | - | - | - | `personal.write` |
| `GET /identity/me/terms/{documentType}/current` | - | R | - | - | - | `personal.read` |
| `POST /identity/me/terms/{termsDocumentId}/accept` | - | W | - | - | - | `personal.write` |
| `GET /identity/me/favorites` e `GET /identity/me/preferences` | - | R | - | - | - | `personal.read` |
| `POST/DELETE /identity/me/favorites` | - | W | - | - | - | `personal.write` |
| `PUT/DELETE /identity/me/preferences` | - | W | - | - | - | `personal.write` |
| `GET /identity/me/lists` | - | R | - | - | - | `personal.read` |
| `POST/DELETE /identity/me/lists` e itens | - | W | - | - | - | `personal.write` |
| `POST /identity/me/data-export` | - | W | - | - | - | `privacy.export` |
| `POST /identity/me/deletion-request` | - | W | - | - | - | `privacy.delete` |

Todas as rotas estao no grupo `/identity/me`, exigem sessao validada pelo OpenIddict e derivam `AccountId` de `NameIdentifier`/`sub`. Nenhum `accountId` fornecido pelo cliente e aceito. O ator `SITE` representa somente o proprietario da propria conta; conhecer o ID de outra conta, lista ou favorito nao amplia o acesso.

## 6. Superficies nao-HTTP

| Recurso | ANON | SITE | WIN | AMS | MLS | OSDB | MAL | Permissao necessaria no alvo |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| SQL LocalDB `Dtudo2026Db` | - | - | - | W/R | - | W/R conforme processo | - | `db.owner` somente para ApiMyAnimes; retirar acesso do WinApp |
| Arquivo `App_Data/auth-users*.json` (legado, removido) | - | - | - | - | - | - | - | Nao existe superficie ativa; identidade usa banco proprio da `ApiIdentity` |
| Raiz de analise escolhida no WinApp | - | - | R | - | - | R conforme ACL | - | Origem local somente leitura; nao e raiz protegida da ApiFileStorage |
| Raiz de exportacao e imagens | - | - | - | - | - | W conforme ACL | - | `filesystem.command`, plano por IDs, quarentena, scanner e lixeira na ApiFileStorage |
| `LogsImportacao` | - | - | W/R | - | - | W/R conforme ACL | - | Escrita controlada e leitura minima; auditoria separada |
| URLs de imagem e dados externos | - | R | R | - | R | - | W/R | `service.mal.read` e egress allowlist; validar resposta |
| `localStorage.auth_user` / `auth_token` (legado, removido) | - | - | - | - | - | - | - | Nenhum token de identidade e acessivel ao React; sessao fica no BFF |
| Processos `dotnet`, `npm`, Chrome | - | - | W/R sobre processo do usuario | - | - | W/R conforme host | - | Operacao administrativa local; `sqllocaldb` foi removido do WinApp na Etapa 24 |

## 7. Recursos e propriedade

Os modelos globais de catalogo continuam sem `OwnerUserId`: `Anime` usa `MalId` e `MyAnime` continua sendo uma colecao interna do DB_Local. A propriedade pessoal da Etapa 18 foi modelada separadamente na `ApiIdentity`:

- favoritos usam `IdentityPersonalFavorites` e unicidade por conta/recurso;
- preferencias usam `IdentityPersonalPreferences` e chave composta por conta/chave;
- listas usam `IdentityPersonalLists`, com `IdentityPersonalListItems` vinculados a conta e lista;
- pedidos de exclusao usam `IdentityPersonalDataDeletionRequests` e conservam estado, processamento e retencao;
- termos aceitos continuam ligados ao documento versionado e a conta;
- nenhuma tabela pessoal recebe o proprietario de um campo enviado pelo cliente: o proprietario e sempre a conta autenticada.

## 8. Regras de negacao por padrao adotadas na Etapa 14

Estas regras registram o destino e a classificacao implementada nas duas APIs:

1. Ausencia de identidade, audiencia, escopo ou permissao deve resultar em negacao.
2. CORS, URL local, rede LAN ou conhecimento de um ID nao concedem permissao.
3. Rotas de escrita e exclusao nunca devem depender apenas da UI do WinApp.
4. Rotas `AuthController` legadas permanecem removidas e nao podem ser usadas como prova de identidade.
5. APIs internas devem aceitar somente o servico autorizado e a rota necessaria.
6. O WinApp deve enviar IDs e comandos para arquivos, nunca caminhos absolutos/UNC livres.
7. O browser nao deve receber access token, refresh token ou credencial de servico.
8. Health, Swagger/OpenAPI e mensagens de erro devem revelar somente o necessario para seu publico.

## 9. Classificacao efetiva da Etapa 14

- `ApiMyAnimes` exige `Authentication:Issuer` HTTPS e `Authentication:Audience` no startup. O bearer valida issuer, audience, assinatura, lifetime e rejeita configuracao ausente.
- A fallback policy exige identidade autenticada. As politicas nomeadas usam o formato `permission:{chave}` e exigem a claim `permission` e o mesmo escopo em `scope` ou `scp`.
- As leituras `GET /apiLocal/Anime`, `GET /apiLocal/Anime/buscar`, `GET /apiLocal/Anime/{malId}`, `GET /apiLocal/MyAnime` e `GET /apiLocal/MyAnime/{id}` sao declaradas publicas com `AllowAnonymous` para preservar o catalogo publico local.
- As operacoes de criacao, conflito de titulo, atualizacao total e parcial exigem `permission:catalog.write` com escopo `catalog.write`; exclusoes exigem `permission:catalog.delete` com escopo `catalog.delete`.
- `GET /apiLocal/Health` exige `permission:health.read` com escopo `health.read`. As tres rotas `/apiLocal/Auth/*` foram removidas na Etapa 20 e nao possuem compatibilidade anonima.
- `ApiMyAnimeList` exige `Authentication:Issuer` HTTPS e audience proprio configurado. Health exige `health.read`; search, detalhes e relacoes exigem `permission:service.mal.read` com escopo `service.mal.read`. Nao ha rota anonima direta nessa API; a exposicao publica futura deve ocorrer pelo gateway.
- Swagger da `ApiMyAnimes` fica somente em Development e exige `health.read`; OpenAPI da `ApiMyAnimeList` fica somente em Development e exige a mesma politica. Falha de issuer/audience impede o startup.

## 10. Evidencias e pendencias

Evidencias principais: `ApiMyAnimes/Controllers/*.cs`, `ApiMyAnimes/Program.cs`, `ApiMyAnimeList/Controllers/MyAnimeListController.cs`, `ApiMyAnimeList/Program.cs`, `WinAppDtudo/Services/ApiMyAnimesService.cs`, `WinAppDtudo/Services/MyAnimeListApiService.cs`, `DtudoSite/src/hooks/useAuth.js`, `DtudoSite/src/api_conect/conectApiLocal.js` e os documentos de inventario/modelo desta etapa.

Evidencias da Etapa 14: testes de 401 anonimo, 403 por claim/escopo ausente, acesso autorizado ate a validacao do controller, leituras publicas, health protegido, Swagger/OpenAPI restritos e falha fechada de issuer/audience nos projetos de testes das duas APIs.

Evidencias da Etapa 15: endpoint de Client Credentials separado do authorization code, rejeicao de segredo compartilhado, validacao de client ID/certificado/EKU, audience e escopo exclusivos, carregamento do certificado pelo Store, overlap de certificado anterior e middleware de validacao na `ApiMyAnimeList`. A operacao local de ACL foi aplicada, validada, reaplicada sem mudanca e revertida com snapshot.

Pendencias que nao bloqueiam a classificacao desta etapa, mas bloqueiam a publicacao: provisionamento de certificados reais e contas de servico em Homologation/Production, exercicio mTLS com processos reais, integracao live do gateway/BFF com o provider OIDC, worker de processamento/purge da retencao LGPD, servico de arquivos, firewall e exposicao real das portas. A API aceita bearer JWT somente quando o issuer configurado fornecer descoberta/chaves validas; a integracao dos tokens opacos da Etapa 13 permanece nas etapas de identidade/gateway.

Evidencia posterior da Etapa 16: `DtudoGateway` possui YARP com somente cinco leituras publicas de catalogo e os dois callbacks OIDC necessarios, sem proxy generico, com cookie server-side, PKCE, antiforgery, allowlist de redirects e testes negativos locais.

Evidencia da Etapa 18: `ApiIdentity` possui owner authorization para recursos pessoais, maioridade sem nascimento completo, termos com hash/versionamento, exportacao sem segredos de autenticacao, solicitacao de exclusao com janela de sete dias, retencao de doze meses e auditoria. Os testes `IdentityPrivacyServiceTests` passaram `6/6`, o teste de startup confirmou `401` para rota pessoal anonima e a suite completa passou `55/55`.

## 11. Atualizacao pos-Etapa 20

- `ApiMyAnimes` nao possui mais `AuthController`, `LocalAuthService`, `AuthOptions`, arquivo JSON de usuarios, DTOs de login local ou wrapper PBKDF2.
- Os endpoints historicos `/apiLocal/Auth/register`, `/apiLocal/Auth/login` e `/apiLocal/Auth/me/{id}` nao sao mapeados e retornam `404`.
- O gate de identidade reexecutou negativos e rollback: `ApiIdentity.Tests` `57/57`, `ApiMyAnimes.Tests` completo `18/18` (incluindo auth/startup/auditoria `10/10`), mTLS `10/10`, `DtudoGateway.Tests` `10/10`, `WinAppDtudo.Tests` `10/10`, `ApiMyAnimeList.Tests` `13/13` e `LibDtudo.Shared.Tests` `24/24`.
- A publicacao continua condicionada a certificados, contas de servico, issuer/discovery e configuracao dos ambientes de homologacao/producao. As Etapas 21 e 22 foram concluidas no Development local; a Etapa 24 removeu o acoplamento SQL/LocalDB do WinApp, mas a migracao de arquivos continua na Etapa 25.

## 12. Atualizacao da Etapa 24 - remocao do SQL direto do WinApp

O inventario da Etapa 23 nao encontrou `DbContext`, EF, `SqlConnection` ou
outra consulta SQL no `WinAppDtudo`. A operacao `sqllocaldb.exe start` de
`DtudoSiteStartupService` foi removida na Etapa 24; o ciclo de vida do banco
permanece responsabilidade do host e da `ApiMyAnimes`.

O `WinAppDtudo` usa o `ApiMyAnimesService` compartilhado pelas telas e pela
importacao, com bearer obtido da sessao PKCE/DPAPI. As mutacoes usam os
comandos protegidos por `catalog.write`:

- `PUT /apiLocal/catalog-migration/my-animes/by-title`, com chave natural de
	titulo e mesclagem idempotente de `AnimesMalId`;
- `PUT /apiLocal/catalog-migration/animes/{malId}/my-anime`, que garante
	simultaneamente `Anime.MyAnimeID` e a lista `MyAnime.AnimesMalId`.

A criacao do anime continua usando o `POST /apiLocal/Anime` autorizado; em
caso de replay/conflito, o cliente nao substitui os detalhes e executa o PUT
de associacao. O feedback da importacao e a ordem de analise, colecao, anime,
associacao e arquivos foram preservados. `catalog.delete` foi incluido nos
escopos do client do WinApp para as telas de exclusao existentes.

As buscas negativas da Etapa 24 nao encontraram `sqllocaldb`,
`LocalDbInstanceName`, `DTUDO_LOCALDB_INSTANCE`, `ConnectionStrings`,
`DbContext`, EF, `SqlConnection`, `SqlCommand`, `UseSqlServer` ou `db.owner`
no codigo/configuracao fonte do WinApp. `bin/` e `obj/` foram excluidos da
varredura por serem artefatos gerados.

Os contratos completos, a ordem de migracao e a matriz acesso -> substituto
estao em `docs/security/ETAPA_23_INVENTARIO_WINAPP.md`. A `ApiFileStorage` ja
possuia os contratos minimos de `resolve`, `import`, `delete` e `reconcile`,
todos sob `filesystem.command`; nenhum endpoint generico de caminho/diretorio
foi criado.

## 13. Atualizacao pos-Etapa 25

O `WinAppDtudo` nao grava mais imagens nem cria diretorios na raiz de
exportacao. `CriadorDeEstruturas` baixa a capa em memoria, solicita
`POST /api/file-storage/export/plan` por IDs e envia multipart para
`POST /api/file-storage/import` com `ObjectId` e `Idempotency-Key`.

Exclusoes em massa usam `delete/preview` e `delete/batch`. A previa e vinculada
ao ator, sessao e dispositivo; o lote consulta o grant de step-up da
`ApiIdentity` para `filesystem.command` e cada item passa pelo lifecycle que
move para a lixeira por sete dias. O WinApp confirma a previa e solicita TOTP
antes do lote.

A verificacao negativa encontrou zero acessos a `Directory`, `File`, `Path`,
seletor de pasta ou APIs de ACL nos arquivos migrados. Permanecem somente a
leitura da origem local escolhida pelo operador, configuracao/DPAPI,
descoberta de ferramentas e log diagnostico, conforme a matriz.
