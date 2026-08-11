namespace ApiMusicX.Models;

public sealed class MusicTrackArtist
{
    private MusicTrackArtist()
    {
    }

    public MusicTrackArtist(
        MusicTrack track,
        MusicArtist artist,
        MusicCreditRole role = MusicCreditRole.Unknown)
    {
        MusicTrack = track ?? throw new ArgumentNullException(nameof(track));
        MusicArtist = artist ?? throw new ArgumentNullException(nameof(artist));
        Role = role;
    }

    public long MusicTrackId { get; private set; }

    public long MusicArtistId { get; private set; }

    public MusicCreditRole Role { get; private set; }

    public MusicTrack MusicTrack { get; private set; } = null!;

    public MusicArtist MusicArtist { get; private set; } = null!;

    public void UpdateRole(MusicCreditRole role)
        => Role = role;
}
