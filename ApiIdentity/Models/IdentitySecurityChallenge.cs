namespace ApiIdentity.Models;

public sealed class IdentitySecurityChallenge
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string ProtectedPayload { get; set; } = string.Empty;

    public string? SessionId { get; set; }

    public string? DeviceId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? ConsumedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount? Account { get; set; }
}
