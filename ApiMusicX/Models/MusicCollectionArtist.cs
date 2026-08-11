namespace ApiMusicX.Models;

public sealed class MusicCollectionArtist
{
    private MusicCollectionArtist()
    {
    }

    public MusicCollectionArtist(
        MusicCollection collection,
        MusicArtist artist,
        MusicCollectionArtistRole role = MusicCollectionArtistRole.Unknown)
    {
        MusicCollection = collection ?? throw new ArgumentNullException(nameof(collection));
        MusicArtist = artist ?? throw new ArgumentNullException(nameof(artist));
        Role = role;
    }

    public long MusicCollectionId { get; private set; }

    public long MusicArtistId { get; private set; }

    public MusicCollectionArtistRole Role { get; private set; }

    public MusicCollection MusicCollection { get; private set; } = null!;

    public MusicArtist MusicArtist { get; private set; } = null!;

        public void UpdateRole(MusicCollectionArtistRole role)
            => Role = role;
}
