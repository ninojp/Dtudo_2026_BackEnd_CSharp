# LibDtudo.Shared

A **LibDtudo.Shared** é a biblioteca de classes (.NET 10) com contratos, modelos e utilitários **realmente compartilhados** entre as APIs e o `WinAppDtudo`: observabilidade (correlação e redação de logs), autenticação serviço-a-serviço por certificado (mTLS), contratos de monitoramento e de anime/MAL, normalização de busca e utilitários de senha. Ela não deve virar um depósito de regras de uma única API.

Contexto: [00_Solucao.md](00_Solucao.md) · Segurança: [01_Solucao_Seguranca.md](01_Solucao_Seguranca.md) · [21_ApiIdentity.md](21_ApiIdentity.md) · [22_ApiMyAnimes.md](22_ApiMyAnimes.md) · [23_ApiMyAnimeList.md](23_ApiMyAnimeList.md) · [24_ApiFileStorage.md](24_ApiFileStorage.md) · [25_ApiMusicX.md](25_ApiMusicX.md) · [26_ApiDiscogs.md](26_ApiDiscogs.md) · [11_WinAppDtudo.md](11_WinAppDtudo.md).

## Visão geral

| Item | Valor |
|---|---|
| Projeto / testes | `LibDtudo.Shared` / `tests\LibDtudo.Shared.Tests` |
| Alvo | `net10.0`, nullable habilitado, documentação XML gerada |
| Pacotes | `Serilog` 4.4.0, `Microsoft.EntityFrameworkCore` 10.0.14 (mais `Design`/`Tools` como *private assets*) |

## Conteúdo

### Logging (`Logging\`)

| Tipo | Função |
|---|---|
| `CorrelationContext` | Cabeçalho `X-Correlation-ID` (≤ 64 caracteres: letras, dígitos, `.`, `-`, `_`); `AsyncLocal`; `GetOrCreate`, `Normalize`, `Push` |
| `CorrelationIdDelegatingHandler` | Propaga a correlação em `HttpClient` de saída (ApiDiscogs, ApiMyAnimeList, ApiMyAnimes) |
| `SensitiveDataRedactionEnricher` | Enricher Serilog que oculta propriedades cujo nome contém `password`, `secret`, `token`, `authorization`, `cookie`, `apikey`, `clientid`, `connectionstring`, `accesskey`, `privatekey`, `body` etc. (ApiDiscogs, ApiFileStorage, ApiMusicX, ApiMyAnimeList, ApiMyAnimes) |

### Segurança (`Security\ServiceCertificateAuthentication.cs`)

Autenticação de principais de serviço por **client credentials com mTLS**:

`ServiceClientCertificateBinding` (`ClientId`, `CertificateThumbprints`, `PreviousCertificateAcceptedUntilUtc`, `AllowedGrants` — lista de `ServiceAudienceGrant` com `Audience` e `Scopes`, que amarra cada escopo à sua audiência; `AllowedScopes`/`AllowedAudiences` são a forma legada, aceita só com uma audiência e nunca combinada com `AllowedGrants`; `GetEffectiveGrants()`, `HasValidGrants()` e `FindScopesForAudience()` centralizam a regra, usada pelo `ServiceTokenRequestValidator` e pela validação de startup da ApiIdentity e da ApiMyAnimeList) e `ServiceClientCredentialsOptions` (opções do lado cliente; `ToBinding()` gera uma única concessão).
- Lado emissor (ApiIdentity): `ServiceCertificateValidator` (certificado obrigatório, `ClientId` coerente, validade, EKU de cliente, primeira *thumbprint* ativa e a segunda aceita até uma data para rotação) e `ServiceTokenRequestValidator` (allowlist de audiences e escopos).
- Lado cliente (ApiMyAnimes): `ServiceCertificateStore` (certificado com chave privada por *thumbprint*), `ServiceAccessTokenProvider` (cache com margem de 30 s; exige endpoint HTTPS) e `ServiceAccessTokenHandler`.
- A ApiMyAnimeList valida o certificado do chamador com `ServiceCertificateValidator`.

### DTOs (`Dtos\`)

