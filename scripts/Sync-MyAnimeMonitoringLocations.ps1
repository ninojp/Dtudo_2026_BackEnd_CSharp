[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$SelfTest,
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'Dtudo2026Db',
    [string]$ReportDirectory = (Join-Path $env:LOCALAPPDATA 'Dtudo2026\Monitoring\Associations')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-CollectionFolderName([string]$Title) {
    $name = $Title.Trim()
    foreach ($character in [IO.Path]::GetInvalidFileNameChars()) { $name = $name.Replace($character, [char]' ') }
    while ($name.Contains('  ')) { $name = $name.Replace('  ', ' ') }
    $name = $name.Trim().TrimEnd([char[]]'. ')
    if ($name.Length -gt 255) { $name = $name.Substring(0, 255).TrimEnd([char[]]'. ') }
    $reserved = @('CON', 'PRN', 'AUX', 'NUL') + @(1..9 | ForEach-Object { "COM$_"; "LPT$_" })
    if ($reserved -contains [IO.Path]::GetFileNameWithoutExtension($name)) { $name = '_' + $name }
    if ([string]::IsNullOrWhiteSpace($name)) { return 'SemNome' }
    return $name
}

function Test-SameLocation($First, $Second) {
    return [string]::Equals($First.RootKey, $Second.RootKey, [StringComparison]::Ordinal) -and
        [string]::Equals($First.RelativePath, $Second.RelativePath, [StringComparison]::OrdinalIgnoreCase)
}

function New-AssociationPlan([object[]]$Folders, [object[]]$Collections, [object[]]$Existing) {
    $names = [Collections.Generic.Dictionary[string,Collections.Generic.List[object]]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($collection in $Collections) {
        foreach ($name in @($collection.Titulo, (ConvertTo-CollectionFolderName $collection.Titulo)) | Select-Object -Unique) {
            if (-not $names.ContainsKey($name)) { $names[$name] = [Collections.Generic.List[object]]::new() }
            if (-not @($names[$name] | Where-Object { $_.Id -eq $collection.Id }).Count) { $names[$name].Add($collection) }
        }
    }
    $plan = [Collections.Generic.List[object]]::new()
    foreach ($folder in $Folders) {
        $candidates = @()
        if ($names.ContainsKey($folder.RelativePath)) { $candidates = @($names[$folder.RelativePath].ToArray()) }
        $evidence = 'Exact title or current folder-name sanitization only'
        $coverIds = @()
        if ($folder.PSObject.Properties.Name -contains 'CoverIds') { $coverIds = @($folder.CoverIds) }
        if ($coverIds.Count -gt 0) {
            $byIds = @($Collections | Where-Object {
                $collection = $_
                ($collection.PSObject.Properties.Name -contains 'AnimesMalId') -and
                    @($coverIds | Where-Object { $collection.AnimesMalId -notcontains $_ }).Count -eq 0
            })
            if ($byIds.Count -gt 0) {
                $candidates = $byIds
                $evidence = 'All numeric JPG cover IDs belong to the candidate internal collection'
            }
        }
        $mapped = @($Existing | Where-Object { Test-SameLocation $_ $folder })
        $row = [pscustomobject]@{
            RootKey = $folder.RootKey; RelativePath = $folder.RelativePath
            MyAnimeId = $null; CandidateIds = @($candidates | ForEach-Object Id)
            Status = 'MissingCatalog'; Evidence = $evidence; CoverIds = $coverIds
        }
        if ($mapped.Count -gt 0) {
            $row.Status = if ($mapped.Count -eq 1) { 'Existing' } else { 'ExistingConflict' }
            $row.MyAnimeId = $mapped[0].MyAnimeId
        }
        elseif ($candidates.Count -gt 1) { $row.Status = 'AmbiguousTitle' }
        elseif ($candidates.Count -eq 1) {
            $row.MyAnimeId = $candidates[0].Id
            $alreadyMapped = @($Existing | Where-Object { $_.MyAnimeId -eq $row.MyAnimeId })
            $row.Status = if ($alreadyMapped.Count) { 'CollectionAlreadyMappedElsewhere' } else { 'Ready' }
        }
        $plan.Add($row)
    }
    foreach ($row in $plan | Where-Object { $_.Status -eq 'Ready' }) {
        $competing = @($plan | Where-Object { $_.CandidateIds -contains $row.MyAnimeId })
        if ($competing.Count -gt 1) { $row.Status = 'MultipleFolders' }
    }
    return $plan.ToArray()
}

function Assert-Test([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Self-test failed: $Message" }
}

if ($SelfTest) {
    $catalog = @(
        [pscustomobject]@{ Id = 1; Titulo = 'Alpha: Title' },
        [pscustomobject]@{ Id = 2; Titulo = 'Duplicate' },
        [pscustomobject]@{ Id = 3; Titulo = 'Duplicate' },
        [pscustomobject]@{ Id = 4; Titulo = 'Existing' },
        [pscustomobject]@{ Id = 5; Titulo = 'TwoFolders' }
    )
    $folders = @('Alpha Title', 'Duplicate', 'Existing', 'Unknown', 'TwoFolders') | ForEach-Object {
        [pscustomobject]@{ RootKey = 'H_A'; RelativePath = $_ }
    }
    $folders += [pscustomobject]@{ RootKey = 'X_A'; RelativePath = 'TwoFolders' }
    $existing = @([pscustomobject]@{ MyAnimeId = 4; RootKey = 'H_A'; RelativePath = 'Existing' })
    $plan = @(New-AssociationPlan $folders $catalog $existing)
    Assert-Test (@($plan | Where-Object Status -eq 'Ready').Count -eq 1) 'Only one unambiguous match'
    Assert-Test (@($plan | Where-Object Status -eq 'AmbiguousTitle').Count -eq 1) 'Duplicate titles rejected'
    Assert-Test (@($plan | Where-Object Status -eq 'MultipleFolders').Count -eq 2) 'Multiple folders rejected'
    Assert-Test (@($plan | Where-Object Status -eq 'Existing').Count -eq 1) 'Existing binding preserved'
    Assert-Test (@($plan | Where-Object Status -eq 'MissingCatalog').Count -eq 1) 'Unmatched folder not assigned'
    $existing += [pscustomobject]@{ MyAnimeId = 1; RootKey = 'H_A'; RelativePath = 'Alpha Title' }
    $second = @(New-AssociationPlan $folders $catalog $existing)
    Assert-Test (@($second | Where-Object Status -eq 'Ready').Count -eq 0) 'Second run is idempotent'
    Assert-Test ((ConvertTo-CollectionFolderName '  Alpha:  Title. ') -eq 'Alpha Title') 'Folder sanitization matches creator'
    Assert-Test ((ConvertTo-CollectionFolderName 'CON') -eq '_CON') 'Reserved Windows name'
    $idCatalog = @(
        [pscustomobject]@{ Id = 10; Titulo = 'Long title'; AnimesMalId = [int[]](ConvertFrom-Json -InputObject '[11,12]') },
        [pscustomobject]@{ Id = 11; Titulo = 'Long title'; AnimesMalId = @(11,13) }
    )
    $idFolder = @([pscustomobject]@{ RootKey = 'H_A'; RelativePath = 'Abbreviated'; CoverIds = @(11,12) })
    $idPlan = @(New-AssociationPlan $idFolder $idCatalog @())
    Assert-Test ($idPlan[0].Status -eq 'Ready' -and $idPlan[0].MyAnimeId -eq 10) 'All cover IDs disambiguate the internal collection'
    $idFolder[0].CoverIds = @(11)
    $idPlan = @(New-AssociationPlan $idFolder $idCatalog @())
    Assert-Test ($idPlan[0].Status -eq 'AmbiguousTitle') 'Shared cover ID never chooses arbitrary collection'
    $idFolder[0].CoverIds = @(12,13)
    $idPlan = @(New-AssociationPlan $idFolder $idCatalog @())
    Assert-Test ($idPlan[0].Status -eq 'MissingCatalog') 'Conflicting IDs do not produce a match'
    Write-Output '11 association self-tests passed. No database or collection access.'
    return
}

function Assert-OrdinaryDirectory([string]$Path) {
    $current = [IO.DirectoryInfo]::new($Path)
    while ($null -ne $current) {
        $current.Refresh()
        if (-not $current.Exists) { throw "Directory unavailable: $($current.FullName)" }
        if (($current.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Redirected directory refused: $($current.FullName)" }
        $current = $current.Parent
    }
}

function Read-SqlRows([Data.SqlClient.SqlConnection]$Connection, [Data.SqlClient.SqlTransaction]$Transaction, [string]$Sql) {
    $command = $Connection.CreateCommand()
    $command.Transaction = $Transaction
    $command.CommandText = $Sql
    $command.CommandTimeout = 30
    try {
        $reader = $command.ExecuteReader()
        try {
            while ($reader.Read()) {
                $row = [ordered]@{}
                for ($index = 0; $index -lt $reader.FieldCount; $index++) { $row[$reader.GetName($index)] = $reader.GetValue($index) }
                [pscustomobject]$row
            }
        }
        finally { $reader.Close() }
    }
    finally { $command.Dispose() }
}

$reportPath = [IO.Path]::GetFullPath($ReportDirectory)
if ($reportPath -notmatch '^[A-FIK-Z]:\\' -or $reportPath -match '^[GHJ]:\\') { throw 'Report directory must be local and outside H:, G: and J:.' }
$ancestor = [IO.DirectoryInfo]::new($reportPath)
while (-not $ancestor.Exists) { $ancestor = $ancestor.Parent }
Assert-OrdinaryDirectory $ancestor.FullName

$roots = @([pscustomobject]@{ Key = 'H_#Dots'; Path = 'H:\#Dots' })
$roots += 'ABCDEFGHIJKLMNOPQRU'.ToCharArray() | ForEach-Object { [pscustomobject]@{ Key = "H_$_"; Path = "H:\$_" } }
$roots += 'SVWXYZ'.ToCharArray() | ForEach-Object { [pscustomobject]@{ Key = "G_$_"; Path = "G:\$_" } }
$roots += [pscustomobject]@{ Key = 'J_T'; Path = 'J:\T' }
$roots += @('#Dots') + @('ABCDEFGHIJKLMNOPQRSTUVWXYZ'.ToCharArray() | ForEach-Object { [string]$_ }) | ForEach-Object {
    [pscustomobject]@{ Key = "X_$_"; Path = "G:\AnimeX\$_" }
}
if ($roots.Count -ne 54) { throw 'Expected exactly 54 authorized letter roots.' }
$folders = [Collections.Generic.List[object]]::new()
$issues = [Collections.Generic.List[object]]::new()
foreach ($root in $roots) {
    try {
        Assert-OrdinaryDirectory $root.Path
        foreach ($folder in Get-ChildItem -LiteralPath $root.Path -Directory -Force -ErrorAction Stop) {
            if ($folder.Name -in @('.ImportanteX', '$RECYCLE.BIN', 'System Volume Information') -or $folder.Name -like '.dtudo-*') { continue }
            if (($folder.Attributes -band ([IO.FileAttributes]::ReparsePoint -bor [IO.FileAttributes]::System)) -ne 0) {
                $issues.Add([pscustomobject]@{ RootKey = $root.Key; Status = 'SkippedDirectory'; Path = $folder.FullName })
                continue
            }
            $folders.Add([pscustomobject]@{ RootKey = $root.Key; RelativePath = $folder.Name })
        }
    }
    catch { $issues.Add([pscustomobject]@{ RootKey = $root.Key; Status = 'RootUnavailable'; Detail = $_.Exception.Message }) }
}

$builder = [Data.SqlClient.SqlConnectionStringBuilder]::new()
$builder['Data Source'] = $Server
$builder['Initial Catalog'] = $Database
$builder['Integrated Security'] = $true
$builder['Connect Timeout'] = 10
$connection = [Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$transaction = $null
$committed = $false
$runId = [Guid]::NewGuid().ToString('N')
$reportFile = Join-Path $reportPath ("associations-{0}-{1}.json" -f (Get-Date -Format 'yyyyMMdd-HHmmss'), $runId)
try {
    $connection.Open()
    $catalog = @(Read-SqlRows $connection $null 'SELECT Id, Titulo, AnimesMalId FROM dbo.MyAnimes')
    foreach ($collection in $catalog) { $collection.AnimesMalId = [int[]](ConvertFrom-Json -InputObject ([string]$collection.AnimesMalId)) }
    $existing = @(Read-SqlRows $connection $null 'SELECT MyAnimeId, RootKey, RelativePath FROM dbo.MyAnimeMonitoringLocations')
    $preliminary = @(New-AssociationPlan $folders.ToArray() $catalog $existing)
    foreach ($row in $preliminary | Where-Object { $_.Status -notin @('Ready', 'Existing', 'ExistingConflict', 'CollectionAlreadyMappedElsewhere') }) {
        $root = $roots | Where-Object Key -eq $row.RootKey
        $path = Join-Path $root.Path $row.RelativePath
        try {
            Assert-OrdinaryDirectory $path
            $coverIds = [Collections.Generic.HashSet[int]]::new()
            $levels = @($path) + @(Get-ChildItem -LiteralPath $path -Directory -Force | Where-Object {
                ($_.Attributes -band ([IO.FileAttributes]::ReparsePoint -bor [IO.FileAttributes]::System)) -eq 0 -and
                $_.Name -notin @('.ImportanteX', '$RECYCLE.BIN', 'System Volume Information') -and $_.Name -notlike '.dtudo-*'
            } | ForEach-Object FullName)
            foreach ($level in $levels) {
                Assert-OrdinaryDirectory $level
                foreach ($cover in Get-ChildItem -LiteralPath $level -Filter '*.jpg' -File -Force) {
                    if (($cover.Attributes -band ([IO.FileAttributes]::ReparsePoint -bor [IO.FileAttributes]::System)) -ne 0) { continue }
                    $coverId = 0
                    if ($cover.BaseName -match '^\d+$' -and [int]::TryParse($cover.BaseName, [ref]$coverId) -and $coverId -gt 0) { [void]$coverIds.Add($coverId) }
                }
            }
            $folder = $folders | Where-Object { Test-SameLocation $_ $row }
            $folder | Add-Member -NotePropertyName CoverIds -NotePropertyValue @($coverIds | Sort-Object)
        }
        catch { $issues.Add([pscustomobject]@{ RootKey = $root.Key; Status = 'EvidenceReadFailed'; Detail = $_.Exception.Message }) }
    }
    if ($Apply) {
        if ($issues.Count -gt 0) { throw 'Apply refused: incomplete discovery. Run preview and resolve directory errors first.' }
        $transaction = $connection.BeginTransaction([Data.IsolationLevel]::Serializable)
    }
    $catalog = @(Read-SqlRows $connection $transaction 'SELECT Id, Titulo, AnimesMalId FROM dbo.MyAnimes')
    foreach ($collection in $catalog) { $collection.AnimesMalId = [int[]](ConvertFrom-Json -InputObject ([string]$collection.AnimesMalId)) }
    $existing = @(Read-SqlRows $connection $transaction 'SELECT MyAnimeId, RootKey, RelativePath FROM dbo.MyAnimeMonitoringLocations')
    $plan = @(New-AssociationPlan $folders.ToArray() $catalog $existing)
    $ready = @($plan | Where-Object Status -eq 'Ready')
    [void][IO.Directory]::CreateDirectory($reportPath)
    $report = [ordered]@{
        RunId = $runId; ObservedAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); Mode = $(if ($Apply) { 'Apply' } else { 'Preview' })
        Database = $Database; AuthorizedRoots = $roots.Count; CollectionFolders = $folders.Count
        CollectionsInDatabase = $catalog.Count; ExistingBefore = $existing; Plan = $plan; DiscoveryIssues = $issues.ToArray()
        CatalogWithoutFolderCandidate = @($catalog | Where-Object { $collectionId = $_.Id; -not @($plan | Where-Object { $_.MyAnimeId -eq $collectionId -or $_.CandidateIds -contains $collectionId }).Count })
    }
    $json = $report | ConvertTo-Json -Depth 12
    $stream = [IO.File]::Open($reportFile, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
    try {
        $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false))
        try { $writer.Write($json) } finally { $writer.Dispose() }
    }
    finally { $stream.Dispose() }
    if ($Apply) {
        foreach ($row in $ready) {
            $root = $roots | Where-Object Key -eq $row.RootKey
            Assert-OrdinaryDirectory (Join-Path $root.Path $row.RelativePath)
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandText = 'INSERT dbo.MyAnimeMonitoringLocations (MyAnimeId, RootKey, RelativePath) VALUES (@Id, @Root, @Path)'
            [void]$command.Parameters.Add('@Id', [Data.SqlDbType]::Int)
            [void]$command.Parameters.Add('@Root', [Data.SqlDbType]::NVarChar, 100)
            [void]$command.Parameters.Add('@Path', [Data.SqlDbType]::NVarChar, -1)
            $command.Parameters['@Id'].Value = $row.MyAnimeId
            $command.Parameters['@Root'].Value = $row.RootKey
            $command.Parameters['@Path'].Value = $row.RelativePath
            try { [void]$command.ExecuteNonQuery() } finally { $command.Dispose() }
        }
        $verified = @(Read-SqlRows $connection $transaction 'SELECT MyAnimeId, RootKey, RelativePath FROM dbo.MyAnimeMonitoringLocations')
        if ($verified.Count -ne $existing.Count + $ready.Count) { throw 'Verification count mismatch; transaction will roll back.' }
        foreach ($row in @($existing) + @($ready)) {
            if (@($verified | Where-Object { $_.MyAnimeId -eq $row.MyAnimeId -and (Test-SameLocation $_ $row) }).Count -ne 1) {
                throw 'Mapping verification failed; transaction will roll back.'
            }
        }
        $transaction.Commit()
        $committed = $true
    }
    [pscustomobject]@{
        Mode = $report.Mode; Folders = $folders.Count; Catalog = $catalog.Count; Existing = $existing.Count
        Ready = $ready.Count; Inserted = $(if ($committed) { $ready.Count } else { 0 }); Committed = $committed
        StatusCounts = @($plan | Group-Object Status | Select-Object Name, Count); DiscoveryIssues = $issues.Count
        Report = $reportFile
    } | ConvertTo-Json -Depth 6
}
finally {
    if ($null -ne $transaction) { if (-not $committed) { $transaction.Rollback() }; $transaction.Dispose() }
    $connection.Dispose()
}
