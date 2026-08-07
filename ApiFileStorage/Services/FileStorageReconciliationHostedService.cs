namespace ApiFileStorage.Services;

public sealed class FileStorageReconciliationHostedService(
    IFileStorageLifecycleService lifecycleService,
    ILogger<FileStorageReconciliationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var result = await lifecycleService.ReconcileAsync(stoppingToken);
            logger.LogInformation(
                "Reconcilacao de arquivos concluida: imports={CompletedImports}, deletes={CompletedDeletes}, scanner_pending={AwaitingScanner}, promotion_pending={AwaitingPromotion}, rejected={RejectedOperations}, purged={PurgedTrashItems}",
                result.CompletedImports,
                result.CompletedDeletes,
                result.AwaitingScanner,
                result.AwaitingPromotion,
                result.RejectedOperations,
                result.PurgedTrashItems);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (FileStorageScannerUnavailableException)
        {
            logger.LogWarning("Reconcilacao interrompida porque o scanner obrigatorio esta indisponivel; nenhuma importacao foi promovida.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reconcilacao de arquivos falhou; estados pendentes permanecem em quarentena.");
        }
    }
}
