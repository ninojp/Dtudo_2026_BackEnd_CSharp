namespace ApiIdentity.Models;

public sealed class PersonalPreference
{
    public string AccountId { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public IdentityAccount? Account { get; set; }
}
