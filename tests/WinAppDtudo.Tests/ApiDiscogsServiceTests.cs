using System.Net;
using System.Net.Http.Json;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class ApiDiscogsServiceTests
{
    [Theory]
    [InlineData(429)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public async Task ExternalErrorsRemainDistinguishable(int statusCode)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage((HttpStatusCode)statusCode)
        {
            Content = JsonContent.Create(new
            {
                code = "discogs_external_error",
                retryAfterSeconds = statusCode == 429 ? 12 : (int?)null
            })
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://winapp.test/")
        };
        var service = new ApiDiscogsService(httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<ApiDiscogsHttpException>(() =>
            service.BuscarArtistasAsync("Artist"));

        Assert.Equal((HttpStatusCode)statusCode, exception.ResponseStatusCode);
        Assert.Equal("/ApiDiscogs/artists/search", handler.Requests.Single().RequestUri?.AbsolutePath);
        Assert.DoesNotContain("token", exception.Message, StringComparison.OrdinalIgnoreCase);
        if (statusCode == 429)
        {
            Assert.Equal(12, exception.RetryAfterSeconds);
        }
    }

    [Fact]
    public async Task CancellationIsPropagatedToTheHttpCall()
    {
        var requestStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            requestStarted.SetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://winapp.test/")
        };
        var service = new ApiDiscogsService(httpClient: httpClient);
        using var cancellation = new CancellationTokenSource();

        var request = service.BuscarArtistasAsync("Artist", cancellationToken: cancellation.Token);
        await requestStarted.Task;
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
    }

    [Fact]
    public async Task EmptySearchReturnsAnEmptyNormalizedPage()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                source = "Discogs",
                items = Array.Empty<object>(),
                pagination = new
                {
                    page = 1,
                    perPage = 10,
                    totalItems = 0,
                    totalPages = 0,
                    hasNextPage = false,
                    uniqueItemsInPage = 0
                },
                isComplete = true,
                warnings = Array.Empty<string>()
            })
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://winapp.test/")
        };
        var service = new ApiDiscogsService(httpClient: httpClient);

        var result = await service.BuscarArtistasAsync("Unknown");

        Assert.Empty(result.Items);
        Assert.Equal("Discogs", result.Source);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : this((request, _) => Task.FromResult(handler(request)))
        {
        }

        public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return _handler(request, cancellationToken);
        }
    }
}
