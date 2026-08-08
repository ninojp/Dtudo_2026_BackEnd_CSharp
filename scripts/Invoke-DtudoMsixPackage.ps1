[CmdletBinding()]
param(
    [ValidateSet('Prepare', 'Package', 'Validate')]
    [string]$Mode = 'Prepare',

    [string]$ProjectPath,
    [string]$PayloadPath,
    [string]$PackagePath,
    [string]$OutputRoot = (Join-Path ([IO.Path]::GetTempPath()) 'Dtudo2026\Etapa29'),
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Publisher = 'CN=Dtudo Internal',
    [ValidateSet('CurrentUser', 'LocalMachine')]
    [string]$CertificateStoreLocation = 'CurrentUser',
    [ValidateSet('My')]
    [string]$CertificateStoreName = 'My',
    [string]$CertificateThumbprint,
    [string]$MakeAppxPath,
    [string]$SignToolPath,
    [switch]$Sign,
    [switch]$RequireSignature,
    [string]$ExpectedSha256,
    [switch]$Force,
    [switch]$Json
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($Path))
}

function Assert-Version {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        throw 'A versao MSIX deve conter quatro componentes numericos.'
    }

    foreach ($component in $Value.Split('.')) {
        $number = 0L
        if (-not [long]::TryParse($component, [ref]$number) -or $number -gt 65535L) {
            throw 'Cada componente da versao MSIX deve estar entre 0 e 65535.'
        }
    }
}

function Assert-RequiredPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description nao encontrado: $Path"
    }
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Resolve-Tool {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [string]$ExplicitPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        Assert-RequiredPath -Path $ExplicitPath -Description $Name
        return (Resolve-FullPath -Path $ExplicitPath)
    }

    $command = Get-Command -Name $Name -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $windowsKitsRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    if (Test-Path -LiteralPath $windowsKitsRoot) {
        $candidate = Get-ChildItem -LiteralPath $windowsKitsRoot -Recurse -Filter $Name -File -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($null -ne $candidate) {
            return $candidate.FullName
        }
    }

    throw "$Name nao esta instalado. Instale o Windows SDK no ambiente de empacotamento protegido."
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    [IO.File]::WriteAllText($Path, $Content, (New-Object Text.UTF8Encoding($false)))
}

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

function Write-HashSidecar {
    param([Parameter(Mandatory = $true)][string]$Path)

    $hashPath = "$Path.sha256"
    Write-Utf8File -Path $hashPath -Content "$(Get-Sha256 -Path $Path)  $(Split-Path -Leaf $Path)"
    return $hashPath
}

function Normalize-Hash {
    param([Parameter(Mandatory = $true)][string]$Value)

    $normalized = ($Value -replace '[^A-Fa-f0-9]', '').ToUpperInvariant()
    if ($normalized -notmatch '^[A-F0-9]{64}$') {
        throw 'Hash SHA-256 invalido.'
    }
    return $normalized
}

function Get-ExpectedHash {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$ExplicitHash
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitHash)) {
        return Normalize-Hash -Value $ExplicitHash
    }

    $sidecarPath = "$Path.sha256"
    if (-not (Test-Path -LiteralPath $sidecarPath)) {
        return $null
    }

    $sidecarContent = Get-Content -LiteralPath $sidecarPath -Raw
    $match = [regex]::Match($sidecarContent, '(?im)^\s*([A-Fa-f0-9]{64})(?:\s|$)')
    if (-not $match.Success) {
        throw 'Manifesto de hash invalido.'
    }
    return Normalize-Hash -Value $match.Groups[1].Value
}

