[CmdletBinding()]
param(
    [ValidateSet('Backup', 'RestoreVerify', 'Prune')]
    [string]$Mode = 'Backup',

    [string]$BackupRoot = $env:DTUDO_BACKUP_ROOT,
    [string]$RepositoryRoot,
    [string]$RestoreRoot = $env:DTUDO_RESTORE_ROOT,
    [string]$SqlServer = '(localdb)\MSSQLLocalDB',
    [string[]]$DatabaseName = @('Dtudo2026Db'),
    [string]$RestoreDatabaseName,
    [string[]]$FileSource = @(),
    [string[]]$ConfigurationPath = @(),
    [string[]]$RecoveryMaterialPath = @(),
    [ValidateRange(1, 3650)]
    [int]$RetentionDays = 30,
    [switch]$SkipDatabase,
    [switch]$SkipFiles,
    [switch]$SkipConfiguration,
    [switch]$SkipRecoveryMaterial,
    [switch]$KeepRestoreDatabase
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

    if ($normalizedRoot.Equals($normalizedPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    return $normalizedPath.StartsWith($normalizedRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-BackupRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Repo
    )

    if (Test-SameOrWithinPath -Root $Repo -Path $Root) {
        throw 'BackupRoot nao pode estar dentro do repositorio.'
    }
}

function Assert-RestoreRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BackupPath,
        [Parameter(Mandatory = $true)]
        [string]$RestorePath
    )

    if ((Test-SameOrWithinPath -Root $BackupPath -Path $RestorePath) -or
        (Test-SameOrWithinPath -Root $RestorePath -Path $BackupPath)) {
        throw 'RestoreRoot e BackupPath devem ser areas independentes.'
    }
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $normalizedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([char[]]@('\', '/'))
    $normalizedPath = [System.IO.Path]::GetFullPath($Path)

    if ($normalizedPath.Equals($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ''
    }

    $prefix = $normalizedRoot + '\'
    if (-not $normalizedPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Caminho fora da raiz de origem.'
    }

    return $normalizedPath.Substring($prefix.Length)
}

function Assert-SafeRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    if ([System.IO.Path]::IsPathRooted($RelativePath) -or
        $RelativePath -match '(^|[\\/])\.\.([\\/]|$)') {
        throw 'Caminho relativo invalido no manifesto.'
    }
}

function ConvertTo-SqlLiteral {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return "N'$($Value.Replace("'", "''"))'"
}

function ConvertTo-SqlIdentifier {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if ($Value -notmatch '^[A-Za-z_][A-Za-z0-9_$#@]{0,127}$') {
        throw 'Nome de banco fora do formato permitido.'
    }

    return "[$($Value.Replace(']', ']]'))]"
}

function Invoke-SqlCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Server,
        [Parameter(Mandatory = $true)]
        [string]$Query,
        [switch]$DelimitedOutput
    )

    $sqlcmd = Get-Command sqlcmd -ErrorAction Stop
    $arguments = @('-S', $Server, '-E', '-d', 'master', '-b', '-r', '1', '-X', '-Q', $Query)
    if ($DelimitedOutput) {
        $arguments += @('-s', '|', '-W', '-h', '-1')
    }

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& $sqlcmd.Source @arguments 2>&1)
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    if ($LASTEXITCODE -ne 0) {
        throw 'sqlcmd falhou; nenhuma mensagem SQL foi registrada pelo runner.'
    }

    return $output
}

function Add-SkippedReparsePoint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,
        [Parameter(Mandatory = $true)]
        [string]$Kind
    )

    $null = $script:SkippedReparsePoints.Add([ordered]@{
            Kind = $Kind
            RelativePath = ($RelativePath -replace '\\', '/')
        })
}

