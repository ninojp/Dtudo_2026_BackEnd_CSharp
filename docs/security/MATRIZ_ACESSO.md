# Matriz de Acesso

## 1. Objetivo e legenda

Esta matriz registra o acesso efetivo observado no estado atual e a permissao necessaria para a arquitetura-alvo. A coluna de alvo e uma referencia de planejamento, nao uma implementacao presente.

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
| `POST /apiLocal/Auth/register` | A* | A* | A | A | - | Remover cadastro publico; substituir por `identity.provision`/bootstrap local |
| `POST /apiLocal/Auth/login` | A* | A* | A | A | - | `identity.login` no servico de identidade; nao manter fluxo legado |
| `GET /apiLocal/Auth/me/{id}` | R* | R* | R | R | - | `identity.self.read` somente para o proprio usuario; admin por politica |

Observacao: `SITE` e `ANON` possuem o mesmo acesso efetivo observado porque a API nao demonstra validacao do token persistido pelo frontend. `AMS` e `WIN` tambem nao enviam uma identidade de servico observada nos clientes lidos.

## 4. Endpoints da ApiMyAnimeList

| Endpoint/recurso | ANON atual | SITE atual | WIN atual | AMS atual | MLS atual | Permissao necessaria no alvo |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `GET /ApiMyAnimeList/health` | R* | R* | R | R | R | `health.read`; detalhes internos somente para rede/servico autorizado |
| `GET /ApiMyAnimeList/search` | R* | R* | R | R | R | `catalog.read` publico via gateway; rota interna deve exigir `service.mal.read` |
| `GET /ApiMyAnimeList/{malId}` | R* | R* | R | R | R | `catalog.read` publico via gateway; chamada interna com `service.mal.read` |
| `GET /ApiMyAnimeList/{malId}/relations` | R* | R* | R | R | R | `catalog.read` publico via gateway; chamada interna com `service.mal.read` |

`MLS` representa o proprio processo ao acessar a API externa MAL, nao um chamador HTTP da API local. A fronteira HTTP interna agora usa o client ID `api-my-animes`, o audience `urn:dtudo:api-my-animelist`, o escopo `service.mal.read` e certificado de cliente com overlap controlado.

## 5. Superficies nao-HTTP

| Recurso | ANON | SITE | WIN | AMS | MLS | OSDB | MAL | Permissao necessaria no alvo |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| SQL LocalDB `Dtudo2026Db` | - | - | - | W/R | - | W/R conforme processo | - | `db.owner` somente para ApiMyAnimes; retirar acesso do WinApp |
| Arquivo `App_Data/auth-users*.json` | - | - | - | W/R | - | W/R conforme ACL | - | Somente identidade/servico designado; migrar para banco proprio |
| Raiz de analise escolhida no WinApp | - | - | R | - | - | R conforme ACL | - | `filesystem.command` em ApiFileStorage; cliente envia ID/comando, nao caminho livre |
| Raiz de exportacao e imagens | - | - | W | - | - | W conforme ACL | - | `filesystem.command`, quarentena, scanner e raiz permitida |
| `LogsImportacao` | - | - | W/R | - | - | W/R conforme ACL | - | Escrita controlada e leitura minima; auditoria separada |
| URLs de imagem e dados externos | - | R | R | - | R | - | W/R | `service.mal.read` e egress allowlist; validar resposta |
| `localStorage.auth_user` / `auth_token` | R/W no JavaScript | R/W no JavaScript | - | - | - | - | - | Nenhum token de identidade deve ser acessivel ao React |
| Processos `sqllocaldb`, `dotnet`, `npm`, Chrome | - | - | W/R sobre processo do usuario | - | - | W/R conforme host | - | Operacao administrativa local com conta e ACL restritas |

## 6. Recursos e propriedade

Os modelos atuais nao possuem `OwnerUserId` ou equivalente em `Anime` e `MyAnime`. Portanto:

- `Anime` usa `MalId` como chave primaria e representa catalogo local.
- `MyAnime` usa `Id`, titulo e lista de `AnimesMalId`; e uma colecao interna do DB_Local.
- A propriedade pessoal de favoritos, preferencias e listas prevista no plano ainda nao aparece nesses modelos.
- A matriz deve ser refinada na etapa de identidade/LGPD antes de atribuir acesso por usuario a um recurso que hoje e global.

## 7. Regras de negacao por padrao adotadas na Etapa 14

Estas regras registram o destino e a classificacao implementada nas duas APIs:

