using ApiIdentity.Configuration;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Privacy;

/// <summary>Resumo de uma varredura do worker (somente contagens).</summary>
public sealed record PrivacyDeletionBatchResult(int Selected, int Completed, int Failed, int Skipped);

/// <summary>
/// Processa, em segundo plano, os pedidos de exclusao LGPD cuja carencia terminou. Um pedido por vez, em um escopo DI proprio
/// por pedido (cada um com seu <c>DbContext</c>), com lote limitado, intervalo configuravel e cancelamento cooperativo no shutdown.
/// Desabilitado por padrao (<c>PrivacyDeletionWorker:Enabled</c>). Os logs nunca incluem identificador de conta, e-mail nem
/// mensagem de excecao: apenas o id do pedido e o tipo da excecao. A auditoria fica nos eventos de provisionamento da ApiIdentity.
/// </summary>
public sealed class PrivacyDeletionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PrivacyDeletionWorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<PrivacyDeletionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Worker de exclusao LGPD desabilitado por configuracao (PrivacyDeletionWorker:Enabled=false).");
            return;
        }

        logger.LogInformation(
            "Worker de exclusao LGPD iniciado (intervalo {IntervalSeconds}s, lote {BatchSize}).",
            settings.PollIntervalSeconds,
            settings.BatchSize);

        try
        {
            if (settings.InitialDelaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.InitialDelaySeconds), timeProvider, stoppingToken);
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollIntervalSeconds), timeProvider);
            do
            {
                try
                {
                    var result = await ProcessBatchAsync(stoppingToken);
                    if (result.Selected > 0)
                    {
                        logger.LogInformation(
                            "Varredura de exclusao LGPD: {Selected} pedido(s), {Completed} concluido(s), {Failed} com falha, {Skipped} ignorado(s).",
                            result.Selected,
                            result.Completed,
                            result.Failed,
                            result.Skipped);
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError("Falha na varredura de exclusao LGPD ({ExceptionType}); nova tentativa no proximo intervalo.", exception.GetType().Name);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker de exclusao LGPD encerrado.");
        }
    }

    /// <summary>Uma varredura: seleciona os pedidos vencidos (lote limitado) e processa um por vez.</summary>
    public async Task<PrivacyDeletionBatchResult> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        IReadOnlyList<Guid> requestIds;
        await using (var selectionScope = scopeFactory.CreateAsyncScope())
        {
            requestIds = await selectionScope.ServiceProvider
                .GetRequiredService<IdentityPrivacyService>()
                .GetDueDeletionRequestIdsAsync(
                    settings.BatchSize,
                    TimeSpan.FromMinutes(settings.RetryDelayMinutes),
                    cancellationToken);
        }

        int completed = 0, failed = 0, skipped = 0;
        foreach (var requestId in requestIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var outcome = await scope.ServiceProvider
                    .GetRequiredService<IdentityPrivacyService>()
                    .ProcessDueDeletionAsync(requestId, cancellationToken);
                switch (outcome)
                {
                    case DeletionProcessingOutcome.Completed:
                        completed++;
                        break;
                    case DeletionProcessingOutcome.DownstreamFailed:
                        failed++;
                        logger.LogWarning("Pedido de exclusao {DeletionRequestId} mantido pendente: ApiMyAnimes nao concluiu a exclusao.", requestId);
                        break;
                    default:
                        skipped++;
                        break;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                failed++;
                logger.LogError(
                    "Falha inesperada ao processar o pedido de exclusao {DeletionRequestId} ({ExceptionType}); o pedido continua pendente.",
                    requestId,
                    exception.GetType().Name);
            }
        }

        return new PrivacyDeletionBatchResult(requestIds.Count, completed, failed, skipped);
    }
}
