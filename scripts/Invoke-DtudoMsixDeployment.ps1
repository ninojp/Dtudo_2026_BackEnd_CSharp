[CmdletBinding()]
param(
    [ValidateSet('Validate', 'Update', 'Rollback')]
    [string]$Mode = 'Validate',
    [string]$PackagePath,
    [string]$Version,
    [string]$ExpectedSha256,
    [string]$StateRoot,
    [string]$PackageIdentityName = 'Dtudo.WinAppDtudo',
    [string]$Publisher = 'CN=Dtudo Internal',
    [string]$SignToolPath,
    [ValidateSet('true', 'false', '1', '0')]
    [string]$RequireSignature = 'true',
    [switch]$PlanOnly,
    [switch]$Json
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
}

function Resolve-StateRoot {
    if ([string]::IsNullOrWhiteSpace($StateRoot)) {
        if (-not [string]::IsNullOrWhiteSpace($env:DTUDO_MSIX_STATE_ROOT)) {
            return Resolve-FullPath -Path $env:DTUDO_MSIX_STATE_ROOT
        }
        return Resolve-FullPath -Path (Join-Path $env:LOCALAPPDATA 'Dtudo2026\Msix')
    }
    return Resolve-FullPath -Path $StateRoot
}

function Assert-ExternalStateRoot {
    param([Parameter(Mandatory = $true)][string]$Root)

    $repositoryRoot = Resolve-FullPath -Path (Join-Path $PSScriptRoot '..')
    $normalizedRoot = $Root.TrimEnd('\', '/')
    if ($normalizedRoot.Equals($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $normalizedRoot.StartsWith($repositoryRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'O estado de implantacao deve ficar fora do repositorio.'
    }
}

function Test-BroadPrincipal {
    param([Parameter(Mandatory = $true)][System.Security.Principal.IdentityReference]$IdentityReference)

    try {
        $sid = $IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Value
    } catch {
        $sid = $IdentityReference.Value
    }
    return $sid -in @('S-1-1-0', 'S-1-5-32-545', 'S-1-5-11', 'S-1-5-4')
}

function Assert-RestrictedStateRoot {
    param([Parameter(Mandatory = $true)][string]$Root)

    if (-not (Test-Path -LiteralPath $Root)) {
        throw "A raiz de estado nao existe. Aplique primeiro a baseline protegida do runner: $Root"
    }

    $acl = Get-Acl -LiteralPath $Root
    $unsafe = @($acl.Access | Where-Object {
            $_.AccessControlType -eq 'Allow' -and
            (Test-BroadPrincipal -IdentityReference $_.IdentityReference) -and
            $_.FileSystemRights.ToString() -match '(?i)(Write|Modify|FullControl|Create|Delete)'
        })
    if ($unsafe.Count -gt 0) {
        throw 'A raiz de estado possui escrita para um principal amplo; recusando a operacao.'
    }
}

function Read-State {
    param([Parameter(Mandatory = $true)][string]$Root)

    $statePath = Join-Path $Root 'deployment-state.json'
    if (-not (Test-Path -LiteralPath $statePath)) {
        return [pscustomobject]@{
            SchemaVersion = 1
            PackageIdentityName = $PackageIdentityName
            Current = $null
            Previous = $null
        }
    }

    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    if ($state.SchemaVersion -ne 1 -or $state.PackageIdentityName -ne $PackageIdentityName) {
        throw 'Estado de implantacao ausente, incompatível ou de outro pacote.'
    }
    return $state
}

function Write-State {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$State
    )

    $statePath = Join-Path $Root 'deployment-state.json'
    $temporaryPath = "$statePath.$PID.tmp"
    $content = $State | ConvertTo-Json -Depth 6
    [IO.File]::WriteAllText($temporaryPath, $content, (New-Object Text.UTF8Encoding($false)))
    Move-Item -LiteralPath $temporaryPath -Destination $statePath -Force
}

function Get-VersionValue {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw 'A versao de implantacao deve conter quatro componentes numericos.'
    }
    return New-Object Version($Value)
}

function Get-Record {
    param(
        [Parameter(Mandatory = $true)][object]$Validation,
        [Parameter(Mandatory = $true)][string]$Path
    )

    return [pscustomobject]@{
        PackageIdentityName = [string]$Validation.Name
        Publisher = [string]$Validation.Publisher
        Version = [string]$Validation.Version
        PackagePath = Resolve-FullPath -Path $Path
        Sha256 = [string]$Validation.Sha256
        InstalledAtUtc = [DateTime]::UtcNow.ToString('o')
    }
}

function Invoke-PackageValidation {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$VersionValue,
        [string]$HashValue
    )

    $packageScript = Join-Path $PSScriptRoot 'Invoke-DtudoMsixPackage.ps1'
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $packageScript,
        '-Mode', 'Validate',
        '-PackagePath', $Path,
        '-Version', $VersionValue,
        '-Publisher', $Publisher,
        '-Json'
    )
    if ($requireSignatureEnabled) {
        $arguments += '-RequireSignature'
    }
    if (-not [string]::IsNullOrWhiteSpace($HashValue)) {
        $arguments += @('-ExpectedSha256', $HashValue)
    }
    if (-not [string]::IsNullOrWhiteSpace($SignToolPath)) {
        $arguments += @('-SignToolPath', $SignToolPath)
    }

    $powershell = (Get-Command powershell.exe -ErrorAction SilentlyContinue).Source
    if ([string]::IsNullOrWhiteSpace($powershell)) {
        $powershell = (Get-Command pwsh.exe -ErrorAction SilentlyContinue).Source
    }
    if ([string]::IsNullOrWhiteSpace($powershell)) {
        throw 'PowerShell nao esta disponivel para validar o pacote.'
    }
    $json = & $powershell @arguments
    if ($LASTEXITCODE -ne 0) {
        throw 'A validacao do pacote MSIX falhou.'
    }
    return ($json | ConvertFrom-Json)
}

