namespace ApiMusicX.Models;

public sealed class MusicCollection
{
    private MusicCollection()
    {
    }

    public MusicCollection(string displayName, string? description = null)
    {
        DisplayName = MusicArtist.RequireText(displayName, nameof(displayName), 256);
        NormalizedName = MusicTextNormalizer.NormalizeSearchText(displayName);
        Description = MusicArtist.NormalizeOptional(description, 2000);
    }

    public long MusicCollectionId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public ICollection<MusicCollectionArtist> ArtistLinks { get; } = [];

    public ICollection<MusicCollectionRelease> ReleaseLinks { get; } = [];

    public ICollection<ExternalSourceIdentifier> ExternalIdentifiers { get; } = [];

        public void UpdateDetails(string displayName, string? description)
        {
            DisplayName = MusicArtist.RequireText(displayName, nameof(displayName), 256);
            NormalizedName = MusicTextNormalizer.NormalizeSearchText(displayName);
            Description = MusicArtist.NormalizeOptional(description, 2000);
        }
}
