using ApiIdentity.Data;
using ApiIdentity.Models;

namespace ApiIdentity.Provisioning;

public sealed class IdentityProvisioningAuditWriter
{
    private readonly IdentityDbContext _context;
    private readonly TimeProvider _timeProvider;

    public IdentityProvisioningAuditWriter(IdentityDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public void Record(
        string actor,
        string action,
        string target,
        string result,
        string deviceId,
        string correlationId,
        string reason)
    {
        var occurredAtUtc = _timeProvider.GetUtcNow();
        _context.ProvisioningAuditEvents.Add(new IdentityProvisioningAuditEvent(
            Require(actor, nameof(actor), 256),
            Require(action, nameof(action), 128),
            Require(target, nameof(target), 512),
            Require(result, nameof(result), 64),
            occurredAtUtc,
            Require(deviceId, nameof(deviceId), 256),
            Require(correlationId, nameof(correlationId), 128),
            Require(reason, nameof(reason), 1000),
            occurredAtUtc.AddMonths(IdentityProvisioningAuditEvent.RetentionMonths)));
    }

    public void Record(
        string actor,
        string action,
        string target,
        string result,
        string deviceId,
        string reason) =>
        Record(actor, action, target, result, deviceId, "identity-privacy", reason);

    private static string Require(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Valor obrigatorio.", parameterName);
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException(
                $"O valor deve ter no maximo {maxLength} caracteres.",
                parameterName);
        }

        return normalizedValue;
    }
}