function Get-FilesWithoutFollowingReparsePoints {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileSystemInfo]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Kind
    )

    if (($Root.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'A raiz de origem nao pode ser um reparse point.'
    }

    if ($Root -is [System.IO.FileInfo]) {
        return @($Root)
    }

    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $null = $pending.Push($Root.FullName)
    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'

    while ($pending.Count -gt 0) {
        $currentPath = $pending.Pop()
        $children = [System.IO.Directory]::EnumerateFileSystemEntries($currentPath)
        foreach ($childPath in $children) {
            $child = Get-Item -LiteralPath $childPath -Force
            if (($child.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                Add-SkippedReparsePoint -RelativePath (Get-RelativePath -Root $Root.FullName -Path $child.FullName) -Kind $Kind
                continue
            }

            if ($child -is [System.IO.DirectoryInfo]) {
                $null = $pending.Push($child.FullName)
            } elseif ($child -is [System.IO.FileInfo]) {
                $null = $files.Add($child)
            }
        }
    }

    return $files.ToArray()
}

function Add-HashedFileToBackup {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$SourceFile,
        [Parameter(Mandatory = $true)]
        [string]$SourceRoot,
        [Parameter(Mandatory = $true)]
        [string]$SourceLabel,
        [Parameter(Mandatory = $true)]
        [string]$Kind,
        [Parameter(Mandatory = $true)]
        [string]$DestinationKindRoot,
        [Parameter(Mandatory = $true)]
        [System.Collections.IList]$Items
    )

    $relativePath = if ($SourceFile.FullName.Equals($SourceRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        $SourceFile.Name
    } else {
        Get-RelativePath -Root $SourceRoot -Path $SourceFile.FullName
    }
    Assert-SafeRelativePath -RelativePath $relativePath

    $backupRelativePath = (Join-Path (Join-Path $Kind $SourceLabel) $relativePath) -replace '\\', '/'
    $destinationPath = Join-Path $DestinationKindRoot (Join-Path $SourceLabel $relativePath)
    Ensure-Directory -Path (Split-Path -Parent $destinationPath)

    $sourceHash = (Get-FileHash -LiteralPath $SourceFile.FullName -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $SourceFile.FullName -Destination $destinationPath -Force
    $destinationFile = Get-Item -LiteralPath $destinationPath
    $destinationHash = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash
    if ($sourceHash -ne $destinationHash) {
        throw 'Hash divergente durante a copia de arquivo.'
    }

    $null = $Items.Add([ordered]@{
            Kind = $Kind
            SourceLabel = $SourceLabel
            RelativePath = ($relativePath -replace '\\', '/')
            BackupRelativePath = $backupRelativePath
            Length = [int64]$destinationFile.Length
            Sha256 = $destinationHash
        })
}

function Add-SourceToBackup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,
        [Parameter(Mandatory = $true)]
        [string]$Kind,
        [Parameter(Mandatory = $true)]
        [int]$SourceIndex,
        [Parameter(Mandatory = $true)]
        [string]$DestinationRoot,
        [Parameter(Mandatory = $true)]
        [System.Collections.IList]$Items
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        throw "Fonte obrigatoria nao encontrada: $Kind source $SourceIndex."
    }

    $source = Get-Item -LiteralPath $SourcePath -Force
    $sourceLabel = '{0}-{1:D2}' -f $Kind.ToLowerInvariant(), $SourceIndex
    $sourceRoot = if ($source -is [System.IO.DirectoryInfo]) { $source.FullName } else { Split-Path -Parent $source.FullName }
    $files = Get-FilesWithoutFollowingReparsePoints -Root $source -Kind $Kind
    $destinationKindRoot = Join-Path $DestinationRoot $Kind.ToLowerInvariant()
    Ensure-Directory -Path $destinationKindRoot

    foreach ($file in $files) {
        Add-HashedFileToBackup `
            -SourceFile $file `
            -SourceRoot $sourceRoot `
            -SourceLabel $sourceLabel `
            -Kind $Kind `
            -DestinationKindRoot $destinationKindRoot `
            -Items $Items
    }
}

