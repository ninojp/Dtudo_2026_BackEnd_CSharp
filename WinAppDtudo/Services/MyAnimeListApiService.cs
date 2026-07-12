using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;

namespace WinAppDtudo.Services;

/// <summary>
/// Serviço HTTP que consome exclusivamente a API local ApiMyAnimeList.
/// A API local mantém o contrato de resposta compatível com os modelos usados pelos cards.
/// </summary>
public sealed class MyAnimeListApiService
{
    private static readonly HttpClient HttpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly SemaphoreSlim ApiStartupLock = new(1, 1);
    private static bool _apiStartupAttempted;

    public const string ApiBase = "http://127.0.0.1:5044";

    static MyAnimeListApiService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        HttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiBase + "/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public async Task<JikanBuscaResult> BuscarPorNomeAsync(string query, int page = 1, CancellationToken cancellationToken = default)
    {
        var url = $"MyAnimeList/search?q={Uri.EscapeDataString(query)}&page={page}";
        HttpResponseMessage response;
        try
        {
            response = await HttpClient.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex) when (EhConexaoRecusada(ex))
        {
            await IniciarApiLocalAsync(cancellationToken);
            response = await HttpClient.GetAsync(url, cancellationToken);
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JikanBuscaResult>(json, JsonOptions) ?? new JikanBuscaResult();
        }
    }

    public Task<JikanAnimeDetalhes?> BuscarPorIdAsync(int malId, CancellationToken cancellationToken = default)
        => GetAsync<JikanAnimeDetalhes>($"MyAnimeList/{malId}", cancellationToken);

    public Task<List<JikanAnimeRelacaoGroup>> BuscarRelacoesAsync(int malId, CancellationToken cancellationToken = default)
        => GetAsync<List<JikanAnimeRelacaoGroup>>($"MyAnimeList/{malId}/relations", cancellationToken);

    private static async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await HttpClient.GetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex) when (EhConexaoRecusada(ex))
        {
            await IniciarApiLocalAsync(cancellationToken);
            response = await HttpClient.GetAsync(url, cancellationToken);
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
    }

    private static bool EhConexaoRecusada(HttpRequestException exception)
        => exception.InnerException is SocketException socketException
           && socketException.SocketErrorCode == SocketError.ConnectionRefused;

    private static async Task IniciarApiLocalAsync(CancellationToken cancellationToken)
    {
        await ApiStartupLock.WaitAsync(cancellationToken);
        try
        {
            if (_apiStartupAttempted) return;
            _apiStartupAttempted = true;

            var solutionRoot = LocalizarRaizDaSolucao();
            var projectPath = solutionRoot is null
                ? null
                : Path.Combine(solutionRoot.FullName, "ApiMyAnimeList", "ApiMyAnimeList.csproj");

            if (projectPath is null || !File.Exists(projectPath)) return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --no-launch-profile --urls {ApiBase}",
                WorkingDirectory = solutionRoot!.FullName,
                UseShellExecute = true,
                CreateNoWindow = false
            });

            for (var attempt = 0; attempt < 30; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

                try
                {
                    using var healthResponse = await HttpClient.GetAsync("MyAnimeList/search?q=test&page=1", cancellationToken);
                    if ((int)healthResponse.StatusCode < 500) return;
                }
                catch (HttpRequestException) { }
            }
        }
        finally
        {
            ApiStartupLock.Release();
        }
    }

    private static DirectoryInfo? LocalizarRaizDaSolucao()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Dtudo2026.slnx"))) return directory;
            directory = directory.Parent;
        }

        return null;
    }
}
