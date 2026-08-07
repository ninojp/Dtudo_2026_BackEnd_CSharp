namespace ApiIdentity.Models;

public sealed class IdentityRecoveryTicket
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string SecretHash { get; set; } = string.Empty;

    public string IssuedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? UsedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount? Account { get; set; }
}
