namespace ApiFileStorage.Monitoring.Data;

public enum InventoryCompletion
{
    Complete,
    Partial,
    Unavailable,
    Cancelled
}

public enum ObservationSource
{
    LiveNotification,
    InventoryComparison,
    Monitor
}

public sealed class MonitoredLocation
{
    public Guid Id { get; set; }
    public string RootKey { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAtUtc { get; set; }
}

public sealed class InventorySnapshot
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset FinishedAtUtc { get; set; }
    public InventoryCompletion Completion { get; set; }
    public long FileCount { get; set; }
    public long DirectoryCount { get; set; }
    public long TotalBytes { get; set; }
    public List<InventoryEntry> Entries { get; set; } = [];
}

public sealed class InventoryEntry
{
    public long Id { get; set; }
    public Guid SnapshotId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long LengthBytes { get; set; }
    public DateTimeOffset LastWriteTimeUtc { get; set; }
    public int Attributes { get; set; }
}

public sealed class MonitoringObservation
{
    public long Id { get; set; }
    public Guid LocationId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public ObservationSource Source { get; set; }
    public string Code { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string? PreviousRelativePath { get; set; }
    public string Detail { get; set; } = string.Empty;
}
