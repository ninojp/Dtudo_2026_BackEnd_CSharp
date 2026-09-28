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

function Get-FirstWords([string]$Value) {
    $normalized = $Value.Normalize([Text.NormalizationForm]::FormD)
    $builder = [Text.StringBuilder]::new()
    foreach ($character in $normalized.ToCharArray()) {
        if ([Globalization.CharUnicodeInfo]::GetUnicodeCategory($character) -eq [Globalization.UnicodeCategory]::NonSpacingMark) { continue }
        [void]$builder.Append($character)
    }
    return @([Regex]::Matches($builder.ToString(), '[\p{L}\p{N}]+') | Select-Object -First 4 | ForEach-Object { $_.Value.ToUpperInvariant() })
}

function Test-FirstFourWords([string]$FolderName, [string]$Title) {
    $folderWords = @(Get-FirstWords $FolderName)
    $titleWords = @(Get-FirstWords $Title)
    $count = [Math]::Min(4, [Math]::Min($folderWords.Count, $titleWords.Count))
    if ($count -eq 0) { return $false }
    return (($folderWords | Select-Object -First $count) -join ' ') -ieq (($titleWords | Select-Object -First $count) -join ' ')
}

function Test-SameLocation($First, $Second) {
    return [string]::Equals($First.RootKey, $Second.RootKey, [StringComparison]::Ordinal) -and
        [string]::Equals($First.RelativePath, $Second.RelativePath, [StringComparison]::OrdinalIgnoreCase)
}

function Convert-LegacyRootKey([string]$RootKey) {
    if ($RootKey -ceq 'H_#Dots') { return 'E_.Dots' }
    if ($RootKey -cmatch '^H_([A-Q])$') { return 'E_' + $Matches[1] }
    if ($RootKey -ceq 'E_R') { return 'H_R' }
    if ($RootKey -cmatch '^G_(S|V|W|X|Y|Z)$') { return 'H_' + $Matches[1] }
    if ($RootKey -ceq 'J_T') { return 'H_T' }
    if ($RootKey -ceq 'X_#Dots') { return 'X_.Dots' }
    return $RootKey
}

function Convert-ExistingLocations([object[]]$Locations, [object[]]$Roots) {
    $currentKeys = @($Roots | ForEach-Object { $_.Key })
    foreach ($location in $Locations) {
        $previousRootKey = [string]$location.RootKey
        $rootKey = Convert-LegacyRootKey $previousRootKey
        if ($currentKeys -notcontains $rootKey) {
            throw "Existing monitoring location uses an unauthorized root key: $previousRootKey."
        }
        [pscustomobject]@{
            MyAnimeId = $location.MyAnimeId
            RootKey = $rootKey
            RelativePath = [string]$location.RelativePath
            PreviousRootKey = $previousRootKey
        }
    }
}

function Get-RootRelocations([object[]]$Locations) {
    @($Locations | Where-Object { $_.PreviousRootKey -cne $_.RootKey })
}

function New-AuthorizedRoots {
    $roots = [Collections.Generic.List[object]]::new()
    [void]$roots.Add([pscustomobject]@{ Key = 'E_.Dots'; Path = 'E:\.Dots' })
    foreach ($letter in 'ABCDEFGHIJKLMNOPQ'.ToCharArray()) {
        [void]$roots.Add([pscustomobject]@{ Key = "E_$letter"; Path = "E:\$letter" })
    }
    foreach ($letter in 'RSTUVWXYZ'.ToCharArray()) {
        [void]$roots.Add([pscustomobject]@{ Key = "H_$letter"; Path = "H:\$letter" })
    }
    [void]$roots.Add([pscustomobject]@{ Key = 'X_.Dots'; Path = 'H:\AnimeX\.Dots' })
    foreach ($letter in 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.ToCharArray()) {
        [void]$roots.Add([pscustomobject]@{ Key = "X_$letter"; Path = "H:\AnimeX\$letter" })
    }
    if ($roots.Count -ne 54) { throw 'Expected exactly 54 authorized letter roots.' }
    return $roots.ToArray()
}

