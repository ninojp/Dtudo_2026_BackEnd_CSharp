using ApiMusicX.Dtos;

namespace ApiMusicX.Services;

/// <summary>
/// Opera sobre consultas e alteracoes da Colecao local.
/// </summary>
public interface IMusicCollectionService
{
    /// <summary>
    /// Lista Colecoes com pagina e filtro opcionais.
    /// </summary>
    Task<PagedResponse<MusicCollectionSummaryDto>> ListCollectionsAsync(
        MusicCollectionQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtem uma Colecao completa pelo ID local.
    /// </summary>
    Task<MusicCollectionDto?> GetCollectionAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os releases de uma Colecao, incluindo faixas.
    /// </summary>
    Task<PagedResponse<MusicReleaseDto>> ListCollectionReleasesAsync(
        long collectionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Busca artistas, bandas e grupos por nome ou alias.
    /// </summary>
    Task<PagedResponse<MusicArtistSummaryDto>> SearchArtistsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtem um artista completo pelo ID local.
    /// </summary>
    Task<MusicArtistDto?> GetArtistAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtem um release completo com suas faixas.
    /// </summary>
    Task<MusicReleaseDto?> GetReleaseAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Cria uma Colecao local.
    /// </summary>
    Task<MusicCollectionDto> CreateCollectionAsync(
        CreateMusicCollectionRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atualiza os metadados de uma Colecao existente.
    /// </summary>
    Task UpdateCollectionAsync(
        long id,
        UpdateMusicCollectionRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Remove uma Colecao sem remover releases compartilhados.
    /// </summary>
    Task DeleteCollectionAsync(long id, CancellationToken cancellationToken);
}
