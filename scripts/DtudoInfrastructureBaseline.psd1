@{
    SchemaVersion = 1
    Name = 'Dtudo2026'
    StateRoot = 'C:\ProgramData\Dtudo2026\Etapa07'
    WorkstationStateRoot = '%LOCALAPPDATA%\Dtudo2026\Etapa07'
    Environments = @(
        @{
            Name = 'Development'
            Provisioning = @{
                Mode = 'Workstation'
                RequiresAdministrator = $false
                RequiresServiceAccounts = $false
                RequiresIis = $false
                RequiresBitLocker = $false
            }
            Root = '%LOCALAPPDATA%\Dtudo2026\Development'
            ApplicationRoot = '%LOCALAPPDATA%\Programs\Dtudo2026\Development'
            DataRoot = '%LOCALAPPDATA%\Dtudo2026\Development\Data'
            SecretsRoot = '%LOCALAPPDATA%\Dtudo2026\Development\Secrets'
            BackupRoot = 'D:\Dtudo2026-Backups\Development'
            Network = @{
                Exposure = 'Loopback'
                ConfigureFirewall = $false
                GatewayHttpsPort = 8443
                InternalPorts = @(63980, 7146, 5341)
                AllowedRemoteAddresses = @('127.0.0.1', '::1')
                PublicPorts = @()
            }
            Sql = @{
                Server = '(localdb)\MSSQLLocalDB'
                MyAnimesDatabase = 'Dtudo2026Db'
                IdentityDatabase = 'Dtudo2026IdentityDb_Development'
                RequiredDatabases = @('Dtudo2026Db')
                RequiredEdition = 'LocalDB'
                RequireWindowsAuthenticationOnly = $true
                TcpEnabled = $false
                ServiceAccount = ''
            }
            Accounts = @(
                @{ Role = 'InteractiveUser'; LocalName = 'CURRENT_USER'; SqlPrincipal = 'CURRENT_USER' }
            )
            DatabaseAccess = @(
                @{ AccountRole = 'InteractiveUser'; Database = 'MyAnimesDatabase'; Roles = @('ExistingLocalDbOwner') }
            )
            Iis = @{
                Enabled = $false
                SiteName = 'DtudoGateway-Development'
                HostName = 'localhost'
                CertificateStore = 'LocalMachine\My'
                CertificateThumbprint = ''
                EnableHsts = $false
            }
        }
        @{
            Name = 'Homologation'
            Provisioning = @{
                Mode = 'Server'
                RequiresAdministrator = $true
                RequiresServiceAccounts = $true
                RequiresIis = $true
                RequiresBitLocker = $true
            }
            Root = 'C:\ProgramData\Dtudo2026\Homologation'
            ApplicationRoot = 'C:\Program Files\Dtudo2026\Homologation'
            DataRoot = 'C:\ProgramData\Dtudo2026\Homologation\Data'
            SecretsRoot = 'C:\ProgramData\Dtudo2026\Homologation\Secrets'
            BackupRoot = 'D:\Dtudo2026-Backups\Homologation'
            Network = @{
                Exposure = 'Loopback'
                ConfigureFirewall = $true
                GatewayHttpsPort = 16443
                InternalPorts = @(16080, 16081, 15341)
                AllowedRemoteAddresses = @('127.0.0.1', '::1')
                PublicPorts = @()
            }
            Sql = @{
                Server = '.\SQLEXPRESS'
                MyAnimesDatabase = 'Dtudo2026Db_Homologation'
                IdentityDatabase = 'Dtudo2026IdentityDb_Homologation'
                RequiredDatabases = @('Dtudo2026Db_Homologation', 'Dtudo2026IdentityDb_Homologation')
                RequiredEdition = 'Express'
                RequireWindowsAuthenticationOnly = $true
                TcpEnabled = $false
                ServiceAccount = 'NT SERVICE\MSSQL$SQLEXPRESS'
            }
            Accounts = @(
                @{ Role = 'ApiMyAnimes'; LocalName = 'DtudoHomAnimes'; SqlPrincipal = '.\DtudoHomAnimes' }
                @{ Role = 'ApiMyAnimeList'; LocalName = 'DtudoHomAnimeList'; SqlPrincipal = '.\DtudoHomAnimeList' }
                @{ Role = 'Gateway'; LocalName = 'DtudoHomGateway'; SqlPrincipal = '.\DtudoHomGateway' }
                @{ Role = 'FileStorage'; LocalName = 'DtudoHomFiles'; SqlPrincipal = '.\DtudoHomFiles' }
                @{ Role = 'Backup'; LocalName = 'DtudoHomBackup'; SqlPrincipal = '.\DtudoHomBackup' }
            )
            DatabaseAccess = @(
                @{ AccountRole = 'ApiMyAnimes'; Database = 'MyAnimesDatabase'; Roles = @('db_datareader', 'db_datawriter') }
                @{ AccountRole = 'Backup'; Database = 'MyAnimesDatabase'; Roles = @('db_backupoperator') }
                @{ AccountRole = 'Backup'; Database = 'IdentityDatabase'; Roles = @('db_backupoperator') }
            )
            Iis = @{
                Enabled = $true
                SiteName = 'DtudoGateway-Homologation'
                HostName = 'homologacao.dtudo.local'
                CertificateStore = 'LocalMachine\My'
                CertificateThumbprint = ''
                EnableHsts = $true
            }
        }
        @{
            Name = 'Production'
            Provisioning = @{
                Mode = 'Server'
                RequiresAdministrator = $true
                RequiresServiceAccounts = $true
                RequiresIis = $true
                RequiresBitLocker = $true
            }
            Root = 'C:\ProgramData\Dtudo2026\Production'
            ApplicationRoot = 'C:\Program Files\Dtudo2026\Production'
            DataRoot = 'C:\ProgramData\Dtudo2026\Production\Data'
            SecretsRoot = 'C:\ProgramData\Dtudo2026\Production\Secrets'
            BackupRoot = 'D:\Dtudo2026-Backups\Production'
            Network = @{
                Exposure = 'PublicGatewayOnly'
                ConfigureFirewall = $true
                GatewayHttpsPort = 443
                InternalPorts = @(17080, 17081, 25341)
                AllowedRemoteAddresses = @('Any')
                PublicPorts = @(443)
            }
            Sql = @{
                Server = '.\SQLEXPRESS'
                MyAnimesDatabase = 'Dtudo2026Db_Production'
                IdentityDatabase = 'Dtudo2026IdentityDb_Production'
                RequiredDatabases = @('Dtudo2026Db_Production', 'Dtudo2026IdentityDb_Production')
                RequiredEdition = 'Express'
                RequireWindowsAuthenticationOnly = $true
                TcpEnabled = $false
                ServiceAccount = 'NT SERVICE\MSSQL$SQLEXPRESS'
            }
            Accounts = @(
                @{ Role = 'ApiMyAnimes'; LocalName = 'DtudoProdAnimes'; SqlPrincipal = '.\DtudoProdAnimes' }
                @{ Role = 'ApiMyAnimeList'; LocalName = 'DtudoProdAnimeList'; SqlPrincipal = '.\DtudoProdAnimeList' }
                @{ Role = 'Gateway'; LocalName = 'DtudoProdGateway'; SqlPrincipal = '.\DtudoProdGateway' }
                @{ Role = 'FileStorage'; LocalName = 'DtudoProdFiles'; SqlPrincipal = '.\DtudoProdFiles' }
                @{ Role = 'Backup'; LocalName = 'DtudoProdBackup'; SqlPrincipal = '.\DtudoProdBackup' }
            )
            DatabaseAccess = @(
                @{ AccountRole = 'ApiMyAnimes'; Database = 'MyAnimesDatabase'; Roles = @('db_datareader', 'db_datawriter') }
                @{ AccountRole = 'Backup'; Database = 'MyAnimesDatabase'; Roles = @('db_backupoperator') }
                @{ AccountRole = 'Backup'; Database = 'IdentityDatabase'; Roles = @('db_backupoperator') }
            )
            Iis = @{
                Enabled = $true
                SiteName = 'DtudoGateway-Production'
                HostName = 'dtudo.example.invalid'
                CertificateStore = 'LocalMachine\My'
                CertificateThumbprint = ''
                EnableHsts = $true
            }
        }
    )
    Firewall = @{
        BlockSqlTcpPorts = @(1433)
        BlockSqlUdpPorts = @(1434)
        RulePrefix = 'Dtudo2026-Etapa07'
    }
    Tls = @{
        DisabledServerProtocols = @('TLS 1.0', 'TLS 1.1')
        EnabledServerProtocols = @('TLS 1.2')
        MinimumCertificateDays = 30
        CipherPolicyRequired = $true
    }
}
