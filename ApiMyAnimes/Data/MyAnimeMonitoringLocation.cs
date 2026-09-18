namespace ApiMyAnimes.Data;

public sealed class MyAnimeMonitoringLocation
{
    public int MyAnimeId { get; set; }
    public string RootKey { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
}
