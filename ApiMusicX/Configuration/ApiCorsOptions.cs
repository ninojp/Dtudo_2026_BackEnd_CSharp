namespace ApiMusicX.Configuration;

internal sealed class ApiCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];

    public static bool IsValidOrigin(string? value, bool requireHttps)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Contains('*')
            || value.EndsWith("/", StringComparison.Ordinal)
            || !Uri.TryCreate(value, UriKind.Absolute, out var origin)
            || origin.Scheme is not ("http" or "https")
            || string.IsNullOrWhiteSpace(origin.Host)
            || !string.IsNullOrEmpty(origin.UserInfo)
            || origin.AbsolutePath != "/"
            || !string.IsNullOrEmpty(origin.Query)
            || !string.IsNullOrEmpty(origin.Fragment))
        {
            return false;
        }

        return !requireHttps || origin.Scheme == Uri.UriSchemeHttps;
    }
}
