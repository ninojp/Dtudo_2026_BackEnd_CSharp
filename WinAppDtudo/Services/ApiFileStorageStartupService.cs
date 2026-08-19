using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace WinAppDtudo.Services;

public sealed class ApiFileStorageStartupService : IDisposable
{
    private const int RequiredContractVersion = 2;
    private static readonly SemaphoreSlim StartupGate = new(1, 1);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private readonly object _processSync = new();
    private Process? _startedProcess;
    private bool _disposed;

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var configuredUrl = AppConfigurationService.ApiFileStorageBaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("https" or "http"))
        {
            throw new WinAppAuthenticationException(
                "A URL local do ApiFileStorage nao esta configurada corretamente.");
        }

        if (await HasRequiredContractAsync(baseUri, cancellationToken))
        {
            return;
        }

        await StartupGate.WaitAsync(cancellationToken);
        try
        {
            if (await HasRequiredContractAsync(baseUri, cancellationToken))
            {
                return;
            }

            var existingProcess = IsApiFileStorageProcessRunning();
            if (!existingProcess)
            {
                await Task.Delay(PollInterval, cancellationToken);
                if (await HasRequiredContractAsync(baseUri, cancellationToken))
                {
                    return;
                }

                existingProcess = IsApiFileStorageProcessRunning();
            }

            if (!existingProcess)
            {
                StartApiFileStorage(baseUri);
            }

            var deadline = DateTimeOffset.UtcNow + StartupTimeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await HasRequiredContractAsync(baseUri, cancellationToken))
                {
                    return;
                }

                await Task.Delay(PollInterval, cancellationToken);
            }
            throw new WinAppAuthenticationException(
                $"A ApiFileStorage em {baseUri} nao disponibilizou o contrato de inicializacao " +
                $"v{RequiredContractVersion}. Encerre instancias antigas da ApiFileStorage, " +
                "inicie novamente pelo perfil IniciaTudo e verifique FileStorage:Roots.");
        }
        finally
        {
            StartupGate.Release();
        }
    }

    private static bool IsApiFileStorageProcessRunning()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("ApiFileStorage"))
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

    private static async Task<bool> HasRequiredContractAsync(
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
                new Uri(baseUri, "api/file-storage/startup"),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var startup = await response.Content.ReadFromJsonAsync<FileStorageStartupProbe>(
                cancellationToken: cancellationToken);
            return startup is not null
                && string.Equals(startup.Service, "ApiFileStorage", StringComparison.Ordinal)
                && startup.ContractVersion == RequiredContractVersion;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void StartApiFileStorage(Uri baseUri)
    {
        var solutionRoot = FindSolutionRoot();
        var projectPath = solutionRoot is null
            ? null
            : Path.Combine(solutionRoot.FullName, "ApiFileStorage", "ApiFileStorage.csproj");
        if (projectPath is null || !File.Exists(projectPath))
        {
            throw new WinAppAuthenticationException(
                "Nao foi possivel localizar o projeto ApiFileStorage na solucao local.");
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

            var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new WinAppAuthenticationException(
                    "Nao foi possivel iniciar o processo local do ApiFileStorage.");
            }

            lock (_processSync)
            {
                if (_disposed)
                {
                    process.Kill(entireProcessTree: true);
                    process.Dispose();
                    throw new ObjectDisposedException(nameof(ApiFileStorageStartupService));
                }

                _startedProcess = process;
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
                "Nao foi possivel iniciar o ApiFileStorage local.",
                exception);
        }
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

    public void Dispose()
    {
        Process? process;
        lock (_processSync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            process = _startedProcess;
            _startedProcess = null;
        }

        if (process is null)
        {
            return;
        }

        using (process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
        }
    }

    private sealed record FileStorageStartupProbe(
        string Service,
        int ContractVersion);
}
