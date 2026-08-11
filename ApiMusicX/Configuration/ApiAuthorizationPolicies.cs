using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ApiMusicX.Configuration;

internal static class ApiAuthorizationPolicies
{
    public const string CatalogReadPolicy = "permission:catalog.read";
    public const string CatalogWritePolicy = "permission:catalog.write";
    public const string CatalogDeletePolicy = "permission:catalog.delete";
    public const string HealthReadPolicy = "permission:health.read";

    public static void AddPermissionPolicy(
        AuthorizationOptions options,
        string policyName,
        string permission,
        string scope)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => HasPermissionAndScope(context.User, permission, scope));
        });
    }

    public static bool HasPermissionAndScope(ClaimsPrincipal principal, string permission, string scope)
        => HasPermission(principal, permission) && HasScope(principal, scope);

    private static bool HasPermission(ClaimsPrincipal principal, string permission)
        => principal.Claims.Any(claim =>
            claim.Type == "permission"
            && string.Equals(claim.Value, permission, StringComparison.Ordinal));

    private static bool HasScope(ClaimsPrincipal principal, string scope)
        => principal.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(value => string.Equals(value, scope, StringComparison.Ordinal));
}
