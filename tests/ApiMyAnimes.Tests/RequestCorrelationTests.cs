using ApiMyAnimes.Infrastructure;
using LibDtudo.Shared.Logging;
using Microsoft.AspNetCore.Http;

namespace ApiMyAnimes.Tests;

public class RequestCorrelationTests
{
    [Fact]
    public async Task Middleware_EchoesProvidedCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationContext.HeaderName] = "stage04-local";
        var middleware = new RequestCorrelationMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("stage04-local", context.Response.Headers[CorrelationContext.HeaderName].ToString());
    }
}
