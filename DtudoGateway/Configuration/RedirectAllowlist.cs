namespace DtudoGateway.Configuration;

public static class RedirectAllowlist
{
    public static bool TryGetAllowedRedirect(
        string? candidate,
        GatewayOptions options,
        out string redirect)
    {
        redirect = "/";

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return true;
        }

        if (candidate.Any(char.IsControl) || candidate.Contains('\\'))
        {
            return false;
        }

        if (candidate.StartsWith("/", StringComparison.Ordinal)
            && !candidate.StartsWith("//", StringComparison.Ordinal))
        {
            redirect = candidate;
            return true;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var absoluteUri)
            || !string.IsNullOrEmpty(absoluteUri.UserInfo)
            || !string.IsNullOrEmpty(absoluteUri.Fragment)
            || !absoluteUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !options.AllowedRedirectOrigins.Any(origin => GatewayOptionsValidator.SameOrigin(origin, absoluteUri.ToString())))
        {
            return false;
        }

        redirect = candidate;
        return true;
    }
}
