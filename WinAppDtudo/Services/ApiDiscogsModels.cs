using System.Net;

namespace WinAppDtudo.Services;

public interface IApiDiscogsClient
{
    Task<ApiDiscogsPagedResponse<ApiDiscogsArtistSearchItem>> BuscarArtistasAsync(
        string query,
        int page = 1,
        int perPage = 10,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ApiDiscogsArtistDetails> ObterArtistaAsync(
        string artistId,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ApiDiscogsArtistReleasesResponse> ObterDiscografiaAsync(
        string artistId,
        int page = 1,
        int perPage = 50,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ApiDiscogsReleaseDetails> ObterReleaseAsync(
        string releaseId,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ApiDiscogsMasterDetails> ObterMasterAsync(
        string masterId,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record ApiDiscogsPagedResponse<T>(
    string Source,
    IReadOnlyList<T> Items,
    ApiDiscogsPagination Pagination,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

public sealed record ApiDiscogsPagination(
    int Page,
    int PerPage,
    int? TotalItems,
    int? TotalPages,
    bool HasNextPage,
    int? UniqueItemsInPage);

public sealed record ApiDiscogsSourceReference(
    string Provider,
    string ResourceType,
    string Id,
    string? ResourceUrl);

public sealed record ApiDiscogsArtistSearchItem(
    ApiDiscogsSourceReference Source,
    string Name,
    string Type,
    string? ThumbnailUrl,
    string? ImageUrl);

public sealed record ApiDiscogsNameReference(string? Id, string Name);

public sealed record ApiDiscogsArtistDetails(
    ApiDiscogsSourceReference Source,
    string Name,
    string? RealName,
    string? Profile,
    IReadOnlyList<ApiDiscogsNameReference> Aliases,
    IReadOnlyList<ApiDiscogsNameReference> Members,
    IReadOnlyList<string> Urls,
    IReadOnlyList<ApiDiscogsImage> Images,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

public sealed record ApiDiscogsReleaseSummary(
    ApiDiscogsSourceReference Source,
    string CanonicalId,
    string ResourceType,
    string Title,
    string? ArtistName,
    string? ArtistId,
    int? Year,
    string? MasterId,
    string? MainReleaseId,
    string? Role,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Formats,
    string Category,
    string? ThumbnailUrl,
    string? ImageUrl,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

public sealed record ApiDiscogsArtistReleasesResponse(
    string Source,
    ApiDiscogsNameReference Artist,
    IReadOnlyList<ApiDiscogsReleaseSummary> Items,
    ApiDiscogsPagination Pagination,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

public sealed record ApiDiscogsImage(
    string? Type,
    string? Uri,
    int? Width,
    int? Height);

public sealed record ApiDiscogsCredit(string? Id, string Name, string? Role);

public sealed record ApiDiscogsLabel(string Name, string? CatalogNumber, string? Id);

public sealed record ApiDiscogsTrack(
    string? Position,
    string Title,
    int? DurationSeconds,
    string? DurationText,
    IReadOnlyList<ApiDiscogsCredit> Artists,
    IReadOnlyList<ApiDiscogsCredit> ExtraArtists);

public sealed record ApiDiscogsReleaseDetails(
    ApiDiscogsSourceReference Source,
    string Title,
    int? Year,
    string? Released,
    string? Country,
    string? Status,
    string? MasterId,
    IReadOnlyList<ApiDiscogsCredit> Artists,
    IReadOnlyList<ApiDiscogsLabel> Labels,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Styles,
    IReadOnlyList<string> Formats,
    IReadOnlyList<ApiDiscogsTrack> Tracklist,
    IReadOnlyList<ApiDiscogsImage> Images,
    string? Notes,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

public sealed record ApiDiscogsMasterDetails(
    ApiDiscogsSourceReference Source,
    string Title,
    string? MainReleaseId,
    int? Year,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Styles,
    IReadOnlyList<ApiDiscogsCredit> Artists,
    IReadOnlyList<ApiDiscogsReleaseSummary> Versions,
    IReadOnlyList<ApiDiscogsImage> Images,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

public sealed class ApiDiscogsHttpException : HttpRequestException
{
    public ApiDiscogsHttpException(
        HttpStatusCode statusCode,
        string message,
        int? retryAfterSeconds = null,
        string? errorCode = null)
        : base(message, inner: null, statusCode)
    {
        ResponseStatusCode = statusCode;
        RetryAfterSeconds = retryAfterSeconds;
        ErrorCode = errorCode;
    }

    public HttpStatusCode ResponseStatusCode { get; }

    public int? RetryAfterSeconds { get; }

    public string? ErrorCode { get; }
}

public sealed class ApiDiscogsImportConflictException(string message)
    : InvalidOperationException(message);
