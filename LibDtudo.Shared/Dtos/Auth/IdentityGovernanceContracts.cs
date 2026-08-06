namespace LibDtudo.Shared.Dtos.Auth;

public sealed record AdultAgeConfirmationContract(
    bool HasConfirmedAdultAge,
    DateTimeOffset? ConfirmedAtUtc);

public sealed record TermsDocumentContract(
    Guid Id,
    string DocumentType,
    string Version,
    string ContentHashSha256,
    DateTimeOffset PublishedAtUtc,
    bool IsActive);

public sealed record TermsAcceptanceContract(
    Guid TermsDocumentId,
    string DocumentType,
    string Version,
    DateTimeOffset AcceptedAtUtc);

public sealed record PermissionDefinitionContract(string Key, string Description);

public sealed record RolePermissionContract(string RoleName, IReadOnlyCollection<string> PermissionKeys);
