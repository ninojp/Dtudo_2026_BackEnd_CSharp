using ApiIdentity.Provisioning;

namespace ApiIdentity.Mfa;

public sealed class IdentitySecurityAuditWriter
{
    private readonly IdentityProvisioningAuditWriter _auditWriter;

    public IdentitySecurityAuditWriter(IdentityProvisioningAuditWriter auditWriter)
    {
        _auditWriter = auditWriter;
    }

    public void Record(
        string actor,
        string action,
        string target,
        string result,
        string deviceId,
        string reason)
    {
        _auditWriter.Record(
            actor,
            action,
            target,
            result,
            string.IsNullOrWhiteSpace(deviceId) ? "unknown-device" : deviceId,
            "identity-security",
            reason);
    }
}
