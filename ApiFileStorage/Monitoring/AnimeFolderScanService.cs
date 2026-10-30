using LibDtudo.Shared.Dtos.FileMonitoring;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Monitoring;

/// <summary>Limites da contagem sob demanda de videos na pasta de um anime (<c>Monitoring:AnimeFolderScan</c>).</summary>
public sealed class AnimeFolderScanOptions
{
    public const string SectionName = "Monitoring:AnimeFolderScan";

    /// <summary>Profundidade maxima de subpastas abaixo da pasta do anime (0 = somente a propria pasta).</summary>
    public int MaxDepth { get; set; } = 8;

    /// <summary>Maximo de arquivos examinados (videos ou nao) em uma varredura.</summary>
    public int MaxFiles { get; set; } = 20_000;

    /// <summary>Maximo de subpastas percorridas em uma varredura.</summary>
    public int MaxDirectories { get; set; } = 2_000;

    /// <summary>Tempo maximo de leitura; ao estourar, devolve o parcial com <c>truncated = true</c>.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Varreduras simultaneas; as demais aguardam vaga.</summary>
    public int MaxConcurrentScans { get; set; } = 2;

    public static bool IsValid(AnimeFolderScanOptions? options) => options is
    {
        MaxDepth: >= 0 and <= 64,
        MaxFiles: >= 1 and <= 1_000_000,
        MaxDirectories: >= 1 and <= 100_000,
        MaxConcurrentScans: >= 1 and <= 16
    } && options.Timeout > TimeSpan.Zero && options.Timeout <= TimeSpan.FromMinutes(2);
}

/// <summary>
/// Diz, sob demanda, se a pasta de um anime existe e quantos videos de episodio ela contem. Somente leitura, sem sessao
/// de monitoramento, sem banco e sem historico; nunca devolve caminho fisico.
/// </summary>
public sealed class AnimeFolderScanService : IDisposable
{
    /// <summary>Maior total de episodios aceito no pedido.</summary>
    public const int MaxExpectedEpisodes = 100_000;

