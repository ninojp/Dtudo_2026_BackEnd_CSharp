using ApiMusicX.Mappers;
using ApiMusicX.Models;

namespace ApiMusicX.Tests;

public sealed class MusicMapperTests
{
    [Fact]
    public void CollectionMapperMapsArtistsReleasesTracksAndExternalIds()
    {
        var artist = new MusicArtist("Banda Mapper", MusicArtistType.Band);
        var collection = new MusicCollection("Colecao Mapper");
        var release = new MusicRelease("Release Mapper", MusicReleaseType.Album, 2026);
        var track = new MusicTrack(release, "Faixa Mapper", positionLabel: "A1", sequence: 1);
        var fileReference = new MusicLocalFileReference(
            release,
            "albums/mapper/track.flac",
            MusicMediaKind.Audio,
            MusicLocalFileRole.TrackAudio,
            track);
        var collectionIdentifier = new ExternalSourceIdentifier("ApiNode.MyMusicX", "Collection", "mapper")
        {
            MusicCollection = collection
        };
        var releaseIdentifier = new ExternalSourceIdentifier("Discogs", "Release", "123")
        {
            MusicRelease = release
        };

        collection.ArtistLinks.Add(new MusicCollectionArtist(collection, artist, MusicCollectionArtistRole.Primary));
        collection.ReleaseLinks.Add(new MusicCollectionRelease(collection, release, "albums", 0));
        collection.ExternalIdentifiers.Add(collectionIdentifier);
        release.Tracks.Add(track);
        release.LocalFileReferences.Add(fileReference);
        track.LocalFileReferences.Add(fileReference);
        release.ExternalIdentifiers.Add(releaseIdentifier);

        var result = MusicMapper.ToDto(collection);

        Assert.Equal("Colecao Mapper", result.DisplayName);
        Assert.Single(result.Artists);
        Assert.Equal("Banda Mapper", result.Artists[0].DisplayName);
        Assert.Single(result.Releases);
        Assert.Equal("Release Mapper", result.Releases[0].Title);
        Assert.Single(result.Releases[0].Tracks);
        Assert.Equal("albums/mapper/track.flac", result.Releases[0].Tracks[0].LocalFileReferences[0].RelativePath);
        Assert.Equal("123", result.Releases[0].ExternalIdentifiers[0].ExternalId);
        Assert.Equal("mapper", result.ExternalIdentifiers[0].ExternalId);
    }

    [Fact]
    public void CollectionSummaryCountsOnlyLoadedReleaseLinks()
    {
        var collection = new MusicCollection("Colecao Resumo");
        var artist = new MusicArtist("Artista Resumo");
        var release = new MusicRelease("Release Resumo");
        collection.ArtistLinks.Add(new MusicCollectionArtist(collection, artist));
        collection.ReleaseLinks.Add(new MusicCollectionRelease(collection, release));

        var result = MusicMapper.ToSummary(collection);

        Assert.Equal(1, result.ReleaseCount);
        Assert.Single(result.Artists);
    }
}
