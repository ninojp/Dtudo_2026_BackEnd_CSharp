using System.Net.Http.Headers;
using ApiDiscogs.Configuration;
using Microsoft.Extensions.Options;

namespace ApiDiscogs.Infrastructure;

public sealed class DiscogsAuthenticationHandler(IOptions<DiscogsOptions> options) : DelegatingHandler
{
    private readonly string _token = options.Value.Token;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Discogs", $"token={_token}");
        return base.SendAsync(request, cancellationToken);
    }
}
