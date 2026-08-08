[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidateSet('Validate', 'Apply', 'Renew', 'Rollback')]
    [string]$Mode = 'Validate',

    [string]$Hostname = $env:DTUDO_HOMOLOGATION_HOSTNAME,

    [string]$GatewayRoot = 'C:\Program Files\Dtudo2026\Homologation\Gateway',

    [string]$StaticDistPath,

    [int]$GatewayPort = 16443,

    [int[]]$InternalPorts = @(16080, 16081, 15341),

    [string]$CertificateThumbprint = $env:DTUDO_HOMOLOGATION_CERT_THUMBPRINT,

    [string]$AcmeClientPath = "$env:ProgramFiles\win-acme\wacs.exe",

    [string]$AcmeEmail = $env:DTUDO_ACME_EMAIL,

    [string]$RenewalName,

    [string]$SiteName = 'DtudoGateway-Homologation',

    [string]$AppPoolName = 'DtudoGateway-Homologation',

    [string]$StatePath = 'C:\ProgramData\Dtudo2026\Homologation\Edge\state.json',

    [switch]$ProvisionCertificate,

    [switch]$Json
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$script:Results = New-Object 'System.Collections.Generic.List[object]'

if ([string]::IsNullOrWhiteSpace($StaticDistPath)) {
    $StaticDistPath = Join-Path $PSScriptRoot '..\DtudoSite\dist'
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

function Test-IsAdministrator {
    return ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-HomologationScope {
    foreach ($value in @($GatewayRoot, $StatePath, $StaticDistPath, $SiteName, $AppPoolName)) {
        if ([string]$value -match '(?i)production|prod') {
            throw 'O runner da Etapa 27 aceita somente caminhos e nomes de Homologation.'
        }
    }
}

function Test-RealHostname {
    if ([string]::IsNullOrWhiteSpace($Hostname)) {
        return $false
    }
    if ($Hostname -match '[/:\\]' -or $Hostname -notmatch '\.' -or $Hostname -match '(?i)(\.invalid|\.local)$') {
        return $false
    }
    return $true
}

function Test-StaticCatalogBuild {
    if (-not (Test-Path -LiteralPath $StaticDistPath -PathType Container)) {
        Add-Result -Check 'Build static catalog' -Status Blocked -Detail 'DtudoSite/dist nao existe; execute npm run build:homologation.'
        return
    }

    $files = @(Get-ChildItem -LiteralPath $StaticDistPath -Recurse -File)
    if (@($files | Where-Object Name -eq 'index.html').Count -ne 1) {
        Add-Result -Check 'Build static catalog' -Status Failed -Detail 'O dist nao contem exatamente um index.html.'
        return
    }

    $forbiddenPatterns = @(
        'auth/login',
        'bff/login',
        'mymusicx',
        'ninoti',
        'apiLocal',
        'swagger',
        'Seq:Url',
        'clientsecret',
        'connectionstring',
        'access_token',
        'refresh_token',
        'localhost:3666',
        'localhost:4010',
        'Hentai +18',
        'hentai'
    )
    $violations = New-Object 'System.Collections.Generic.List[string]'
    foreach ($file in $files) {
        $content = [System.IO.File]::ReadAllText($file.FullName)
        foreach ($pattern in $forbiddenPatterns) {
            if ($content.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $null = $violations.Add($pattern)
            }
        }
    }

    if ($violations.Count -gt 0) {
        Add-Result -Check 'Build static catalog' -Status Failed -Detail "Padroes proibidos encontrados: $($violations | Sort-Object -Unique -join ', ')."
        return
    }

    Add-Result -Check 'Build static catalog' -Status Passed -Detail 'Artefato catalog-only sem rotas privadas, servicos internos, segredos ou conteudo adulto.'
}

function Test-RequiredCommands {
    $commands = @('Get-Website', 'Get-WebBinding', 'Get-NetFirewallRule', 'Get-NetTCPConnection')
    $missing = @($commands | Where-Object { -not (Get-Command $_ -ErrorAction SilentlyContinue) })
    if ($missing.Count -gt 0) {
        Add-Result -Check 'IIS e rede do host' -Status Blocked -Detail "Comandos indisponiveis: $($missing -join ', ')."
        return $false
    }

    Add-Result -Check 'IIS e rede do host' -Status Passed -Detail 'WebAdministration e cmdlets de firewall/listeners disponiveis.'
    return $true
}

function Test-InternalBindings {
    if (-not (Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue)) {
        Add-Result -Check 'Bindings internos' -Status Blocked -Detail 'Get-NetTCPConnection indisponivel neste host.'
        return
    }

    $externalListeners = New-Object 'System.Collections.Generic.List[string]'
    $observed = $false
    foreach ($port in $InternalPorts) {
        $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue)
        if ($listeners.Count -gt 0) {
            $observed = $true
        }
        foreach ($listener in $listeners) {
            if ($listener.LocalAddress -notin @('127.0.0.1', '::1')) {
                $null = $externalListeners.Add("$($listener.LocalAddress):$port")
            }
        }
    }

    if ($externalListeners.Count -gt 0) {
        Add-Result -Check 'Bindings internos' -Status Failed -Detail "Listener externo detectado: $($externalListeners -join ', ')."
    } elseif ($observed) {
        Add-Result -Check 'Bindings internos' -Status Passed -Detail 'APIs e Seq observados somente em loopback.'
    } else {
        Add-Result -Check 'Bindings internos' -Status Blocked -Detail 'Nenhum servico interno esta escutando; o binding ainda precisa ser exercitado.'
    }
}

function Test-IisAndCertificate {
    if (-not (Get-Command Get-Website -ErrorAction SilentlyContinue)) {
        Add-Result -Check 'IIS binding e certificado' -Status Blocked -Detail 'IIS/WebAdministration nao esta instalado neste host.'
        return
    }

    Import-Module WebAdministration -ErrorAction Stop
    $site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
    if ($null -eq $site) {
        Add-Result -Check 'IIS binding e certificado' -Status Blocked -Detail "Site $SiteName ainda nao foi provisionado."
        return
    }

    $binding = @(Get-WebBinding -Name $SiteName -Protocol https -ErrorAction SilentlyContinue |
        Where-Object { $_.bindingInformation -like "*:${GatewayPort}:${Hostname}" })
    if ($binding.Count -ne 1) {
        Add-Result -Check 'IIS binding e certificado' -Status Failed -Detail 'Binding HTTPS do host de homologacao nao esta unico.'
        return
    }

    if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
        Add-Result -Check 'IIS binding e certificado' -Status Blocked -Detail 'Thumbprint real nao foi fornecido por fonte externa.'
        return
    }

    $certificatePath = "Cert:\LocalMachine\My\$CertificateThumbprint"
    $certificate = Get-Item -LiteralPath $certificatePath -ErrorAction SilentlyContinue
    if ($null -eq $certificate) {
        Add-Result -Check 'IIS binding e certificado' -Status Blocked -Detail 'Certificado de homologacao nao existe no Certificate Store.'
        return
    }
    if ($certificate.NotAfter -lt [DateTime]::UtcNow.AddDays(30)) {
        Add-Result -Check 'IIS binding e certificado' -Status Failed -Detail 'Certificado expira em menos de 30 dias.'
        return
    }

    Add-Result -Check 'IIS binding e certificado' -Status Passed -Detail 'Binding HTTPS e certificado LocalMachine\\My validos; thumbprint omitido do relatorio.'
}

