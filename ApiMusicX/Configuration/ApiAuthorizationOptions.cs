namespace ApiMusicX.Configuration;

internal sealed class ApiAuthorizationOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}