function Invoke-AppxInstall {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][bool]$AllowDowngrade
    )

    if ($PlanOnly) {
        return
    }

    $command = Get-Command Add-AppxPackage -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw 'Add-AppxPackage nao esta disponivel neste host Windows.'
    }
    $arguments = @('-Path', $Path, '-ForceApplicationShutdown')
    if ($AllowDowngrade) {
        if (-not $command.Parameters.ContainsKey('ForceUpdateFromAnyVersion')) {
            throw 'O host nao suporta rollback MSIX com ForceUpdateFromAnyVersion.'
        }
        $arguments += '-ForceUpdateFromAnyVersion'
    }
    & $command.Name @arguments
}

function New-Result {
    param(
        [Parameter(Mandatory = $true)][string]$Operation,
        [Parameter(Mandatory = $true)][object]$State,
        [object]$Candidate
    )

    return [pscustomobject]@{
        Mode = $Mode
        Operation = $Operation
        PlanOnly = $PlanOnly.IsPresent
        StateRoot = (Resolve-StateRoot)
        CurrentVersion = if ($null -eq $State.Current) { $null } else { [string]$State.Current.Version }
        PreviousVersion = if ($null -eq $State.Previous) { $null } else { [string]$State.Previous.Version }
        CandidateVersion = if ($null -eq $Candidate) { $null } else { [string]$Candidate.Version }
    }
}

$resolvedStateRoot = Resolve-StateRoot
Assert-ExternalStateRoot -Root $resolvedStateRoot
Assert-RestrictedStateRoot -Root $resolvedStateRoot
$state = Read-State -Root $resolvedStateRoot
$requireSignatureEnabled = $RequireSignature -in @('true', '1')

