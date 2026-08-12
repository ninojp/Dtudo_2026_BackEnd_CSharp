using System.Text.Json;
using ApiDiscogs.Mappers;
using ApiDiscogs.Services;

namespace ApiDiscogs.Tests;

public sealed class DiscogsMapperTests
{
    [Fact]
    public void MapsArtistDetailsWithAliasesMembersAndImages()
    {
        using var document = JsonDocument.Parse("""
        {
          "id": 42,
          "name": "Artist Name",
          "realname": "Real Name",
          "profile": "Artist profile",
          "aliases": [{ "id": 43, "name": "Alias Name" }],
          "members": [{ "id": 44, "name": "Member Name" }],
          "urls": ["https://artist.example/profile"],
          "images": [{ "type": "primary", "uri": "https://img.example/artist.jpg", "height": 800, "width": 600 }],
          "resource_url": "https://api.discogs.com/artists/42"
        }
        """);

        var result = DiscogsMapper.MapArtistDetails(document);

        Assert.Equal("42", result.Source.Id);
        Assert.Equal("Real Name", result.RealName);
        Assert.Equal("Alias Name", Assert.Single(result.Aliases).Name);
        Assert.Equal("Member Name", Assert.Single(result.Members).Name);
        Assert.Equal("https://artist.example/profile", Assert.Single(result.Urls));
        Assert.Equal("https://api.discogs.com/artists/42", result.Source.ResourceUrl);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void MapsMasterDetailsWithMainReleaseAndVersions()
    {
        using var document = JsonDocument.Parse("""
        {
          "id": 7,
          "title": "Master Title",
          "main_release": 99,
          "year": 2001,
          "genres": ["Rock"],
          "styles": ["Alternative"],
          "artists": [{ "id": 42, "name": "Artist Name" }],
          "versions": {
            "versions": [
              { "id": 99, "title": "Master Title CD", "type": "release", "master_id": 7, "format": "CD, Album" }
            ]
          },
          "images": [{ "uri": "https://img.example/master.jpg" }]
        }
        """);

        var result = DiscogsMapper.MapMasterDetails(document);

        Assert.Equal("7", result.Source.Id);
        Assert.Equal("99", result.MainReleaseId);
        Assert.Equal("Rock", Assert.Single(result.Genres));
        Assert.Equal("42", Assert.Single(result.Artists).Id);
        var version = Assert.Single(result.Versions);
        Assert.Equal("master:7", version.CanonicalId);
        Assert.Equal("release", version.ResourceType);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void MapsArtistSearchToStableContractAndValidatesImages()
    {
        using var document = JsonDocument.Parse("""
        {
          "pagination": { "page": 1, "per_page": 10, "items": 1, "pages": 1 },
          "results": [
            {
              "id": 42,
              "type": "artist",
              "title": "Artist Name",
              "thumb": "https://img.example/thumbnail.jpg",
              "cover_image": "http://insecure.example/cover.jpg",
              "resource_url": "https://api.discogs.com/artists/42"
            }
          ]
        }
        """);

        var result = DiscogsMapper.MapArtistSearch(document);

        var item = Assert.Single(result.Items);
        Assert.Equal("Discogs", item.Source.Provider);
        Assert.Equal("artist", item.Source.ResourceType);
        Assert.Equal("42", item.Source.Id);
        Assert.Equal("Artist Name", item.Name);
        Assert.Equal("https://img.example/thumbnail.jpg", item.ThumbnailUrl);
        Assert.Null(item.ImageUrl);
        Assert.False(result.Pagination.HasNextPage);
    }

    [Fact]
    public void MapsReleaseDetailsIncludingTracklistAndOptionalFields()
    {
        using var document = JsonDocument.Parse("""
        {
          "id": 99,
          "title": "A Release",
          "year": 2024,
          "master_id": 7,
          "artists": [{ "id": 42, "name": "Artist Name", "role": "Main" }],
          "labels": [{ "id": 3, "name": "Label", "catno": "CAT-1" }],
          "formats": [{ "name": "CD", "descriptions": ["Album"] }],
          "tracklist": [
            {
              "position": "A1",
              "title": "Opening",
              "duration": "4:05",
              "artists": [{ "id": 42, "name": "Artist Name" }]
            }
          ],
          "images": [{ "type": "primary", "uri": "https://img.example/release.jpg", "width": 600 }]
        }
        """);

        var result = DiscogsMapper.MapReleaseDetails(document);

        Assert.Equal("99", result.Source.Id);
        Assert.Equal("7", result.MasterId);
        Assert.Equal("CD (Album)", Assert.Single(result.Formats));
        var track = Assert.Single(result.Tracklist);
        Assert.Equal(245, track.DurationSeconds);
        Assert.Equal("Artist Name", Assert.Single(track.Artists).Name);
        Assert.Equal(600, Assert.Single(result.Images).Width);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void DeduplicatesDiscographyAndAggregatesFormatsAndRoles()
    {
        using var document = JsonDocument.Parse("""
        {
          "pagination": { "page": 1, "per_page": 50, "items": 4, "pages": 1 },
          "releases": [
            { "id": 10, "master_id": 20, "title": "Album", "type": "release", "format": "CD, Album", "role": "Main", "year": 2000 },
            { "id": 11, "master_id": 20, "title": "Album", "type": "release", "format": "Vinyl, Album", "role": "Main" },
            { "id": 30, "title": "Unofficial", "type": "release", "format": "CD, Unofficial", "role": "Main" },
            { "id": 40, "title": "Live Video", "type": "release", "format": "DVD, Video", "role": "Video" }
          ]
        }
        """);

        var result = DiscogsMapper.MapArtistReleases(document, 42);

        Assert.Equal(2, result.Items.Count);
        var album = Assert.Single(result.Items, item => item.CanonicalId == "master:20");
        Assert.Equal(2, album.Formats.Count);
        Assert.Equal("album", album.Category);
        var video = Assert.Single(result.Items, item => item.CanonicalId == "release:40");
        Assert.Equal("video", video.Category);
        Assert.Equal("42", result.Artist.Id);
        Assert.Equal(2, result.Pagination.UniqueItemsInPage);
    }

    [Fact]
    public void MissingPaginationIsReportedAsInvalidExternalResponse()
    {
        using var document = JsonDocument.Parse("""
        {
          "results": [{ "id": 42, "title": "Artist Name" }]
        }
        """);

        Assert.Throws<DiscogsInvalidResponseException>(() => DiscogsMapper.MapArtistSearch(document));
    }
}