    private static readonly EnumerationOptions Enumeration = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = false,
        AttributesToSkip = 0,
        ReturnSpecialDirectories = false
    };

    private readonly MonitoringRoots roots;
    private readonly AnimeFolderScanOptions options;
    private readonly ILogger<AnimeFolderScanService> logger;
    private readonly TimeProvider timeProvider;
    private readonly SemaphoreSlim gate;

    public AnimeFolderScanService(MonitoringRoots roots, IOptions<AnimeFolderScanOptions> options,
        ILogger<AnimeFolderScanService> logger, TimeProvider? timeProvider = null)
    {
        this.roots = roots;
        this.options = options.Value;
        this.logger = logger;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        if (!AnimeFolderScanOptions.IsValid(this.options)) throw new ArgumentException("Limites de varredura invalidos.", nameof(options));
        gate = new SemaphoreSlim(this.options.MaxConcurrentScans, this.options.MaxConcurrentScans);
    }

    /// <exception cref="ArgumentException">Pedido, chave de raiz ou caminho relativo invalido.</exception>
    /// <exception cref="OperationCanceledException">O chamador cancelou; o estouro do tempo limite nao lanca.</exception>
    public async Task<AnimeFolderScanResponse> ScanAsync(AnimeFolderScanRequest? request, CancellationToken cancellationToken)
    {
        var (path, expected) = Validate(request);
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => Scan(request!, path, expected, cancellationToken), cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Lista as pastas de anime (subpastas de 1o nivel) de uma pasta de colecao com os MalIds das capas numericas do
    /// 1o nivel de cada uma. Somente leitura, mesma disciplina de limites da varredura e nenhum caminho fisico.
    /// </summary>
    /// <exception cref="ArgumentException">Pedido, chave de raiz ou caminho relativo invalido.</exception>
    /// <exception cref="OperationCanceledException">O chamador cancelou; o estouro do tempo limite nao lanca.</exception>
    public async Task<AnimeFolderDiscoveryResponse> DiscoverAsync(AnimeFolderDiscoveryRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentException("Corpo da requisicao obrigatorio.");
        if (string.IsNullOrWhiteSpace(request.RootKey)) throw new ArgumentException("Raiz de monitoramento obrigatoria.");
        if (string.IsNullOrEmpty(request.RelativePath)) throw new ArgumentException("Caminho relativo da pasta da colecao obrigatorio.");
        string path;
        try
        {
            path = roots.Resolve(request.RootKey, request.RelativePath);
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException)
        {
            throw new ArgumentException("Caminho relativo invalido.");
        }
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => Discover(request, path, cancellationToken), cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();

    private AnimeFolderDiscoveryResponse Discover(AnimeFolderDiscoveryRequest request, string path, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(options.Timeout, timeProvider);
        try
        {
            MonitoringReader.EnsureNoRedirectedAncestors(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogInformation("Pasta de colecao indisponivel em {RootKey}/{RelativePath}: {Reason}",
                request.RootKey, request.RelativePath, exception.GetType().Name);
            return new AnimeFolderDiscoveryResponse(false, [], false);
        }

        var folders = new List<AnimeFolderCandidateDto>();
        var truncated = false;
        var files = 0;
        var subfolders = new List<DirectoryInfo>();
        try
        {
            using var lease = MonitoringDirectoryLease.Open(path);
            var collectionCovers = new SortedSet<int>();
            foreach (var item in new DirectoryInfo(path).EnumerateFileSystemInfos("*", Enumeration))
            {
                if (Stopped()) break;
                if (MonitoringRoots.IsExcluded(item.Name)
                    || (item.Attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0) continue;
                if ((item.Attributes & FileAttributes.Directory) != 0)
                {
                    if (subfolders.Count >= options.MaxDirectories)
                    {
                        truncated = true;
                        continue;
                    }
                    subfolders.Add((DirectoryInfo)item);
                    continue;
                }
                if (TryReadCoverId(item.Name, out var coverId)) collectionCovers.Add(coverId);
            }
            if (collectionCovers.Count > 0)
                folders.Add(new AnimeFolderCandidateDto(request.RelativePath, [.. collectionCovers]));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogInformation("Pasta de colecao ilegivel em {RootKey}/{RelativePath}: {Reason}",
                request.RootKey, request.RelativePath, exception.GetType().Name);
            return new AnimeFolderDiscoveryResponse(false, [], false);
        }

        foreach (var subfolder in subfolders)
        {
            if (Stopped()) break;
            var relativePath = request.RelativePath.TrimEnd('\\') + "\\" + subfolder.Name;
            if (!MonitoringPathPolicy.IsValidRelativePath(relativePath)) continue;
            var covers = new SortedSet<int>();
            try
            {
                using var lease = MonitoringDirectoryLease.Open(subfolder.FullName);
                foreach (var item in subfolder.EnumerateFileSystemInfos("*", Enumeration))
                {
                    if (Stopped()) break;
                    if ((item.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.System)) != 0) continue;
                    if (++files > options.MaxFiles)
                    {
                        truncated = true;
                        break;
                    }
                    if (TryReadCoverId(item.Name, out var coverId)) covers.Add(coverId);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                truncated = true;
                logger.LogWarning("Subpasta ilegivel na descoberta de {RootKey}/{RelativePath}: {Reason}",
                    request.RootKey, request.RelativePath, exception.GetType().Name);
            }
            folders.Add(new AnimeFolderCandidateDto(relativePath, [.. covers]));
            if (files > options.MaxFiles) break;
        }

        return new AnimeFolderDiscoveryResponse(true, [.. folders], truncated);

        bool Stopped()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!timeout.IsCancellationRequested) return false;
            truncated = true;
            return true;
        }
    }

    private static bool TryReadCoverId(string fileName, out int coverId)
    {
        coverId = 0;
        return CollectionSyncDiscoveryService.ImageExtensions.Contains(Path.GetExtension(fileName))
            && int.TryParse(Path.GetFileNameWithoutExtension(fileName), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out coverId)
            && coverId > 0;
    }

    private (string Path, int? Expected) Validate(AnimeFolderScanRequest? request)
    {
        if (request is null) throw new ArgumentException("Corpo da requisicao obrigatorio.");
        if (string.IsNullOrWhiteSpace(request.RootKey)) throw new ArgumentException("Raiz de monitoramento obrigatoria.");
        if (string.IsNullOrEmpty(request.RelativePath)) throw new ArgumentException("Caminho relativo da pasta do anime obrigatorio.");
        if (request.ExpectedEpisodes is < 0 or > MaxExpectedEpisodes) throw new ArgumentException("Total de episodios invalido.");
        string path;
        try
        {
            path = roots.Resolve(request.RootKey, request.RelativePath);
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException)
        {
            throw new ArgumentException("Caminho relativo invalido.");
        }
        return (path, request.ExpectedEpisodes is > 0 ? request.ExpectedEpisodes : null);
    }

    private AnimeFolderScanResponse Scan(AnimeFolderScanRequest request, string path, int? expected, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(options.Timeout, timeProvider);
        try
        {
            MonitoringReader.EnsureNoRedirectedAncestors(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return NotFound(request, exception);
        }

        int videos = 0, directories = 0, files = 0;
        var truncated = false;
        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((path, 0));
        while (pending.TryPop(out var current))
        {
            if (Stopped()) break;
            try
            {
                using var lease = MonitoringDirectoryLease.Open(current.Path);
                foreach (var item in new DirectoryInfo(current.Path).EnumerateFileSystemInfos("*", Enumeration))
                {
                    if (Stopped()) break;
                    if (MonitoringRoots.IsExcluded(item.Name)
                        || (item.Attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0) continue;
                    if ((item.Attributes & FileAttributes.Directory) != 0)
                    {
                        if (EpisodeVideoPolicy.IsExtraDirectoryName(item.Name)) continue;
                        if (current.Depth >= options.MaxDepth || directories >= options.MaxDirectories)
                        {
                            truncated = true;
                            continue;
                        }
                        directories++;
                        pending.Push((item.FullName, current.Depth + 1));
                        continue;
                    }
                    if (++files > options.MaxFiles)
                    {
                        truncated = true;
                        break;
                    }
                    if (EpisodeVideoPolicy.IsEpisodeVideoFile(item.Name, ((FileInfo)item).Length)) videos++;
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                if (current.Depth == 0) return NotFound(request, exception);
                truncated = true;
                logger.LogWarning("Subpasta ilegivel na varredura de {RootKey}/{RelativePath}: {Reason}",
                    request.RootKey, request.RelativePath, exception.GetType().Name);
            }
            if (files > options.MaxFiles) break;
        }

        return new AnimeFolderScanResponse(true, videos, directories,
            AnimeFolderState.Evaluate(exists: true, videos, expected, truncated), truncated);

        bool Stopped()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!timeout.IsCancellationRequested) return false;
            truncated = true;
            return true;
        }
    }

    private AnimeFolderScanResponse NotFound(AnimeFolderScanRequest request, Exception exception)
    {
        logger.LogInformation("Pasta de anime indisponivel em {RootKey}/{RelativePath}: {Reason}",
            request.RootKey, request.RelativePath, exception.GetType().Name);
        return new AnimeFolderScanResponse(false, 0, 0, AnimeFolderState.None, false);
    }
}
