using Fido2NetLib;

namespace ApiIdentity.Mfa;

public static class IdentitySecurityChallengeKinds
{
    public const string PasskeyRegistration = "passkey-registration";

    public const string PasskeyAuthentication = "passkey-authentication";
}

public static class IdentityMfaMethods
{
    public const string Passkey = "passkey";

    public const string Totp = "totp";

    public const string RecoveryCode = "recovery-code";

    public const string LocalRecovery = "local-recovery";
}

public sealed record SecurityContext(string? SessionId, string? DeviceId);

public sealed record SecurityContextRequest(string? SessionId, string? DeviceId)
{
    public SecurityContext ToContext() => new(SessionId, DeviceId);
}

public static class SecurityContextFactory
{
    public static SecurityContext FromIds(Guid sessionId, Guid deviceId) =>
        new(sessionId.ToString("D"), deviceId.ToString("D"));

    public static SecurityContext Normalize(SecurityContext context) =>
        new(NormalizeValue(context.SessionId), NormalizeValue(context.DeviceId));

    public static bool TryGetIds(
        SecurityContext context,
        out Guid sessionId,
        out Guid deviceId)
    {
        var sessionIsValid = Guid.TryParse(context.SessionId, out sessionId);
        var deviceIsValid = Guid.TryParse(context.DeviceId, out deviceId);
        return sessionIsValid && deviceIsValid;
    }

    private static string? NormalizeValue(string? value) =>
        Guid.TryParse(value, out var identifier)
            ? identifier.ToString("D")
            : string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record PasskeyRegistrationOptions(
    Guid ChallengeId,
    CredentialCreateOptions Options);

public sealed record PasskeyAuthenticationOptions(
    Guid ChallengeId,
    AssertionOptions Options);

public sealed record PasskeyAuthenticationResult(
    StepUpGrantResult Grant,
    uint SignCount);

public sealed record TotpSetupResult(string AuthenticatorKey);

public sealed record RecoveryCodesResult(IReadOnlyList<string> Codes);

public sealed record StepUpGrantResult(Guid GrantId, DateTimeOffset ExpiresAtUtc);

public sealed record LocalRecoveryTicketDelivery(
    Guid TicketId,
    string Secret,
    DateTimeOffset ExpiresAtUtc);

public sealed record SecuritySnapshotResult(Guid SnapshotId);

public sealed record SecurityOperationResult(
    bool Succeeded,
    Guid? SnapshotId = null,
    IReadOnlyList<string>? RecoveryCodes = null);

public sealed record SecurityDeviceSessionResult(
    Guid DeviceId,
    Guid SessionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset SessionExpiresAtUtc,
    DateTimeOffset TrustedUntilUtc);

public sealed record SecurityDeviceView(
    Guid DeviceId,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset TrustedUntilUtc,
    bool IsRevoked);

public sealed record SecuritySessionView(
    Guid SessionId,
    Guid DeviceId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool IsRevoked);

public sealed record SecurityTokenPair(
    Guid DeviceId,
    Guid SessionId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    DateTimeOffset SessionExpiresAtUtc);

public enum SecurityTokenRefreshStatus
{
    Succeeded,
    Invalid,
    Expired,
    Revoked,
    SessionInactive,
    ReuseDetected
}

public sealed record SecurityTokenRefreshResult(
    SecurityTokenRefreshStatus Status,
    SecurityTokenPair? Tokens = null);

public sealed record SecurityAccessTokenInfo(
    string AccountId,
    Guid DeviceId,
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc);

public sealed record PasskeyRegistrationRequest(
    Guid ChallengeId,
    AuthenticatorAttestationRawResponse Response,
    string? Name,
    string? SessionId,
    string? DeviceId);

public sealed record PasskeyAuthenticationRequest(
    Guid ChallengeId,
    AuthenticatorAssertionRawResponse Response,
    string Action,
    string? SessionId,
    string? DeviceId);

public sealed record TotpSetupConfirmationRequest(string Token);

public sealed record StepUpVerificationRequest(
    string Action,
    string Token,
    string? SessionId,
    string? DeviceId);

public sealed record SecuritySessionCreateRequest(string Name);

public sealed record SecuritySessionTokenBindingRequest(Guid DeviceId);

public sealed record SecurityTokenRefreshRequest(string RefreshToken);

public sealed record SecurityTokenIntrospectionRequest(string AccessToken);

public sealed record LocalRecoveryIssueRequest(string AccountId);

public sealed record LocalRecoveryRedeemRequest(
    Guid TicketId,
    string Secret,
    string NewPassword);

public sealed record SecuritySnapshotRestoreRequest(
    Guid SnapshotId,
    string? SessionId,
    string? DeviceId);
