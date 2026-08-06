namespace ApiIdentity.Configuration;

public sealed class IdentityDatabaseOptions
{
    public const string SectionName = "IdentityDatabase";

    public string DatabaseName { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;
}
