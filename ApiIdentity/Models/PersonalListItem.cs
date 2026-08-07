namespace ApiIdentity.Models;

public sealed class PersonalListItem
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public Guid ListId { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public string ResourceKey { get; set; } = string.Empty;

    public int Position { get; set; }

    public DateTimeOffset AddedAtUtc { get; set; }

    public IdentityAccount? Account { get; set; }

    public PersonalList? List { get; set; }
}