function Get-SigningCertificate {
    param(
        [Parameter(Mandatory = $true)][string]$Thumbprint,
        [Parameter(Mandatory = $true)][string]$StoreLocation,
        [Parameter(Mandatory = $true)][string]$StoreName,
        [Parameter(Mandatory = $true)][string]$ExpectedPublisher
    )

    $normalizedThumbprint = ($Thumbprint -replace '\s', '').ToUpperInvariant()
    if ($normalizedThumbprint -notmatch '^[A-F0-9]{40}$') {
        throw 'Thumbprint de assinatura invalido.'
    }

    $location = [System.Security.Cryptography.X509Certificates.StoreLocation]::$StoreLocation
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($StoreName, $location)
    try {
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
        $certificate = $store.Certificates |
            Where-Object { ($_.Thumbprint -replace '\s', '').ToUpperInvariant() -eq $normalizedThumbprint } |
            Select-Object -First 1
    } finally {
        $store.Close()
    }

    if ($null -eq $certificate) {
        throw 'Certificado de assinatura nao encontrado no Certificate Store indicado.'
    }
    if (-not $certificate.HasPrivateKey) {
        throw 'O certificado de assinatura nao possui chave privada acessivel ao processo.'
    }
    if ($certificate.NotAfter.ToUniversalTime() -le [DateTime]::UtcNow) {
        throw 'O certificado de assinatura esta expirado.'
    }
    if ($certificate.Subject -ne $ExpectedPublisher) {
        throw 'O Subject do certificado nao coincide com o Publisher do manifest.'
    }

    $hasCodeSigningEku = @($certificate.Extensions | Where-Object { $_.Oid.Value -eq '1.3.6.1.5.5.7.3.3' }).Count -gt 0
    if (-not $hasCodeSigningEku) {
        throw 'O certificado de assinatura nao possui EKU de Code Signing.'
    }

    return $certificate
}

function New-Manifest {
    param(
        [Parameter(Mandatory = $true)][string]$PayloadRoot,
        [Parameter(Mandatory = $true)][string]$VersionValue,
        [Parameter(Mandatory = $true)][string]$PublisherValue
    )

    $templatePath = Join-Path $PSScriptRoot '..\WinAppDtudo\Package.appxmanifest.template'
    Assert-RequiredPath -Path $templatePath -Description 'Template do manifest'
    $template = Get-Content -LiteralPath $templatePath -Raw
    $escapedPublisher = [System.Security.SecurityElement]::Escape($PublisherValue)
    $manifest = $template.Replace('{{PACKAGE_VERSION}}', $VersionValue).Replace('{{PACKAGE_PUBLISHER}}', $escapedPublisher)
    Write-Utf8File -Path (Join-Path $PayloadRoot 'AppxManifest.xml') -Content $manifest
}

function Prepare-Payload {
    param(
        [Parameter(Mandatory = $true)][string]$Project,
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$VersionValue,
        [Parameter(Mandatory = $true)][bool]$Overwrite
    )

    $rootPath = Resolve-FullPath -Path $Root
    if ((Test-Path -LiteralPath $rootPath) -and -not $Overwrite) {
        throw "A pasta de saida ja existe. Use -Force somente para a propria area temporaria: $rootPath"
    }
    Ensure-Directory -Path $rootPath

    $publishRoot = Join-Path $rootPath 'payload'
    if (Test-Path -LiteralPath $publishRoot) {
        Remove-Item -LiteralPath $publishRoot -Recurse -Force
    }
    Ensure-Directory -Path $publishRoot
    $dotnet = Resolve-Tool -Name 'dotnet.exe'
    $arguments = @(
        'publish',
        (Resolve-FullPath -Path $Project),
        '--configuration', 'Release',
        '--runtime', 'win-x64',
        '--self-contained', 'true',
        '--output', $publishRoot,
        '--nologo',
        ('-p:Version=' + $VersionValue),
        ('-p:AssemblyVersion=' + $VersionValue),
        ('-p:FileVersion=' + $VersionValue),
        ('-p:InformationalVersion=' + $VersionValue)
    )
    & $dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish falhou com codigo $LASTEXITCODE."
    }

    $executablePath = Join-Path $publishRoot 'WinAppDtudo.exe'
    Assert-RequiredPath -Path $executablePath -Description 'Executavel publicado'
    $assetSource = Join-Path $PSScriptRoot '..\WinAppDtudo\Resources\YingYang_HD.png'
    Assert-RequiredPath -Path $assetSource -Description 'Logo do WinApp'
    $assetRoot = Join-Path $publishRoot 'Assets'
    Ensure-Directory -Path $assetRoot
    foreach ($assetName in @('StoreLogo.png', 'Square150x150Logo.png', 'Square44x44Logo.png')) {
        Copy-Item -LiteralPath $assetSource -Destination (Join-Path $assetRoot $assetName) -Force
    }
    New-Manifest -PayloadRoot $publishRoot -VersionValue $VersionValue -PublisherValue $Publisher

    $zipPath = Join-Path $rootPath ('WinAppDtudo-input-' + $VersionValue + '.zip')
    Compress-Archive -Path (Join-Path $publishRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal -Force
    $hashPath = Write-HashSidecar -Path $zipPath
    return [pscustomobject]@{
        Mode = 'Prepare'
        Version = $VersionValue
        PayloadRoot = $publishRoot
        PayloadArchive = $zipPath
        PayloadHash = Get-Sha256 -Path $zipPath
        HashFile = $hashPath
    }
}

