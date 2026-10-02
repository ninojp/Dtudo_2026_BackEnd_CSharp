using System.Net;
using System.Net.Sockets;
using ApiMyAnimeList.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Timeout;

namespace ApiMyAnimeList.Services;

public static class MyAnimeListResilience
{
    public static void Configure(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        MyAnimeListOptions options)
    {
        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds)
        });

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = options.CircuitBreakerFailureRatio,
            MinimumThroughput = options.CircuitBreakerMinimumThroughput,
            SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
            BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds),
            ShouldHandle = args => new ValueTask<bool>(
                IsIdempotent(args.Context.GetRequestMessage()) && IsTransient(args.Outcome))
        });

        if (options.MaxRetries > 0)
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetries,
                Delay = TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds),
                MaxDelay = TimeSpan.FromSeconds(8),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldRetryAfterHeader = true,
                ShouldHandle = args => new ValueTask<bool>(
                    IsIdempotent(args.Context.GetRequestMessage()) && IsTransient(args.Outcome))
            });
        }

        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        });
    }

    private static bool IsIdempotent(HttpRequestMessage? request)
        => request?.Method == HttpMethod.Get
            || request?.Method == HttpMethod.Head
            || request?.Method == HttpMethod.Options
            || request?.Method == HttpMethod.Put
            || request?.Method == HttpMethod.Delete;

    private static bool IsTransient(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Result is { } response)
        {
            var statusCode = (int)response.StatusCode;
            return response.StatusCode == HttpStatusCode.RequestTimeout
                || response.StatusCode == HttpStatusCode.TooManyRequests
                || statusCode is >= 500 and <= 599;
        }

        return outcome.Exception is TimeoutRejectedException
            || outcome.Exception is HttpRequestException exception
                && exception is not MyAnimeListEgressException
            || outcome.Exception is SocketException socketException
                && IsTransient(socketException.SocketErrorCode);
    }

    private static bool IsTransient(SocketError error)
        => error is SocketError.HostNotFound
            or SocketError.TryAgain
            or SocketError.NoData
            or SocketError.NetworkDown
            or SocketError.NetworkUnreachable
            or SocketError.HostUnreachable
            or SocketError.ConnectionAborted
            or SocketError.ConnectionReset
            or SocketError.ConnectionRefused
            or SocketError.TimedOut;
}
