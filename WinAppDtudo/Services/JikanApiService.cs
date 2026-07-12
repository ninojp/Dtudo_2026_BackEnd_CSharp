using System.Text.Json;

namespace WinAppDtudo.Services;

public sealed class JikanApiService
{
    private static readonly HttpClient HttpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public const string ApiBase = "https://localhost:63982";

    static JikanApiService()
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
        using var response = await HttpClient.GetAsync(
            $"ApiJikan/search?q={Uri.EscapeDataString(query)}&page={page}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JikanBuscaResult>(json, JsonOptions) ?? new JikanBuscaResult();
    }

    public async Task<JikanAnimeDetalhes?> BuscarPorIdAsync(int malId, CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync($"ApiJikan/{malId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JikanAnimeDetalhes>(json, JsonOptions);
    }

    public async Task<List<JikanAnimeRelacaoGroup>> BuscarRelacoesAsync(int malId, CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync($"ApiJikan/{malId}/relations", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<JikanAnimeRelacaoGroup>>(json, JsonOptions) ?? [];
    }
}