function Expand-Payload {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $sourcePath = Resolve-FullPath -Path $Source
    Assert-RequiredPath -Path $sourcePath -Description 'Payload de empacotamento'
    $expandedRoot = Join-Path (Resolve-FullPath -Path $Root) 'expanded-payload'
    if (Test-Path -LiteralPath $expandedRoot) {
        Remove-Item -LiteralPath $expandedRoot -Recurse -Force
    }
    Ensure-Directory -Path $expandedRoot
    if ([IO.Path]::GetExtension($sourcePath).Equals('.zip', [StringComparison]::OrdinalIgnoreCase)) {
        Expand-Archive -LiteralPath $sourcePath -DestinationPath $expandedRoot -Force
    } else {
        Copy-Item -Path (Join-Path $sourcePath '*') -Destination $expandedRoot -Recurse -Force
    }
    return $expandedRoot
}

function Invoke-MakeAppx {
    param(
        [Parameter(Mandatory = $true)][string]$PayloadRoot,
        [Parameter(Mandatory = $true)][string]$Destination,
        [string]$ToolPath
    )

    $makeAppx = Resolve-Tool -Name 'makeappx.exe' -ExplicitPath $ToolPath
    & $makeAppx @('pack', '/d', $PayloadRoot, '/p', $Destination, '/o')
    if ($LASTEXITCODE -ne 0) {
        throw "makeappx falhou com codigo $LASTEXITCODE."
    }
}

function Invoke-SignPackage {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Thumbprint,
        [Parameter(Mandatory = $true)][string]$StoreLocation,
        [Parameter(Mandatory = $true)][string]$StoreName,
        [string]$ToolPath
    )

    $null = Get-SigningCertificate -Thumbprint $Thumbprint -StoreLocation $StoreLocation -StoreName $StoreName -ExpectedPublisher $Publisher
    $signTool = Resolve-Tool -Name 'signtool.exe' -ExplicitPath $ToolPath
    $arguments = @('sign', '/fd', 'SHA256', '/sha1', (($Thumbprint -replace '\s', '').ToUpperInvariant()))
    if ($StoreLocation -eq 'LocalMachine') {
        $arguments += '/sm'
    }
    $arguments += @('/s', $StoreName, $Path)
    & $signTool @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "signtool sign falhou com codigo $LASTEXITCODE."
    }
}

function Get-PackageManifest {
    param([Parameter(Mandatory = $true)][string]$Path)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.GetEntry('AppxManifest.xml')
        if ($null -eq $entry) {
            throw 'O pacote nao contem AppxManifest.xml.'
        }
        $stream = $entry.Open()
        $reader = New-Object IO.StreamReader($stream, (New-Object Text.UTF8Encoding($false)), $true)
        try {
            return [xml]$reader.ReadToEnd()
        } finally {
            $reader.Dispose()
            $stream.Dispose()
        }
    } finally {
        $archive.Dispose()
    }
}

