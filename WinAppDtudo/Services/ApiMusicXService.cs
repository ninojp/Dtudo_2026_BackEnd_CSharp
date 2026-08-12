using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace WinAppDtudo.Services;

public sealed class ApiMusicXService : IApiMusicXCollectionImporter, IApiMusicXLocalConflictReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly WinAppAuthenticationService? _authenticationService;
    private readonly ApiMusicXStartupService? _startupService;

    public static string ApiBase => AppConfigurationService.ApiMusicXBaseUrl;

    public ApiMusicXService(
        WinAppAuthenticationService? authenticationService = null,
        HttpClient? httpClient = null,
        ApiMusicXStartupService? startupService = null)
    {
        _authenticationService = authenticationService;
        _httpClient = httpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _httpClient.BaseAddress ??= new Uri(ApiBase.TrimEnd('/') + "/", UriKind.Absolute);
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _startupService = startupService
            ?? (httpClient is null
                ? new ApiMusicXStartupService(new ApiMusicXHealthCheckService(authenticationService))
                : null);
    }

    public async Task<ApiMusicXPagedResponse<ApiMusicXCollectionSummaryDto>> ObterColecoesAsync(
        ApiMusicXCollectionQuery? query = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        query ??= new ApiMusicXCollectionQuery();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var path = $"apiLocal/collections?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            path += $"&search={Uri.EscapeDataString(query.Search.Trim())}";
        }

        using var response = await SendAsync(
            HttpMethod.Get,
            path,
            contentFactory: null,
            operation: "leitura de Colecoes",
            progress,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ApiMusicXPagedResponse<ApiMusicXCollectionSummaryDto>>(
            response,
            "A ApiMusicX retornou uma lista de Colecoes vazia.",
            cancellationToken);
    }

    public async Task<ApiMusicXCollectionDto?> ObterColecaoPorIdAsync(
        long id,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"apiLocal/collections/{id}",
            contentFactory: null,
            operation: "leitura da Colecao",
            progress,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ApiMusicXCollectionDto>(
            response,
            "A ApiMusicX retornou uma Colecao vazia.",
            cancellationToken);
    }

    public async Task<ApiMusicXPagedResponse<ApiMusicXReleaseDto>> ObterReleasesDaColecaoAsync(
        long collectionId,
        int page = 1,
        int pageSize = 20,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        using var response = await SendAsync(
            HttpMethod.Get,
            $"apiLocal/collections/{collectionId}/releases?page={safePage}&pageSize={safePageSize}",
            contentFactory: null,
            operation: "leitura de releases",
            progress,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ApiMusicXPagedResponse<ApiMusicXReleaseDto>>(
            response,
            "A ApiMusicX retornou uma lista de releases vazia.",
            cancellationToken);
    }

    public async Task<ApiMusicXPagedResponse<ApiMusicXArtistSummaryDto>> BuscarArtistasAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var path = $"apiLocal/artists?page={safePage}&pageSize={safePageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            path += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        using var response = await SendAsync(
            HttpMethod.Get,
            path,
            contentFactory: null,
            operation: "busca de artistas",
            progress,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ApiMusicXPagedResponse<ApiMusicXArtistSummaryDto>>(
            response,
            "A ApiMusicX retornou uma busca de artistas vazia.",
            cancellationToken);
    }

    public async Task<ApiMusicXArtistDto?> ObterArtistaPorIdAsync(
        long id,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"apiLocal/artists/{id}",
            contentFactory: null,
            operation: "leitura do artista",
            progress,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ApiMusicXArtistDto>(
            response,
            "A ApiMusicX retornou um artista vazio.",
            cancellationToken);
    }

    public async Task<ApiMusicXReleaseDto?> ObterReleasePorIdAsync(
        long id,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"apiLocal/releases/{id}",
            contentFactory: null,
            operation: "leitura do release",
            progress,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ApiMusicXReleaseDto>(
            response,
            "A ApiMusicX retornou um release vazio.",
            cancellationToken);
    }

    public async Task<ApiMusicXCollectionDto> CriarColecaoAsync(
        ApiMusicXCreateCollectionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var response = await SendJsonAsync(
            HttpMethod.Post,
            "apiLocal/collections",
            request,
            operation: "criacao da Colecao",
            progress,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadRequiredAsync<ApiMusicXCollectionDto>(
            response,
            "A ApiMusicX nao retornou a Colecao criada.",
            cancellationToken);
    }

    public async Task AtualizarColecaoAsync(
        long id,
        ApiMusicXUpdateCollectionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var response = await SendJsonAsync(
            HttpMethod.Put,
            $"apiLocal/collections/{id}",
            request,
            operation: "atualizacao da Colecao",
            progress,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoverColecaoAsync(
        long id,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"apiLocal/collections/{id}",
            contentFactory: null,
            operation: "exclusao da Colecao",
            progress,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ApiMusicXImportCollectionResponse> ImportarColecaoAsync(
        ApiMusicXImportCollectionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Report(progress, "importacao: preparando o conjunto normalizado.");
        using var response = await SendJsonAsync(
            HttpMethod.Post,
            "apiLocal/collections/import",
            request,
            operation: "importacao da Colecao",
            progress,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await ReadRequiredAsync<ApiMusicXImportCollectionResponse>(
            response,
            "A ApiMusicX nao retornou o resultado da importacao.",
            cancellationToken);
        Report(
            progress,
            $"importacao: concluida ({result.ArtistsAdded} artista(s), " +
            $"{result.ReleasesAdded} release(s), {result.TracksAdded} faixa(s)).");
        return result;
    }

    private async Task<HttpResponseMessage> SendJsonAsync<T>(
        HttpMethod method,
        string path,
        T payload,
        string operation,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return await SendAsync(
            method,
            path,
            () => new StringContent(json, Encoding.UTF8, "application/json"),
            operation,
            progress,
            cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        Func<HttpContent?>? contentFactory,
        string operation,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            Report(progress, $"{operation}: verificando disponibilidade da API.");
            await EnsureApiReadyAsync(cancellationToken);
            Report(progress, $"{operation}: enviando requisicao autenticada.");

            if (_authenticationService is null)
            {
                throw new WinAppAuthenticationException(
                    "A sessao administrativa do WinApp nao esta configurada.");
            }

            var response = await _authenticationService.SendAuthenticatedAsync(
                _httpClient,
                _ => CreateRequest(method, path, contentFactory),
                cancellationToken);
            Report(progress, $"{operation}: resposta HTTP {(int)response.StatusCode} recebida.");
            if (!response.IsSuccessStatusCode)
            {
                StartupDiagnostics.Record(
                    $"ApiMusicX {operation}",
                    new HttpRequestException(
                        $"A ApiMusicX retornou HTTP {(int)response.StatusCode}.",
                        inner: null,
                        statusCode: response.StatusCode));
                Report(progress, $"{operation}: falha HTTP registrada sem expor a resposta ou credenciais.");
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Report(progress, $"{operation}: operacao cancelada.");
            throw;
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record($"ApiMusicX {operation}", exception);
            Report(progress, $"{operation}: falha registrada sem expor credenciais.");
            throw;
        }
    }

    private Task EnsureApiReadyAsync(CancellationToken cancellationToken) =>
        _startupService?.EnsureReadyAsync(cancellationToken) ?? Task.CompletedTask;

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        Func<HttpContent?>? contentFactory)
    {
        return new HttpRequestMessage(method, path)
        {
            Content = contentFactory?.Invoke()
        };
    }

    private static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        string emptyMessage,
        CancellationToken cancellationToken)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException(emptyMessage);
    }

    private static void Report(IProgress<string>? progress, string message) => progress?.Report($"ApiMusicX: {message}");
}
