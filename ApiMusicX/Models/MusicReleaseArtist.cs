namespace ApiMusicX.Models;

public sealed class MusicReleaseArtist
{
    private MusicReleaseArtist()
    {
    }

    public MusicReleaseArtist(
        MusicRelease release,
        MusicArtist artist,
        MusicCreditRole role = MusicCreditRole.Unknown)
    {
        MusicRelease = release ?? throw new ArgumentNullException(nameof(release));
        MusicArtist = artist ?? throw new ArgumentNullException(nameof(artist));
        Role = role;
    }

    public long MusicReleaseId { get; private set; }

    public long MusicArtistId { get; private set; }

    public MusicCreditRole Role { get; private set; }

    public MusicRelease MusicRelease { get; private set; } = null!;

    public MusicArtist MusicArtist { get; private set; } = null!;

    public void UpdateRole(MusicCreditRole role)
        => Role = role;
}
