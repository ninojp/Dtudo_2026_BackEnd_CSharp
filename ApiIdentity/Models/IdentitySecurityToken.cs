namespace ApiIdentity.Models;

public static class IdentitySecurityTokenTypes
{
    public const string Access = "access";

    public const string Refresh = "refresh";
}

public sealed class IdentitySecurityToken
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public Guid SessionId { get; set; }

    public Guid DeviceId { get; set; }

    public Guid FamilyId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public string TokenType { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? UsedAtUtc { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount? Account { get; set; }
}
