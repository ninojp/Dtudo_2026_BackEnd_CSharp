using ApiIdentity.Configuration;
using LibDtudo.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using System.Security.Cryptography.X509Certificates;

namespace ApiIdentity;

internal sealed class ServiceTokenEndpoint(
    IOptions<ServiceTokenIssuerOptions> serviceOptions,
    IOptions<OpenIddictServerConfigurationOptions> openIddictOptions,
    IOptions<OpenIddictServerOptions> serverOptions,
    ServiceTokenRequestValidator validator,
    TimeProvider timeProvider)
{
    public async Task<bool> TryHandleAsync(HttpContext context)
    {
        var options = serviceOptions.Value;
        if (!options.Enabled || !context.Request.HasFormContentType)
        {
            return false;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var grantTypes = form[OpenIddictConstants.Parameters.GrantType];
        if (grantTypes.Count != 1
            || !string.Equals(
                grantTypes[0],
                OpenIddictConstants.GrantTypes.ClientCredentials,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (HasNonEmptyParameter(form, OpenIddictConstants.Parameters.ClientSecret)
            || HasNonEmptyParameter(form, OpenIddictConstants.Parameters.ClientAssertion)
            || HasNonEmptyParameter(form, OpenIddictConstants.Parameters.ClientAssertionType)
            || context.Request.Headers.Authorization.Any(value =>
                value is not null
                && value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)))
        {
            await WriteErrorAsync(context, "invalid_client", StatusCodes.Status401Unauthorized);
            return true;
        }

        var clientIdValues = form[OpenIddictConstants.Parameters.ClientId];
        if (clientIdValues.Count != 1 || string.IsNullOrWhiteSpace(clientIdValues[0]))
        {
            await WriteErrorAsync(context, "invalid_client", StatusCodes.Status401Unauthorized);
            return true;
        }

        var clientId = clientIdValues[0]!;
        var resourceValues = form[OpenIddictConstants.Parameters.Resource];
        if (resourceValues.Count != 1 || string.IsNullOrWhiteSpace(resourceValues[0]))
        {
            await WriteErrorAsync(context, "invalid_target", StatusCodes.Status400BadRequest);
            return true;
        }

        var scopeValues = form[OpenIddictConstants.Parameters.Scope];
        if (scopeValues.Count > 1)
        {
            await WriteErrorAsync(context, "invalid_request", StatusCodes.Status400BadRequest);
            return true;
        }

        var scopes = scopeValues.Count == 0
            ? []
            : scopeValues[0]!
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        var audience = resourceValues[0]!;
        var client = options.FindClient(clientId);
        if (client is null)
        {
            await WriteErrorAsync(context, "invalid_client", StatusCodes.Status401Unauthorized);
            return true;
        }

        var certificate = await context.Connection.GetClientCertificateAsync();
        var validation = validator.Validate(
            certificate,
            new ServiceTokenRequest(clientId, audience, scopes),
            client,
            timeProvider.GetUtcNow());
        if (!validation.Succeeded)
        {
            var error = validation.FailureReason == "scope-not-allowed"
                ? "invalid_scope"
                : validation.FailureReason == "audience-not-allowed"
                    ? "invalid_target"
                    : "invalid_client";
            await WriteErrorAsync(
                context,
                error,
                error == "invalid_client"
                    ? StatusCodes.Status401Unauthorized
                    : StatusCodes.Status400BadRequest);
            return true;
        }

        var signingCredentials = serverOptions.Value.SigningCredentials.FirstOrDefault();
        if (signingCredentials is null)
        {
            await WriteErrorAsync(context, "server_error", StatusCodes.Status500InternalServerError);
            return true;
        }

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddSeconds(options.AccessTokenLifetimeSeconds);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = openIddictOptions.Value.Issuer,
            Audience = audience,
            Claims = new Dictionary<string, object>
            {
                [OpenIddictConstants.Claims.Subject] = clientId,
                [OpenIddictConstants.Claims.ClientId] = clientId,
                [OpenIddictConstants.Claims.Scope] = string.Join(' ', scopes),
                ["permission"] = scopes,
                [OpenIddictConstants.Claims.JwtId] = Guid.NewGuid().ToString("N")
            },
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = signingCredentials
        });

        await context.Response.WriteAsJsonAsync(
            new Dictionary<string, object>
            {
                ["access_token"] = token,
                ["token_type"] = "Bearer",
                ["expires_in"] = options.AccessTokenLifetimeSeconds,
                ["scope"] = string.Join(' ', scopes)
            },
            context.RequestAborted);
        return true;
    }

    private static bool HasNonEmptyParameter(IFormCollection form, string name) =>
        form[name].Any(value => !string.IsNullOrWhiteSpace(value));

    private static async Task WriteErrorAsync(HttpContext context, string error, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error }, context.RequestAborted);
    }
}
