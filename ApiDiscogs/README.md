# ApiDiscogs

API ASP.NET Core .NET 10 que encapsula a API externa Discogs. Nesta parte, o projeto fornece a infraestrutura autenticada, o health local e o cliente HTTP nomeado para as partes seguintes. Nao existe banco local nem endpoint de gravacao.

## Execucao local

A API escuta, pelo launch profile `ApiDiscogs`, em:

- HTTPS: `https://localhost:7147`
- HTTP: `http://localhost:7148`
- Swagger: `https://localhost:7147/swagger`
- Health local: `https://localhost:7147/ApiDiscogs/health`

O health e protegido por JWT e exige a permissao e o escopo `health.read`. O Swagger exige a mesma permissao em ambiente de desenvolvimento.

## Segredo Discogs

O token nao pertence a nenhum arquivo versionado. Configure-o no armazenamento de user-secrets do projeto:

```powershell
dotnet user-secrets --project .\ApiDiscogs\ApiDiscogs.csproj set "ApiDiscogs:Token" "TOKEN_LOCAL_FORA_DO_REPOSITORIO"
```

Em ambientes automatizados, use a variavel segura `ApiDiscogs__Token`. O processo falha no startup quando o token esta ausente ou invalido. Nunca coloque esse valor em `appsettings.json`, `appsettings.Development.json`, `DtudoSite`, `WinAppDtudo` ou arquivos de teste.

## Validacao

```powershell
dotnet build .\ApiDiscogs\ApiDiscogs.csproj
dotnet run --project .\ApiDiscogs\ApiDiscogs.csproj --launch-profile ApiDiscogs
```

Envie um JWT emitido pelo `ApiIdentity` no header `Authorization` para consultar o health ou o Swagger. O cliente `HttpClientFactory`, o egress restrito e as politicas de resiliencia da Parte 2.3 ficam disponiveis para os endpoints normalizados da Parte 2.4.

O `BaseUrl` e fixado por options validadas para `https://api.discogs.com/`. O handler de egress rejeita hosts, portas, redirecionamentos e caminhos fora da allowlist; nenhuma requisicao aceita URL fornecida pelo cliente.
O cliente usa cache em memoria por parametros normalizados, limite de resposta configuravel, timeout por tentativa e total, retry seletivo, `Retry-After` para `429` e circuit breaker. O token e aplicado por um handler interno somente ao request externo.

## Ajustes coordenados

- `ApiIdentity`: registrar o recurso `urn:dtudo:api-discogs` no provisionamento OpenIddict e manter `health.read` e `catalog.read` disponiveis para o cliente autorizado. Uma permissao dedicada `service.discogs.read` pode ser adicionada em uma etapa posterior se a politica deixar de reutilizar `catalog.read`.
- `WinAppDtudo`: adicionar `ApiDiscogs:BaseUrl` e `ApiDiscogs:AutoStartUrl` apontando para `https://localhost:7147`, incluir `urn:dtudo:api-discogs` em `Identity.Resources` e criar o health/startup service correspondente. O WinApp nunca recebe `ApiDiscogs:Token`.
- Scripts de inicializacao: iniciar este projeto com o perfil `ApiDiscogs` ou com `--urls https://localhost:7147`, garantir o certificado HTTPS local e fornecer `ApiDiscogs__Token` somente como segredo do processo. Nao passar token em argumentos, logs ou arquivos.

Uma probe sem JWT retorna `401 Unauthorized` por desenho. O health protegido confirma a disponibilidade local depois que o `ApiIdentity` emitir um token com audience `urn:dtudo:api-discogs` e a permissao `health.read`.
