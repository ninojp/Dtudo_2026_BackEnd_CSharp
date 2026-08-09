using System.Diagnostics;

namespace WinAppDtudo.Services;

public sealed class ApiMyAnimeListStartupService
{
    private static readonly SemaphoreSlim StartupGate = new(1, 1);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        var configuredUrl = AppConfigurationService.ApiMyAnimeListBaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("https" or "http"))
        {
            throw new WinAppAuthenticationException(
                "A URL local do ApiMyAnimeList nao esta configurada corretamente.");
        }

        if (await IsReachableAsync(baseUri, cancellationToken))
        {
            return;
        }

        await StartupGate.WaitAsync(cancellationToken);
        try
        {
            if (await IsReachableAsync(baseUri, cancellationToken))
            {
                return;
            }

            if (!IsApiMyAnimeListProcessRunning())
            {
                StartApiMyAnimeList(baseUri);
            }

            var deadline = DateTimeOffset.UtcNow + StartupTimeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await IsReachableAsync(baseUri, cancellationToken))
                {
                    return;
                }

                await Task.Delay(PollInterval, cancellationToken);
            }
            throw new WinAppAuthenticationException(
                $"O ApiMyAnimeList nao ficou disponivel em {baseUri}. " +
                "Verifique a configuracao local e o User Secret de Development.");
        }
        finally
        {
            StartupGate.Release();
        }
    }

    private static async Task<bool> IsReachableAsync(
        Uri baseUri,
        CancellationToken cancellationToken)
    {
        using var handler = AppConfigurationService.CreateHttpClientHandler();
        using var client = new HttpClient(handler)
        {
            Timeout = ProbeTimeout
        };

        try
        {
            using var response = await client.GetAsync(
                new Uri(baseUri, "ApiMyAnimeList/health"),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            return (int)response.StatusCode < 500;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private static void StartApiMyAnimeList(Uri baseUri)
    {
        var configuredStartUrl = AppConfigurationService.ApiMyAnimeListAutoStartUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(configuredStartUrl, UriKind.Absolute, out var startUri)
            || startUri.Scheme is not ("https" or "http"))
        {
            throw new WinAppAuthenticationException(
                "A URL de inicializacao do ApiMyAnimeList nao esta configurada corretamente.");
        }

        var solutionRoot = FindSolutionRoot();
        var projectPath = solutionRoot is null
            ? null
            : Path.Combine(solutionRoot.FullName, "ApiMyAnimeList", "ApiMyAnimeList.csproj");
        if (projectPath is null || !File.Exists(projectPath))
        {
            throw new WinAppAuthenticationException(
                "Nao foi possivel localizar o projeto ApiMyAnimeList na solucao local.");
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
                    "Nao foi possivel iniciar o processo local do ApiMyAnimeList.");
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
                "Nao foi possivel iniciar o ApiMyAnimeList local.",
                exception);
        }
    }

    private static bool IsApiMyAnimeListProcessRunning()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("ApiMyAnimeList"))
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
