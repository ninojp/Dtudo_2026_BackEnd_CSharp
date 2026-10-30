# Dtudo2026 — Operação da solução

scripts da pasta `scripts\`, CI/CD, backup e restauração, hardening de ambientes e contas, painel de saúde com alertas e o pipeline de release MSIX.

> **Estado atual:** projeto pessoal em desenvolvimento, sem versão de produção nem implantação. O escopo operacional validado é **somente Development local**; não há homologação (a borda IIS/TLS de homologação foi removida por não estar em uso).

## Ambiente PowerShell

| Item | Situação |
|---|---|
| PowerShell Core | 7.6.x instalado por ZIP, sem administrador, em `%LOCALAPPDATA%\Programs\PowerShell\<versão>\pwsh.exe`, no PATH do usuário |
| VS Code | Perfil padrão do terminal: PowerShell 7 |
| Windows PowerShell | 5.1 mantido; `powershell.exe` continua sendo o 5.1 |
| Atualização | Instalação por ZIP **não** atualiza sozinha: atualizar apenas com release estável oficial, verificando hash e assinatura Authenticode antes |

Executar scripts do repositório com `pwsh -NoLogo -NoProfile -File .\scripts\<script>.ps1`. Verificação rápida do ambiente:

```powershell
pwsh -NoLogo -NoProfile -File .\scripts\Test-DtudoPowerShell.ps1
pwsh -NoLogo -NoProfile -File .\scripts\Test-DtudoEtapa29.ps1
```

Regras: os scripts de backup e MSIX usam o executável do host atual; a validação de MSIX aceita separadores `/` e `\` no ZIP; ler JSON UTF-8 com codificação explícita no 5.1; nunca desabilitar verificações de hash. A validação dos scripts em 7.6 e 5.1 se limitou a sintaxe, testes sintéticos e registro da tarefa de backup com `-WhatIf`.

## Pasta `scripts\`

| Arquivo | Função |
|---|---|
| `Configure-DtudoSqlWindowsAuthentication.sql` | Script SQLCMD (sem senha) que cria logins Windows e permissões mínimas por banco; executado pelo runner de hardening com `-E` |
| `DtudoInfrastructureBaseline.psd1` | Linha de base declarativa por ambiente (diretórios, portas internas, contas, bancos, firewall, TLS) consumida pelo hardening |
| `Invoke-DtudoBackup.ps1` | Backup, verificação de restauração e poda de retenção (`Backup` / `RestoreVerify` / `Prune`) |
| `Invoke-DtudoInfrastructureHardening.ps1` | Hardening de infraestrutura (`Validate` / `Apply` / `Rollback`) com `-Environment Development\|Homologation\|Production` |
| `Invoke-DtudoMsixDeployment.ps1` | Instalação/atualização/rollback do MSIX (`Validate` padrão / `Update` / `Rollback`), com estado fora do repositório |
| `Invoke-DtudoMsixPackage.ps1` | Preparação, empacotamento/assinatura e validação do MSIX (`Prepare` / `Package` / `Validate`) com `makeappx`/`signtool` |
| `Invoke-DtudoReleaseRunnerHardening.ps1` | Valida/aplica/reverte o hardening do runner de release (`-FailOnBlocked`) |
| `Register-DtudoBackupTask.ps1` | Registra de forma idempotente a tarefa agendada diária de backup (02:00, `StartWhenAvailable`, limite 8 h, `IgnoreNew`) |
| `run-if-down.js` | Utilitário do `npm run serv`: só inicia um processo se a URL de health informada estiver indisponível |
| `Test-DtudoEtapa29.ps1` | Suíte sintética (10 testes) de empacotamento, hash, update e rollback do MSIX |
| `Test-DtudoPowerShell.ps1` | Verificação de sintaxe e sanidade dos scripts PowerShell |

### Invoke-DtudoInfrastructureHardening.ps1

Parâmetros relevantes: `-Environment`, `-EnableTlsRegistryChanges`, `-ConfigureIis`, `-ConfigureSql`, `-ConfigureCertificateAcl -CertificateThumbprint <THUMBPRINT_HEX_40> -CertificatePrincipal <CONTA>`, `-StateRoot <STATE_ROOT>`, `-RollbackSql`, `-Json`. Em Windows Server, **falha fechado** se não estiver elevado. O estado de rollback em Development fica em `%LOCALAPPDATA%\Dtudo2026\...\state.json`; em servidor use `-StateRoot`.

```powershell
pwsh -NoLogo -NoProfile -File .\scripts\Invoke-DtudoInfrastructureHardening.ps1 -Environment Development -Mode Validate -Json
```

## CI/CD (`.github\workflows`)

### `ci.yml`

Dispara em pull request e push para `main`. Todas as actions são fixadas por SHA de 40 caracteres; permissões `contents: read`; sem `pull_request_target`, sem runner self-hosted e sem segredo de produção.

| Job | O que faz |
|---|---|
| Build/test/lint (windows-2025) | `dotnet restore/build` Release com analisadores e `dotnet test` de `Dtudo2026.slnx`; `npm ci --ignore-scripts`, lint e build do DtudoSite |
| CodeQL | C# e JavaScript, consultas `security-extended`; envio dos resultados apenas em push |
| Auditoria de dependências | NuGet vulnerável + `npm audit --audit-level=high` |
| Dependency review | Somente em pull request |
| Secret scan | gitleaks sobre o histórico completo |
| Higiene de workflows | Exige `uses:` fixado em SHA de 40 caracteres |

### `msix-release.yml`

Ver [Pipeline de release MSIX](#pipeline-de-release-msix).

### Configurações manuais no GitHub (pendentes)

Ruleset da branch principal, secret scanning/push protection, environments (`dtudo-msix-release` com aprovação) e runner group restrito são configurações administrativas **diferidas**; o repositório é pessoal e somente o proprietário faz commits.

## Backup e restauração

`Invoke-DtudoBackup.ps1` monta uma pasta de staging e só publica o snapshot diário (UTC, `yyyyMMdd`) depois que todas as cópias e hashes terminam. O manifesto guarda somente metadados e SHA-256 (nenhum segredo).

| Aspecto | Política |
|---|---|
| Banco | `sqlcmd -E` (autenticação Windows), `BACKUP ... COPY_ONLY, CHECKSUM` e `RESTORE VERIFYONLY` |
| Escopo padrão | Banco `Dtudo2026Db`, `ApiMyAnimes/App_Data`, configurações/launchSettings, migrations, projetos e runner |
`-BackupRoot` (padrão: variável `DTUDO_BACKUP_ROOT`); raízes de mídia via `-FileSource`;
| RestoreVerify | Restaura em banco temporário com `MOVE`, executa `DBCC CHECKDB`; recusa restaurar sobre a origem ou dentro do backup |
| Retenção | 30 dias; só remove pastas que possuem manifesto |
| RPO / RTO | 24 h / meta de 8 h |
| Agendamento | `Register-DtudoBackupTask.ps1` (diário 02:00); tarefa herdada `Dtudo2026-Etapa06-Backup`, identidade interativa atual em Development |

```powershell
pwsh -NoLogo -NoProfile -File .\scripts\Invoke-DtudoBackup.ps1 -Mode Backup -BackupRoot <BACKUP_ROOT>
pwsh -NoLogo -NoProfile -File .\scripts\Invoke-DtudoBackup.ps1 -Mode RestoreVerify -BackupRoot <BACKUP_ROOT>
```

Último teste local: backup em ~1,4 s, restauração em ~3,5 s, `CHECKDB` OK; backup adulterado é rejeitado.

**Riscos residuais:** a raiz está no mesmo host (volume separado); não há cópia off-site/offline; teste semestral de restauração fora do host e conta dedicada/BitLocker estão pendentes para Homologação/Produção. O painel de saúde do WinApp valida backups via variável `DTUDO_BACKUP_ROOT` (definida fora do repositório).

## Hardening de ambientes e contas

| Item | Development | Homologation | Production |
|---|---|---|---|
| Raiz da aplicação | `%LOCALAPPDATA%\Programs\Dtudo2026\Development` | `%ProgramFiles%\Dtudo2026\Homologation` | `%ProgramFiles%\Dtudo2026\Production` |
| SQL | LocalDB | SQL Express | SQL Express |
| Bancos | `Dtudo2026Db` | `Dtudo2026Db_Homologation` | `Dtudo2026Db_Production` |
| Bancos de identidade | `Dtudo2026IdentityDb_<Env>` | idem | idem |
| Portas internas (ApiMyAnimes / ApiMyAnimeList / Seq) | 63980 / 7146 / 5341 | 16080 / 16081 / 15341 | 17080 / 17081 / 25341 |
| Exposição | loopback | somente gateway via IIS em 16443 | somente gateway em 443 |
| Contas de serviço | usuário atual | `DtudoHom*` (ApiMyAnimes, ApiMyAnimeList, Gateway, FileStorage, Backup) | `DtudoProd*` (idem) |
| Aplicado? | **Sim** | Não | Não |

Regras comuns:

- Preferir gMSA ou contas virtuais; **nenhuma senha** na linha de base.
- SQL somente com Autenticação Windows, sem TCP público.
- ACLs com herança desativada: SYSTEM e Administrators `FullControl`; raiz da aplicação `ReadAndExecute`; dados `Modify`; segredos `Read`; raiz de backup somente para a conta de Backup.
- Firewall (regras com prefixo `Dtudo2026-Etapa07`): bloqueio de 1433/1434, portas internas apenas em loopback, Produção expõe só 443.
- BitLocker é **apenas verificado**, nunca habilitado pelo runner.
- IIS/TLS: TLS 1.2+ (desativação de 1.0/1.1 via Schannel é opt-in), HSTS, cabeçalhos de segurança, limite de 50 MiB, remoção de `X-Powered-By`, backup de configuração IIS com `appcmd`.
- Tarefas que mudam o Schannel, IIS, SQL ou ACL de chave privada exigem elevação e confirmação explícita.

## Habilitar o service-to-service e o worker LGPD (ApiIdentity → ApiMyAnimes)

A exportação e a exclusão LGPD do progresso de episódios usam client credentials + mTLS entre a [ApiIdentity](21_ApiIdentity.md) e os endpoints internos da [ApiMyAnimes](22_ApiMyAnimes.md). **Tudo vem desabilitado**; nesse estado a exportação responde `503` e os pedidos de exclusão vencidos ficam `Pending` (comportamento seguro). Valores reais (thumbprints, client IDs, URLs) ficam só em User Secrets/variáveis de ambiente protegidas, nunca no repositório. Ordem:

1. **Migração da ApiIdentity** (gerada, não aplicada): `dotnet ef database update --project ApiIdentity\ApiIdentity.csproj --startup-project ApiIdentity\ApiIdentity.csproj`.
2. **Certificados:** um certificado de cliente (EKU de cliente, chave privada acessível à conta do processo; ver `Invoke-DtudoInfrastructureHardening.ps1 -ConfigureCertificateAcl`) para o client `api-identity`, no store configurado.
3. **ApiIdentity — emissor:** `ServiceAuthentication:Enabled=true` e, em `ServiceAuthentication:Clients`, o client `api-identity` com `CertificateThumbprints` e `AllowedGrants` = `{ Audience: urn:dtudo:api-my-animes, Scopes: [service.privacy.myanimes] }`. A ApiIdentity não inicia se esse escopo for concedido a outra audiência.
4. **ApiIdentity — credenciais de saída:** `ServiceAuthentication:ApiMyAnimes` com `Enabled=true`, `TokenEndpoint` (o próprio `/connect/token` da ApiIdentity, HTTPS), `ClientId` `api-identity`, `CertificateThumbprints` e store; `Audience` e `Scopes` já vêm fixos.
5. **ApiMyAnimes:** `InternalServiceAuthentication:Enabled=true` e `Clients` com `ClientId` `api-identity` e os mesmos thumbprints (rotação: novo thumbprint primeiro e `PreviousCertificateAcceptedUntilUtc` para o antigo). O Kestrel passa a pedir certificado de cliente opcional nessa API.
6. **ApiIdentity — destino:** `PrivacyIntegration:MyAnimes:BaseUrl` (HTTPS da ApiMyAnimes; em Development `https://localhost:63980/`). Reiniciar as duas APIs.
7. **Teste manual da exportação** (`POST /identity/me/data-export` com um usuário): deve trazer `exportVersion` `"2"` e a seção `myAnimes`.
8. **Worker:** `PrivacyDeletionWorker:Enabled=true` (ajustes: `PollIntervalSeconds`, `InitialDelaySeconds`, `BatchSize`, `RetryDelayMinutes`). O log (Seq) registra a configuração ao iniciar; com `Enabled=false` registra "Worker de exclusao LGPD desabilitado por configuracao".

