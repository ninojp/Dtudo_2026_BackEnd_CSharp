namespace ApiFileStorage.Contracts;

public sealed record ResolveStorageObjectRequest(
    string ObjectId);

public sealed record ResolveStorageObjectResponse(
    string ObjectId,
    string Kind,
    long Length,
    DateTimeOffset LastWriteTimeUtc);

public sealed record PrepareStorageExportAnimeRequest(
    int MalId,
    int? Year,
    string? Title,
    string? Type);

public sealed record PrepareStorageExportRequest(
    int MyAnimeId,
    string? MyAnimeTitle,
    IReadOnlyCollection<PrepareStorageExportAnimeRequest>? Animes,
    string? DestinationId);

public sealed record StorageExportDestinationResponse(
    string Id,
    string DisplayName);

public sealed record PreparedStorageObjectResponse(
    int MalId,
    string ObjectId);

public sealed record PrepareStorageExportResponse(
    int MyAnimeId,
    IReadOnlyList<PreparedStorageObjectResponse> Items);

public sealed record BulkDeletePreviewRequest(
    IReadOnlyCollection<string>? ObjectIds);

public sealed record BulkDeletePreviewItem(
    string ObjectId,
    string Kind,
    long Length,
    DateTimeOffset LastWriteTimeUtc);

public sealed record BulkDeletePreviewResponse(
    Guid PreviewId,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<BulkDeletePreviewItem> Items);

public sealed record BulkDeleteRequest(Guid PreviewId);

public sealed record BulkDeleteItemResponse(
    string ObjectId,
    string Status,
    string? Sha256,
    DateTimeOffset? PurgeAtUtc);

public sealed record BulkDeleteResponse(
    Guid PreviewId,
    IReadOnlyList<BulkDeleteItemResponse> Items);
