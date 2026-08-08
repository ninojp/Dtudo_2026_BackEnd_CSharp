# Etapa 29 - MSIX, runner e implantacao

## Estado

- Estado: Bloqueada.
- A preparacao local foi implementada e testada com fixtures sem assinatura.
- A Etapa 27 continua bloqueada; nenhum host de homologacao, runner protegido, certificado interno ou environment do GitHub foi alterado.
- A Etapa 30 nao foi iniciada.

## Implementacao

- `WinAppDtudo/Package.appxmanifest.template` define uma identidade MSIX estavel (`Dtudo.WinAppDtudo`), `Publisher` parametrizado, quatro componentes de versao, `Windows.FullTrustApplication` e assets derivados do logo existente.
- `scripts/Invoke-DtudoMsixPackage.ps1` possui os modos `Prepare`, `Package` e `Validate`. Ele publica `WinAppDtudo` como `win-x64`, gera o manifest, cria payload, executa `makeappx`, assina somente pelo `Certificate Store` e confere hash, XML, identidade, versao, executavel, assets e assinatura `signtool` quando solicitada.
- A assinatura exige thumbprint explicito, chave privada acessivel ao processo e certificado cujo Subject coincide com o Publisher e possui EKU de Code Signing. Nenhum PFX, senha ou chave privada e lido do repositorio.
- `scripts/Invoke-DtudoMsixDeployment.ps1` mantem `deployment-state.json` fora do repositorio, com pacote atual/anterior, hash e versao. Update somente aceita versao maior; rollback somente aceita o pacote anterior mais antigo, valida novamente hash/assinatura e usa `ForceUpdateFromAnyVersion`. O estado e gravado por substituicao atomica.
- `scripts/Invoke-DtudoReleaseRunnerHardening.ps1` valida conta dedicada nao administrativa e ACLs por SID. `Apply` e `Rollback` exigem sessao elevada, nao criam conta nem recebem senha, e preservam o SDDL anterior para restauracao.
- `.github/workflows/msix-release.yml` aceita apenas tag `v*.*.*` ou despacho manual a partir de `main`. Nao possui gatilho de PR. A preparacao sem segredos usa runner hospedado; assinatura, instalacao e rollback usam runner `[self-hosted, windows, dtudo-release]` atras do environment `dtudo-msix-release`. Uploads usam artifacts v4 com nomes unicos e `overwrite: false`.

## Configuracao protegida posterior

No host dedicado, criar manualmente uma conta local exclusiva do runner, sem reutilizar conta administrativa. A senha deve ser inserida diretamente no host e nunca em commit, log ou chat. Depois, em PowerShell elevado:

```powershell
.\scripts\Invoke-DtudoReleaseRunnerHardening.ps1 -Mode Apply `
  -RunnerRoot C:\ProgramData\Dtudo2026\ReleaseRunner `
  -RunnerAccount .\DtudoReleaseRunner `
  -StatePath C:\ProgramData\Dtudo2026\Etapa29\runner-state.json `
  -Confirm
```

Validar antes de registrar o runner:

```powershell
.\scripts\Invoke-DtudoReleaseRunnerHardening.ps1 -Mode Validate `
  -RunnerRoot C:\ProgramData\Dtudo2026\ReleaseRunner `
  -RunnerAccount .\DtudoReleaseRunner `
  -FailOnBlocked
```

No GitHub, configurar manualmente:

1. Environment `dtudo-msix-release` com revisores obrigatorios, branch `main`/tags de release protegidos e nenhum segredo no escopo do CI.
2. Runner group exclusivo, label `dtudo-release`, conta de servico restrita, sem login interativo, sem privilegio de administrador e com workspace `_work` isolado.
3. Variables `DTUDO_MSIX_STATE_ROOT`, `DTUDO_RELEASE_RUNNER_ROOT`, `DTUDO_RELEASE_RUNNER_ACCOUNT` e `DTUDO_MSIX_PUBLISHER` apontando para caminhos/identidade do host.
4. Secret `DTUDO_MSIX_SIGNING_THUMBPRINT` contendo somente a referencia do certificado. A chave privada deve permanecer em `LocalMachine\My`, com ACL apenas para o processo do runner, backup protegido e rotacao sobreposta.
5. Certificado raiz/intermediario interno confiavel instalado somente nas maquinas autorizadas a receber o MSIX.

O workflow deve continuar sendo o unico caminho de deploy. Nao executar deploy por `pull_request`, `pull_request_target`, shell manual do runner ou workflow que aceite artefato substituivel.

## Validacoes executadas

- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-DtudoEtapa29.ps1`: `10 Passed`, `0 Failed`.
- `Prepare` + `Validate` com o `WinAppDtudo` publicado em `win-x64`, versao `1.0.0.0`, payload real e hash temporario: aprovado.
- A suite comprovou manifest/identidade/hash, adulteracao recusada, planos de update/rollback, downgrade recusado, raiz de estado com escrita ampla recusada, actions por SHA, ausencia de gatilho de PR, environment/runner dedicado e `overwrite: false`.
- Parsing Windows PowerShell dos quatro scripts: aprovado.
- `dotnet build .\WinAppDtudo\WinAppDtudo.csproj --configuration Release --no-restore --nologo`: aprovado; permaneceram apenas `NU1510` e conflito preexistente de `WindowsBase`.
- Verificacao nativa de workflows: 25 referencias `uses:` fixadas em SHA completo; `rg` nao esta instalado no workstation, entao a mesma regra foi executada com `Select-String`.
- Validacao negativa do runner com `-FailOnBlocked`: aprovada.

## Validacoes bloqueadas

`makeappx.exe` e `signtool.exe` nao estao instalados neste workstation. Por isso, o teste local usa somente um ZIP sintetico com extensao `.msix`, hash e manifest para validar rejeicao de adulteracao e planos de estado. Nao foi afirmada assinatura, instalacao, atualizacao ou rollback reais.

Antes de marcar a etapa como concluida, o host protegido deve comprovar:

- pacote real criado por `makeappx` e aceito por `signtool verify`;
- assinatura com certificado interno confiavel e chave privada fora do repositorio;
- instalacao de uma versao, update monotonicamente maior, adulteracao recusada e rollback real para o pacote anterior;
- ACL da conta do runner, environment com aprovacao, runner group exclusivo e nenhum caminho de PR nao confiavel;
- artefato assinado baixado com hash esperado e sem possibilidade de overwrite.

## Rollback

- Codigo/configuracao: restaurar os arquivos da Etapa 29 e a linha da Etapa 29 no status, sem remover `bin`, `obj`, bancos, certificados ou chaves automaticamente.
- Runner: executar `Invoke-DtudoReleaseRunnerHardening.ps1 -Mode Rollback` com o `StatePath` protegido; dados e instalacao do runner permanecem no host.
- Aplicacao: usar o workflow manual com `operation=rollback`, depois confirmar o `deployment-state.json` e o hash do pacote anterior. Nao apagar o estado antes da verificacao.

## Proxima etapa

Desbloquear e concluir a Etapa 27; depois repetir as validacoes protegidas da Etapa 29. Nao iniciar a Etapa 30.