function Validate-Package {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$VersionValue,
        [string]$ExpectedHash,
        [Parameter(Mandatory = $true)][bool]$CheckSignature,
        [string]$ToolPath
    )

    $packagePath = Resolve-FullPath -Path $Path
    Assert-RequiredPath -Path $packagePath -Description 'Pacote MSIX'
    $expected = Get-ExpectedHash -Path $packagePath -ExplicitHash $ExpectedHash
    if ($null -ne $expected -and (Get-Sha256 -Path $packagePath) -ne $expected) {
        throw 'O hash SHA-256 do pacote nao corresponde ao manifesto de hash.'
    }

    $manifest = Get-PackageManifest -Path $packagePath
    $namespace = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
    $namespace.AddNamespace('pkg', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $identity = $manifest.SelectSingleNode('/pkg:Package/pkg:Identity', $namespace)
    if ($null -eq $identity) {
        throw 'Identity ausente no manifest MSIX.'
    }
    if (-not [string]::IsNullOrWhiteSpace($VersionValue) -and $identity.Version -ne $VersionValue) {
        throw 'A versao do pacote nao corresponde a versao esperada.'
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        foreach ($entryName in @('WinAppDtudo.exe', 'Assets\StoreLogo.png', 'Assets\Square150x150Logo.png', 'Assets\Square44x44Logo.png')) {
            if ($null -eq $archive.GetEntry($entryName)) {
                throw "Arquivo obrigatorio ausente no pacote: $entryName"
            }
        }
    } finally {
        $archive.Dispose()
    }

    if ($CheckSignature) {
        $signTool = Resolve-Tool -Name 'signtool.exe' -ExplicitPath $ToolPath
        & $signTool @('verify', '/pa', '/all', $packagePath) *> $null
        if ($LASTEXITCODE -ne 0) {
            throw 'A assinatura Authenticode do pacote foi rejeitada.'
        }
    }

    return [pscustomobject]@{
        Mode = 'Validate'
        Name = [string]$identity.Name
        PackagePath = $packagePath
        Version = [string]$identity.Version
        Publisher = [string]$identity.Publisher
        Sha256 = Get-Sha256 -Path $packagePath
        SignatureChecked = $CheckSignature
    }
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $PSScriptRoot '..\WinAppDtudo\WinAppDtudo.csproj'
}

Assert-Version -Value $Version

switch ($Mode) {
    'Prepare' {
        $result = Prepare-Payload -Project $ProjectPath -Root $OutputRoot -VersionValue $Version -Overwrite $Force.IsPresent
    }
    'Package' {
        if ([string]::IsNullOrWhiteSpace($PayloadPath)) {
            throw '-PayloadPath e obrigatorio no modo Package.'
        }
        if ($Sign -and [string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
            throw 'A assinatura exige -CertificateThumbprint; nenhuma chave privada fica no repositorio.'
        }
        $rootPath = Resolve-FullPath -Path $OutputRoot
        Ensure-Directory -Path $rootPath
        $expandedRoot = Expand-Payload -Source $PayloadPath -Root $rootPath
        New-Manifest -PayloadRoot $expandedRoot -VersionValue $Version -PublisherValue $Publisher
        $destination = if ([string]::IsNullOrWhiteSpace($PackagePath)) {
            Join-Path $rootPath ('WinAppDtudo-' + $Version + '.msix')
        } else {
            Resolve-FullPath -Path $PackagePath
        }
        Ensure-Directory -Path (Split-Path -Parent $destination)
        Invoke-MakeAppx -PayloadRoot $expandedRoot -Destination $destination -ToolPath $MakeAppxPath
        if ($Sign) {
            Invoke-SignPackage -Path $destination -Thumbprint $CertificateThumbprint -StoreLocation $CertificateStoreLocation -StoreName $CertificateStoreName -ToolPath $SignToolPath
        }
        $hashPath = Write-HashSidecar -Path $destination
        $result = Validate-Package -Path $destination -VersionValue $Version -ExpectedHash (Get-Sha256 -Path $destination) -CheckSignature $Sign.IsPresent -ToolPath $SignToolPath
        $result | Add-Member -NotePropertyName HashFile -NotePropertyValue $hashPath
    }
    'Validate' {
        $result = Validate-Package -Path $PackagePath -VersionValue $Version -ExpectedHash $ExpectedSha256 -CheckSignature $RequireSignature.IsPresent -ToolPath $SignToolPath
    }
}

if ($Json) {
    $result | ConvertTo-Json -Depth 4 -Compress
} else {
    $result
}