function Test-AcmeRenewal {
    if (-not (Test-Path -LiteralPath $AcmeClientPath -PathType Leaf)) {
        Add-Result -Check 'Renovacao ACME' -Status Blocked -Detail 'Cliente ACME win-acme nao encontrado no caminho configurado.'
        return
    }

    if (-not (Get-Command Get-ScheduledTask -ErrorAction SilentlyContinue)) {
        Add-Result -Check 'Renovacao ACME' -Status Blocked -Detail 'Task Scheduler nao esta disponivel para comprovar renovacao automatica.'
        return
    }

    $renewalTask = Get-ScheduledTask -TaskName 'Dtudo2026-Etapa27-Homologation' -ErrorAction SilentlyContinue
    if ($null -eq $renewalTask) {
        Add-Result -Check 'Renovacao ACME' -Status Blocked -Detail 'Nenhuma tarefa de renovacao ACME de homologacao foi encontrada.'
        return
    }

    Add-Result -Check 'Renovacao ACME' -Status Passed -Detail 'Cliente ACME e tarefa de renovacao encontrados.'
}

function Test-Firewall {
    if (-not (Get-Command Get-NetFirewallRule -ErrorAction SilentlyContinue)) {
        Add-Result -Check 'Firewall de homologacao' -Status Blocked -Detail 'Cmdlets de firewall indisponiveis neste host.'
        return
    }

    $gatewayRule = Get-NetFirewallRule -Name 'Dtudo2026-Etapa27-Homologation-Gateway' -ErrorAction SilentlyContinue
    $internalRules = @($InternalPorts | ForEach-Object {
            Get-NetFirewallRule -Name "Dtudo2026-Etapa27-Homologation-Internal-$_-Block" -ErrorAction SilentlyContinue
        })
    if ($null -eq $gatewayRule -or $internalRules.Count -ne $InternalPorts.Count) {
        Add-Result -Check 'Firewall de homologacao' -Status Blocked -Detail 'Regras do gateway e bloqueios internos ainda nao foram aplicados.'
        return
    }

    Add-Result -Check 'Firewall de homologacao' -Status Passed -Detail 'Somente a porta do gateway e permitida; APIs e Seq possuem bloqueio externo.'
}

