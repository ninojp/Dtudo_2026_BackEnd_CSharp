# Contrato externo e politica de acesso: ApiDiscogs

**Status:** aprovado para a Parte 2.1
**Escopo:** contrato de leitura externa, seguranca, erros e limites da ApiDiscogs
**Fora do escopo:** cliente HTTP completo, controllers, cache implementado, migrations, persistencia e integracao do WinAppDtudo

## 1. Decisao resumida

A `ApiDiscogs` sera uma API de leitura que encapsula a API externa Discogs. Seus consumidores conhecem somente os contratos normalizados da Dtudo2026. O token, as URLs externas, os detalhes de resiliencia e o formato bruto da Discogs permanecem no servidor.

O fluxo permitido e:

```text
WinAppDtudo ou DtudoSite -> ApiDiscogs -> API externa Discogs
WinAppDtudo -> ApiMusicX -> SQL Server da Colecao
```

A `ApiDiscogs` nao chama a `ApiMusicX`, nao possui o SQL Server da Colecao e nao oferece operacoes de salvar, alterar ou excluir dados locais.

## 2. Evidencias e analise do uso atual

### 2.1 Operacoes observadas no proxy Node

Foram analisados somente os arquivos relacionados a Discogs em `ApiNode/mymusicx` e os consumidores atuais do `DtudoSite`.

| Uso atual | Operacao Discogs observada | Decisao para a ApiDiscogs |
|---|---|---|
| Sugestoes de artista em `/api/discogs/artists` | `GET /database/search` com `type=artist`, `q` e `per_page=10` | Manter como busca de artistas normalizada em `/ApiDiscogs/artists/search` |
| Discografia em `/api/discogs/search` | `GET /artists/{id}/releases`, primeira requisicao com `per_page=100`, seguida por `pagination.urls.next` | Expor discografia paginada em `/ApiDiscogs/artists/{id}/releases`; nunca seguir uma URL externa sem reconstruir e validar o caminho |
| Normalizacao da discografia | Deduplicacao por `master_id` ou `id`, agregacao de formatos e papeis, filtragem de `unofficial` e classificacao em album, single/EP, compilacao ou video | Representar em `DiscogsReleaseSummary` com `canonicalId`, `category`, `formats` e `roles`; nao expor `_category` nem o objeto bruto |
| Enriquecimento de masters | `GET /releases/{main_release}` para masters sem formato, em lotes de 10, com atraso de 400 ms entre lotes | Tratar como expansao opcional e limitada; falha de dado complementar gera aviso de incompletude, nao sucesso falso |
| Detalhe em `/api/discogs/release/:id` | Tenta `GET /masters/{id}` e, em qualquer falha, tenta `GET /releases/{id}` | Separar os contratos de release e master. O consumidor escolhe o tipo e uma falha de autorizacao, limite ou servidor nao pode ser mascarada por fallback |
| `/api/discogs/save` | Grava `artistasDiscografia.json` no processo Node | Nao migrar. A gravacao confirmada pertence ao WinAppDtudo via ApiMusicX |
| `/health/live` | Health local do processo Node | Expor health local em `/ApiDiscogs/health`, sem exigir que a Discogs esteja disponivel para provar que o processo esta vivo |

O endpoint `/mymusicx` do proxy e leitura do JSON legado, nao uma operacao Discogs. Ele pertence ao fluxo de migracao/consulta local da ApiMusicX e nao entra neste contrato.

### 2.2 Comparacao com ApiMyAnimeList

A ApiMyAnimeList e a referencia de infraestrutura para a Parte 2.1 em vez de um contrato de dominio a ser copiado. A ApiDiscogs deve reutilizar as seguintes decisoes ja existentes:

- options tipadas vinculadas por configuracao e validadas no startup;
- `HttpClientFactory` como unica porta de egress;
- `AllowedHosts` e `AllowedPathPrefix` validados antes de iniciar;
- `User-Agent` identificavel, sem token em log;
- timeout por tentativa e timeout total;
- retry somente para operacoes idempotentes e falhas transitorias;
- respeito a `Retry-After`, circuit breaker e cache somente para leituras;
- `CancellationToken` propagado;
- autorizacao JWT com health separado da leitura externa;
- DTOs de contrato separados do payload externo;
- erros normalizados pelo controller, sem devolver o corpo bruto da dependencia.

