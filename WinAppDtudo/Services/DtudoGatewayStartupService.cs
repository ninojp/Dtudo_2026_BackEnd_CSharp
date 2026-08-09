using System.Diagnostics;

namespace WinAppDtudo.Services;

public sealed class DtudoGatewayStartupService
{
    private static readonly SemaphoreSlim StartupGate = new(1, 1);
    private static readonly TimeSpan HealthTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = AppConfigurationService.DtudoGatewayBaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("https" or "http"))
        {
            throw new WinAppAuthenticationException(
                "A URL local do DtudoGateway nao esta configurada corretamente.");
        }

        if (await IsReadyAsync(baseUri, cancellationToken))
        {
            return;
        }

#if DEBUG
        await StartupGate.WaitAsync(cancellationToken);
        try
        {
            if (await IsReadyAsync(baseUri, cancellationToken))
            {
                return;
            }

            var existingProcess = IsDtudoGatewayProcessRunning();
            if (!existingProcess)
            {
                StartGateway(baseUri);
            }
            var deadline = DateTimeOffset.UtcNow + StartupTimeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await IsReadyAsync(baseUri, cancellationToken))
                {
                    return;
                }

                await Task.Delay(PollInterval, cancellationToken);
            }
#endif

            throw new WinAppAuthenticationException(
                $"O DtudoGateway nao ficou disponivel em {baseUri}. " +
                "Verifique o User Secret do OpenIdConnect e a ApiIdentity.");
#if DEBUG
        }
        finally
        {
            StartupGate.Release();
        }
#endif
    }

    private static async Task<bool> IsReadyAsync(
        Uri baseUri,
        CancellationToken cancellationToken)
    {
        using var handler = AppConfigurationService.CreateHttpClientHandler();
        using var client = new HttpClient(handler)
        {
            Timeout = HealthTimeout
        };

        try
        {
            using var response = await client.GetAsync(
                new Uri(baseUri, "health/live"),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            return response.IsSuccessStatusCode;
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

    private static void StartGateway(Uri baseUri)
    {
        var solutionRoot = FindSolutionRoot();
        var projectPath = solutionRoot is null
            ? null
            : Path.Combine(solutionRoot.FullName, "DtudoGateway", "DtudoGateway.csproj");
        if (projectPath is null || !File.Exists(projectPath))
        {
            throw new WinAppAuthenticationException(
                "Nao foi possivel localizar o projeto DtudoGateway na solucao local.");
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
                    "Nao foi possivel iniciar o processo local do DtudoGateway.");
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
                "Nao foi possivel iniciar o DtudoGateway local.",
                exception);
        }
    }

    private static bool IsDtudoGatewayProcessRunning()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("DtudoGateway"))
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
