[CmdletBinding()]
param(
    [ValidateSet(5, 7)]
    [int]$ExpectedMajor = 7
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSVersion.Major -ne $ExpectedMajor) {
    throw "Expected PowerShell $ExpectedMajor, running $($PSVersionTable.PSVersion). Use the PowerShell 7 terminal or invoke pwsh -NoProfile -File scripts/Test-DtudoPowerShell.ps1."
}

$scripts = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1' -File)
$syntaxErrors = [Collections.Generic.List[string]]::new()
foreach ($script in $scripts) {
    $tokens = $null
    $parseErrors = $null
    [void][Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$parseErrors)
    foreach ($parseError in $parseErrors) {
        $syntaxErrors.Add("$($script.Name):$($parseError.Extent.StartLineNumber): $($parseError.Message)")
    }
}
if ($syntaxErrors.Count -gt 0) { throw ($syntaxErrors -join [Environment]::NewLine) }

$builder = [Data.SqlClient.SqlConnectionStringBuilder]::new()
$builder['Data Source'] = '(localdb)\MSSQLLocalDB'
$builder['Initial Catalog'] = 'TestOnly_NoConnectionOpened'
$builder['Integrated Security'] = $true
if ($builder['Data Source'] -ne '(localdb)\MSSQLLocalDB') { throw 'SQL connection builder regression.' }

foreach ($sample in @(
    [pscustomobject]@{ Json = '[]'; Count = 0 },
    [pscustomobject]@{ Json = '[42]'; Count = 1 },
    [pscustomobject]@{ Json = '[11,12]'; Count = 2 }
)) {
    [int[]]$ids = @(ConvertFrom-Json -InputObject $sample.Json | ForEach-Object { $_ })
    if ($ids.Count -ne $sample.Count) { throw "JSON array regression for $($sample.Json)." }
}

$unicode = ConvertFrom-Json -InputObject '{"title":"Cole\u00e7\u00e3o"}'
$serialized = $unicode | ConvertTo-Json -Compress
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$restored = ConvertFrom-Json -InputObject $utf8.GetString($utf8.GetBytes($serialized))
if ($restored.title -cne $unicode.title) { throw 'UTF-8 round-trip regression.' }

& (Join-Path $PSScriptRoot 'Sync-MyAnimeMonitoringLocations.ps1') -SelfTest

[pscustomobject]@{
    Version = $PSVersionTable.PSVersion.ToString()
    Edition = $PSVersionTable.PSEdition
    ScriptsParsed = $scripts.Count
    SyntaxErrors = $syntaxErrors.Count
    SqlBuilder = 'Passed; no connection opened'
    JsonArrays = 'Passed: empty, single and multiple IDs'
    Utf8 = 'Passed'
    CollectionsAccessed = $false
} | ConvertTo-Json
