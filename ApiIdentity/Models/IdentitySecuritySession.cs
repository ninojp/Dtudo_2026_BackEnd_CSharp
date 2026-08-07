namespace ApiIdentity.Models;

public sealed class IdentitySecuritySession
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public Guid DeviceId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset LastSeenAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount? Account { get; set; }

    public IdentitySecurityDevice? Device { get; set; }
}
