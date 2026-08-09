using System.Diagnostics;

namespace WinAppDtudo.Services;

public sealed class ApiMyAnimesStartupService
{
    private static readonly SemaphoreSlim StartupGate = new(1, 1);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly ApiMyAnimesHealthCheckService _healthCheckService;

    public ApiMyAnimesStartupService(ApiMyAnimesHealthCheckService? healthCheckService = null)
    {
        _healthCheckService = healthCheckService ?? new ApiMyAnimesHealthCheckService();
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = AppConfigurationService.ApiMyAnimesBaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("https" or "http"))
        {
            throw new WinAppAuthenticationException(
                "A URL local do ApiMyAnimes nao esta configurada corretamente.");
        }

        if ((await _healthCheckService.CheckAsync(cancellationToken)).IsAvailable)
        {
            return;
        }

#if DEBUG
        await StartupGate.WaitAsync(cancellationToken);
        try
        {
            if ((await _healthCheckService.CheckAsync(cancellationToken)).IsAvailable)
            {
                return;
            }

            var existingProcess = IsApiMyAnimesProcessRunning();
            if (!existingProcess)
            {
                StartApiMyAnimes(baseUri);
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
#endif

            throw new WinAppAuthenticationException(
                $"O ApiMyAnimes nao ficou disponivel em {baseUri}. " +
                "Verifique o banco DB_Local e a configuracao local.");
#if DEBUG
        }
        finally
        {
            StartupGate.Release();
        }
#endif
    }

    private static void StartApiMyAnimes(Uri baseUri)
    {
        var solutionRoot = FindSolutionRoot();
        var projectPath = solutionRoot is null
            ? null
            : Path.Combine(solutionRoot.FullName, "ApiMyAnimes", "ApiMyAnimes.csproj");
        if (projectPath is null || !File.Exists(projectPath))
        {
            throw new WinAppAuthenticationException(
                "Nao foi possivel localizar o projeto ApiMyAnimes na solucao local.");
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
            startInfo.ArgumentList.Add(baseUri.GetLeftPart(UriPartial.Authority));
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

            if (Process.Start(startInfo) is null)
            {
                throw new WinAppAuthenticationException(
                    "Nao foi possivel iniciar o processo local do ApiMyAnimes.");
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
                "Nao foi possivel iniciar o ApiMyAnimes local.",
                exception);
        }
    }

    private static bool IsApiMyAnimesProcessRunning()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("ApiMyAnimes"))
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
