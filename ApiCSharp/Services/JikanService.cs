using ApiCSharp.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text.Json;

namespace ApiCSharp.Services;

public class JikanService : IJikanService
{
    private const int MaxRateLimitRetries = 2;
    private static readonly TimeSpan FallbackRetryDelay = TimeSpan.FromSeconds(2);
    private const int RelationHydrationMaxParallelism = 2;
    private static readonly TimeSpan RelationImageCacheDuration = TimeSpan.FromHours(12);
    private static readonly TimeSpan MissingRelationImageCacheDuration = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan RelationImageRequestTimeout = TimeSpan.FromSeconds(4);
    private const int MaxRelationImagesToHydrate = 12;
    private const string MissingImageCacheMarker = "__MISSING__";

    private readonly HttpClient _httpClient;
    private readonly ILogger<JikanService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly JsonSerializerOptions _jsonOptions;

    public JikanService(HttpClient httpClient, ILogger<JikanService> logger, IMemoryCache memoryCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _memoryCache = memoryCache;

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

            using var response = await GetWithRateLimitRetryAsync(
                $"anime/{malId}",
                $"detalhes do anime {malId}");

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonSerializer.Deserialize<JsonDocument>(content, _jsonOptions);

            if (jsonDocument?.RootElement.TryGetProperty("data", out var dataElement) != true)
            {
                return null;
            }

            var anime = JsonSerializer.Deserialize<AnimeData>(dataElement.GetRawText(), _jsonOptions);

            if (anime == null)
            {
                return null;
            }

            anime.Relations = await GetAnimeRelationsAsync(malId);
            return anime;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar anime por ID na Jikan API");
            throw new Exception("Erro ao comunicar com a API Jikan. Tente novamente mais tarde.", ex);
        }
    }

    private async Task<List<AnimeRelation>> GetAnimeRelationsAsync(int malId)
    {
        try
        {
            using var response = await GetWithRateLimitRetryAsync(
                $"anime/{malId}/relations",
                $"relações do anime {malId}");

            var content = await response.Content.ReadAsStringAsync();
            var relationResponse = JsonSerializer.Deserialize<AnimeRelationResponse>(content, _jsonOptions);
            var relations = relationResponse?.Data ?? new List<AnimeRelation>();

            await HydrateRelationEntryImagesAsync(relations);
            return relations;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Não foi possível buscar relações do anime {MalId}", malId);
            return new List<AnimeRelation>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Erro ao deserializar relações do anime {MalId}", malId);
            return new List<AnimeRelation>();
        }
    }

    private async Task HydrateRelationEntryImagesAsync(List<AnimeRelation> relations)
    {
        if (relations.Count == 0)
        {
            return;
        }

        var imageCache = new Dictionary<int, string?>();
        var entriesToHydrate = relations
            .SelectMany(relation => relation.Entry ?? Enumerable.Empty<MalItem>())
            .Where(entry => entry.Mal_Id > 0)
            .ToList();

        if (entriesToHydrate.Count == 0)
        {
            return;
        }

        var uniqueIds = entriesToHydrate
            .Select(entry => entry.Mal_Id)
            .Distinct()
            .ToList();

        var idsToHydrate = uniqueIds.Take(MaxRelationImagesToHydrate).ToList();

        if (uniqueIds.Count > idsToHydrate.Count)
        {
            _logger.LogInformation(
                "Limitando hidratação de imagens de relations para {HydratedCount} de {TotalCount} itens.",
                idsToHydrate.Count,
                uniqueIds.Count);
        }

        var semaphore = new SemaphoreSlim(RelationHydrationMaxParallelism);
        var imageTasks = idsToHydrate.Select(async malId =>
        {
            await semaphore.WaitAsync();
            try
            {
                var imageUrl = await GetAnimeImageByIdAsync(malId);
                lock (imageCache)
                {
                    imageCache[malId] = imageUrl;
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(imageTasks);

        foreach (var entry in entriesToHydrate)
        {
            if (imageCache.TryGetValue(entry.Mal_Id, out var imageUrl))
            {
                entry.ImageUrl = imageUrl;
            }
        }
    }

    private async Task<string?> GetAnimeImageByIdAsync(int malId)
    {
        var cacheKey = $"jikan:relation-image:{malId}";

        if (_memoryCache.TryGetValue<string>(cacheKey, out var cachedValue))
        {
            return cachedValue == MissingImageCacheMarker ? null : cachedValue;
        }

        try
        {
            using var cts = new CancellationTokenSource(RelationImageRequestTimeout);
            using var response = await GetWithRateLimitRetryAsync(
                $"anime/{malId}",
                $"imagem do anime relacionado {malId}",
                cts.Token);

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonSerializer.Deserialize<JsonDocument>(content, _jsonOptions);

            if (jsonDocument?.RootElement.TryGetProperty("data", out var dataElement) != true)
            {
                _memoryCache.Set(cacheKey, MissingImageCacheMarker, MissingRelationImageCacheDuration);
                return null;
            }

            var anime = JsonSerializer.Deserialize<AnimeData>(dataElement.GetRawText(), _jsonOptions);
            var imageUrl = anime?.Images?.GetValueOrDefault("jpg")?.Image_Url;

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                _memoryCache.Set(cacheKey, MissingImageCacheMarker, MissingRelationImageCacheDuration);
                return null;
            }

            _memoryCache.Set(cacheKey, imageUrl, RelationImageCacheDuration);
            return imageUrl;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Timeout ao hidratar imagem do anime relacionado {MalId} após {TimeoutSeconds}s",
                malId,
                RelationImageRequestTimeout.TotalSeconds);

            _memoryCache.Set(cacheKey, MissingImageCacheMarker, MissingRelationImageCacheDuration);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            _logger.LogWarning(ex, "Não foi possível hidratar imagem do anime relacionado {MalId}", malId);
            _memoryCache.Set(cacheKey, MissingImageCacheMarker, MissingRelationImageCacheDuration);
            return null;
        }
    }

    private async Task<HttpResponseMessage> GetWithRateLimitRetryAsync(string relativeUrl, string operationName, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= MaxRateLimitRetries; attempt++)
        {
            var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRateLimitRetries)
            {
                var delay = GetRetryDelay(response, attempt);

                _logger.LogWarning(
                    "Rate limit da Jikan ao buscar {Operation}. Tentativa {Attempt}/{MaxAttempts}. Aguardando {DelaySeconds}s.",
                    operationName,
                    attempt + 1,
                    MaxRateLimitRetries + 1,
                    delay.TotalSeconds);

                response.Dispose();
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();
        }

        throw new HttpRequestException($"Falha ao buscar {operationName} na API Jikan após múltiplas tentativas.");
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is DateTimeOffset retryDate)
        {
            var delay = retryDate - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        var exponentialDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
        return exponentialDelay > FallbackRetryDelay ? exponentialDelay : FallbackRetryDelay;
    }

    private static AnimeSearchResponse MapToSearchResponse(JikanAnimeResponse jikanResponse)
    {
        var results = jikanResponse.Data?
            .Select(anime => new JikanAnimeSearchResult
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
            .ToList() ?? new List<JikanAnimeSearchResult>();

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
