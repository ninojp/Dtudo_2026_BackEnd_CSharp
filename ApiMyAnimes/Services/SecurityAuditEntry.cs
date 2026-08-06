namespace ApiMyAnimes.Services;

public sealed record SecurityAuditEntry(
    string Actor,
    string Action,
    string Target,
    string Result,
    string DeviceId,
    string CorrelationId,
    string Reason);
