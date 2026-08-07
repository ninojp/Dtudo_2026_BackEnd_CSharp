namespace ApiFileStorage.Configuration;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public AllowedStorageRootOptions[] Roots { get; set; } = [];

    public string ExportRootId { get; set; } = "media";

    public string ExportPathPrefix { get; set; } = "my-animes";

    public FileStorageLimitsOptions Limits { get; set; } = new();

    public FileStorageStepUpOptions StepUp { get; set; } = new();

    public AllowedStorageFileTypeOptions[] AllowedFileTypes { get; set; } = [];

    public FileStorageScannerOptions Scanner { get; set; } = new();
}

public sealed class FileStorageStepUpOptions
{
    public string IdentityBaseUrl { get; set; } = "https://localhost:7243";

    public string Action { get; set; } = "filesystem.command";

    public int TimeoutSeconds { get; set; } = 10;
}

public sealed class AllowedStorageRootOptions
{
    public string Id { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
}

public sealed class FileStorageLimitsOptions
{
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    public int MaxFileNameLength { get; set; } = 255;

    public long MinimumFreeSpaceBytes { get; set; } = 100 * 1024 * 1024;

    public int MaxIdempotencyKeyLength { get; set; } = 128;

    public int ScannerTimeoutSeconds { get; set; } = 60;

    public int MaxBulkDeleteItems { get; set; } = 100;

    public int DeletePreviewLifetimeSeconds { get; set; } = 120;
}

public sealed class AllowedStorageFileTypeOptions
{
    public string Extension { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public string MagicBytesHex { get; set; } = string.Empty;

    public int MagicOffset { get; set; }
}

public sealed class FileStorageScannerOptions
{
    public bool RequireDefender { get; set; } = true;

    public bool RequireAmsi { get; set; } = true;

    public string? DefenderExecutablePath { get; set; }
}
