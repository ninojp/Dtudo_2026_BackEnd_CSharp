using ApiMyAnimes.Data;

namespace ApiMyAnimes.Services;

public sealed class SecurityAuditWriter : ISecurityAuditWriter
{
    private readonly MyAnimesContext _context;

    public SecurityAuditWriter(MyAnimesContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<long> RecordAsync(
        SecurityAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var occurredAtUtc = DateTimeOffset.UtcNow;
        var auditEvent = new SecurityAuditEvent(
            Require(entry.Actor, nameof(entry.Actor), 256),
            Require(entry.Action, nameof(entry.Action), 128),
            Require(entry.Target, nameof(entry.Target), 512),
            Require(entry.Result, nameof(entry.Result), 64),
            occurredAtUtc,
            Require(entry.DeviceId, nameof(entry.DeviceId), 256),
            Require(entry.CorrelationId, nameof(entry.CorrelationId), 128),
            Require(entry.Reason, nameof(entry.Reason), 1000),
            occurredAtUtc.AddMonths(SecurityAuditEvent.RetentionMonths));

        _context.SecurityAuditEvents.Add(auditEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return auditEvent.Id;
    }

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
