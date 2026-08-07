using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiMyAnimeList.Tests;

internal static class TestAuthentication
{
    public const string Scheme = "Test";

    public static void Add(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = Scheme;
            options.DefaultChallengeScheme = Scheme;
        }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(Scheme, _ => { });
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Claims", out var rawClaims))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>();
        foreach (var rawClaim in rawClaims.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = rawClaim.IndexOf('=');
            if (separator <= 0)
                return Task.FromResult(AuthenticateResult.Fail("Formato de claim de teste invalido."));

            claims.Add(new Claim(rawClaim[..separator], rawClaim[(separator + 1)..]));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