function Get-DefaultConfigurationPaths {
    return @(
        'ApiMyAnimes/appsettings.json',
        'ApiMyAnimes/appsettings.Development.json',
        'ApiMyAnimes/Properties/launchSettings.json',
        'ApiMyAnimeList/appsettings.json',
        'ApiMyAnimeList/appsettings.Development.json',
        'ApiMyAnimeList/Properties/launchSettings.json',
        'WinAppDtudo/appsettings.json'
    )
}

function Get-DefaultRecoveryMaterialPaths {
    return @(
        'ApiMyAnimes/Migrations',
        'ApiMyAnimes/Configuration',
        'ApiMyAnimes/ApiMyAnimes.csproj',
        'ApiMyAnimeList/ApiMyAnimeList.csproj',
        'scripts/Invoke-DtudoBackup.ps1',
        'PLANO_SEGURANCA_DTUDO2026.md'
    )
}

function Get-DefaultFileSources {
    $defaults = New-Object 'System.Collections.Generic.List[string]'
    foreach ($relativePath in @('ApiMyAnimes/App_Data', 'ApiMyAnimes/LogsImportacao', 'WinAppDtudo/LogsImportacao')) {
        $absolutePath = Resolve-AbsolutePath -Path $relativePath -BasePath $RepositoryRoot
        if (Test-Path -LiteralPath $absolutePath) {
            $null = $defaults.Add($absolutePath)
        }
    }

    return $defaults.ToArray()
}

function Merge-ConfiguredPaths {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Defaults,
        [string[]]$Provided
    )

    $paths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($path in @($Defaults) + @($Provided)) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            continue
        }

        $absolutePath = Resolve-AbsolutePath -Path $path -BasePath $RepositoryRoot
        if (-not $paths.Contains($absolutePath)) {
            $null = $paths.Add($absolutePath)
        }
    }

    return $paths.ToArray()
}

function New-DatabaseBackup {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Server,
        [Parameter(Mandatory = $true)]
        [string]$StagingRoot,
        [Parameter(Mandatory = $true)]
        [System.Collections.IList]$Items
    )

    $identifier = ConvertTo-SqlIdentifier -Value $Name
    $databaseRoot = Join-Path $StagingRoot 'database'
    Ensure-Directory -Path $databaseRoot
    $backupFileName = ($Name + '.bak')
    $backupPath = Join-Path $databaseRoot $backupFileName
    $backupLiteral = ConvertTo-SqlLiteral -Value $backupPath
    $nameLiteral = ConvertTo-SqlLiteral -Value $Name

    $backupQuery = "IF DB_ID($nameLiteral) IS NULL BEGIN RAISERROR(N'Database not found', 16, 1); RETURN; END; BACKUP DATABASE $identifier TO DISK = $backupLiteral WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;"
    $null = Invoke-SqlCommand -Server $Server -Query $backupQuery

    $verifyQuery = "RESTORE VERIFYONLY FROM DISK = $backupLiteral WITH CHECKSUM;"
    $null = Invoke-SqlCommand -Server $Server -Query $verifyQuery

    $backupFile = Get-Item -LiteralPath $backupPath
    if ($backupFile.Length -le 0) {
        throw 'O backup SQL foi criado vazio.'
    }

    $hash = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash
    $backupRelativePath = ('database/' + $backupFileName)
    $null = $Items.Add([ordered]@{
            Kind = 'Database'
            SourceLabel = 'sql-server'
            RelativePath = $Name
            BackupRelativePath = $backupRelativePath
            Length = [int64]$backupFile.Length
            Sha256 = $hash
        })

    return [ordered]@{
        Name = $Name
        BackupRelativePath = $backupRelativePath
        Length = [int64]$backupFile.Length
        Sha256 = $hash
        BackupStatus = 'Passed'
        VerifyOnlyStatus = 'Passed'
    }
}

