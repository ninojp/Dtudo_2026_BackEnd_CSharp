[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$script:Passed = 0
$script:Failed = 0
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('Dtudo2026-Etapa29-' + [Guid]::NewGuid().ToString('N'))

function Assert-Test {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][bool]$Condition,
        [string]$Detail
    )

    if ($Condition) {
        $script:Passed++
        Write-Output "PASS: $Name"
    } else {
        $script:Failed++
        Write-Output "FAIL: $Name"
        if (-not [string]::IsNullOrWhiteSpace($Detail)) {
            Write-Output $Detail
        }
    }
}

function Invoke-ScriptCapture {
    param(
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $output = ''
    $captureId = [guid]::NewGuid().ToString('N')
    $stdoutPath = Join-Path $testRoot "$captureId.out"
    $stderrPath = Join-Path $testRoot "$captureId.err"
    try {
        $process = Start-Process -FilePath 'powershell.exe' -ArgumentList (@('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $ScriptPath) + $Arguments) -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -Wait -PassThru
        $stdout = if (Test-Path -LiteralPath $stdoutPath) { Get-Content -LiteralPath $stdoutPath -Raw } else { '' }
        $stderr = if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Raw } else { '' }
        return [pscustomobject]@{ ExitCode = [int]$process.ExitCode; Output = ($stdout + $stderr) }
    } catch {
        return [pscustomobject]@{ ExitCode = 1; Output = (($_ | Out-String) + $output) }
    }
}

function New-FixturePackage {
    param(
        [Parameter(Mandatory = $true)][string]$VersionValue,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    $fixtureRoot = Join-Path $testRoot ('fixture-' + $VersionValue)
    New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'Assets') -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'WinAppDtudo.exe'), 'fixture executable')
    foreach ($asset in @('StoreLogo.png', 'Square150x150Logo.png', 'Square44x44Logo.png')) {
        [IO.File]::WriteAllText((Join-Path $fixtureRoot ('Assets\' + $asset)), 'fixture asset')
    }
    $manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
  <Identity Name="Dtudo.WinAppDtudo" Publisher="CN=Dtudo Internal" Version="$VersionValue" />
</Package>
"@
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'AppxManifest.xml'), $manifest)
    $archivePath = "$Destination.zip"
    Compress-Archive -Path (Join-Path $fixtureRoot '*') -DestinationPath $archivePath -Force
    Move-Item -LiteralPath $archivePath -Destination $Destination -Force
    $hash = (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash
    [IO.File]::WriteAllText("$Destination.sha256", "$hash  $(Split-Path -Leaf $Destination)")
    return [pscustomobject]@{ Path = $Destination; Hash = $hash; Version = $VersionValue }
}

function Set-SecureTestAcl {
    param([Parameter(Mandatory = $true)][string]$Path)

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
    $acl = Get-Acl -LiteralPath $Path
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($rule in @($acl.Access)) {
        $acl.RemoveAccessRule($rule) | Out-Null
    }
    $inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
    $systemSid = New-Object Security.Principal.SecurityIdentifier('S-1-5-18')
    $administratorsSid = New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')
    $currentAccount = New-Object Security.Principal.NTAccount([Security.Principal.WindowsIdentity]::GetCurrent().Name)
    $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule -ArgumentList @($systemSid, 'FullControl', $inheritance, 'None', 'Allow')))
    $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule -ArgumentList @($administratorsSid, 'FullControl', $inheritance, 'None', 'Allow')))
    $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule -ArgumentList @($currentAccount, 'Modify', $inheritance, 'None', 'Allow')))
    Set-Acl -LiteralPath $Path -AclObject $acl
}

