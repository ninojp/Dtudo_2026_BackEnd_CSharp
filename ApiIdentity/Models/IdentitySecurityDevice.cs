namespace ApiIdentity.Models;

public sealed class IdentitySecurityDevice
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset LastSeenAtUtc { get; set; }

    public DateTimeOffset TrustedAtUtc { get; set; }

    public DateTimeOffset TrustedUntilUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount? Account { get; set; }
}
