namespace ApiIdentity.Configuration;

public sealed class IdentitySessionOptions
{
    public const string SectionName = "IdentitySessions";

    public int LifetimeDays { get; set; } = 30;

    public int AccessTokenLifetimeSeconds { get; set; } = 300;

    public int RefreshTokenLifetimeDays { get; set; } = 30;

    public int TokenEntropyBytes { get; set; } = 32;
}
