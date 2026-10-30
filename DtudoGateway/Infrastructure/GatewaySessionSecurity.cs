using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Yarp.ReverseProxy.Model;

namespace DtudoGateway.Infrastructure;

public static class GatewayAuthenticationSchemes
{
    public const string Cookie = "BffCookie";
    public const string OpenIdConnect = "oidc";
}

/// <summary>Define em quais rotas o gateway injeta o access token da sessao como Bearer.</summary>
public static class GatewayAccessTokenRoutes
{
    public static bool RequiresAccessToken(HttpRequest request) =>
        request.Path.StartsWithSegments("/api/catalog/music")
        || request.Path.StartsWithSegments("/api/external/discogs")
        || HttpMethods.IsPost(request.Method)
            && request.Path.Equals(GatewayRouteConfiguration.AnimeStatusPublicPath, StringComparison.OrdinalIgnoreCase)
        || (HttpMethods.IsGet(request.Method) || HttpMethods.IsPut(request.Method) || HttpMethods.IsDelete(request.Method))
            && GatewayRouteConfiguration.IsAnimeWatchPath(request.Path);
}

/// <summary>Renova o access token guardado na sessao do cookie antes de ele ser repassado a uma API.</summary>
public static class GatewayCookieTokenEvents
{
    private const string AccessTokenName = "access_token";
    private const string RefreshTokenName = "refresh_token";
    private const string IdTokenName = "id_token";
    private const string ExpiresAtName = "expires_at";

    public static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        if (!GatewayAccessTokenRoutes.RequiresAccessToken(context.Request))
        {
            return;
        }

        var properties = context.Properties;
        var refreshToken = properties.GetTokenValue(RefreshTokenName);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
        if (TryGetExpiresAt(properties, out var expiresAt)
            && expiresAt - RefreshSkew > timeProvider.GetUtcNow())
        {
            return;
        }

        var refresher = context.HttpContext.RequestServices.GetRequiredService<IGatewayTokenRefresher>();
        var result = await refresher.RefreshAsync(refreshToken, context.HttpContext.RequestAborted);
        switch (result.Outcome)
        {
            case TokenRefreshOutcome.Succeeded:
                var tokens = ReadTokens(properties);
                tokens[AccessTokenName] = result.AccessToken!;
                if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                {
                    tokens[RefreshTokenName] = result.RefreshToken;
                }

                if (!string.IsNullOrWhiteSpace(result.IdToken))
                {
                    tokens[IdTokenName] = result.IdToken;
                }

                tokens[ExpiresAtName] = result.ExpiresAt!.Value.ToString("o", CultureInfo.InvariantCulture);
                WriteTokens(properties, tokens);
                context.ShouldRenew = true;
                break;

            case TokenRefreshOutcome.Rejected:
                // A ApiIdentity encerrou a concessao: descarta os tokens para nao repetir o refresh a cada chamada.
                // As APIs passam a responder 401 e o site conduz o usuario a um novo login.
                var remaining = ReadTokens(properties);
                remaining.Remove(AccessTokenName);
                remaining.Remove(RefreshTokenName);
                remaining.Remove(ExpiresAtName);
                WriteTokens(properties, remaining);
                context.ShouldRenew = true;
                break;
        }
    }

    private static bool TryGetExpiresAt(AuthenticationProperties properties, out DateTimeOffset expiresAt) =>
        DateTimeOffset.TryParse(
            properties.GetTokenValue(ExpiresAtName),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out expiresAt);

    private static Dictionary<string, string> ReadTokens(AuthenticationProperties properties) =>
        properties.GetTokens()
            .Where(token => token.Name is not null && token.Value is not null)
            .ToDictionary(token => token.Name, token => token.Value, StringComparer.Ordinal);

    private static void WriteTokens(AuthenticationProperties properties, Dictionary<string, string> tokens) =>
        properties.StoreTokens(tokens.Select(token => new AuthenticationToken
        {
            Name = token.Key,
            Value = token.Value
        }));
}

/// <summary>
/// Aplica, apos a autorizacao e antes do proxy, as protecoes declaradas nos metadados da rota YARP:
/// token CSRF, JSON obrigatorio e limite de corpo.
/// </summary>
public static class GatewayRequestProtection
{
    public static IApplicationBuilder UseGatewayRequestProtection(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var routeMetadata = context.GetEndpoint()?.Metadata.GetMetadata<RouteModel>()?.Config.Metadata;
            if (routeMetadata is null)
            {
                await next(context);
                return;
            }

            if (routeMetadata.TryGetValue(GatewayRouteConfiguration.RequireCsrfMetadataKey, out var requireCsrf)
                && string.Equals(requireCsrf, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                if (!await antiforgery.IsRequestValidAsync(context))
                {
                    await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "invalid_csrf");
                    return;
                }

                var allowEmptyBody = routeMetadata.TryGetValue(GatewayRouteConfiguration.AllowEmptyBodyMetadataKey, out var rawAllowEmpty)
                    && string.Equals(rawAllowEmpty, bool.TrueString, StringComparison.OrdinalIgnoreCase);
                if (!(allowEmptyBody && !HasRequestBody(context.Request)) && !context.Request.HasJsonContentType())
                {
                    await WriteErrorAsync(context, StatusCodes.Status415UnsupportedMediaType, "unsupported_media_type");
                    return;
                }
            }

            if (routeMetadata.TryGetValue(GatewayRouteConfiguration.MaxBodyBytesMetadataKey, out var rawLimit)
                && long.TryParse(rawLimit, NumberStyles.None, CultureInfo.InvariantCulture, out var limit))
            {
                if (context.Request.ContentLength > limit)
                {
                    await WriteErrorAsync(context, StatusCodes.Status413PayloadTooLarge, "payload_too_large");
                    return;
                }

                // Sem Content-Length (chunked) o Kestrel interrompe a leitura ao passar do limite.
                var bodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (bodySizeFeature is { IsReadOnly: false })
                {
                    bodySizeFeature.MaxRequestBodySize = limit;
                }
            }

            await next(context);
        });

    private static bool HasRequestBody(HttpRequest request) =>
        request.ContentLength > 0 || request.Headers.ContainsKey("Transfer-Encoding");

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string error)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new { error });
    }
}
