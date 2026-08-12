using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WinAppDtudo.Services;

public sealed class ApiDiscogsService : IApiDiscogsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly WinAppAuthenticationService? _authenticationService;
    private readonly ApiDiscogsStartupService? _startupService;
    private readonly bool _allowUnauthenticatedTransport;

    public static string ApiBase => AppConfigurationService.ApiDiscogsBaseUrl;

    public ApiDiscogsService(
        WinAppAuthenticationService? authenticationService = null,
        HttpClient? httpClient = null,
        ApiDiscogsStartupService? startupService = null)
    {
        var suppliedHttpClient = httpClient is not null;
        _authenticationService = authenticationService;
        _httpClient = httpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _httpClient.BaseAddress ??= new Uri(ApiBase.TrimEnd('/') + "/", UriKind.Absolute);
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _allowUnauthenticatedTransport = suppliedHttpClient && authenticationService is null;
        _startupService = startupService
            ?? (!suppliedHttpClient
                ? new ApiDiscogsStartupService(new ApiDiscogsHealthCheckService(authenticationService))
                : null);
    }

    public Task<ApiDiscogsPagedResponse<ApiDiscogsArtistSearchItem>> BuscarArtistasAsync(
        string query,
        int page = 1,
        int perPage = 10,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Informe um nome de artista ou banda.", nameof(query));
        }

        var safePage = Math.Max(1, page);
        var safePerPage = Math.Clamp(perPage, 1, 100);
        var path = $"ApiDiscogs/artists/search?q={Uri.EscapeDataString(query.Trim())}" +
                   $"&page={safePage}&perPage={safePerPage}";
        return GetAsync<ApiDiscogsPagedResponse<ApiDiscogsArtistSearchItem>>(
            path,
            "busca de artistas e bandas",
            "A ApiDiscogs retornou uma busca vazia.",
            progress,
            cancellationToken);
    }

    public Task<ApiDiscogsArtistDetails> ObterArtistaAsync(
        string artistId,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
        => GetAsync<ApiDiscogsArtistDetails>(
            $"ApiDiscogs/artists/{NormalizeResourceId(artistId, nameof(artistId))}",
            "detalhes do artista",
            "A ApiDiscogs nao retornou os detalhes do artista.",
            progress,
            cancellationToken);

    public Task<ApiDiscogsArtistReleasesResponse> ObterDiscografiaAsync(
        string artistId,
        int page = 1,
        int perPage = 50,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePerPage = Math.Clamp(perPage, 1, 100);
        var normalizedId = NormalizeResourceId(artistId, nameof(artistId));
        return GetAsync<ApiDiscogsArtistReleasesResponse>(
            $"ApiDiscogs/artists/{normalizedId}/releases?page={safePage}&perPage={safePerPage}",
            "consulta da discografia",
            "A ApiDiscogs retornou uma discografia vazia.",
            progress,
            cancellationToken);
    }

    public Task<ApiDiscogsReleaseDetails> ObterReleaseAsync(
        string releaseId,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
        => GetAsync<ApiDiscogsReleaseDetails>(
            $"ApiDiscogs/releases/{NormalizeResourceId(releaseId, nameof(releaseId))}",
            "detalhes do release",
            "A ApiDiscogs nao retornou os detalhes do release.",
            progress,
            cancellationToken);

    public Task<ApiDiscogsMasterDetails> ObterMasterAsync(
        string masterId,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
        => GetAsync<ApiDiscogsMasterDetails>(
            $"ApiDiscogs/masters/{NormalizeResourceId(masterId, nameof(masterId))}",
            "detalhes do master release",
            "A ApiDiscogs nao retornou os detalhes do master release.",
            progress,
            cancellationToken);

    private async Task<T> GetAsync<T>(
        string path,
        string operation,
        string emptyMessage,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            Report(progress, $"{operation}: verificando disponibilidade da API local.");
            await (_startupService?.EnsureReadyAsync(cancellationToken) ?? Task.CompletedTask);
            Report(progress, $"{operation}: enviando requisicao autenticada para a ApiDiscogs.");

            using var response = await SendAsync(path, cancellationToken);
            Report(progress, $"{operation}: resposta HTTP {(int)response.StatusCode} recebida.");
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateHttpExceptionAsync(response, operation, cancellationToken);
            }

            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException(emptyMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Report(progress, $"{operation}: operacao cancelada.");
            throw;
        }
        catch (ApiDiscogsHttpException exception)
        {
            Report(progress, $"{operation}: falha externa HTTP {(int)exception.ResponseStatusCode}.");
            StartupDiagnostics.Record($"ApiDiscogs {operation}", exception);
            throw;
        }
        catch (Exception exception)
        {
            Report(progress, $"{operation}: falha registrada sem expor credenciais.");
            StartupDiagnostics.Record($"ApiDiscogs {operation}", exception);
            throw;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (_authenticationService is null && !_allowUnauthenticatedTransport)
        {
            throw new WinAppAuthenticationException(
                "A sessao administrativa do WinApp nao esta configurada para consultar a ApiDiscogs.");
        }

        if (_authenticationService is not null)
        {
            return await _authenticationService.SendAuthenticatedAsync(
                _httpClient,
                _ => new HttpRequestMessage(HttpMethod.Get, path),
                cancellationToken);
        }

        return await _httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, path),
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    private static async Task<ApiDiscogsHttpException> CreateHttpExceptionAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var retryAfter = ReadRetryAfterSeconds(response);
        string? errorCode = null;
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("code", out var codeElement))
                {
                    errorCode = codeElement.GetString();
                }

                if (retryAfter is null
                    && document.RootElement.TryGetProperty("retryAfterSeconds", out var retryElement)
                    && retryElement.ValueKind == JsonValueKind.Number
                    && retryElement.TryGetInt32(out var retryFromBody))
                {
                    retryAfter = Math.Max(0, retryFromBody);
                }
            }
        }
        catch (JsonException)
        {
        }

        var message = response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => retryAfter is { } seconds
                ? $"A ApiDiscogs limitou a consulta durante aproximadamente {seconds} segundo(s)."
                : "A ApiDiscogs limitou temporariamente a consulta externa.",
            HttpStatusCode.BadGateway => "A ApiDiscogs recebeu uma falha da fonte externa.",
            HttpStatusCode.ServiceUnavailable => "A ApiDiscogs esta temporariamente indisponivel.",
            HttpStatusCode.GatewayTimeout => "A ApiDiscogs nao recebeu resposta da fonte externa a tempo.",
            HttpStatusCode.NotFound => $"O recurso consultado nao foi encontrado na ApiDiscogs ({operation}).",
            _ => $"A ApiDiscogs retornou HTTP {(int)response.StatusCode} durante {operation}."
        };
        return new ApiDiscogsHttpException(response.StatusCode, message, retryAfter, errorCode);
    }

    private static int? ReadRetryAfterSeconds(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            return Math.Max(0, (int)Math.Ceiling(delta.TotalSeconds));
        }

        return response.Headers.TryGetValues("Retry-After", out var values)
            && int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            ? Math.Max(0, seconds)
            : null;
    }

    private static string NormalizeResourceId(string value, string parameterName)
    {
        if (!int.TryParse(
                value?.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var id)
            || id <= 0)
        {
            throw new ArgumentException(
                "O ID Discogs deve ser um numero inteiro positivo.",
                parameterName);
        }

        return id.ToString(CultureInfo.InvariantCulture);
    }

    private static void Report(IProgress<string>? progress, string message)
        => progress?.Report($"ApiDiscogs: {message}");
}
