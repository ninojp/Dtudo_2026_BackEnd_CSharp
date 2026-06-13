using ApiCSharp.Models;
using System.Text.Json;

namespace ApiCSharp.Services;

public class JikanService : IJikanService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JikanService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JikanService(HttpClient httpClient, ILogger<JikanService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public async Task<AnimeSearchResponse> SearchAnimeAsync(string query, int page = 1)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(query)
                ? $"anime?page={page}"
                : $"anime?q={Uri.EscapeDataString(query)}&page={page}";

            _logger.LogInformation("Buscando anime na Jikan API: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var jikanResponse = JsonSerializer.Deserialize<JikanAnimeResponse>(content, _jsonOptions);

            if (jikanResponse?.Data == null)
            {
                _logger.LogWarning("Resposta vazia da Jikan API");
                return new AnimeSearchResponse();
            }

            return MapToSearchResponse(jikanResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar anime na Jikan API");
            throw new Exception("Erro ao comunicar com a API Jikan. Tente novamente mais tarde.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta da Jikan API");
            throw new Exception("Erro ao processar dados da API Jikan.", ex);
        }
    }

    public async Task<AnimeData?> GetAnimeByIdAsync(int malId)
    {
        try
        {
            _logger.LogInformation("Buscando anime por ID: {MalId}", malId);

            var response = await _httpClient.GetAsync($"anime/{malId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonSerializer.Deserialize<JsonDocument>(content, _jsonOptions);

            if (jsonDocument?.RootElement.TryGetProperty("data", out var dataElement) == true)
            {
                return JsonSerializer.Deserialize<AnimeData>(dataElement.GetRawText(), _jsonOptions);
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar anime por ID na Jikan API");
            throw new Exception("Erro ao comunicar com a API Jikan. Tente novamente mais tarde.", ex);
        }
    }

    private static AnimeSearchResponse MapToSearchResponse(JikanAnimeResponse jikanResponse)
    {
        var results = jikanResponse.Data?
            .Select(anime => new AnimeSearchResult
            {
                MalId = anime.Mal_Id,
                Title = anime.Title,
                TitleEnglish = anime.Title_English,
                TitleJapanese = anime.Title_Japanese,
                ImageUrl = anime.Images?.GetValueOrDefault("jpg")?.Image_Url,
                Type = anime.Type,
                Episodes = anime.Episodes,
                Status = anime.Status,
                Score = anime.Score,
                Year = anime.Year,
                Synopsis = anime.Synopsis,
                Genres = anime.Genres?.Select(g => g.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList(),
                Url = anime.Url
            })
            .ToList() ?? new List<AnimeSearchResult>();

        return new AnimeSearchResponse
        {
            Results = results,
            CurrentPage = jikanResponse.Pagination?.Current_Page ?? 1,
            TotalPages = jikanResponse.Pagination?.Last_Visible_Page ?? 1,
            HasNextPage = jikanResponse.Pagination?.Has_Next_Page ?? false,
            TotalResults = jikanResponse.Pagination?.Items?.Total ?? results.Count
        };
    }
}
