using ApiJikan.Dtos.External;
using ApiJikan.Dtos.Responses;
using ApiJikan.Mappers;
using System.Text.Json;

namespace ApiJikan.Services;

/// <summary>
/// Serviço responsável por buscar os animes relacionados a um anime específico na API Jikan.
/// Chama o endpoint dedicado /anime/{id}/relations e popula a ImageUrl de cada entrada
/// usando o ServiceBuscarPorID, que sempre retorna a imagem correta do anime.
/// </summary>
/// <remarks>
/// A Jikan NÃO retorna "relations" nos endpoints /anime?q= nem /anime/{id}.
/// Este serviço usa o endpoint exclusivo: GET /anime/{id}/relations
/// </remarks>
public class ServiceBuscarAnimeRelacionadoPorID
{
    private readonly HttpClient _httpClient;
    private readonly ServiceBuscarPorID _serviceBuscarPorID;
    private readonly ILogger<ServiceBuscarAnimeRelacionadoPorID> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    /// <summary>
    /// Inicializa o serviço de busca de animes relacionados por ID.
    /// </summary>
    public ServiceBuscarAnimeRelacionadoPorID( HttpClient httpClient, ServiceBuscarPorID serviceBuscarPorID,
        ILogger<ServiceBuscarAnimeRelacionadoPorID> logger)
    {
        _httpClient = httpClient;
        _serviceBuscarPorID = serviceBuscarPorID;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }
    /// <summary>
    /// Busca as relações de um anime na Jikan API (/anime/{id}/relations).
    /// Para cada entrada retornada, busca a imagem correta via ServiceBuscarPorID (uma por vez).
    /// </summary>
    /// <param name="malId">ID do anime no MyAnimeList</param>
    /// <returns>Lista de relações com ImageUrl populada em cada entrada</returns>
    public async Task<List<AnimeRelationGroupDto>> JikanBuscarRelacoesPorIDAsync(int malId)
    {
        try
        {
            _logger.LogInformation("Buscando relações do anime ID {MalId} na Jikan API (/anime/{MalId}/relations)", malId, malId);

            string content = string.Empty;
            for (var tentativa = 1; tentativa <= 3; tentativa++)
            {
                using var response = await _httpClient.GetAsync($"anime/{malId}/relations");
                if (response.IsSuccessStatusCode)
                {
                    content = await response.Content.ReadAsStringAsync();
                    break;
                }

                var transitório = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    || (int)response.StatusCode >= 500;
                if (!transitório || tentativa == 3)
                    response.EnsureSuccessStatusCode();

                var atraso = response.Headers.RetryAfter?.Delta
                    ?? TimeSpan.FromMilliseconds(1000 * tentativa);
                await Task.Delay(atraso, CancellationToken.None);
            }

            if (string.IsNullOrWhiteSpace(content))
                return new List<AnimeRelationGroupDto>();

            var relationResponse = JsonSerializer.Deserialize<JikanAnimeRelationsResponseDto>(content, _jsonOptions);
            var relations = ApiJikanResponseMapper.Map(relationResponse?.Data);

            _logger.LogInformation("Jikan retornou {Count} tipo(s) de relação para o anime ID {MalId}", relations.Count, malId);

            await PopularImagensEntradasAsync(relations);
            return relations;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Não foi possível buscar relações do anime ID {MalId}", malId);
            return new List<AnimeRelationGroupDto>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Erro ao deserializar relações do anime ID {MalId}", malId);
            return new List<AnimeRelationGroupDto>();
        }
    }

    /// <summary>
    /// Itera cada entrada das relações e busca a imagem via ServiceBuscarPorID (uma por vez).
    /// ServiceBuscarPorID usa GET /anime/{id} da Jikan, que sempre retorna a imagem correta.
    /// </summary>
    private async Task PopularImagensEntradasAsync(List<AnimeRelationGroupDto> relations)
    {
        var entries = relations
            .SelectMany(r => r.Entry ?? Enumerable.Empty<AnimeRelationEntryDto>())
            .Where(e => e.MalId > 0)
            .ToList();

        if (entries.Count == 0) return;

        _logger.LogInformation("Buscando imagem para {Count} entrada(s) de relações via ServiceBuscarPorID.", entries.Count);

        foreach (var entry in entries)
        {
            try
            {
                var anime = await _serviceBuscarPorID.JikanBuscarPorIDAsync(entry.MalId);
                entry.ImageUrl = anime?.Images?.Jpg?.ImageUrl ?? anime?.Images?.Jpg?.LargeImageUrl;
                _logger.LogInformation("Imagem para Mal_Id={MalId}: {ImageUrl}", entry.MalId, entry.ImageUrl ?? "(null)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível obter imagem para Mal_Id={MalId}.", entry.MalId);
            }

            // Pausa preventiva para respeitar o rate limit da Jikan (3 req/s).
            // GetComRetryRateLimitAsync em ServiceBuscarPorID trata o 429 caso ainda ocorra.
            await Task.Delay(400);
        }
    }
}
