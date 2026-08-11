namespace ApiMusicX.Models;

public enum MusicArtistType
{
    Unknown = 0,
    Solo = 1,
    Band = 2,
    Group = 3
}

public enum MusicCollectionArtistRole
{
    Unknown = 0,
    Primary = 1,
    Member = 2,
    Associated = 3
}

public enum MusicReleaseType
{
    Unknown = 0,
    Album = 1,
    Single = 2,
    EP = 3,
    Compilation = 4,
    Video = 5
}

public enum MusicCreditRole
{
    Unknown = 0,
    Primary = 1,
    Featured = 2,
    Composer = 3
}

public enum MusicMediaKind
{
    Other = 0,
    Audio = 1,
    Image = 2,
    Document = 3
}

public enum MusicLocalFileRole
{
    Unknown = 0,
    TrackAudio = 1,
    Cover = 2,
    Booklet = 3,
    Artwork = 4
}
