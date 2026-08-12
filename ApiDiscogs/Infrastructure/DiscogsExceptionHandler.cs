using System.Globalization;
using System.Net;
using ApiDiscogs.Services;
using LibDtudo.Shared.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ApiDiscogs.Infrastructure;

/// <summary>
/// Converte falhas da entrada e da Discogs para ProblemDetails sem expor o payload externo.
/// </summary>
public sealed class DiscogsExceptionHandler(
    ILogger<DiscogsExceptionHandler> logger) : IExceptionHandler
{
    private const string DependencyType = "https://dtudo.local/problems/discogs-dependency";
    private const string ValidationType = "https://dtudo.local/problems/validation";

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted
            || exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var mapping = Map(exception);
        if (mapping.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Falha externa na ApiDiscogs. StatusCode {StatusCode} Code {Code} Path {Path}",
                mapping.StatusCode,
                mapping.Code,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Operacao rejeitada na ApiDiscogs. StatusCode {StatusCode} Code {Code} Path {Path}",
                mapping.StatusCode,
                mapping.Code,
                httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Type = mapping.Type,
            Title = mapping.Title,
            Status = mapping.StatusCode,
            Detail = mapping.Detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = mapping.Code;
        problem.Extensions["traceId"] = CorrelationContext.Current ?? httpContext.TraceIdentifier;

        if (mapping.RetryAfterSeconds is { } retryAfterSeconds)
        {
            problem.Extensions["retryAfterSeconds"] = retryAfterSeconds;
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = mapping.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
        return true;
    }

    private static ProblemMapping Map(Exception exception)
        => exception switch
        {
            DiscogsValidationException validation => new(
                ValidationType,
                StatusCodes.Status400BadRequest,
                "Dados de consulta invalidos.",
                validation.Message,
                "discogs_validation",
                null),
            DiscogsInvalidResponseException => new(
                DependencyType,
                StatusCodes.Status502BadGateway,
                "A fonte externa de musicas nao esta disponivel.",
                "A Discogs retornou uma resposta invalida.",
                "discogs_invalid_response",
                null),
            BrokenCircuitException => new(
                DependencyType,
                StatusCodes.Status503ServiceUnavailable,
                "A fonte externa de musicas esta temporariamente indisponivel.",
                "A consulta externa foi interrompida temporariamente. Tente novamente.",
                "discogs_unavailable",
                null),
            TimeoutRejectedException => new(
                DependencyType,
                StatusCodes.Status504GatewayTimeout,
                "A fonte externa de musicas demorou para responder.",
                "A consulta externa excedeu o tempo limite.",
                "discogs_timeout",
                null),
            HttpRequestException request => MapHttpStatus(
                request.StatusCode,
                request.Data["DiscogsRetryAfterSeconds"] as int?),
            ArgumentException argument => new(
                ValidationType,
                StatusCodes.Status400BadRequest,
                "Dados de consulta invalidos.",
                argument.Message,
                "discogs_validation",
                null),
            OperationCanceledException => new(
                DependencyType,
                StatusCodes.Status504GatewayTimeout,
                "A fonte externa de musicas demorou para responder.",
                "A consulta externa foi cancelada por exceder o prazo operacional.",
                "discogs_timeout",
                null),
            _ => new(
                DependencyType,
                StatusCodes.Status502BadGateway,
                "A fonte externa de musicas nao esta disponivel.",
                "Nao foi possivel concluir a consulta externa.",
                "discogs_connection_error",
                null)
        };

    private static ProblemMapping MapHttpStatus(
        HttpStatusCode? statusCode,
        int? retryAfterSeconds)
        => statusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => new(
                ValidationType,
                StatusCodes.Status400BadRequest,
                "A consulta para a Discogs foi rejeitada.",
                "A fonte externa rejeitou os parametros da consulta.",
                "discogs_request_rejected",
                null),
            HttpStatusCode.NotFound => new(
                DependencyType,
                StatusCodes.Status404NotFound,
                "Recurso Discogs nao encontrado.",
                "O recurso externo solicitado nao foi encontrado.",
                "discogs_resource_not_found",
                null),
            HttpStatusCode.TooManyRequests => new(
                DependencyType,
                StatusCodes.Status429TooManyRequests,
                "A fonte externa de musicas nao esta disponivel.",
                "A consulta foi limitada temporariamente pela fonte externa.",
                "discogs_rate_limited",
                retryAfterSeconds),
            HttpStatusCode.ServiceUnavailable => new(
                DependencyType,
                StatusCodes.Status503ServiceUnavailable,
                "A fonte externa de musicas esta temporariamente indisponivel.",
                "A Discogs esta temporariamente indisponivel.",
                "discogs_unavailable",
                null),
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => new(
                DependencyType,
                StatusCodes.Status504GatewayTimeout,
                "A fonte externa de musicas demorou para responder.",
                "A Discogs nao respondeu dentro do prazo.",
                statusCode == HttpStatusCode.GatewayTimeout
                    ? "discogs_gateway_timeout"
                    : "discogs_timeout",
                null),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new(
                DependencyType,
                StatusCodes.Status502BadGateway,
                "A fonte externa de musicas nao esta disponivel.",
                "A consulta externa nao pode ser concluida pela configuracao do servidor.",
                "discogs_configuration_error",
                null),
            HttpStatusCode.InternalServerError or HttpStatusCode.NotImplemented or HttpStatusCode.BadGateway => new(
                DependencyType,
                StatusCodes.Status502BadGateway,
                "A fonte externa de musicas nao esta disponivel.",
                "A Discogs apresentou uma falha temporaria.",
                "discogs_upstream_error",
                null),
            _ => new(
                DependencyType,
                StatusCodes.Status502BadGateway,
                "A fonte externa de musicas nao esta disponivel.",
                "Nao foi possivel comunicar com a Discogs.",
                "discogs_connection_error",
                null)
        };

    private sealed record ProblemMapping(
        string Type,
        int StatusCode,
        string Title,
        string Detail,
        string Code,
        int? RetryAfterSeconds);
}
