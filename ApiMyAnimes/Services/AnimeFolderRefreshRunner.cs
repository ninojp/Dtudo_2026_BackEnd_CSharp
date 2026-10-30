using LibDtudo.Shared.Dtos;

namespace ApiMyAnimes.Services;

/// <summary>Ja ha uma atualizacao de status de pasta em andamento neste processo.</summary>
public sealed class AnimeFolderRefreshBusyException(Guid activeRunId)
    : InvalidOperationException("Ja existe uma atualizacao de status de pasta em andamento.")
{
    /// <summary>Execucao ativa (<see cref="Guid.Empty"/> enquanto ela ainda esta sendo registrada).</summary>
    public Guid ActiveRunId { get; } = activeRunId;
}

/// <summary>
/// Executa, em segundo plano e uma por vez, as atualizacoes de status de pasta. A requisicao HTTP apenas registra a execucao
/// e recebe o identificador para consulta; o trabalho roda em escopo proprio e e cancelado ao encerrar a aplicacao.
/// </summary>
public sealed class AnimeFolderRefreshRunner(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    ILogger<AnimeFolderRefreshRunner> logger)
{
    private readonly object sync = new();
    private ActiveRun? active;

    /// <summary>Registra a execucao e a inicia em segundo plano.</summary>
    /// <exception cref="AnimeFolderRefreshBusyException">Outra execucao esta ativa.</exception>
    public async Task<AnimeFolderRefreshRunDto> StartAsync(int? myAnimeId, string requestedBy, CancellationToken cancellationToken)
    {
        var current = new ActiveRun(CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping));
        lock (sync)
        {
            if (active is not null)
            {
                current.Cancellation.Dispose();
                throw new AnimeFolderRefreshBusyException(active.RunId);
            }
            active = current;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var run = await scope.ServiceProvider.GetRequiredService<AnimeFolderRefreshService>()
                .CreateRunAsync(myAnimeId, requestedBy, cancellationToken);
            current.RunId = run.Id;
            current.Task = Task.Run(() => ExecuteAsync(run.Id, current), CancellationToken.None);
            return run.ToDto();
        }
        catch
        {
            Release(current);
            throw;
        }
    }

    /// <summary>A execucao esta em andamento neste processo.</summary>
    public bool IsActive(Guid runId)
    {
        lock (sync) return active is not null && active.RunId == runId;
    }

    /// <summary>Pede o cancelamento; retorna <c>false</c> se a execucao nao esta ativa.</summary>
    public bool Cancel(Guid runId)
    {
        ActiveRun? current;
        lock (sync) current = active is not null && active.RunId == runId ? active : null;
        if (current is null) return false;
        try
        {
            current.Cancellation.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>Aguarda o termino da execucao ativa informada (ou retorna se ja terminou); usado por testes e no desligamento.</summary>
    public Task WaitAsync(Guid runId)
    {
        lock (sync) return active is not null && active.RunId == runId && active.Task is not null ? active.Task : Task.CompletedTask;
    }

    private async Task ExecuteAsync(Guid runId, ActiveRun current)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AnimeFolderRefreshService>()
                .ExecuteAsync(runId, current.Cancellation.Token);
        }
        catch (Exception exception)
        {
            // A execucao fica "em andamento" no banco e sera marcada como interrompida na proxima consulta.
            logger.LogError(exception, "Falha ao concluir a atualizacao de status de pasta {RunId}.", runId);
        }
        finally
        {
            Release(current);
        }
    }

    private void Release(ActiveRun current)
    {
        lock (sync)
        {
            if (ReferenceEquals(active, current)) active = null;
        }
        current.Cancellation.Dispose();
    }

    private sealed class ActiveRun(CancellationTokenSource cancellation)
    {
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public Guid RunId { get; set; }
        public Task? Task { get; set; }
    }
}
