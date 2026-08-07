# Etapa 18 - Recursos pessoais e LGPD

## Estado

- Estado: `Concluida no escopo de implementacao e validacao local Development`.
- Escopo executado exclusivamente nesta etapa: autorizacao por proprietario para favoritos, preferencias e listas; maioridade minimizada; termos versionados; exportacao; solicitacao/exclusao com janela de processamento, retencao e auditoria.
- A `ApiIdentity` e a proprietaria dos dados pessoais. Nenhum identificador de conta enviado pelo cliente e usado para escolher o proprietario.

## Autorizacao por proprietario

O grupo `/identity/me` exige autenticacao OpenIddict validada localmente e politicas de permissao. Cada handler deriva o `AccountId` da claim `NameIdentifier` ou `sub` em `HttpContext.User`. Os contratos de favoritos, preferencias, listas e exportacao nao recebem `AccountId` do cliente.

As consultas e mutacoes filtram pelo `AccountId` derivado da sessao. Portanto, conhecer um GUID, chave de recurso ou ID de lista de outra conta nao concede acesso: leituras retornam somente os dados do proprietario; mutacoes cruzadas retornam `false`, `404` ou `400` conforme a operacao.

Os recursos persistidos sao:

- `IdentityPersonalFavorites`: favorito por conta e recurso, com unicidade em `(AccountId, ResourceType, ResourceKey)`.
- `IdentityPersonalPreferences`: preferencia por conta e chave, com chave composta `(AccountId, Key)`.
- `IdentityPersonalLists`: lista por conta, com nome e timestamps UTC.
- `IdentityPersonalListItems`: item ligado simultaneamente a conta e lista; a relacao conta/lista impede que um item seja usado para atravessar proprietarios.

Os tipos de recurso aceitos sao somente `anime` e `my-anime`. As chaves de recurso, nomes de lista, posicoes e valores de preferencia possuem limites de tamanho, caracteres e allowlists. As chaves de preferencia aceitas sao `catalog-sort`, `language`, `notifications` e `theme`.

Permissoes adicionadas ao catalogo central e atribuidas aos papeis existentes:

- `personal.read`: leitura dos recursos pessoais do proprio usuario.
- `personal.write`: criacao, alteracao e exclusao dos recursos pessoais do proprio usuario.
- `privacy.export`: exportacao dos dados pessoais do proprio usuario.
- `privacy.delete`: solicitacao de exclusao dos dados do proprio usuario.

## Rotas da ApiIdentity

| Rota | Metodo | Politica | Controle de propriedade |
| --- | --- | --- | --- |
| `/identity/me/age-confirmation` | GET | `personal.read` | Conta derivada da sessao |
| `/identity/me/age-confirmation` | POST | `personal.write` | Conta derivada da sessao |
| `/identity/me/terms/{documentType}/current` | GET | `personal.read` | Documento publicado; sem conta no payload |
| `/identity/me/terms/{termsDocumentId}/accept` | POST | `personal.write` | Aceite gravado para a conta da sessao |
| `/identity/me/favorites` | GET/POST | `personal.read`/`personal.write` | Filtro por `AccountId` interno |
| `/identity/me/favorites/{favoriteId}` | DELETE | `personal.write` | ID so e aceito dentro da conta |
| `/identity/me/preferences` | GET/PUT | `personal.read`/`personal.write` | Chave composta com `AccountId` |
| `/identity/me/preferences/{key}` | DELETE | `personal.write` | Chave so e removida dentro da conta |
| `/identity/me/lists` | GET/POST | `personal.read`/`personal.write` | Lista criada e consultada pela conta |
| `/identity/me/lists/{listId}` | DELETE | `personal.write` | ID so e aceito dentro da conta |
| `/identity/me/lists/{listId}/items` | POST | `personal.write` | Lista e item exigem a mesma conta |
| `/identity/me/lists/{listId}/items/{listItemId}` | DELETE | `personal.write` | Lista, item e conta sao conferidos |
| `/identity/me/data-export` | POST | `privacy.export` | Exportacao da conta autenticada |
| `/identity/me/deletion-request` | POST | `privacy.delete` | Solicitacao da conta autenticada |

Uma requisicao anonima a uma rota protegida e desafiada pelo esquema `OpenIddict.Validation.AspNetCore` e retorna `401`; nao depende de uma barreira de UI.

## Maioridade e termos

A maioridade usa somente `IdentityAccount.HasConfirmedAdultAge` e `AdultAgeConfirmedAtUtc`. Nao existe `DateOfBirth`, nascimento completo, documento ou imagem de documento no modelo de conta.

`TermsDocument` armazena tipo, versao, conteudo, hash SHA-256, data de publicacao e estado ativo. A leitura retorna o documento ativo mais recente do tipo solicitado. O aceite referencia o `TermsDocumentId` exato, e a API somente aceita documento ativo cujo hash SHA-256 corresponde ao conteudo persistido. O mesmo aceite e idempotente por `(AccountId, TermsDocumentId)`.

