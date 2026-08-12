using System.Diagnostics;
using LibDtudo.Shared.Logging;
using Serilog.Context;

namespace ApiDiscogs.Infrastructure;

public sealed class RequestCorrelationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationContext.GetOrCreate(
            context.Request.Headers[CorrelationContext.HeaderName].FirstOrDefault());
        context.Response.Headers[CorrelationContext.HeaderName] = correlationId;

        var activity = Activity.Current;
        Activity? startedActivity = null;
        if (activity is null)
        {
            startedActivity = new Activity("ApiDiscogs.Request")
                .SetIdFormat(ActivityIdFormat.W3C)
                .Start();
            activity = startedActivity;
        }

        using var correlationScope = CorrelationContext.Push(correlationId);
        using var correlationProperty = LogContext.PushProperty("CorrelationId", correlationId);
        using var traceProperty = LogContext.PushProperty("TraceId", activity.TraceId.ToHexString());
        using var spanProperty = LogContext.PushProperty("SpanId", activity.SpanId.ToHexString());

        try
        {
            await _next(context);
        }
        finally
        {
            startedActivity?.Stop();
        }
    }
}
