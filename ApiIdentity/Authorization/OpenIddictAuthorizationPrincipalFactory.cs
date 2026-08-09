using System.Security.Claims;
using ApiIdentity.Configuration;
using ApiIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace ApiIdentity.Authorization;

public sealed class OpenIddictAuthorizationPrincipalFactory
{
    private readonly UserManager<IdentityAccount> _userManager;
    private readonly OpenIddictServerConfigurationOptions _options;

    public OpenIddictAuthorizationPrincipalFactory(
        UserManager<IdentityAccount> userManager,
        IOptions<OpenIddictServerConfigurationOptions> options)
    {
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task<ClaimsPrincipal?> CreateAsync(
        ClaimsPrincipal browserPrincipal,
        OpenIddictRequest request,
        CancellationToken cancellationToken = default)
    {
        var accountId = browserPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? browserPrincipal.FindFirstValue(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return null;
        }

        var account = await _userManager.FindByIdAsync(accountId);
        if (account is null
            || !account.IsActivationCompleted
            || await _userManager.IsLockedOutAsync(account))
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(account);
        if (string.Equals(request.ClientId, _options.WinApp.ClientId, StringComparison.Ordinal)
            && !roles.Contains(
                AuthorizationCatalog.Roles.SuperAdministrator,
                StringComparer.Ordinal))
        {
            return null;
        }

        var permissions = AuthorizationCatalog.AllRoles
            .Where(role => roles.Contains(role.Name, StringComparer.Ordinal))
            .SelectMany(role => role.PermissionKeys)
            .Distinct(StringComparer.Ordinal);

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, account.Id));
        if (!string.IsNullOrWhiteSpace(account.UserName))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, account.UserName));
        }

        if (!string.IsNullOrWhiteSpace(account.Email))
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Email, account.Email));
        }

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
        }

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(AuthorizationCatalog.PermissionClaimType, permission));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(request.GetResources());
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken);
        }

        return principal;
    }
}
