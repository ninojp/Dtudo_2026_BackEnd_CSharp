namespace ApiMyAnimes.Services;

public interface ISecurityAuditWriter
{
    Task<long> RecordAsync(
        SecurityAuditEntry entry,
        CancellationToken cancellationToken = default);
}
