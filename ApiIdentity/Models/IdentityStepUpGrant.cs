namespace ApiIdentity.Models;

public sealed class IdentityStepUpGrant
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string? SessionId { get; set; }

    public string? DeviceId { get; set; }

    public DateTimeOffset GrantedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount? Account { get; set; }
}
