using ApiMyAnimes.Services;
using LibDtudo.Shared.Models;

namespace ApiMyAnimes.Tests;

public sealed class PublicCatalogPolicyTests
{
    [Fact]
    public void HentaiGenreIsNotPublic()
    {
        var anime = new Anime { Genres = ["Comedy", "Hentai"] };

        Assert.True(PublicCatalogPolicy.IsAdult(anime));
    }

    [Theory]
    [InlineData("Rx - Hentai")]
    [InlineData("R+ - Mild Nudity")]
    [InlineData("Adult Only")]
    public void AdultRatingsAreNotPublic(string rating)
    {
        var anime = new Anime { Rating = rating };

        Assert.True(PublicCatalogPolicy.IsAdult(anime));
    }

    [Fact]
    public void OrdinaryCatalogEntryIsPublic()
    {
        var anime = new Anime { Rating = "PG-13 - Teens 13 or older", Genres = ["Action"] };

        Assert.False(PublicCatalogPolicy.IsAdult(anime));
    }
}
