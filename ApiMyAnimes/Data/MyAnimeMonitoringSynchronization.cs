namespace ApiMyAnimes.Data;

public sealed class MyAnimeMonitoringSyncRun
{
    public Guid Id { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AuthorizedRoots { get; set; }
    public int AvailableRoots { get; set; }
    public int DiscoveredFolders { get; set; }
    public int AddedMappings { get; set; }
    public int UpdatedMappings { get; set; }
    public int UnchangedMappings { get; set; }
    public int MissingCatalog { get; set; }
    public int AmbiguousFolders { get; set; }
    public int Conflicts { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
    public string? Error { get; set; }
}

public sealed class MyAnimeMonitoringSyncEvent
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string RootKey { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public int? MyAnimeId { get; set; }
    public string? PreviousRootKey { get; set; }
    public string? NewRootKey { get; set; }
    public string Detail { get; set; } = string.Empty;
}
