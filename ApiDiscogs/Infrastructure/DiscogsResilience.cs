using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Timeout;

namespace ApiDiscogs.Infrastructure;

public static class DiscogsResilience
{
    public static void Configure(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        Configuration.DiscogsOptions options)
    {
        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds)
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

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = options.CircuitBreakerFailureRatio,
            MinimumThroughput = options.CircuitBreakerMinimumThroughput,
            SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
            BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds),
            ShouldHandle = args => new ValueTask<bool>(
                IsIdempotent(args.Context.GetRequestMessage()) && IsTransient(args.Outcome))
        });

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

        return IsTransientException(outcome.Exception);
    }

    private static bool IsTransientException(Exception? exception)
    {
        if (exception is TimeoutRejectedException)
        {
            return true;
        }

        if (exception is not HttpRequestException requestException
            || requestException is DiscogsEgressException)
        {
            return false;
        }

        return requestException.StatusCode is null
            || requestException.StatusCode == HttpStatusCode.RequestTimeout
            || requestException.StatusCode == HttpStatusCode.TooManyRequests
            || (int)requestException.StatusCode is >= 500 and <= 599;
    }
}