A diferenca de dominio e que a Discogs fornece artistas, releases, masters, formatos, papeis e tracklists. A ApiDiscogs nao deve reutilizar DTOs de anime nem a regra de relacoes da ApiMyAnimeList.

## 3. Superficie publica da ApiDiscogs

Todas as rotas abaixo sao somente `GET` e exigem autenticacao JWT. O prefixo segue a convencao do controller da ApiMyAnimeList.

| Metodo e rota | Fonte externa | Permissao | Resultado |
|---|---|---|---|
| `GET /ApiDiscogs/health` | Nenhuma; verifica somente o processo local | `health.read` | `HealthResponse` |
| `GET /ApiDiscogs/artists/search` | `/database/search?type=artist` | politica `catalog.external.read` | `DiscogsPagedResponse<DiscogsArtistSearchItem>` |
| `GET /ApiDiscogs/artists/{discogsArtistId}` | `/artists/{id}` | politica `catalog.external.read` | `DiscogsArtistDetails` |
| `GET /ApiDiscogs/artists/{discogsArtistId}/releases` | `/artists/{id}/releases` | politica `catalog.external.read` | `DiscogsArtistReleasesResponse` |
| `GET /ApiDiscogs/releases/{discogsReleaseId}` | `/releases/{id}` | politica `catalog.external.read` | `DiscogsReleaseDetails` |
| `GET /ApiDiscogs/masters/{discogsMasterId}` | `/masters/{id}` | politica `catalog.external.read` | `DiscogsMasterDetails` |

Nao fazem parte do contrato:

- `POST`, `PUT`, `PATCH` ou `DELETE` para dados da Colecao;
- endpoint que receba uma URL externa arbitraria;
- endpoint que receba ou devolva token Discogs;
- endpoint que leia `ApiNode/mymusicx/mymusicx.json`;
- chamada direta da Discogs pelo `WinAppDtudo` ou `DtudoSite`.

### 3.1 Busca de artistas

```text
GET /ApiDiscogs/artists/search?q={termo}&page={pagina}&perPage={quantidade}
```

Parametros normalizados:

| Parametro | Tipo | Obrigatorio | Padrao | Limite |
|---|---|---:|---:|---:|
| `q` | `string` | sim | nenhum | 2 a 120 caracteres apos `Trim`; nao aceitar somente espacos |
| `page` | `int` | nao | `1` | maior ou igual a 1 |
| `perPage` | `int` | nao | `10` | de 1 a 20 para esta operacao |

O cliente externo traduzira `perPage` para `per_page` e sempre acrescentara `type=artist`. O texto `artistName` usado pelo proxy antigo nao sera aceito como fonte de verdade: o nome da resposta deve vir do resultado da Discogs.

A resposta contem somente itens do tipo artista. Resultado sem itens e uma resposta valida `200`, com `items: []` e paginacao consistente.

### 3.2 Detalhes de artista

```text
GET /ApiDiscogs/artists/{discogsArtistId}
```

`discogsArtistId` deve ser um inteiro decimal positivo, sem sinal, sem espacos e sem segmentos de caminho adicionais. O endpoint devolve o perfil normalizado do artista, aliases, membros, URLs publicas e imagens quando existirem.

### 3.3 Discografia paginada do artista

```text
GET /ApiDiscogs/artists/{discogsArtistId}/releases?page={pagina}&perPage={quantidade}&expand={expansao}
```

Parametros:

| Parametro | Tipo | Obrigatorio | Padrao | Limite |
|---|---|---:|---:|---:|
| `page` | `int` | nao | `1` | maior ou igual a 1 |
| `perPage` | `int` | nao | `50` | de 1 a 100 |
| `expand` | `string` | nao | `none` | somente `none` ou `master` |

Regras:

