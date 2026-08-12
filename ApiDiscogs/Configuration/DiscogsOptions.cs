namespace ApiDiscogs.Configuration;

public sealed class DiscogsOptions
{
    public const string SectionName = "ApiDiscogs";

    public string BaseUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string[] AllowedHosts { get; set; } = ["api.discogs.com"];
    public string AllowedPathPrefix { get; set; } = "/";
    public string UserAgent { get; set; } = "Dtudo-ApiDiscogs/1.0";
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 250;
    public int CacheMinutes { get; set; } = 15;
    public int MaxResponseBytes { get; set; } = 2_000_000;
    public int TotalTimeoutSeconds { get; set; } = 90;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; set; } = 5;
    public int CircuitBreakerSamplingSeconds { get; set; } = 30;
    public int CircuitBreakerBreakSeconds { get; set; } = 30;
}
