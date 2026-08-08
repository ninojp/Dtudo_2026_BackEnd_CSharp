using System.Net;
using System.Net.Http.Json;
using ApiMyAnimeList.Configuration;
using ApiMyAnimeList.Services;
using LibDtudo.Shared.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace ApiMyAnimeList.Tests;

public sealed class MyAnimeListResilienceTests
{
    [Fact]
    public async Task Retries429And504ForGetWithCorrelationThenReturnsSuccess()
    {
        var handler = new SequenceHandler(
            _ => CreateResponse(HttpStatusCode.TooManyRequests),
            _ => CreateResponse(HttpStatusCode.GatewayTimeout),
            _ => CreateResponse(HttpStatusCode.OK, new { id = 42, title = "Recovered" }));
        var options = CreateOptions(maxRetries: 2);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        using (CorrelationContext.Push("resilience-test"))
        {
            var result = await client.GetAnimeAsync(42, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Recovered", result.Title);
        }

        Assert.Equal(3, handler.RequestCount);
        Assert.Equal(3, handler.CorrelationIds.Count);
        Assert.All(handler.CorrelationIds, value => Assert.Equal("resilience-test", value));
    }

    [Fact]
    public async Task DoesNotRetryUnsafeMethod()
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.ServiceUnavailable));
        var options = CreateOptions(maxRetries: 3);
        using var httpClient = CreateHttpClient(handler, options);

        using var request = new HttpRequestMessage(HttpMethod.Post, "v2/anime");
        using var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task OpensCircuitAfterRepeatedTransientFailuresAndRecovers()
    {
        var shouldFail = true;
        var handler = new SequenceHandler(_ => shouldFail
            ? CreateResponse(HttpStatusCode.BadGateway)
            : CreateResponse(HttpStatusCode.OK, new { id = 7, title = "Recovered" }));
        var options = CreateOptions(
            maxRetries: 0,
            circuitBreakerMinimumThroughput: 2,
            circuitBreakerBreakSeconds: 1);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAnimeAsync(7, CancellationToken.None));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAnimeAsync(7, CancellationToken.None));
        await Assert.ThrowsAsync<BrokenCircuitException>(() => client.GetAnimeAsync(7, CancellationToken.None));

        shouldFail = false;
        await Task.Delay(TimeSpan.FromMilliseconds(1200));

        var result = await client.GetAnimeAsync(7, CancellationToken.None);

        Assert.Equal("Recovered", result?.Title);
        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task PropagatesCallerCancellationWithoutRetrying()
    {
        var handler = new CancellationHandler();
        var options = CreateOptions(maxRetries: 3, timeoutSeconds: 10);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);
        using var cancellation = new CancellationTokenSource();

        var requestTask = client.GetAnimeAsync(99, cancellation.Token);
        await Task.Delay(50);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requestTask);
        await handler.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task AppliesTimeoutToUpstreamRequest()
    {
        var handler = new CancellationHandler();
        var options = CreateOptions(maxRetries: 0, timeoutSeconds: 1);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(
            () => client.GetAnimeAsync(100, CancellationToken.None));

        await handler.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData("https://127.0.0.1/v2/anime/1")]
    [InlineData("https://api.myanimelist.net/internal/anime/1")]
    public async Task RejectsDestinationOutsideEgressAllowlist(string uri)
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.OK));
        var options = CreateOptions();
        using var httpClient = new HttpClient(new MyAnimeListEgressHandler(Options.Create(options))
        {
            InnerHandler = handler
        });

        await Assert.ThrowsAsync<MyAnimeListEgressException>(() => httpClient.GetAsync(uri));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public void RejectsInvalidEgressConfiguration()
    {
        var options = CreateOptions();
        options.BaseUrl = "http://api.myanimelist.net/v2/";

        Assert.False(MyAnimeListEgressHandler.IsValidConfiguration(options));
    }

    private static MyAnimeListClient CreateClient(HttpClient httpClient, MyAnimeListOptions options)
        => new(
            httpClient,
            Options.Create(options),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<MyAnimeListClient>.Instance);

    private static HttpClient CreateHttpClient(HttpMessageHandler terminalHandler, MyAnimeListOptions options)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        MyAnimeListResilience.Configure(pipelineBuilder, options);
        var resilienceHandler = new ResilienceHandler(pipelineBuilder.Build())
        {
            InnerHandler = terminalHandler
        };
        var egressHandler = new MyAnimeListEgressHandler(Options.Create(options))
        {
            InnerHandler = resilienceHandler
        };
        var correlationHandler = new CorrelationIdDelegatingHandler
        {
            InnerHandler = egressHandler
        };

        return new HttpClient(correlationHandler)
        {
            BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute)
        };
    }

    private static MyAnimeListOptions CreateOptions(
        int maxRetries = 1,
        int timeoutSeconds = 2,
        int circuitBreakerMinimumThroughput = 100,
        int circuitBreakerBreakSeconds = 5)
        => new()
        {
            BaseUrl = "https://api.myanimelist.net/v2/",
            ClientId = "test-client-id",
            AllowedHosts = ["api.myanimelist.net"],
            AllowedPathPrefix = "/v2/",
            MaxRetries = maxRetries,
            RetryDelayMilliseconds = 1,
            TimeoutSeconds = timeoutSeconds,
            CacheMinutes = 1,
            TotalTimeoutSeconds = Math.Max(timeoutSeconds * 2, timeoutSeconds),
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerMinimumThroughput = circuitBreakerMinimumThroughput,
            CircuitBreakerSamplingSeconds = 10,
            CircuitBreakerBreakSeconds = circuitBreakerBreakSeconds
        };

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, object? body = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (body is not null)
        {
            response.Content = JsonContent.Create(body);
        }

        return response;
    }

    private sealed class SequenceHandler(params Func<int, HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage>[] _responses = responses;

        public int RequestCount { get; private set; }

        public List<string?> CorrelationIds { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CorrelationIds.Add(request.Headers.TryGetValues(CorrelationContext.HeaderName, out var values)
                ? values.SingleOrDefault()
                : null);
            var index = RequestCount++;
            var responseFactory = _responses[Math.Min(index, _responses.Length - 1)];
            return Task.FromResult(responseFactory(index));
        }
    }

    private sealed class CancellationHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public TaskCompletionSource<bool> CancellationObserved { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("O handler de teste deveria ser cancelado.");
            }
            catch (OperationCanceledException)
            {
                CancellationObserved.TrySetResult(true);
                throw;
            }
        }
    }
}
