using System.Globalization;
using System.Net;
using System.Text;

namespace WinAppDtudo.Services;

public interface IApiMusicXLocalConflictReader
{
    Task<ApiMusicXPagedResponse<ApiMusicXArtistSummaryDto>> BuscarArtistasAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ApiMusicXArtistDto?> ObterArtistaPorIdAsync(
        long id,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record ApiDiscogsReleaseImportDetails(
    ApiDiscogsReleaseSummary Summary,
    ApiDiscogsReleaseDetails? Release,
    ApiDiscogsMasterDetails? Master);

public sealed record ApiDiscogsImportPreview(
    ApiDiscogsArtistDetails Artist,
    IReadOnlyList<ApiDiscogsReleaseImportDetails> Releases,
    ApiMusicXImportCollectionRequest Request,
    bool HasLocalConflict,
    string? LocalConflictMessage,
    IReadOnlyList<string> Warnings)
{
    public int TrackCount => Releases.Sum(release => release.Release?.Tracklist.Count ?? 0);

    public string DisplaySummary =>
        $"Artista: {Artist.Name}{Environment.NewLine}" +
        $"Releases selecionados: {Releases.Count}{Environment.NewLine}" +
        $"Faixas encontradas: {TrackCount}{Environment.NewLine}" +
        (HasLocalConflict
            ? $"Conflito local: {LocalConflictMessage}"
            : "Nenhum conflito local identificado.");
}

public sealed record ApiDiscogsImportResult(
    bool Confirmed,
    bool Imported,
    ApiMusicXImportCollectionResponse? Response);

public sealed class ApiDiscogsImportCoordinator
{
    private const string DiscogsProvider = "Discogs";
    private readonly IApiDiscogsClient _discogsClient;
    private readonly IApiMusicXCollectionImporter _importer;
    private readonly IApiMusicXLocalConflictReader _conflictReader;

    public ApiDiscogsImportCoordinator(
        ApiDiscogsService discogsService,
        ApiMusicXService apiMusicXService)
        : this(discogsService, apiMusicXService, apiMusicXService)
    {
    }

    public ApiDiscogsImportCoordinator(
        IApiDiscogsClient discogsClient,
        IApiMusicXCollectionImporter importer,
        IApiMusicXLocalConflictReader conflictReader)
    {
        _discogsClient = discogsClient ?? throw new ArgumentNullException(nameof(discogsClient));
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _conflictReader = conflictReader ?? throw new ArgumentNullException(nameof(conflictReader));
    }

    public Task<ApiDiscogsPagedResponse<ApiDiscogsArtistSearchItem>> BuscarArtistasAsync(
        string query,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Report(progress, "Etapa 1/5: buscando artistas e bandas.");
        return _discogsClient.BuscarArtistasAsync(
            query,
            progress: progress,
            cancellationToken: cancellationToken);
    }

    public Task<ApiDiscogsArtistReleasesResponse> ObterDiscografiaAsync(
        ApiDiscogsArtistSearchItem artist,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artist);
        Report(progress, $"Etapa 2/5: carregando a discografia de {artist.Name}.");
        return _discogsClient.ObterDiscografiaAsync(
            artist.Source.Id,
            progress: progress,
            cancellationToken: cancellationToken);
    }

    public async Task<ApiDiscogsImportPreview> PrepararPreviewAsync(
        ApiDiscogsArtistSearchItem artist,
        IReadOnlyCollection<ApiDiscogsReleaseSummary> selectedReleases,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artist);
        ArgumentNullException.ThrowIfNull(selectedReleases);
        if (selectedReleases.Count == 0)
        {
            throw new ArgumentException(
                "Selecione ao menos um release para gerar o preview.",
                nameof(selectedReleases));
        }

        Report(progress, $"Etapa 3/5: carregando detalhes de {artist.Name}.");
        var artistDetails = await _discogsClient.ObterArtistaAsync(
            artist.Source.Id,
            progress,
            cancellationToken);
        var releaseDetails = new List<ApiDiscogsReleaseImportDetails>(selectedReleases.Count);
        for (var index = 0; index < selectedReleases.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var summary = selectedReleases.ElementAt(index);
            Report(
                progress,
                $"Etapa 3/5: carregando detalhe {index + 1}/{selectedReleases.Count}: {summary.Title}.");

            if (string.Equals(summary.ResourceType, "master", StringComparison.OrdinalIgnoreCase))
            {
                var master = await _discogsClient.ObterMasterAsync(
                    summary.Source.Id,
                    progress,
                    cancellationToken);
                ApiDiscogsReleaseDetails? release = null;
                var mainReleaseId = master.MainReleaseId ?? summary.MainReleaseId;
                if (!string.IsNullOrWhiteSpace(mainReleaseId))
                {
                    release = await _discogsClient.ObterReleaseAsync(
                        mainReleaseId,
                        progress,
                        cancellationToken);
                }

                releaseDetails.Add(new ApiDiscogsReleaseImportDetails(summary, release, master));
            }
            else
            {
                var release = await _discogsClient.ObterReleaseAsync(
                    summary.Source.Id,
                    progress,
                    cancellationToken);
                releaseDetails.Add(new ApiDiscogsReleaseImportDetails(summary, release, null));
            }
        }

        var request = BuildImportRequest(artistDetails, releaseDetails);
        Report(progress, "Etapa 4/5: verificando possiveis conflitos na Colecao local.");
        var conflict = await FindLocalConflictAsync(
            artistDetails,
            request,
            progress,
            cancellationToken);
        var warnings = CollectWarnings(artistDetails, releaseDetails);
        if (conflict is not null)
        {
            Report(progress, $"Etapa 4/5: conflito local identificado: {conflict}");
        }
        else
        {
            Report(progress, "Etapa 4/5: nenhum conflito local bloqueante identificado.");
        }

        return new ApiDiscogsImportPreview(
            artistDetails,
            releaseDetails,
            request,
            conflict is not null,
            conflict,
            warnings);
    }

