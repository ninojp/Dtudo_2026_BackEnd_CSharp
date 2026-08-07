[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidateSet('Validate', 'Apply', 'Rollback')]
    [string]$Mode = 'Validate',

    [string]$BaselinePath,

    [ValidateSet('Development', 'Homologation', 'Production')]
    [string[]]$Environment = @('Development'),

    [string]$StateRoot,

    [switch]$RunNegativeTests,
    [switch]$ConfigureSql,
    [switch]$ConfigureIis,
    [switch]$EnableTlsRegistryChanges,
    [switch]$RollbackSql,
    [switch]$ConfigureCertificateAcl,
    [string]$CertificateThumbprint,
    [ValidateSet('My')]
    [string]$CertificateStoreName = 'My',
    [ValidateSet('CurrentUser', 'LocalMachine')]
    [string]$CertificateStoreLocation = 'CurrentUser',
    [string]$CertificatePrincipal,
    [switch]$Json,
    [switch]$FailOnBlocked
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$script:Results = New-Object 'System.Collections.Generic.List[object]'

if ([string]::IsNullOrWhiteSpace($BaselinePath)) {
    $BaselinePath = Join-Path $PSScriptRoot 'DtudoInfrastructureBaseline.psd1'
}

function Add-Result {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Check,
        [Parameter(Mandatory = $true)]
        [ValidateSet('Passed', 'Blocked', 'Warning', 'Failed', 'NotChecked')]
        [string]$Status,
        [Parameter(Mandatory = $true)]
        [string]$Detail
    )

    $null = $script:Results.Add([pscustomobject]@{
            Check = $Check
            Status = $Status
            Detail = $Detail
        })
}

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $expandedPath = [Environment]::ExpandEnvironmentVariables($Path)
    return [System.IO.Path]::GetFullPath($expandedPath).TrimEnd([char[]]@('\', '/'))
}

function Test-SameOrWithinPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $normalizedRoot = Resolve-FullPath -Path $Root
    $normalizedPath = Resolve-FullPath -Path $Path
    return $normalizedRoot.Equals($normalizedPath, [System.StringComparison]::OrdinalIgnoreCase) -or
        $normalizedPath.StartsWith($normalizedRoot + '\', [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-PathWithinRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-SameOrWithinPath -Root $Root -Path $Path)) {
        throw 'O caminho nao esta dentro da raiz esperada.'
    }
}

function Assert-UniqueValues {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Values,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    $normalized = @($Values | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $uniqueNormalized = @($normalized | Sort-Object -Unique)
    if ($uniqueNormalized.Count -ne $normalized.Count) {
        throw "Valores duplicados em $Description."
    }
}

function Assert-Baseline {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Baseline,
        [Parameter(Mandatory = $true)]
        [hashtable[]]$Environments
    )

    if ($Baseline.SchemaVersion -ne 1) {
        throw 'Versao de schema da baseline nao suportada.'
    }
    if (@($Environments).Count -ne 3) {
        throw 'A baseline deve conter Development, Homologation e Production.'
    }

    Assert-UniqueValues -Values @($Environments | ForEach-Object { $_.Name }) -Description 'ambientes'
    $databaseNames = @($Environments | ForEach-Object { $_.Sql.MyAnimesDatabase; $_.Sql.IdentityDatabase })
    Assert-UniqueValues -Values $databaseNames -Description 'bancos'

    $ports = New-Object 'System.Collections.Generic.List[object]'
    $protectedRoots = New-Object 'System.Collections.Generic.List[string]'
    foreach ($currentEnvironment in $Environments) {
        foreach ($requiredProperty in @('Provisioning', 'Root', 'ApplicationRoot', 'DataRoot', 'SecretsRoot', 'BackupRoot', 'Network', 'Sql', 'Accounts', 'ServiceCertificates', 'DatabaseAccess', 'Iis')) {
            if (-not $currentEnvironment.ContainsKey($requiredProperty)) {
                throw "Propriedade obrigatoria ausente no ambiente $($currentEnvironment.Name): $requiredProperty."
            }
        }

        foreach ($requiredProvisioningProperty in @('Mode', 'RequiresAdministrator', 'RequiresServiceAccounts', 'RequiresIis', 'RequiresBitLocker')) {
            if (-not $currentEnvironment.Provisioning.ContainsKey($requiredProvisioningProperty)) {
                throw "Propriedade de provisionamento ausente no ambiente $($currentEnvironment.Name): $requiredProvisioningProperty."
            }
        }
        if ($currentEnvironment.Provisioning.Mode -notin @('Workstation', 'Server')) {
            throw "Modo de provisionamento invalido no ambiente $($currentEnvironment.Name)."
        }
        if (-not $currentEnvironment.Network.ContainsKey('ConfigureFirewall') -or
            -not $currentEnvironment.Iis.ContainsKey('Enabled') -or
            -not $currentEnvironment.Sql.ContainsKey('RequiredDatabases')) {
            throw "Baseline incompleta para o ambiente $($currentEnvironment.Name)."
        }

        foreach ($rootProperty in @('Root', 'ApplicationRoot', 'BackupRoot')) {
            $null = $protectedRoots.Add((Resolve-FullPath -Path $currentEnvironment[$rootProperty]))
        }

        $null = $ports.Add($currentEnvironment.Network.GatewayHttpsPort)
        foreach ($port in @($currentEnvironment.Network.InternalPorts)) {
            $null = $ports.Add($port)
        }
        if (@($currentEnvironment.Network.PublicPorts | Where-Object { $_ -in @($currentEnvironment.Network.InternalPorts) }).Count -gt 0) {
            throw "Porta interna publicada no ambiente $($currentEnvironment.Name)."
        }
        if ($currentEnvironment.Network.Exposure -ne 'PublicGatewayOnly' -and @($currentEnvironment.Network.PublicPorts).Count -gt 0) {
            throw "Ambiente nao produtivo possui porta publica: $($currentEnvironment.Name)."
        }
        if ($currentEnvironment.Network.Exposure -eq 'PublicGatewayOnly') {
            $publicPorts = @($currentEnvironment.Network.PublicPorts)
            if ($publicPorts.Count -ne 1 -or [int]$publicPorts[0] -ne [int]$currentEnvironment.Network.GatewayHttpsPort) {
                throw "A exposicao publica do ambiente $($currentEnvironment.Name) deve ser somente o gateway."
            }
        }
        $serviceClientIds = @($currentEnvironment.ServiceCertificates | ForEach-Object { $_.ClientId })
        Assert-UniqueValues -Values $serviceClientIds -Description "client IDs de servico do ambiente $($currentEnvironment.Name)"
        foreach ($certificateBinding in @($currentEnvironment.ServiceCertificates)) {
            foreach ($requiredCertificateProperty in @('ClientId', 'ServiceRole', 'StoreName', 'StoreLocation', 'PrivateKeyPrincipal', 'CertificateThumbprint', 'PreviousCertificateThumbprint', 'PreviousCertificateAcceptedUntilUtc', 'AllowedScopes', 'AllowedAudiences')) {
                if (-not $certificateBinding.ContainsKey($requiredCertificateProperty)) {
                    throw "Binding de certificado incompleto no ambiente $($currentEnvironment.Name): $requiredCertificateProperty."
                }
            }
            if ($certificateBinding.StoreName -ne 'My' -or $certificateBinding.StoreLocation -notin @('CurrentUser', 'LocalMachine')) {
                throw "Store de certificado invalido no ambiente $($currentEnvironment.Name)."
            }
            foreach ($thumbprint in @($certificateBinding.CertificateThumbprint, $certificateBinding.PreviousCertificateThumbprint)) {
                if (-not [string]::IsNullOrWhiteSpace($thumbprint) -and $thumbprint -notmatch '^[A-Fa-f0-9]{40}$') {
                    throw "Thumbprint de certificado invalido no ambiente $($currentEnvironment.Name)."
                }
            }
            if ([string]::IsNullOrWhiteSpace($certificateBinding.PrivateKeyPrincipal)) {
                throw "Principal ACL ausente no ambiente $($currentEnvironment.Name)."
            }
            if (@($certificateBinding.AllowedScopes).Count -eq 0 -or @($certificateBinding.AllowedAudiences).Count -eq 0) {
                throw "Scopes/audiences ausentes no binding de certificado do ambiente $($currentEnvironment.Name)."
            }
            foreach ($audience in @($certificateBinding.AllowedAudiences)) {
                $parsedAudience = $null
                if (-not [Uri]::TryCreate([string]$audience, [UriKind]::Absolute, [ref]$parsedAudience)) {
                    throw "Audience de certificado invalido no ambiente $($currentEnvironment.Name)."
                }
            }
        }
        if ($currentEnvironment.Sql.TcpEnabled) {
            throw "TCP do SQL deve permanecer desabilitado na baseline: $($currentEnvironment.Name)."
        }
        foreach ($databaseProperty in @('MyAnimesDatabase', 'IdentityDatabase')) {
            if ($currentEnvironment.Sql[$databaseProperty] -notmatch '^[A-Za-z_][A-Za-z0-9_]{0,127}$') {
                throw "Nome de banco invalido em $($currentEnvironment.Name)."
            }
        }

        $roles = @($currentEnvironment.Accounts | ForEach-Object { $_.Role })
        Assert-UniqueValues -Values $roles -Description "contas do ambiente $($currentEnvironment.Name)"
        foreach ($account in @($currentEnvironment.Accounts)) {
            if ($currentEnvironment.Provisioning.Mode -eq 'Workstation' -and $account.SqlPrincipal -eq 'CURRENT_USER') {
                continue
            }
            if ($account.LocalName -notmatch '^[A-Za-z][A-Za-z0-9_-]{2,19}$') {
                throw "Nome de conta Windows invalido no ambiente $($currentEnvironment.Name)."
            }
            if ([string]::IsNullOrWhiteSpace($account.SqlPrincipal)) {
                throw "Principal SQL ausente no ambiente $($currentEnvironment.Name)."
            }
        }
    }

    Assert-UniqueValues -Values $ports.ToArray() -Description 'portas dos ambientes'
    for ($leftIndex = 0; $leftIndex -lt $protectedRoots.Count; $leftIndex++) {
        for ($rightIndex = $leftIndex + 1; $rightIndex -lt $protectedRoots.Count; $rightIndex++) {
            if ((Test-SameOrWithinPath -Root $protectedRoots[$leftIndex] -Path $protectedRoots[$rightIndex]) -or
                (Test-SameOrWithinPath -Root $protectedRoots[$rightIndex] -Path $protectedRoots[$leftIndex])) {
                throw 'Raizes de ambientes diferentes se sobrepoem.'
            }
        }
    }

    $baselineText = Get-Content -LiteralPath $BaselinePath -Raw
    if ($baselineText -match '(?im)(password|token|clientsecret|apikey)\s*=') {
        throw 'A baseline contem uma chave de segredo proibida.'
    }
}

