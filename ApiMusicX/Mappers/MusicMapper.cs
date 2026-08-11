using ApiMusicX.Dtos;
using ApiMusicX.Models;

namespace ApiMusicX.Mappers;

/// <summary>
/// Converte entidades persistentes da ApiMusicX em contratos publicos.
/// </summary>
public static class MusicMapper
{
    /// <summary>
    /// Converte uma Colecao em um resumo de listagem.
    /// </summary>
    public static MusicCollectionSummaryDto ToSummary(MusicCollection collection)
        => new(
            collection.MusicCollectionId,
            collection.DisplayName,
            collection.Description,
            collection.ArtistLinks
                .OrderBy(link => link.MusicArtist.DisplayName)
                .Select(link => ToSummary(link.MusicArtist))
                .ToList(),
            collection.ReleaseLinks.Count);

    /// <summary>
    /// Converte uma Colecao completa, incluindo releases e faixas carregados.
    /// </summary>
    public static MusicCollectionDto ToDto(MusicCollection collection)
        => new(
            collection.MusicCollectionId,
            collection.DisplayName,
            collection.Description,
            collection.ArtistLinks
                .OrderBy(link => link.MusicArtist.DisplayName)
                .Select(link => ToSummary(link.MusicArtist))
                .ToList(),
            collection.ReleaseLinks
                .OrderBy(link => link.DisplayOrder is null)
                .ThenBy(link => link.DisplayOrder)
                .ThenBy(link => link.MusicRelease.Title)
                .Select(link => ToDto(link.MusicRelease))
                .ToList(),
            collection.ExternalIdentifiers
                .OrderBy(identifier => identifier.Provider)
                .ThenBy(identifier => identifier.ResourceType)
                .ThenBy(identifier => identifier.ExternalId)
                .Select(ToDto)
                .ToList());

    /// <summary>
    /// Converte um artista para o contrato resumido.
    /// </summary>
    public static MusicArtistSummaryDto ToSummary(MusicArtist artist)
        => new(artist.MusicArtistId, artist.DisplayName, artist.ArtistType);

    /// <summary>
    /// Converte um artista com seus aliases e Colecoes relacionadas.
    /// </summary>
    public static MusicArtistDto ToDto(MusicArtist artist)
        => new(
            artist.MusicArtistId,
            artist.DisplayName,
            artist.ArtistType,
            artist.SortName,
            artist.Aliases
                .OrderBy(alias => alias.Value)
                .Select(alias => alias.Value)
                .ToList(),
            artist.CollectionLinks
                .OrderBy(link => link.MusicCollection.DisplayName)
                .Select(link => ToSummary(link.MusicCollection))
                .ToList(),
            artist.ExternalIdentifiers
                .OrderBy(identifier => identifier.Provider)
                .ThenBy(identifier => identifier.ResourceType)
                .ThenBy(identifier => identifier.ExternalId)
                .Select(ToDto)
                .ToList());

    /// <summary>
    /// Converte um release com suas faixas.
    /// </summary>
    public static MusicReleaseDto ToDto(MusicRelease release)
        => new(
            release.MusicReleaseId,
            release.Title,
            release.ReleaseType,
            release.ReleaseYear,
            release.Notes,
            release.ArtistCredits
                .OrderBy(credit => credit.MusicArtist.DisplayName)
                .Select(credit => ToSummary(credit.MusicArtist))
                .ToList(),
            release.Tracks
                .OrderBy(track => track.Sequence is null)
                .ThenBy(track => track.Sequence)
                .ThenBy(track => track.PositionLabel)
                .ThenBy(track => track.Title)
                .Select(ToDto)
                .ToList(),
            release.LocalFileReferences
                .OrderBy(reference => reference.NormalizedPath)
                .Select(ToDto)
                .ToList(),
            release.ExternalIdentifiers
                .OrderBy(identifier => identifier.Provider)
                .ThenBy(identifier => identifier.ResourceType)
                .ThenBy(identifier => identifier.ExternalId)
                .Select(ToDto)
                .ToList());

    /// <summary>
    /// Converte uma faixa para o contrato publico.
    /// </summary>
    public static MusicTrackDto ToDto(MusicTrack track)
        => new(
            track.MusicTrackId,
            track.PositionLabel,
            track.Sequence,
            track.Title,
            track.DurationSeconds,
            track.DurationText,
            track.Notes,
            track.ArtistCredits
                .OrderBy(credit => credit.MusicArtist.DisplayName)
                .Select(credit => ToSummary(credit.MusicArtist))
                .ToList(),
            track.LocalFileReferences
                .OrderBy(reference => reference.NormalizedPath)
                .Select(ToDto)
                .ToList(),
            track.ExternalIdentifiers
                .OrderBy(identifier => identifier.Provider)
                .ThenBy(identifier => identifier.ResourceType)
                .ThenBy(identifier => identifier.ExternalId)
                .Select(ToDto)
                .ToList());

    /// <summary>
    /// Converte uma referencia de arquivo sem acessar o caminho no disco.
    /// </summary>
    public static MusicLocalFileReferenceDto ToDto(MusicLocalFileReference reference)
        => new(
            reference.MusicLocalFileReferenceId,
            reference.RelativePath,
            reference.MediaKind,
            reference.Role,
            reference.MusicTrackId);

    /// <summary>
    /// Converte um identificador externo.
    /// </summary>
    public static ExternalSourceIdentifierDto ToDto(ExternalSourceIdentifier identifier)
        => new(identifier.Provider, identifier.ResourceType, identifier.ExternalId);
}
