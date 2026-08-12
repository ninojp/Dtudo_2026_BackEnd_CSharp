using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiDiscogs.Configuration;
using ApiDiscogs.Infrastructure;
using ApiDiscogs.Services;
using LibDtudo.Shared.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ApiDiscogs.Tests;

public sealed class DiscogsClientTests
{
    [Fact]
    public async Task SuccessfulSearchReturnsJsonAndPreservesCorrelation()
    {
        var handler = new SequenceHandler(_ => CreateResponse(
            HttpStatusCode.OK,
            new { results = new[] { new { id = 42, title = "Artist" } } }));
        var options = CreateOptions();
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        using (CorrelationContext.Push("discogs-test"))
        using (var document = await client.SearchArtistsAsync("  Artist  ", cancellationToken: CancellationToken.None))
        {
            Assert.Equal(42, document.RootElement.GetProperty("results")[0].GetProperty("id").GetInt32());
        }

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal("/database/search", handler.Requests.Single().RequestUri?.AbsolutePath);
        Assert.Equal("discogs-test", handler.CorrelationIds.Single());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ClientDoesNotRetryPermanentStatuses(HttpStatusCode statusCode)
    {
        var handler = new SequenceHandler(_ => CreateResponse(statusCode, new { secret = "upstream-body" }));
        var options = CreateOptions(maxRetries: 3);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetReleaseAsync(7, CancellationToken.None));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.DoesNotContain("upstream-body", exception.ToString(), StringComparison.Ordinal);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task DoesNotRetryHttpExceptionWithPermanentStatus()
    {
        var handler = new ThrowingHandler(new HttpRequestException(
            "bad request",
            null,
            HttpStatusCode.BadRequest));
        var options = CreateOptions(maxRetries: 3);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetArtistAsync(9, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Retries429UsingRetryAfterHeaderThenReturnsSuccess()
    {
        var handler = new SequenceHandler(
            _ => CreateResponse(HttpStatusCode.TooManyRequests, retryAfter: TimeSpan.Zero),
            _ => CreateResponse(HttpStatusCode.OK, new { id = 7 }));
        var options = CreateOptions(maxRetries: 1);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        using var document = await client.GetMasterAsync(7, CancellationToken.None);

        Assert.Equal(7, document.RootElement.GetProperty("id").GetInt32());
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task Retries500OnlyUpToConfiguredLimit()
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.InternalServerError));
        var options = CreateOptions(maxRetries: 1);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetArtistAsync(9, CancellationToken.None));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task TimeoutCancelsUpstreamRequest()
    {
        var handler = new CancellationHandler();
        var options = CreateOptions(maxRetries: 0, timeoutSeconds: 1, totalTimeoutSeconds: 2);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        await Assert.ThrowsAsync<TimeoutRejectedException>(
            () => client.GetArtistAsync(100, CancellationToken.None));

        await handler.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task CallerCancellationIsPropagatedWithoutRetry()
    {
        var handler = new CancellationHandler();
        var options = CreateOptions(maxRetries: 3, timeoutSeconds: 10, totalTimeoutSeconds: 20);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);
        using var cancellation = new CancellationTokenSource();

        var requestTask = client.GetArtistAsync(99, cancellation.Token);
        await Task.Delay(50);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requestTask);
        await handler.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task OpensCircuitAfterRepeatedTransientFailures()
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.BadGateway));
        var options = CreateOptions(
            maxRetries: 0,
            circuitBreakerMinimumThroughput: 2,
            circuitBreakerBreakSeconds: 5);
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetArtistAsync(7, CancellationToken.None));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetArtistAsync(8, CancellationToken.None));
        await Assert.ThrowsAsync<BrokenCircuitException>(() => client.GetArtistAsync(9, CancellationToken.None));

        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task CachesSearchesAndDetailsUsingStableKeys()
    {
        var handler = new SequenceHandler(request => request.RequestUri?.AbsolutePath switch
        {
            "/database/search" => CreateResponse(HttpStatusCode.OK, new { kind = "search" }),
            "/releases/42" => CreateResponse(HttpStatusCode.OK, new { kind = "release" }),
            _ => throw new InvalidOperationException("Endpoint inesperado no teste.")
        });
        var options = CreateOptions();
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        using var firstSearch = await client.SearchArtistsAsync(" Artist   Name ", cancellationToken: CancellationToken.None);
        using var secondSearch = await client.SearchArtistsAsync("artist name", cancellationToken: CancellationToken.None);
        using var firstDetails = await client.GetReleaseAsync(42, CancellationToken.None);
        using var secondDetails = await client.GetReleaseAsync(42, CancellationToken.None);

        Assert.Equal("search", firstSearch.RootElement.GetProperty("kind").GetString());
        Assert.Equal("search", secondSearch.RootElement.GetProperty("kind").GetString());
        Assert.Equal("release", firstDetails.RootElement.GetProperty("kind").GetString());
        Assert.Equal("release", secondDetails.RootElement.GetProperty("kind").GetString());
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task SearchInputCannotChangeHostOrAllowedPath()
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.OK, new { results = Array.Empty<object>() }));
        var options = CreateOptions();
        using var httpClient = CreateHttpClient(handler, options);
        var client = CreateClient(httpClient, options);

        using var document = await client.SearchArtistsAsync(
            "https://evil.example/releases/1",
            cancellationToken: CancellationToken.None);

        Assert.Equal("/database/search", handler.Requests.Single().RequestUri?.AbsolutePath);
        Assert.Equal("api.discogs.com", handler.Requests.Single().RequestUri?.DnsSafeHost);
        Assert.Equal(0, document.RootElement.GetProperty("results").GetArrayLength());
    }

    [Theory]
    [InlineData("https://evil.example/releases/1")]
    [InlineData("https://api.discogs.com/private/1")]
    [InlineData("https://api.discogs.com/database/../private/1")]
    public async Task EgressRejectsHostAndPathOutsideAllowlist(string uri)
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.OK));
        var options = CreateOptions();
        using var httpClient = new HttpClient(new DiscogsEgressHandler(Options.Create(options))
        {
            InnerHandler = handler
        });

        await Assert.ThrowsAsync<DiscogsEgressException>(() => httpClient.GetAsync(uri));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task AuthenticationHandlerAddsTokenOnlyToUpstreamRequest()
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.OK));
        var options = CreateOptions();
        using var httpClient = new HttpClient(new DiscogsAuthenticationHandler(Options.Create(options))
        {
            InnerHandler = handler
        });

        await httpClient.GetAsync(new Uri("https://api.discogs.com/releases/1"));

        var authorization = handler.Requests.Single().Headers.Authorization;
        Assert.NotNull(authorization);
        Assert.Equal("Discogs", authorization.Scheme);
        Assert.Equal("token=test-discogs-token", authorization.Parameter);
    }

    private static DiscogsClient CreateClient(HttpClient httpClient, DiscogsOptions options)
        => new(
            httpClient,
            Options.Create(options),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<DiscogsClient>.Instance);

    private static HttpClient CreateHttpClient(
        HttpMessageHandler terminalHandler,
        DiscogsOptions options)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        DiscogsResilience.Configure(pipelineBuilder, options);
        var resilienceHandler = new ResilienceHandler(pipelineBuilder.Build())
        {
            InnerHandler = terminalHandler
        };
        var egressHandler = new DiscogsEgressHandler(Options.Create(options))
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

    private static DiscogsOptions CreateOptions(
        int maxRetries = 1,
        int timeoutSeconds = 2,
        int totalTimeoutSeconds = 5,
        int circuitBreakerMinimumThroughput = 100,
        int circuitBreakerBreakSeconds = 5)
        => new()
        {
            BaseUrl = "https://api.discogs.com/",
            Token = "test-discogs-token",
            AllowedHosts = ["api.discogs.com"],
            AllowedPathPrefix = "/",
            UserAgent = "Dtudo-ApiDiscogs.Tests/1.0",
            MaxRetries = maxRetries,
            RetryDelayMilliseconds = 1,
            TimeoutSeconds = timeoutSeconds,
            CacheMinutes = 1,
            MaxResponseBytes = 2_000_000,
            TotalTimeoutSeconds = totalTimeoutSeconds,
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerMinimumThroughput = circuitBreakerMinimumThroughput,
            CircuitBreakerSamplingSeconds = 10,
            CircuitBreakerBreakSeconds = circuitBreakerBreakSeconds
        };

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        object? body = null,
        TimeSpan? retryAfter = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (body is not null)
        {
            response.Content = JsonContent.Create(body);
        }

        if (retryAfter is not null)
        {
            response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter.Value);
        }

        return response;
    }

    private sealed class SequenceHandler(
        params Func<HttpRequestMessage, HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>[] _responses = responses;

        public int RequestCount { get; private set; }

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string?> CorrelationIds { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            CorrelationIds.Add(request.Headers.TryGetValues(CorrelationContext.HeaderName, out var values)
                ? values.SingleOrDefault()
                : null);
            var index = RequestCount++;
            var responseFactory = _responses[Math.Min(index, _responses.Length - 1)];
            return Task.FromResult(responseFactory(request));
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

    private sealed class ThrowingHandler(HttpRequestException exception) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromException<HttpResponseMessage>(exception);
        }
    }
}
