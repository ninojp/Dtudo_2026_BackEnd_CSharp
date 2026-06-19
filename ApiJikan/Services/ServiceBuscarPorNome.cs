using ApiJikan.Dtos.External;
using ApiJikan.Dtos.Responses;
using ApiJikan.Mappers;
using System.Net;
using System.Text.Json;

namespace ApiJikan.Services;
/// <summary>
/// Serviço responsável por interagir com a API externa Jikan para buscar informações sobre animes.
/// API externa Jikan, tem a Limitação de 3 requisições por segundo e 60 requisições por minuto.
/// </summary>
public class ServiceBuscarPorNome
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceBuscarPorNome> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    /// <summary>
    /// Inicializa uma nova instância do serviço JikanService.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP para realizar requisições à API Jikan.</param>
    /// <param name="logger">Logger para registrar informações e erros.</param>
    public ServiceBuscarPorNome(HttpClient httpClient, ILogger<ServiceBuscarPorNome> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
    }
    /// <summary>
    /// Busca animes na API Jikan com base em uma consulta de texto e número da página. Retorna uma resposta estruturada contendo os resultados da busca e informações de paginação.
    /// </summary>
    /// <param name="query">Termo de busca</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <returns>Lista de animes encontrados</returns>
    /// <exception cref="TimeoutException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<BuscarAnimePorNomeResponseDto> JikanBuscarPorNomeAsync(string query, int page = 1)
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(query)
                ? $"anime?page={page}"
                : $"anime?q={Uri.EscapeDataString(query)}&page={page}";

            _logger.LogInformation("Buscando anime por NOME, na Jikan API Externa: {Url}", url);

            using var response = await GetComRetryAsync(url);

            var content = await response.Content.ReadAsStringAsync();
            LogarRespostaDetalhada(content);

            var jikanResponse = JsonSerializer.Deserialize<JikanAnimeSearchResponseDto>(content, _jsonOptions);

            if (jikanResponse == null)
            {
                _logger.LogWarning("Resposta vazia da Jikan API");
                return new BuscarAnimePorNomeResponseDto();
            }
            _logger.LogInformation("Deserialização OK — Itens: {Count} | Página: {Page}/{Total} | HasNext: {HasNext}",
                jikanResponse.Data?.Count ?? 0,
                jikanResponse.Pagination?.Current_Page,
                jikanResponse.Pagination?.Last_Visible_Page,
                jikanResponse.Pagination?.Has_Next_Page);
            return ApiJikanResponseMapper.Map(jikanResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar anime na Jikan API");

            if (ex.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                throw new TimeoutException("A API Jikan demorou para responder. Tente novamente em instantes.", ex);
            }
            throw new Exception("Erro ao comunicar com a API Jikan. Tente novamente mais tarde.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta da Jikan API");
            throw new Exception("Erro ao processar dados da API Jikan.", ex);
        }
    }

    private async Task<HttpResponseMessage> GetComRetryAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 2;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if ((response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.GatewayTimeout)
                && attempt < maxRetries)
            {
                var delay = ObterDelayRetry(response);
                _logger.LogWarning(
                    "Falha transitória da Jikan ({StatusCode}) para '{Url}'. Tentativa {Attempt}/{Max}. Aguardando {Delay}s.",
                    (int)response.StatusCode,
                    relativeUrl,
                    attempt + 1,
                    maxRetries + 1,
                    delay.TotalSeconds);

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

    /// <summary>
    /// Loga o JSON bruto da Jikan API antes de qualquer mapeamento.
    /// Exibe o JSON completo, o primeiro item com TODAS as propriedades e a propriedade 'relations' isolada.
    /// </summary>
    private void LogarRespostaDetalhada(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var prettyJson = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("=== JIKAN API - JSON BRUTO COMPLETO (antes do mapeamento) ===\n{Json}", prettyJson);

            if (doc.RootElement.TryGetProperty("data", out var dataArray)
                && dataArray.ValueKind == JsonValueKind.Array
                && dataArray.GetArrayLength() > 0)
            {
                _logger.LogInformation("=== JIKAN API - Total de itens em 'data[]': {Count}", dataArray.GetArrayLength());

                var primeiroItem = dataArray[0];
                var prettyItem = JsonSerializer.Serialize(primeiroItem, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("=== JIKAN API - PRIMEIRO ITEM 'data[0]' (todas as propriedades brutas, antes do mapeamento) ===\n{Json}", prettyItem);

                if (primeiroItem.TryGetProperty("relations", out var relProp))
                {
                    _logger.LogInformation("=== JIKAN API - Propriedade 'relations' do primeiro item ===\n{Relations}",
                        JsonSerializer.Serialize(relProp, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    _logger.LogWarning("=== JIKAN API - Propriedade 'relations' NÃO encontrada na busca por nome (disponível apenas no endpoint por ID).");
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "=== JIKAN API - Falha ao formatar log detalhado do JSON bruto.");
        }
    }
}
