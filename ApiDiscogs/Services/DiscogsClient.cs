using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiDiscogs.Configuration;
using ApiDiscogs.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ApiDiscogs.Services;

public sealed class DiscogsClient(
    HttpClient httpClient,
    IOptions<DiscogsOptions> options,
    IMemoryCache cache,
    ILogger<DiscogsClient> logger)
{
    private const int DefaultSearchPageSize = 10;
    private const int MaxSearchPageSize = 20;
    private const int MaxReleasePageSize = 100;
    private const string CacheKeyPrefix = "apidiscogs:v1";

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly DiscogsOptions _options = options.Value;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<DiscogsClient> _logger = logger;

    public Task<JsonDocument> SearchArtistsAsync(
        string query,
        int page = 1,
        int perPage = DefaultSearchPageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        ValidatePage(page);
        ValidatePageSize(perPage, MaxSearchPageSize);

        var encodedQuery = Uri.EscapeDataString(normalizedQuery);
        var endpoint = $"database/search?type=artist&q={encodedQuery}&page={page}&per_page={perPage}";
        var cacheKey = $"{CacheKeyPrefix}:search:artist:{page}:{perPage}:{normalizedQuery.ToUpperInvariant()}";
        return GetJsonAsync("artist-search", endpoint, cacheKey, cancellationToken);
    }

    public Task<JsonDocument> GetArtistAsync(
        int artistId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(artistId, nameof(artistId));
        return GetJsonAsync(
            "artist-details",
            $"artists/{artistId}",
            $"{CacheKeyPrefix}:details:artist:{artistId}",
            cancellationToken);
    }

    public Task<JsonDocument> GetArtistReleasesAsync(
        int artistId,
        int page = 1,
        int perPage = 50,
        string expand = "none",
        CancellationToken cancellationToken = default)
    {
        ValidateId(artistId, nameof(artistId));
        ValidatePage(page);
        ValidatePageSize(perPage, MaxReleasePageSize);

        var normalizedExpand = NormalizeExpand(expand);
        var endpoint = $"artists/{artistId}/releases?page={page}&per_page={perPage}&expand={normalizedExpand}";
        var cacheKey = $"{CacheKeyPrefix}:details:artist-releases:{artistId}:{page}:{perPage}:{normalizedExpand}";
        return GetJsonAsync("artist-releases", endpoint, cacheKey, cancellationToken);
    }

    public Task<JsonDocument> GetReleaseAsync(
        int releaseId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(releaseId, nameof(releaseId));
        return GetJsonAsync(
            "release-details",
            $"releases/{releaseId}",
            $"{CacheKeyPrefix}:details:release:{releaseId}",
            cancellationToken);
    }

    public Task<JsonDocument> GetMasterAsync(
        int masterId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(masterId, nameof(masterId));
        return GetJsonAsync(
            "master-details",
            $"masters/{masterId}",
            $"{CacheKeyPrefix}:details:master:{masterId}",
            cancellationToken);
    }

    private async Task<JsonDocument> GetJsonAsync(
        string operation,
        string relativeEndpoint,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        var requestUri = CreateAllowedRequestUri(relativeEndpoint);
        if (_cache.TryGetValue(cacheKey, out string? cachedPayload)
            && cachedPayload is not null)
        {
            _logger.LogDebug(
                "Cache hit da Discogs para a operacao {Operation} no endpoint {Endpoint}.",
                operation,
                GetEndpoint(requestUri));
            return ParseJson(cachedPayload, operation, requestUri);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeEndpoint);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Falha final da Discogs ({StatusCode}) na operacao {Operation} e endpoint {Endpoint}.",
                    (int)response.StatusCode,
                    operation,
                    GetEndpoint(requestUri));
                var upstreamException = new HttpRequestException(
                    $"A Discogs retornou {(int)response.StatusCode} no endpoint {GetEndpoint(requestUri)}.",
                    null,
                    response.StatusCode);
                if (response.StatusCode == HttpStatusCode.TooManyRequests
                    && ParseRetryAfterSeconds(response.Headers.RetryAfter) is { } retryAfterSeconds)
                {
                    upstreamException.Data["DiscogsRetryAfterSeconds"] = retryAfterSeconds;
                }

                throw upstreamException;
            }

            var payload = await ReadPayloadAsync(response, cancellationToken);
            var parsed = ParseJson(payload, operation, requestUri);
            _cache.Set(
                cacheKey,
                payload,
                TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));
            return parsed;
        }
        catch (BrokenCircuitException exception)
        {
            _logger.LogWarning(
                exception,
                "Circuito da Discogs aberto para a operacao {Operation} e endpoint {Endpoint}.",
                operation,
                GetEndpoint(requestUri));
            throw;
        }
        catch (TimeoutRejectedException exception)
        {
            _logger.LogWarning(
                exception,
                "Timeout da Discogs na operacao {Operation} e endpoint {Endpoint}.",
                operation,
                GetEndpoint(requestUri));
            throw;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Falha de transporte da Discogs na operacao {Operation}, status {StatusCode} e endpoint {Endpoint}.",
                operation,
                exception.StatusCode is { } statusCode ? (int)statusCode : null,
                GetEndpoint(requestUri));
            throw;
        }
    }

    private Uri CreateAllowedRequestUri(string relativeEndpoint)
    {
        if (string.IsNullOrWhiteSpace(relativeEndpoint)
            || relativeEndpoint.StartsWith("//", StringComparison.Ordinal)
            || Uri.TryCreate(relativeEndpoint, UriKind.Absolute, out _)
            || _httpClient.BaseAddress is null)
        {
            throw new DiscogsEgressException("O endpoint da API Discogs deve ser relativo e permitido.");
        }

        var requestUri = new Uri(_httpClient.BaseAddress, relativeEndpoint);
        if (!DiscogsEgressHandler.IsAllowedRequestUri(_options, requestUri))
        {
            throw new DiscogsEgressException("O endpoint da API Discogs nao pertence a allowlist de egress.");
        }

        return requestUri;
    }

    private async Task<string> ReadPayloadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > _options.MaxResponseBytes)
        {
            throw new DiscogsInvalidResponseException("A resposta da Discogs excede o limite configurado.");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var payload = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(81920, _options.MaxResponseBytes));

        try
        {
            while (true)
            {
                var bytesRead = await responseStream.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                if (payload.Length + bytesRead > _options.MaxResponseBytes)
                {
                    throw new DiscogsInvalidResponseException("A resposta da Discogs excede o limite configurado.");
                }

                await payload.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return Encoding.UTF8.GetString(payload.GetBuffer(), 0, checked((int)payload.Length));
    }

    private JsonDocument ParseJson(string payload, string operation, Uri requestUri)
    {
        try
        {
            return JsonDocument.Parse(payload, JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Resposta JSON invalida da Discogs na operacao {Operation} e endpoint {Endpoint}.",
                operation,
                GetEndpoint(requestUri));
            throw new DiscogsInvalidResponseException("A Discogs retornou um payload JSON invalido.");
        }
    }

    private static string NormalizeQuery(string query)
    {
        var normalized = string.Join(
            ' ',
            query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return normalized.Length is >= 2 and <= 120
            ? normalized
            : throw new DiscogsValidationException("A busca deve possuir entre 2 e 120 caracteres.");
    }

    private static string NormalizeExpand(string expand)
        => expand.Trim().ToLowerInvariant() switch
        {
            "none" => "none",
            "master" => "master",
            _ => throw new DiscogsValidationException("A expansao deve ser none ou master.")
        };

    private static void ValidateId(int id, string parameterName)
    {
        if (id <= 0)
        {
            throw new DiscogsValidationException("O ID Discogs deve ser positivo.");
        }
    }

    private static void ValidatePage(int page)
    {
        if (page < 1)
        {
            throw new DiscogsValidationException("A pagina deve ser maior ou igual a 1.");
        }
    }

    private static void ValidatePageSize(int pageSize, int maximum)
    {
        if (pageSize is < 1 || pageSize > maximum)
        {
            throw new DiscogsValidationException($"O tamanho da pagina deve estar entre 1 e {maximum}.");
        }
    }

    private static int? ParseRetryAfterSeconds(RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter is null)
        {
            return null;
        }

        var seconds = retryAfter.Delta?.TotalSeconds
            ?? (retryAfter.Date - DateTimeOffset.UtcNow)?.TotalSeconds;
        if (seconds is null || double.IsNaN(seconds.Value) || double.IsInfinity(seconds.Value))
        {
            return null;
        }

        return Math.Clamp((int)Math.Ceiling(Math.Max(0, seconds.Value)), 0, 3600);
    }

    private static string GetEndpoint(Uri requestUri)
        => requestUri.AbsolutePath;
}
