using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using ApiFileStorage.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Services;

public sealed record PrepareStorageExportCommand(
    int MyAnimeId,
    IReadOnlyCollection<int> MalIds);

public sealed record PreparedStorageObject(
    int MalId,
    string ObjectId);

public sealed record PrepareStorageExportResult(
    int MyAnimeId,
    IReadOnlyList<PreparedStorageObject> Items);

public interface IFileStorageCommandService
{
    PrepareStorageExportResult PrepareExport(PrepareStorageExportCommand command);
}

public sealed class FileStorageCommandService(
    StorageRootCatalog rootCatalog,
    IOptions<FileStorageOptions> options) : IFileStorageCommandService
{
    private readonly StorageRootCatalog _rootCatalog = rootCatalog;
    private readonly FileStorageOptions _options = options.Value;

    public PrepareStorageExportResult PrepareExport(PrepareStorageExportCommand command)
    {
        if (command.MyAnimeId <= 0
            || command.MalIds is null
            || command.MalIds.Count == 0
            || command.MalIds.Count > _options.Limits.MaxBulkDeleteItems
            || command.MalIds.Any(malId => malId <= 0))
        {
            throw new FileStorageValidationException();
        }

        _rootCatalog.Get(_options.ExportRootId);
        var distinctMalIds = command.MalIds
            .Distinct()
            .OrderBy(malId => malId)
            .ToArray();
        var prefix = _options.ExportPathPrefix.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new FileStorageValidationException();
        }

        var items = distinctMalIds
            .Select(malId => new PreparedStorageObject(
                malId,
                StorageObjectId.Create(
                    _options.ExportRootId,
                    $"{prefix}/{command.MyAnimeId}/{malId}.jpg")))
            .ToArray();

        return new PrepareStorageExportResult(command.MyAnimeId, items);
    }
}

public sealed record FileStorageDeletePreview(
    Guid PreviewId,
    string Subject,
    string SessionId,
    string DeviceId,
    IReadOnlyList<string> ObjectIds,
    DateTimeOffset ExpiresAtUtc);

public sealed class FileStorageDeletePreviewStore(
    TimeProvider timeProvider,
    IOptions<FileStorageOptions> options)
{
    private readonly ConcurrentDictionary<Guid, FileStorageDeletePreview> _previews = [];
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly FileStorageOptions _options = options.Value;

    public FileStorageDeletePreview Create(
        ClaimsPrincipal principal,
        string sessionId,
        string deviceId,
        IReadOnlyList<string> objectIds)
    {
        var subject = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new FileStorageValidationException();
        }

        var now = _timeProvider.GetUtcNow();
        var preview = new FileStorageDeletePreview(
            Guid.NewGuid(),
            subject,
            sessionId,
            deviceId,
            objectIds,
            now.AddSeconds(_options.Limits.DeletePreviewLifetimeSeconds));
        _previews[preview.PreviewId] = preview;
        RemoveExpired(now);
        return preview;
    }

    public bool TryGet(
        Guid previewId,
        ClaimsPrincipal principal,
        string sessionId,
        string deviceId,
        out FileStorageDeletePreview? preview)
    {
        preview = null;
        var subject = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject)
            || !_previews.TryGetValue(previewId, out var candidate))
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        if (candidate.ExpiresAtUtc <= now
            || !string.Equals(candidate.Subject, subject, StringComparison.Ordinal)
            || !string.Equals(candidate.SessionId, sessionId, StringComparison.Ordinal)
            || !string.Equals(candidate.DeviceId, deviceId, StringComparison.Ordinal))
        {
            _previews.TryRemove(previewId, out _);
            return false;
        }

        preview = candidate;
        return true;
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var pair in _previews)
        {
            if (pair.Value.ExpiresAtUtc <= now)
            {
                _previews.TryRemove(pair.Key, out _);
            }
        }
    }
}

public interface IFileStorageStepUpValidator
{
    Task<bool> IsAllowedAsync(CancellationToken cancellationToken = default);
}

public sealed class IdentityFileStorageStepUpValidator(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<FileStorageOptions> options) : IFileStorageStepUpValidator
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly FileStorageStepUpOptions _options = options.Value.StepUp;

    public async Task<bool> IsAllowedAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null
            || !Guid.TryParse(
                httpContext.Request.Headers[FileStorageRequestHeaders.SessionId].FirstOrDefault(),
                out var sessionId)
            || !Guid.TryParse(
                httpContext.Request.Headers[FileStorageRequestHeaders.DeviceId].FirstOrDefault(),
                out var deviceId))
        {
            return false;
        }

        var authorization = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return false;
        }

        var path = $"identity/security/step-up/{Uri.EscapeDataString(_options.Action)}" +
                   $"?sessionId={sessionId:D}&deviceId={deviceId:D}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode == HttpStatusCode.NoContent;
    }
}

public static class FileStorageRequestHeaders
{
    public const string SessionId = "X-Dtudo-Session-Id";
    public const string DeviceId = "X-Dtudo-Device-Id";
}