function Get-SelectedEnvironments {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable[]]$AllEnvironments,
        [Parameter(Mandatory = $true)]
        [string[]]$Names
    )

    $selected = New-Object 'System.Collections.Generic.List[hashtable]'
    foreach ($name in $Names) {
        $match = @($AllEnvironments | Where-Object { $_.Name -eq $name })
        if ($match.Count -ne 1) {
            throw "Ambiente nao encontrado ou duplicado: $name."
        }
        $null = $selected.Add($match[0])
    }
    return $selected.ToArray()
}

function Test-IsAdministrator {
    return ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-Administrator {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable[]]$Environments
    )

    $requiresAdministrator = @($Environments | Where-Object { $_.Provisioning.RequiresAdministrator }).Count -gt 0
    if (-not $requiresAdministrator) {
        return
    }
    if (-not (Test-IsAdministrator)) {
        throw 'Apply/Rollback exigem uma sessao elevada de administrador; nenhuma alteracao foi aplicada.'
    }
}

function Resolve-AccountPrincipal {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Principal
    )

    if ($Principal.Equals('CURRENT_USER', [System.StringComparison]::OrdinalIgnoreCase)) {
        return [Security.Principal.WindowsIdentity]::GetCurrent().Name
    }
    if ($Principal.StartsWith('SID:', [System.StringComparison]::OrdinalIgnoreCase)) {
        $sid = New-Object System.Security.Principal.SecurityIdentifier($Principal.Substring(4))
        return $sid.Translate([System.Security.Principal.NTAccount]).Value
    }
    if ($Principal.StartsWith('.\', [System.StringComparison]::OrdinalIgnoreCase)) {
        return "$env:COMPUTERNAME\$($Principal.Substring(2))"
    }
    return $Principal
}

function Test-WindowsPrincipal {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Principal
    )

    try {
        $account = New-Object System.Security.Principal.NTAccount (Resolve-AccountPrincipal -Principal $Principal)
        $null = $account.Translate([System.Security.Principal.SecurityIdentifier])
        return $true
    } catch {
        return $false
    }
}

function Get-StateFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    return Join-Path (Resolve-FullPath -Path $Root) 'state.json'
}

function New-HardeningState {
    return [ordered]@{
        SchemaVersion = 1
        CreatedUtc = [DateTime]::UtcNow.ToString('o')
        BaselinePath = (Resolve-FullPath -Path $BaselinePath)
        Acls = @()
        CreatedDirectories = @()
        CreatedFirewallRules = @()
        RegistryValues = @()
        IisBackups = @()
        SqlLoginsCreated = @()
        CertificateKeyAcls = @()
    }
}

function Read-HardeningState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Estado de hardening nao encontrado: $Path."
    }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Save-HardeningState {
    param(
        [Parameter(Mandatory = $true)]
        [object]$State,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    $json = $State | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText($Path, $json, (New-Object System.Text.UTF8Encoding($false)))
}

function Add-StateEntry {
    param(
        [Parameter(Mandatory = $true)]
        [object]$State,
        [Parameter(Mandatory = $true)]
        [string]$Property,
        [Parameter(Mandatory = $true)]
        [object]$Entry,
        [string]$UniqueProperty = ''
    )

    $entries = @($State.$Property)
    if (-not [string]::IsNullOrWhiteSpace($UniqueProperty) -and
        @($entries | Where-Object { $_.$UniqueProperty -eq $Entry.$UniqueProperty }).Count -gt 0) {
        return
    }
    $State.$Property = @($entries + $Entry)
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    if (Test-Path -LiteralPath $Path -PathType Container) {
        return
    }
    if ($PSCmdlet.ShouldProcess($Path, 'criar diretorio de ambiente')) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Add-StateEntry -State $State -Property 'CreatedDirectories' -Entry $Path
    }
}

function Get-EnvironmentPaths {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CurrentEnvironment
    )

    return @(
        $CurrentEnvironment.Root,
        $CurrentEnvironment.ApplicationRoot,
        $CurrentEnvironment.DataRoot,
        $CurrentEnvironment.SecretsRoot,
        $CurrentEnvironment.BackupRoot
    ) | ForEach-Object { Resolve-FullPath -Path $_ }
}

function Get-AclRules {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CurrentEnvironment,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $rules = New-Object 'System.Collections.Generic.List[object]'
    $null = $rules.Add(@{ Principal = 'SID:S-1-5-18'; Rights = 'FullControl' })
    $null = $rules.Add(@{ Principal = 'SID:S-1-5-32-544'; Rights = 'FullControl' })

    if ($CurrentEnvironment.Provisioning.Mode -eq 'Workstation') {
        $null = $rules.Add(@{ Principal = 'CURRENT_USER'; Rights = 'Modify' })
        return $rules.ToArray()
    }

    $applicationRoot = Resolve-FullPath -Path $CurrentEnvironment.ApplicationRoot
    $dataRoot = Resolve-FullPath -Path $CurrentEnvironment.DataRoot
    $secretsRoot = Resolve-FullPath -Path $CurrentEnvironment.SecretsRoot
    $backupRoot = Resolve-FullPath -Path $CurrentEnvironment.BackupRoot

    if ($Path -eq $applicationRoot) {
        foreach ($account in @($CurrentEnvironment.Accounts | Where-Object { $_.Role -ne 'Backup' })) {
            $null = $rules.Add(@{ Principal = $account.SqlPrincipal; Rights = 'ReadAndExecute' })
        }
    } elseif ($Path -eq $dataRoot) {
        foreach ($account in @($CurrentEnvironment.Accounts | Where-Object { $_.Role -in @('ApiMyAnimes', 'FileStorage', 'Backup') })) {
            $null = $rules.Add(@{ Principal = $account.SqlPrincipal; Rights = 'Modify' })
        }
    } elseif ($Path -eq $secretsRoot) {
        foreach ($account in @($CurrentEnvironment.Accounts | Where-Object { $_.Role -ne 'Backup' })) {
            $null = $rules.Add(@{ Principal = $account.SqlPrincipal; Rights = 'Read' })
        }
    } elseif ($Path -eq $backupRoot) {
        $backup = @($CurrentEnvironment.Accounts | Where-Object Role -eq 'Backup')
        if ($backup.Count -eq 1) {
            $null = $rules.Add(@{ Principal = $backup[0].SqlPrincipal; Rights = 'Modify' })
        }
    }
    return $rules.ToArray()
}

function Save-AclSnapshot {
    param(
        [Parameter(Mandatory = $true)]
        [object]$State,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return
    }
    if (@($State.Acls | Where-Object Path -eq $Path).Count -eq 0) {
        $acl = Get-Acl -LiteralPath $Path
        Add-StateEntry -State $State -Property 'Acls' -UniqueProperty 'Path' -Entry ([ordered]@{
                Path = $Path
                Sddl = $acl.Sddl
            })
    }
}