function Remove-ExpiredBackups {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [int]$Days
    )

    $cutoff = [DateTime]::UtcNow.Date.AddDays(-($Days - 1))
    $removed = New-Object 'System.Collections.Generic.List[string]'
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return $removed.ToArray()
    }

    foreach ($directory in Get-ChildItem -LiteralPath $Root -Directory -Force) {
        if ($directory.Name -notmatch '^\d{8}$') {
            continue
        }

        $date = [DateTime]::ParseExact($directory.Name, 'yyyyMMdd', [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal)
        if ($date -lt $cutoff -and (Test-Path -LiteralPath (Join-Path $directory.FullName 'manifest.json'))) {
            Remove-Item -LiteralPath $directory.FullName -Recurse -Force
            $null = $removed.Add($directory.Name)
        }
    }

    return $removed.ToArray()
}

function Write-JsonWithoutSecrets {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Value,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $json = $Value | ConvertTo-Json -Depth 12
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8)
}

function Invoke-Backup {
    if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
        throw 'Informe BackupRoot ou DTUDO_BACKUP_ROOT; o destino deve ser um volume separado.'
    }

    $backupRootFull = Resolve-AbsolutePath -Path $BackupRoot -BasePath (Get-Location).Path
    $repoFull = Resolve-AbsolutePath -Path $RepositoryRoot -BasePath (Get-Location).Path
    Assert-BackupRoot -Root $backupRootFull -Repo $repoFull
    Ensure-Directory -Path $backupRootFull

    $backupId = [DateTime]::UtcNow.ToString('yyyyMMdd')
    $finalPath = Join-Path $backupRootFull $backupId
    $stagingPath = Join-Path $backupRootFull ('.staging-' + [Guid]::NewGuid().ToString('N'))
    Ensure-Directory -Path $stagingPath
    $script:SkippedReparsePoints = New-Object 'System.Collections.Generic.List[object]'
    $items = New-Object 'System.Collections.Generic.List[object]'
    $databaseBackups = New-Object 'System.Collections.Generic.List[object]'
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        if (-not $SkipDatabase) {
            foreach ($name in @($DatabaseName)) {
                if ([string]::IsNullOrWhiteSpace($name)) {
                    continue
                }

                $null = $databaseBackups.Add((New-DatabaseBackup -Name $name -Server $SqlServer -StagingRoot $stagingPath -Items $items))
            }
        }

        if (-not $SkipFiles) {
            $fileSources = Merge-ConfiguredPaths -Defaults (Get-DefaultFileSources) -Provided $FileSource
            $index = 1
            foreach ($source in $fileSources) {
                Add-SourceToBackup -SourcePath $source -Kind 'Files' -SourceIndex $index -DestinationRoot $stagingPath -Items $items
                $index++
            }
        }

        if (-not $SkipConfiguration) {
            $configurationSources = Merge-ConfiguredPaths -Defaults (Get-DefaultConfigurationPaths) -Provided $ConfigurationPath
            $index = 1
            foreach ($source in $configurationSources) {
                Add-SourceToBackup -SourcePath $source -Kind 'Configuration' -SourceIndex $index -DestinationRoot $stagingPath -Items $items
                $index++
            }
        }

        if (-not $SkipRecoveryMaterial) {
            $recoverySources = Merge-ConfiguredPaths -Defaults (Get-DefaultRecoveryMaterialPaths) -Provided $RecoveryMaterialPath
            $index = 1
            foreach ($source in $recoverySources) {
                Add-SourceToBackup -SourcePath $source -Kind 'RecoveryMaterial' -SourceIndex $index -DestinationRoot $stagingPath -Items $items
                $index++
            }
        }

        $stopwatch.Stop()
        $manifest = [ordered]@{
            SchemaVersion = '1.0'
            BackupType = 'Dtudo'
            BackupId = $backupId
            CreatedUtc = [DateTime]::UtcNow.ToString('o')
            RetentionDays = $RetentionDays
            DatabaseBackups = @($databaseBackups.ToArray())
            Items = @($items.ToArray())
            SkippedReparsePoints = @($script:SkippedReparsePoints.ToArray())
            Verification = [ordered]@{
                CopyHashes = 'Passed'
                DurationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
                SecretsLogged = $false
            }
        }

        $manifestPath = Join-Path $stagingPath 'manifest.json'
        Write-JsonWithoutSecrets -Value $manifest -Path $manifestPath
        $manifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
        Set-Content -LiteralPath (Join-Path $stagingPath 'manifest.sha256') -Value ($manifestHash + '  manifest.json') -Encoding ASCII

        if (Test-Path -LiteralPath $finalPath) {
            if (-not (Test-Path -LiteralPath (Join-Path $finalPath 'manifest.json'))) {
                throw 'O destino diario existente nao possui manifesto; retencao manual necessaria.'
            }

            Remove-Item -LiteralPath $finalPath -Recurse -Force
        }

        Move-Item -LiteralPath $stagingPath -Destination $finalPath
        $removed = @(Remove-ExpiredBackups -Root $backupRootFull -Days $RetentionDays)
        Write-Output ('Backup concluido: {0}; itens={1}; duracao_s={2}; removidos={3}' -f $backupId, $items.Count, [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3), $removed.Count)
    } catch {
        if (Test-Path -LiteralPath $stagingPath) {
            Remove-Item -LiteralPath $stagingPath -Recurse -Force -ErrorAction SilentlyContinue
        }

        throw
    }
}

