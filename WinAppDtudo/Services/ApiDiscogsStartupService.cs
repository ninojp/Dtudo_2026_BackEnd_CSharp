using System.Diagnostics;

namespace WinAppDtudo.Services;

public sealed class ApiDiscogsStartupService
{
    private static readonly SemaphoreSlim StartupGate = new(1, 1);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly ApiDiscogsHealthCheckService _healthCheckService;

    public ApiDiscogsStartupService(ApiDiscogsHealthCheckService? healthCheckService = null)
    {
        _healthCheckService = healthCheckService ?? new ApiDiscogsHealthCheckService();
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        var configuredUrl = AppConfigurationService.ApiDiscogsBaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("https" or "http"))
        {
            throw new WinAppAuthenticationException(
                "A URL local da ApiDiscogs nao esta configurada corretamente.");
        }

        if ((await _healthCheckService.CheckAsync(cancellationToken)).IsAvailable)
        {
            return;
        }

        await StartupGate.WaitAsync(cancellationToken);
        try
        {
            if ((await _healthCheckService.CheckAsync(cancellationToken)).IsAvailable)
            {
                return;
            }

            var existingProcess = IsApiDiscogsProcessRunning();
            if (!existingProcess)
            {
                await Task.Delay(PollInterval, cancellationToken);
                if ((await _healthCheckService.CheckAsync(cancellationToken)).IsAvailable)
                {
                    return;
                }

                existingProcess = IsApiDiscogsProcessRunning();
            }

            if (!existingProcess)
            {
                StartApiDiscogs(baseUri);
            }

            var deadline = DateTimeOffset.UtcNow + StartupTimeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ((await _healthCheckService.CheckAsync(cancellationToken)).IsAvailable)
                {
                    return;
                }

                await Task.Delay(PollInterval, cancellationToken);
            }

            throw new WinAppAuthenticationException(
                $"A ApiDiscogs nao ficou disponivel em {baseUri}. " +
                "Verifique a configuracao local e o User Secret de Development.");
        }
        finally
        {
            StartupGate.Release();
        }
    }

    private static void StartApiDiscogs(Uri baseUri)
    {
        var configuredStartUrl = AppConfigurationService.ApiDiscogsAutoStartUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(configuredStartUrl, UriKind.Absolute, out var startUri)
            || startUri.Scheme is not ("https" or "http"))
        {
            throw new WinAppAuthenticationException(
                "A URL de inicializacao da ApiDiscogs nao esta configurada corretamente.");
        }

        var solutionRoot = FindSolutionRoot();
        var projectPath = solutionRoot is null
            ? null
            : Path.Combine(solutionRoot.FullName, "ApiDiscogs", "ApiDiscogs.csproj");
        if (projectPath is null || !File.Exists(projectPath))
        {
            throw new WinAppAuthenticationException(
                "Nao foi possivel localizar o projeto ApiDiscogs na solucao local.");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = solutionRoot!.FullName,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--no-launch-profile");
            startInfo.ArgumentList.Add("--urls");
            startInfo.ArgumentList.Add(startUri.GetLeftPart(UriPartial.Authority));
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

            if (Process.Start(startInfo) is null)
            {
                throw new WinAppAuthenticationException(
                    "Nao foi possivel iniciar o processo local da ApiDiscogs.");
            }
        }
        catch (WinAppAuthenticationException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new WinAppAuthenticationException(
                "Nao foi possivel iniciar a ApiDiscogs local.",
                exception);
        }
    }

    private static bool IsApiDiscogsProcessRunning()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("ApiDiscogs"))
            {
                using (process)
                {
                    if (!process.HasExited)
                    {
                        return true;
                    }
                }
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }

        return false;
    }

    private static DirectoryInfo? FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Dtudo2026.slnx")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