function Set-DirectoryAcl {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CurrentEnvironment,
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    Save-AclSnapshot -State $State -Path $Path
    $currentAcl = Get-Acl -LiteralPath $Path
    $currentRules = @($currentAcl.Access)
    $desiredRules = New-Object 'System.Collections.Generic.List[object]'
    foreach ($ruleDefinition in @(Get-AclRules -CurrentEnvironment $CurrentEnvironment -Path $Path)) {
        $principal = Resolve-AccountPrincipal -Principal $ruleDefinition.Principal
        $rights = [System.Enum]::Parse([System.Security.AccessControl.FileSystemRights], $ruleDefinition.Rights)
        $inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule (
            $principal,
            $rights,
            $inheritance,
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow
        )
        $null = $desiredRules.Add($rule)
    }

    $aclMatches = $currentAcl.AreAccessRulesProtected -and $currentRules.Count -eq $desiredRules.Count
    if ($aclMatches) {
        foreach ($desiredRule in $desiredRules) {
            $matchingRules = @($currentRules | Where-Object {
                    $_.IdentityReference.Value.Equals($desiredRule.IdentityReference.Value, [System.StringComparison]::OrdinalIgnoreCase) -and
                    $_.FileSystemRights -eq $desiredRule.FileSystemRights -and
                    $_.InheritanceFlags -eq $desiredRule.InheritanceFlags -and
                    $_.PropagationFlags -eq $desiredRule.PropagationFlags -and
                    $_.AccessControlType -eq $desiredRule.AccessControlType -and
                    $_.IsInherited -eq $desiredRule.IsInherited
                })
            if ($matchingRules.Count -ne 1) {
                $aclMatches = $false
                break
            }
        }
    }

    if (-not $aclMatches -and $PSCmdlet.ShouldProcess($Path, 'aplicar ACL minima')) {
        $acl = $currentAcl
        $acl.SetAccessRuleProtection($true, $false)
        foreach ($existingRule in @($acl.Access)) {
            $null = $acl.RemoveAccessRule($existingRule)
        }
        foreach ($desiredRule in $desiredRules) {
            $acl.SetAccessRule($desiredRule)
        }
        Set-Acl -LiteralPath $Path -AclObject $acl
    }
}

function Normalize-CertificateThumbprint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Thumbprint
    )

    return $Thumbprint.Replace(' ', '').Replace(':', '').ToUpperInvariant()
}

function Assert-CertificateAclParameters {
    if (-not $ConfigureCertificateAcl) {
        return
    }
    $thumbprintInput = if ($null -eq $CertificateThumbprint) { '' } else { $CertificateThumbprint }
    $normalizedThumbprint = Normalize-CertificateThumbprint -Thumbprint $thumbprintInput
    if ([string]::IsNullOrWhiteSpace($CertificateThumbprint) -or ($normalizedThumbprint -notmatch '^[A-F0-9]{40}$')) {
        throw 'Informe um thumbprint hexadecimal de 40 caracteres para o certificado de cliente.'
    }
    if ([string]::IsNullOrWhiteSpace($CertificatePrincipal)) {
        throw 'Informe o principal Windows que executara o servico para configurar a ACL da chave privada.'
    }
    if ($CertificateStoreLocation -eq 'LocalMachine' -and -not (Test-IsAdministrator)) {
        throw 'A ACL de chave privada em LocalMachine exige uma sessao elevada de administrador.'
    }
}