**Acompanhamento:** pedidos pendentes e falhas ficam em `IdentityPersonalDataDeletionRequests` (`Status`, `ScheduledForUtc`, `AttemptCount`, `LastAttemptAtUtc`, `LastError` com código curto: `not-configured`, `http-503`, `timeout`, `network`, `service-auth-unavailable`, `token-http-NNN`, `invalid-response`). A auditoria fica em `IdentityProvisioningAuditEvents` (`privacy.deletion-failed`/`privacy.deletion-completed`) e, na ApiMyAnimes, em `SecurityAuditEvents` (apenas hash curto da conta). Falhas são repetidas após `RetryDelayMinutes`; nunca se marca `Completed` com dado remanescente. Se `AttemptCount` crescer sem parar, verificar certificados/thumbprints, a allowlist da ApiMyAnimes, `BaseUrl` e a saúde das duas APIs. Para pausar, `PrivacyDeletionWorker:Enabled=false` e reiniciar. A conta bootstrap é sempre ignorada.

**Status:** implementado e testado com substitutos; **não validado ao vivo** até os itens 2–8 estarem aplicados no ambiente.

## Painel de saúde e alertas

O WinApp possui a aba **Painel de Saúde** (`FUC_PainelSaude`), alimentada por `WinAppHealthMonitoringService`, com timer de 60 s:

