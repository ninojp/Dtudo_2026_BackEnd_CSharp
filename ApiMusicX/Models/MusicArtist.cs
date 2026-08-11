namespace ApiMusicX.Models;

public sealed class MusicArtist
{
    private MusicArtist()
    {
    }

    public MusicArtist(
        string displayName,
        MusicArtistType artistType = MusicArtistType.Unknown,
        string? sortName = null)
    {
        DisplayName = RequireText(displayName, nameof(displayName), 256);
        NormalizedName = MusicTextNormalizer.NormalizeSearchText(displayName);
        ArtistType = artistType;
        SortName = NormalizeOptional(sortName, 256);
    }

    public long MusicArtistId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public MusicArtistType ArtistType { get; private set; }

    public string? SortName { get; private set; }

    public ICollection<MusicArtistAlias> Aliases { get; } = [];

    public ICollection<MusicCollectionArtist> CollectionLinks { get; } = [];

    public ICollection<MusicReleaseArtist> ReleaseCredits { get; } = [];

    public ICollection<MusicTrackArtist> TrackCredits { get; } = [];

    public ICollection<ExternalSourceIdentifier> ExternalIdentifiers { get; } = [];
    
        public void UpdateDetails(
            string displayName,
            MusicArtistType artistType,
            string? sortName)
        {
            DisplayName = RequireText(displayName, nameof(displayName), 256);
            NormalizedName = MusicTextNormalizer.NormalizeSearchText(displayName);
            ArtistType = artistType;
            SortName = NormalizeOptional(sortName, 256);
        }

    internal static string RequireText(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"O texto deve ter no maximo {maxLength} caracteres.");
        }

        return trimmed;
    }

    internal static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"O texto deve ter no maximo {maxLength} caracteres.");
        }

        return trimmed;
    }
}