function Read-BackupManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $manifestPath = Join-Path $Path 'manifest.json'
    $hashPath = Join-Path $Path 'manifest.sha256'
    if (-not (Test-Path -LiteralPath $manifestPath) -or -not (Test-Path -LiteralPath $hashPath)) {
        throw 'Backup sem manifesto ou hash do manifesto.'
    }

    $expectedHash = (Get-Content -LiteralPath $hashPath -ErrorAction Stop | Select-Object -First 1).ToString().Split(' ')[0]
    $actualHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
    if ($expectedHash -ne $actualHash) {
        throw 'Hash do manifesto divergente.'
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.SchemaVersion -ne '1.0' -or $manifest.BackupType -ne 'Dtudo') {
        throw 'Formato de manifesto nao suportado.'
    }

    return $manifest
}

function Copy-AndVerifyManifestItems {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BackupPath,
        [Parameter(Mandatory = $true)]
        [string]$RestoreRunPath,
        [Parameter(Mandatory = $true)]
        [object]$Manifest
    )

    $count = 0
    foreach ($item in @($Manifest.Items)) {
        if ($item.Kind -eq 'Database') {
            continue
        }

        Assert-SafeRelativePath -RelativePath $item.BackupRelativePath
        $sourcePath = Join-Path $BackupPath ($item.BackupRelativePath -replace '/', '\')
        $destinationPath = Join-Path $RestoreRunPath ($item.BackupRelativePath -replace '/', '\')
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw 'Item do backup ausente durante a restauracao.'
        }

        Ensure-Directory -Path (Split-Path -Parent $destinationPath)
        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
        $hash = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash
        if ($hash -ne $item.Sha256) {
            throw 'Hash divergente durante a restauracao de arquivo.'
        }

        $count++
    }

    return $count
}

function Get-RestoreFileList {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Server,
        [Parameter(Mandatory = $true)]
        [string]$BackupFile
    )

    $backupLiteral = ConvertTo-SqlLiteral -Value $BackupFile
    $query = "RESTORE FILELISTONLY FROM DISK = $backupLiteral WITH CHECKSUM;"
    $lines = Invoke-SqlCommand -Server $Server -Query $query -DelimitedOutput
    $files = New-Object 'System.Collections.Generic.List[object]'

    foreach ($line in $lines) {
        $parts = ([string]$line).Split('|')
        if ($parts.Count -lt 3) {
            continue
        }

        $logicalName = $parts[0].Trim()
        $type = $parts[2].Trim()
        if ([string]::IsNullOrWhiteSpace($logicalName) -or $type -notin @('D', 'L')) {
            continue
        }

        $null = $files.Add([ordered]@{
                LogicalName = $logicalName
                Type = $type
            })
    }

    if ($files.Count -eq 0) {
        throw 'Nao foi possivel ler os arquivos logicos do backup SQL.'
    }

    return $files.ToArray()
}

