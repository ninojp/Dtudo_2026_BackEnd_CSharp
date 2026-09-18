using System.Collections.Concurrent;
using System.Threading.Channels;
using ApiFileStorage.Monitoring.Data;
using LibDtudo.Shared.Dtos.FileMonitoring;
using Microsoft.EntityFrameworkCore;

namespace ApiFileStorage.Monitoring;

public sealed class MonitoringSessions(IServiceScopeFactory scopeFactory, MonitoringRoots roots,
    MonitoringReader reader, ILogger<MonitoringSessions> logger, TimeProvider? timeProvider = null) : BackgroundService
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly ConcurrentDictionary<Guid, Session> sessions = new();
    private readonly SemaphoreSlim starts = new(1, 1);

    public async Task<MonitoringSessionDto> StartAsync(string owner, StartMonitoringRequest request, CancellationToken cancellationToken)
    {
        if (request.RelativePath is null || (request.RootKey is null && request.RelativePath.Length != 0)) throw new ArgumentException("Escopo geral nao aceita caminho relativo.");
        var targets = request.RootKey is null ? roots.All.Select(root => (root.Key, Path: "")).ToArray()
            : new[] { (Key: request.RootKey, Path: request.RelativePath) };
        foreach (var target in targets) roots.Resolve(target.Key, target.Path);
        await starts.WaitAsync(cancellationToken);
        try
        {
            if (sessions.Count >= 4) throw new InvalidOperationException("Limite de sessoes de monitoramento atingido.");
            foreach (var existing in sessions.Values)
                if (existing.Locations.Any(location => targets.Any(target => target.Key == location.RootKey
                    && Overlaps(target.Path, location.RelativePath))))
                    throw new InvalidOperationException("Este escopo ja esta sendo monitorado. Feche a outra tela de monitoramento antes de abrir esta.");
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
            var locations = new List<MonitoredLocation>();
            foreach (var target in targets)
            {
                var location = await context.Locations.FirstOrDefaultAsync(item => item.RootKey == target.Key && item.RelativePath == target.Path, cancellationToken);
                if (location is null)
                {
                    location = new MonitoredLocation { Id = Guid.NewGuid(), RootKey = target.Key, RelativePath = target.Path, RegisteredAtUtc = DateTimeOffset.UtcNow };
                    context.Locations.Add(location);
                }
                locations.Add(location);
            }
            await context.SaveChangesAsync(cancellationToken);
            var session = new Session(owner, locations) { ExpiresAt = clock.GetUtcNow().AddSeconds(45) };
            sessions[session.Id] = session;
            session.Work = Task.Run(() => RunSessionAsync(session));
            return View(session);
        }
        finally { starts.Release(); }
    }

    public MonitoringSessionDto Get(Guid id, string owner, bool renew = false)
    {
        var session = Find(id, owner);
        if (renew) session.ExpiresAt = clock.GetUtcNow().AddSeconds(45);
        return View(session);
    }

    public void Refresh(Guid id, string owner) => Interlocked.Exchange(ref Find(id, owner).RefreshRequested, 1);

    public Task StopAsync(Guid id, string owner)
    {
        var session = Find(id, owner);
        lock (session.StopLock)
            return session.StopTask ??= EndSessionAsync(session);
    }

    private async Task EndSessionAsync(Session session)
    {
        session.Cancellation.Cancel();
        await session.Work;
        sessions.TryRemove(session.Id, out _);
        session.Cancellation.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
                foreach (var session in sessions.Values.Where(session => session.ExpiresAt < clock.GetUtcNow()))
                {
                    try { await StopAsync(session.Id, session.Owner); }
                    catch (KeyNotFoundException) { }
                }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            foreach (var session in sessions.Values)
            {
                try { await StopAsync(session.Id, session.Owner); }
                catch (KeyNotFoundException) { }
            }
        }
    }

    private async Task RunSessionAsync(Session session)
    {
        var token = session.Cancellation.Token;
        var watchers = new Dictionary<Guid, FileSystemWatcher>();
        try
        {
            await PersistAsync(session.Locations.Select(location => Event(location.Id, "SESSION_STARTED", "", "Coleta iniciada; intervalos anteriores nao foram observados.")), token);
            while (!token.IsCancellationRequested)
            {
                if (Interlocked.Exchange(ref session.RefreshRequested, 0) == 1)
                {
                    session.State = "Inventariando";
                    session.InventoryOutdated = false;
                    foreach (var location in session.Locations)
                    {
                        token.ThrowIfCancellationRequested();
                        session.State = "Inventariando " + location.RootKey + "\\" + location.RelativePath;
                        TryWatch(location);
                        var result = reader.Read(location, token);
                        using var scope = scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
                        var store = scope.ServiceProvider.GetRequiredService<MonitoringInventoryStore>();
                        var previous = await store.GetLatestCompleteSnapshotAsync(location.Id, token);
                        var observations = result.Observations.ToList();
                        if (previous is not null && result.Snapshot.Completion == InventoryCompletion.Complete)
                        {
                            var previousEntries = await context.Entries.AsNoTracking().Where(entry => entry.SnapshotId == previous.Id).ToListAsync(token);
                            var before = previousEntries.ToDictionary(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase);
                            foreach (var entry in result.Snapshot.Entries)
                            {
                                if (!before.Remove(entry.RelativePath, out var old)) observations.Add(Difference(location.Id, "ADDED", entry.RelativePath));
                                else if (old.LengthBytes != entry.LengthBytes || old.LastWriteTimeUtc != entry.LastWriteTimeUtc || old.IsDirectory != entry.IsDirectory)
                                    observations.Add(Difference(location.Id, "CHANGED", entry.RelativePath));
                            }
                            observations.AddRange(before.Keys.Select(path => Difference(location.Id, "REMOVED", path)));
                        }
                        context.Observations.AddRange(observations);
                        await store.AppendSnapshotAsync(result.Snapshot, token);
                        var display = result.Snapshot.Completion == InventoryCompletion.Unavailable && previous is not null ? previous : result.Snapshot;
                        session.Views[location.Id] = new MonitoringLocationDto(location.Id, location.RootKey, location.RelativePath,
                            display.Id, result.Snapshot.Completion.ToString(), display.FileCount, display.DirectoryCount, display.TotalBytes, display.FinishedAtUtc);
                        var pendingEvents = new List<MonitoringObservation>();
                        while (pendingEvents.Count < 500 && session.Events.Reader.TryRead(out var pendingEvent)) pendingEvents.Add(pendingEvent);
                        if (pendingEvents.Count > 0)
                        {
                            session.InventoryOutdated = true;
                            await PersistAsync(pendingEvents, token);
                        }
                    }
                    session.State = "Monitorando";
                }
                var batch = new List<MonitoringObservation>();
                while (batch.Count < 500 && session.Events.Reader.TryRead(out var observation)) batch.Add(observation);
                if (Interlocked.Exchange(ref session.Overflow, 0) == 1)
                    batch.AddRange(session.Locations.Select(location => Event(location.Id, "EVENT_GAP", "", "Fila excedida. Algumas notificacoes foram perdidas; atualize o inventario.")));
                if (batch.Count > 0)
                {
                    session.InventoryOutdated = true;
                    await PersistAsync(batch, token);
                }
                if (DateTimeOffset.UtcNow >= session.NextAvailabilityCheck)
                {
                    session.NextAvailabilityCheck = DateTimeOffset.UtcNow.AddSeconds(15);
                    foreach (var location in session.Locations)
                    {
                        var path = roots.Resolve(location.RootKey, location.RelativePath);
                        try
                        {
                            MonitoringReader.EnsureNoRedirectedAncestors(path);
                            if (session.Unavailable.Remove(location.Id))
                                await PersistAsync([Event(location.Id, "AVAILABLE", "", "Localizacao acessivel novamente; atualize o inventario.")], token);
                            TryWatch(location);
                        }
                        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                        {
                            if (watchers.Remove(location.Id, out var watcher)) watcher.Dispose();
                            if (session.Unavailable.Add(location.Id))
                                await PersistAsync([Event(location.Id, "UNAVAILABLE", "", exception.Message)], token);
                            session.InventoryOutdated = true;
                        }
                    }
                }
                await Task.Delay(500, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sessao de monitoramento {SessionId} interrompida", session.Id);
            session.Error = "Coleta interrompida: " + exception.GetType().Name + ". Verifique a API e o banco de monitoramento.";
            session.State = "Falha";
        }
        finally
        {
            foreach (var watcher in watchers.Values) watcher.Dispose();
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var remaining = new List<MonitoringObservation>();
                while (session.Events.Reader.TryRead(out var observation)) remaining.Add(observation);
                if (Interlocked.Exchange(ref session.Overflow, 0) == 1)
                    remaining.AddRange(session.Locations.Select(location => Event(location.Id, "EVENT_GAP", "", "Notificacoes perdidas por excesso de eventos; inventario requer atualizacao.")));
                remaining.AddRange(session.Locations.Select(location => Event(location.Id, "SESSION_STOPPED", "", "Coleta encerrada. Fora da sessao nao ha observacao ativa.")));
                await PersistAsync(remaining, timeout.Token);
            }
            catch (Exception exception) { logger.LogWarning(exception, "Nao foi possivel persistir o encerramento de {SessionId}", session.Id); }
        }

        void TryWatch(MonitoredLocation location)
        {
            if (watchers.ContainsKey(location.Id)) return;
            try
            {
                var path = roots.Resolve(location.RootKey, location.RelativePath);
                MonitoringReader.EnsureNoRedirectedAncestors(path);
                var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = true, InternalBufferSize = 32 * 1024,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
                };
                watcher.Created += (_, args) => Capture(location, "CREATED", args.FullPath);
                watcher.Changed += (_, args) => Capture(location, "CHANGED", args.FullPath);
                watcher.Deleted += (_, args) => Capture(location, "DELETED", args.FullPath);
                watcher.Renamed += (_, args) => Capture(location, "RENAMED", args.FullPath, args.OldFullPath);
                watcher.Error += (_, _) => Interlocked.Exchange(ref session.Overflow, 1);
                watchers[location.Id] = watcher;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                if (watchers.Remove(location.Id, out var watcher)) watcher.Dispose();
                Enqueue(Event(location.Id, "WATCH_FAILED", "", exception.Message));
            }
        }

        void Capture(MonitoredLocation location, string code, string fullPath, string? oldPath = null)
        {
            var basePath = roots.Resolve(location.RootKey, location.RelativePath);
            var relative = Path.GetRelativePath(basePath, fullPath);
            if (relative.Split('\\').Any(MonitoringRoots.IsExcluded)) return;
            Enqueue(new MonitoringObservation
            {
                LocationId = location.Id, ObservedAtUtc = DateTimeOffset.UtcNow, Source = ObservationSource.LiveNotification,
                Code = code, RelativePath = relative, PreviousRelativePath = oldPath is null ? null : Path.GetRelativePath(basePath, oldPath),
                Detail = "Notificacao do sistema de arquivos; autoria nao coletada."
            });
        }
        void Enqueue(MonitoringObservation observation)
        {
            if (!session.Events.Writer.TryWrite(observation)) Interlocked.Exchange(ref session.Overflow, 1);
        }
    }

    private async Task PersistAsync(IEnumerable<MonitoringObservation> observations, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
        context.Observations.AddRange(observations);
        await context.SaveChangesAsync(cancellationToken);
    }

    private Session Find(Guid id, string owner) => sessions.TryGetValue(id, out var session) && session.Owner == owner
        ? session : throw new KeyNotFoundException("Sessao inexistente ou expirada.");
    private static bool Overlaps(string first, string second) => first.Length == 0 || second.Length == 0
        || first.Equals(second, StringComparison.OrdinalIgnoreCase) || first.StartsWith(second + "\\", StringComparison.OrdinalIgnoreCase)
        || second.StartsWith(first + "\\", StringComparison.OrdinalIgnoreCase);
    private static MonitoringObservation Event(Guid location, string code, string path, string detail) => new()
    {
        LocationId = location, Code = code, RelativePath = path, ObservedAtUtc = DateTimeOffset.UtcNow,
        Source = ObservationSource.Monitor, Detail = detail.Length > 2000 ? detail[..2000] : detail
    };
    private static MonitoringObservation Difference(Guid location, string code, string path)
    {
        var observation = Event(location, code, path, "Diferenca entre inventarios; horario exato da alteracao desconhecido.");
        observation.Source = ObservationSource.InventoryComparison;
        return observation;
    }
    private static MonitoringSessionDto View(Session session) => new(session.Id, session.State, session.Error,
        session.InventoryOutdated, session.Locations.Select(location => session.Views.TryGetValue(location.Id, out var view) ? view
            : new MonitoringLocationDto(location.Id, location.RootKey, location.RelativePath, null, "Pendente", 0, 0, 0, null)).ToArray());

    private sealed class Session(string owner, List<MonitoredLocation> locations)
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Owner { get; } = owner;
        public List<MonitoredLocation> Locations { get; } = locations;
        public CancellationTokenSource Cancellation { get; } = new();
        public Channel<MonitoringObservation> Events { get; } = Channel.CreateBounded<MonitoringObservation>(4096);
        public ConcurrentDictionary<Guid, MonitoringLocationDto> Views { get; } = new();
        public HashSet<Guid> Unavailable { get; } = [];
        public Task Work { get; set; } = Task.CompletedTask;
        private long expiresAtTicks = DateTimeOffset.UtcNow.AddSeconds(45).UtcTicks;
        public DateTimeOffset ExpiresAt
        {
            get => new(Interlocked.Read(ref expiresAtTicks), TimeSpan.Zero);
            set => Interlocked.Exchange(ref expiresAtTicks, value.UtcTicks);
        }
        public object StopLock { get; } = new();
        public Task? StopTask { get; set; }
        public DateTimeOffset NextAvailabilityCheck { get; set; }
        public string State = "Iniciando";
        public string? Error;
        public bool InventoryOutdated;
        public int RefreshRequested = 1;
        public int Overflow;
    }
}
