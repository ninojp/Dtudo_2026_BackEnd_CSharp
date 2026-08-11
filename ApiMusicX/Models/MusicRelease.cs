namespace ApiMusicX.Models;

public sealed class MusicRelease
{
    private MusicRelease()
    {
    }

    public MusicRelease(
        string title,
        MusicReleaseType releaseType = MusicReleaseType.Unknown,
        int? releaseYear = null,
        string? notes = null)
    {
        Title = MusicArtist.RequireText(title, nameof(title), 512);
        NormalizedTitle = MusicTextNormalizer.NormalizeSearchText(title);
        ReleaseType = releaseType;
        ReleaseYear = ValidateYear(releaseYear);
        Notes = MusicArtist.NormalizeOptional(notes, 2000);
    }

    public long MusicReleaseId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string NormalizedTitle { get; private set; } = string.Empty;

    public MusicReleaseType ReleaseType { get; private set; }

    public int? ReleaseYear { get; private set; }

    public string? Notes { get; private set; }

    public ICollection<MusicCollectionRelease> CollectionLinks { get; } = [];

    public ICollection<MusicTrack> Tracks { get; } = [];

    public ICollection<MusicReleaseArtist> ArtistCredits { get; } = [];

    public ICollection<MusicLocalFileReference> LocalFileReferences { get; } = [];

    public ICollection<ExternalSourceIdentifier> ExternalIdentifiers { get; } = [];

        public void UpdateDetails(
            string title,
            MusicReleaseType releaseType,
            int? releaseYear,
            string? notes)
        {
            Title = MusicArtist.RequireText(title, nameof(title), 512);
            NormalizedTitle = MusicTextNormalizer.NormalizeSearchText(title);
            ReleaseType = releaseType;
            ReleaseYear = ValidateYear(releaseYear);
            Notes = MusicArtist.NormalizeOptional(notes, 2000);
        }

    private static int? ValidateYear(int? value)
    {
        if (value is not null and (< 1000 or > 9999))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "O ano do release deve estar entre 1000 e 9999.");
        }

        return value;
    }
}
