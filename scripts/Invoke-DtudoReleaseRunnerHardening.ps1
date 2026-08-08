[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidateSet('Validate', 'Apply', 'Rollback')]
    [string]$Mode = 'Validate',
    [Parameter(Mandatory = $true)]
    [string]$RunnerRoot,
    [Parameter(Mandatory = $true)]
    [string]$RunnerAccount,
    [string]$StatePath,
    [switch]$Json,
    [switch]$FailOnBlocked
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$script:Results = New-Object 'System.Collections.Generic.List[object]'

function Add-Result {
    param(
        [Parameter(Mandatory = $true)][string]$Check,
        [Parameter(Mandatory = $true)][ValidateSet('Passed', 'Blocked', 'Warning', 'Failed', 'NotChecked')][string]$Status,
        [Parameter(Mandatory = $true)][string]$Detail
    )

    $null = $script:Results.Add([pscustomobject]@{
            Check = $Check
            Status = $Status
            Detail = $Detail
        })
}

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
}

function Resolve-PrincipalSid {
    param([Parameter(Mandatory = $true)][string]$Principal)

    $accountName = $Principal
    if ($Principal.StartsWith('.\', [StringComparison]::OrdinalIgnoreCase)) {
        $accountName = "$env:COMPUTERNAME\$($Principal.Substring(2))"
    }
    $account = New-Object System.Security.Principal.NTAccount($accountName)
    return $account.Translate([System.Security.Principal.SecurityIdentifier]).Value
}

function Test-IsAdministratorMember {
    param([Parameter(Mandatory = $true)][string]$Sid)

    $getLocalGroupMember = Get-Command Get-LocalGroupMember -ErrorAction SilentlyContinue
    if ($null -eq $getLocalGroupMember) {
        throw 'Get-LocalGroupMember nao esta disponivel para comprovar a restricao da conta.'
    }
    $administratorsSid = New-Object System.Security.Principal.SecurityIdentifier('S-1-5-32-544')
    $administratorsName = $administratorsSid.Translate([System.Security.Principal.NTAccount]).Value.Split('\')[-1]
    $members = @(Get-LocalGroupMember -Group $administratorsName -ErrorAction Stop)
    return @($members | Where-Object { $_.SID.Value -eq $Sid }).Count -gt 0
}

function Assert-RunnerAccount {
    $sid = Resolve-PrincipalSid -Principal $RunnerAccount
    if ($RunnerAccount -notmatch '^(?:\.\\|[^\\]+\\)[A-Za-z][A-Za-z0-9_-]{2,31}$') {
        throw 'A conta do runner deve ser uma conta local dedicada, sem senha ou segredo no repositorio.'
    }
    if (Test-IsAdministratorMember -Sid $sid) {
        throw 'A conta do runner nao pode ser membro de Administrators.'
    }
    if ($RunnerAccount -match '(?i)(Administrator|Guest|DefaultAccount|WDAGUtilityAccount|SYSTEM|LOCAL SERVICE|NETWORK SERVICE)') {
        throw 'A conta do runner nao pode ser uma conta built-in ou de servico do sistema.'
    }
    return $sid
}

function Get-ManagedPaths {
    $root = Resolve-FullPath -Path $RunnerRoot
    return @(
        [pscustomobject]@{ Name = 'RunnerRoot'; Path = $root; RunnerRights = 'ReadAndExecute' }
        [pscustomobject]@{ Name = 'Work'; Path = (Join-Path $root '_work'); RunnerRights = 'Modify' }
        [pscustomobject]@{ Name = 'State'; Path = (Join-Path $root 'state'); RunnerRights = 'Modify' }
    )
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

function Test-BroadWriteAccess {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }
    $acl = Get-Acl -LiteralPath $Path
    $unsafe = @($acl.Access | Where-Object {
            $_.AccessControlType -eq 'Allow' -and
            (Test-BroadPrincipal -IdentityReference $_.IdentityReference) -and
            $_.FileSystemRights.ToString() -match '(?i)(Write|Modify|FullControl|Create|Delete)'
        })
    return $unsafe.Count -gt 0
}

function New-AccessRule {
    param(
        [Parameter(Mandatory = $true)][object]$Principal,
        [Parameter(Mandatory = $true)][string]$Rights
    )

    $identity = if ($Principal -is [System.Security.Principal.SecurityIdentifier]) {
        $Principal
    } else {
        New-Object System.Security.Principal.NTAccount([string]$Principal)
    }
    $inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
    return New-Object System.Security.AccessControl.FileSystemAccessRule -ArgumentList @(
        $identity,
        ([System.Security.AccessControl.FileSystemRights]::$Rights),
        $inheritance,
        [System.Security.AccessControl.PropagationFlags]::None,
        [System.Security.AccessControl.AccessControlType]::Allow
    )
}

function Set-RestrictedAcl {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RunnerRights,
        [Parameter(Mandatory = $true)][string]$RunnerSid
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
    $acl = Get-Acl -LiteralPath $Path
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($rule in @($acl.Access)) {
        $acl.RemoveAccessRule($rule) | Out-Null
    }
    $acl.AddAccessRule((New-AccessRule -Principal (New-Object System.Security.Principal.SecurityIdentifier('S-1-5-18')) -Rights 'FullControl'))
    $acl.AddAccessRule((New-AccessRule -Principal (New-Object System.Security.Principal.SecurityIdentifier('S-1-5-32-544')) -Rights 'FullControl'))
    $runnerAccountObject = New-Object System.Security.Principal.SecurityIdentifier($RunnerSid)
    $acl.AddAccessRule((New-AccessRule -Principal $runnerAccountObject.Translate([System.Security.Principal.NTAccount]).Value -Rights $RunnerRights))
    Set-Acl -LiteralPath $Path -AclObject $acl
}

function Test-IsAdministrator {
    return ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-Administrator {
    if (-not (Test-IsAdministrator)) {
        throw 'Apply/Rollback exigem uma sessao elevada; nenhuma alteracao foi aplicada.'
    }
}

function Resolve-StatePath {
    if (-not [string]::IsNullOrWhiteSpace($StatePath)) {
        return Resolve-FullPath -Path $StatePath
    }
    return Join-Path (Resolve-FullPath -Path $RunnerRoot) 'runner-hardening-state.json'
}

function Write-State {
    param([Parameter(Mandatory = $true)][object]$State)

    $path = Resolve-StatePath
    $parent = Split-Path -Parent $path
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    $temporaryPath = "$path.$PID.tmp"
    [IO.File]::WriteAllText($temporaryPath, ($State | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding($false)))
    Move-Item -LiteralPath $temporaryPath -Destination $path -Force
}

function Read-State {
    $path = Resolve-StatePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Estado de hardening do runner nao encontrado: $path"
    }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

$managedPaths = Get-ManagedPaths

if ($Mode -eq 'Validate') {
    try {
        $runnerSid = Assert-RunnerAccount
        Add-Result -Check 'runner-account-resolved' -Status 'Passed' -Detail 'A conta dedicada existe e nao e Administrators.'
        foreach ($managedPath in $managedPaths) {
            if (-not (Test-Path -LiteralPath $managedPath.Path)) {
                Add-Result -Check ("path-" + $managedPath.Name) -Status 'Blocked' -Detail "Diretorio ausente: $($managedPath.Path)"
                continue
            }
            if (Test-BroadWriteAccess -Path $managedPath.Path) {
                Add-Result -Check ("acl-" + $managedPath.Name) -Status 'Failed' -Detail 'Principal amplo possui escrita no diretorio.'
            } else {
                Add-Result -Check ("acl-" + $managedPath.Name) -Status 'Passed' -Detail 'Nao ha escrita ampla detectada.'
            }
        }
    } catch {
        Add-Result -Check 'runner-validation' -Status 'Blocked' -Detail $_.Exception.Message
    }
} elseif ($Mode -eq 'Apply') {
    Assert-Administrator
    $runnerSid = Assert-RunnerAccount
    $state = New-Object 'System.Collections.Generic.List[object]'
    foreach ($managedPath in $managedPaths) {
        $existed = Test-Path -LiteralPath $managedPath.Path
        $sddl = if ($existed) { (Get-Acl -LiteralPath $managedPath.Path).Sddl } else { $null }
        $state.Add([pscustomobject]@{ Name = $managedPath.Name; Path = $managedPath.Path; Existed = $existed; Sddl = $sddl })
        if ($PSCmdlet.ShouldProcess($managedPath.Path, 'Aplicar ACL restrita do runner')) {
            Set-RestrictedAcl -Path $managedPath.Path -RunnerRights $managedPath.RunnerRights -RunnerSid $runnerSid
        }
    }
    Write-State -State ([pscustomobject]@{
            SchemaVersion = 1
            RunnerAccount = $RunnerAccount
            AppliedAtUtc = [DateTime]::UtcNow.ToString('o')
            Paths = $state.ToArray()
        })
    Add-Result -Check 'runner-acl-apply' -Status 'Passed' -Detail 'ACLs do root, work e state foram aplicadas; nenhuma conta ou senha foi criada.'
} else {
    Assert-Administrator
    $state = Read-State
    foreach ($managedPath in @($state.Paths)) {
        if (-not (Test-Path -LiteralPath $managedPath.Path)) {
            continue
        }
        if ([string]::IsNullOrWhiteSpace([string]$managedPath.Sddl)) {
            $acl = Get-Acl -LiteralPath $managedPath.Path
            $acl.SetAccessRuleProtection($false, $true)
            Set-Acl -LiteralPath $managedPath.Path -AclObject $acl
        } else {
            $acl = New-Object System.Security.AccessControl.DirectorySecurity
            $acl.SetSecurityDescriptorSddlForm([string]$managedPath.Sddl)
            Set-Acl -LiteralPath $managedPath.Path -AclObject $acl
        }
    }
    Add-Result -Check 'runner-acl-rollback' -Status 'Passed' -Detail 'ACLs anteriores foram restauradas; dados e instalacao do runner nao foram removidos.'
}

if ($FailOnBlocked -and @($script:Results | Where-Object { $_.Status -in @('Blocked', 'Failed') }).Count -gt 0) {
    if ($Json) {
        $script:Results | ConvertTo-Json -Depth 5 -Compress
    } else {
        $script:Results
    }
    exit 1
}

if ($Json) {
    $script:Results | ConvertTo-Json -Depth 5 -Compress
} else {
    $script:Results
}