function Get-CertificatePrivateKeyPath {
    param(
        [Parameter(Mandatory = $true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )

    $rsa = $null
    try {
        $rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($Certificate)
        if ($rsa -is [System.Security.Cryptography.RSACng]) {
            $key = $rsa.Key
            if (-not $key.IsEphemeral -and -not [string]::IsNullOrWhiteSpace($key.UniqueName)) {
                $root = if ($key.IsMachineKey) {
                    Join-Path $env:ProgramData 'Microsoft\Crypto\Keys'
                } else {
                    Join-Path $env:APPDATA 'Microsoft\Crypto\Keys'
                }
                return Join-Path $root $key.UniqueName
            }
        } elseif ($rsa -is [System.Security.Cryptography.RSACryptoServiceProvider]) {
            $container = $rsa.CspKeyContainerInfo
            if (-not [string]::IsNullOrWhiteSpace($container.UniqueKeyContainerName)) {
                $root = if ($container.MachineKeyStore) {
                    Join-Path $env:ProgramData 'Microsoft\Crypto\RSA\MachineKeys'
                } else {
                    $sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
                    Join-Path (Join-Path $env:APPDATA 'Microsoft\Crypto\RSA') $sid
                }
                return Join-Path $root $container.UniqueKeyContainerName
            }
        }
    } catch {
    } finally {
        if ($null -ne $rsa) {
            $rsa.Dispose()
        }
    }

    $ecdsa = $null
    try {
        $ecdsa = [System.Security.Cryptography.X509Certificates.ECDsaCertificateExtensions]::GetECDsaPrivateKey($Certificate)
        if ($ecdsa -is [System.Security.Cryptography.ECDsaCng]) {
            $key = $ecdsa.Key
            if (-not $key.IsEphemeral -and -not [string]::IsNullOrWhiteSpace($key.UniqueName)) {
                $root = if ($key.IsMachineKey) {
                    Join-Path $env:ProgramData 'Microsoft\Crypto\Keys'
                } else {
                    Join-Path $env:APPDATA 'Microsoft\Crypto\Keys'
                }
                return Join-Path $root $key.UniqueName
            }
        }
    } catch {
    } finally {
        if ($null -ne $ecdsa) {
            $ecdsa.Dispose()
        }
    }

    throw 'Nao foi possivel resolver a chave privada do certificado para um arquivo ACLavel suportado.'
}

function Get-CertificateKeyContext {
    $thumbprint = Normalize-CertificateThumbprint -Thumbprint $CertificateThumbprint
    if ($thumbprint -notmatch '^[A-F0-9]{40}$') {
        throw 'Thumbprint de certificado invalido.'
    }

    $storeLocation = [System.Security.Cryptography.X509Certificates.StoreLocation]::$CertificateStoreLocation
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store (
        $CertificateStoreName,
        $storeLocation)
    $certificate = $null
    try {
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
        $certificate = @($store.Certificates.Find(
                [System.Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
                $thumbprint,
                $false)) | Select-Object -First 1
        if ($null -eq $certificate) {
            throw 'Certificado nao encontrado no Certificate Store informado.'
        }
        if (-not $certificate.HasPrivateKey) {
            throw 'O certificado localizado nao possui chave privada.'
        }

        $path = Get-CertificatePrivateKeyPath -Certificate $certificate
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw 'O arquivo da chave privada do certificado nao foi encontrado.'
        }

        $principal = Resolve-AccountPrincipal -Principal $CertificatePrincipal
        if (-not (Test-WindowsPrincipal -Principal $CertificatePrincipal)) {
            throw 'O principal Windows informado para a ACL nao foi resolvido.'
        }

        return [pscustomobject]@{
            Path = $path
            Principal = $principal
            StoreName = $CertificateStoreName
            StoreLocation = $CertificateStoreLocation
        }
    } finally {
        if ($null -ne $certificate) {
            $certificate.Dispose()
        }
        $store.Dispose()
    }
}

function Set-DaclFromSddl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Sddl
    )

    if ($null -eq ('DtudoInfrastructureNativeSecurity' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

public static class DtudoInfrastructureNativeSecurity
{
    public enum SeObjectType { SeFileObject = 1 }

    [Flags]
    public enum SecurityInformation : uint { Dacl = 0x00000004 }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint SetNamedSecurityInfo(
        string objectName,
        SeObjectType objectType,
        SecurityInformation securityInformation,
        IntPtr owner,
        IntPtr group,
        IntPtr dacl,
        IntPtr sacl);

    public static void SetDacl(string path, byte[] dacl)
    {
        GCHandle handle = new GCHandle();
        try
        {
            IntPtr daclPointer = IntPtr.Zero;
            if (dacl != null)
            {
                handle = GCHandle.Alloc(dacl, GCHandleType.Pinned);
                daclPointer = handle.AddrOfPinnedObject();
            }

            uint result = SetNamedSecurityInfo(
                path,
                SeObjectType.SeFileObject,
                SecurityInformation.Dacl,
                IntPtr.Zero,
                IntPtr.Zero,
                daclPointer,
                IntPtr.Zero);
            if (result != 0)
            {
                throw new Win32Exception((int)result);
            }
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }
}
'@
    }

    $descriptor = New-Object System.Security.AccessControl.RawSecurityDescriptor($Sddl)
    $dacl = $descriptor.DiscretionaryAcl
    $daclBytes = $null
    if ($null -ne $dacl) {
        $daclBytes = New-Object byte[] $dacl.BinaryLength
        $dacl.GetBinaryForm($daclBytes, 0)
    }
    [DtudoInfrastructureNativeSecurity]::SetDacl($Path, $daclBytes)
}

function Test-CertificateKeyReadAcl {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Context
    )

    $acl = Get-Acl -LiteralPath $Context.Path
    $readRights = [System.Security.AccessControl.FileSystemRights]::Read
    $readWithSynchronizeRights = $readRights -bor [System.Security.AccessControl.FileSystemRights]::Synchronize
    $explicitRules = @($acl.Access | Where-Object {
            -not $_.IsInherited -and
            $_.IdentityReference.Value.Equals($Context.Principal, [System.StringComparison]::OrdinalIgnoreCase) -and
            $_.AccessControlType -in @(
                [System.Security.AccessControl.AccessControlType]::Allow,
                [System.Security.AccessControl.AccessControlType]::Deny)
        })
    $readRules = @($explicitRules | Where-Object {
            $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow -and
            ($_.FileSystemRights -eq $readRights -or $_.FileSystemRights -eq $readWithSynchronizeRights) -and
            $_.InheritanceFlags -eq [System.Security.AccessControl.InheritanceFlags]::None -and
            $_.PropagationFlags -eq [System.Security.AccessControl.PropagationFlags]::None
        })
    return $readRules.Count -eq 1 -and $explicitRules.Count -eq 1
}

function Save-CertificateKeyAclSnapshot {
    param(
        [Parameter(Mandatory = $true)]
        [object]$State,
        [Parameter(Mandatory = $true)]
        [object]$Context
    )

    if (-not ($State.PSObject.Properties.Name -contains 'CertificateKeyAcls')) {
        $State | Add-Member -MemberType NoteProperty -Name CertificateKeyAcls -Value @()
    }
    if (@($State.CertificateKeyAcls | Where-Object {
                $_.Path -eq $Context.Path -and $_.Principal -eq $Context.Principal
            }).Count -eq 0) {
        $acl = Get-Acl -LiteralPath $Context.Path
        Add-StateEntry -State $State -Property 'CertificateKeyAcls' -UniqueProperty 'Path' -Entry ([ordered]@{
                Path = $Context.Path
                Principal = $Context.Principal
                Sddl = $acl.Sddl
            })
    }
}

function Invoke-CertificateKeyAclValidation {
    try {
        $context = Get-CertificateKeyContext
        if (Test-CertificateKeyReadAcl -Context $context) {
            Add-Result -Check 'ACL da chave privada do certificado' -Status Passed -Detail 'Principal do servico possui somente a regra explicita de leitura esperada.'
        } else {
            Add-Result -Check 'ACL da chave privada do certificado' -Status Failed -Detail 'A regra explicita de leitura para o principal do servico esta ausente.'
        }
    } catch {
        Add-Result -Check 'ACL da chave privada do certificado' -Status Blocked -Detail $_.Exception.Message
    }
}

function Invoke-CertificateKeyAclApply {
    param(
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    $context = Get-CertificateKeyContext
    if (Test-CertificateKeyReadAcl -Context $context) {
        Add-Result -Check 'ACL da chave privada do certificado' -Status Passed -Detail 'ACL ja estava aplicada; nenhuma alteracao foi necessaria.'
        return
    }

    Save-CertificateKeyAclSnapshot -State $State -Context $context
    $acl = Get-Acl -LiteralPath $context.Path
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule (
        $context.Principal,
        [System.Security.AccessControl.FileSystemRights]::Read,
        [System.Security.AccessControl.InheritanceFlags]::None,
        [System.Security.AccessControl.PropagationFlags]::None,
        [System.Security.AccessControl.AccessControlType]::Allow
    )
    if ($PSCmdlet.ShouldProcess($context.Path, 'conceder somente leitura da chave privada ao servico')) {
        foreach ($existingRule in @($acl.Access | Where-Object {
                    -not $_.IsInherited -and
                    $_.IdentityReference.Value.Equals($context.Principal, [System.StringComparison]::OrdinalIgnoreCase)
                })) {
            $acl.RemoveAccessRuleSpecific($existingRule)
        }
        $acl.SetAccessRule($rule)
        Set-Acl -LiteralPath $context.Path -AclObject $acl
        Add-Result -Check 'ACL da chave privada do certificado' -Status Passed -Detail 'Leitura explicita concedida ao principal do servico; snapshot salvo para rollback.'
    }
}

function Get-FirewallRule {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    return Get-NetFirewallRule -Name $Name -ErrorAction SilentlyContinue
}

function Add-FirewallRule {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Parameters,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    $existing = Get-FirewallRule -Name $Parameters.Name
    if ($null -ne $existing) {
        return
    }
    if ($PSCmdlet.ShouldProcess($Parameters.Name, 'criar regra de firewall')) {
        $null = New-NetFirewallRule @Parameters
        Add-StateEntry -State $State -Property 'CreatedFirewallRules' -Entry $Parameters.Name
    }
}

function Invoke-FirewallApply {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Baseline,
        [Parameter(Mandatory = $true)]
        [hashtable[]]$Environments,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    $prefix = $Baseline.Firewall.RulePrefix
    Add-FirewallRule -State $State -Parameters @{
        Name = "$prefix-Block-Sql-Tcp"
        DisplayName = "$prefix Block SQL TCP"
        Direction = 'Inbound'
        Action = 'Block'
        Protocol = 'TCP'
        LocalPort = (@($Baseline.Firewall.BlockSqlTcpPorts) -join ',')
        Profile = 'Any'
        Description = 'SQL nao recebe conexoes TCP externas; use Windows Authentication local.'
    }
    Add-FirewallRule -State $State -Parameters @{
        Name = "$prefix-Block-Sql-Udp"
        DisplayName = "$prefix Block SQL Browser"
        Direction = 'Inbound'
        Action = 'Block'
        Protocol = 'UDP'
        LocalPort = (@($Baseline.Firewall.BlockSqlUdpPorts) -join ',')
        Profile = 'Any'
        Description = 'SQL Browser nao e publicado.'
    }

    foreach ($currentEnvironment in $Environments) {
        $environmentToken = $currentEnvironment.Name.ToLowerInvariant()
        $network = $currentEnvironment.Network
        $remoteAddresses = @($network.AllowedRemoteAddresses)
        $gatewayName = "$prefix-$environmentToken-Gateway"
        if ($network.Exposure -eq 'PublicGatewayOnly') {
            Add-FirewallRule -State $State -Parameters @{
                Name = $gatewayName
                DisplayName = "$prefix $($currentEnvironment.Name) gateway HTTPS"
                Direction = 'Inbound'
                Action = 'Allow'
                Protocol = 'TCP'
                LocalPort = $network.GatewayHttpsPort
                RemoteAddress = 'Any'
                Profile = 'Any'
                Description = 'Somente IIS/DtudoGateway pode usar a porta publica.'
            }
        } else {
            Add-FirewallRule -State $State -Parameters @{
                Name = "$gatewayName-Block"
                DisplayName = "$prefix $($currentEnvironment.Name) bloqueio gateway externo"
                Direction = 'Inbound'
                Action = 'Block'
                Protocol = 'TCP'
                LocalPort = $network.GatewayHttpsPort
                RemoteAddress = 'Any'
                Profile = 'Any'
                Description = 'Gateway de ambiente nao produtivo fica em loopback.'
            }
            Add-FirewallRule -State $State -Parameters @{
                Name = $gatewayName
                DisplayName = "$prefix $($currentEnvironment.Name) gateway loopback"
                Direction = 'Inbound'
                Action = 'Allow'
                Protocol = 'TCP'
                LocalPort = $network.GatewayHttpsPort
                RemoteAddress = $remoteAddresses
                OverrideBlockRules = $true
                Profile = 'Any'
                Description = 'Gateway nao produtivo aceita somente loopback.'
            }
        }

        foreach ($port in @($network.InternalPorts)) {
            $portToken = [string]$port
            Add-FirewallRule -State $State -Parameters @{
                Name = "$prefix-$environmentToken-Internal-$portToken-Block"
                DisplayName = "$prefix $($currentEnvironment.Name) API interna $port bloqueio externo"
                Direction = 'Inbound'
                Action = 'Block'
                Protocol = 'TCP'
                LocalPort = $port
                RemoteAddress = 'Any'
                Profile = 'Any'
                Description = 'APIs, Seq e servicos internos nao sao publicos.'
            }
            Add-FirewallRule -State $State -Parameters @{
                Name = "$prefix-$environmentToken-Internal-$portToken-AllowLoopback"
                DisplayName = "$prefix $($currentEnvironment.Name) API interna $port loopback"
                Direction = 'Inbound'
                Action = 'Allow'
                Protocol = 'TCP'
                LocalPort = $port
                RemoteAddress = @('127.0.0.1', '::1')
                OverrideBlockRules = $true
                Profile = 'Any'
                Description = 'Somente o gateway no mesmo host acessa a API interna.'
            }
        }
    }
}

function Get-RegistryValueSnapshot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $exists = Test-Path -LiteralPath $Path
    $valueExists = $false
    $value = $null
    if ($exists) {
        try {
            $value = (Get-ItemProperty -LiteralPath $Path -Name $Name -ErrorAction Stop).$Name
            $valueExists = $true
        } catch {
            $valueExists = $false
        }
    }
    return [ordered]@{
        Path = $Path
        Name = $Name
        KeyExists = $exists
        ValueExists = $valueExists
        Value = $value
    }
}

function Set-SchannelProtocol {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Protocol,
        [Parameter(Mandatory = $true)]
        [bool]$Enabled,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    $path = "HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\$Protocol\Server"
    foreach ($property in @('Enabled', 'DisabledByDefault')) {
        $snapshot = Get-RegistryValueSnapshot -Path $path -Name $property
        if (@($State.RegistryValues | Where-Object { $_.Path -eq $snapshot.Path -and $_.Name -eq $snapshot.Name }).Count -eq 0) {
            $State.RegistryValues = @(@($State.RegistryValues) + $snapshot)
        }
    }
    if ($PSCmdlet.ShouldProcess($path, "configurar $Protocol")) {
        New-Item -Path $path -Force | Out-Null
        if ($Enabled) {
            New-ItemProperty -LiteralPath $path -Name Enabled -PropertyType DWord -Value 1 -Force | Out-Null
            New-ItemProperty -LiteralPath $path -Name DisabledByDefault -PropertyType DWord -Value 0 -Force | Out-Null
        } else {
            New-ItemProperty -LiteralPath $path -Name Enabled -PropertyType DWord -Value 0 -Force | Out-Null
            New-ItemProperty -LiteralPath $path -Name DisabledByDefault -PropertyType DWord -Value 1 -Force | Out-Null
        }
    }
}

function Invoke-TlsApply {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Baseline,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    foreach ($protocol in @($Baseline.Tls.DisabledServerProtocols)) {
        Set-SchannelProtocol -Protocol $protocol -Enabled $false -State $State
    }
    foreach ($protocol in @($Baseline.Tls.EnabledServerProtocols)) {
        Set-SchannelProtocol -Protocol $protocol -Enabled $true -State $State
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

    if ($Value -notmatch '^[A-Za-z_][A-Za-z0-9_\\$#@]{0,127}$') {
        throw 'Identificador SQL fora da baseline permitida.'
    }
    return "[$($Value.Replace(']', ']]'))]"
}

function Invoke-SqlCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Server,
        [Parameter(Mandatory = $true)]
        [string]$Query
    )

    $sqlcmd = Get-Command sqlcmd -ErrorAction Stop
    $arguments = @('-S', $Server, '-E', '-d', 'master', '-b', '-r', '1', '-X', '-Q', $Query)
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& $sqlcmd.Source @arguments 2>&1)
    } finally {
        $ErrorActionPreference = $previousPreference
    }
    if ($LASTEXITCODE -ne 0) {
        throw 'sqlcmd falhou; nenhuma mensagem SQL foi registrada.'
    }
    return $output
}

