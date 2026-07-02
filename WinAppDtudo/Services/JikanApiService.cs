using System.Text.Json;

namespace WinAppDtudo.Services;

/// <summary>
/// Serviço HTTP que consome a API local ApiJikan em https://localhost:63982.
/// Utiliza um HttpClient estático compartilhado e aceita certificados de desenvolvimento.
/// </summary>
public class JikanApiService
{
    private static readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public const string ApiBase = "https://localhost:63982";

    static JikanApiService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiBase + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>Busca animes por nome com paginação.</summary>
    public async Task<JikanBuscaResult> BuscarPorNomeAsync(string query, int page = 1)
    {
        var url = $"ApiJikan/search?q={Uri.EscapeDataString(query)}&page={page}";
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JikanBuscaResult>(json, _jsonOptions) ?? new JikanBuscaResult();
    }

    /// <summary>Busca detalhes completos de um anime por ID do MAL.</summary>
    public async Task<JikanAnimeDetalhes?> BuscarPorIdAsync(int malId)
    {
        using var response = await _httpClient.GetAsync($"ApiJikan/{malId}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JikanAnimeDetalhes>(json, _jsonOptions);
    }
}
