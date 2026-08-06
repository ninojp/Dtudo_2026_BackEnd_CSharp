using System.Diagnostics;
using Microsoft.Win32;

namespace WinAppDtudo.Services;

public sealed class DtudoSiteStartupService(ApiMyAnimesHealthCheckService apiHealthCheckService) : IDisposable
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan SiteHealthCheckTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private Process? _stackTerminalProcess;
    private string? _npmExecutablePath;

    public async Task<DtudoSiteStartupResult> EnsureReadyAsync(Uri siteUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(siteUri);

        if (!TryResolveDtudoSiteDirectory(out var siteDirectory, out var directoryError))
            return DtudoSiteStartupResult.Failed(directoryError);

        var apiHealth = await apiHealthCheckService.CheckAsync(cancellationToken);
        var siteIsAvailable = await IsSiteAvailableAsync(siteUri, cancellationToken);

        if (apiHealth.IsAvailable && siteIsAvailable)
            return DtudoSiteStartupResult.Ready();

        if (!apiHealth.IsAvailable)
        {
            var localDbStart = await StartLocalDbAsync(cancellationToken);
            if (!localDbStart.Succeeded)
                return localDbStart;
        }

        var npmCheck = await CheckNpmAvailabilityAsync(cancellationToken);
        if (!npmCheck.Succeeded)
            return npmCheck;

        var stackStart = StartStackInTerminal(siteDirectory);
        if (!stackStart.Succeeded)
            return stackStart;

        return await WaitForServicesAsync(siteUri, cancellationToken);
    }

    public DtudoSiteStartupResult OpenInGoogleChrome(Uri siteUri)
    {
        ArgumentNullException.ThrowIfNull(siteUri);

        var chromePath = FindGoogleChromePath();
        if (chromePath is null)
        {
            return DtudoSiteStartupResult.Failed(
                "Google Chrome nao foi encontrado. Instale-o ou defina DTUDO_GOOGLE_CHROME_PATH.");
        }

        try
        {
            var chromeStartInfo = new ProcessStartInfo
            {
                FileName = chromePath,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(chromePath)
            };
            chromeStartInfo.ArgumentList.Add("--new-window");
            chromeStartInfo.ArgumentList.Add(siteUri.AbsoluteUri);

            using var chrome = Process.Start(chromeStartInfo);

            return chrome is null
                ? DtudoSiteStartupResult.Failed("O Google Chrome nao iniciou.")
                : DtudoSiteStartupResult.Ready();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return DtudoSiteStartupResult.Failed($"Nao foi possivel abrir o Google Chrome: {exception.Message}");
        }
    }

    public void Dispose()
    {
        _stackTerminalProcess?.Dispose();
        _stackTerminalProcess = null;
    }

    private async Task<DtudoSiteStartupResult> WaitForServicesAsync(Uri siteUri, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + AppConfigurationService.DtudoSiteStartupTimeout;
        var latestApiHealth = ApiMyAnimesHealthStatus.Unavailable("ApiMyAnimes is still starting.");

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            latestApiHealth = await apiHealthCheckService.CheckAsync(cancellationToken);
            var siteIsAvailable = await IsSiteAvailableAsync(siteUri, cancellationToken);
            if (latestApiHealth.IsAvailable && siteIsAvailable)
                return DtudoSiteStartupResult.Ready();

            await Task.Delay(PollInterval, cancellationToken);
        }

        var siteAddress = siteUri.GetLeftPart(UriPartial.Authority);
        return DtudoSiteStartupResult.Failed(
            $"Os servicos nao ficaram prontos em {AppConfigurationService.DtudoSiteStartupTimeout.TotalSeconds:0} segundos. " +
            $"ApiMyAnimes/DB_Local: {latestApiHealth.Message} Vite: {siteAddress} nao respondeu.");
    }

    private static async Task<bool> IsSiteAvailableAsync(Uri siteUri, CancellationToken cancellationToken)
    {
        try
        {
            using var handler = new HttpClientHandler { UseProxy = false };
            using var client = new HttpClient(handler) { Timeout = SiteHealthCheckTimeout };
            using var response = await client.GetAsync(
                siteUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task<DtudoSiteStartupResult> StartLocalDbAsync(CancellationToken cancellationToken)
    {
        var localDbInstanceName = AppConfigurationService.DtudoLocalDbInstanceName;
        if (string.IsNullOrWhiteSpace(localDbInstanceName))
            return DtudoSiteStartupResult.Failed("A instancia do SQL Server LocalDB nao esta configurada.");

        try
        {
            var command = new ProcessStartInfo
            {
                FileName = "sqllocaldb.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            command.ArgumentList.Add("start");
            command.ArgumentList.Add(localDbInstanceName);

            using var process = Process.Start(command);
            if (process is null)
                return DtudoSiteStartupResult.Failed("Nao foi possivel iniciar o SQL Server LocalDB.");

            var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).WaitAsync(CommandTimeout, cancellationToken);
            var output = (await standardError).Trim();
            if (string.IsNullOrWhiteSpace(output))
                output = (await standardOutput).Trim();

            return process.ExitCode == 0
                ? DtudoSiteStartupResult.Ready()
                : DtudoSiteStartupResult.Failed(
                    $"Nao foi possivel iniciar a instancia LocalDB '{localDbInstanceName}'. {output}".Trim());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return DtudoSiteStartupResult.Failed("O SQL Server LocalDB excedeu o tempo para iniciar.");
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return DtudoSiteStartupResult.Failed($"sqllocaldb.exe nao foi encontrado: {exception.Message}");
        }
    }

    private async Task<DtudoSiteStartupResult> CheckNpmAvailabilityAsync(CancellationToken cancellationToken)
    {
        _npmExecutablePath = FindNpmExecutablePath();
        if (_npmExecutablePath is null)
        {
            return DtudoSiteStartupResult.Failed(
                "npm nao foi encontrado. Instale o Node.js ou defina DTUDO_NPM_PATH com o caminho para npm.cmd.");
        }

        try
        {
            var command = new ProcessStartInfo
            {
                FileName = _npmExecutablePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            command.ArgumentList.Add("--version");

            using var process = Process.Start(command);
            if (process is null)
                return DtudoSiteStartupResult.Failed("Nao foi possivel iniciar o npm.");

            var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).WaitAsync(CommandTimeout, cancellationToken);
            if (process.ExitCode == 0)
                return DtudoSiteStartupResult.Ready();

            var error = (await standardError).Trim();
            return DtudoSiteStartupResult.Failed(
                $"npm nao esta funcional em '{_npmExecutablePath}'. {error}".Trim());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return DtudoSiteStartupResult.Failed("O npm excedeu o tempo de verificacao.");
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return DtudoSiteStartupResult.Failed($"Nao foi possivel executar npm em '{_npmExecutablePath}': {exception.Message}");
        }
    }

    private DtudoSiteStartupResult StartStackInTerminal(string siteDirectory)
    {
        if (_stackTerminalProcess is { HasExited: false })
            return DtudoSiteStartupResult.Ready();

        _stackTerminalProcess?.Dispose();
        _npmExecutablePath ??= FindNpmExecutablePath();
        if (_npmExecutablePath is null)
            return DtudoSiteStartupResult.Failed("npm nao foi encontrado para iniciar o DtudoSite.");

        try
        {
            var command = $"/k \"{_npmExecutablePath}\" run serv";
            _stackTerminalProcess = Process.Start(new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                Arguments = command,
                WorkingDirectory = siteDirectory,
                UseShellExecute = true
            });

            return _stackTerminalProcess is null
                ? DtudoSiteStartupResult.Failed("Nao foi possivel abrir o terminal para iniciar o DtudoSite.")
                : DtudoSiteStartupResult.Ready();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return DtudoSiteStartupResult.Failed($"Nao foi possivel executar 'npm run serv': {exception.Message}");
        }
    }

    private static bool TryResolveDtudoSiteDirectory(out string siteDirectory, out string error)
    {
        var configuredDirectory = AppConfigurationService.DtudoSiteDirectory;
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            siteDirectory = Path.GetFullPath(configuredDirectory);
            error = string.Empty;
        }
        else
        {
            var solutionRoot = FindSolutionRoot();
            if (solutionRoot is null)
            {
                siteDirectory = string.Empty;
                error = "Nao foi possivel localizar a pasta raiz da solucao Dtudo2026. Defina DTUDO_SITE_DIRECTORY.";
                return false;
            }

            siteDirectory = Path.Combine(solutionRoot.FullName, "DtudoSite");
            error = string.Empty;
        }

        if (File.Exists(Path.Combine(siteDirectory, "package.json")))
            return true;

        error = $"A pasta do DtudoSite nao contem package.json: {siteDirectory}";
        return false;
    }

    private static DirectoryInfo? FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Dtudo2026.slnx")))
                return directory;

            directory = directory.Parent;
        }

        return null;
    }

    private static string? FindGoogleChromePath()
    {
        var candidates = new List<string?>
        {
            AppConfigurationService.GoogleChromeExecutablePath,
            ReadChromePathFromRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe"),
            ReadChromePathFromRegistry(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe")
        };

        return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
    }

    private static string? FindNpmExecutablePath()
    {
        var nodeInstallPath = ReadRegistryValue(@"HKEY_LOCAL_MACHINE\Software\Node.js", "InstallPath");
        var candidates = new List<string?>
        {
            AppConfigurationService.NpmExecutablePath,
            nodeInstallPath is null ? null : Path.Combine(nodeInstallPath, "npm.cmd"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "npm.cmd"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "nodejs", "npm.cmd"),
            ResolveNpmFromPath()
        };

        return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
    }

    private static string? ResolveNpmFromPath()
    {
        var pathDirectories = (Environment.GetEnvironmentVariable("Path") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return pathDirectories
            .Select(directory => Path.Combine(directory, "npm.cmd"))
            .FirstOrDefault(File.Exists);
    }

    private static string? ReadChromePathFromRegistry(string keyName)
    {
        try
        {
            return Registry.GetValue(keyName, null, null) as string;
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? ReadRegistryValue(string keyName, string valueName)
    {
        try
        {
            return Registry.GetValue(keyName, valueName, null) as string;
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

public sealed record DtudoSiteStartupResult(bool Succeeded, string Message)
{
    public static DtudoSiteStartupResult Ready() => new(true, string.Empty);

    public static DtudoSiteStartupResult Failed(string message) => new(false, message);
}
