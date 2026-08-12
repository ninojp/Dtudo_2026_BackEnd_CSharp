using System.Net;
using System.Text.Json;
using ApiDiscogs.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiDiscogs.Tests;

public sealed class DiscogsExceptionHandlerTests
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound, 404, "discogs_resource_not_found")]
    [InlineData(HttpStatusCode.BadGateway, 502, "discogs_upstream_error")]
    [InlineData(HttpStatusCode.ServiceUnavailable, 503, "discogs_unavailable")]
    [InlineData(HttpStatusCode.GatewayTimeout, 504, "discogs_gateway_timeout")]
    public async Task MapsUpstreamStatusToSanitizedProblemDetails(
        HttpStatusCode upstreamStatus,
        int expectedStatus,
        string expectedCode)
    {
        using var responseBody = new MemoryStream();
        var context = CreateContext(responseBody);
        var handler = new DiscogsExceptionHandler(NullLogger<DiscogsExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new HttpRequestException("upstream-body-secret", null, upstreamStatus),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        var problem = await ReadProblemAsync(responseBody);
        Assert.Equal(expectedCode, problem.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("upstream-body-secret", problem.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("api.discogs.com", problem.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapsRateLimitAndSanitizesRetryAfter()
    {
        using var responseBody = new MemoryStream();
        var context = CreateContext(responseBody);
        var exception = new HttpRequestException(
            "upstream-body-secret",
            null,
            HttpStatusCode.TooManyRequests);
        exception.Data["DiscogsRetryAfterSeconds"] = 12;
        var handler = new DiscogsExceptionHandler(NullLogger<DiscogsExceptionHandler>.Instance);

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.Equal("12", context.Response.Headers.RetryAfter.ToString());
        var problem = await ReadProblemAsync(responseBody);
        Assert.Equal("discogs_rate_limited", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(12, problem.RootElement.GetProperty("retryAfterSeconds").GetInt32());
    }

    private static DefaultHttpContext CreateContext(Stream responseBody)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/ApiDiscogs/releases/42";
        context.TraceIdentifier = "discogs-test-trace";
        context.Response.Body = responseBody;
        return context;
    }

    private static async Task<JsonDocument> ReadProblemAsync(Stream responseBody)
    {
        responseBody.Position = 0;
        return await JsonDocument.ParseAsync(responseBody);
    }
}
