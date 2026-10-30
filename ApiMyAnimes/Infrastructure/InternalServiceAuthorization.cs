using ApiMyAnimes.Configuration;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ApiMyAnimes.Infrastructure;

/// <summary>Politica das rotas internas de privacidade (servico-a-servico).</summary>
public static class InternalServiceAuthorization
{
    public const string PrivacyPolicyName = "permission:" + MyAnimesPrivacyContract.ServicePermission;

    public static void AddPrivacyPolicy(AuthorizationOptions options) =>
        options.AddPolicy(PrivacyPolicyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new InternalServiceRequirement(MyAnimesPrivacyContract.ServicePermission));
        });
}

/// <summary>Exige token de client credentials com a permissao/escopo de servico e o certificado mTLS do mesmo cliente.</summary>
public sealed class InternalServiceRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>
/// Falha fechada: autoriza somente se (1) a autenticacao de servico esta habilitada, (2) o token tem a permissao E o escopo,
/// (3) e um token de servico (<c>sub == client_id</c>, o que exclui qualquer token de usuario), (4) o client esta na allowlist
/// e (5) o certificado de cliente da conexao TLS e o registrado para esse client. A audiencia ja e validada pelo JwtBearer.
/// </summary>
public sealed class InternalServiceAuthorizationHandler(
    IOptionsMonitor<InternalServiceAuthenticationOptions> options,
    ServiceCertificateValidator certificateValidator,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : AuthorizationHandler<InternalServiceRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InternalServiceRequirement requirement)
    {
        var settings = options.CurrentValue;
        var principal = context.User;
        if (!settings.Enabled || principal.Identity?.IsAuthenticated != true) return;

        if (!HasPermission(principal, requirement.Permission) || !HasScope(principal, requirement.Permission)) return;

        var clientId = principal.FindFirst("client_id")?.Value;
        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(clientId)
            || !string.Equals(clientId, subject, StringComparison.Ordinal)) return;

        var client = settings.FindClient(clientId);
        var httpContext = context.Resource as HttpContext ?? httpContextAccessor.HttpContext;
        if (client is null || httpContext is null) return;

        var certificate = await httpContext.Connection.GetClientCertificateAsync();
        var validation = certificateValidator.Validate(certificate, clientId, client.ToBinding(), timeProvider.GetUtcNow());
        if (validation.Succeeded) context.Succeed(requirement);
    }

    private static bool HasPermission(System.Security.Claims.ClaimsPrincipal principal, string permission) =>
        principal.Claims.Any(claim => claim.Type == "permission" && string.Equals(claim.Value, permission, StringComparison.Ordinal));

    private static bool HasScope(System.Security.Claims.ClaimsPrincipal principal, string scope) =>
        principal.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(value => string.Equals(value, scope, StringComparison.Ordinal));
}
