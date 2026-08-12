namespace ApiDiscogs.Configuration;

public sealed class ApiCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];

    public static bool IsValidOrigin(string origin, bool requireHttps)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        return !requireHttps || uri.Scheme == Uri.UriSchemeHttps;
    }
}
