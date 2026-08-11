namespace ApiMusicX.Models;

public sealed class MusicCollectionRelease
{
    private MusicCollectionRelease()
    {
    }

    public MusicCollectionRelease(
        MusicCollection collection,
        MusicRelease release,
        string? sourceCategory = null,
        int? displayOrder = null)
    {
        MusicCollection = collection ?? throw new ArgumentNullException(nameof(collection));
        MusicRelease = release ?? throw new ArgumentNullException(nameof(release));
        SourceCategory = MusicArtist.NormalizeOptional(sourceCategory, 64);
        if (displayOrder is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayOrder), "A ordem do release nao pode ser negativa.");
        }

        DisplayOrder = displayOrder;
    }

    public long MusicCollectionId { get; private set; }

    public long MusicReleaseId { get; private set; }

    public string? SourceCategory { get; private set; }

    public int? DisplayOrder { get; private set; }

    public MusicCollection MusicCollection { get; private set; } = null!;

    public MusicRelease MusicRelease { get; private set; } = null!;

        public void UpdateMetadata(string? sourceCategory, int? displayOrder)
        {
            SourceCategory = MusicArtist.NormalizeOptional(sourceCategory, 64);
            if (displayOrder is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(displayOrder), "A ordem do release nao pode ser negativa.");
            }

            DisplayOrder = displayOrder;
        }
}
