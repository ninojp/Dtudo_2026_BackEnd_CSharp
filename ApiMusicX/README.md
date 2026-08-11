# ApiMusicX

API ASP.NET Core da Colecao local de musicas. A persistencia relacional da Parte 1.3 usa Entity Framework Core, SQL Server e migrations proprias.

## Endpoints documentados

- Health protegido com `health.read`: `GET https://localhost:63982/apiLocal/Health`
- Listagem paginada protegida com `catalog.read`: `GET /apiLocal/collections?page=1&pageSize=20&search=...`
- Detalhe de Colecao com releases e faixas: `GET /apiLocal/collections/{id}`
- Releases de uma Colecao: `GET /apiLocal/collections/{id}/releases?page=1&pageSize=20`
- Busca de artistas, bandas e grupos: `GET /apiLocal/artists?search=...&page=1&pageSize=20`
- Detalhe de artista: `GET /apiLocal/artists/{id}`
- Detalhe de release com faixas: `GET /apiLocal/releases/{id}`
- Criacao protegida com `catalog.write`: `POST /apiLocal/collections`
- Atualizacao protegida com `catalog.write`: `PUT /apiLocal/collections/{id}`
- Importacao normalizada protegida com `catalog.write`: `POST /apiLocal/collections/import`
- Exclusao protegida com `catalog.delete`: `DELETE /apiLocal/collections/{id}`
- Swagger de desenvolvimento: `https://localhost:63982/swagger`
- Correlacao: header `X-Correlation-ID`

Os endpoints exigem autenticacao JWT e a permissao/escopo correspondente. O health devolve `status`, `service` e `database`, sem connection strings, tokens ou outras configuracoes sensiveis.

A importacao aceita somente dados ja normalizados. Para ser idempotente, ela exige `MusicCollectionId` ou um identificador externo de recurso `Collection`. Repeticoes preenchem campos locais vazios e nao substituem valores existentes; divergencias retornam `409 Conflict` para decisao explicita do WinAppDtudo. A operacao e transacional e nao consulta a Discogs, o ApiNode ou o sistema de arquivos.

## Configuracao

O issuer JWT local e `https://localhost:7243/` e a audience da API e `urn:dtudo:api-musicx`. Em ambientes nao-Development, configure `Authentication` e `Cors:AllowedOrigins` por ambiente ou secret store. Origins de producao devem ser HTTPS e nunca wildcard.

O projeto possui User Secrets configurado para a connection string local:

```text
dotnet user-secrets --project ApiMusicX/ApiMusicX.csproj set "ConnectionStrings:LocalDbConnection" "<valor-fornecido-localmente>"
```

O startup da API exige `ConnectionStrings:LocalDbConnection`. A migration deve ser aplicada explicitamente pelo operador; a API nao chama `Database.Migrate()` durante o startup.

Para gerar ou validar migrations sem uma connection string configurada, o factory de design-time usa somente uma connection string LocalDB de desenvolvimento. Esse fallback nao e usado pelo runtime.