try {
    New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
    $packageScript = Join-Path $PSScriptRoot 'Invoke-DtudoMsixPackage.ps1'
    $deploymentScript = Join-Path $PSScriptRoot 'Invoke-DtudoMsixDeployment.ps1'
    $workflowPath = Join-Path $PSScriptRoot '..\.github\workflows\msix-release.yml'
    $packageV1 = New-FixturePackage -VersionValue '1.0.0.0' -Destination (Join-Path $testRoot 'v1.msix')
    $packageV2 = New-FixturePackage -VersionValue '2.0.0.0' -Destination (Join-Path $testRoot 'v2.msix')

    $validation = Invoke-ScriptCapture -ScriptPath $packageScript -Arguments @('-Mode', 'Validate', '-PackagePath', $packageV1.Path, '-Version', $packageV1.Version, '-ExpectedSha256', $packageV1.Hash, '-Json')
    Assert-Test -Name 'pacote fixture valida manifest, identidade e hash' -Condition ($validation.ExitCode -eq 0) -Detail $validation.Output

    $tamperedPath = Join-Path $testRoot 'tampered.msix'
    Copy-Item -LiteralPath $packageV1.Path -Destination $tamperedPath
    $bytes = [IO.File]::ReadAllBytes($tamperedPath)
    $bytes[$bytes.Length - 1] = $bytes[$bytes.Length - 1] -bxor 1
    [IO.File]::WriteAllBytes($tamperedPath, $bytes)
    $tampered = Invoke-ScriptCapture -ScriptPath $packageScript -Arguments @('-Mode', 'Validate', '-PackagePath', $tamperedPath, '-Version', $packageV1.Version, '-ExpectedSha256', $packageV1.Hash, '-Json')
    Assert-Test -Name 'pacote adulterado e recusado pelo hash' -Condition ($tampered.ExitCode -ne 0)

    $secureStateRoot = Join-Path $testRoot 'secure-state'
    Set-SecureTestAcl -Path $secureStateRoot
    $updatePlan = Invoke-ScriptCapture -ScriptPath $deploymentScript -Arguments @('-Mode', 'Update', '-PackagePath', $packageV1.Path, '-Version', $packageV1.Version, '-ExpectedSha256', $packageV1.Hash, '-StateRoot', $secureStateRoot, '-RequireSignature', '0', '-PlanOnly', '-Json')
    Assert-Test -Name 'plano de update aceita primeira versao sem instalar' -Condition ($updatePlan.ExitCode -eq 0) -Detail $updatePlan.Output

    $state = [pscustomobject]@{
        SchemaVersion = 1
        PackageIdentityName = 'Dtudo.WinAppDtudo'
        Current = [pscustomobject]@{ PackageIdentityName = 'Dtudo.WinAppDtudo'; Publisher = 'CN=Dtudo Internal'; Version = '2.0.0.0'; PackagePath = $packageV2.Path; Sha256 = $packageV2.Hash; InstalledAtUtc = [DateTime]::UtcNow.ToString('o') }
        Previous = [pscustomobject]@{ PackageIdentityName = 'Dtudo.WinAppDtudo'; Publisher = 'CN=Dtudo Internal'; Version = '1.0.0.0'; PackagePath = $packageV1.Path; Sha256 = $packageV1.Hash; InstalledAtUtc = [DateTime]::UtcNow.ToString('o') }
    }
    $state | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $secureStateRoot 'deployment-state.json') -Encoding utf8
    $rollbackPlan = Invoke-ScriptCapture -ScriptPath $deploymentScript -Arguments @('-Mode', 'Rollback', '-StateRoot', $secureStateRoot, '-RequireSignature', '0', '-PlanOnly', '-Json')
    Assert-Test -Name 'plano de rollback escolhe pacote anterior e downgrade controlado' -Condition ($rollbackPlan.ExitCode -eq 0) -Detail $rollbackPlan.Output
    $downgrade = Invoke-ScriptCapture -ScriptPath $deploymentScript -Arguments @('-Mode', 'Update', '-PackagePath', $packageV1.Path, '-Version', $packageV1.Version, '-ExpectedSha256', $packageV1.Hash, '-StateRoot', $secureStateRoot, '-RequireSignature', '0', '-PlanOnly', '-Json')
    Assert-Test -Name 'update que reduz versao e recusado' -Condition ($downgrade.ExitCode -ne 0)

    $insecureStateRoot = Join-Path $testRoot 'insecure-state'
    New-Item -ItemType Directory -Path $insecureStateRoot -Force | Out-Null
    $acl = Get-Acl -LiteralPath $insecureStateRoot
    $inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
    $usersSid = New-Object Security.Principal.SecurityIdentifier('S-1-5-32-545')
    $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule -ArgumentList @($usersSid, 'Modify', $inheritance, 'None', 'Allow')))
    Set-Acl -LiteralPath $insecureStateRoot -AclObject $acl
    $permission = Invoke-ScriptCapture -ScriptPath $deploymentScript -Arguments @('-Mode', 'Validate', '-PackagePath', $packageV1.Path, '-Version', $packageV1.Version, '-ExpectedSha256', $packageV1.Hash, '-StateRoot', $insecureStateRoot, '-RequireSignature', '0', '-Json')
    Assert-Test -Name 'estado com escrita ampla e recusado' -Condition ($permission.ExitCode -ne 0)

    $workflowText = Get-Content -LiteralPath $workflowPath -Raw
    $actionsPinned = @([regex]::Matches($workflowText, '(?m)^\s*uses:\s*[^\s]+@([0-9a-fA-F]{40})(?:\s|$)')).Count -eq @([regex]::Matches($workflowText, '(?m)^\s*uses:')).Count
    Assert-Test -Name 'workflow de release usa actions fixadas por SHA' -Condition $actionsPinned
    Assert-Test -Name 'workflow nao possui gatilho de pull request' -Condition ($workflowText -notmatch '(?m)^\s*pull_request(?:_target)?:')
    Assert-Test -Name 'deploy usa environment e runner dedicado' -Condition ($workflowText.Contains('environment:') -and $workflowText.Contains('dtudo-msix-release') -and $workflowText.Contains('self-hosted, windows, dtudo-release'))
    Assert-Test -Name 'artefatos de release nao permitem overwrite' -Condition ($workflowText -notmatch '(?im)^\s*overwrite:\s*true')
} finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Output "Etapa 29 local: $script:Passed Passed, $script:Failed Failed"
if ($script:Failed -gt 0) {
    exit 1
}