function Restore-DatabaseInIsolation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Server,
        [Parameter(Mandatory = $true)]
        [object]$DatabaseBackup,
        [Parameter(Mandatory = $true)]
        [string]$BackupPath,
        [Parameter(Mandatory = $true)]
        [string]$RestoreRunPath,
        [Parameter(Mandatory = $true)]
        [int]$Index
    )

    $sourceName = [string]$DatabaseBackup.Name
    $targetName = if (-not [string]::IsNullOrWhiteSpace($RestoreDatabaseName) -and @($DatabaseName).Count -eq 1) {
        $RestoreDatabaseName
    } else {
        '{0}_RestoreCheck_{1}_{2:D2}' -f $sourceName, [DateTime]::UtcNow.ToString('yyyyMMddHHmmss'), $Index
    }
    if ($targetName -eq $sourceName) {
        throw 'A restauracao isolada nao pode usar o nome do banco de origem.'
    }

    $targetIdentifier = ConvertTo-SqlIdentifier -Value $targetName
    $sourceIdentifier = ConvertTo-SqlIdentifier -Value $sourceName
    $backupFile = Join-Path $BackupPath ($DatabaseBackup.BackupRelativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $backupFile -PathType Leaf)) {
        throw 'Arquivo de backup SQL ausente durante a restauracao.'
    }

    $databaseRestorePath = Join-Path $RestoreRunPath ('database-' + $Index.ToString('D2'))
    Ensure-Directory -Path $databaseRestorePath
    $logicalFiles = Get-RestoreFileList -Server $Server -BackupFile $backupFile
    $moveClauses = New-Object 'System.Collections.Generic.List[string]'
    $dataIndex = 0
    $logIndex = 0

    foreach ($logicalFile in $logicalFiles) {
        if ($logicalFile.Type -eq 'D') {
            $extension = if ($dataIndex -eq 0) { '.mdf' } else { '.ndf' }
            $fileIndex = $dataIndex
            $dataIndex++
        } else {
            $extension = '.ldf'
            $fileIndex = $logIndex
            $logIndex++
        }

        $targetFile = Join-Path $databaseRestorePath ($targetName + '-' + $fileIndex.ToString('D2') + $extension)
        $moveClauses.Add(('MOVE {0} TO {1}' -f (ConvertTo-SqlLiteral -Value $logicalFile.LogicalName), (ConvertTo-SqlLiteral -Value $targetFile)))
    }

    $backupLiteral = ConvertTo-SqlLiteral -Value $backupFile
    $moveSql = [string]::Join(', ', $moveClauses.ToArray())
    $restoreQuery = "IF DB_ID($(ConvertTo-SqlLiteral -Value $targetName)) IS NOT NULL BEGIN RAISERROR(N'Restore target already exists', 16, 1); RETURN; END; RESTORE DATABASE $targetIdentifier FROM DISK = $backupLiteral WITH $moveSql, RECOVERY, CHECKSUM; DBCC CHECKDB ($(ConvertTo-SqlLiteral -Value $targetName)) WITH NO_INFOMSGS, ALL_ERRORMSGS;"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $null = Invoke-SqlCommand -Server $Server -Query $restoreQuery
    $stopwatch.Stop()

    $cleanupStatus = 'Kept'
    if (-not $KeepRestoreDatabase) {
        $cleanupQuery = "ALTER DATABASE $targetIdentifier SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE $targetIdentifier;"
        $null = Invoke-SqlCommand -Server $Server -Query $cleanupQuery
        $cleanupStatus = 'DroppedIsolatedDatabase'
    }

    return [ordered]@{
        SourceDatabase = $sourceName
        RestoredDatabase = $targetName
        Integrity = 'DBCC CHECKDB passed'
        RestoreDurationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
        Cleanup = $cleanupStatus
    }
}