| Pasta | Conteúdo | Consumidores |
|---|---|---|
| `FileMonitoring` | `MonitoringContracts`, `CollectionSynchronizationContracts`, `MonitoringPathPolicy` (chaves de raiz e normalização de caminhos), `EpisodeVideoPolicy` (política única do que é vídeo de episódio), `AnimeFolderScanRequest`/`AnimeFolderScanResponse` (contrato do `POST monitoring/anime-folders/scan`) e `AnimeFolderDiscoveryRequest`/`AnimeFolderCandidateDto`/`AnimeFolderDiscoveryResponse` (contrato do `POST monitoring/anime-folders/discovery`: subpastas da coleção com os `MalId` das capas) | ApiFileStorage, ApiMyAnimes, WinAppDtudo |
| `MyAnimeList` | `MyAnimeListContracts` (busca, detalhes, relações) | ApiMyAnimeList, WinAppDtudo |
| Anime/MyAnime | `AdicionaAnimeDto`, `AtualizaAnimeDto`, `ObterAnimeDto`, `AdicionaMyAnimeDto`, `AtualizaMyAnimeDto`, `ObterMyAnimeDto`, `ConflitoTituloAnimeDto`, `CatalogMigrationDtos` | ApiMyAnimes, WinAppDtudo |
| `Auth` | `IdentityGovernanceContracts` (maioridade, termos, permissões, papéis) | sem consumidor fora da biblioteca identificado |

### Modelos e busca

- `Models\Anime` e `Models\MyAnime`: entidades do DB_Local (ver [22_ApiMyAnimes.md](22_ApiMyAnimes.md)); consumidas pela ApiMyAnimes e pelos testes.
- `Search\AnimeSearchText`, `AnimeSearchTextNormalizer` (ApiMyAnimes e WinAppDtudo — mesma normalização de caracteres especiais nos dois lados) e `AnimeTitleEquivalence` (ApiMyAnimes).

### Utilitários (`Utils\`)

`PasswordHasher` (ApiIdentity e testes), `ChecaForcaSenha` e `ValidaCPF`.

## Quem consome

| Consumidor | Usa |
|---|---|
| ApiMyAnimes | modelos, DTOs de anime/monitoramento, busca, logging, provider/handler de token de serviço |
| ApiMyAnimeList | contratos MAL, logging, validação de certificado de serviço |
| ApiIdentity | `PasswordHasher`, validadores/opções de serviço |
| ApiFileStorage | `MonitoringPathPolicy`/contratos, `EpisodeVideoPolicy`, `AnimeFolderState`, logging |
| ApiMusicX, ApiDiscogs | logging/correlação (os DTOs de domínio musical ficam em cada API) |
| WinAppDtudo | contratos de anime, MAL e monitoramento; normalizador de busca |
| DtudoGateway | **não referencia** a biblioteca |

## Convenções

- Entram aqui apenas contratos e utilitários com **mais de um consumidor real**; tipos de uma única API permanecem no projeto dela (caso de ApiMusicX e ApiDiscogs).
- Compatibilidade de contrato: mudanças em DTOs exigem revisar APIs e WinAppDtudo juntos (solução única).
- Nenhum segredo, caminho de usuário ou configuração de ambiente na biblioteca; opções (`*Options`) são apenas tipos, os valores vêm de cada host.
- Código novo de logging deve usar `CorrelationContext` e o enricher de redação em vez de implementações locais.

## Testes

`MonitoringPathPolicyTests` (8), `EpisodeVideoPolicyTests` (extensões, extras por palavra inteira, 0 bytes, caminho com pasta de extras e regra de estado), `PasswordHasherTests`

```powershell
dotnet test .\tests\LibDtudo.Shared.Tests\LibDtudo.Shared.Tests.csproj
```

## Pendências/Divergências

- **Possível código morto:** `ChecaForcaSenha` e `ValidaCPF` não têm consumidor fora da biblioteca; `Dtos\Auth\IdentityGovernanceContracts` também não é usado fora dela. Decidir entre remover ou manter.
- **Exceção à regra "só o comum":** a biblioteca carrega modelos, DTOs e busca específicos de anime (usados por ApiMyAnimes e WinAppDtudo).
- **Pacotes de design do EF Core** (`Design`/`Tools`) estão no csproj da biblioteca; avaliar se pertencem aos projetos de API.
- **Uso por ApiFileStorage** dos DTOs de monitoramento além de `MonitoringPathPolicy` não foi aprofundado.
