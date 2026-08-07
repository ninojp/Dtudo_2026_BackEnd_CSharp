namespace ApiIdentity.Models;

public sealed class PersonalFavorite
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string ResourceKey { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public IdentityAccount? Account { get; set; }
}
