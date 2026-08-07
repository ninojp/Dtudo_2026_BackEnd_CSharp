using System.Net;

namespace ApiIdentity.Configuration;

public sealed class OpenIddictServerConfigurationOptions
{
    public const string SectionName = "OpenIddict";

    public string Issuer { get; set; } = string.Empty;

    public WinAppOpenIddictOptions WinApp { get; set; } = new();
}

public sealed class WinAppOpenIddictOptions
{
    public string ClientId { get; set; } = "dtudo-winapp";

    public string RedirectUri { get; set; } = "http://127.0.0.1:49173/callback/";

    public string[] Scopes { get; set; } = [];

    public static bool IsValidLoopbackRedirectUri(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttp
            && uri.Host == IPAddress.Loopback.ToString()
            && uri.Port is >= 1024 and <= 65535
            && uri.AbsolutePath == "/callback/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);
    }
}
