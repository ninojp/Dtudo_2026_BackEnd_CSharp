using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.FileMonitoring;

namespace WinAppDtudo.Services;

/// <summary>
/// Contrato do WinApp para a atualizacao do status de pasta dos animes (endpoints administrativos da ApiMyAnimes).
/// Existe para permitir testar o acompanhamento sem HTTP nem UI.
/// </summary>
public interface IAnimeFolderStatusApi
{
    /// <summary>Inicia a verificacao das pastas dos animes de uma colecao MyAnime.</summary>
    Task<AnimeFolderRefreshRunDto> StartCollectionRefreshAsync(int myAnimeId, CancellationToken cancellationToken);

    /// <summary>Inicia a verificacao das pastas de todas as colecoes.</summary>
    Task<AnimeFolderRefreshRunDto> StartAllRefreshAsync(CancellationToken cancellationToken);

    /// <summary>Consulta o progresso de uma execucao e, opcionalmente, o relatorio por anime (cursor <paramref name="afterItemId"/>).</summary>
    Task<AnimeFolderRefreshRunDto> GetRunAsync(
        Guid runId, bool includeItems, long afterItemId, int take, CancellationToken cancellationToken);

    /// <summary>Execucoes mais recentes (sem relatorio por anime).</summary>
    Task<IReadOnlyList<AnimeFolderRefreshRunDto>> ListRunsAsync(int take, CancellationToken cancellationToken);

    /// <summary>Pede o cancelamento de uma execucao em andamento.</summary>
    Task CancelRunAsync(Guid runId, CancellationToken cancellationToken);

    /// <summary>Ultimo status de pasta persistido dos animes (mais de 200 ids sao enviados em varias requisicoes).</summary>
    Task<IReadOnlyDictionary<int, AnimeFolderStatusInfo>> GetFolderStatusAsync(
        IEnumerable<int> malIds, CancellationToken cancellationToken);
}

/// <summary>
/// Cliente HTTP dos endpoints de atualizacao do status de pasta da ApiMyAnimes
/// (<c>apiLocal/MyAnime/.../folder-status/*</c> e o status em lote <c>apiLocal/Anime/status</c>), com o mesmo bearer/PKCE dos demais
/// clientes do WinApp. Nao acessa banco nem disco: a varredura das pastas e feita pela ApiMyAnimes via ApiFileStorage.
/// </summary>
public sealed class AnimeFolderStatusApiClient : IAnimeFolderStatusApi, IDisposable
{
    /// <summary>Limite de ids por requisicao do status em lote na ApiMyAnimes.</summary>
    public const int MaxMalIdsPerRequest = AnimeStatusRequestDto.MaxMalIdsPerRequest;

    private const string RefreshBasePath = "apiLocal/MyAnime/folder-status";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly WinAppAuthenticationService _authentication;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly ApiMyAnimesStartupService? _startupService;

