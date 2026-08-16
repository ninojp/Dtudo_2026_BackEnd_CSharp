using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Search;
using System.Net;

namespace WinAppDtudo.Services;

public class ApiMyAnimesService
{
    private const int MaxResultadosBusca = 100;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient;
    private readonly WinAppAuthenticationService? _authenticationService;
    private readonly ApiMyAnimesStartupService? _startupService;

    public static string ApiBase => AppConfigurationService.ApiMyAnimesBaseUrl;

    public ApiMyAnimesService(
        WinAppAuthenticationService? authenticationService = null,
        HttpClient? httpClient = null,
        ApiMyAnimesStartupService? startupService = null)
    {
        _authenticationService = authenticationService;
        _httpClient = httpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _httpClient.BaseAddress ??= new Uri(ApiBase.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _startupService = startupService
            ?? (httpClient is null ? new ApiMyAnimesStartupService() : null);
    }

    public async Task<EnsureMyAnimeCollectionResponse> GarantirMyAnimeColecaoAsync(
        AdicionaMyAnimeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var request = new EnsureMyAnimeCollectionRequest
        {
            Titulo = dto.Titulo,
            AnimesMalId = dto.AnimesMalId
        };

        using var response = await SendJsonAsync(
            HttpMethod.Put,
            "apiLocal/catalog-migration/my-animes/by-title",
            request,
            requiresAuthentication: true,
            cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<EnsureMyAnimeCollectionResponse>(
            _jsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("A ApiMyAnimes retornou uma colecao vazia.");
    }

    public async Task<ApiAnimesBuscaResult> BuscarAnimesPorTituloAsync(string query, int page = 1, int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new ApiAnimesBuscaResult
            {
                CurrentPage = 1,
                TotalPages = 1,
                HasNextPage = false,
                TotalResults = 0,
                Results = []
            };

        var termoEscapado = Uri.EscapeDataString(query.Trim());
        using var response = await GetAsync(
            $"apiLocal/Anime/buscar?termo={termoEscapado}&take={MaxResultadosBusca}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var resultados = JsonSerializer.Deserialize<List<ObterAnimeDto>>(json, _jsonOptions) ?? [];
        var totalResultados = Math.Min(resultados.Count, MaxResultadosBusca);
        var tamanhoPagina = Math.Max(1, pageSize);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalResultados / (double)tamanhoPagina));
        var paginaAtual = Math.Min(Math.Max(1, page), totalPaginas);

        return new ApiAnimesBuscaResult
        {
            Results = resultados
                .Skip((paginaAtual - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToList(),
            CurrentPage = paginaAtual,
            TotalPages = totalPaginas,
            HasNextPage = paginaAtual < totalPaginas,
            TotalResults = totalResultados
        };
    }

    public async Task<ApiMyColecoesBuscaResult> BuscarMyAnimesPorTituloAsync(string query, int page = 1, int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new ApiMyColecoesBuscaResult
            {
                CurrentPage = 1,
                TotalPages = 1,
                HasNextPage = false,
                TotalResults = 0,
                Results = []
            };

        var termo = AnimeSearchTextNormalizer.Normalize(query);
        if (termo.IsEmpty)
            return new ApiMyColecoesBuscaResult
            {
                CurrentPage = 1,
                TotalPages = 1,
                HasNextPage = false,
                TotalResults = 0,
                Results = []
            };

        var todos = new List<ObterMyAnimeDto>();
        var skip = 0;
        const int take = 200;

        while (true)
        {
            var paginaMyAnimes = await ObterMyAnimesAsync(skip, take);
            if (paginaMyAnimes.Count == 0)
                break;

            todos.AddRange(paginaMyAnimes);

            if (paginaMyAnimes.Count < take)
                break;

            skip += take;
        }

        var filtrados = todos
            .Where(m => AnimeSearchTextNormalizer.Normalize(m.Titulo).Matches(termo))
            .OrderBy(m => m.Titulo)
            .ThenBy(m => m.Id)
            .ToList();

        var totalResultados = filtrados.Count;
        var tamanhoPagina = Math.Max(1, pageSize);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalResultados / (double)tamanhoPagina));
        var paginaAtual = Math.Min(Math.Max(1, page), totalPaginas);

        var paginaResultados = filtrados
            .Skip((paginaAtual - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToList();

        return new ApiMyColecoesBuscaResult
        {
            Results = paginaResultados,
            CurrentPage = paginaAtual,
            TotalPages = totalPaginas,
            HasNextPage = paginaAtual < totalPaginas,
            TotalResults = totalResultados
        };
    }

    public async Task<List<ObterMyAnimeDto>> ObterMyAnimesAsync(int skip = 0, int take = 100)
    {
        using var response = await GetAsync($"apiLocal/MyAnime?skip={skip}&take={take}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ObterMyAnimeDto>>(json, _jsonOptions) ?? [];
    }

    public async Task<ObterMyAnimeDto?> ObterMyAnimePorIdAsync(int id)
    {
        using var response = await GetAsync($"apiLocal/MyAnime/{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ObterMyAnimeDto>(json, _jsonOptions);
    }

    public async Task<int?> AdicionarMyAnimeAsync(
        AdicionaMyAnimeDto dto,
        CancellationToken cancellationToken = default)
    {
        var response = await GarantirMyAnimeColecaoAsync(dto, cancellationToken);
        return response.Id;
    }

    public async Task AtualizarMyAnimeAsync(
        int id,
        AtualizaMyAnimeDto dto,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendJsonAsync(
            HttpMethod.Put,
            $"apiLocal/MyAnime/{id}",
            dto,
            requiresAuthentication: true,
            cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);
    }

    public async Task RemoverMyAnimeAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"apiLocal/MyAnime/{id}",
            contentFactory: null,
            requiresAuthentication: true,
            cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);
    }

    public async Task<EnsureAnimeAssociationResponse> AssociarAnimeAoMyAnimeAsync(
        int malId,
        int myAnimeId,
        CancellationToken cancellationToken = default)
    {
        var request = new EnsureAnimeAssociationRequest { MyAnimeId = myAnimeId };
        using var response = await SendJsonAsync(
            HttpMethod.Put,
            $"apiLocal/catalog-migration/animes/{malId}/my-anime",
            request,
            requiresAuthentication: true,
            cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<EnsureAnimeAssociationResponse>(
            _jsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("A ApiMyAnimes retornou uma associacao vazia.");
    }

    public async Task AdicionarAnimeAsync(
        AdicionaAnimeDto dto,
        CancellationToken cancellationToken = default)
    {
        var animeExistente = await ObterAnimePorMalIdAsync(dto.MalId);
        if (animeExistente is not null)
            throw new HttpRequestException($"Anime com MalId {dto.MalId} já existe.", null, HttpStatusCode.Conflict);

        using var response = await SendJsonAsync(
            HttpMethod.Post,
            "apiLocal/Anime",
            dto,
            requiresAuthentication: true,
            cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);
    }

    public async Task<ConflitoTituloAnimeDto?> BuscarConflitoDeTituloAsync(
        AdicionaAnimeDto dto,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendJsonAsync(
            HttpMethod.Post,
            "apiLocal/Anime/conflito-titulo",
            dto,
            requiresAuthentication: true,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ConflitoTituloAnimeDto>(_jsonOptions, cancellationToken);
    }

    public async Task AtualizarAnimeAsync(
        int malId,
        AtualizaAnimeDto dto,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendJsonAsync(
            HttpMethod.Put,
            $"apiLocal/Anime/{malId}",
            dto,
            requiresAuthentication: true,
            cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);
    }

    public async Task AtualizarAnimesRelacionadosIdsAsync(
        int malId,
        IReadOnlyCollection<int> animesRelacionadosIds,
        CancellationToken cancellationToken = default)
    {
        var patch = new[]
        {
            new
            {
                op = "replace",
                path = "/AnimesRelacionadosIds",
                value = animesRelacionadosIds.Where(id => id > 0).Distinct().ToList()
            }
        };

        using var response = await SendJsonAsync(
            HttpMethod.Patch,
            $"apiLocal/Anime/{malId}",
            patch,
            requiresAuthentication: true,
            cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);
    }

    public async Task RemoverAnimeAsync(int malId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"apiLocal/Anime/{malId}",
            contentFactory: null,
            requiresAuthentication: true,
            cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);
    }

    public async Task<List<ObterAnimeDto>> ObterAnimesAsync(int skip = 0, int take = 100)
    {
        using var response = await GetAsync($"apiLocal/Anime?skip={skip}&take={take}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ObterAnimeDto>>(json, _jsonOptions) ?? [];
    }

    public async Task<ObterAnimeDto?> ObterAnimePorMalIdAsync(int malId)
    {
        using var response = await GetAsync($"apiLocal/Anime/{malId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ObterAnimeDto>(json, _jsonOptions);
    }

    public async Task<List<ObterAnimeDto>> ObterAnimesPorMyAnimeIdAsync(int myAnimeId, int take = 200)
    {
        var skip = 0;
        var filtrados = new List<ObterAnimeDto>();

        while (true)
        {
            var pagina = await ObterAnimesAsync(skip, take);
            if (pagina.Count == 0)
                break;

            filtrados.AddRange(pagina.Where(a => a.MyAnimeID == myAnimeId));

            if (pagina.Count < take)
                break;

            skip += take;
        }

        return filtrados
            .OrderBy(a => a.Year ?? int.MaxValue)
            .ThenBy(a => a.Titulo)
            .ToList();
    }

    private async Task<HttpResponseMessage> SendJsonAsync<T>(
        HttpMethod method,
        string path,
        T payload,
        bool requiresAuthentication,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        return await SendAsync(
            method,
            path,
            () => new StringContent(json, Encoding.UTF8, "application/json"),
            requiresAuthentication,
            cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        Func<HttpContent?>? contentFactory,
        bool requiresAuthentication,
        CancellationToken cancellationToken)
    {
        await EnsureApiReadyAsync(cancellationToken);
        if (requiresAuthentication)
        {
            if (_authenticationService is null)
                throw new WinAppAuthenticationException("A sessao administrativa do WinApp nao esta configurada.");

            return await _authenticationService.SendAuthenticatedAsync(
                _httpClient,
                _ => CreateRequest(method, path, contentFactory),
                cancellationToken);
        }

        using var request = CreateRequest(method, path, contentFactory);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> GetAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        await EnsureApiReadyAsync(cancellationToken);
        return await _httpClient.GetAsync(path, cancellationToken);
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

    private static async Task EnsureSuccessStatusCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        detail = ObterDetalhesValidacao(detail);
        if (detail.Length > 600)
            detail = detail[..600];

        var message = string.IsNullOrWhiteSpace(detail)
            ? $"A ApiMyAnimes retornou {(int)response.StatusCode} ({response.ReasonPhrase})."
            : $"A ApiMyAnimes retornou {(int)response.StatusCode}: {detail.Trim()}";

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static string ObterDetalhesValidacao(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Object)
            {
                return responseBody;
            }

            var messages = errors.EnumerateObject()
                .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array
                    ? error.Value.EnumerateArray()
                        .Select(message => $"{error.Name}: {message.GetString()}")
                    : [$"{error.Name}: {error.Value}"])
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();

            return messages.Count > 0
                ? string.Join(Environment.NewLine, messages)
                : responseBody;
        }
        catch (JsonException)
        {
            return responseBody;
        }
    }

    public async Task<ObterMyAnimeDto?> ObterMyAnimePorTituloAsync(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            return null;

        const int take = 200;
        var skip = 0;

        while (true)
        {
            var pagina = await ObterMyAnimesAsync(skip, take);
            if (pagina.Count == 0)
                return null;

            var existente = pagina.FirstOrDefault(item =>
                string.Equals(item.Titulo?.Trim(), titulo, StringComparison.OrdinalIgnoreCase));
            if (existente is not null)
                return existente;

            if (pagina.Count < take)
                return null;

            skip += take;
        }
    }
}
