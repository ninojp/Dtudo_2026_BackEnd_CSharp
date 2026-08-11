namespace ApiMusicX.Models;

public sealed class MusicTrack
{
    private MusicTrack()
    {
    }

    public MusicTrack(
        MusicRelease release,
        string title,
        string? positionLabel = null,
        int? sequence = null,
        int? durationSeconds = null,
        string? durationText = null,
        string? notes = null)
    {
        MusicRelease = release ?? throw new ArgumentNullException(nameof(release));
        Title = MusicArtist.RequireText(title, nameof(title), 512);
        NormalizedTitle = MusicTextNormalizer.NormalizeSearchText(title);
        PositionLabel = MusicArtist.NormalizeOptional(positionLabel, 64);
        if (sequence is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "A sequencia da faixa nao pode ser negativa.");
        }

        if (durationSeconds is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "A duracao da faixa nao pode ser negativa.");
        }

        Sequence = sequence;
        DurationSeconds = durationSeconds;
        DurationText = MusicArtist.NormalizeOptional(durationText, 32);
        Notes = MusicArtist.NormalizeOptional(notes, 2000);
    }

    public long MusicTrackId { get; private set; }

    public long MusicReleaseId { get; private set; }

    public string? PositionLabel { get; private set; }

    public int? Sequence { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string NormalizedTitle { get; private set; } = string.Empty;

    public int? DurationSeconds { get; private set; }

    public string? DurationText { get; private set; }

    public string? Notes { get; private set; }

    public MusicRelease MusicRelease { get; private set; } = null!;

    public ICollection<MusicTrackArtist> ArtistCredits { get; } = [];

    public ICollection<MusicLocalFileReference> LocalFileReferences { get; } = [];

    public ICollection<ExternalSourceIdentifier> ExternalIdentifiers { get; } = [];

        public void UpdateDetails(
            string title,
            string? positionLabel,
            int? sequence,
            int? durationSeconds,
            string? durationText,
            string? notes)
        {
            Title = MusicArtist.RequireText(title, nameof(title), 512);
            NormalizedTitle = MusicTextNormalizer.NormalizeSearchText(title);
            PositionLabel = MusicArtist.NormalizeOptional(positionLabel, 64);
            if (sequence is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence), "A sequencia da faixa nao pode ser negativa.");
            }

            if (durationSeconds is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "A duracao da faixa nao pode ser negativa.");
            }

            Sequence = sequence;
            DurationSeconds = durationSeconds;
            DurationText = MusicArtist.NormalizeOptional(durationText, 32);
            Notes = MusicArtist.NormalizeOptional(notes, 2000);
        }
}
