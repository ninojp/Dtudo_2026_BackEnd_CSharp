using LibDtudo.Shared.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace LibDtudo.Shared.Tests;

public class LoggingSecurityTests
{
    [Fact]
    public async Task CorrelationHandler_PropagatesCurrentCorrelationId()
    {
        var captureHandler = new CaptureHandler();
        using var httpClient = new HttpClient(new CorrelationIdDelegatingHandler
        {
            InnerHandler = captureHandler
        });
        using var correlationScope = CorrelationContext.Push("stage04-correlation");

        using var response = await httpClient.GetAsync("https://example.test/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("stage04-correlation", captureHandler.CorrelationId);
    }

    [Fact]
    public void Redactor_ReplacesSensitiveAndNestedProperties()
    {
        var sink = new CaptureSink();
        using var logger = new LoggerConfiguration()
            .Enrich.With<SensitiveDataRedactionEnricher>()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger
            .ForContext("Authorization", "Bearer forbidden-token")
            .ForContext("RequestBody", "{\"password\":\"forbidden-password\"}")
            .ForContext("Request", new { Password = "forbidden-nested-password" }, destructureObjects: true)
            .Information("Request {RequestPath}", "/health");

        Assert.NotNull(sink.Event);
        Assert.Equal("[REDACTED]", GetScalarValue(sink.Event!, "Authorization"));
        Assert.Equal("[REDACTED]", GetScalarValue(sink.Event!, "RequestBody"));

        var request = Assert.IsType<StructureValue>(sink.Event!.Properties["Request"]);
        Assert.Equal("[REDACTED]", GetScalarValue(request.Properties, "Password"));
        Assert.Equal("/health", GetScalarValue(sink.Event!, "RequestPath"));
    }

    private static object? GetScalarValue(LogEvent logEvent, string propertyName)
        => Assert.IsType<ScalarValue>(logEvent.Properties[propertyName]).Value;

    private static object? GetScalarValue(
        IEnumerable<LogEventProperty> properties,
        string propertyName)
    {
        var property = properties.Single(property => property.Name == propertyName);
        return Assert.IsType<ScalarValue>(property.Value).Value;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? CorrelationId { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CorrelationId = request.Headers.GetValues(CorrelationContext.HeaderName).Single();
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class CaptureSink : ILogEventSink
    {
        public LogEvent? Event { get; private set; }

        public void Emit(LogEvent logEvent)
        {
            Event = logEvent;
        }
    }
}