function Get-CollectionCoverIds([string]$CollectionPath, [string]$RootKey, [string]$RelativePath, [Collections.Generic.List[object]]$Issues) {
    $coverIds = [Collections.Generic.HashSet[int]]::new()
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push($CollectionPath)
    $imageExtensions = @('.jpg', '.jpeg', '.png', '.webp', '.gif', '.bmp')
    while ($pending.Count -gt 0) {
        $current = $pending.Pop()
        try {
            foreach ($item in Get-ChildItem -LiteralPath $current -Force -ErrorAction Stop) {
                $itemRelative = [IO.Path]::GetRelativePath($CollectionPath, $item.FullName)
                if (($item.Attributes -band ([IO.FileAttributes]::ReparsePoint -bor [IO.FileAttributes]::System)) -ne 0) {
                    if ($item.PSIsContainer) { $Issues.Add([pscustomobject]@{ RootKey = $RootKey; Status = 'SkippedDirectory'; Path = (Join-Path $RelativePath $itemRelative) }) }
                    continue
                }
                if ($item.PSIsContainer) {
                    if ($item.Name -notin @('.ImportanteX', '$RECYCLE.BIN', 'System Volume Information') -and $item.Name -notlike '.dtudo-*') { $pending.Push($item.FullName) }
                    continue
                }
                if ($imageExtensions -notcontains $item.Extension.ToLowerInvariant()) { continue }
                $coverId = 0
                if ($item.BaseName -match '^\d+$' -and [int]::TryParse($item.BaseName, [ref]$coverId) -and $coverId -gt 0) { [void]$coverIds.Add($coverId) }
            }
        }
        catch { $Issues.Add([pscustomobject]@{ RootKey = $RootKey; Status = 'EvidenceReadFailed'; Path = (Join-Path $RelativePath ([IO.Path]::GetRelativePath($CollectionPath, $current))); Detail = $_.Exception.Message }) }
    }
    return @($coverIds | Sort-Object)
}

