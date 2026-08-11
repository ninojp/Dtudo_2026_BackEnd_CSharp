using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinAppDtudo.Services;

public interface IApiMusicXCollectionImporter
{
    Task<ApiMusicXImportCollectionResponse> ImportarColecaoAsync(
        ApiMusicXImportCollectionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record LegacyMusicMigrationProgress(
    string Stage,
    int Current,
    int Total,
    string Message)
{
    public int Percentual => Total <= 0
        ? 0
        : Math.Clamp((int)Math.Round(Current / (double)Total * 100, MidpointRounding.AwayFromZero), 0, 100);
}

public sealed class LegacyMusicMigrationSummary
{
    public bool DryRun { get; init; }

    public bool Cancelada { get; internal set; }

    public int Lidos { get; internal set; }

    public int Importados { get; internal set; }

    public int Atualizados { get; internal set; }

    public int Ignorados { get; internal set; }

    public int Falhos { get; internal set; }

    public int Simulados { get; internal set; }

    public List<string> Erros { get; } = [];
}

public sealed class LegacyMusicMigrationItemResult
{
    internal LegacyMusicMigrationItemResult(
        string legacyKey,
        ApiMusicXImportCollectionRequest? request,
        IEnumerable<string> warnings,
        IEnumerable<string> errors)
    {
        LegacyKey = legacyKey;
        Request = request;
        Warnings = warnings.ToList();
        Errors = errors.ToList();
    }

    public string LegacyKey { get; }

    public ApiMusicXImportCollectionRequest? Request { get; }

    public IReadOnlyList<string> Warnings { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsDuplicate { get; internal set; }

    public bool IsSimulated { get; internal set; }

    public ApiMusicXImportCollectionResponse? ApiResponse { get; internal set; }
}

public sealed class LegacyMusicMigrationResult
{
    internal LegacyMusicMigrationResult(
        LegacyMusicMigrationSummary summary,
        IReadOnlyList<LegacyMusicMigrationItemResult> items)
    {
        Summary = summary;
        Items = items;
    }

    public LegacyMusicMigrationSummary Summary { get; }

    public IReadOnlyList<LegacyMusicMigrationItemResult> Items { get; }
}

public sealed class LegacyMusicMigrationException : Exception
{
    public LegacyMusicMigrationException(string message)
        : base(message)
    {
    }

    public LegacyMusicMigrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class LegacyMusicCollectionMigrationService
{
    private const string LegacyProvider = "ApiNode.MyMusicX";
    private const string LegacyCollectionResourceType = "Collection";
    private const string DiscogsProvider = "Discogs";
    private const int MaxApiCollectionReleases = 1000;
    private const int MaxApiReleaseReferences = 1000;
    private const int MaxApiReleaseTracks = 1000;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly IApiMusicXCollectionImporter? _apiMusicXImporter;

    public LegacyMusicCollectionMigrationService(IApiMusicXCollectionImporter? apiMusicXImporter = null)
    {
        _apiMusicXImporter = apiMusicXImporter;
    }

    public async Task<IReadOnlyList<LegacyMusicMigrationItemResult>> LerEPrepararAsync(
        string filePath,
        IProgress<LegacyMusicMigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Report(progress, "validacao", 0, 0, "Validando o arquivo legado.");
        var json = await ReadLegacyJsonAsync(filePath, cancellationToken);
        Report(progress, "leitura", 0, 0, "Arquivo UTF-8 validado e carregado em memoria.");

        using var document = ParseJson(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !TryGetProperty(document.RootElement, out var recordsElement, "mymusicx")
            || recordsElement.ValueKind != JsonValueKind.Array)
        {
            throw new LegacyMusicMigrationException(
                "O JSON legado precisa conter a propriedade 'mymusicx' como uma lista.");
        }

        var recordElements = recordsElement.EnumerateArray().ToList();
        var results = new List<LegacyMusicMigrationItemResult>(recordElements.Count);
        for (var index = 0; index < recordElements.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Report(
                progress,
                "normalizacao",
                index + 1,
                recordElements.Count,
                $"Normalizando registro {index + 1}/{recordElements.Count}.");
            results.Add(NormalizeRecord(recordElements[index], index));
        }

        Report(
            progress,
            "normalizacao",
            recordElements.Count,
            recordElements.Count,
            $"Normalizacao concluida: {results.Count} registro(s) lido(s).");
        return results;
    }

    public async Task<LegacyMusicMigrationResult> ExecutarAsync(
        string filePath,
        bool dryRun,
        IProgress<LegacyMusicMigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var items = await LerEPrepararAsync(filePath, progress, cancellationToken);
        var summary = new LegacyMusicMigrationSummary
        {
            DryRun = dryRun,
            Lidos = items.Count
        };
        var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < items.Count; index++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = items[index];
                Report(
                    progress,
                    dryRun ? "dry-run" : "persistencia",
                    index + 1,
                    items.Count,
                    $"Processando Colecao {index + 1}/{items.Count}: {item.LegacyKey}.");

                if (!processedKeys.Add(item.LegacyKey))
                {
                    item.IsDuplicate = true;
                    summary.Ignorados++;
                    Report(
                        progress,
                        "duplicidade",
                        index + 1,
                        items.Count,
                        $"Registro duplicado ignorado: {item.LegacyKey}.");
                    continue;
                }

                foreach (var warning in item.Warnings)
                {
                    Report(
                        progress,
                        "aviso",
                        index + 1,
                        items.Count,
                        $"{item.LegacyKey}: {warning}");
                }

                AddItemErrorsToSummary(item, summary);
                foreach (var error in item.Errors)
                {
                    Report(
                        progress,
                        "falha",
                        index + 1,
                        items.Count,
                        $"{item.LegacyKey}: {error}");
                }
                if (item.Request is null)
                {
                    summary.Falhos++;
                    Report(
                        progress,
                        "falha",
                        index + 1,
                        items.Count,
                        $"Registro {item.LegacyKey} nao possui dados suficientes para importacao.");
                    continue;
                }

                if (dryRun)
                {
                    item.IsSimulated = true;
                    summary.Simulados++;
                    Report(
                        progress,
                        "dry-run",
                        index + 1,
                        items.Count,
                        $"Dry-run validado para {item.LegacyKey}; nenhuma requisicao foi enviada.");
                    continue;
                }

                if (_apiMusicXImporter is null)
                {
                    throw new InvalidOperationException(
                        "A importacao real precisa de um cliente da ApiMusicX.");
                }

                var apiProgress = new Progress<string>(message =>
                    Report(
                        progress,
                        "persistencia",
                        index + 1,
                        items.Count,
                        message));
                var response = await _apiMusicXImporter.ImportarColecaoAsync(
                    item.Request,
                    apiProgress,
                    cancellationToken);
                item.ApiResponse = response;

                if (response.Created)
                {
                    summary.Importados++;
                }
                else if (response.Changed)
                {
                    summary.Atualizados++;
                }
                else
                {
                    summary.Ignorados++;
                }

                Report(
                    progress,
                    "persistencia",
                    index + 1,
                    items.Count,
                    response.Created
                        ? $"Colecao {item.LegacyKey} importada."
                        : response.Changed
                            ? $"Colecao {item.LegacyKey} atualizada sem substituir dados locais."
                            : $"Colecao {item.LegacyKey} ja estava atualizada; importacao idempotente.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                summary.Cancelada = true;
                Report(
                    progress,
                    "cancelamento",
                    index,
                    items.Count,
                    "Migracao cancelada pelo operador; os itens ja concluidos foram preservados.");
                break;
            }
            catch (Exception exception)
            {
                summary.Falhos++;
                var item = items[index];
                var message = $"Falha na Colecao {item.LegacyKey}: {exception.Message}";
                summary.Erros.Add(message);
                Report(progress, "falha", index + 1, items.Count, message);
            }
        }

        Report(
            progress,
            "resumo",
            items.Count,
            items.Count,
            $"Resumo: lidos={summary.Lidos}, importados={summary.Importados}, " +
            $"atualizados={summary.Atualizados}, ignorados={summary.Ignorados}, " +
            $"falhos={summary.Falhos}, simulados={summary.Simulados}.");
        return new LegacyMusicMigrationResult(summary, items);
    }

    private static async Task<string> ReadLegacyJsonAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new LegacyMusicMigrationException("Informe o caminho do JSON legado.");
        }

        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(filePath);
        }
        catch (FileNotFoundException exception)
        {
            throw new LegacyMusicMigrationException("O arquivo JSON legado nao foi encontrado.", exception);
        }
        catch (DirectoryNotFoundException exception)
        {
            throw new LegacyMusicMigrationException("A pasta do JSON legado nao foi encontrada.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new LegacyMusicMigrationException("O WinAppDtudo nao tem permissao para ler o JSON legado.", exception);
        }
        catch (IOException exception)
        {
            throw new LegacyMusicMigrationException("Nao foi possivel validar o arquivo JSON legado.", exception);
        }

        if ((attributes & FileAttributes.Directory) != 0)
        {
            throw new LegacyMusicMigrationException("O caminho informado e uma pasta, nao um arquivo JSON.");
        }

        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new LegacyMusicMigrationException("O WinAppDtudo nao tem permissao para ler o JSON legado.", exception);
        }
        catch (IOException exception)
        {
            throw new LegacyMusicMigrationException("Nao foi possivel ler o arquivo JSON legado.", exception);
        }

        if (bytes.Length == 0)
        {
            throw new LegacyMusicMigrationException("O arquivo JSON legado esta vazio.");
        }

        string json;
        try
        {
            json = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new LegacyMusicMigrationException(
                "O arquivo JSON legado nao esta em UTF-8 valido.",
                exception);
        }

        return json.Length > 0 && json[0] == '\uFEFF'
            ? json[1..]
            : json;
    }