function Invoke-RestoreVerification {
    if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
        throw 'Informe BackupRoot ou DTUDO_BACKUP_ROOT.'
    }

    $backupPathFull = Resolve-AbsolutePath -Path $BackupRoot -BasePath (Get-Location).Path
    if (-not (Test-Path -LiteralPath $backupPathFull -PathType Container)) {
        throw 'BackupPath nao encontrado.'
    }

    $manifest = Read-BackupManifest -Path $backupPathFull
    $restoreParent = if ([string]::IsNullOrWhiteSpace($RestoreRoot)) {
        Join-Path ([System.IO.Path]::GetTempPath()) 'DtudoRestoreVerification'
    } else {
        Resolve-AbsolutePath -Path $RestoreRoot -BasePath (Get-Location).Path
    }
    Assert-RestoreRoot -BackupPath $backupPathFull -RestorePath $restoreParent
    Ensure-Directory -Path $restoreParent

    $restoreRunPath = Join-Path $restoreParent ('run-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff'))
    Ensure-Directory -Path $restoreRunPath
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $fileCount = Copy-AndVerifyManifestItems -BackupPath $backupPathFull -RestoreRunPath $restoreRunPath -Manifest $manifest
    $databaseResults = New-Object 'System.Collections.Generic.List[object]'
    $databaseIndex = 1

    foreach ($databaseBackup in @($manifest.DatabaseBackups)) {
        if ($null -eq $databaseBackup -or [string]::IsNullOrWhiteSpace([string]$databaseBackup.Name)) {
            continue
        }

        $null = $databaseResults.Add((Restore-DatabaseInIsolation `
                -Server $SqlServer `
                -DatabaseBackup $databaseBackup `
                -BackupPath $backupPathFull `
                -RestoreRunPath $restoreRunPath `
                -Index $databaseIndex))
        $databaseIndex++
    }

    $stopwatch.Stop()
    $evidence = [ordered]@{
        SchemaVersion = '1.0'
        VerificationType = 'DtudoRestoreVerification'
        StartedUtc = $manifest.CreatedUtc
        CompletedUtc = [DateTime]::UtcNow.ToString('o')
        BackupId = $manifest.BackupId
        FileItemsVerified = $fileCount
        DatabaseResults = @($databaseResults.ToArray())
        DurationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
        SecretsLogged = $false
    }
    $evidencePath = Join-Path $restoreRunPath 'restore-verification.json'
    Write-JsonWithoutSecrets -Value $evidence -Path $evidencePath
    $evidenceHash = (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash
    Set-Content -LiteralPath (Join-Path $restoreRunPath 'restore-verification.sha256') -Value ($evidenceHash + '  restore-verification.json') -Encoding ASCII
    Write-Output ('Restauracao isolada concluida: backup={0}; arquivos={1}; bancos={2}; duracao_s={3}' -f $manifest.BackupId, $fileCount, $databaseResults.Count, [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3))
}

function Invoke-Prune {
    if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
        throw 'Informe BackupRoot ou DTUDO_BACKUP_ROOT.'
    }

    $backupRootFull = Resolve-AbsolutePath -Path $BackupRoot -BasePath (Get-Location).Path
    $removed = @(Remove-ExpiredBackups -Root $backupRootFull -Days $RetentionDays)
    Write-Output ('Retencao concluida: removidos={0}; janela_dias={1}' -f $removed.Count, $RetentionDays)
}

try {
    switch ($Mode) {
        'Backup' { Invoke-Backup }
        'RestoreVerify' { Invoke-RestoreVerification }
        'Prune' { Invoke-Prune }
    }
} catch {
    Write-Error $_.Exception.Message
    exit 1
}
