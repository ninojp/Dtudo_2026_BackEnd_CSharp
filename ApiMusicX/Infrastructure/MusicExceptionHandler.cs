using ApiMusicX.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiMusicX.Infrastructure;

/// <summary>
/// Converte falhas conhecidas em ProblemDetails sem expor detalhes internos.
/// </summary>
public sealed class MusicExceptionHandler(
    ILogger<MusicExceptionHandler> logger) : IExceptionHandler
{
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

        var (statusCode, title, detail, code) = exception switch
        {
            MusicApiException musicException =>
                (musicException.StatusCode, musicException.Title, musicException.Message, musicException.Code),
            ArgumentException argumentException =>
                (StatusCodes.Status400BadRequest, "Dados invalidos", argumentException.Message, "music.validation"),
            DbUpdateException =>
                (StatusCodes.Status409Conflict, "Conflito na Colecao local", "A operacao viola uma restricao de persistencia.", "music.persistence_conflict"),
            _ =>
                (StatusCodes.Status500InternalServerError, "Erro interno", "Nao foi possivel concluir a operacao.", "music.internal")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Falha nao tratada na ApiMusicX. StatusCode {StatusCode} Path {Path}",
                statusCode,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Operacao rejeitada na ApiMusicX. StatusCode {StatusCode} Path {Path}",
                statusCode,
                httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
        return true;
    }
}
