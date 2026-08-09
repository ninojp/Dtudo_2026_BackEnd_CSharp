using ApiIdentity.Provisioning;

namespace ApiIdentity.Authorization;

public sealed record IdentityAdminProvisionRequest(
    string UserName,
    string Email,
    string RoleName,
    string Password,
    string? SessionId,
    string? DeviceId);

public sealed record IdentityAdminRoleAssignmentRequest(
    string RoleName,
    bool Assign,
    string? SessionId,
    string? DeviceId);

public sealed record IdentityAdminLockRequest(
    bool Lock,
    string? SessionId,
    string? DeviceId);

public sealed record IdentityAdminAccountView(
    string Id,
    string? UserName,
    string? Email,
    bool IsActivationCompleted,
    bool IsLocked,
    DateTimeOffset? LockoutEndUtc,
    IReadOnlyList<string> Roles);

public sealed record IdentityAdminRoleView(
    string Id,
    string Name,
    IReadOnlyList<string> PermissionKeys);

public sealed record IdentityAdminPermissionView(
    string Key,
    string Description);

public sealed record IdentityAdminDeviceView(
    string AccountId,
    Guid DeviceId,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset TrustedUntilUtc,
    bool IsRevoked);

public sealed record IdentityAdminSessionView(
    string AccountId,
    Guid SessionId,
    Guid DeviceId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool IsRevoked);

public sealed record IdentityAdminProvisionResult(
    bool Succeeded,
    InitialSecretDelivery? Delivery = null,
    IReadOnlyList<string>? Errors = null);
