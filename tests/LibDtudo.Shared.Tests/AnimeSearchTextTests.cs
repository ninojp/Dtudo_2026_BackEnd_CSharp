using LibDtudo.Shared.Search;

namespace LibDtudo.Shared.Tests;

public class AnimeSearchTextTests
{
    [Fact]
    public void MatchesTitleWithOneMissingLetter()
    {
        var title = AnimeSearchTextNormalizer.Normalize("Raikou Shinki Aigis Magia Pandra Saga 3rd Ignition");
        var query = AnimeSearchTextNormalizer.Normalize("Raikou Shinki Igis Magia Pandra Saga 3rd Ignition");

        Assert.True(title.Matches(query));
    }

    [Fact]
    public void MatchesIgnoringAccentsHtmlEntitiesAndPunctuation()
    {
        var title = AnimeSearchTextNormalizer.Normalize("Héllo &amp; World: 2");
        var query = AnimeSearchTextNormalizer.Normalize("hello world 2");

        Assert.True(title.Matches(query));
    }

    [Fact]
    public void DoesNotAcceptDifferentShortWordsAsFuzzyMatch()
    {
        var title = AnimeSearchTextNormalizer.Normalize("One Piece");
        var query = AnimeSearchTextNormalizer.Normalize("One Ace");

        Assert.False(title.Matches(query));
    }

    [Fact]
    public void EmptySearchDoesNotMatch()
    {
        var title = AnimeSearchTextNormalizer.Normalize("Aigis");

        Assert.False(title.Matches(AnimeSearchTextNormalizer.Normalize("   ")));
    }
}
