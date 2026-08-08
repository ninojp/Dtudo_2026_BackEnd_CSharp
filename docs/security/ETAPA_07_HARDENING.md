# Etapa 07 - Ambientes, contas e hardening

## Estado desta execucao

- Estado: `Concluida no escopo Development; Homologation/Production diferidos ate decisao de promocao`.
- O workstation de desenvolvimento nao exige elevacao, contas de servico, IIS, BitLocker ou firewall dedicado para esta etapa. O banco local usa `(localdb)\MSSQLLocalDB` com Windows Authentication implicita.
- Foram criadas as cinco raizes do Development, aplicadas ACLs minimas e salvo o estado de rollback em `%LOCALAPPDATA%\Dtudo2026\Etapa07\state.json`.
- Nenhum servico foi publicado ou iniciado. Nenhuma regra de firewall, registro Schannel, IIS, SQL Express ou BitLocker foi alterada.
- Homologation e Production continuam fora do escopo por decisao: a solucao permanece em Development e nao sera promovida agora.

## Artefatos

- `scripts/DtudoInfrastructureBaseline.psd1`: fonte declarativa sem segredos.
- `scripts/Invoke-DtudoInfrastructureHardening.ps1`: `Validate`, `Apply` e `Rollback` idempotentes.
- `scripts/Configure-DtudoSqlWindowsAuthentication.sql`: SQLCMD script sem senha, executado pelo runner somente com `-E`.

O runner falha fechado para `Apply` e `Rollback` de ambientes `Server` sem administrador. No perfil `Workstation`, ele opera somente nas raizes do usuario atual e nao cria sites IIS, bindings, certificados, bancos ou contas com senha implicita. A criacao de contas locais deve ser feita por procedimento administrativo seguro, ou substituida por gMSA/conta virtual no servidor alvo antes da aplicacao da baseline.

## Separacao de ambientes

| Ambiente | Raiz de aplicacao | Banco MyAnimes | Banco Identity declarado | SQL esperado | Exposicao |
| --- | --- | --- | --- | --- | --- |
| Development | `%LOCALAPPDATA%\Programs\Dtudo2026\Development` | `Dtudo2026Db` | `Dtudo2026IdentityDb_Development` | LocalDB; banco validado: `Dtudo2026Db` | loopback |
| Homologation | `C:\Program Files\Dtudo2026\Homologation` | `Dtudo2026Db_Homologation` | `Dtudo2026IdentityDb_Homologation` | SQL Server Express | somente gateway IIS em `16443` |
| Production | `C:\Program Files\Dtudo2026\Production` | `Dtudo2026Db_Production` | `Dtudo2026IdentityDb_Production` | SQL Server Express | somente gateway IIS |

Cada ambiente tambem possui `Data`, `Secrets` e `Backup` proprios. As raizes de aplicacao, dados e backup de ambientes diferentes nao podem ser iguais nem estar contidas umas nas outras. Os bancos possuem nomes distintos mesmo quando usam a mesma instancia local do host.

Development preserva as portas atuais das APIs (`63980`, `7146`) e Seq (`5341`) como portas internas. Homologation usa `16080`, `16081` e `15341`; Production usa `17080`, `17081` e `25341`. As portas internas nao sao publicas e o gateway de Production e a unica porta publica (`443`).

## Contas Windows e SQL

Development usa somente o principal `CURRENT_USER`, resolvido para a identidade Windows interativa atual. Homologation e Production declaram nomes locais distintos: `DtudoHom*` e `DtudoProd*`, para `ApiMyAnimes`, `ApiMyAnimeList`, `Gateway`, `FileStorage` e `Backup`. O valor `.` nos principals SQL e resolvido pelo runner para o nome real do computador; nao ha dominio, senha ou token armazenado na baseline.

No servidor alvo:

1. Preferir gMSA ou contas virtuais para os processos de servico. Se contas locais forem inevitaveis, cria-las manualmente com entrada segura de senha e politica de rotacao fora do repositorio.
2. Conceder a cada processo somente a raiz e banco de que precisa. O gateway nao recebe acesso direto ao SQL.
3. Usar SQL Server Express em homologacao e producao. SQL Server Developer e permitido somente em desenvolvimento/homologacao conforme a decisao do plano.
4. Instalar/configurar as instancias de servidor com Windows Authentication only e sem habilitar TCP publico. O runner usa `sqlcmd -E`; nao aceita usuario ou senha SQL. No Development, o LocalDB existente `Dtudo2026Db` e apenas consultado.
5. O script SQL cria somente logins Windows ausentes, usuarios nos dois bancos esperados e os papeis `db_datareader`, `db_datawriter` e `db_backupoperator` declarados. Ele nao cria bancos.