function Get-ExpectedSqlLogins {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CurrentEnvironment
    )

    $roles = @($CurrentEnvironment.DatabaseAccess | ForEach-Object { $_.AccountRole } | Sort-Object -Unique)
    return @($CurrentEnvironment.Accounts | Where-Object { $_.Role -in $roles } | ForEach-Object { Resolve-AccountPrincipal -Principal $_.SqlPrincipal })
}

function Invoke-SqlBaseline {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CurrentEnvironment,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    $sqlFile = Join-Path $PSScriptRoot 'Configure-DtudoSqlWindowsAuthentication.sql'
    if (-not (Test-Path -LiteralPath $sqlFile -PathType Leaf)) {
        throw 'Script SQL da Etapa 07 nao encontrado.'
    }
    $logins = @(Get-ExpectedSqlLogins -CurrentEnvironment $CurrentEnvironment)
    $missingBeforeApply = New-Object 'System.Collections.Generic.List[string]'
    foreach ($login in $logins) {
        $literal = ConvertTo-SqlLiteral -Value $login
        $result = Invoke-SqlCommand -Server $CurrentEnvironment.Sql.Server -Query "SET NOCOUNT ON; SELECT CASE WHEN SUSER_ID($literal) IS NULL THEN N'0' ELSE N'1' END;"
        $lastValue = @($result | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_ -in @('0', '1') } | Select-Object -Last 1)
        if ($lastValue.Count -ne 1) {
            throw 'Nao foi possivel verificar o login Windows antes da configuracao.'
        }
        if ($lastValue[0] -eq '0') {
            $null = $missingBeforeApply.Add($login)
        }
    }

    if ($PSCmdlet.ShouldProcess($CurrentEnvironment.Sql.Server, "configurar SQL Windows Authentication para $($CurrentEnvironment.Name)")) {
        $sqlcmd = Get-Command sqlcmd -ErrorAction Stop
        $arguments = @(
            '-S', $CurrentEnvironment.Sql.Server,
            '-E',
            '-d', 'master',
            '-b',
            '-r', '1',
            '-X',
            '-i', $sqlFile,
            '-v',
            "MyAnimesDatabase=$($CurrentEnvironment.Sql.MyAnimesDatabase)",
            "IdentityDatabase=$($CurrentEnvironment.Sql.IdentityDatabase)",
            "ApiMyAnimesPrincipal=$(Resolve-AccountPrincipal -Principal (@($CurrentEnvironment.Accounts | Where-Object Role -eq 'ApiMyAnimes')[0].SqlPrincipal))",
            "BackupPrincipal=$(Resolve-AccountPrincipal -Principal (@($CurrentEnvironment.Accounts | Where-Object Role -eq 'Backup')[0].SqlPrincipal))"
        )
        $previousPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            $null = @(& $sqlcmd.Source @arguments 2>&1)
        } finally {
            $ErrorActionPreference = $previousPreference
        }
        if ($LASTEXITCODE -ne 0) {
            throw 'Configuracao SQL falhou; nenhuma mensagem SQL foi registrada.'
        }
        foreach ($login in $missingBeforeApply) {
            Add-StateEntry -State $State -Property 'SqlLoginsCreated' -Entry $login
        }
    }
}

function Invoke-SqlRollback {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CurrentEnvironment,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    $createdLogins = @($State.SqlLoginsCreated)
    if ($createdLogins.Count -eq 0) {
        return
    }
    foreach ($login in $createdLogins) {
        $safeLogin = ConvertTo-SqlIdentifier -Value ([string]$login)
        $loginLiteral = ConvertTo-SqlLiteral -Value ([string]$login)
        foreach ($database in @($CurrentEnvironment.Sql.MyAnimesDatabase, $CurrentEnvironment.Sql.IdentityDatabase)) {
            $safeDatabase = ConvertTo-SqlIdentifier -Value $database
            $query = "IF DB_ID($(ConvertTo-SqlLiteral -Value $database)) IS NOT NULL BEGIN USE $safeDatabase; IF USER_ID($loginLiteral) IS NOT NULL DROP USER $safeLogin; END;"
            if ($PSCmdlet.ShouldProcess($database, "remover usuario SQL criado pela Etapa 07: $login")) {
                Invoke-SqlCommand -Server $CurrentEnvironment.Sql.Server -Query $query | Out-Null
            }
        }
        $dropLoginQuery = "IF SUSER_ID($loginLiteral) IS NOT NULL DROP LOGIN $safeLogin;"
        if ($PSCmdlet.ShouldProcess($CurrentEnvironment.Sql.Server, "remover login SQL criado pela Etapa 07: $login")) {
            Invoke-SqlCommand -Server $CurrentEnvironment.Sql.Server -Query $dropLoginQuery | Out-Null
        }
    }
}