- `perPage=100` preserva o limite usado pelo proxy antigo, mas a API local nunca aceita mais de 100 itens por pagina.
- A resposta representa uma pagina; nao existe requisicao sem limite que descarregue uma discografia inteira.
- `expand=none` retorna os dados da listagem. `expand=master` pode buscar detalhes do `main_release` apenas para completar formatos ou campos necessarios a classificacao.
- A expansao e limitada a no maximo 10 requisicoes complementares por pagina, conforme o padrao observado no Node. Quando o limite for atingido, os itens permanecem na resposta com `isComplete=false` e `warnings` explicitos.
- Releases com formato `unofficial` sao excluidos por padrao, preservando o comportamento atual. A Parte 2.4 pode propor um filtro explicito somente se houver consumidor que precise deles; isso nao sera inferido por URL ou payload arbitrario.
- A classificacao inicial sera `album`, `singleEp`, `compilation`, `video` ou `unknown`. O formato original fica preservado em `formats`.
- A deduplicacao dentro da pagina usa `canonicalId`: `master:{id}` quando houver `master_id`, caso contrario `release:{id}`. Formatos e papeis repetidos sao agregados. Ao acumular varias paginas, o consumidor tambem deve deduplicar por `canonicalId`.
- O total da paginacao representa os registros da fonte externa. `uniqueItemsInPage` informa quantos itens ficaram depois da deduplicacao local.
- Uma operacao interna que precise percorrer varias paginas tera limite de `20` paginas ou `1000` itens por solicitacao de orquestracao. Ao atingir o limite, a resposta deve indicar `isComplete=false`; nunca continuar indefinidamente.

A API nao seguira cegamente `pagination.urls.next` devolvido pela Discogs. O cliente reconstruira a rota relativa permitida a partir de `page`, `perPage` e dos parametros aprovados.

### 3.4 Detalhes de release

```text
GET /ApiDiscogs/releases/{discogsReleaseId}
```

O endpoint devolve um release concreto, incluindo tracklist, formatos, labels, generos, estilos, artistas creditados, imagens e a referencia ao master quando a fonte fornecer esses dados. Campos opcionais ausentes sao representados como `null` ou lista vazia, com `warnings` somente quando a ausencia afetar uma parte esperada do contrato.

### 3.5 Detalhes de master release

```text
GET /ApiDiscogs/masters/{discogsMasterId}
```

Master e release sao recursos diferentes no contrato. A resposta preserva `masterId` e `mainReleaseId` e pode conter versoes resumidas. O endpoint nao converte automaticamente um master em release nem faz fallback silencioso. Para os detalhes do release principal, o consumidor deve chamar explicitamente `/releases/{mainReleaseId}`.

### 3.6 Health

```text
GET /ApiDiscogs/health
```

O health verifica somente a disponibilidade local da ApiDiscogs e de suas configuracoes obrigatorias. Nao chama a Discogs, nao devolve token, host externo, connection string ou estado interno de circuito. A resposta esperada e:

```json
{
  "status": "ok",
  "service": "ApiDiscogs"
}
```

Uma verificacao da dependencia externa, se necessaria para monitoramento, sera uma probe interna separada e nao mudara o contrato do health local.

## 4. DTOs normalizados

Os nomes abaixo sao o contrato conceitual para a Parte 2.4. Entidades da Discogs, JSON bruto e propriedades com nomes como `main_release`, `master_id` ou `_category` nao atravessam a fronteira publica.

### 4.1 DTOs de entrada

Nao ha corpo JSON em nenhum endpoint da Parte 2.1. As entradas sao query strings e IDs de rota validados:

```text
ArtistSearchQuery
- Query: string
- Page: int = 1
- PerPage: int = 10

ArtistReleasesQuery
- Page: int = 1
- PerPage: int = 50
- Expand: none | master = none

DiscogsResourceIdRoute
- Id: int positivo
```

O DTO de entrada nao possui `Token`, `Authorization`, `Url`, `BaseUrl`, `ConnectionString` ou `artistName` usado para substituir o nome externo.

### 4.2 Tipos comuns de saida

`DiscogsSourceReference`:

| Campo | Tipo | Regra |
|---|---|---|
| `provider` | `string` | sempre `Discogs` |
| `resourceType` | `string` | `artist`, `release`, `master` ou `track` |
| `id` | `string` | ID externo preservado como texto no DTO |
| `resourceUrl` | `string?` | URL publica da Discogs somente quando validada e necessaria para exibicao; nunca usada como URL de egress |

`DiscogsPagination`:

| Campo | Tipo | Regra |
|---|---|---|
| `page` | `int` | pagina atual, iniciando em 1 |
| `perPage` | `int` | limite efetivamente aplicado |
| `totalItems` | `int?` | total informado pela fonte, quando valido |
| `totalPages` | `int?` | total informado pela fonte, quando valido |
| `hasNextPage` | `bool` | calculado a partir da paginacao validada |
| `uniqueItemsInPage` | `int?` | quantidade apos deduplicacao quando aplicavel |

`DiscogsImage`:

```text
- Type: string?
- Uri: string?
- Width: int?
- Height: int?
```

URLs de imagem sao dados de saida e devem ser validadas como HTTPS antes de serem entregues. Elas nao autorizam a ApiDiscogs a buscar hosts adicionais nesta parte.

### 4.3 Busca e artista

`DiscogsArtistSearchItem`:

```text
- Source: DiscogsSourceReference
- Name: string
- Type: artist
- ThumbnailUrl: string?
- ImageUrl: string?
```

`DiscogsArtistDetails`:

```text
- Source: DiscogsSourceReference
- Name: string
- RealName: string?
- Profile: string?
- Aliases: IReadOnlyList<DiscogsNameReference>
- Members: IReadOnlyList<DiscogsNameReference>
- Urls: IReadOnlyList<string>
- Images: IReadOnlyList<DiscogsImage>
- IsComplete: bool
- Warnings: IReadOnlyList<string>
```

`DiscogsNameReference` possui `Id: string?` e `Name: string`.

`DiscogsPagedResponse<T>`:

```text
- Source: string = Discogs
- Items: IReadOnlyList<T>
- Pagination: DiscogsPagination
- IsComplete: bool
- Warnings: IReadOnlyList<string>
```

### 4.4 Discografia e release resumido

`DiscogsReleaseSummary`:

```text
- Source: DiscogsSourceReference
- CanonicalId: string       // master:{id} ou release:{id}
- ResourceType: release | master
- Title: string
- ArtistName: string?
- ArtistId: string?
- Year: int?
- MasterId: string?
- MainReleaseId: string?
- Role: string?
- Roles: IReadOnlyList<string>
- Formats: IReadOnlyList<string>
- Category: album | singleEp | compilation | video | unknown
- ThumbnailUrl: string?
- ImageUrl: string?
- IsComplete: bool
- Warnings: IReadOnlyList<string>
```

`DiscogsArtistReleasesResponse`:

```text
- Source: string = Discogs
- Artist: DiscogsNameReference
- Items: IReadOnlyList<DiscogsReleaseSummary>
- Pagination: DiscogsPagination
- IsComplete: bool
- Warnings: IReadOnlyList<string>
```

A categoria e um valor normalizado para a aplicacao. `Formats` e `Role/Roles` preservam os valores externos uteis para auditoria e para uma futura decisao de importacao. Nao existe no contrato uma propriedade com nome `Total` em maiusculas ou um objeto `summary` copiado do proxy; um adaptador de migracao pode produzir o formato temporario que uma tela antiga ainda exigir.

### 4.5 Release, master e tracklist

`DiscogsReleaseDetails`:

```text
- Source: DiscogsSourceReference      // resourceType=release
- Title: string
- Year: int?
- Released: string?
- Country: string?
- Status: string?
- MasterId: string?
- Artists: IReadOnlyList<DiscogsCredit>
- Labels: IReadOnlyList<DiscogsLabel>
- Genres: IReadOnlyList<string>
- Styles: IReadOnlyList<string>
- Formats: IReadOnlyList<string>
- Tracklist: IReadOnlyList<DiscogsTrack>
- Images: IReadOnlyList<DiscogsImage>
- Notes: string?
- IsComplete: bool
- Warnings: IReadOnlyList<string>
```

