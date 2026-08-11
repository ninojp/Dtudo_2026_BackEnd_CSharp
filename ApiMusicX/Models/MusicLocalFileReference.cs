namespace ApiMusicX.Models;

public sealed class MusicLocalFileReference
{
    private MusicLocalFileReference()
    {
    }

    public MusicLocalFileReference(
        MusicRelease release,
        string relativePath,
        MusicMediaKind mediaKind,
        MusicLocalFileRole role = MusicLocalFileRole.Unknown,
        MusicTrack? track = null)
    {
        MusicRelease = release ?? throw new ArgumentNullException(nameof(release));
        if (track is not null && !ReferenceEquals(track.MusicRelease, release))
        {
            throw new ArgumentException("A faixa deve pertencer ao mesmo release da referencia local.", nameof(track));
        }

        RelativePath = MusicArtist.RequireText(relativePath, nameof(relativePath), 1024);
        NormalizedPath = MusicTextNormalizer.NormalizeRelativePath(relativePath);
        MediaKind = mediaKind;
        Role = role;
        MusicTrack = track;
    }

    public long MusicLocalFileReferenceId { get; private set; }

    public long MusicReleaseId { get; private set; }

    public long? MusicTrackId { get; private set; }

    public string RelativePath { get; private set; } = string.Empty;

    public string NormalizedPath { get; private set; } = string.Empty;

    public MusicMediaKind MediaKind { get; private set; }

    public MusicLocalFileRole Role { get; private set; }

    public MusicRelease MusicRelease { get; private set; } = null!;

    public MusicTrack? MusicTrack { get; private set; }
}
