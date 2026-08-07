namespace ApiIdentity.Models;

public sealed class IdentityBootstrapState
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public string BootstrappedAccountId { get; set; } = string.Empty;

    public DateTimeOffset CompletedAtUtc { get; set; }

    public IdentityAccount? BootstrappedAccount { get; set; }
}