function Set-IisHeader {
    param(
        [Parameter(Mandatory = $true)]
        [string]$IisPath,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $filter = 'system.webServer/httpProtocol/customHeaders'
    $existing = @(Get-WebConfigurationProperty -PSPath $IisPath -Filter $filter -Name '.' | Where-Object { $_.name -eq $Name })
    if ($existing.Count -eq 0) {
        Add-WebConfigurationProperty -PSPath $IisPath -Filter $filter -Name '.' -Value @{ name = $Name; value = $Value }
    } else {
        Set-WebConfigurationProperty -PSPath $IisPath -Filter "$filter/add[@name='$Name']" -Name value -Value $Value
    }
}

function Invoke-IisApply {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$CurrentEnvironment,
        [Parameter(Mandatory = $true)]
        [object]$State
    )

    if (-not (Get-Command Get-Website -ErrorAction SilentlyContinue)) {
        throw 'IIS/WebAdministration nao esta instalado; nenhuma configuracao IIS foi aplicada.'
    }
    $appcmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
    if (-not (Test-Path -LiteralPath $appcmd -PathType Leaf)) {
        throw 'appcmd.exe nao esta disponivel para criar rollback da configuracao IIS.'
    }
    Import-Module WebAdministration -ErrorAction Stop
    $site = Get-Website -Name $CurrentEnvironment.Iis.SiteName -ErrorAction SilentlyContinue
    if ($null -eq $site) {
        throw "Site IIS inexistente; a Etapa 07 nao cria nem publica sites: $($CurrentEnvironment.Iis.SiteName)."
    }
    $iisPath = "IIS:\Sites\$($CurrentEnvironment.Iis.SiteName)"
    if ($PSCmdlet.ShouldProcess($CurrentEnvironment.Iis.SiteName, 'aplicar baseline IIS')) {
        $backupName = "Dtudo2026-Etapa07-$($CurrentEnvironment.Name)"
        if (@($State.IisBackups | Where-Object Name -eq $backupName).Count -eq 0) {
            $previousPreference = $ErrorActionPreference
            try {
                $ErrorActionPreference = 'Continue'
                $null = @(& $appcmd add backup $backupName 2>&1)
            } finally {
                $ErrorActionPreference = $previousPreference
            }
            if ($LASTEXITCODE -ne 0) {
                throw 'Nao foi possivel criar backup da configuracao IIS; nenhuma alteracao IIS foi aplicada.'
            }
            Add-StateEntry -State $State -Property 'IisBackups' -UniqueProperty 'Name' -Entry ([ordered]@{
                    Name = $backupName
                    Environment = $CurrentEnvironment.Name
                })
        }
        Set-WebConfigurationProperty -PSPath $iisPath -Filter 'system.webServer/security/requestFiltering/requestLimits' -Name maxAllowedContentLength -Value 52428800
        Set-IisHeader -IisPath $iisPath -Name 'X-Content-Type-Options' -Value 'nosniff'
        Set-IisHeader -IisPath $iisPath -Name 'X-Frame-Options' -Value 'DENY'
        Set-IisHeader -IisPath $iisPath -Name 'Referrer-Policy' -Value 'no-referrer'
        if ($CurrentEnvironment.Iis.EnableHsts) {
            Set-IisHeader -IisPath $iisPath -Name 'Strict-Transport-Security' -Value 'max-age=31536000; includeSubDomains'
        }
        Remove-WebConfigurationProperty -PSPath $iisPath -Filter 'system.webServer/httpProtocol/customHeaders' -Name '.' -AtElement @{ name = 'X-Powered-By' } -ErrorAction SilentlyContinue
    }
}

function Invoke-NegativeTest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    $failedAsExpected = $false
    try {
        & $Action
    } catch {
        $failedAsExpected = $true
    }
    if (-not $failedAsExpected) {
        Add-Result -Check "Negativo: $Name" -Status Failed -Detail 'Entrada proibida foi aceita.'
        throw "Teste negativo falhou: $Name."
    }
    Add-Result -Check "Negativo: $Name" -Status Passed -Detail 'Entrada proibida foi recusada.'
}

function Invoke-NegativeTests {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable[]]$Environments
    )

    $development = @($Environments | Where-Object Name -eq 'Development')[0]
    $production = @($Environments | Where-Object Name -eq 'Production')[0]
    Invoke-NegativeTest -Name 'traversal entre ambientes' -Action {
        Assert-PathWithinRoot -Root $development.Root -Path (Join-Path $development.Root '..\Production\Secrets')
    }
    Invoke-NegativeTest -Name 'banco duplicado' -Action {
        Assert-UniqueValues -Values @('Dtudo2026Db_Production', 'Dtudo2026Db_Production') -Description 'bancos sinteticos'
    }
    Invoke-NegativeTest -Name 'colisao de porta' -Action {
        Assert-UniqueValues -Values @(443, 443) -Description 'portas sinteticas'
    }
    Invoke-NegativeTest -Name 'porta interna publicada' -Action {
        if (@($production.Network.InternalPorts | Where-Object { $_ -eq $production.Network.GatewayHttpsPort }).Count -eq 0) {
            throw 'porta interna inexistente'
        }
    }
    Invoke-NegativeTest -Name 'credencial SQL na baseline' -Action {
        $text = Get-Content -LiteralPath $BaselinePath -Raw
        if ($text -notmatch '(?im)(password|token|clientsecret|apikey)\s*=') {
            throw 'nenhuma credencial presente'
        }
    }
}

