using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class ApiDiscogsImportCoordinatorTests
{
    [Fact]
    public async Task LocalConflictBlocksConfirmedImport()
    {
        var artist = CreateArtistSearchItem();
        var discogs = new FakeDiscogsClient(artist);
        var importer = new FakeImporter();
        var local = new FakeLocalReader
        {
            ArtistResults = new ApiMusicXPagedResponse<ApiMusicXArtistSummaryDto>(
                [new ApiMusicXArtistSummaryDto(12, "Example Artist", ApiMusicXArtistType.Solo)],
                1,
                20,
                1),
            ArtistDetails = new ApiMusicXArtistDto(
                12,
                "Example Artist",
                ApiMusicXArtistType.Solo,
                null,
                [],
                [],
                [new ApiMusicXExternalIdentifierDto("Discogs", "artist", "999")])
        };
        var coordinator = new ApiDiscogsImportCoordinator(discogs, importer, local);

        var preview = await coordinator.PrepararPreviewAsync(
            artist,
            [discogs.ReleaseSummary]);

        Assert.True(preview.HasLocalConflict);
        await Assert.ThrowsAsync<ApiDiscogsImportConflictException>(() =>
            coordinator.ImportarConfirmadaAsync(preview, confirmed: true));
        Assert.Equal(0, importer.CallCount);
    }

    [Fact]
    public async Task UnconfirmedPreviewDoesNotCallApiMusicX()
    {
        var artist = CreateArtistSearchItem();
        var discogs = new FakeDiscogsClient(artist);
        var importer = new FakeImporter();
        var coordinator = new ApiDiscogsImportCoordinator(
            discogs,
            importer,
            new FakeLocalReader());

        var preview = await coordinator.PrepararPreviewAsync(
            artist,
            [discogs.ReleaseSummary]);
        var result = await coordinator.ImportarConfirmadaAsync(preview, confirmed: false);

        Assert.False(result.Confirmed);
        Assert.False(result.Imported);
        Assert.Equal(0, importer.CallCount);
    }

    [Fact]
    public async Task ConfirmedPreviewSendsNormalizedSelectionToApiMusicX()
    {
        var artist = CreateArtistSearchItem();
        var discogs = new FakeDiscogsClient(artist);
        var importer = new FakeImporter();
        var coordinator = new ApiDiscogsImportCoordinator(
            discogs,
            importer,
            new FakeLocalReader());

        var preview = await coordinator.PrepararPreviewAsync(
            artist,
            [discogs.ReleaseSummary]);
        var result = await coordinator.ImportarConfirmadaAsync(preview, confirmed: true);

        Assert.True(result.Confirmed);
        Assert.True(result.Imported);
        Assert.Equal(1, importer.CallCount);
        Assert.Equal("Example Artist", importer.LastRequest!.DisplayName);
        Assert.Contains(
            importer.LastRequest.ExternalIdentifiers,
            identifier => identifier.Provider == "Discogs"
                && identifier.ResourceType == "Collection"
                && identifier.ExternalId == "123");
        Assert.Contains(
            importer.LastRequest.Artists[0].ExternalIdentifiers,
            identifier => identifier.Provider == "Discogs"
                && identifier.ResourceType == "artist"
                && identifier.ExternalId == "123");
        Assert.Single(importer.LastRequest.Releases);
        Assert.Single(importer.LastRequest.Releases[0].Tracks);
    }

    private static ApiDiscogsArtistSearchItem CreateArtistSearchItem() =>
        new(
            new ApiDiscogsSourceReference("Discogs", "artist", "123", null),
            "Example Artist",
            "artist",
            null,
            null);

    private sealed class FakeDiscogsClient(ApiDiscogsArtistSearchItem artist) : IApiDiscogsClient
    {
        public ApiDiscogsReleaseSummary ReleaseSummary { get; } = new(
            new ApiDiscogsSourceReference("Discogs", "release", "456", null),
            "release:456",
            "release",
            "Example Album",
            artist.Name,
            artist.Source.Id,
            2024,
            null,
            null,
            "main",
            ["main"],
            ["Album", "CD"],
            "album",
            null,
            null,
            true,
            []);

        public Task<ApiDiscogsPagedResponse<ApiDiscogsArtistSearchItem>> BuscarArtistasAsync(
            string query,
            int page = 1,
            int perPage = 10,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ApiDiscogsPagedResponse<ApiDiscogsArtistSearchItem>(
                "Discogs",
                [artist],
                new ApiDiscogsPagination(1, 10, 1, 1, false, 1),
                true,
                []));

        public Task<ApiDiscogsArtistDetails> ObterArtistaAsync(
            string artistId,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ApiDiscogsArtistDetails(
                artist.Source,
                artist.Name,
                null,
                "Perfil de exemplo",
                [],
                [],
                [],
                [],
                true,
                []));

        public Task<ApiDiscogsArtistReleasesResponse> ObterDiscografiaAsync(
            string artistId,
            int page = 1,
            int perPage = 50,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ApiDiscogsArtistReleasesResponse(
                "Discogs",
                new ApiDiscogsNameReference(artist.Source.Id, artist.Name),
                [ReleaseSummary],
                new ApiDiscogsPagination(1, 50, 1, 1, false, 1),
                true,
                []));

        public Task<ApiDiscogsReleaseDetails> ObterReleaseAsync(
            string releaseId,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ApiDiscogsReleaseDetails(
                ReleaseSummary.Source,
                ReleaseSummary.Title,
                2024,
                "2024-01-01",
                "Brazil",
                "Official",
                null,
                [new ApiDiscogsCredit("123", artist.Name, null)],
                [],
                ["Rock"],
                ["Alternative"],
                ["CD"],
                [new ApiDiscogsTrack(
                    "1",
                    "Example Track",
                    180,
                    "3:00",
                    [],
                    [])],
                [],
                null,
                true,
                []));

        public Task<ApiDiscogsMasterDetails> ObterMasterAsync(
            string masterId,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeImporter : IApiMusicXCollectionImporter
    {
        public int CallCount { get; private set; }

        public ApiMusicXImportCollectionRequest? LastRequest { get; private set; }

        public Task<ApiMusicXImportCollectionResponse> ImportarColecaoAsync(
            ApiMusicXImportCollectionRequest request,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            var collection = new ApiMusicXCollectionDto(
                1,
                request.DisplayName,
                request.Description,
                [],
                [],
                []);
            return Task.FromResult(new ApiMusicXImportCollectionResponse(
                collection,
                true,
                true,
                1,
                1,
                1,
                0));
        }
    }

    private sealed class FakeLocalReader : IApiMusicXLocalConflictReader
    {
        public ApiMusicXPagedResponse<ApiMusicXArtistSummaryDto> ArtistResults { get; init; } =
            new([], 1, 20, 0);

        public ApiMusicXArtistDto? ArtistDetails { get; init; }

        public Task<ApiMusicXPagedResponse<ApiMusicXArtistSummaryDto>> BuscarArtistasAsync(
            string? search = null,
            int page = 1,
            int pageSize = 20,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ArtistResults);

        public Task<ApiMusicXArtistDto?> ObterArtistaPorIdAsync(
            long id,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ArtistDetails);
    }
}
