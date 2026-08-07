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
        if (!serviceOptions.Enabled || string.IsNullOrWhiteSpace(clientId))
        {
            await next(context);
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
}
