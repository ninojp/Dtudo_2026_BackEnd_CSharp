namespace ApiIdentity.Models;

public sealed class TermsAcceptance
{
    public Guid Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public Guid TermsDocumentId { get; set; }

    public DateTimeOffset AcceptedAtUtc { get; set; }

    public IdentityAccount? Account { get; set; }

    public TermsDocument? TermsDocument { get; set; }
}
