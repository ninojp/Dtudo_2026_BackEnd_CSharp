namespace ApiIdentity.Models;

public sealed class IdentityProvisioningAuditEvent
{
    internal const int RetentionMonths = 12;

    private IdentityProvisioningAuditEvent()
    {
    }

    internal IdentityProvisioningAuditEvent(
        string actor,
        string action,
        string target,
        string result,
        DateTimeOffset occurredAtUtc,
        string deviceId,
        string correlationId,
        string reason,
        DateTimeOffset retentionUntilUtc)
    {
        Actor = actor;
        Action = action;
        Target = target;
        Result = result;
        OccurredAtUtc = occurredAtUtc;
        DeviceId = deviceId;
        CorrelationId = correlationId;
        Reason = reason;
        RetentionUntilUtc = retentionUntilUtc;
    }

    public long Id { get; private set; }

    public string Actor { get; private set; } = null!;

    public string Action { get; private set; } = null!;

    public string Target { get; private set; } = null!;

    public string Result { get; private set; } = null!;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string DeviceId { get; private set; } = null!;

    public string CorrelationId { get; private set; } = null!;

    public string Reason { get; private set; } = null!;

    public DateTimeOffset RetentionUntilUtc { get; private set; }
}