if ($Mode -eq 'Validate') {
    if ([string]::IsNullOrWhiteSpace($PackagePath) -or [string]::IsNullOrWhiteSpace($Version)) {
        throw '-PackagePath e -Version sao obrigatorios no modo Validate.'
    }
    $validation = Invoke-PackageValidation -Path $PackagePath -VersionValue $Version -HashValue $ExpectedSha256
    if ($validation.Name -ne $PackageIdentityName -or $validation.Publisher -ne $Publisher) {
        throw 'Identity ou Publisher do pacote nao correspondem ao pacote esperado.'
    }
    $result = [pscustomobject]@{
        Mode = $Mode
        PackagePath = Resolve-FullPath -Path $PackagePath
        Version = [string]$validation.Version
        Sha256 = [string]$validation.Sha256
        SignatureChecked = [bool]$validation.SignatureChecked
        StateRoot = $resolvedStateRoot
    }
} elseif ($Mode -eq 'Update') {
    if ([string]::IsNullOrWhiteSpace($PackagePath) -or [string]::IsNullOrWhiteSpace($Version)) {
        throw '-PackagePath e -Version sao obrigatorios no modo Update.'
    }
    $candidateVersion = Get-VersionValue -Value $Version
    if ($null -ne $state.Current -and $candidateVersion -le (Get-VersionValue -Value ([string]$state.Current.Version))) {
        throw 'Atualizacao recusada: a versao candidata nao e maior que a versao atual.'
    }
    if ($null -eq $state.Current) {
        $installed = @(Get-AppxPackage -Name $PackageIdentityName -ErrorAction SilentlyContinue)
        if ($installed.Count -gt 0) {
            throw 'Existe uma instalacao sem estado confiavel de rollback; recusando sobrescrita.'
        }
    }
    $validation = Invoke-PackageValidation -Path $PackagePath -VersionValue $Version -HashValue $ExpectedSha256
    if ($validation.Name -ne $PackageIdentityName -or $validation.Publisher -ne $Publisher) {
        throw 'Identity ou Publisher do pacote nao correspondem ao pacote esperado.'
    }
    $candidate = Get-Record -Validation $validation -Path $PackagePath
    Invoke-AppxInstall -Path $PackagePath -AllowDowngrade $false
    if (-not $PlanOnly) {
        $state = [pscustomobject]@{
            SchemaVersion = 1
            PackageIdentityName = $PackageIdentityName
            Current = $candidate
            Previous = $state.Current
        }
        Write-State -Root $resolvedStateRoot -State $state
    }
    $result = New-Result -Operation 'Update' -State $state -Candidate $candidate
} else {
    if ($null -eq $state.Current -or $null -eq $state.Previous) {
        throw 'Rollback recusado: nao existe pacote anterior registrado.'
    }
    $currentVersion = Get-VersionValue -Value ([string]$state.Current.Version)
    $previousVersion = Get-VersionValue -Value ([string]$state.Previous.Version)
    if ($previousVersion -ge $currentVersion) {
        throw 'Rollback recusado: o pacote anterior nao e mais antigo que o atual.'
    }
    $previousValidation = Invoke-PackageValidation -Path ([string]$state.Previous.PackagePath) -VersionValue ([string]$state.Previous.Version) -HashValue ([string]$state.Previous.Sha256)
    if ($previousValidation.Name -ne $PackageIdentityName -or $previousValidation.Publisher -ne $Publisher) {
        throw 'Identity ou Publisher do pacote anterior nao correspondem ao pacote esperado.'
    }
    $candidate = Get-Record -Validation $previousValidation -Path ([string]$state.Previous.PackagePath)
    Invoke-AppxInstall -Path $candidate.PackagePath -AllowDowngrade $true
    if (-not $PlanOnly) {
        $state = [pscustomobject]@{
            SchemaVersion = 1
            PackageIdentityName = $PackageIdentityName
            Current = $candidate
            Previous = $state.Current
        }
        Write-State -Root $resolvedStateRoot -State $state
    }
    $result = New-Result -Operation 'Rollback' -State $state -Candidate $candidate
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6 -Compress
} else {
    $result
}
