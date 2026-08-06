namespace ApiIdentity.Models;

public sealed class TermsDocument
{
    public Guid Id { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string ContentHashSha256 { get; set; } = string.Empty;

    public DateTimeOffset PublishedAtUtc { get; set; }

    public bool IsActive { get; set; }

    public ICollection<TermsAcceptance> Acceptances { get; } = new List<TermsAcceptance>();
}