`DiscogsMasterDetails`:

```text
- Source: DiscogsSourceReference      // resourceType=master
- Title: string
- MainReleaseId: string?
- Year: int?
- Genres: IReadOnlyList<string>
- Styles: IReadOnlyList<string>
- Artists: IReadOnlyList<DiscogsCredit>
- Versions: IReadOnlyList<DiscogsReleaseSummary>
- Images: IReadOnlyList<DiscogsImage>
- IsComplete: bool
- Warnings: IReadOnlyList<string>
```

`DiscogsTrack`:

```text
- Position: string?
- Title: string
- DurationSeconds: int?
- DurationText: string?
- Artists: IReadOnlyList<DiscogsCredit>
- ExtraArtists: IReadOnlyList<DiscogsCredit>
```

`DiscogsCredit` possui `Id: string?`, `Name: string` e `Role: string?`. `DiscogsLabel` possui `Name: string`, `CatalogNumber: string?` e `Id: string?`.

### 4.6 Erro normalizado

Toda falha HTTP da dependencia que chegar ao controller sera convertida para `ProblemDetails` com extensoes estaveis:

```json
{
  "type": "https://dtudo.local/problems/discogs-dependency",
  "title": "A fonte externa de musicas nao esta disponivel.",
  "status": 429,
  "detail": "A consulta foi limitada temporariamente pela fonte externa.",
  "code": "discogs_rate_limited",
  "retryAfterSeconds": 12,
  "traceId": "correlation-id"
}
```

`detail`, `code` e `retryAfterSeconds` nao podem conter token, header de autorizacao, URL arbitraria, corpo bruto ou connection string. `retryAfterSeconds` e omitido quando nao houver valor confiavel.

## 5. Mapeamento de status e falhas

A autenticacao local acontece antes da chamada externa. Portanto, `401` e `403` gerados pelo middleware da ApiDiscogs indicam problema no JWT, permissao ou escopo do consumidor e nao devem ser confundidos com o mesmo status devolvido pela Discogs.

| Resultado da Discogs ou da infraestrutura | Resposta local | `code` sugerido | Comportamento |
|---|---:|---|---|
| `200` com envelope valido | `200` | nenhum | Mapear para DTO normalizado |
| `200` com campo opcional ausente | `200` | nenhum | Usar `null`/lista vazia e registrar `warnings` se afetar completude |
| `200` sem envelope, ID/titulo obrigatorio ou paginacao valida | `502` | `discogs_invalid_response` | Nao transformar resposta quebrada em lista vazia; registrar metrica sem payload sensivel |
| `400` ou `422` | `400` | `discogs_request_rejected` | Corpo normalizado; nao devolver o JSON externo |
| `401` ou `403` da Discogs | `502` | `discogs_configuration_error` | Tratar como falha de configuracao/segredo; nunca revelar essa causa ao cliente |
| `404` | `404` | `discogs_resource_not_found` | Recurso solicitado nao existe; nao tentar fallback de outro tipo |
| `408` ou timeout de socket | `504` | `discogs_timeout` | Retry limitado quando ainda houver prazo total |
| `429` | `429` | `discogs_rate_limited` | Respeitar `Retry-After`, aplicar retry limitado e devolver o atraso sanitizado se continuar limitado |
| `500`, `501` ou `502` | `502` | `discogs_upstream_error` | Retry seletivo e circuit breaker; nunca simular resposta vazia |
| `503` | `503` | `discogs_unavailable` | Retry seletivo; circuito aberto tambem usa `503` |
| `504` | `504` | `discogs_gateway_timeout` | Retry somente dentro do prazo total |
| erro DNS, TLS ou conexao recusada | `502` | `discogs_connection_error` | Nao expor host resolvido ou detalhes de rede |
| cancelamento do cliente | sem conversao forcada | nenhum | Propagar `CancellationToken`; nao registrar como falha da Discogs |
| falha de uma expansao opcional | `200` | nenhum | Manter item base, `IsComplete=false` e aviso por item/resposta |

