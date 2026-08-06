[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$TaskName = 'Dtudo2026-Backup-Daily',
    [Parameter(Mandatory = $true)]
    [string]$BackupRoot,
    [string]$RepositoryRoot,
    [ValidatePattern('^([01][0-9]|2[0-3]):[0-5][0-9]$')]
    [string]$StartTime = '02:00',
    [string]$PowerShellExecutable = (Join-Path $PSHOME 'powershell.exe')
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
}

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$BasePath
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

function Test-SameOrWithinPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $normalizedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('\', '/'))
    $normalizedPath = [System.IO.Path]::GetFullPath($Path).TrimEnd([char[]]@('\', '/'))
    return $normalizedRoot.Equals($normalizedPath, [System.StringComparison]::OrdinalIgnoreCase) -or
        $normalizedPath.StartsWith($normalizedRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)
}

$repositoryFull = Resolve-AbsolutePath -Path $RepositoryRoot -BasePath (Get-Location).Path
$backupFull = Resolve-AbsolutePath -Path $BackupRoot -BasePath (Get-Location).Path
$runnerPath = Join-Path $repositoryFull 'scripts\Invoke-DtudoBackup.ps1'
if (-not (Test-Path -LiteralPath $runnerPath -PathType Leaf)) {
    throw 'Runner de backup nao encontrado.'
}
if (Test-SameOrWithinPath -Root $repositoryFull -Path $backupFull) {
    throw 'BackupRoot nao pode estar dentro do repositorio.'
}
if (-not (Test-Path -LiteralPath $PowerShellExecutable -PathType Leaf)) {
    throw 'Executavel do PowerShell nao encontrado.'
}

$start = [DateTime]::ParseExact($StartTime, 'HH:mm', [Globalization.CultureInfo]::InvariantCulture)
$triggerTime = [DateTime]::Today.Add($start.TimeOfDay)
$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$arguments = '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "{0}" -Mode Backup -BackupRoot "{1}" -RepositoryRoot "{2}"' -f $runnerPath, $backupFull, $repositoryFull
$action = New-ScheduledTaskAction -Execute $PowerShellExecutable -Argument $arguments -WorkingDirectory $repositoryFull
$trigger = New-ScheduledTaskTrigger -Daily -At $triggerTime
$principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 8)

if ($PSCmdlet.ShouldProcess($TaskName, 'registrar tarefa diaria de backup')) {
    Register-ScheduledTask `
        -TaskName $TaskName `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Settings $settings `
        -Description 'Backup diario Dtudo com retencao de 30 dias.' `
        -Force | Out-Null
    Write-Output ('Tarefa registrada: {0}; horario={1}; identidade={2}' -f $TaskName, $StartTime, $currentUser)
} else {
    Write-Output ('Simulacao: tarefa={0}; horario={1}; identidade={2}' -f $TaskName, $StartTime, $currentUser)
}
