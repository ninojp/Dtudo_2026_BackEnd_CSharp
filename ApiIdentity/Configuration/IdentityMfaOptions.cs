namespace ApiIdentity.Configuration;

public sealed class IdentityMfaOptions
{
    public const string SectionName = "IdentityMfa";

    public string RelyingPartyDomain { get; set; } = "localhost";

    public string RelyingPartyName { get; set; } = "Dtudo2026 Identity";

    public string[] Origins { get; set; } = ["https://localhost:7243"];

    public int Fido2TimeoutMilliseconds { get; set; } = 60_000;

    public int Fido2TimestampDriftMilliseconds { get; set; } = 30_000;

    public int ChallengeLifetimeSeconds { get; set; } = 120;

    public int StepUpLifetimeSeconds { get; set; } = 300;

    public int LocalRecoveryLifetimeMinutes { get; set; } = 15;

    public int SnapshotLifetimeHours { get; set; } = 24;

    public int RecoveryCodeCount { get; set; } = 10;

    public int ClockSkewSeconds { get; set; } = 30;
}