| Fonte | Verificação |
|---|---|
| ApiIdentity | `/health/ready` |
| ApiMyAnimes, ApiMyAnimeList, ApiFileStorage | endpoints de health autenticados (`health.read`) |
| Backup | pasta definida em `DTUDO_BACKUP_ROOT` (idade e manifesto) |

Alertas visuais (NotifyIcon) são emitidos apenas para estados críticos. O alerta local não existe se o WinApp ou o host estiver indisponível. Em Development (compilação `DEBUG`), o WinApp pode iniciar a ApiFileStorage automaticamente; se outro processo ocupar a porta 7244, o health é a verificação definitiva e nenhuma instância conflitante é iniciada. Detalhes em [11_WinAppDtudo.md](11_WinAppDtudo.md). Logs estruturados vão ao Seq (5341 em Development); dados sensíveis são redigidos por `SensitiveDataRedactionEnricher` ([30_LibDtudo.Shared.md](30_LibDtudo.Shared.md)).

## Pipeline de release MSIX

`msix-release.yml` dispara em tag `v*.*.*` ou `workflow_dispatch` (somente a partir da `main`; operação `update` ou `rollback`).

| Etapa | Onde roda | O que faz |
|---|---|---|
| guard | GitHub-hosted | Valida origem (tag/main) e operação |
| prepare | windows-2025 | `Invoke-DtudoMsixPackage.ps1 -Mode Prepare`; artefato imutável (`overwrite: false`) com hash SHA-256 |
| deploy-update / rollback | runner `[self-hosted, windows, dtudo-release]`, environment `dtudo-msix-release` (com aprovação) | Valida o hardening do runner e o SHA-256, empacota e **assina a partir do Certificate Store** (`LocalMachine\My`), valida, executa `Invoke-DtudoMsixDeployment.ps1 -Mode Update/Rollback` e publica o artefato assinado |

Variáveis do repositório: `DTUDO_MSIX_STATE_ROOT`, `DTUDO_RELEASE_RUNNER_ROOT`, `DTUDO_RELEASE_RUNNER_ACCOUNT`, `DTUDO_MSIX_PUBLISHER`. Segredo (apenas referência): `DTUDO_MSIX_SIGNING_THUMBPRINT`. A chave de assinatura nunca sai do store e o estado de update/rollback fica fora do repositório.

**Bloqueios:** `makeappx`/`signtool` (Windows SDK) ausentes neste host, sem assinatura real, sem runner dedicado nem teste real de instalação/atualização/rollback.

## Pendências/Divergências

- O gateway
- Nomes herdados
- A documentação antiga de `Test-DtudoPowerShell` falava em "10 scripts"; hoje são 8 `.ps1` + 1 `.sql` + 1 `.psd1` + 1 `.js`.
- O `ci.yml` testa `Dtudo2026.slnx`, mas `tests\` é ignorado pelo Git (ver [00_Solucao.md](00_Solucao.md)).
- Auditoria `npm audit`