## ACLs

Todas as ACLs aplicadas pelo runner desabilitam heranca e preservam `SYSTEM` e `BUILTIN\Administrators` com `FullControl`.

No Development workstation, as cinco raizes ficam em caminhos do usuario e recebem tambem `CURRENT_USER` com `Modify`. `SYSTEM` e `Administrators` sao resolvidos por SID bem conhecido, portanto a regra nao depende do idioma do Windows.

| Raiz | Acesso adicional |
| --- | --- |
| `ApplicationRoot` | contas de API, gateway e FileStorage com `ReadAndExecute` |
| `DataRoot` | ApiMyAnimes, FileStorage e Backup com `Modify` |
| `SecretsRoot` | processos, exceto Backup, com `Read` |
| `BackupRoot` | somente Backup com `Modify` |

Antes de alterar uma raiz existente, o runner guarda o SDDL no estado operacional. Diretorios criados pelo runner somente sao removidos no rollback quando continuam vazios; dados existentes nunca sao apagados.

## Firewall e portas

As regras nomeadas com o prefixo `Dtudo2026-Etapa07` sao idempotentes e sao removidas no rollback somente quando foram criadas pelo runner. No Development, `ConfigureFirewall=$false`: o runner apenas verifica listeners e nao altera regras do host.

- Bloqueio de entrada TCP `1433` e UDP `1434` para impedir SQL/SQL Browser publico.
- Portas internas de APIs e Seq bloqueadas para enderecos externos e permitidas somente para loopback, com `OverrideBlockRules`.
- Gateway de Development limitado a loopback; Homologation publica somente o gateway IIS em `16443`.
- Production permite somente TCP `443` para o gateway IIS; SQL, Seq e APIs continuam internos.

O runner tambem verifica listeners existentes e falha o check se uma porta interna estiver ligada a `0.0.0.0` ou `::`.

## BitLocker

BitLocker deve ser habilitado administrativamente nos volumes que carregam aplicacoes, dados, segredos e backups de Homologation/Production. No Development workstation ele e `NotChecked` por nao ser requisito deste perfil. A chave de recuperacao deve ser escorada fora do host e nunca entrar no repositorio, log, manifesto ou status. O runner somente verifica `FullyEncrypted` e `ProtectionStatus=On`; ele nao habilita BitLocker automaticamente e nao manipula chaves.

## IIS e TLS

Para Homologation/Production, o baseline exige:

- IIS existente, com site e certificado ja provisionados no `LocalMachine\My`.
- TLS 1.0 e TLS 1.1 desabilitados no Schannel; TLS 1.2 habilitado.
- HSTS somente em sites HTTPS de homologacao/producao.
- Headers `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` e `Referrer-Policy: no-referrer`.
- Limite de requisicao de 50 MiB e remocao de `X-Powered-By`.
- Backup nativo do IIS via `appcmd` antes da alteracao; `Rollback` restaura esse backup.

O runner nao cria site, binding, certificado, regra de publicacao ou processo. A alteracao Schannel e global ao host e exige janela aprovada, backup do estado e reinicio conforme a politica do servidor.

No Development, IIS, HSTS e Schannel sao `NotChecked`; as APIs permanecem em Kestrel/loopback e nenhum servico e iniciado pelo runner.

## Operacao segura

Validacao sem alteracoes:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Validate -Environment Development -Json
```

No workstation de desenvolvimento, a aplicacao limitada as raizes e ACLs locais deve ser chamada diretamente no PowerShell para preservar o tipo booleano de `-Confirm`:

```powershell
& .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Apply -Environment Development -Confirm:$false
```

Aplicacao em um servidor preparado, em PowerShell elevado, deve ser feita para um ambiente por vez. O `-Confirm` torna a acao administrativa explicita:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Apply -Environment Production -Confirm
```

