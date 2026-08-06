namespace ApiIdentity.Configuration;

public sealed class OpenIddictServerConfigurationOptions
{
    public const string SectionName = "OpenIddict";

    public string Issuer { get; set; } = string.Empty;
}