Quando a fonte externa informar `Retry-After`, o cliente deve interpreta-lo como segundos ou data HTTP, limitar o atraso a uma janela operacional configurada e nao fazer retry depois do deadline total. O header local pode ser devolvido somente com um valor sanitizado. A ausencia do header usa backoff exponencial com jitter.

### 5.1 Politica de resiliencia

A configuracao inicial deve seguir os limites ja usados pela ApiMyAnimeList:

- timeout por tentativa: 20 segundos;
- timeout total: 90 segundos;
- ate 3 retries para `GET` transitorio;
- atraso inicial de 250 ms, backoff exponencial com jitter e teto de 8 segundos;
- circuit breaker com `FailureRatio=0.5`, throughput minimo 5, janela de 30 segundos e pausa de 30 segundos;
- cache de leituras por 10 minutos, ajustavel por options e sem cachear erros.

Esses valores sao defaults operacionais, nao parametros aceitos pelo consumidor. A Parte 2.2 deve transforma-los em options validadas. O cliente nao deve repetir `400`, `401`, `403` ou `404`. `429` pode ser repetido somente quando o atraso couber no deadline total e o metodo for idempotente.

As chaves de cache devem incluir rota, ID e parametros normalizados. Token, header de autorizacao e corpo externo nao entram na chave. Respostas incompletas de detalhe podem ser armazenadas somente se o contrato preservar `IsComplete=false`; erros nunca sao cacheados como sucesso.

## 6. Token, segredo e identificacao do cliente

- O segredo deve ser lido por `ApiDiscogs:Token` a partir de user-secrets em desenvolvimento ou de um secret store/variavel de ambiente protegido em outros ambientes.
- O proxy Node legado foi retirado; `DISCOGS_TOKEN` nunca e fonte de runtime da nova API.
- O token e enviado exclusivamente pelo cliente servidor no header `Authorization: Discogs token=<token>`. Nunca vai em query string, DTO, resposta, log, excecao serializada, cache, `WinAppDtudo` ou `DtudoSite`.
- O `User-Agent` deve identificar a aplicacao, por exemplo `Dtudo-ApiDiscogs/1.0`, sem colocar o token ou dados do usuario. Um contato operacional pode ser configurado separadamente quando exigido pela politica da Discogs.
- O startup deve falhar de forma clara quando o token obrigatorio estiver ausente ou invalido na configuracao do ambiente. A mensagem nao deve imprimir o valor recebido.
- Rotacao de token exige somente alteracao no segredo e reinicio/reload seguro da ApiDiscogs; nao exige alterar contratos dos consumidores.
- Testes usam `HttpMessageHandler` falso ou servidor de teste. Nenhum token real entra em fixtures, snapshots ou arquivos versionados.

## 7. Allowlist de host, caminho e egress

A configuracao de egress deve iniciar com:

```json
{
  "ApiDiscogs": {
    "BaseUrl": "https://api.discogs.com/",
    "AllowedHosts": ["api.discogs.com"],
    "AllowedPathPrefixes": [
      "/database/search",
      "/artists/",
      "/releases/",
      "/masters/"
    ]
  }
}
```

Regras obrigatorias:

1. `BaseUrl` deve ser HTTPS absoluto, sem `UserInfo`, sem porta diferente de 443 e com host exatamente `api.discogs.com` apos normalizacao de ponto final.
2. O handler deve aceitar somente os quatro grupos de caminho acima, com IDs numericos e limites de segmentos correspondentes aos endpoints definidos. `/users`, `/labels`, `/marketplace`, `/oauth`, `/database` fora de `search` e qualquer caminho adicional ficam bloqueados.
3. O cliente recebe apenas caminhos relativos construidos internamente. Nenhum controller aceita uma URL para o servidor buscar.
4. Redirect automatico fica desabilitado. Um redirect para outro host ou caminho nao e seguido.
5. A validacao do host nao pode aceitar sufixos como `api.discogs.com.example.test`. A conexao deve repetir a verificacao no handler primario e rejeitar destinos DNS privados, loopback, link-local, multicast ou de documentacao, conforme o handler equivalente da ApiMyAnimeList.
6. A URL `pagination.urls.next` da Discogs e apenas informacao; ela nunca e usada diretamente como destino. O cliente reconstrui a requisicao dentro da allowlist.
7. URLs de imagem devolvidas ao consumidor nao ampliam a allowlist de egress da ApiDiscogs. O frontend deve trata-las como midia externa validada, sem enviar a URL de volta para a API como comando.

