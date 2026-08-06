namespace ApiMyAnimes.Configuration;

public sealed class ApiMyAnimeListOptions
{
    public const string SectionName = "ApiMyAnimeList";

    public string BaseUrl { get; set; } = string.Empty;
}
