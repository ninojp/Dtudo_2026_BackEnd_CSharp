namespace ApiIdentity.Models;

public static class PersonalDataDeletionStatuses
{
    public const string Pending = "Pending";

    public const string Completed = "Completed";
}

public sealed class PersonalDataDeletionRequest
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string Status { get; set; } = PersonalDataDeletionStatuses.Pending;

    public DateTimeOffset RequestedAtUtc { get; set; }

    public DateTimeOffset ScheduledForUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public DateTimeOffset? RetentionUntilUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
