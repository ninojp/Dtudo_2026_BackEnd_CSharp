namespace ApiIdentity.Models;

public sealed class IdentitySecuritySnapshot
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string ProtectedPayload { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RestoredAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public string? RestoredBy { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount? Account { get; set; }
}
