# Etapa 15 - Client Credentials e mTLS

## Estado desta execucao

- Estado: `Concluida no escopo de implementacao e validacao local Development`.
- Escopo: somente HTTP interno entre `ApiMyAnimes`, `ApiIdentity` e `ApiMyAnimeList`.
- O fluxo de authorization code do OpenIddict permanece preservado; o Client Credentials de servico e tratado em endpoint separado.
- Nenhum segredo, chave privada, token, thumbprint real ou certificado de ambiente foi registrado no repositorio.
- Homologation e Production continuam diferidos. A ativacao nesses ambientes exige certificados provisionados, contas de servico e uma janela operacional aprovada.

## Fluxo implementado

1. `ApiMyAnimes` usa o client ID exclusivo `api-my-animes`.
2. O cliente envia `grant_type=client_credentials`, `client_id`, `scope=service.mal.read` e `resource=urn:dtudo:api-my-animelist` para `ApiIdentity` por HTTPS.
3. O certificado de cliente e carregado do Store `My` e apresentado no canal TLS. Nenhuma chave compartilhada ou client secret e aceito.
4. `ApiIdentity` valida certificado, client ID, EKU, validade, thumbprint ativo/anterior, escopo e audience antes de emitir um JWT curto.
5. `ApiMyAnimes` envia o JWT como Bearer para `ApiMyAnimeList` e apresenta o mesmo certificado no canal HTTPS interno.
6. `ApiMyAnimeList` valida issuer, audience, lifetime, assinatura, escopo e permissao do token; depois valida novamente a ligacao entre `client_id` e certificado.

O audience e absoluto e exclusivo: `urn:dtudo:api-my-animelist`. O escopo aceito nesta fronteira e somente `service.mal.read`.

## Binding por servico

Cada ambiente declara um binding em `ServiceCertificates` na baseline. O primeiro thumbprint e o certificado ativo; o segundo, quando presente, e o certificado anterior aceito somente ate `PreviousCertificateAcceptedUntilUtc`.

| Ambiente | Store | Principal da chave privada | Client ID | Escopo | Audience |
| --- | --- | --- | --- | --- | --- |
| Development | `CurrentUser\My` | `CURRENT_USER` | `api-my-animes` | `service.mal.read` | `urn:dtudo:api-my-animelist` |
| Homologation | `LocalMachine\My` | `.\DtudoHomAnimes` | `api-my-animes` | `service.mal.read` | `urn:dtudo:api-my-animelist` |
| Production | `LocalMachine\My` | `.\DtudoProdAnimes` | `api-my-animes` | `service.mal.read` | `urn:dtudo:api-my-animelist` |

Os principals acima sao nomes Windows literais. A baseline nao cria contas nem senhas; o provisionamento da conta do processo ocorre fora do repositorio.

## Certificate requirements

- Store name: `My`.
- Store location: `CurrentUser` for Development and `LocalMachine` for server environments.
- Private key present and readable by the process identity only through the explicit least-privilege ACL.
- Client Authentication EKU: `1.3.6.1.5.5.7.3.2`.
- Certificate currently valid when used.
- One active thumbprint and, only during rotation, one previous thumbprint with an explicit UTC expiration.
- Active and previous thumbprints must be different and belong to the same service binding.

The application validates certificate identity independently of the subject name. The client ID and registered thumbprint are the binding; a valid certificate from another service is rejected.

## Private-key ACL operation

The runner accepts a thumbprint, Store location and Windows principal. It resolves the private-key file, snapshots the previous DACL without storing certificate or key material, grants an explicit `Read` rule (Windows may materialize it as `Read, Synchronize`), and behaves idempotently on a second Apply.

Example with a placeholder only:

```powershell
$thumbprint = '<THUMBPRINT_HEX_40>'

& .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Validate -Environment Development -ConfigureCertificateAcl `
  -CertificateThumbprint $thumbprint -CertificatePrincipal CURRENT_USER -Json

& .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Apply -Environment Development -ConfigureCertificateAcl `
  -CertificateThumbprint $thumbprint -CertificatePrincipal CURRENT_USER -Confirm
```

For Homologation/Production, run the same operation in an elevated PowerShell session with the corresponding `LocalMachine` Store and service principal. The runner refuses server Apply/Rollback without elevation. The state file must remain in the protected state root and contains only operational rollback data.

## Overlapping rotation

1. Issue the new client certificate with the Client Authentication EKU and install it with its private key in the target `My` Store.
2. Grant the service principal explicit read access to the new private-key file and validate the Store/ACL before changing application configuration.
3. Configure the new thumbprint first and the old thumbprint second in `CertificateThumbprints`.
4. Set `PreviousCertificateAcceptedUntilUtc` to a short, explicit UTC deadline that covers deployment and connection draining.
5. Deploy the same overlap binding to `ApiIdentity`, `ApiMyAnimeList` and the `ApiMyAnimes` client configuration. Validate that the new certificate succeeds and the old certificate succeeds only before the deadline.
6. After the deadline, remove the old thumbprint from all bindings, remove its private-key ACL for the service principal, and remove the old certificate from the Store according to the approved retirement window.
7. Re-run validation and retain the operational record without recording the certificate value or token.

There is no automatic indefinite fallback: after the overlap expires, the previous certificate is rejected as `client-certificate-not-registered`.

## Negative and rollback evidence

The Etapa 15 tests cover:

- wrong absolute audience;
- scope outside `service.mal.read`;
- unknown client ID;
- client ID and certificate mismatch;
- wrong registered certificate;
- missing certificate;
- certificate without Client Authentication EKU;
- shared secret, client assertion and Basic authentication attempts;
- previous certificate during the overlap;
- previous certificate after the overlap;
- active certificate loading from `CurrentUser\My` with a private key.

The local infrastructure exercise used a temporary certificate only: Apply granted the private-key read ACL, Validate returned `19 Passed`, a second Apply reported that no ACL change was needed, Rollback restored the prior DACL, and the final inspection found zero temporary explicit read rules. The temporary certificate, key state and state directory were removed after the exercise.

Rollback restores only the saved DACL through the native Windows DACL API, so it does not require `SeSecurityPrivilege` to rewrite an audit SACL. Existing data and certificate material are never deleted automatically by the runner.

## Residual risks and manual actions

- The repository does not contain real environment thumbprints or certificates. Operators must provision them outside Git and inject only the non-secret configuration required by each environment.
- Development configuration keeps service authentication disabled until a local certificate and issuer configuration are deliberately supplied.
- A live process-to-process mTLS exercise with real service accounts was not run on this workstation; the endpoint and validator tests use synthetic certificates and the ACL exercise uses a temporary Store certificate.
- Before promotion, confirm Store placement, private-key ACL, Kestrel certificate negotiation, issuer discovery/JWKS, service account access, overlap deadline and old-certificate retirement on the target host.

## Rollback

Use the same environment and state root used by Apply:

```powershell
& .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Rollback -Environment Development -Confirm
```

For a server environment, use an elevated session and the protected `-StateRoot` chosen for that environment. If a rotation must be aborted before the deadline, keep the previous binding and certificate until the new path has been restored and validated; do not remove both certificates in one operation.

## Proxima etapa

A Etapa 16 e a proxima etapa permitida. Ela nao foi iniciada neste chat.