1. Ausencia de identidade, audiencia, escopo ou permissao deve resultar em negacao.
2. CORS, URL local, rede LAN ou conhecimento de um ID nao concedem permissao.
3. Rotas de escrita e exclusao nunca devem depender apenas da UI do WinApp.
4. `AuthController` legado nao deve ser usado como prova de identidade depois da migracao.
5. APIs internas devem aceitar somente o servico autorizado e a rota necessaria.
6. O WinApp deve enviar IDs e comandos para arquivos, nunca caminhos absolutos/UNC livres.
7. O browser nao deve receber access token, refresh token ou credencial de servico.
8. Health, Swagger/OpenAPI e mensagens de erro devem revelar somente o necessario para seu publico.

## 8. Classificacao efetiva da Etapa 14

- `ApiMyAnimes` exige `Authentication:Issuer` HTTPS e `Authentication:Audience` no startup. O bearer valida issuer, audience, assinatura, lifetime e rejeita configuracao ausente.
- A fallback policy exige identidade autenticada. As politicas nomeadas usam o formato `permission:{chave}` e exigem a claim `permission` e o mesmo escopo em `scope` ou `scp`.
- As leituras `GET /apiLocal/Anime`, `GET /apiLocal/Anime/buscar`, `GET /apiLocal/Anime/{malId}`, `GET /apiLocal/MyAnime` e `GET /apiLocal/MyAnime/{id}` sao declaradas publicas com `AllowAnonymous` para preservar o catalogo publico local.
- As operacoes de criacao, conflito de titulo, atualizacao total e parcial exigem `permission:catalog.write` com escopo `catalog.write`; exclusoes exigem `permission:catalog.delete` com escopo `catalog.delete`.
- `GET /apiLocal/Health` exige `permission:health.read` com escopo `health.read`. `GET /apiLocal/Auth/me/{id}` exige `permission:identity.self.read` com escopo homonimo. `Auth/register` e `Auth/login` continuam explicitamente anonimos somente como compatibilidade do fluxo legado, cuja remocao pertence a Etapa 20.
- `ApiMyAnimeList` exige `Authentication:Issuer` HTTPS e audience proprio configurado. Health exige `health.read`; search, detalhes e relacoes exigem `permission:service.mal.read` com escopo `service.mal.read`. Nao ha rota anonima direta nessa API; a exposicao publica futura deve ocorrer pelo gateway.
- Swagger da `ApiMyAnimes` fica somente em Development e exige `health.read`; OpenAPI da `ApiMyAnimeList` fica somente em Development e exige a mesma politica. Falha de issuer/audience impede o startup.

## 9. Evidencias e pendencias

Evidencias principais: `ApiMyAnimes/Controllers/*.cs`, `ApiMyAnimes/Program.cs`, `ApiMyAnimeList/Controllers/MyAnimeListController.cs`, `ApiMyAnimeList/Program.cs`, `WinAppDtudo/Services/ApiMyAnimesService.cs`, `WinAppDtudo/Services/MyAnimeListApiService.cs`, `DtudoSite/src/hooks/useAuth.js`, `DtudoSite/src/api_conect/conectApiLocal.js` e os documentos de inventario/modelo desta etapa.

Evidencias da Etapa 14: testes de 401 anonimo, 403 por claim/escopo ausente, acesso autorizado ate a validacao do controller, leituras publicas, health protegido, Swagger/OpenAPI restritos e falha fechada de issuer/audience nos projetos de testes das duas APIs.

Evidencias da Etapa 15: endpoint de Client Credentials separado do authorization code, rejeicao de segredo compartilhado, validacao de client ID/certificado/EKU, audience e escopo exclusivos, carregamento do certificado pelo Store, overlap de certificado anterior e middleware de validacao na `ApiMyAnimeList`. A operacao local de ACL foi aplicada, validada, reaplicada sem mudanca e revertida com snapshot.

Pendencias que nao bloqueiam a classificacao desta etapa, mas bloqueiam a publicacao: provisionamento de certificados reais e contas de servico em Homologation/Production, exercicio mTLS com processos reais, integracao live do gateway/BFF com o provider OIDC, propriedade de recursos pessoais, servico de arquivos, firewall e exposicao real das portas. A API aceita bearer JWT somente quando o issuer configurado fornecer descoberta/chaves validas; a integracao dos tokens opacos da Etapa 13 permanece nas etapas de identidade/gateway.

Evidencia posterior da Etapa 16: `DtudoGateway` possui YARP com somente cinco leituras publicas de catalogo e os dois callbacks OIDC necessarios, sem proxy generico, com cookie server-side, PKCE, antiforgery, allowlist de redirects e testes negativos locais.
