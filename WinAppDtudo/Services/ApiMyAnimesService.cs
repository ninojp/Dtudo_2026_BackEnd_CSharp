using System.Text;
using System.Text.Json;
using LibDtudo.Shared.Dtos;

namespace WinAppDtudo.Services;

public class ApiMyAnimesService
{
    private static readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public const string ApiBase = "https://localhost:63980";

    static ApiMyAnimesService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiBase + "/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public async Task<List<ObterMyAnimeDto>> ObterMyAnimesAsync(int skip = 0, int take = 100)
    {
        using var response = await _httpClient.GetAsync($"apiLocal/MyAnime?skip={skip}&take={take}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ObterMyAnimeDto>>(json, _jsonOptions) ?? [];
    }

    public async Task<ObterMyAnimeDto?> ObterMyAnimePorIdAsync(int id)
    {
        using var response = await _httpClient.GetAsync($"apiLocal/MyAnime/{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ObterMyAnimeDto>(json, _jsonOptions);
    }

    public async Task AdicionarMyAnimeAsync(AdicionaMyAnimeDto dto)
    {
        var content = SerializarJson(dto);
        using var response = await _httpClient.PostAsync("apiLocal/MyAnime", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task AtualizarMyAnimeAsync(int id, AtualizaMyAnimeDto dto)
    {
        var content = SerializarJson(dto);
        using var response = await _httpClient.PutAsync($"apiLocal/MyAnime/{id}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task AdicionarAnimeAsync(AdicionaAnimeDto dto)
    {
        var content = SerializarJson(dto);
        using var response = await _httpClient.PostAsync("apiLocal/Anime", content);
        response.EnsureSuccessStatusCode();
    }

    private static StringContent SerializarJson<T>(T dto)
    {
        return new StringContent(
            JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json");
    }
}
