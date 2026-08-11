using ApiIdentity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace ApiIdentity.Authorization;

public sealed class OpenIddictConfigurationSeeder
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly OpenIddictServerConfigurationOptions _options;

    public OpenIddictConfigurationSeeder(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IOptions<OpenIddictServerConfigurationOptions> options)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var scope in _options.WinApp.Scopes
            .Concat(_options.Gateway.Scopes)
            .Where(scope => !string.Equals(scope, OpenIddictConstants.Scopes.OpenId, StringComparison.Ordinal)
                && !string.Equals(scope, OpenIddictConstants.Scopes.OfflineAccess, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal))
        {
            var existingScope = await _scopeManager.FindByNameAsync(scope, cancellationToken);
            if (existingScope is null)
            {
                await _scopeManager.CreateAsync(
                    new OpenIddictScopeDescriptor
                    {
                        Name = scope,
                        DisplayName = scope switch
                        {
                            "profile" => "Perfil do usuario",
                            "identity.login" => "Autenticacao do WinApp",
                            "identity.provision" => "Administracao de identidade",
                            _ => scope
                        }
                    },
                    cancellationToken);
            }
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = _options.WinApp.ClientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "Dtudo WinApp"
        };
        descriptor.RedirectUris.Add(new Uri(_options.WinApp.RedirectUri, UriKind.Absolute));
        descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        descriptor.AddResourcePermissions(
            "urn:dtudo:api-my-animes",
            "urn:dtudo:api-my-animelist",
            "urn:dtudo:api-file-storage",
            "urn:dtudo:api-musicx");
        descriptor.Permissions.UnionWith(
            new[]
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
            }.Concat(
                _options.WinApp.Scopes
                    .Where(scope => !string.Equals(scope, OpenIddictConstants.Scopes.OpenId, StringComparison.Ordinal)
                        && !string.Equals(scope, OpenIddictConstants.Scopes.OfflineAccess, StringComparison.Ordinal))
                    .Distinct(StringComparer.Ordinal)
                    .Select(scope => OpenIddictConstants.Permissions.Prefixes.Scope + scope)));

        var application = await _applicationManager.FindByClientIdAsync(_options.WinApp.ClientId, cancellationToken);
        if (application is null)
        {
            await _applicationManager.CreateAsync(descriptor, cancellationToken);
        }
        else
        {
            await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_options.Gateway.ClientSecret))
        {
            return;
        }

        var gatewayDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = _options.Gateway.ClientId,
            ClientSecret = _options.Gateway.ClientSecret,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "Dtudo Gateway"
        };
        gatewayDescriptor.RedirectUris.Add(new Uri(_options.Gateway.RedirectUri, UriKind.Absolute));
        gatewayDescriptor.PostLogoutRedirectUris.Add(new Uri(_options.Gateway.PostLogoutRedirectUri, UriKind.Absolute));
        gatewayDescriptor.AddResourcePermissions("urn:dtudo:api-musicx");
        gatewayDescriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        gatewayDescriptor.Permissions.UnionWith(
        new[]
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.EndSession,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId
        }
        .Concat(
            _options.Gateway.Scopes
                .Where(scope => !string.Equals(scope, OpenIddictConstants.Scopes.OpenId, StringComparison.Ordinal)
                    && !string.Equals(scope, OpenIddictConstants.Scopes.OfflineAccess, StringComparison.Ordinal))
                .Select(scope => OpenIddictConstants.Permissions.Prefixes.Scope + scope)));

        var gatewayApplication = await _applicationManager.FindByClientIdAsync(
            _options.Gateway.ClientId,
            cancellationToken);
        if (gatewayApplication is null)
        {
            await _applicationManager.CreateAsync(gatewayDescriptor, cancellationToken);
        }
        else
        {
            await _applicationManager.UpdateAsync(gatewayApplication, gatewayDescriptor, cancellationToken);
        }
    }
}
