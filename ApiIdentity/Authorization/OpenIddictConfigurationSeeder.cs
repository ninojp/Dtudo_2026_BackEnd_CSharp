using ApiIdentity.Configuration;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace ApiIdentity.Authorization;

public sealed class OpenIddictConfigurationSeeder
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly WinAppOpenIddictOptions _options;

    public OpenIddictConfigurationSeeder(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IOptions<OpenIddictServerConfigurationOptions> options)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _options = options.Value.WinApp;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var scope in _options.Scopes
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
            ClientId = _options.ClientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "Dtudo WinApp"
        };
        descriptor.RedirectUris.Add(new Uri(_options.RedirectUri, UriKind.Absolute));
        descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
        descriptor.AddResourcePermissions(
            "urn:dtudo:api-my-animes",
            "urn:dtudo:api-my-animelist",
            "urn:dtudo:api-file-storage");
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
                _options.Scopes
                    .Where(scope => !string.Equals(scope, OpenIddictConstants.Scopes.OpenId, StringComparison.Ordinal)
                        && !string.Equals(scope, OpenIddictConstants.Scopes.OfflineAccess, StringComparison.Ordinal))
                    .Distinct(StringComparer.Ordinal)
                    .Select(scope => OpenIddictConstants.Permissions.Prefixes.Scope + scope)));

        var application = await _applicationManager.FindByClientIdAsync(_options.ClientId, cancellationToken);
        if (application is null)
        {
            await _applicationManager.CreateAsync(descriptor, cancellationToken);
            return;
        }

        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);
    }
}
