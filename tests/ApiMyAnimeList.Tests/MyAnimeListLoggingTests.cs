using System.Net;
using ApiMyAnimeList.Configuration;
using ApiMyAnimeList.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApiMyAnimeList.Tests;

public class MyAnimeListLoggingTests
{
    [Fact]
    public async Task UpstreamError_DoesNotIncludeResponseBodyInException()
    {
        const string forbiddenResponseBody = "forbidden-upstream-response-body";
        using var httpClient = new HttpClient(new FixedResponseHandler(forbiddenResponseBody))
        {
            BaseAddress = new Uri("https://example.test/v2/")
        };
        var options = Options.Create(new MyAnimeListOptions
        {
            BaseUrl = "https://example.test/v2/",
            ClientId = "test-client-id",
            MaxRetries = 0
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = new MyAnimeListClient(
            httpClient,
            options,
            cache,
            NullLogger<MyAnimeListClient>.Instance);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SearchAsync("query", 0, 1, CancellationToken.None));

        Assert.DoesNotContain(forbiddenResponseBody, exception.ToString(), StringComparison.Ordinal);
    }

    private sealed class FixedResponseHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(responseBody)
            });
    }
}