function Save-State {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    $parent = Split-Path -Parent $StatePath
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    $State | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $StatePath -Encoding UTF8
}

function Add-FirewallRule {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Parameters,
        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    if ($null -ne (Get-NetFirewallRule -Name $Parameters.Name -ErrorAction SilentlyContinue)) {
        return
    }
    if ($PSCmdlet.ShouldProcess($Parameters.Name, 'criar regra de firewall de homologacao')) {
        New-NetFirewallRule @Parameters | Out-Null
        $State.CreatedFirewallRules += $Parameters.Name
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
    $existing = @(Get-WebConfigurationProperty -PSPath $IisPath -Filter $filter -Name '.' |
        Where-Object { $_.name -eq $Name })
    if ($existing.Count -eq 0) {
        Add-WebConfigurationProperty -PSPath $IisPath -Filter $filter -Name '.' -Value @{ name = $Name; value = $Value }
    } else {
        Set-WebConfigurationProperty -PSPath $IisPath -Filter "$filter/add[@name='$Name']" -Name value -Value $Value
    }
}

function Invoke-Apply {
    if (-not (Test-IsAdministrator)) {
        throw 'Apply da Etapa 27 exige PowerShell elevado; nenhum host foi alterado.'
    }
    if (-not (Test-RealHostname)) {
        throw 'Informe um hostname real de homologacao por -Hostname ou DTUDO_HOMOLOGATION_HOSTNAME.'
    }
    if (-not (Test-Path -LiteralPath $StaticDistPath -PathType Container)) {
        throw 'Build catalog-only ausente; execute npm run build:homologation antes do Apply.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $StaticDistPath 'index.html') -PathType Leaf)) {
        throw 'Build catalog-only sem index.html; Apply interrompido.'
    }
    if (-not (Test-Path -LiteralPath $AcmeClientPath -PathType Leaf)) {
        throw 'Cliente ACME ausente; Apply nao cria certificado manualmente.'
    }
    if (-not (Test-RequiredCommands)) {
        throw 'IIS/firewall/listeners indisponiveis; Apply interrompido antes de alterar o host.'
    }
    if (-not $ProvisionCertificate) {
        if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
            throw 'Forneca um thumbprint externo ou use -ProvisionCertificate; nenhum binding sera criado sem TLS.'
        }
        $certificate = Get-Item -LiteralPath "Cert:\LocalMachine\My\$CertificateThumbprint" -ErrorAction SilentlyContinue
        if ($null -eq $certificate -or $certificate.NotAfter -lt [DateTime]::UtcNow.AddDays(30)) {
            throw 'Certificado externo ausente ou expira em menos de 30 dias; Apply interrompido antes de alterar o host.'
        }
    }

    Import-Module WebAdministration -ErrorAction Stop
    $appcmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
    if (-not (Test-Path -LiteralPath $appcmd -PathType Leaf)) {
        throw 'appcmd.exe ausente; backup de rollback IIS nao pode ser criado.'
    }

    $stateRoot = Split-Path -Parent $StatePath
    $backupRoot = Join-Path $stateRoot ('rollback-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    $state = [ordered]@{
        SchemaVersion = 1
        Environment = 'Homologation'
        CreatedUtc = [DateTime]::UtcNow.ToString('o')
        IisBackup = "Dtudo2026-Etapa27-Homologation-$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))"
        BackupRoot = $backupRoot
        CreatedFirewallRules = @()
        CreatedSite = $false
        CreatedAppPool = $false
    }
    Save-State -State $state

    if ($PSCmdlet.ShouldProcess($SiteName, 'criar backup da configuracao IIS')) {
        & $appcmd add backup $state.IisBackup | Out-Null
    }

    $webRoot = Join-Path $GatewayRoot 'wwwroot'
    New-Item -ItemType Directory -Path $GatewayRoot -Force | Out-Null
    if (Test-Path -LiteralPath $webRoot -PathType Container) {
        Copy-Item -LiteralPath $webRoot -Destination (Join-Path $backupRoot 'wwwroot') -Recurse -Force
    }
    $webConfigPath = Join-Path $GatewayRoot 'web.config'
    if (Test-Path -LiteralPath $webConfigPath -PathType Leaf) {
        Copy-Item -LiteralPath $webConfigPath -Destination (Join-Path $backupRoot 'web.config') -Force
    }

    if (-not (Test-Path -LiteralPath $webRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $webRoot -Force | Out-Null
    }
    if ($PSCmdlet.ShouldProcess($webRoot, 'publicar build catalog-only')) {
        Remove-Item -LiteralPath (Join-Path $webRoot '*') -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item -Path (Join-Path $StaticDistPath '*') -Destination $webRoot -Recurse -Force
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\deploy\homologation\web.config') -Destination $webConfigPath -Force
    }

    $appPool = Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue
    if ($null -eq $appPool) {
        if ($PSCmdlet.ShouldProcess($AppPoolName, 'criar application pool IIS')) {
            New-WebAppPool -Name $AppPoolName | Out-Null
            $state.CreatedAppPool = $true
        }
    }
    Set-ItemProperty -LiteralPath "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty -LiteralPath "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value 4

    $site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
    if ($null -eq $site) {
        if ($PSCmdlet.ShouldProcess($SiteName, 'criar site IIS HTTPS de homologacao')) {
            New-Website -Name $SiteName -PhysicalPath $GatewayRoot -Port $GatewayPort -HostHeader $Hostname -Ssl -ApplicationPool $AppPoolName | Out-Null
            $state.CreatedSite = $true
        }
    } elseif (-not [System.IO.Path]::GetFullPath($site.PhysicalPath).TrimEnd('\').Equals(
            [System.IO.Path]::GetFullPath($GatewayRoot).TrimEnd('\'), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Site IIS existente aponta para uma raiz diferente da homologacao.'
    }

    $binding = @(Get-WebBinding -Name $SiteName -Protocol https -ErrorAction SilentlyContinue |
        Where-Object { $_.bindingInformation -like "*:${GatewayPort}:${Hostname}" })
    if ($binding.Count -eq 0) {
        New-WebBinding -Name $SiteName -Protocol https -Port $GatewayPort -HostHeader $Hostname -SslFlags 0 | Out-Null
        $binding = @(Get-WebBinding -Name $SiteName -Protocol https |
            Where-Object { $_.bindingInformation -like "*:${GatewayPort}:${Hostname}" })
    }
    if (-not $ProvisionCertificate -and [string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
        throw 'Certificado nao fornecido; binding IIS nao sera ativado sem certificado externo.'
    }
    if (-not $ProvisionCertificate) {
        $binding[0].AddSslCertificate($CertificateThumbprint, 'My')
    }

    $iisPath = "IIS:\Sites\$SiteName"
    Set-WebConfigurationProperty -PSPath $iisPath -Filter 'system.webServer/security/requestFiltering/requestLimits' -Name maxAllowedContentLength -Value 1048576
    Set-WebConfigurationProperty -PSPath $iisPath -Filter 'system.webServer/security/requestFiltering/requestLimits' -Name maxUrl -Value 2048
    Set-WebConfigurationProperty -PSPath $iisPath -Filter 'system.webServer/security/requestFiltering/requestLimits' -Name maxQueryString -Value 1024
    Set-IisHeader -IisPath $iisPath -Name 'X-Content-Type-Options' -Value 'nosniff'
    Set-IisHeader -IisPath $iisPath -Name 'X-Frame-Options' -Value 'DENY'
    Set-IisHeader -IisPath $iisPath -Name 'Referrer-Policy' -Value 'no-referrer'
    Set-IisHeader -IisPath $iisPath -Name 'Permissions-Policy' -Value 'camera=(), microphone=(), geolocation=()'
    Set-IisHeader -IisPath $iisPath -Name 'Content-Security-Policy' -Value "default-src 'self'; img-src 'self' https: data:; connect-src 'self'; style-src 'self'; script-src 'self'; font-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'"
    Set-IisHeader -IisPath $iisPath -Name 'Strict-Transport-Security' -Value 'max-age=31536000; includeSubDomains'
    Remove-WebConfigurationProperty -PSPath $iisPath -Filter 'system.webServer/httpProtocol/customHeaders' -Name '.' -AtElement @{ name = 'X-Powered-By' } -ErrorAction SilentlyContinue

    Add-FirewallRule -State $state -Parameters @{
        Name = 'Dtudo2026-Etapa27-Homologation-Gateway'
        DisplayName = 'Dtudo2026 Etapa27 Homologation gateway HTTPS'
        Direction = 'Inbound'
        Action = 'Allow'
        Protocol = 'TCP'
        LocalPort = $GatewayPort
        RemoteAddress = 'Any'
        Profile = 'Any'
        Description = 'Somente o gateway catalog-only de homologacao e publicado.'
    }
    foreach ($port in $InternalPorts) {
        Add-FirewallRule -State $state -Parameters @{
            Name = "Dtudo2026-Etapa27-Homologation-Internal-$port-Block"
            DisplayName = "Dtudo2026 Etapa27 Homologation bloqueio interno $port"
            Direction = 'Inbound'
            Action = 'Block'
            Protocol = 'TCP'
            LocalPort = $port
            RemoteAddress = 'Any'
            Profile = 'Any'
            Description = 'API interna ou Seq nao recebe conexoes externas.'
        }
        Add-FirewallRule -State $state -Parameters @{
            Name = "Dtudo2026-Etapa27-Homologation-Internal-$port-AllowLoopback"
            DisplayName = "Dtudo2026 Etapa27 Homologation loopback $port"
            Direction = 'Inbound'
            Action = 'Allow'
            Protocol = 'TCP'
            LocalPort = $port
            RemoteAddress = @('127.0.0.1', '::1')
            OverrideBlockRules = $true
            Profile = 'Any'
            Description = 'Somente o gateway no mesmo host acessa o servico interno.'
        }
    }
    Save-State -State $state
    Add-Result -Check 'Apply Homologation' -Status Passed -Detail 'IIS, build, headers, limites, binding e firewall de homologacao aplicados; Production nao foi tocada.'
}

function Invoke-AcmeProvision {
    if (-not (Test-IsAdministrator)) {
        throw 'Provisionamento ACME exige PowerShell elevado.'
    }
    if (-not (Test-RealHostname)) {
        throw 'Hostname real de homologacao ausente ou invalido.'
    }
    if ([string]::IsNullOrWhiteSpace($AcmeEmail)) {
        throw 'Informe um email ACME operacional por variavel DTUDO_ACME_EMAIL; nenhum segredo e solicitado pelo script.'
    }
    if (-not (Test-Path -LiteralPath $AcmeClientPath -PathType Leaf)) {
        throw 'Cliente ACME ausente.'
    }

    Import-Module WebAdministration -ErrorAction Stop
    $site = Get-Website -Name $SiteName -ErrorAction Stop
    $arguments = @(
        '--target', 'iis',
        '--siteid', [string]$site.Id,
        '--host', $Hostname,
        '--installation', 'iis',
        '--store', 'certificatestore',
        '--validationmode', 'dns-01',
        '--friendlyname', "Dtudo2026-Etapa27-Homologation-$Hostname",
        '--taskname', 'Dtudo2026-Etapa27-Homologation',
        '--emailaddress', $AcmeEmail,
        '--accepttos'
    )
    if ($PSCmdlet.ShouldProcess($Hostname, 'provisionar certificado ACME de homologacao')) {
        & $AcmeClientPath @arguments
        if ($LASTEXITCODE -ne 0) {
            throw 'win-acme falhou ao provisionar o certificado; nenhum segredo foi registrado.'
        }
    }
    Add-Result -Check 'Provisionamento ACME' -Status Passed -Detail 'win-acme foi chamado para o host de homologacao com validacao DNS-01; credenciais do provedor permanecem fora do repositorio.'
}

function Invoke-AcmeRenewal {
    if (-not (Test-IsAdministrator)) {
        throw 'Renew exige PowerShell elevado.'
    }
    if ([string]::IsNullOrWhiteSpace($RenewalName)) {
        throw 'Informe -RenewalName da renovacao ACME de Homologation; nao e permitido renovar todos os certificados do host.'
    }
    if ($RenewalName -match '(?i)production|prod') {
        throw 'Renew da Etapa 27 rejeita nomes de renovacao de Production.'
    }
    if (-not (Test-Path -LiteralPath $AcmeClientPath -PathType Leaf)) {
        throw 'Cliente ACME ausente.'
    }

    if ($PSCmdlet.ShouldProcess($RenewalName, 'renovar somente o certificado ACME de homologacao')) {
        & $AcmeClientPath --renew --friendlyname $RenewalName --force
        if ($LASTEXITCODE -ne 0) {
            throw 'win-acme falhou na renovacao da homologacao.'
        }
    }
    Add-Result -Check 'Renovacao ACME' -Status Passed -Detail 'Renovacao direcionada a uma renewal de Homologation; nenhuma renovacao global foi executada.'
}

function Invoke-Rollback {
    if (-not (Test-IsAdministrator)) {
        throw 'Rollback da Etapa 27 exige PowerShell elevado.'
    }
    if (-not (Test-Path -LiteralPath $StatePath -PathType Leaf)) {
        throw 'Estado de rollback da Etapa 27 nao foi encontrado.'
    }

    $state = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
    foreach ($ruleName in @($state.CreatedFirewallRules)) {
        if ($PSCmdlet.ShouldProcess($ruleName, 'remover regra de firewall criada pela Etapa 27')) {
            Remove-NetFirewallRule -Name $ruleName -ErrorAction SilentlyContinue
        }
    }

    $appcmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
    if (@($state.IisBackup).Count -gt 0 -and (Test-Path -LiteralPath $appcmd -PathType Leaf)) {
        if ($PSCmdlet.ShouldProcess($state.IisBackup, 'restaurar backup IIS da Etapa 27')) {
            & $appcmd restore backup $state.IisBackup | Out-Null
        }
    }

    $backupRoot = [string]$state.BackupRoot
    $webRoot = Join-Path $GatewayRoot 'wwwroot'
    $backupWebRoot = Join-Path $backupRoot 'wwwroot'
    if (Test-Path -LiteralPath $backupWebRoot -PathType Container) {
        if ($PSCmdlet.ShouldProcess($webRoot, 'restaurar build anterior')) {
            Remove-Item -LiteralPath $webRoot -Recurse -Force -ErrorAction SilentlyContinue
            Copy-Item -LiteralPath $backupWebRoot -Destination $webRoot -Recurse -Force
        }
    }
    Add-Result -Check 'Rollback Homologation' -Status Passed -Detail 'Firewall e backup IIS restaurados; nenhum banco, segredo ou Production foi alterado.'
}

try {
    Assert-HomologationScope
    switch ($Mode) {
        'Validate' {
            if (Test-RealHostname) {
                Add-Result -Check 'Hostname de homologacao' -Status Passed -Detail 'Hostname real fornecido externamente.'
            } else {
                Add-Result -Check 'Hostname de homologacao' -Status Blocked -Detail 'Use um dominio real de homologacao; example.invalid/local nao pode emitir TLS.'
            }
            Test-StaticCatalogBuild
            $null = Test-RequiredCommands
            Test-InternalBindings
            Test-IisAndCertificate
            Test-AcmeRenewal
            Test-Firewall
        }
        'Apply' {
            Test-StaticCatalogBuild
            if (@($script:Results | Where-Object Status -eq 'Failed').Count -gt 0) {
                throw 'Build catalog-only reprovado; Apply interrompido.'
            }
            Invoke-Apply
            if ($ProvisionCertificate) {
                Invoke-AcmeProvision
            }
        }
        'Renew' {
            Invoke-AcmeRenewal
        }
        'Rollback' {
            Invoke-Rollback
        }
    }
} catch {
    Add-Result -Check $Mode -Status Failed -Detail $_.Exception.Message
}

if ($Json) {
    $script:Results | ConvertTo-Json -Depth 10
} else {
    $script:Results | Format-Table -AutoSize
}

if (@($script:Results | Where-Object Status -eq 'Failed').Count -gt 0) {
    exit 1
}
if ($Mode -eq 'Validate' -and @($script:Results | Where-Object Status -eq 'Blocked').Count -gt 0) {
    exit 2
}