A allowlist e uma regra de seguranca, nao somente uma validacao de configuracao. Ela deve ser testada com host alternativo, caminho proibido, redirect, URL absoluta em parametro e tentativa de alterar a porta.

## 8. Autorizacao por endpoint

A matriz inicial reutiliza a convencao da ApiMyAnimeList:

| Grupo | Policy local | Claims necessarias |
|---|---|---|
| Health | `permission:health.read` | permissao `health.read` e escopo `health.read` |
| Busca, artista, releases e masters | `permission:catalog.external.read` | usuario autenticado com `catalog.read` + escopo `catalog.read`, ou principal de servico explicitamente aprovado para leitura externa |

A policy `catalog.external.read` deve permanecer uma fronteira local. A implementacao da ApiMyAnimeList atualmente aceita `catalog.read` ou `service.mal.read`; para Discogs, a Parte 2.2 deve decidir se o mesmo principal de servico sera suficiente ou se sera provisionado um `service.discogs.read` coordenado no `ApiIdentity`. Nao criar uma permissao divergente somente dentro da ApiDiscogs.

Nao ha permissao de escrita Discogs, `catalog.write` ou `catalog.delete` neste contrato. O usuario que confirmar uma importacao sera autorizado separadamente pela ApiMusicX, e o WinAppDtudo continuara sendo o orquestrador.

## 9. Cache, limites e completude

- Somente `GET` pode ser cacheado.
- Busca usa chave com `q` normalizado, pagina e limite.
- Artista, release e master usam chave com tipo e ID.
- Respostas da Discogs nao devem ser reutilizadas entre usuarios se a autorizacao local ou politica de privacidade tornar isso inadequado; o cache nao contem token.
- Lista vazia e valida quando o envelope e a paginacao sao validos.
- Campo opcional ausente nao e erro por si so.
- Envelope invalido, ID obrigatorio ausente, pagina invalida ou lista sem paginacao confiavel e resposta incompleta de transporte e vira `502`, nunca `200` vazio.
- Falha em dado complementar solicitado por `expand=master` preserva o dado base e marca `IsComplete=false`.
- O contrato nao promete baixar imagens nem criar arquivos locais. Qualquer referencia de imagem e somente URL de saida.

## 10. Compatibilidade e migracao do proxy

O mapeamento de transicao sera:

| Rota Node | Contrato novo | Observacao |
|---|---|---|
| `/api/discogs/artists?q=...` | `/ApiDiscogs/artists/search?q=...&page=1&perPage=10` | `id/title` viram `source.id/name` |
| `/api/discogs/search?artistId=...` | `/ApiDiscogs/artists/{id}/releases?page=1&perPage=50` | A nova resposta e paginada e nao confia em `artistName` do cliente |
| `/api/discogs/release/:id` | `/ApiDiscogs/releases/{id}` ou `/ApiDiscogs/masters/{id}` | O tipo deve ser escolhido explicitamente |
| `/api/discogs/save` | sem equivalente na ApiDiscogs | Preview/confirmacao no WinAppDtudo e escrita na ApiMusicX |
| `/health/live` | `/ApiDiscogs/health` | Health local autenticado |

Enquanto o frontend antigo for migrado, um adaptador pode converter o contrato novo para a forma temporaria da tela. Esse adaptador nao deve recolocar token, URL externa arbitraria ou escrita na ApiDiscogs.

## 11. Fronteiras que a implementacao deve preservar

