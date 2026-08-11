namespace ApiMusicX.Models;

public sealed class ExternalSourceIdentifier
{
    private ExternalSourceIdentifier()
    {
    }

    public ExternalSourceIdentifier(string provider, string resourceType, string externalId)
    {
        Provider = MusicTextNormalizer.NormalizeExternalValue(provider, nameof(provider));
        ResourceType = MusicTextNormalizer.NormalizeExternalValue(resourceType, nameof(resourceType));
        ExternalId = MusicTextNormalizer.NormalizeExternalValue(externalId, nameof(externalId));

        if (Provider.Length > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(provider), "O provedor deve ter no maximo 64 caracteres.");
        }

        if (ResourceType.Length > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(resourceType), "O tipo do recurso deve ter no maximo 64 caracteres.");
        }

        if (ExternalId.Length > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(externalId), "O identificador deve ter no maximo 256 caracteres.");
        }
    }

    public long ExternalSourceIdentifierId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public string ExternalId { get; private set; } = string.Empty;

    public long? MusicArtistId { get; private set; }

    public long? MusicCollectionId { get; private set; }

    public long? MusicReleaseId { get; private set; }

    public long? MusicTrackId { get; private set; }

    public MusicArtist? MusicArtist { get; set; }

    public MusicCollection? MusicCollection { get; set; }

    public MusicRelease? MusicRelease { get; set; }

    public MusicTrack? MusicTrack { get; set; }
}
