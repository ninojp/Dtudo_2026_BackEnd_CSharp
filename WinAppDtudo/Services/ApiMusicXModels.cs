namespace WinAppDtudo.Services;

public sealed class ApiMusicXCollectionQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }
}

public sealed record ApiMusicXPagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public enum ApiMusicXArtistType
{
    Unknown = 0,
    Solo = 1,
    Band = 2,
    Group = 3
}

public enum ApiMusicXCollectionArtistRole
{
    Unknown = 0,
    Primary = 1,
    Member = 2,
    Associated = 3
}

public enum ApiMusicXReleaseType
{
    Unknown = 0,
    Album = 1,
    Single = 2,
    EP = 3,
    Compilation = 4,
    Video = 5
}

public enum ApiMusicXCreditRole
{
    Unknown = 0,
    Primary = 1,
    Featured = 2,
    Composer = 3
}

public enum ApiMusicXMediaKind
{
    Other = 0,
    Audio = 1,
    Image = 2,
    Document = 3
}

public enum ApiMusicXLocalFileRole
{
    Unknown = 0,
    TrackAudio = 1,
    Cover = 2,
    Booklet = 3,
    Artwork = 4
}

public sealed record ApiMusicXCollectionSummaryDto(
    long MusicCollectionId,
    string DisplayName,
    string? Description,
    IReadOnlyList<ApiMusicXArtistSummaryDto> Artists,
    int ReleaseCount);

public sealed record ApiMusicXCollectionDto(
    long MusicCollectionId,
    string DisplayName,
    string? Description,
    IReadOnlyList<ApiMusicXArtistSummaryDto> Artists,
    IReadOnlyList<ApiMusicXReleaseDto> Releases,
    IReadOnlyList<ApiMusicXExternalIdentifierDto> ExternalIdentifiers);

public sealed record ApiMusicXArtistSummaryDto(
    long MusicArtistId,
    string DisplayName,
    ApiMusicXArtistType ArtistType);

public sealed record ApiMusicXArtistDto(
    long MusicArtistId,
    string DisplayName,
    ApiMusicXArtistType ArtistType,
    string? SortName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<ApiMusicXCollectionSummaryDto> Collections,
    IReadOnlyList<ApiMusicXExternalIdentifierDto> ExternalIdentifiers);

public sealed record ApiMusicXReleaseDto(
    long MusicReleaseId,
    string Title,
    ApiMusicXReleaseType ReleaseType,
    int? ReleaseYear,
    string? Notes,
    IReadOnlyList<ApiMusicXArtistSummaryDto> Artists,
    IReadOnlyList<ApiMusicXTrackDto> Tracks,
    IReadOnlyList<ApiMusicXLocalFileReferenceDto> LocalFileReferences,
    IReadOnlyList<ApiMusicXExternalIdentifierDto> ExternalIdentifiers);

public sealed record ApiMusicXTrackDto(
    long MusicTrackId,
    string? PositionLabel,
    int? Sequence,
    string Title,
    int? DurationSeconds,
    string? DurationText,
    string? Notes,
    IReadOnlyList<ApiMusicXArtistSummaryDto> Artists,
    IReadOnlyList<ApiMusicXLocalFileReferenceDto> LocalFileReferences,
    IReadOnlyList<ApiMusicXExternalIdentifierDto> ExternalIdentifiers);

public sealed record ApiMusicXLocalFileReferenceDto(
    long MusicLocalFileReferenceId,
    string RelativePath,
    ApiMusicXMediaKind MediaKind,
    ApiMusicXLocalFileRole Role,
    long? MusicTrackId);

public sealed record ApiMusicXExternalIdentifierDto(
    string Provider,
    string ResourceType,
    string ExternalId);

public sealed class ApiMusicXCreateCollectionRequest
{
    public string DisplayName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public List<ApiMusicXCollectionArtistRequest> Artists { get; init; } = [];
}

public sealed class ApiMusicXUpdateCollectionRequest
{
    public string DisplayName { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class ApiMusicXCollectionArtistRequest
{
    public long MusicArtistId { get; init; }

    public ApiMusicXCollectionArtistRole Role { get; init; } = ApiMusicXCollectionArtistRole.Primary;
}

public sealed class ApiMusicXImportCollectionRequest
{
    public long? MusicCollectionId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? Description { get; init; }

    public List<ApiMusicXExternalIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    public List<ApiMusicXArtistImportRequest> Artists { get; init; } = [];

    public List<ApiMusicXReleaseImportRequest> Releases { get; init; } = [];
}

public sealed class ApiMusicXArtistImportRequest
{
    public long? MusicArtistId { get; init; }

    public string? DisplayName { get; init; }

    public ApiMusicXArtistType ArtistType { get; init; } = ApiMusicXArtistType.Unknown;

    public string? SortName { get; init; }

    public List<string> Aliases { get; init; } = [];

    public List<ApiMusicXExternalIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    public ApiMusicXCollectionArtistRole CollectionRole { get; init; } = ApiMusicXCollectionArtistRole.Primary;
}

public sealed class ApiMusicXReleaseImportRequest
{
    public long? MusicReleaseId { get; init; }

    public string Title { get; init; } = string.Empty;

    public ApiMusicXReleaseType ReleaseType { get; init; } = ApiMusicXReleaseType.Unknown;

    public int? ReleaseYear { get; init; }

    public string? Notes { get; init; }

    public string? SourceCategory { get; init; }

    public int? DisplayOrder { get; init; }

    public List<ApiMusicXExternalIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    public List<ApiMusicXArtistCreditImportRequest> ArtistCredits { get; init; } = [];

    public List<ApiMusicXTrackImportRequest> Tracks { get; init; } = [];

    public List<ApiMusicXLocalFileReferenceImportRequest> LocalFileReferences { get; init; } = [];
}

public sealed class ApiMusicXArtistCreditImportRequest
{
    public long? MusicArtistId { get; init; }

    public string? DisplayName { get; init; }

    public ApiMusicXArtistType ArtistType { get; init; } = ApiMusicXArtistType.Unknown;

    public List<ApiMusicXExternalIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    public ApiMusicXCreditRole Role { get; init; } = ApiMusicXCreditRole.Unknown;
}

public sealed class ApiMusicXTrackImportRequest
{
    public string Title { get; init; } = string.Empty;

    public string? PositionLabel { get; init; }

    public int? Sequence { get; init; }

    public int? DurationSeconds { get; init; }

    public string? DurationText { get; init; }

    public string? Notes { get; init; }

    public List<ApiMusicXExternalIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    public List<ApiMusicXArtistCreditImportRequest> ArtistCredits { get; init; } = [];

    public List<ApiMusicXLocalFileReferenceImportRequest> LocalFileReferences { get; init; } = [];
}

public sealed class ApiMusicXLocalFileReferenceImportRequest
{
    public string RelativePath { get; init; } = string.Empty;

    public ApiMusicXMediaKind MediaKind { get; init; } = ApiMusicXMediaKind.Other;

    public ApiMusicXLocalFileRole Role { get; init; } = ApiMusicXLocalFileRole.Unknown;

    public long? MusicTrackId { get; init; }
}

public sealed class ApiMusicXExternalIdentifierRequest
{
    public string Provider { get; init; } = string.Empty;

    public string ResourceType { get; init; } = string.Empty;

    public string ExternalId { get; init; } = string.Empty;
}

public sealed record ApiMusicXImportCollectionResponse(
    ApiMusicXCollectionDto Collection,
    bool Created,
    bool Changed,
    int ArtistsAdded,
    int ReleasesAdded,
    int TracksAdded,
    int LocalFileReferencesAdded);
