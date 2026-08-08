# Etapa 27 - IIS, TLS e isolamento de rede

## Estado

- Estado: `Bloqueada - preparacao implementada; homologacao externa pendente`.
- Escopo executado somente para `Homologation`; nenhum site, binding, certificado, regra de firewall ou processo de `Production` foi criado ou alterado.
- O primeiro lancamento continua catalog-only: sem login, cadastro, logout, recursos pessoais, upload, escrita ou conteudo condicionado a idade.

## Implementacao

- `DtudoGateway` recebeu perfil `PublicCatalogOnly`, destinos HTTPS, proxy confiavel somente para loopback, HSTS, headers de seguranca, limite de corpo de 1 MiB, CORS por origem exata, rate limiting por IP e health liveness minimo.
- YARP registra somente cinco rotas GET de catalogo no modo Homologation e as encaminha para `/apiLocal/Anime/public` e `/apiLocal/MyAnime/public`. Callbacks OIDC, BFF, discovery, token, Swagger, Seq, health detalhado e mutacoes nao sao registrados.
- O gateway serve o `DtudoSite/dist` catalog-only em `wwwroot` e o fallback SPA aceita somente `/` e `/animes*`. Rotas fora dessa allowlist retornam `404`.
- `ApiMyAnimes` filtra no servidor genero/classificacao adulta antes de serializar lista, busca, detalhe e IDs de colecoes publicas.
- `DtudoSite` possui `npm run build:homologation`, entrada Vite catalog-only, cliente exclusivamente BFF same-origin e varredura sem rotas privadas, endpoints internos, tokens, connection strings ou texto adulto.
- `deploy/homologation/web.config` fixa ASP.NET Core no IIS, HTTPS, HSTS, CSP same-origin para o site, headers, request limits e remocao de `X-Powered-By`.
- `deploy/homologation/service-bindings.json` declara gateway em `127.0.0.1:16443`, APIs em `127.0.0.1:16080/16081` e Seq em `127.0.0.1:15341`.
- `scripts/Invoke-DtudoHomologationEdge.ps1` e limitado a Homologation, tem `Validate` como padrao, exige hostname real/certificado/ACME/IIS/admin para Apply, configura binding/firewall/headers e guarda estado de rollback sem segredos. Provisionamento ACME usa win-acme com DNS-01; renovacao exige nome explicito da renewal de Homologation e nunca executa renovacao global.

## Allowlist publica

| Rota | Metodo | Resultado Homologation |
| --- | --- | --- |
| `/` e `/animes*` | GET/HEAD | frontend catalog-only |
| `/api/catalog/animes` | GET | lista publica filtrada |
| `/api/catalog/animes/search` | GET | busca publica filtrada |
| `/api/catalog/animes/{id}` | GET | detalhe publico filtrado |
| `/api/catalog/collections` | GET | colecoes sem IDs adultos |
| `/api/catalog/collections/{id}` | GET | colecao sem IDs adultos |
| `/health/live` | GET | status generico sem detalhes |

Qualquer outro caminho ou metodo nao e uma superficie publica. Em particular, `/bff/*`, `/identity/*`, `/auth/*`, `/swagger`, `/seq`, `/health/ready`, `/apiLocal/*` e POST/PUT/PATCH/DELETE de catalogo sao recusados.

## Validacoes executadas

- `dotnet test .\tests\DtudoGateway.Tests\DtudoGateway.Tests.csproj --no-restore`: `11/11` aprovados.
- Filtro `HomologationCatalogOnly`: `1/1` aprovado, incluindo HSTS, headers, CORS externo, health liveness, login/identidade/Swagger/Seq/health detalhado negados e POST publico negado.
- Filtro `PublicCatalogPolicyTests`: `5/5` aprovados.
- `npm run build:homologation`: aprovado; `149` modulos transformados.
- `npm run lint`: aprovado.
- Varredura do `DtudoSite/dist`: zero ocorrencias de `Hentai +18`, `hentai`, `/auth`, `bff/login`, `mymusicx`, `ninoti`, `apiLocal`, Swagger, tokens, connection strings e proxies locais.
- `Invoke-DtudoHomologationEdge.ps1 -Mode Validate -Json`: build publico `Passed`; hostname real, IIS, listeners internos, certificado, ACME e firewall `Blocked` neste workstation.

## Bloqueios obrigatorios

Os testes externos de porta, DNS, TLS real, renovacao, binding IIS, firewall e CORS por dominio nao podem ser afirmados neste Development workstation. O host nao possui `WebAdministration`, win-acme, certificado de `LocalMachine\My`, regras Etapa 27 ou servicos escutando nas portas de Homologation. O dominio versionado e deliberadamente `example.invalid` e nao pode emitir certificado.

Antes de marcar a etapa como concluida, uma janela administrativa de Homologation deve:

1. Provisionar Windows Server/IIS, contas de processo, SQL Express, certificados internos das APIs e o certificado ACME do dominio real.
2. Configurar DNS do dominio real para o host e a renewal DNS-01 sem colocar credenciais no repositorio ou nos argumentos registrados.
3. Publicar o gateway e `dist` catalog-only, executar `Validate`, revisar portas/listeners, aplicar o runner com `-Confirm`, observar Seq e testar somente o gateway externamente.
4. Exercitar `curl`/PowerShell para TLS, cadeia, HSTS, headers, CORS permitido/negado, `429`, rotas privadas, escrita, Swagger, Seq, health detalhado e uma renovacao direcionada.

## Rollback

No host de homologacao, parar o site/processo e executar o mesmo runner com o mesmo `-StatePath`:

```powershell
& .\scripts\Invoke-DtudoHomologationEdge.ps1 `
  -Mode Rollback `
  -StatePath C:\ProgramData\Dtudo2026\Homologation\Edge\state.json `
  -Confirm
```

O rollback remove somente regras de firewall registradas pela etapa, restaura o backup IIS e o `wwwroot` anterior. Nao remove bancos, dados, certificados, chaves ou Production automaticamente.

## Proxima etapa

A Etapa 28 nao foi iniciada. A Etapa 27 precisa de homologacao externa reproduzivel antes de ser marcada `Concluida`.
