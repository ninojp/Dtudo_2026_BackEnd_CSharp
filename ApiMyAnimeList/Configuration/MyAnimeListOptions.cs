namespace ApiMyAnimeList.Configuration;

public sealed class MyAnimeListOptions
{
    public const string SectionName = "MyAnimeList";

    public string BaseUrl { get; set; } = "https://api.myanimelist.net/v2/";
    public string ClientId { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxRetries { get; set; } = 3;
    public int CacheMinutes { get; set; } = 15;
}