    private static JsonDocument ParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
        }
        catch (JsonException exception)
        {
            throw new LegacyMusicMigrationException(
                "O arquivo JSON legado possui estrutura invalida.",
                exception);
        }
    }

    private static LegacyMusicMigrationItemResult NormalizeRecord(JsonElement record, int index)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        if (record.ValueKind != JsonValueKind.Object)
        {
            var invalidKey = $"registro-{index + 1}";
            errors.Add($"O item {index + 1} do JSON nao e um objeto.");
            return new LegacyMusicMigrationItemResult(invalidKey, null, warnings, errors);
        }

        var sourceKey = ReadString(record, warnings, errors, "id");
        var artistName = ReadString(record, warnings, errors, "artista", "artist");
        var legacyKey = CreateLegacyKey(record, sourceKey, warnings, errors);
        if (string.IsNullOrWhiteSpace(artistName))
        {
            errors.Add($"A Colecao {legacyKey} nao possui o campo artista.");
            return new LegacyMusicMigrationItemResult(legacyKey, null, warnings, errors);
        }

        var artistType = ReadArtistType(record, warnings);

        var releaseIdentities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var releases = new List<ApiMusicXReleaseImportRequest>();
        if (TryGetProperty(record, out var releasesElement, "releases"))
        {
            if (releasesElement.ValueKind != JsonValueKind.Object)
            {
                warnings.Add($"A propriedade releases da Colecao {legacyKey} nao e um objeto; foi ignorada.");
            }
            else
            {
                foreach (var category in ReleaseCategories)
                {
                    if (releases.Count >= MaxApiCollectionReleases)
                    {
                        warnings.Add(
                            $"A Colecao {legacyKey} atingiu o limite de {MaxApiCollectionReleases} releases da ApiMusicX; os demais foram ignorados.");
                        break;
                    }

                    if (!TryGetProperty(releasesElement, out var categoryElement, category.LegacyName))
                    {
                        continue;
                    }

                    if (categoryElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    {
                        continue;
                    }

                    if (categoryElement.ValueKind != JsonValueKind.Array)
                    {
                        warnings.Add(
                            $"A lista {category.LegacyName} da Colecao {legacyKey} nao e um array; foi ignorada.");
                        continue;
                    }

                    var displayOrder = releases.Count;
                    foreach (var releaseElement in categoryElement.EnumerateArray())
                    {
                        var release = NormalizeRelease(
                            releaseElement,
                            legacyKey,
                            category,
                            displayOrder,
                            releaseIdentities,
                            warnings,
                            errors);
                        if (release is null)
                        {
                            continue;
                        }

                        releases.Add(release);
                        displayOrder++;
                        if (releases.Count >= MaxApiCollectionReleases)
                        {
                            break;
                        }
                    }

                    if (releases.Count >= MaxApiCollectionReleases)
                    {
                        warnings.Add(
                            $"A Colecao {legacyKey} atingiu o limite de {MaxApiCollectionReleases} releases da ApiMusicX; os demais foram ignorados.");
                        break;
                    }
                }
            }
        }

        if (releases.Count == 0)
        {
            warnings.Add($"A Colecao {legacyKey} nao possui releases validos para importar.");
        }

        var request = new ApiMusicXImportCollectionRequest
        {
            DisplayName = artistName,
            ExternalIdentifiers =
            [
                CreateIdentifier(LegacyProvider, LegacyCollectionResourceType, legacyKey)
            ],
            Artists =
            [
                new ApiMusicXArtistImportRequest
                {
                    DisplayName = artistName,
                    ArtistType = artistType,
                    CollectionRole = ApiMusicXCollectionArtistRole.Primary
                }
            ],
            Releases = releases
        };

        return new LegacyMusicMigrationItemResult(legacyKey, request, warnings, errors);
    }

    private static ApiMusicXReleaseImportRequest? NormalizeRelease(
        JsonElement releaseElement,
        string legacyKey,
        ReleaseCategory category,
        int displayOrder,
        HashSet<string> releaseIdentities,
        List<string> warnings,
        List<string> errors)
    {
        if (releaseElement.ValueKind != JsonValueKind.Object)
        {
            warnings.Add($"Um item de {category.LegacyName} da Colecao {legacyKey} nao e um objeto e foi ignorado.");
            return null;
        }

        var title = ReadString(releaseElement, warnings, errors, "titulo", "title");
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add($"Um release da Colecao {legacyKey} nao possui titulo e foi ignorado.");
            return null;
        }

        var discogsId = ReadString(releaseElement, warnings, errors, "discogs_id", "discogsId");
        var year = ReadYear(releaseElement, legacyKey, title, warnings);
        var identity = BuildReleaseIdentity(category.LegacyName, title, discogsId, year);
        if (!releaseIdentities.Add(identity))
        {
            warnings.Add($"Release duplicado ignorado na Colecao {legacyKey}: {title}.");
            return null;
        }

        var externalIdentifiers = new List<ApiMusicXExternalIdentifierRequest>();
        if (!string.IsNullOrWhiteSpace(discogsId))
        {
            externalIdentifiers.Add(CreateIdentifier(DiscogsProvider, "Release", discogsId));
        }

        var tracks = ReadTracks(releaseElement, legacyKey, title, warnings, errors);
        var localFileReferences = ReadLocalFileReferences(
            releaseElement,
            legacyKey,
            title,
            warnings);
        if (tracks.Count > MaxApiReleaseTracks)
        {
            warnings.Add(
                $"O release {title} excedeu o limite de {MaxApiReleaseTracks} faixas; as excedentes foram ignoradas.");
            tracks = tracks.Take(MaxApiReleaseTracks).ToList();
        }

        if (localFileReferences.Count > MaxApiReleaseReferences)
        {
            warnings.Add(
                $"O release {title} excedeu o limite de {MaxApiReleaseReferences} referencias locais; as excedentes foram ignoradas.");
            localFileReferences = localFileReferences.Take(MaxApiReleaseReferences).ToList();
        }

        return new ApiMusicXReleaseImportRequest
        {
            Title = title,
            ReleaseType = category.ReleaseType,
            ReleaseYear = year,
            SourceCategory = category.LegacyName,
            DisplayOrder = displayOrder,
            ExternalIdentifiers = externalIdentifiers,
            ArtistCredits = ReadArtistCredits(releaseElement, warnings),
            Tracks = tracks,
            LocalFileReferences = localFileReferences
        };
    }

    private static List<ApiMusicXTrackImportRequest> ReadTracks(
        JsonElement release,
        string legacyKey,
        string releaseTitle,
        List<string> warnings,
        List<string> errors)
    {
        if (!TryGetProperty(release, out var tracksElement, "tracks", "faixas", "tracklist", "trackList"))
        {
            return [];
        }

        if (tracksElement.ValueKind != JsonValueKind.Array)
        {
            warnings.Add($"As faixas do release {releaseTitle} da Colecao {legacyKey} nao sao uma lista; foram ignoradas.");
            return [];
        }

        var tracks = new List<ApiMusicXTrackImportRequest>();
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sequence = 0;
        foreach (var trackElement in tracksElement.EnumerateArray())
        {
            if (trackElement.ValueKind != JsonValueKind.Object)
            {
                warnings.Add($"Uma faixa do release {releaseTitle} nao e um objeto e foi ignorada.");
                continue;
            }

            var title = ReadString(trackElement, warnings, errors, "titulo", "title", "nome");
            if (string.IsNullOrWhiteSpace(title))
            {
                errors.Add($"Uma faixa do release {releaseTitle} nao possui titulo e foi ignorada.");
                continue;
            }

            var positionLabel = ReadString(
                trackElement,
                warnings,
                errors,
                "positionLabel",
                "posicao",
                "position",
                "lado");
            var trackSequence = ReadInteger(
                trackElement,
                warnings,
                "sequence",
                "sequencia",
                "numero",
                "trackNumber");
            var durationText = ReadString(trackElement, warnings, errors, "durationText", "duracao", "duration");
            var durationSeconds = ReadInteger(trackElement, warnings, "durationSeconds", "duracaoSegundos");
            var identity = BuildTrackIdentity(title, positionLabel, trackSequence);
            if (!identities.Add(identity))
            {
                warnings.Add($"Faixa duplicada ignorada no release {releaseTitle}: {title}.");
                continue;
            }

            tracks.Add(new ApiMusicXTrackImportRequest
            {
                Title = title,
                PositionLabel = positionLabel,
                Sequence = trackSequence ?? sequence,
                DurationSeconds = durationSeconds,
                DurationText = durationText,
                ArtistCredits = ReadArtistCredits(trackElement, warnings),
                LocalFileReferences = ReadLocalFileReferences(
                    trackElement,
                    legacyKey,
                    $"faixa {title}",
                    warnings)
            });
            sequence++;
        }

        return tracks;
    }

    private static List<ApiMusicXArtistCreditImportRequest> ReadArtistCredits(
        JsonElement element,
        List<string> warnings)
    {
        if (!TryGetProperty(element, out var artistsElement, "artistCredits", "creditos", "artists", "artistas"))
        {
            return [];
        }

        var creditElements = artistsElement.ValueKind == JsonValueKind.Array
            ? artistsElement.EnumerateArray().ToList()
            : artistsElement.ValueKind == JsonValueKind.String
                ? [artistsElement]
                : [];
        if (creditElements.Count == 0)
        {
            if (artistsElement.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
            {
                warnings.Add("Uma lista de artistas participantes possui formato invalido e foi ignorada.");
            }

            return [];
        }

        var credits = new List<ApiMusicXArtistCreditImportRequest>();
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var creditElement in creditElements)
        {
            string? name;
            string? discogsId = null;
            ApiMusicXCreditRole role = ApiMusicXCreditRole.Unknown;
            if (creditElement.ValueKind == JsonValueKind.String)
            {
                name = creditElement.GetString()?.Trim();
            }
            else if (creditElement.ValueKind == JsonValueKind.Object)
            {
                var localWarnings = new List<string>();
                name = ReadString(creditElement, localWarnings, localWarnings, "nome", "name", "artista", "artist");
                discogsId = ReadString(creditElement, localWarnings, localWarnings, "discogs_id", "discogsId");
                role = ParseCreditRole(ReadString(creditElement, localWarnings, localWarnings, "role", "papel"));
            }
            else
            {
                warnings.Add("Um credito de artista possui formato invalido e foi ignorado.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(discogsId))
            {
                warnings.Add("Um credito de artista nao possui nome ou ID Discogs e foi ignorado.");
                continue;
            }

            var identity = !string.IsNullOrWhiteSpace(discogsId)
                ? $"discogs:{discogsId}"
                : $"name:{NormalizeForIdentity(name!)}";
            if (!identities.Add(identity))
            {
                warnings.Add($"Credito de artista duplicado ignorado: {name ?? discogsId}.");
                continue;
            }

            var identifiers = string.IsNullOrWhiteSpace(discogsId)
                ? []
                : new List<ApiMusicXExternalIdentifierRequest>
                {
                    CreateIdentifier(DiscogsProvider, "Artist", discogsId)
                };
            credits.Add(new ApiMusicXArtistCreditImportRequest
            {
                DisplayName = name,
                ArtistType = ApiMusicXArtistType.Unknown,
                ExternalIdentifiers = identifiers,
                Role = role
            });
        }

        return credits;
    }

    private static List<ApiMusicXLocalFileReferenceImportRequest> ReadLocalFileReferences(
        JsonElement element,
        string legacyKey,
        string itemDescription,
        List<string> warnings)
    {
        if (!TryGetProperty(element, out var pathsElement, "arquivosLocais", "localFiles", "localFileReferences"))
        {
            return [];
        }

        if (pathsElement.ValueKind != JsonValueKind.Array)
        {
            warnings.Add($"As referencias locais de {itemDescription} da Colecao {legacyKey} nao sao uma lista; foram ignoradas.");
            return [];
        }

        var references = new List<ApiMusicXLocalFileReferenceImportRequest>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pathElement in pathsElement.EnumerateArray())
        {
            var path = pathElement.ValueKind == JsonValueKind.String
                ? pathElement.GetString()
                : pathElement.ValueKind == JsonValueKind.Object
                    ? ReadPathObject(pathElement)
                    : null;
            if (string.IsNullOrWhiteSpace(path))
            {
                warnings.Add($"Uma referencia local de {itemDescription} e invalida e foi ignorada.");
                continue;
            }

            string normalizedPath;
            try
            {
                normalizedPath = NormalizeRelativePath(path);
            }
            catch (ArgumentException exception)
            {
                warnings.Add($"Referencia local ignorada em {itemDescription}: {exception.Message}");
                continue;
            }

            if (!paths.Add(normalizedPath))
            {
                warnings.Add($"Referencia local duplicada ignorada em {itemDescription}: {normalizedPath}.");
                continue;
            }

            var media = ClassifyPath(normalizedPath);
            references.Add(new ApiMusicXLocalFileReferenceImportRequest
            {
                RelativePath = normalizedPath,
                MediaKind = media.Kind,
                Role = media.Role
            });
        }

        return references;
    }

    private static string? ReadPathObject(JsonElement element)
    {
        if (!TryGetProperty(element, out var pathElement, "relativePath", "path", "caminho"))
        {
            return null;
        }

        return pathElement.ValueKind == JsonValueKind.String
            ? pathElement.GetString()
            : null;
    }

    private static int? ReadYear(
        JsonElement release,
        string legacyKey,
        string title,
        List<string> warnings)
    {
        var value = ReadScalarText(release, "ano", "year");
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
            && year is >= 1000 and <= 9999)
        {
            return year;
        }

        warnings.Add($"Ano invalido ignorado no release {title} da Colecao {legacyKey}: {value}.");
        return null;
    }

    private static string? ReadScalarText(JsonElement parent, params string[] propertyNames)
    {
        if (!TryGetProperty(parent, out var value, propertyNames))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => null
        };
    }

    private static int? ReadInteger(
        JsonElement parent,
        List<string> warnings,
        params string[] propertyNames)
    {
        var value = ReadScalarText(parent, propertyNames);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            && number >= 0)
        {
            return number;
        }

        warnings.Add($"Valor numerico invalido ignorado: {value}.");
        return null;
    }

    private static string? ReadString(
        JsonElement parent,
        List<string> warnings,
        List<string> errors,
        params string[] propertyNames)
    {
        if (!TryGetProperty(parent, out var value, propertyNames))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => AddInvalidStringIssue(value, propertyNames[0], errors)
        };
    }

    private static string? AddInvalidStringIssue(
        JsonElement value,
        string propertyName,
        List<string> errors)
    {
        errors.Add($"O campo {propertyName} possui tipo JSON invalido ({value.ValueKind}).");
        return null;
    }

    private static ApiMusicXArtistType ReadArtistType(
        JsonElement record,
        List<string> warnings)
    {
        var value = ReadScalarText(record, "tipoArtista", "artistType", "tipo");
        if (string.IsNullOrWhiteSpace(value))
        {
            return ApiMusicXArtistType.Unknown;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "solo" or "solista" => ApiMusicXArtistType.Solo,
            "band" or "banda" => ApiMusicXArtistType.Band,
            "group" or "grupo" => ApiMusicXArtistType.Group,
            _ => WarnUnknownArtistType(value, warnings)
        };
    }

    private static ApiMusicXArtistType WarnUnknownArtistType(
        string value,
        List<string> warnings)
    {
        warnings.Add($"Tipo de artista desconhecido ignorado: {value}.");
        return ApiMusicXArtistType.Unknown;
    }

    private static ApiMusicXCreditRole ParseCreditRole(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "primary" or "principal" => ApiMusicXCreditRole.Primary,
            "featured" or "participacao" or "participacao especial" => ApiMusicXCreditRole.Featured,
            "composer" or "compositor" => ApiMusicXCreditRole.Composer,
            _ => ApiMusicXCreditRole.Unknown
        };
    }

    private static string CreateLegacyKey(
        JsonElement record,
        string? sourceKey,
        List<string> warnings,
        List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(sourceKey)
            && !sourceKey.Any(char.IsControl))
        {
            return sourceKey.Trim();
        }

        if (!string.IsNullOrWhiteSpace(sourceKey))
        {
            errors.Add("A chave legada possui caracteres de controle e nao pode ser preservada.");
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(record.GetRawText()));
        var generatedKey = "generated-" + Convert.ToHexString(hash)[..24].ToLowerInvariant();
        warnings.Add(
            $"Registro sem chave legada valida; foi criada a chave deterministica {generatedKey} para idempotencia.");
        return generatedKey;
    }

    private static string BuildReleaseIdentity(
        string category,
        string title,
        string? discogsId,
        int? year)
        => !string.IsNullOrWhiteSpace(discogsId)
            ? $"discogs:{discogsId}"
            : $"legacy:{category}:{NormalizeForIdentity(title)}:{year?.ToString(CultureInfo.InvariantCulture) ?? ""}";

    private static string BuildTrackIdentity(
        string title,
        string? positionLabel,
        int? sequence)
        => $"{NormalizeForIdentity(title)}:{positionLabel?.Trim().ToLowerInvariant() ?? ""}:{sequence?.ToString(CultureInfo.InvariantCulture) ?? ""}";

    private static string NormalizeForIdentity(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWhitespace = false;
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWhitespace)
                {
                    builder.Append(' ');
                    previousWhitespace = true;
                }

                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
            previousWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private static string NormalizeRelativePath(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0
            || trimmed.StartsWith("/", StringComparison.Ordinal)
            || trimmed.StartsWith("\\", StringComparison.Ordinal)
            || trimmed.Length >= 2 && char.IsAsciiLetter(trimmed[0]) && trimmed[1] == ':')
        {
            throw new ArgumentException("o caminho nao e relativo");
        }

        var segments = trimmed
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("o caminho possui navegacao de pasta");
        }

        if (segments.Any(segment => segment.Any(char.IsControl)))
        {
            throw new ArgumentException("o caminho possui caracteres de controle");
        }

        return string.Join('/', segments);
    }

    private static (ApiMusicXMediaKind Kind, ApiMusicXLocalFileRole Role) ClassifyPath(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".mp3" or ".flac" or ".wav" or ".m4a" or ".aac" or ".ogg" or ".opus" or ".wma"
                => (ApiMusicXMediaKind.Audio, ApiMusicXLocalFileRole.TrackAudio),
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp"
                => (ApiMusicXMediaKind.Image, ApiMusicXLocalFileRole.Artwork),
            ".pdf" or ".txt" or ".nfo"
                => (ApiMusicXMediaKind.Document, ApiMusicXLocalFileRole.Booklet),
            _ => (ApiMusicXMediaKind.Other, ApiMusicXLocalFileRole.Unknown)
        };
    }

    private static ApiMusicXExternalIdentifierRequest CreateIdentifier(
        string provider,
        string resourceType,
        string externalId)
        => new()
        {
            Provider = provider,
            ResourceType = resourceType,
            ExternalId = externalId
        };

    private static bool TryGetProperty(
        JsonElement element,
        out JsonElement value,
        params string[] propertyNames)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (propertyNames.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static void AddItemErrorsToSummary(
        LegacyMusicMigrationItemResult item,
        LegacyMusicMigrationSummary summary)
    {
        foreach (var error in item.Errors)
        {
            summary.Erros.Add($"[{item.LegacyKey}] {error}");
        }

        if (item.Errors.Count > 0)
        {
            summary.Falhos++;
        }
    }

    private static void Report(
        IProgress<LegacyMusicMigrationProgress>? progress,
        string stage,
        int current,
        int total,
        string message)
        => progress?.Report(new LegacyMusicMigrationProgress(stage, current, total, message));

    private sealed record ReleaseCategory(
        string LegacyName,
        ApiMusicXReleaseType ReleaseType);

    private static readonly ReleaseCategory[] ReleaseCategories =
    [
        new("albums", ApiMusicXReleaseType.Album),
        new("singles-EP", ApiMusicXReleaseType.EP),
        new("compilations", ApiMusicXReleaseType.Compilation),
        new("videos", ApiMusicXReleaseType.Video)
    ];
}
