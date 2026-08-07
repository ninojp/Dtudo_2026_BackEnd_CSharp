namespace ApiIdentity.Models;

public sealed class PersonalList
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public IdentityAccount? Account { get; set; }

    public ICollection<PersonalListItem> Items { get; } = new List<PersonalListItem>();
}
