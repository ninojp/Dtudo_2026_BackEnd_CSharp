namespace ApiMusicX.Models;

public sealed class MusicArtistAlias
{
    private MusicArtistAlias()
    {
    }

    public MusicArtistAlias(MusicArtist artist, string value)
    {
        MusicArtist = artist ?? throw new ArgumentNullException(nameof(artist));
        Value = MusicArtist.RequireText(value, nameof(value), 256);
        NormalizedValue = MusicTextNormalizer.NormalizeSearchText(value);
    }

    public long MusicArtistAliasId { get; private set; }

    public long MusicArtistId { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public string NormalizedValue { get; private set; } = string.Empty;

    public MusicArtist MusicArtist { get; private set; } = null!;
}
