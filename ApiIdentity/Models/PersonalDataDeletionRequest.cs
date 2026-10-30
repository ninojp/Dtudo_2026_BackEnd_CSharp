namespace ApiIdentity.Models;

public static class PersonalDataDeletionStatuses
{
    public const string Pending = "Pending";

    public const string Completed = "Completed";
}

public sealed class PersonalDataDeletionRequest
{
    public const int LastErrorMaxLength = 200;

    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string Status { get; set; } = PersonalDataDeletionStatuses.Pending;

    public DateTimeOffset RequestedAtUtc { get; set; }

    public DateTimeOffset ScheduledForUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public DateTimeOffset? RetentionUntilUtc { get; set; }

    /// <summary>Tentativas de processamento (inclui a que concluiu). O processamento so e tentado apos a carencia.</summary>
    public int AttemptCount { get; set; }

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    /// <summary>Codigo curto do ultimo erro (sem dado pessoal nem mensagem de excecao); <c>null</c> apos sucesso.</summary>
    public string? LastError { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
