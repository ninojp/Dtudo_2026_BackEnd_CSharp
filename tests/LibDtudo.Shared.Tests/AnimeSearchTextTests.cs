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

    [Fact]
    public void TitleEquivalenceMatchesNormalizedTitlesOnly()
    {
        Assert.True(AnimeTitleEquivalence.AreEquivalent("Héllo &amp; World: 2", " hello-world 2 "));
        Assert.False(AnimeTitleEquivalence.AreEquivalent("One Piece", "One Pace"));
    }

    [Fact]
    public void FindEquivalentTitleChecksEveryCandidateAndExistingTitle()
    {
        var existingTitle = AnimeTitleEquivalence.FindEquivalentTitle(
            ["Original title", "English title"],
            ["Outro", " english-title "]);

        Assert.Equal(" english-title ", existingTitle);
    }
}
