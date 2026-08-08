namespace ApiMyAnimeList.Configuration;

public sealed class MyAnimeListOptions
{
    public const string SectionName = "MyAnimeList";

    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string[] AllowedHosts { get; set; } = ["api.myanimelist.net"];
    public string AllowedPathPrefix { get; set; } = "/v2/";
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 250;
    public int CacheMinutes { get; set; } = 15;
    public int TotalTimeoutSeconds { get; set; } = 90;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; set; } = 5;
    public int CircuitBreakerSamplingSeconds { get; set; } = 30;
    public int CircuitBreakerBreakSeconds { get; set; } = 30;
}