function New-AssociationPlan([object[]]$Folders, [object[]]$Collections, [object[]]$Existing) {
    $catalogIds = [Collections.Generic.HashSet[int]]::new()
    foreach ($collection in $Collections) { foreach ($id in @($collection.AnimesMalId)) { if ($id -gt 0) { [void]$catalogIds.Add([int]$id) } } }
    $plan = [Collections.Generic.List[object]]::new()
    foreach ($folder in $Folders) {
        $titleCandidates = @($Collections | Where-Object { Test-FirstFourWords $folder.RelativePath $_.Titulo })
        $coverIds = @()
        if ($folder.PSObject.Properties.Name -contains 'CoverIds') { $coverIds = @($folder.CoverIds) }
        $validCoverIds = @($coverIds | Where-Object { $catalogIds.Contains([int]$_) } | Select-Object -Unique)
        $scored = @($Collections | ForEach-Object {
            $collection = $_
            $score = @($validCoverIds | Where-Object { $collection.AnimesMalId -contains $_ }).Count
            if ($score -gt 0) { [pscustomobject]@{ Collection = $collection; Score = $score } }
        } | Sort-Object Score -Descending)
        $candidates = $titleCandidates
        $evidence = if ($titleCandidates.Count -gt 0) { 'Correspondencia basada nas primeiras quatro palavras do nome da pasta.' } else { 'Nenhuma correspondencia pelas primeiras quatro palavras.' }
        if ($validCoverIds.Count -gt 0 -and $scored.Count -gt 0) {
            $bestScore = [int]$scored[0].Score
            $best = @($scored | Where-Object { $_.Score -eq $bestScore } | ForEach-Object { $_.Collection })
            if ($best.Count -gt 1) {
                $titleCandidateIds = @($titleCandidates | ForEach-Object { [int]$_.Id })
                $titleBest = @($best | Where-Object { $titleCandidateIds -contains ([int]$_.Id) })
                if ($titleBest.Count -eq 1) { $best = $titleBest }
            }
            $candidates = $best
            $evidence = "$bestScore de $($validCoverIds.Count) mal_id validos das imagens correspondem a colecao; IDs tem prioridade."
        }
        elseif ($coverIds.Count -gt 0) {
            $evidence += ' As imagens nao possuem mal_id presente no catalogo; nenhum ID foi usado como evidencia.'
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
    $roots = @(New-AuthorizedRoots)
    Assert-Test ($roots.Count -eq 54) 'New collection layout contains 54 authorized roots'
    Assert-Test ((Convert-LegacyRootKey 'H_A') -eq 'E_A') 'Legacy H root moves to E'
    Assert-Test ((Convert-LegacyRootKey 'H_R') -eq 'H_R') 'R remains on the H root'
    Assert-Test ((Convert-LegacyRootKey 'E_R') -eq 'H_R') 'Intermediate E R root moves back to H'
    Assert-Test ((Convert-LegacyRootKey 'G_S') -eq 'H_S') 'Legacy G root moves to H'
    Assert-Test ((Convert-LegacyRootKey 'J_T') -eq 'H_T') 'Legacy J root moves to H'
    Assert-Test ((Convert-LegacyRootKey 'X_#Dots') -eq 'X_.Dots') 'Legacy AnimeX #Dots root is renamed'
    $legacyLocations = @(
        [pscustomobject]@{ MyAnimeId = 1; RootKey = 'H_A'; RelativePath = 'Alpha' },
        [pscustomobject]@{ MyAnimeId = 2; RootKey = 'X_#Dots'; RelativePath = 'Dots' }
    )
    $translatedLocations = @(Convert-ExistingLocations $legacyLocations $roots)
    Assert-Test ($translatedLocations[0].RootKey -eq 'E_A' -and $translatedLocations[1].RootKey -eq 'X_.Dots') 'Legacy locations translate to current roots'
    Assert-Test (@(Get-RootRelocations $translatedLocations).Count -eq 2) 'Legacy locations are reported for relocation'
    $catalog = @(
        [pscustomobject]@{ Id = 1; Titulo = 'Alpha: Title'; AnimesMalId = @() },
        [pscustomobject]@{ Id = 2; Titulo = 'Duplicate'; AnimesMalId = @() },
        [pscustomobject]@{ Id = 3; Titulo = 'Duplicate'; AnimesMalId = @() },
        [pscustomobject]@{ Id = 4; Titulo = 'Existing'; AnimesMalId = @() },
        [pscustomobject]@{ Id = 5; Titulo = 'TwoFolders'; AnimesMalId = @() }
    )
    $folders = @('Alpha Title', 'Duplicate', 'Existing', 'Unknown', 'TwoFolders') | ForEach-Object {
        [pscustomobject]@{ RootKey = 'E_A'; RelativePath = $_ }
    }
    $folders += [pscustomobject]@{ RootKey = 'X_A'; RelativePath = 'TwoFolders' }
    $existing = @([pscustomobject]@{ MyAnimeId = 4; RootKey = 'E_A'; RelativePath = 'Existing' })
    $plan = @(New-AssociationPlan $folders $catalog $existing)
    Assert-Test (@($plan | Where-Object Status -eq 'Ready').Count -eq 1) 'Only one unambiguous match'
    Assert-Test (@($plan | Where-Object Status -eq 'AmbiguousTitle').Count -eq 1) 'Duplicate titles rejected'
    Assert-Test (@($plan | Where-Object Status -eq 'MultipleFolders').Count -eq 2) 'Multiple folders rejected'
    Assert-Test (@($plan | Where-Object Status -eq 'Existing').Count -eq 1) 'Existing binding preserved'
    Assert-Test (@($plan | Where-Object Status -eq 'MissingCatalog').Count -eq 1) 'Unmatched folder not assigned'
    $existing += [pscustomobject]@{ MyAnimeId = 1; RootKey = 'E_A'; RelativePath = 'Alpha Title' }
    $second = @(New-AssociationPlan $folders $catalog $existing)
    Assert-Test (@($second | Where-Object Status -eq 'Ready').Count -eq 0) 'Second run is idempotent'
    Assert-Test ((ConvertTo-CollectionFolderName '  Alpha:  Title. ') -eq 'Alpha Title') 'Folder sanitization matches creator'
    Assert-Test ((ConvertTo-CollectionFolderName 'CON') -eq '_CON') 'Reserved Windows name'
    $idCatalog = @(
        [pscustomobject]@{ Id = 10; Titulo = 'Long title'; AnimesMalId = [int[]](ConvertFrom-Json -InputObject '[11,12]') },
        [pscustomobject]@{ Id = 11; Titulo = 'Long title'; AnimesMalId = @(11,13) }
    )
    $idFolder = @([pscustomobject]@{ RootKey = 'E_A'; RelativePath = 'Abbreviated'; CoverIds = @(11,12) })
    $idPlan = @(New-AssociationPlan $idFolder $idCatalog @())
    Assert-Test ($idPlan[0].Status -eq 'Ready' -and $idPlan[0].MyAnimeId -eq 10) 'All cover IDs disambiguate the internal collection'
    $idFolder[0].CoverIds = @(11)
    $idPlan = @(New-AssociationPlan $idFolder $idCatalog @())
    Assert-Test ($idPlan[0].Status -eq 'AmbiguousTitle') 'Shared cover ID never chooses arbitrary collection'
    $idFolder[0].CoverIds = @(12,13)
    $idPlan = @(New-AssociationPlan $idFolder $idCatalog @())
    Assert-Test ($idPlan[0].Status -eq 'AmbiguousTitle') 'Conflicting IDs remain unresolved without arbitrary selection'
    $prefixCatalog = @(
        [pscustomobject]@{ Id = 20; Titulo = 'Alpha Beta Gamma Delta Collection'; AnimesMalId = @(20,21,22) },
        [pscustomobject]@{ Id = 21; Titulo = 'Alpha Beta Other Collection'; AnimesMalId = @(20) }
    )
    $prefixFolder = @([pscustomobject]@{ RootKey = 'E_A'; RelativePath = 'Alpha Beta Gamma Delta Cutoff'; CoverIds = @(20,21,22) })
    $prefixPlan = @(New-AssociationPlan $prefixFolder $prefixCatalog @())
    Assert-Test ($prefixPlan[0].Status -eq 'Ready' -and $prefixPlan[0].MyAnimeId -eq 20) 'Image IDs prioritize truncated folder names'
    Assert-Test (Test-FirstFourWords 'Gamma Ray Fighters 2020 TV' 'Gamma Ray Fighters') 'First four words tolerate different word counts'
    Write-Output '20 association self-tests passed. No database or collection access.'
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
if ($reportPath -notmatch '^[A-D,F-G,I-Z]:\\' -or $reportPath -match '^[EH]:\\') { throw 'Report directory must be local and outside E: and H:.' }
$ancestor = [IO.DirectoryInfo]::new($reportPath)
while (-not $ancestor.Exists) { $ancestor = $ancestor.Parent }
Assert-OrdinaryDirectory $ancestor.FullName

$roots = @(New-AuthorizedRoots)
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
            $coverIds = @(Get-CollectionCoverIds $folder.FullName $root.Key $folder.Name $issues)
            $folders.Add([pscustomobject]@{ RootKey = $root.Key; RelativePath = $folder.Name; CoverIds = $coverIds })
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
    $existingBefore = @(Read-SqlRows $connection $null 'SELECT MyAnimeId, RootKey, RelativePath FROM dbo.MyAnimeMonitoringLocations')
    $existing = @(Convert-ExistingLocations $existingBefore $roots)
    $rootRelocations = @(Get-RootRelocations $existing)
    if ($Apply) {
        if ($issues.Count -gt 0) { throw 'Apply refused: incomplete discovery. Run preview and resolve directory errors first.' }
        $transaction = $connection.BeginTransaction([Data.IsolationLevel]::Serializable)
    }
    $catalog = @(Read-SqlRows $connection $transaction 'SELECT Id, Titulo, AnimesMalId FROM dbo.MyAnimes')
    foreach ($collection in $catalog) { $collection.AnimesMalId = [int[]](ConvertFrom-Json -InputObject ([string]$collection.AnimesMalId)) }
    $existingBefore = @(Read-SqlRows $connection $transaction 'SELECT MyAnimeId, RootKey, RelativePath FROM dbo.MyAnimeMonitoringLocations')
    $existing = @(Convert-ExistingLocations $existingBefore $roots)
    $rootRelocations = @(Get-RootRelocations $existing)
    if ($Apply) {
        foreach ($relocation in $rootRelocations) {
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandText = 'UPDATE dbo.MyAnimeMonitoringLocations SET RootKey = @NewRoot WHERE MyAnimeId = @Id AND RootKey = @OldRoot AND RelativePath = @Path'
            [void]$command.Parameters.Add('@Id', [Data.SqlDbType]::Int)
            [void]$command.Parameters.Add('@OldRoot', [Data.SqlDbType]::NVarChar, 100)
            [void]$command.Parameters.Add('@NewRoot', [Data.SqlDbType]::NVarChar, 100)
            [void]$command.Parameters.Add('@Path', [Data.SqlDbType]::NVarChar, -1)
            $command.Parameters['@Id'].Value = $relocation.MyAnimeId
            $command.Parameters['@OldRoot'].Value = $relocation.PreviousRootKey
            $command.Parameters['@NewRoot'].Value = $relocation.RootKey
            $command.Parameters['@Path'].Value = $relocation.RelativePath
            try {
                if ($command.ExecuteNonQuery() -ne 1) { throw "Root-key relocation verification failed for MyAnimeId $($relocation.MyAnimeId)." }
            }
            finally { $command.Dispose() }
        }
        $relocatedRows = @(Read-SqlRows $connection $transaction 'SELECT MyAnimeId, RootKey, RelativePath FROM dbo.MyAnimeMonitoringLocations')
        $existing = @(Convert-ExistingLocations $relocatedRows $roots)
    }
    $plan = @(New-AssociationPlan $folders.ToArray() $catalog $existing)
    $ready = @($plan | Where-Object Status -eq 'Ready')
    [void][IO.Directory]::CreateDirectory($reportPath)
    $report = [ordered]@{
        RunId = $runId; ObservedAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); Mode = $(if ($Apply) { 'Apply' } else { 'Preview' })
        Database = $Database; AuthorizedRoots = $roots.Count; CollectionFolders = $folders.Count
        CollectionsInDatabase = $catalog.Count; ExistingBefore = $existingBefore; RootKeyRelocations = $rootRelocations
        ExistingAfterRootMigration = $existing; Plan = $plan; DiscoveryIssues = $issues.ToArray()
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
        if (@($verified | Where-Object { (Convert-LegacyRootKey ([string]$_.RootKey)) -cne [string]$_.RootKey }).Count -gt 0) {
            throw 'Legacy root keys remain after relocation; transaction will roll back.'
        }
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
        Relocated = $rootRelocations.Count; Ready = $ready.Count; Inserted = $(if ($committed) { $ready.Count } else { 0 }); Committed = $committed
        StatusCounts = @($plan | Group-Object Status | Select-Object Name, Count); DiscoveryIssues = $issues.Count
        Report = $reportFile
    } | ConvertTo-Json -Depth 6
}
finally {
    if ($null -ne $transaction) { if (-not $committed) { $transaction.Rollback() }; $transaction.Dispose() }
    $connection.Dispose()
}
