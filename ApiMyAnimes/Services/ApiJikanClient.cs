using System.Net;

namespace ApiMyAnimes.Services;

/// <summary>
/// Cliente para interagir com a API Jikan.
/// </summary>
public class ApiJikanClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Obtém os dados de importação de um anime a partir do seu ID no MyAnimeList (malId) usando a API Jikan.
    /// </summary>
    /// <param name="malId">O ID do anime no MyAnimeList.</param>
    /// <param name="cancellationToken">Token de cancelamento para a operação assíncrona.</param>
    /// <returns>Os dados de importação do anime ou null se não encontrado.</returns>
    /// <exception cref="HttpRequestException">Lançada quando ocorre um erro na solicitação HTTP.</exception>
    public async Task<AnimeImportData?> ObterAnimePorIdAsync(int malId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"ApiJikan/{malId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Falha ao consultar ApiJikan. StatusCode={(int)response.StatusCode}",
                null,
                response.StatusCode);
        }
        var payload = await response.Content.ReadFromJsonAsync<ApiJikanAnimeDetailsResponseDto>(cancellationToken: cancellationToken);
        if (payload is null) return null;

        var titulo = payload.Title ?? payload.TitleEnglish ?? payload.TitleJapanese;

        var subtitulos = new List<string>();
        if (!string.IsNullOrWhiteSpace(payload.TitleEnglish)) subtitulos.Add(payload.TitleEnglish);
        if (!string.IsNullOrWhiteSpace(payload.TitleJapanese)) subtitulos.Add(payload.TitleJapanese);
        if (payload.TitleSynonyms is not null)
        {
            subtitulos.AddRange(payload.TitleSynonyms.Where(s => !string.IsNullOrWhiteSpace(s)).Cast<string>());
        }

        var imagens = new List<string>();
        if (!string.IsNullOrWhiteSpace(payload.Images?.Jpg?.ImageUrl)) imagens.Add(payload.Images.Jpg.ImageUrl);
        if (!string.IsNullOrWhiteSpace(payload.Images?.Jpg?.SmallImageUrl)) imagens.Add(payload.Images.Jpg.SmallImageUrl);
        if (!string.IsNullOrWhiteSpace(payload.Images?.Jpg?.LargeImageUrl)) imagens.Add(payload.Images.Jpg.LargeImageUrl);

        return new AnimeImportData
        {
            MalId = payload.MalId,
            Titulo = titulo ?? $"Anime {malId}",
            Episodios = payload.Episodes.GetValueOrDefault(1) > 0 ? payload.Episodes!.Value : 1,
            MalUrl = payload.Url ?? string.Empty,
            ImagensUrlMal = imagens.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            SubTitulos = subtitulos
                .Where(s => !string.Equals(s, titulo, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Trailer = payload.Trailer,
            Approved = payload.Approved,
            Title = payload.Title,
            TitleEnglish = payload.TitleEnglish,
            TitleJapanese = payload.TitleJapanese,
            TitleSynonyms = payload.TitleSynonyms ?? new List<string>(),
            Type = payload.Type,
            Source = payload.Source,
            Episodes = payload.Episodes,
            Status = payload.Status,
            Airing = payload.Airing,
            Aired = payload.Aired,
            Duration = payload.Duration,
            Rating = payload.Rating,
            Score = payload.Score,
            ScoredBy = payload.ScoredBy,
            Rank = payload.Rank,
            Popularity = payload.Popularity,
            Members = payload.Members,
            Favorites = payload.Favorites,
            Synopsis = payload.Synopsis,
            Background = payload.Background,
            Season = payload.Season,
            Year = payload.Year,
            Producers = payload.Producers ?? new List<string>(),
            Licensors = payload.Licensors ?? new List<string>(),
            Studios = payload.Studios ?? new List<string>(),
            Genres = payload.Genres ?? new List<string>(),
            ExplicitGenres = payload.ExplicitGenres ?? new List<string>(),
            Themes = payload.Themes ?? new List<string>(),
            Demographics = payload.Demographics ?? new List<string>()
        };
    }
}
/// <summary>
/// Representa os dados de importação de um anime obtidos da API Jikan.
/// </summary>
public class AnimeImportData
{
    public int MalId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int Episodios { get; set; }
    public string MalUrl { get; set; } = string.Empty;
    public List<string> ImagensUrlMal { get; set; } = new();
    public List<string> SubTitulos { get; set; } = new();
    public string? Trailer { get; set; }
    public bool Approved { get; set; }
    public string? Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public List<string> TitleSynonyms { get; set; } = new();
    public string? Type { get; set; }
    public string? Source { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public bool Airing { get; set; }
    public string? Aired { get; set; }
    public string? Duration { get; set; }
    public string? Rating { get; set; }
    public double? Score { get; set; }
    public int? ScoredBy { get; set; }
    public int? Rank { get; set; }
    public int? Popularity { get; set; }
    public int? Members { get; set; }
    public int? Favorites { get; set; }
    public string? Synopsis { get; set; }
    public string? Background { get; set; }
    public string? Season { get; set; }
    public int? Year { get; set; }
    public List<string> Producers { get; set; } = new();
    public List<string> Licensors { get; set; } = new();
    public List<string> Studios { get; set; } = new();
    public List<string> Genres { get; set; } = new();
    public List<string> ExplicitGenres { get; set; } = new();
    public List<string> Themes { get; set; } = new();
    public List<string> Demographics { get; set; } = new();
}

file class ApiJikanAnimeDetailsResponseDto
{
    public int MalId { get; set; }
    public string? Url { get; set; }
    public ApiJikanImageCollectionDto? Images { get; set; }
    public string? Trailer { get; set; }
    public bool Approved { get; set; }
    public string? Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public List<string>? TitleSynonyms { get; set; }
    public string? Type { get; set; }
    public string? Source { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public bool Airing { get; set; }
    public string? Aired { get; set; }
    public string? Duration { get; set; }
    public string? Rating { get; set; }
    public double? Score { get; set; }
    public int? ScoredBy { get; set; }
    public int? Rank { get; set; }
    public int? Popularity { get; set; }
    public int? Members { get; set; }
    public int? Favorites { get; set; }
    public string? Synopsis { get; set; }
    public string? Background { get; set; }
    public string? Season { get; set; }
    public int? Year { get; set; }
    public List<string>? Producers { get; set; }
    public List<string>? Licensors { get; set; }
    public List<string>? Studios { get; set; }
    public List<string>? Genres { get; set; }
    public List<string>? ExplicitGenres { get; set; }
    public List<string>? Themes { get; set; }
    public List<string>? Demographics { get; set; }
}

file class ApiJikanImageCollectionDto
{
    public ApiJikanImageVariantDto? Jpg { get; set; }
}

file class ApiJikanImageVariantDto
{
    public string? ImageUrl { get; set; }
    public string? SmallImageUrl { get; set; }
    public string? LargeImageUrl { get; set; }
}