## Exportacao

`PersonalDataExport` usa a versao de contrato `1` e inclui, para a conta autenticada:

- identificadores de conta e dados basicos `UserName`/`Email`;
- declaracao de maioridade e instante UTC;
- aceites com documento, versao, conteudo, hash e instante;
- favoritos, preferencias, listas e itens de lista;
- historico de solicitacoes de exclusao, incluindo estado e timestamps de retencao.

Segredos e material de autenticacao nao fazem parte do contrato. O teste serializa o export e confirma a ausencia de `PasswordHash`, `SecretHash`, `ProtectedPayload` e `TokenHash`; sessoes, dispositivos, challenges, grants, recovery tickets, tokens de seguranca e dados OpenIddict tambem nao sao exportados.

Cada exportacao grava evento de auditoria com correlacao `identity-privacy`, sem registrar segredo ou conteudo de autenticacao.

## Exclusao, retencao e auditoria

A solicitacao de exclusao:

- e idempotente enquanto houver solicitacao `Pending`;
- nao pode ser criada para a conta inicial de bootstrap;
- agenda o processamento para sete dias depois de `RequestedAtUtc`;
- e processada por `ProcessDueDeletionAsync` somente quando a data programada chegou;
- remove os recursos pessoais, aceites, segredos de ativacao, challenges, grants, tokens, sessoes, dispositivos, snapshots, recovery tickets, claims, logins, tokens Identity, papeis e tokens/authorizations OpenIddict da conta;
- remove a conta depois da limpeza transacional;
- conserva a linha do pedido como `Completed`, com `ProcessedAtUtc` e `RetentionUntilUtc` doze meses depois;
- conserva os eventos minimos de auditoria de solicitacao e conclusao por doze meses a partir do proprio `OccurredAtUtc`.

A tabela `IdentityPersonalDataDeletionRequests` possui indices para conta/estado, unicidade para uma solicitacao pendente por conta e constraints para impedir combinacoes invalidas de estado, agendamento, processamento e retencao. A auditoria continua append-only pelo `IdentityProvisioningAuditWriter`; a rotina operacional de purge deve respeitar os timestamps de retencao antes da promocao para um ambiente publicado.

## Migrations e rollback

Foram adicionadas:

- `20260807051638_AddPersonalDataPrivacy`: tabelas, indices, constraints e relacionamentos dos recursos pessoais e pedidos de exclusao.
- `20260807051857_AddPersonalPrivacyPermissions`: permissoes de privacidade e atribuicoes aos papeis `Superadministrador` e `Usuario do Site`.

O teste `PersonalDataMigrationRollsBackToThePreviousIdentityVersion` migra o banco temporario de volta para `20260807020126_AddSessionTokens` e confirma a remocao das tabelas da Etapa 18.

## Evidencias

Comandos executados:

```text
dotnet test .\tests\ApiIdentity.Tests\ApiIdentity.Tests.csproj --no-restore --filter FullyQualifiedName~IdentityPrivacyServiceTests
dotnet test .\tests\ApiIdentity.Tests\ApiIdentity.Tests.csproj --no-restore --filter FullyQualifiedName~StartsAndPublishesOpenIdDiscoveryWithoutPublicRegistration
dotnet test .\tests\ApiIdentity.Tests\ApiIdentity.Tests.csproj --no-restore
```

Resultados:

- `IdentityPrivacyServiceTests`: `6/6` aprovados, incluindo isolamento, maioridade/termos, exportacao sem segredos, exclusao com retencao/auditoria, payloads invalidos e rollback da migration.
- Teste de startup: `1/1` aprovado; a rota pessoal anonima retorna `401` e o registro publico continua `404`.
- Suite completa `ApiIdentity.Tests`: `55/55` aprovados, `0` falhas e `0` ignorados.
- O build executado pela suite concluiu com sucesso e nao houve erros de diagnostico nos arquivos da implementacao/testes.

## Riscos residuais e acoes manuais

- O fluxo OIDC live, provedor externo, expiracao e revogacao continuam dependentes da configuracao externa descrita nas etapas anteriores.
- A janela de sete dias e a retencao estao implementadas no servico e nos dados persistidos; antes de homologacao/producao deve existir um worker/agendamento operacional para processar pedidos devidos e purgar somente registros expirados.
- A conta inicial de bootstrap permanece protegida contra exclusao por esta rota; sua governanca continua sendo local e administrativa.
- Nenhum segredo real, connection string real ou certificado real foi registrado no repositorio.

## Rollback

Remover as duas migrations da Etapa 18, migrar o banco para `20260807020126_AddSessionTokens`, retirar o servico/rotas/modelos de privacidade e reverter as quatro permissoes e seus testes. Restaurar tambem esta evidencia, a matriz de acesso e a entrada correspondente no status. O rollback foi exercitado em banco LocalDB temporario; nao houve rollback destrutivo em ambiente publicado.

## Proxima etapa

A Etapa 19 e a unica proxima etapa permitida. Ela nao foi iniciada neste chat.
