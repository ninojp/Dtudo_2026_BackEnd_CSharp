using System.Net.Http.Json;
using System.Text.Json;

namespace WinAppDtudo.Services;

public sealed record WinAppStorageExportPlanItem(int MalId, string ObjectId);

public sealed record WinAppStorageExportAnime(
    int MalId,
    int? Year,
    string? Title,
    string? Type);

public sealed record WinAppStorageExportDestination(
    string Id,
    string DisplayName);

public sealed record WinAppStorageExportPlan(
    int MyAnimeId,
    IReadOnlyList<WinAppStorageExportPlanItem> Items);

public sealed record WinAppStorageImportResult(
    string ObjectId,
    string Sha256,
    long Length,
    DateTimeOffset PromotedAtUtc,
    bool Replayed);

public sealed record WinAppStorageDeletePreviewItem(
    string ObjectId,
    string Kind,
    long Length,
    DateTimeOffset LastWriteTimeUtc);

public sealed record WinAppStorageDeletePreview(
    Guid PreviewId,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<WinAppStorageDeletePreviewItem> Items);

public sealed record WinAppStorageDeleteItem(
    string ObjectId,
    string Status,
    string? Sha256,
    DateTimeOffset? PurgeAtUtc);

public sealed record WinAppStorageDeleteBatch(
    Guid PreviewId,
    IReadOnlyList<WinAppStorageDeleteItem> Items);

public sealed record WinAppStepUpGrant(Guid GrantId, DateTimeOffset ExpiresAtUtc);

public interface IFileStorageApiClient
{
    Task<IReadOnlyList<WinAppStorageExportDestination>> GetExportDestinationsAsync(
        CancellationToken cancellationToken = default);

    Task<WinAppStorageExportPlan> PrepareExportAsync(
        int myAnimeId,
        string myAnimeTitle,
        IReadOnlyCollection<WinAppStorageExportAnime> animes,
        string? destinationId = null,
        CancellationToken cancellationToken = default);

    Task<WinAppStorageImportResult> ImportAsync(
        string objectId,
        string fileName,
        string contentType,
        ReadOnlyMemory<byte> content,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<WinAppStorageDeletePreview> PreviewDeleteAsync(
        IReadOnlyCollection<string> objectIds,
        CancellationToken cancellationToken = default);

    Task<WinAppStepUpGrant> GrantDeleteStepUpAsync(
        string totpToken,
        CancellationToken cancellationToken = default);

    Task<WinAppStorageDeleteBatch> DeleteBatchAsync(
        Guid previewId,
        CancellationToken cancellationToken = default);
}

public sealed class FileStorageApiClient : IFileStorageApiClient
{
    private const string StepUpAction = "filesystem.command";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _storageHttpClient;
    private readonly HttpClient _identityHttpClient;
    private readonly WinAppAuthenticationService _authenticationService;
    private readonly ApiFileStorageStartupService? _startupService;

    public static string ApiBase => AppConfigurationService.ApiFileStorageBaseUrl;

    public FileStorageApiClient(
        WinAppAuthenticationService? authenticationService = null,
        HttpClient? storageHttpClient = null,
        HttpClient? identityHttpClient = null,
        ApiFileStorageStartupService? startupService = null)
    {
        _authenticationService = authenticationService ?? new WinAppAuthenticationService();
        _storageHttpClient = storageHttpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _identityHttpClient = identityHttpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _storageHttpClient.BaseAddress ??= new Uri(ApiBase.TrimEnd('/') + "/", UriKind.Absolute);
        _identityHttpClient.BaseAddress ??= new Uri(AppConfigurationService.ApiIdentityBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _storageHttpClient.Timeout = TimeSpan.FromSeconds(120);
        _identityHttpClient.Timeout = TimeSpan.FromSeconds(30);
        _startupService = startupService
            ?? (storageHttpClient is null && identityHttpClient is null
                ? new ApiFileStorageStartupService()
                : null);
    }

    public async Task<IReadOnlyList<WinAppStorageExportDestination>> GetExportDestinationsAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);
        using var response = await _authenticationService.SendAuthenticatedAsync(
            _storageHttpClient,
            _ =>
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    "api/file-storage/export/destinations");
                AddSessionHeaders(request);
                return request;
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WinAppStorageExportDestination[]>(
                JsonOptions,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "A ApiFileStorage retornou uma lista de destinos vazia.");
    }