function Invoke-HostValidation {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Baseline,
        [Parameter(Mandatory = $true)]
        [hashtable[]]$Environments
    )

    $requiresAdministrator = @($Environments | Where-Object { $_.Provisioning.RequiresAdministrator }).Count -gt 0
    if (-not $requiresAdministrator) {
        Add-Result -Check 'Sessao administrativa' -Status NotChecked -Detail 'Nao necessaria para o ambiente Development workstation.'
    } elseif (Test-IsAdministrator) {
        Add-Result -Check 'Sessao administrativa' -Status Passed -Detail 'Administrador detectado.'
    } else {
        Add-Result -Check 'Sessao administrativa' -Status Blocked -Detail 'Execute Apply/Rollback em PowerShell elevado no servidor alvo.'
    }

    if ($ConfigureCertificateAcl) {
        Invoke-CertificateKeyAclValidation
    } else {
        Add-Result -Check 'ACL da chave privada do certificado' -Status NotChecked -Detail 'Use -ConfigureCertificateAcl com thumbprint e principal Windows para validar a chave do servico.'
    }

    foreach ($commandName in @('sqlcmd', 'Get-NetFirewallProfile', 'Get-NetTCPConnection')) {
        if (Get-Command $commandName -ErrorAction SilentlyContinue) {
            Add-Result -Check "Comando: $commandName" -Status Passed -Detail 'Disponivel.'
        } else {
            Add-Result -Check "Comando: $commandName" -Status Blocked -Detail 'Nao disponivel neste host.'
        }
    }

    if (@($Environments | Where-Object { $_.Network.ConfigureFirewall }).Count -eq 0) {
        Add-Result -Check 'Firewall profiles' -Status NotChecked -Detail 'Nao aplicavel ao Development workstation; regras do host nao foram alteradas.'
    } elseif (Get-Command Get-NetFirewallProfile -ErrorAction SilentlyContinue) {
        try {
            $profiles = @(Get-NetFirewallProfile -ErrorAction Stop)
            $disabled = @($profiles | Where-Object { -not $_.Enabled })
            if ($disabled.Count -eq 0) {
                Add-Result -Check 'Firewall profiles' -Status Passed -Detail 'Perfis habilitados.'
            } else {
                Add-Result -Check 'Firewall profiles' -Status Blocked -Detail 'Existe perfil de firewall desabilitado.'
            }
        } catch {
            Add-Result -Check 'Firewall profiles' -Status Blocked -Detail 'Leitura requer permissao administrativa.'
        }
    }

    if (Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue) {
        try {
            $internalPorts = @($Environments | ForEach-Object { $_.Network.InternalPorts })
            $blockedSqlPorts = @($Baseline.Firewall.BlockSqlTcpPorts) + @($Baseline.Firewall.BlockSqlUdpPorts)
            $listeners = @(Get-NetTCPConnection -State Listen -ErrorAction Stop)
            $publicInternal = @($listeners | Where-Object {
                    $_.LocalPort -in $internalPorts -and $_.LocalAddress -notin @('127.0.0.1', '::1')
                })
            $sqlListeners = @($listeners | Where-Object { $_.LocalPort -in @($Baseline.Firewall.BlockSqlTcpPorts) })
            if ($publicInternal.Count -eq 0) {
                Add-Result -Check 'Portas internas sem bind publico' -Status Passed -Detail 'Nenhuma API/Seq da baseline escuta em endereco externo.'
            } else {
                Add-Result -Check 'Portas internas sem bind publico' -Status Failed -Detail 'Existe listener externo em porta interna da baseline.'
            }
            if ($sqlListeners.Count -eq 0) {
                Add-Result -Check 'SQL sem listener TCP publico' -Status Passed -Detail 'Nenhum listener TCP em porta SQL bloqueada foi detectado.'
            } else {
                Add-Result -Check 'SQL sem listener TCP publico' -Status Failed -Detail 'Listener SQL detectado em porta bloqueada.'
            }
        } catch {
            Add-Result -Check 'Portas e listeners' -Status Blocked -Detail 'Leitura de listeners requer permissao adequada.'
        }
    }

    foreach ($currentEnvironment in $Environments) {
        foreach ($path in @(Get-EnvironmentPaths -CurrentEnvironment $currentEnvironment)) {
            if (Test-Path -LiteralPath $path -PathType Container) {
                Add-Result -Check "Diretorio $($currentEnvironment.Name)" -Status Passed -Detail 'Raiz existente; ACL sera comparada no Apply.'
            } else {
                Add-Result -Check "Diretorio $($currentEnvironment.Name)" -Status Blocked -Detail 'Raiz ainda nao foi provisionada.'
            }
        }
        foreach ($account in @($currentEnvironment.Accounts)) {
            if (Test-WindowsPrincipal -Principal $account.SqlPrincipal) {
                Add-Result -Check "Conta $($currentEnvironment.Name)/$($account.Role)" -Status Passed -Detail 'Principal Windows resolvido.'
            } else {
                Add-Result -Check "Conta $($currentEnvironment.Name)/$($account.Role)" -Status Blocked -Detail 'Principal nao provisionado; nenhum segredo foi solicitado.'
            }
        }
    }

    if (@($Environments | Where-Object { $_.Provisioning.RequiresBitLocker }).Count -eq 0) {
        Add-Result -Check 'BitLocker' -Status NotChecked -Detail 'Nao aplicavel ao Development workstation nesta fase.'
    } else {
        try {
        $bitLockerVolumes = @(Get-BitLockerVolume -ErrorAction Stop)
        $unprotected = @($bitLockerVolumes | Where-Object { $_.VolumeStatus -ne 'FullyEncrypted' -or $_.ProtectionStatus -ne 'On' })
        if ($bitLockerVolumes.Count -gt 0 -and $unprotected.Count -eq 0) {
            Add-Result -Check 'BitLocker' -Status Passed -Detail 'Volumes protegidos e criptografados.'
        } else {
            Add-Result -Check 'BitLocker' -Status Blocked -Detail 'BitLocker nao esta comprovadamente ativo em todos os volumes.'
        }
        } catch {
            Add-Result -Check 'BitLocker' -Status Blocked -Detail 'Consulta nao disponivel sem permissao administrativa ou volume BitLocker.'
        }
    }

    if (@($Environments | Where-Object { $_.Iis.Enabled }).Count -eq 0) {
        Add-Result -Check 'IIS baseline' -Status NotChecked -Detail 'Nao aplicavel ao Development workstation; o projeto usa Kestrel local.'
    } elseif (Get-Command Get-Website -ErrorAction SilentlyContinue) {
        Add-Result -Check 'IIS baseline' -Status NotChecked -Detail 'Modulo IIS presente; informe site e certificado reais antes de Apply.'
    } else {
        Add-Result -Check 'IIS baseline' -Status Blocked -Detail 'IIS/WebAdministration nao instalado neste host.'
    }

    foreach ($currentEnvironment in $Environments) {
        if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
            Add-Result -Check "SQL $($currentEnvironment.Name)" -Status Blocked -Detail 'sqlcmd nao esta disponivel.'
            continue
        }
        try {
            $missingDatabases = New-Object 'System.Collections.Generic.List[string]'
            foreach ($databaseName in @($currentEnvironment.Sql.RequiredDatabases)) {
                $databaseQuery = "SET NOCOUNT ON; SELECT CASE WHEN DB_ID($(ConvertTo-SqlLiteral -Value $databaseName)) IS NULL THEN N'0' ELSE N'1' END;"
                $databaseOutput = Invoke-SqlCommand -Server $currentEnvironment.Sql.Server -Query $databaseQuery
                $databaseExists = @($databaseOutput | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_ -in @('0', '1') } | Select-Object -Last 1)
                if ($databaseExists.Count -ne 1 -or $databaseExists[0] -eq '0') {
                    $null = $missingDatabases.Add($databaseName)
                }
            }
            if ($missingDatabases.Count -gt 0) {
                Add-Result -Check "SQL $($currentEnvironment.Name)" -Status Failed -Detail 'Banco obrigatorio da baseline nao existe.'
                continue
            }
            $query = "SET NOCOUNT ON; SELECT CAST(ISNULL(SERVERPROPERTY('Edition'), '') AS nvarchar(128)) + N'|' + CAST(ISNULL(SERVERPROPERTY('IsIntegratedSecurityOnly'), 0) AS nvarchar(10));"
            $output = Invoke-SqlCommand -Server $currentEnvironment.Sql.Server -Query $query
            $metadata = @($output | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_ -match '\|' } | Select-Object -Last 1)
            if ($metadata.Count -ne 1) {
                throw 'metadata SQL ausente'
            }
            $parts = $metadata[0].Split('|')
            $edition = $parts[0]
            $integratedOnly = $parts[1]
            $windowsAuthenticationSatisfied = ($currentEnvironment.Sql.RequiredEdition -eq 'LocalDB' -and
                $currentEnvironment.Sql.Server -match '\(localdb\)') -or $integratedOnly -eq '1'
            if ($currentEnvironment.Sql.RequiredEdition -eq 'Express' -and $edition -notmatch 'Express') {
                Add-Result -Check "SQL $($currentEnvironment.Name)" -Status Failed -Detail 'A edicao retornada nao e SQL Express.'
            } elseif ($currentEnvironment.Sql.RequireWindowsAuthenticationOnly -and -not $windowsAuthenticationSatisfied) {
                Add-Result -Check "SQL $($currentEnvironment.Name)" -Status Failed -Detail 'SQL nao esta comprovadamente em Windows Authentication only.'
            } else {
                $authDetail = if ($windowsAuthenticationSatisfied -and $currentEnvironment.Sql.RequiredEdition -eq 'LocalDB') { 'implicit(LocalDB)' } else { $integratedOnly }
                Add-Result -Check "SQL $($currentEnvironment.Name)" -Status Passed -Detail "Edicao=$edition; WindowsAuthenticationOnly=$authDetail."
            }
        } catch {
            Add-Result -Check "SQL $($currentEnvironment.Name)" -Status Blocked -Detail 'Instancia indisponivel ou consulta requer ambiente administrativo.'
        }
    }
}

function Invoke-Apply {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Baseline,
        [Parameter(Mandatory = $true)]
        [hashtable[]]$Environments,
        [Parameter(Mandatory = $true)]
        [string]$EffectiveStateRoot
    )

    Assert-Administrator -Environments $Environments
    foreach ($currentEnvironment in $Environments) {
        if (-not $currentEnvironment.Provisioning.RequiresServiceAccounts) {
            continue
        }
        foreach ($account in @($currentEnvironment.Accounts)) {
            if (-not (Test-WindowsPrincipal -Principal $account.SqlPrincipal)) {
                throw "Conta Windows ausente; Apply nao cria senhas nem contas implicitamente: $($currentEnvironment.Name)/$($account.Role)."
            }
        }
    }

    $statePath = Get-StateFilePath -Root $EffectiveStateRoot
    $state = if (Test-Path -LiteralPath $statePath -PathType Leaf) { Read-HardeningState -Path $statePath } else { New-HardeningState }
    Save-HardeningState -State $state -Path $statePath

    if ($ConfigureCertificateAcl) {
        Invoke-CertificateKeyAclApply -State $state
        Save-HardeningState -State $state -Path $statePath
    } else {
        Add-Result -Check 'ACL da chave privada do certificado' -Status NotChecked -Detail 'Use -ConfigureCertificateAcl com thumbprint e principal Windows para aplicar a ACL da chave do servico.'
    }

    foreach ($currentEnvironment in $Environments) {
        foreach ($path in @(Get-EnvironmentPaths -CurrentEnvironment $currentEnvironment)) {
            Ensure-Directory -Path $path -State $state
        }
        Save-HardeningState -State $state -Path $statePath
        foreach ($path in @(Get-EnvironmentPaths -CurrentEnvironment $currentEnvironment)) {
            Set-DirectoryAcl -CurrentEnvironment $currentEnvironment -Path $path -State $state
        }
        Save-HardeningState -State $state -Path $statePath
    }

    $firewallEnvironments = @($Environments | Where-Object { $_.Network.ConfigureFirewall })
    if ($firewallEnvironments.Count -gt 0) {
        Assert-Administrator -Environments $firewallEnvironments
        Invoke-FirewallApply -Baseline $Baseline -Environments $firewallEnvironments -State $state
    } else {
        Add-Result -Check 'Firewall apply' -Status NotChecked -Detail 'Nao aplicavel ao Development workstation; nenhuma regra foi alterada.'
    }
    Save-HardeningState -State $state -Path $statePath
    $serverEnvironments = @($Environments | Where-Object { $_.Provisioning.Mode -eq 'Server' })
    if ($EnableTlsRegistryChanges -and $serverEnvironments.Count -gt 0) {
        Invoke-TlsApply -Baseline $Baseline -State $state
        Save-HardeningState -State $state -Path $statePath
    } else {
        Add-Result -Check 'TLS Schannel registry' -Status NotChecked -Detail 'Nao aplicavel ao Development workstation ou nao solicitado; requer janela aprovada e reinicio no servidor.'
    }
    if ($ConfigureIis) {
        foreach ($currentEnvironment in $Environments) {
            if ($currentEnvironment.Iis.Enabled) {
                Invoke-IisApply -CurrentEnvironment $currentEnvironment -State $state
                Save-HardeningState -State $state -Path $statePath
            } else {
                Add-Result -Check "IIS $($currentEnvironment.Name)" -Status NotChecked -Detail 'Nao aplicavel ao Development workstation; o projeto usa Kestrel local.'
            }
        }
    } else {
        Add-Result -Check 'IIS baseline apply' -Status NotChecked -Detail 'Use -ConfigureIis somente com site/certificado provisionados; o script nao publica sites.'
    }
    if ($ConfigureSql) {
        foreach ($currentEnvironment in $Environments) {
            if ($currentEnvironment.Provisioning.Mode -eq 'Workstation') {
                Add-Result -Check "SQL $($currentEnvironment.Name)" -Status NotChecked -Detail 'LocalDB usa Windows Authentication implicita; nenhum login SQL de servico foi criado.'
            } else {
                Invoke-SqlBaseline -CurrentEnvironment $currentEnvironment -State $state
                Save-HardeningState -State $state -Path $statePath
            }
        }
    } else {
        Add-Result -Check 'SQL Windows Authentication apply' -Status NotChecked -Detail 'Use -ConfigureSql em instancia SQL Express existente; conexao usa somente -E.'
    }
    Add-Result -Check 'Apply' -Status Passed -Detail "Baseline aplicada; estado sem segredos em $statePath."
}