    public async Task<ApiDiscogsImportResult> ImportarConfirmadaAsync(
        ApiDiscogsImportPreview preview,
        bool confirmed,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!confirmed)
        {
            Report(progress, "Etapa 5/5: importacao nao confirmada; nenhuma gravacao foi enviada.");
            return new ApiDiscogsImportResult(false, false, null);
        }

        if (preview.HasLocalConflict)
        {
            throw new ApiDiscogsImportConflictException(
                preview.LocalConflictMessage ??
                "A importacao possui conflito local e nao pode sobrescrever dados silenciosamente.");
        }

        Report(progress, "Etapa 5/5: confirmacao recebida; enviando o resultado para a ApiMusicX.");
        try
        {
            var response = await _importer.ImportarColecaoAsync(
                preview.Request,
                progress,
                cancellationToken);
            Report(
                progress,
                $"Etapa 5/5: importacao concluida ({response.ArtistsAdded} artista(s), " +
                $"{response.ReleasesAdded} release(s), {response.TracksAdded} faixa(s)).");
            return new ApiDiscogsImportResult(true, true, response);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ApiDiscogsImportConflictException(
                "A ApiMusicX identificou um conflito local. Nenhum dado local foi sobrescrito.");
        }
    }

    private async Task<string?> FindLocalConflictAsync(
        ApiDiscogsArtistDetails artist,
        ApiMusicXImportCollectionRequest request,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var matches = await _conflictReader.BuscarArtistasAsync(
            artist.Name,
            page: 1,
            pageSize: 20,
            progress,
            cancellationToken);
        var exactMatches = matches.Items
            .Where(item => string.Equals(item.DisplayName, artist.Name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exactMatches.Length == 0)
        {
            return null;
        }

        var externalArtistId = request.Artists
            .SelectMany(item => item.ExternalIdentifiers)
            .FirstOrDefault(identifier =>
                string.Equals(identifier.Provider, DiscogsProvider, StringComparison.OrdinalIgnoreCase)
                && string.Equals(identifier.ResourceType, "artist", StringComparison.OrdinalIgnoreCase))
            ?.ExternalId;
        var hasSameExternalIdentity = false;
        foreach (var match in exactMatches)
        {
            var details = await _conflictReader.ObterArtistaPorIdAsync(
                match.MusicArtistId,
                progress,
                cancellationToken);
            if (details is null)
            {
                continue;
            }

            hasSameExternalIdentity = details.ExternalIdentifiers.Any(identifier =>
                string.Equals(identifier.Provider, DiscogsProvider, StringComparison.OrdinalIgnoreCase)
                && string.Equals(identifier.ResourceType, "artist", StringComparison.OrdinalIgnoreCase)
                && string.Equals(identifier.ExternalId, externalArtistId, StringComparison.Ordinal));
            if (!hasSameExternalIdentity)
            {
                return $"O artista local '{details.DisplayName}' ja existe com outra identidade externa. " +
                       "Revise a Colecao local antes de importar.";
            }
        }

        return hasSameExternalIdentity
            ? null
            : "Existe um artista local com o mesmo nome e a identidade nao pode ser confirmada.";
    }

    private static ApiMusicXImportCollectionRequest BuildImportRequest(
        ApiDiscogsArtistDetails artist,
        IReadOnlyList<ApiDiscogsReleaseImportDetails> releaseDetails)
    {
        var artistType = artist.Members.Count > 0
            ? ApiMusicXArtistType.Band
            : ApiMusicXArtistType.Solo;
        var artistRequest = new ApiMusicXArtistImportRequest
        {
            DisplayName = artist.Name,
            ArtistType = artistType,
            SortName = artist.Name,
            Aliases = artist.Aliases
                .Select(alias => alias.Name)
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(50)
                .ToList(),
            CollectionRole = ApiMusicXCollectionArtistRole.Primary,
            ExternalIdentifiers =
            [
                new ApiMusicXExternalIdentifierRequest
                {
                    Provider = DiscogsProvider,
                    ResourceType = "artist",
                    ExternalId = artist.Source.Id
                }
            ]
        };

        return new ApiMusicXImportCollectionRequest
        {
            DisplayName = artist.Name,
            Description = LimitText(artist.Profile, 2000),
            ExternalIdentifiers =
            [
                new ApiMusicXExternalIdentifierRequest
                {
                    Provider = DiscogsProvider,
                    ResourceType = "Collection",
                    ExternalId = artist.Source.Id
                }
            ],
            Artists = [artistRequest],
            Releases = releaseDetails
                .Select((release, index) => MapRelease(release, index + 1))
                .ToList()
        };
    }

    private static ApiMusicXReleaseImportRequest MapRelease(
        ApiDiscogsReleaseImportDetails details,
        int displayOrder)
    {
        var summary = details.Summary;
        var release = details.Release;
        var master = details.Master;
        var title = release?.Title ?? master?.Title ?? summary.Title;
        var year = release?.Year ?? master?.Year ?? summary.Year;
        var identifiers = new List<ApiMusicXExternalIdentifierRequest>
        {
            new()
            {
                Provider = DiscogsProvider,
                ResourceType = summary.Source.ResourceType,
                ExternalId = summary.Source.Id
            }
        };
        var mainReleaseId = master?.MainReleaseId ?? summary.MainReleaseId;
        if (!string.IsNullOrWhiteSpace(mainReleaseId)
            && !identifiers.Any(identifier =>
                identifier.ResourceType == "release"
                && identifier.ExternalId == mainReleaseId))
        {
            identifiers.Add(new ApiMusicXExternalIdentifierRequest
            {
                Provider = DiscogsProvider,
                ResourceType = "release",
                ExternalId = mainReleaseId
            });
        }

        var releaseArtists = release?.Artists ?? master?.Artists ?? [];
        var tracks = release?.Tracklist
            .Select(MapTrack)
            .ToList() ?? [];
        var genres = release?.Genres ?? master?.Genres ?? [];
        var styles = release?.Styles ?? master?.Styles ?? [];
        var notes = new StringBuilder();
        AppendText(notes, release?.Notes);
        AppendText(notes, genres.Count == 0 ? null : $"Generos: {string.Join(", ", genres)}");
        AppendText(notes, styles.Count == 0 ? null : $"Estilos: {string.Join(", ", styles)}");

        return new ApiMusicXReleaseImportRequest
        {
            Title = title,
            ReleaseType = MapReleaseType(summary.Category, summary.Formats, title),
            ReleaseYear = year,
            Notes = LimitText(notes.ToString(), 2000),
            SourceCategory = LimitText(summary.Category, 64),
            DisplayOrder = displayOrder,
            ExternalIdentifiers = identifiers,
            ArtistCredits = releaseArtists.Select(credit => MapArtistCredit(credit, ApiMusicXCreditRole.Primary)).ToList(),
            Tracks = tracks
        };
    }

    private static ApiMusicXTrackImportRequest MapTrack(ApiDiscogsTrack track)
    {
        var credits = track.Artists
            .Concat(track.ExtraArtists)
            .Where(credit => !string.IsNullOrWhiteSpace(credit.Name))
            .Select(credit => MapArtistCredit(credit, MapCreditRole(credit.Role)))
            .ToList();
        return new ApiMusicXTrackImportRequest
        {
            Title = track.Title,
            PositionLabel = track.Position,
            Sequence = ParseSequence(track.Position),
            DurationSeconds = track.DurationSeconds,
            DurationText = track.DurationText,
            ArtistCredits = credits
        };
    }

    private static ApiMusicXArtistCreditImportRequest MapArtistCredit(
        ApiDiscogsCredit credit,
        ApiMusicXCreditRole role)
    {
        var request = new ApiMusicXArtistCreditImportRequest
        {
            DisplayName = credit.Name,
            Role = role,
            ArtistType = ApiMusicXArtistType.Unknown
        };
        if (!string.IsNullOrWhiteSpace(credit.Id))
        {
            request.ExternalIdentifiers.Add(new ApiMusicXExternalIdentifierRequest
            {
                Provider = DiscogsProvider,
                ResourceType = "artist",
                ExternalId = credit.Id
            });
        }

        return request;
    }

    private static ApiMusicXCreditRole MapCreditRole(string? role)
    {
        var normalized = role?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized.Contains("composer", StringComparison.Ordinal)
            ? ApiMusicXCreditRole.Composer
            : normalized.Contains("featur", StringComparison.Ordinal)
                ? ApiMusicXCreditRole.Featured
                : ApiMusicXCreditRole.Primary;
    }

    private static ApiMusicXReleaseType MapReleaseType(
        string? category,
        IReadOnlyList<string> formats,
        string title)
    {
        var normalizedCategory = category?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedCategory == "video")
        {
            return ApiMusicXReleaseType.Video;
        }

        if (normalizedCategory == "compilation")
        {
            return ApiMusicXReleaseType.Compilation;
        }

        var formatText = string.Join(' ', formats).ToLowerInvariant();
        var titleText = title.ToLowerInvariant();
        if (normalizedCategory == "singleep")
        {
            return formatText.Contains("ep", StringComparison.Ordinal)
                || titleText.Contains(" ep", StringComparison.Ordinal)
                ? ApiMusicXReleaseType.EP
                : ApiMusicXReleaseType.Single;
        }

        return ApiMusicXReleaseType.Album;
    }

    private static int? ParseSequence(string? position)
    {
        if (string.IsNullOrWhiteSpace(position))
        {
            return null;
        }

        var digits = new StringBuilder();
        foreach (var character in position)
        {
            if (char.IsDigit(character))
            {
                digits.Append(character);
            }
            else if (digits.Length > 0)
            {
                break;
            }
        }

        return int.TryParse(digits.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : null;
    }

    private static IReadOnlyList<string> CollectWarnings(
        ApiDiscogsArtistDetails artist,
        IReadOnlyList<ApiDiscogsReleaseImportDetails> releases)
    {
        var warnings = new List<string>(artist.Warnings);
        foreach (var release in releases)
        {
            warnings.AddRange(release.Summary.Warnings);
            warnings.AddRange(release.Release?.Warnings ?? []);
            warnings.AddRange(release.Master?.Warnings ?? []);
        }

        return warnings.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static void AppendText(StringBuilder builder, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.Append(value.Trim());
    }

    private static string? LimitText(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }

    private static void Report(IProgress<string>? progress, string message)
        => progress?.Report($"ApiDiscogs: {message}");
}
