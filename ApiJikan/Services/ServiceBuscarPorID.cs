using ApiJikan.Dtos.External;
using ApiJikan.Dtos.Responses;
using ApiJikan.Mappers;
using System.Net;
using System.Text.Json;

namespace ApiJikan.Services;
/// <summary>
/// Serviço responsável por interagir com a API externa Jikan para buscar informações de um Anime por ID
/// </summary>
public class ServiceBuscarPorID
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceBuscarPorID> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    public ServiceBuscarPorID(HttpClient httpClient, ILogger<ServiceBuscarPorID> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public async Task<BuscarAnimePorIdResponseDto?> JikanBuscarPorIDAsync(int malId)
    {
        try
        {
            _logger.LogInformation("Buscando anime por ID: {MalId}", malId);

            using var response = await GetComRetryRateLimitAsync($"anime/{malId}");

            var content = await response.Content.ReadAsStringAsync();
            //try
            //{
            //    using var docLog = JsonDocument.Parse(content);
            //    var prettyJson = JsonSerializer.Serialize(docLog.RootElement, new JsonSerializerOptions { WriteIndented = true });
            //    _logger.LogInformation("=== JIKAN API BUSCA POR ID {MalId} - JSON BRUTO COMPLETO (antes do mapeamento) ===\n{Json}", malId, prettyJson);
            //}
            //catch (JsonException ex)
            //{
            //    _logger.LogWarning(ex, "=== JIKAN API BUSCA POR ID {MalId} - Falha ao formatar log detalhado.", malId);
            //}

            var jikanResponse = JsonSerializer.Deserialize<JikanAnimeByIdResponseDto>(content, _jsonOptions);

            if (jikanResponse?.Data == null) return null;

            return ApiJikanResponseMapper.Map(jikanResponse.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar anime por ID na Jikan API");
            throw new Exception("Erro ao comunicar com a API Jikan. Tente novamente mais tarde.", ex);
        }
    }
    /// <summary>
    /// Executa GET com retry automático em caso de 429 (Too Many Requests).
    /// Respeita o header Retry-After da Jikan; usa 2s de fallback se não informado.
    /// </summary>
    private async Task<HttpResponseMessage> GetComRetryRateLimitAsync(
        string relativeUrl, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 2;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            if (response.IsSuccessStatusCode) return response;

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < maxRetries)
            {
                var delay = ObterDelayRetry(response);
                _logger.LogWarning(
                    "Rate limit da Jikan (429) para '{Url}'. Tentativa {Attempt}/{Max}. Aguardando {Delay}s.",
                    relativeUrl, attempt + 1, maxRetries + 1, delay.TotalSeconds);
                response.Dispose();
                await Task.Delay(delay, cancellationToken);
                continue;
            }
            response.EnsureSuccessStatusCode();
        }
        throw new HttpRequestException($"Falha ao buscar '{relativeUrl}' na Jikan após múltiplas tentativas.");
    }

    private static TimeSpan ObterDelayRetry(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero) return delta;
        if (retryAfter?.Date is DateTimeOffset retryDate)
        {
            var delay = retryDate - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero) return delay;
        }
        return TimeSpan.FromSeconds(2);
    }

}