Opcoes que alteram subsistemas de servidor devem ser solicitadas conscientemente na mesma janela:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Apply -Environment Production -EnableTlsRegistryChanges -ConfigureIis -ConfigureSql -Confirm
```

O comando acima pressupoe contas, bancos, IIS, certificado e SQL Express ja preparados. Ele nao deve ser executado neste workstation de desenvolvimento. Para o estado operacional do servidor, informe `-StateRoot C:\ProgramData\Dtudo2026\Etapa07`.

Rollback usando o mesmo conjunto de ambientes da aplicacao:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Rollback -Environment Production -Confirm
```

Logins SQL criados pela etapa permanecem por padrao para evitar remocao acidental. Depois de parar os servicos e confirmar a dependencia, o administrador pode solicitar a remocao rastreada:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-DtudoInfrastructureHardening.ps1 `
  -Mode Rollback -Environment Production -RollbackSql -Confirm
```

O arquivo de estado contem apenas caminhos, SDDL, nomes de regras, nomes de logins criados e valores de registro necessarios ao rollback. Ele nao contem senha, token, cookie, connection string ou chave.

## Verificacoes negativas e evidencia local

Comandos executados em 2026-08-06:

```text
Invoke-DtudoInfrastructureHardening.ps1 -Mode Validate -Environment Development -Json
Invoke-DtudoInfrastructureHardening.ps1 -Mode Apply -Environment Development -Confirm:$false -Json
Invoke-DtudoInfrastructureHardening.ps1 -Mode Apply -Confirm:$false -Json
```

Resultado resumido:

| Verificacao | Resultado |
| --- | --- |
| Baseline estrutural | Passed |
| Traversal entre ambientes | Passed: recusado |
| Banco duplicado | Passed: recusado |
| Colisao de porta | Passed: recusada |
| Porta interna publicada | Passed: recusada |
| Credencial na baseline | Passed: recusada |
| Bind externo de APIs/Seq | Passed: nenhum detectado |
| Listener SQL TCP bloqueado | Passed: nenhum detectado |
| Checks de validacao | 18 Passed, 4 NotChecked, 0 Blocked, 0 Failed |
| Apply idempotente | 2 Passed, 4 NotChecked, 0 Blocked, 0 Failed |
| Segredos registrados | `SecretsLogged=False` |

Os itens `NotChecked` sao deliberadamente fora do perfil local: elevacao, perfis de firewall, BitLocker e IIS. Development conectou ao LocalDB por Windows Authentication implicita, confirmou `Dtudo2026Db` online e nao apresentou listener interno publico. Isso nao qualifica o host para homologacao ou producao.

## Registro de acoes administrativas

- Acao executada: leitura de baseline, parse PowerShell, validacao estrutural, cinco testes negativos, validacao SQL LocalDB, leitura de listeners, `Apply` do Development e reaplicacao idempotente.
- Acao executada no Development: criacao das cinco raizes, ACLs com `SYSTEM`, `Administrators` e `CURRENT_USER`, e persistencia do estado em `%LOCALAPPDATA%\Dtudo2026\Etapa07\state.json`.
- Acao nao executada: `Rollback`, instalacao de IIS/SQL Express, criacao de contas de servico, regra de firewall, BitLocker, Schannel, binding ou publicacao.
- Segredo solicitado: nenhum.
- Proxima acao manual: quando houver decisao de promover, preparar servidor elevado, provisionar contas/SQL Express/IIS/certificados/BitLocker, informar `-StateRoot` de servidor, repetir `Validate`, revisar a janela de mudanca e somente entao executar `Apply`.

## Rollback

O rollback do runner restaura SDDL e valores Schannel salvos, remove somente regras de firewall criadas pela etapa, restaura backup IIS do `appcmd`, remove diretorios criados que estejam vazios e, somente com `-RollbackSql`, remove usuarios/logins registrados como criados nesta execucao. No Development, use o mesmo estado em `%LOCALAPPDATA%\Dtudo2026\Etapa07`; em servidor, informe o mesmo `-StateRoot` usado no Apply. Nenhum banco, arquivo de dados ou segredo e apagado automaticamente.

## Proxima etapa

A Etapa 07 esta concluida no escopo de desenvolvimento local definido pelo plano. A Etapa 08 e o proximo gate e deve ser executada em chat independente; Homologation/Production so serao aplicados quando houver decisao de promocao para o servidor Windows proprio.
