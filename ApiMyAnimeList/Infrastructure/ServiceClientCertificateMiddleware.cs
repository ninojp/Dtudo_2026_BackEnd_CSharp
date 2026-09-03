using System.Security.Claims;
using LibDtudo.Shared.Security;
using Microsoft.Extensions.Options;

namespace ApiMyAnimeList.Infrastructure;

public sealed class ServiceClientCertificateMiddleware(
    RequestDelegate next,
    IOptions<ServiceTokenIssuerOptions> options,
    ServiceCertificateValidator certificateValidator,
    TimeProvider timeProvider)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var serviceOptions = options.Value;
        var clientId = context.User.FindFirst("client_id")?.Value;
        if (!serviceOptions.Enabled || !IsServiceToken(context.User))
        {
            await next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var binding = serviceOptions.FindClient(clientId);
        var certificate = await context.Connection.GetClientCertificateAsync();
        var validation = binding is null
            ? new ServiceCertificateValidationResult(false, FailureReason: "client-id-not-registered")
            : certificateValidator.Validate(
                certificate,
                clientId,
                binding,
                timeProvider.GetUtcNow());
        if (!validation.Succeeded)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private static bool IsServiceToken(ClaimsPrincipal principal)
        => principal.Claims.Any(claim =>
            claim.Type == "permission"
            && string.Equals(claim.Value, "service.mal.read", StringComparison.Ordinal))
        && principal.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(scope => string.Equals(scope, "service.mal.read", StringComparison.Ordinal));
}
