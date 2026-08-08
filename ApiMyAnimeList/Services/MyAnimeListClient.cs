using System.Net.Http.Json;
using System.Text.Json;
using ApiMyAnimeList.Configuration;
using ApiMyAnimeList.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ApiMyAnimeList.Services;

public sealed class MyAnimeListClient(
    HttpClient httpClient,
    IOptions<MyAnimeListOptions> options,
    IMemoryCache cache,
    ILogger<MyAnimeListClient> logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly MyAnimeListOptions _options = options.Value;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<MyAnimeListClient> _logger = logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<MalPagedResponse<MalAnimeNode>> SearchAsync(string query, int offset, int limit, CancellationToken cancellationToken) =>
        GetAsync<MalPagedResponse<MalAnimeNode>>($"anime?q={Uri.EscapeDataString(query)}&offset={offset}&limit={limit}&nsfw=true&fields={SearchFields}", cancellationToken);

    public async Task<MalAnimeNode?> GetAnimeAsync(int id, CancellationToken cancellationToken)
    {
        var key = $"mal-anime-{id}";
        if (_cache.TryGetValue(key, out MalAnimeNode? cached)) return cached;
        var result = await GetAsync<MalAnimeNode>($"anime/{id}?nsfw=true&fields={DetailsFields}", cancellationToken);
        _cache.Set(key, result, TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));
        return result;
    }

    private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Falha final da MAL ({StatusCode}) no endpoint {Endpoint} após a política de resiliência.",
                (int)response.StatusCode,
                GetEndpoint(relativeUrl));
            throw new HttpRequestException(
                $"MyAnimeList retornou {(int)response.StatusCode} no endpoint {GetEndpoint(relativeUrl)}.",
                null,
                response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("A API MyAnimeList retornou uma resposta vazia.");
    }

    private static string GetEndpoint(string relativeUrl)
        => relativeUrl.Split('?', 2)[0];

    private const string SearchFields = "id,title,main_picture,alternative_titles,start_season,media_type,status,num_episodes,mean,genres";
    private const string DetailsFields = "id,title,main_picture,alternative_titles,start_date,end_date,synopsis,mean,rank,popularity,num_list_users,num_scoring_users,media_type,status,genres,num_episodes,average_episode_duration,start_season,source,rating,background,studios,related_anime{node{id,title,main_picture,media_type},relation_type}";
}
