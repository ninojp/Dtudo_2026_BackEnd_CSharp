using ApiFileStorage.Monitoring.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiFileStorage.Monitoring.Relocation;

// Unica excecao controlada a regra append-only: regrava somente RootKey de chaves
// legadas, por UPDATE em massa dentro de transacao. Ids e datas nao mudam, entao
// Snapshots e Observations continuam referenciando as mesmas localizacoes.
// Idempotente: apos a primeira execucao o plano fica vazio.
public sealed class MonitoringRootKeyMigration(IServiceScopeFactory scopeFactory,
    ILogger<MonitoringRootKeyMigration> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
            var storedKeys = await context.Locations.Select(location => location.RootKey).Distinct().ToListAsync(cancellationToken);
            var plan = MonitoringRootKeyPlan.Plan(storedKeys);
            if (plan.Count == 0)
            {
                logger.LogInformation("Monitoramento: nenhuma chave legada em monitoring.Locations.");
                return;
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var updated = 0;
            foreach (var translation in plan)
            {
                updated += await context.Locations
                    .Where(location => location.RootKey == translation.From)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(location => location.RootKey, translation.To), cancellationToken);
                logger.LogInformation("Monitoramento: chave legada {From} atualizada para {To}.", translation.From, translation.To);
            }

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Monitoramento: {Updated} localizacoes atualizadas para as chaves atuais.", updated);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A leitura continua traduzindo chaves legadas; a proxima inicializacao tenta novamente.
            logger.LogError(exception, "Monitoramento: falha ao atualizar chaves legadas; a leitura continua traduzindo-as.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