    public async Task<WinAppStorageExportPlan> PrepareExportAsync(
        int myAnimeId,
        string myAnimeTitle,
        IReadOnlyCollection<WinAppStorageExportAnime> animes,
        string? destinationId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);
        var payload = new
        {
            MyAnimeId = myAnimeId,
            MyAnimeTitle = myAnimeTitle,
            DestinationId = destinationId,
            Animes = animes
                .GroupBy(anime => anime.MalId)
                .Select(group => group.First())
                .OrderBy(anime => anime.Year ?? int.MaxValue)
                .ThenBy(anime => anime.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(anime => anime.MalId)
                .ToArray()
        };
        using var response = await SendJsonAsync(
            _storageHttpClient,
            HttpMethod.Post,
            "api/file-storage/export/plan",
            payload,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WinAppStorageExportPlan>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("A ApiFileStorage retornou um plano vazio.");
    }

    public async Task<WinAppStorageImportResult> ImportAsync(
        string objectId,
        string fileName,
        string contentType,
        ReadOnlyMemory<byte> content,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);
        var payload = content.ToArray();
        using var response = await _authenticationService.SendAuthenticatedAsync(
            _storageHttpClient,
            _ => CreateMultipartRequest(objectId, fileName, contentType, payload, idempotencyKey),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WinAppStorageImportResult>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("A ApiFileStorage retornou uma importacao vazia.");
    }

    public async Task<WinAppStorageDeletePreview> PreviewDeleteAsync(
        IReadOnlyCollection<string> objectIds,
        CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);
        using var response = await SendJsonAsync(
            _storageHttpClient,
            HttpMethod.Post,
            "api/file-storage/delete/preview",
            new { ObjectIds = objectIds.Distinct(StringComparer.Ordinal).ToArray() },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WinAppStorageDeletePreview>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("A ApiFileStorage retornou uma previa vazia.");
    }

    public async Task<WinAppStepUpGrant> GrantDeleteStepUpAsync(
        string totpToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(totpToken))
        {
            throw new ArgumentException("O codigo TOTP deve ser informado.", nameof(totpToken));
        }

        await _authenticationService.GetAccessTokenAsync(cancellationToken);
        var session = _authenticationService.CurrentSession
            ?? throw new WinAppAuthenticationException("A sessao administrativa do WinApp nao esta configurada.");
        using var response = await SendJsonAsync(
            _identityHttpClient,
            HttpMethod.Post,
            "identity/security/totp/step-up",
            new
            {
                Action = StepUpAction,
                Token = totpToken.Trim(),
                SessionId = session.SessionId,
                DeviceId = session.DeviceId
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WinAppStepUpGrant>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("A ApiIdentity nao retornou o grant de step-up.");
    }

    public async Task<WinAppStorageDeleteBatch> DeleteBatchAsync(
        Guid previewId,
        CancellationToken cancellationToken = default)
    {
        await EnsureStorageReadyAsync(cancellationToken);
        using var response = await SendJsonAsync(
            _storageHttpClient,
            HttpMethod.Post,
            "api/file-storage/delete/batch",
            new { PreviewId = previewId },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WinAppStorageDeleteBatch>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("A ApiFileStorage retornou um resultado de exclusao vazio.");
    }

    private Task EnsureStorageReadyAsync(CancellationToken cancellationToken) =>
        _startupService?.EnsureReadyAsync(cancellationToken) ?? Task.CompletedTask;

    private async Task<HttpResponseMessage> SendJsonAsync(
        HttpClient httpClient,
        HttpMethod method,
        string path,
        object payload,
        CancellationToken cancellationToken)
    {
        return await _authenticationService.SendAuthenticatedAsync(
            httpClient,
            _ =>
            {
                var request = new HttpRequestMessage(method, path)
                {
                    Content = JsonContent.Create(payload)
                };
                AddSessionHeaders(request);
                return request;
            },
            cancellationToken);
    }

    private HttpRequestMessage CreateMultipartRequest(
        string objectId,
        string fileName,
        string contentType,
        byte[] content,
        string idempotencyKey)
    {
        var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(objectId), "objectId");

        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        multipart.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, "api/file-storage/import")
        {
            Content = multipart
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        AddSessionHeaders(request);
        return request;
    }

    private void AddSessionHeaders(HttpRequestMessage request)
    {
        var session = _authenticationService.CurrentSession
            ?? throw new WinAppAuthenticationException("A sessao administrativa do WinApp nao esta configurada.");
        request.Headers.TryAddWithoutValidation(
            "X-Dtudo-Session-Id",
            session.SessionId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            "X-Dtudo-Device-Id",
            session.DeviceId.ToString("D"));
    }
}