    public AnimeFolderStatusApiClient(
        WinAppAuthenticationService authenticationService,
        HttpClient? httpClient = null,
        ApiMyAnimesStartupService? startupService = null)
    {
        _authentication = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler()) { Timeout = RequestTimeout };
        _httpClient.BaseAddress ??= new Uri(ApiMyAnimesService.ApiBase.TrimEnd('/') + "/");
        _startupService = startupService ?? (httpClient is null ? new ApiMyAnimesStartupService() : null);
    }

    public Task<AnimeFolderRefreshRunDto> StartCollectionRefreshAsync(int myAnimeId, CancellationToken cancellationToken)
    {
        if (myAnimeId <= 0) throw new ArgumentOutOfRangeException(nameof(myAnimeId), "Informe uma colecao MyAnime valida.");
        return SendAsync<AnimeFolderRefreshRunDto>(
            HttpMethod.Post, $"apiLocal/MyAnime/{myAnimeId}/folder-status/refresh", null, "A colecao MyAnime nao foi encontrada.", cancellationToken);
    }

    public Task<AnimeFolderRefreshRunDto> StartAllRefreshAsync(CancellationToken cancellationToken) =>
        SendAsync<AnimeFolderRefreshRunDto>(
            HttpMethod.Post, $"{RefreshBasePath}/refresh", null, "Endpoint de atualizacao nao encontrado na ApiMyAnimes.", cancellationToken);

    public Task<AnimeFolderRefreshRunDto> GetRunAsync(
        Guid runId, bool includeItems, long afterItemId, int take, CancellationToken cancellationToken)
    {
        var query = $"includeItems={(includeItems ? "true" : "false")}&afterId={Math.Max(0, afterItemId)}&take={Math.Clamp(take, 1, 500)}";
        return SendAsync<AnimeFolderRefreshRunDto>(
            HttpMethod.Get, $"{RefreshBasePath}/runs/{runId}?{query}", null,
            "A execucao nao foi encontrada (a ApiMyAnimes pode ter sido reiniciada).", cancellationToken);
    }

    public Task<IReadOnlyList<AnimeFolderRefreshRunDto>> ListRunsAsync(int take, CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<AnimeFolderRefreshRunDto>>(
            HttpMethod.Get, $"{RefreshBasePath}/runs?take={Math.Clamp(take, 1, 50)}", null,
            "Endpoint de execucoes nao encontrado na ApiMyAnimes.", cancellationToken);

    public async Task CancelRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(
            HttpMethod.Post, $"{RefreshBasePath}/runs/{runId}/cancel", null, cancellationToken);
        await EnsureSuccessAsync(response, "Nenhuma execucao em andamento com este identificador.", cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, AnimeFolderStatusInfo>> GetFolderStatusAsync(
        IEnumerable<int> malIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(malIds);
        var result = new Dictionary<int, AnimeFolderStatusInfo>();
        var ids = malIds.Where(id => id > 0).Distinct().ToList();
        for (var offset = 0; offset < ids.Count; offset += MaxMalIdsPerRequest)
        {
            var chunk = ids.Skip(offset).Take(MaxMalIdsPerRequest).ToArray();
            var response = await SendAsync<AnimeStatusResponseDto>(
                HttpMethod.Post, "apiLocal/Anime/status", new AnimeStatusRequestDto { MalIds = chunk }, "Endpoint de status nao encontrado na ApiMyAnimes.", cancellationToken);
            foreach (var item in response.Items ?? [])
            {
                if (item.Folder is null) continue;
                result[item.MalId] = new AnimeFolderStatusInfo(
                    item.MalId,
                    AnimeFolderState.Normalize(item.Folder.State),
                    Math.Max(0, item.Folder.Videos),
                    item.Folder.Expected,
                    item.Folder.CheckedAtUtc is { } checkedAt ? DateTime.SpecifyKind(checkedAt, DateTimeKind.Utc) : null);
            }
        }

        return result;
    }

    public void Dispose()
    {
        if (_ownsHttpClient) _httpClient.Dispose();
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method, string path, object? body, string notFoundMessage, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, path, body, cancellationToken);
        await EnsureSuccessAsync(response, notFoundMessage, cancellationToken);
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                ?? throw new FolderRefreshException(FolderRefreshFailureKind.InvalidResponse, "A ApiMyAnimes retornou uma resposta vazia.");
        }
        catch (JsonException exception)
        {
            throw new FolderRefreshException(
                FolderRefreshFailureKind.InvalidResponse, "A ApiMyAnimes retornou uma resposta fora do formato esperado.", response.StatusCode, null, exception);
        }
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        try
        {
            if (_startupService is not null) await EnsureApiReadyAsync(cancellationToken);
            return await _authentication.SendAuthenticatedAsync(
                _httpClient,
                _ => new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) },
                cancellationToken);
        }
        catch (WinAppAuthenticationException exception)
        {
            throw new FolderRefreshException(
                FolderRefreshFailureKind.Unauthorized,
                "A sessao do WinApp nao e valida ou expirou. Reconecte-se pelo menu Arquivo > Conectar e tente novamente.",
                HttpStatusCode.Unauthorized, null, exception);
        }
        catch (HttpRequestException exception)
        {
            throw new FolderRefreshException(
                FolderRefreshFailureKind.ApiUnavailable,
                $"Nao foi possivel conectar a ApiMyAnimes em {ApiMyAnimesService.ApiBase} (ou ao ApiIdentity para renovar a sessao). Verifique se as APIs locais estao em execucao.",
                null, null, exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FolderRefreshException(
                FolderRefreshFailureKind.ApiUnavailable,
                "A ApiMyAnimes nao respondeu a tempo. Verifique se a API esta em execucao e tente novamente.",
                null, null, exception);
        }
    }

    private async Task EnsureApiReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _startupService!.EnsureReadyAsync(cancellationToken);
        }
        catch (WinAppAuthenticationException exception)
        {
            // O servico de partida usa o mesmo tipo de excecao da sessao, mas aqui significa que a API nao subiu.
            throw new FolderRefreshException(FolderRefreshFailureKind.ApiUnavailable, exception.Message, innerException: exception);
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, string notFoundMessage, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var title = await ReadProblemTitleAsync(response, cancellationToken);
        var status = response.StatusCode;
        throw status switch
        {
            HttpStatusCode.Unauthorized => new FolderRefreshException(
                FolderRefreshFailureKind.Unauthorized,
                "A ApiMyAnimes recusou a sessao (401). Reconecte-se pelo menu Arquivo > Conectar e tente novamente.", status),
            HttpStatusCode.Forbidden => new FolderRefreshException(
                FolderRefreshFailureKind.Forbidden,
                "Voce nao tem permissao para atualizar o status das pastas (403). E necessario o acesso administrativo ao catalogo (catalog.write).", status),
            HttpStatusCode.NotFound => new FolderRefreshException(
                FolderRefreshFailureKind.NotFound, string.IsNullOrWhiteSpace(title) ? notFoundMessage : title, status),
            HttpStatusCode.Conflict => new FolderRefreshException(
                FolderRefreshFailureKind.Busy,
                "Ja existe uma verificacao de pastas em andamento na ApiMyAnimes.", status, ReadActiveRunId(response)),
            HttpStatusCode.ServiceUnavailable => new FolderRefreshException(
                FolderRefreshFailureKind.FileStorageNotConfigured,
                "A ApiMyAnimes nao esta configurada para falar com a ApiFileStorage. Verifique a configuracao e se a ApiFileStorage esta em execucao.", status),
            HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout => new FolderRefreshException(
                FolderRefreshFailureKind.ApiUnavailable, "A ApiMyAnimes esta temporariamente indisponivel.", status),
            _ => new FolderRefreshException(
                FolderRefreshFailureKind.Other,
                string.IsNullOrWhiteSpace(title)
                    ? $"A ApiMyAnimes retornou o erro {(int)status} ({response.ReasonPhrase})."
                    : $"A ApiMyAnimes retornou o erro {(int)status}: {title}",
                status)
        };
    }

    private static Guid? ReadActiveRunId(HttpResponseMessage response)
    {
        var location = response.Headers.Location?.OriginalString;
        if (string.IsNullOrWhiteSpace(location)) return null;
        var path = location.Split('?', 2)[0].TrimEnd('/');
        var lastSegment = path[(path.LastIndexOf('/') + 1)..];
        return Guid.TryParse(lastSegment, out var id) ? id : null;
    }

    private static async Task<string?> ReadProblemTitleAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body)) return null;
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("title", out var title)
                && title.ValueKind == JsonValueKind.String)
            {
                var text = title.GetString();
                return text is { Length: > 300 } ? text[..300] : text;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
