namespace ApiFileStorage.Contracts;

public sealed record ResolveStorageObjectRequest(
    string ObjectId);

public sealed record ResolveStorageObjectResponse(
    string ObjectId,
    string Kind,
    long Length,
    DateTimeOffset LastWriteTimeUtc);
