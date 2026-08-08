using System.Net;

namespace DtudoGateway.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public string PublicOrigin { get; set; } = string.Empty;

    public string[] AllowedRedirectOrigins { get; set; } = [];

    public string[] AllowedCorsOrigins { get; set; } = [];

    public string[] TrustedProxyAddresses { get; set; } = [];

    public string ApiMyAnimesBaseUrl { get; set; } = string.Empty;

    public string ApiIdentityBaseUrl { get; set; } = string.Empty;

    public bool PublicCatalogOnly { get; set; }

    public long MaxRequestBodyBytes { get; set; } = 1_048_576;

    public int RateLimitPermitLimit { get; set; } = 60;

    public int RateLimitWindowSeconds { get; set; } = 60;
}

public sealed class GatewayOpenIdConnectOptions
{
    public const string SectionName = "OpenIdConnect";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string[] Scopes { get; set; } = ["openid", "profile"];
}

public static class GatewayOptionsValidator
{
    public static bool IsValid(GatewayOptions options)
    {
        if (!IsHttpsOrigin(options.PublicOrigin)
            || options.AllowedRedirectOrigins.Length == 0
            || !options.AllowedRedirectOrigins.All(IsHttpsOrigin)
            || !options.AllowedRedirectOrigins.Any(origin => SameOrigin(origin, options.PublicOrigin))
            || options.AllowedCorsOrigins.Length == 0
            || !options.AllowedCorsOrigins.All(IsHttpsOrigin)
            || options.PublicCatalogOnly && options.AllowedCorsOrigins.Any(origin => !SameOrigin(origin, options.PublicOrigin))
            || options.TrustedProxyAddresses.Length == 0
            || options.TrustedProxyAddresses.Any(address => !IPAddress.TryParse(address, out _)))
        {
            return false;
        }

        return options.MaxRequestBodyBytes is >= 1_024 and <= 10_485_760
            && options.RateLimitPermitLimit is >= 1 and <= 10_000
            && options.RateLimitWindowSeconds is >= 1 and <= 3_600
            && IsHttpsBaseUrl(options.ApiMyAnimesBaseUrl)
            && IsHttpsBaseUrl(options.ApiIdentityBaseUrl);
    }

    public static bool IsValid(GatewayOpenIdConnectOptions options) =>
        IsHttpsBaseUrl(options.Authority)
        && !string.IsNullOrWhiteSpace(options.ClientId)
        && !string.IsNullOrWhiteSpace(options.ClientSecret)
        && options.Scopes.Length > 0
        && options.Scopes.Contains("openid", StringComparer.Ordinal)
        && options.Scopes.Distinct(StringComparer.Ordinal).Count() == options.Scopes.Length;

    public static bool IsHttpsOrigin(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && uri.AbsolutePath == "/";
    }

    public static bool IsHttpsBaseUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);
    }

    public static bool SameOrigin(string left, string right)
    {
        if (!Uri.TryCreate(left, UriKind.Absolute, out var leftUri)
            || !Uri.TryCreate(right, UriKind.Absolute, out var rightUri))
        {
            return false;
        }

        return Uri.Compare(
            leftUri,
            rightUri,
            UriComponents.SchemeAndServer,
            UriFormat.Unescaped,
            StringComparison.OrdinalIgnoreCase) == 0;
    }
}
