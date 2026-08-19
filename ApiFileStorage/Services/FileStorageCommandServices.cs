using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using ApiFileStorage.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Services;

public sealed record PrepareStorageExportAnime(
    int MalId,
    int? Year,
    string? Title,
    string? Type);

public sealed record PrepareStorageExportCommand(
    int MyAnimeId,
    string? MyAnimeTitle,
    IReadOnlyCollection<PrepareStorageExportAnime> Animes,
    string? DestinationId = null)
{
    public PrepareStorageExportCommand(int myAnimeId, IReadOnlyCollection<int> malIds)
        : this(
            myAnimeId,
            $"MyAnime_{myAnimeId}",
            malIds.Select(malId => new PrepareStorageExportAnime(malId, null, null, null)).ToArray())
    {
    }
}

public sealed record PreparedStorageObject(
    int MalId,
    string ObjectId);

public sealed record PrepareStorageExportResult(
    int MyAnimeId,
    IReadOnlyList<PreparedStorageObject> Items);

public sealed record StorageExportDestination(
    string Id,
    string DisplayName);

public interface IFileStorageCommandService
{
    IReadOnlyList<StorageExportDestination> GetExportDestinations();

    PrepareStorageExportResult PrepareExport(PrepareStorageExportCommand command);
}

public sealed class FileStorageCommandService(
    StorageRootCatalog rootCatalog,
    IOptions<FileStorageOptions> options) : IFileStorageCommandService
{
    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly StorageRootCatalog _rootCatalog = rootCatalog;
    private readonly FileStorageOptions _options = options.Value;

    public IReadOnlyList<StorageExportDestination> GetExportDestinations() =>
        GetConfiguredExportDestinations()
            .Select(destination => new StorageExportDestination(
                destination.Id,
                destination.DisplayName))
            .ToArray();

    public PrepareStorageExportResult PrepareExport(PrepareStorageExportCommand command)
    {
        if (command.MyAnimeId <= 0
            || string.IsNullOrWhiteSpace(command.MyAnimeTitle)
            || command.Animes is null
            || command.Animes.Count == 0
            || command.Animes.Count > _options.Limits.MaxExportItems
            || command.Animes.Any(anime => anime is null || anime.MalId <= 0))
        {
            throw new FileStorageValidationException();
        }

        var destination = ResolveExportDestination(command.DestinationId);
        _rootCatalog.Get(destination.RootId);
        var distinctAnimes = command.Animes
            .GroupBy(anime => anime.MalId)
            .Select(group => group.First())
            .OrderBy(anime => anime.Year ?? int.MaxValue)
            .ThenBy(anime => anime.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(anime => anime.MalId)
            .ToArray();
        var prefix = destination.PathPrefix.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new FileStorageValidationException();
        }

        var collectionFolder = SanitizeName(command.MyAnimeTitle);
        var usedAnimeFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = distinctAnimes
            .Select(anime => new
            {
                Anime = anime,
                Folder = BuildUniqueAnimeFolder(anime, usedAnimeFolders)
            })
            .Select(item => new PreparedStorageObject(
                item.Anime.MalId,
                StorageObjectId.Create(
                    destination.RootId,
                    $"{prefix}/{collectionFolder}/{item.Folder}/{item.Anime.MalId}.jpg")))
            .ToArray();

        return new PrepareStorageExportResult(command.MyAnimeId, items);
    }

    private ConfiguredExportDestination ResolveExportDestination(string? destinationId)
    {
        var destinations = GetConfiguredExportDestinations();
        if (string.IsNullOrWhiteSpace(destinationId))
        {
            if (destinations.Count == 1)
            {
                return destinations[0];
            }

            throw new FileStorageValidationException();
        }

        return destinations.FirstOrDefault(destination =>
                string.Equals(destination.Id, destinationId, StringComparison.Ordinal))
            ?? throw new FileStorageValidationException();
    }

    private IReadOnlyList<ConfiguredExportDestination> GetConfiguredExportDestinations()
    {
        var configuredDestinations = _options.ExportDestinations ?? [];
        if (configuredDestinations.Length == 0)
        {
            return
            [
                new ConfiguredExportDestination(
                    "default",
                    "Pasta padrao de MyAnimes",
                    _options.ExportRootId,
                    _options.ExportPathPrefix)
            ];
        }

        var destinations = new List<ConfiguredExportDestination>(configuredDestinations.Length);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var configuredDestination in configuredDestinations)
        {
            if (configuredDestination is null
                || !IsValidDestinationId(configuredDestination.Id)
                || !ids.Add(configuredDestination.Id)
                || string.IsNullOrWhiteSpace(configuredDestination.DisplayName)
                || string.IsNullOrWhiteSpace(configuredDestination.RootId)
                || string.IsNullOrWhiteSpace(configuredDestination.PathPrefix))
            {
                throw new StorageRootConfigurationException(
                    "FileStorage:ExportDestinations possui uma configuracao invalida.");
            }

            _rootCatalog.Get(configuredDestination.RootId);
            destinations.Add(new ConfiguredExportDestination(
                configuredDestination.Id,
                configuredDestination.DisplayName.Trim(),
                configuredDestination.RootId,
                configuredDestination.PathPrefix));
        }

        return destinations;
    }

    private static bool IsValidDestinationId(string? destinationId) =>
        !string.IsNullOrWhiteSpace(destinationId)
        && destinationId.Length <= 64
        && char.IsAsciiLetterOrDigit(destinationId[0])
        && destinationId.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static string BuildUniqueAnimeFolder(
        PrepareStorageExportAnime anime,
        HashSet<string> usedNames)
    {
        var year = anime.Year?.ToString() ?? "0000";
        var title = string.IsNullOrWhiteSpace(anime.Title)
            ? $"Anime_{anime.MalId}"
            : anime.Title;
        var type = string.IsNullOrWhiteSpace(anime.Type)
            ? "TipoDesconhecido"
            : anime.Type;
        var baseName = SanitizeName($"{year} {title} - {type}");
        var name = baseName;
        var suffix = 2;

        while (!usedNames.Add(name))
        {
            var suffixText = $" ({suffix++})";
            name = SanitizeNameWithLimit(baseName, 255 - suffixText.Length) + suffixText;
        }

        return name;
    }

    private static string SanitizeName(string name)
    {
        var sanitizedName = SanitizeNameWithLimit(name, 255);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedName);
        if (ReservedWindowsNames.Contains(nameWithoutExtension))
        {
            sanitizedName = $"_{sanitizedName}";
        }

        return string.IsNullOrWhiteSpace(sanitizedName) ? "SemNome" : sanitizedName;
    }

    private static string SanitizeNameWithLimit(string name, int limit)
    {
        var sanitizedName = name.Trim();
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            sanitizedName = sanitizedName.Replace(character, ' ');
        }

        while (sanitizedName.Contains("  ", StringComparison.Ordinal))
        {
            sanitizedName = sanitizedName.Replace("  ", " ", StringComparison.Ordinal);
        }

        sanitizedName = sanitizedName.Trim().TrimEnd('.', ' ');
        return sanitizedName.Length <= limit
            ? sanitizedName
            : sanitizedName[..limit].TrimEnd('.', ' ');
    }

    private sealed record ConfiguredExportDestination(
        string Id,
        string DisplayName,
        string RootId,
        string PathPrefix);
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
