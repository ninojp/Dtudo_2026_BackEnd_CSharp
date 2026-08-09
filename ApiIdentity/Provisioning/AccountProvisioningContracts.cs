namespace ApiIdentity.Provisioning;

public sealed record ProvisionAccountRequest(string UserName, string Email, string RoleName, string Password);

public sealed record BootstrapAccountRequest(string UserName, string Email);

public sealed record InitialAccountActivationRequest(Guid ActivationId, string InitialSecret, string NewPassword);

public sealed record InitialSecretDelivery(
    Guid ActivationId,
    string InitialSecret,
    DateTimeOffset ExpiresAtUtc);

public sealed record BootstrapAccountResult(bool Succeeded, bool IsAlreadyCompleted, InitialSecretDelivery? Delivery);

public sealed record ProvisionAccountResult(
    bool Succeeded,
    InitialSecretDelivery? Delivery,
    IReadOnlyList<string>? Errors = null);

public sealed record AccountActivationResult(bool Activated);