- A Parte 2.2 pode criar o projeto e as options, mas nao deve adicionar connection string ou `DbContext`.
- A Parte 2.3 deve concentrar todo egress em um cliente/handler autorizado e testar allowlist, 429, 5xx, timeout, circuito e cache.
- A Parte 2.4 deve devolver somente DTOs normalizados e manter os comentarios XML dos controllers.
- O `WinAppDtudo` seleciona e confirma; a ApiDiscogs somente consulta; a ApiMusicX persiste.
- A indisponibilidade da ApiDiscogs nao pode impedir leituras locais da ApiMusicX.
- Nenhuma parte desta decisao antecipa a Fase 3 de pastas e arquivos.

## 12. Referencias

- `ApiNode/mymusicx/discogsProxy.js`
- `ApiNode/mymusicx/services/discogsSearch.js`
- `ApiNode/mymusicx/services/enrichment.js`
- `ApiNode/mymusicx/services/classification.js`
- `ApiNode/mymusicx/utils/cache.js`
- `DtudoSite/src/pages/MyMusicX/MyMusicXBuscar/MyMusicXBuscar.jsx`
- `ApiMyAnimeList/Configuration/MyAnimeListOptions.cs`
- `ApiMyAnimeList/Services/MyAnimeListEgressHandler.cs`
- `ApiMyAnimeList/Services/MyAnimeListResilience.cs`
- `ApiMyAnimeList/Controllers/MyAnimeListController.cs`
- Documentacao publica de referencia: `https://www.discogs.com/developers/`

Esta decisao encerra a Parte 2.1. A proxima parte pode criar a ApiDiscogs e validar as options, mas nao deve alterar este contrato sem registrar nova decisao.

## 13. Estado da migracao do DtudoSite (Parte 2.6)

O `DtudoSite` deixou de iniciar ou chamar o `discogsProxy.js`. A leitura local continua na `ApiMusicX`; a busca externa, a discografia, os detalhes de release/master e as referencias de imagem passam pelo cliente da `ApiDiscogs`.

No ambiente atual, o `DtudoGateway` ainda nao possui cluster ou rota publica para a `ApiDiscogs`. Por isso, o cliente do site usa por padrao a rota same-origin preparada `/api/external/discogs`, encaminhada pelo Vite para o gateway quando essa fachada for publicada. Os nomes de ambiente `VITE_API_DISCOGS_BASE_URL` e `VITE_API_DISCOGS_PATH_PREFIX` permitem apontar para uma fachada local aprovada durante a transicao; nenhum deles pode apontar diretamente para `api.discogs.com` ou carregar token no navegador.

Rotas legadas e estado:

| Rota Node | Estado apos a Parte 2.6 | Criterio objetivo para retirada definitiva |
|---|---|---|
| `/api/discogs/artists` | Sem consumidor no `DtudoSite`; substituida por `/ApiDiscogs/artists/search` | Manter sem chamadas em codigo de runtime, scripts de inicializacao e deploys |
| `/api/discogs/search` | Sem consumidor no `DtudoSite`; substituida por `/ApiDiscogs/artists/{id}/releases` | Mesmo criterio da rota de artistas, confirmado por busca global e smoke test do contrato novo |
| `/api/discogs/release/:id` | Sem consumidor no `DtudoSite`; substituida por `/ApiDiscogs/releases/{id}` ou `/ApiDiscogs/masters/{id}` | Mesmo criterio, com ambos os tipos cobertos pelo teste de contrato |
| `/api/discogs/save` | Nao migrada por decisao; a persistencia pertence ao `WinAppDtudo` via `ApiMusicX` | Pode ser apagada quando nao houver consumidor externo aprovado e o fluxo de importacao do WinApp estiver validado |
| `/mymusicx` do proxy | Nao e necessario para a Colecao; a consulta local pertence a `ApiMusicX` | Pode ser apagada quando a migracao do JSON legado tiver relatorio concluido e nenhum processo de deploy iniciar o proxy |

Os caminhos `/mymusicx/...` usados como assets estaticos pelo `DtudoSite` nao sao rotas do proxy e permanecem somente enquanto as capas locais de fallback existirem. A retirada dos arquivos Node exige confirmar, no repositorio e nos manifests de deploy, ausencia de `discogsProxy.js`, das rotas acima e de qualquer processo que o inicie; a presenca de referencias historicas nesta decisao ou em documentacao nao bloqueia a retirada.