function Restore-RegistryValue {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Snapshot
    )

    if ($Snapshot.ValueExists) {
        if (-not (Test-Path -LiteralPath $Snapshot.Path)) {
            New-Item -Path $Snapshot.Path -Force | Out-Null
        }
        Set-ItemProperty -LiteralPath $Snapshot.Path -Name $Snapshot.Name -Value $Snapshot.Value -Force
    } elseif (Test-Path -LiteralPath $Snapshot.Path) {
        Remove-ItemProperty -LiteralPath $Snapshot.Path -Name $Snapshot.Name -ErrorAction SilentlyContinue
    }
}

function Invoke-Rollback {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable[]]$Environments,
        [Parameter(Mandatory = $true)]
        [string]$EffectiveStateRoot
    )

    Assert-Administrator -Environments $Environments
    $statePath = Get-StateFilePath -Root $EffectiveStateRoot
    $state = Read-HardeningState -Path $statePath
    if ($RollbackSql) {
        foreach ($currentEnvironment in $Environments) {
            Invoke-SqlRollback -CurrentEnvironment $currentEnvironment -State $state
        }
    } else {
        Add-Result -Check 'Rollback SQL' -Status NotChecked -Detail 'Logins criados pela etapa permanecem; use -RollbackSql apos confirmar dependencias.'
    }

    if ($state.PSObject.Properties.Name -contains 'CertificateKeyAcls') {
        foreach ($snapshot in @($state.CertificateKeyAcls)) {
            if (-not (Test-Path -LiteralPath $snapshot.Path -PathType Leaf)) {
                throw "Arquivo de chave privada ausente para rollback: $($snapshot.Path)."
            }
            if ($PSCmdlet.ShouldProcess($snapshot.Path, 'restaurar ACL anterior da chave privada')) {
                Set-DaclFromSddl -Path $snapshot.Path -Sddl $snapshot.Sddl
            }
        }
        if (@($state.CertificateKeyAcls).Count -gt 0) {
            Add-Result -Check 'Rollback ACL da chave privada' -Status Passed -Detail 'ACLs de chave privada restauradas a partir dos snapshots sem armazenar certificados ou chaves.'
        } else {
            Add-Result -Check 'Rollback ACL da chave privada' -Status NotChecked -Detail 'Nenhum snapshot de ACL de chave privada foi registrado.'
        }
    } else {
        Add-Result -Check 'Rollback ACL da chave privada' -Status NotChecked -Detail 'Estado anterior a Etapa 15 nao possui snapshots de ACL de chave privada.'
    }

    foreach ($ruleName in @($state.CreatedFirewallRules)) {
        if ($PSCmdlet.ShouldProcess($ruleName, 'remover regra de firewall criada pela Etapa 07')) {
            Remove-NetFirewallRule -Name $ruleName -ErrorAction SilentlyContinue
        }
    }
    foreach ($snapshot in @($state.RegistryValues)) {
        if ($PSCmdlet.ShouldProcess($snapshot.Path, "restaurar registro TLS $($snapshot.Name)")) {
            Restore-RegistryValue -Snapshot $snapshot
        }
    }
    if (@($state.IisBackups).Count -gt 0) {
        $appcmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
        if (-not (Test-Path -LiteralPath $appcmd -PathType Leaf)) {
            throw 'appcmd.exe nao esta disponivel para restaurar a configuracao IIS.'
        }
        foreach ($iisBackup in @($state.IisBackups)) {
            if ($PSCmdlet.ShouldProcess($iisBackup.Name, 'restaurar backup IIS da Etapa 07')) {
                $previousPreference = $ErrorActionPreference
                try {
                    $ErrorActionPreference = 'Continue'
                    $null = @(& $appcmd restore backup $iisBackup.Name 2>&1)
                } finally {
                    $ErrorActionPreference = $previousPreference
                }
                if ($LASTEXITCODE -ne 0) {
                    throw "Falha ao restaurar backup IIS: $($iisBackup.Name)."
                }
            }
        }
    }
    foreach ($aclSnapshot in @($state.Acls)) {
        if ($PSCmdlet.ShouldProcess($aclSnapshot.Path, 'restaurar ACL anterior')) {
            Set-DaclFromSddl -Path $aclSnapshot.Path -Sddl $aclSnapshot.Sddl
        }
    }
    foreach ($directory in @($state.CreatedDirectories | Sort-Object Length -Descending)) {
        if (Test-Path -LiteralPath $directory -PathType Container) {
            $children = @(Get-ChildItem -LiteralPath $directory -Force)
            if ($children.Count -eq 0 -and $PSCmdlet.ShouldProcess($directory, 'remover diretorio vazio criado pela Etapa 07')) {
                Remove-Item -LiteralPath $directory -Force
            }
        }
    }
    Add-Result -Check 'Rollback' -Status Passed -Detail 'Recursos criados pela etapa foram removidos/restaurados; dados nao foram apagados.'
}

try {
    if (-not (Test-Path -LiteralPath $BaselinePath -PathType Leaf)) {
        throw "Baseline nao encontrada: $BaselinePath."
    }
    $baseline = Import-PowerShellDataFile -LiteralPath $BaselinePath
    $allEnvironments = @($baseline.Environments)
    $selectedEnvironments = @(Get-SelectedEnvironments -AllEnvironments $allEnvironments -Names $Environment)
    Assert-Baseline -Baseline $baseline -Environments $allEnvironments
    Assert-CertificateAclParameters
    if ([string]::IsNullOrWhiteSpace($StateRoot)) {
        $workstationOnly = @($selectedEnvironments | Where-Object { $_.Provisioning.Mode -eq 'Workstation' }).Count -eq $selectedEnvironments.Count
        $effectiveStateRoot = if ($workstationOnly -and $baseline.ContainsKey('WorkstationStateRoot')) {
            $baseline.WorkstationStateRoot
        } else {
            $baseline.StateRoot
        }
    } else {
        $effectiveStateRoot = $StateRoot
    }
    Add-Result -Check 'Baseline estrutural' -Status Passed -Detail 'Ambientes, bancos, portas, raizes e ausencia de segredos validados.'

    if ($Mode -eq 'Validate') {
        Invoke-NegativeTests -Environments $allEnvironments
        Invoke-HostValidation -Baseline $baseline -Environments $selectedEnvironments
    } elseif ($Mode -eq 'Apply') {
        Invoke-Apply -Baseline $baseline -Environments $selectedEnvironments -EffectiveStateRoot $effectiveStateRoot
    } elseif ($Mode -eq 'Rollback') {
        Invoke-Rollback -Environments $selectedEnvironments -EffectiveStateRoot $effectiveStateRoot
    }

    $blockedCount = @($script:Results | Where-Object Status -eq 'Blocked').Count
    $failedCount = @($script:Results | Where-Object Status -eq 'Failed').Count
    $summary = [pscustomobject]@{
        Mode = $Mode
        Environments = ($selectedEnvironments | ForEach-Object { $_.Name }) -join ','
        Passed = @($script:Results | Where-Object Status -eq 'Passed').Count
        Blocked = $blockedCount
        Failed = $failedCount
        SecretsLogged = $false
    }
    if ($Json) {
        $checks = @($script:Results | ForEach-Object {
                [pscustomobject]@{
                    Check = [string]$_.Check
                    Status = [string]$_.Status
                    Detail = [string]$_.Detail
                }
            })
        [pscustomobject]@{ Summary = $summary; Checks = $checks } | ConvertTo-Json -Depth 10
    } else {
        $script:Results | Format-Table -AutoSize | Out-String -Width 220 | Write-Output
        $summary | Format-List | Out-String -Width 220 | Write-Output
    }
    if ($failedCount -gt 0 -or ($FailOnBlocked -and $blockedCount -gt 0)) {
        throw 'A validacao encontrou falhas ou bloqueios conforme o modo solicitado.'
    }
} catch {
    Write-Error $_.Exception.Message
    exit 1
}
