using ApiDiscogs.Dtos;
using ApiDiscogs.Mappers;

namespace ApiDiscogs.Services;

/// <summary>
/// Contrato de leitura normalizada da fonte externa Discogs.
/// </summary>
public interface IDiscogsService
{
    /// <summary>
    /// Busca artistas e bandas de forma paginada.
    /// </summary>
    Task<DiscogsPagedResponse<DiscogsArtistSearchItem>> SearchArtistsAsync(
        ArtistSearchQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtem os detalhes de um artista.
    /// </summary>
    Task<DiscogsArtistDetails> GetArtistAsync(
        int artistId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtem uma pagina da discografia de um artista.
    /// </summary>
    Task<DiscogsArtistReleasesResponse> GetArtistReleasesAsync(
        int artistId,
        ArtistReleasesQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtem os detalhes de um release concreto.
    /// </summary>
    Task<DiscogsReleaseDetails> GetReleaseAsync(
        int releaseId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtem os detalhes de um master release.
    /// </summary>
    Task<DiscogsMasterDetails> GetMasterAsync(
        int masterId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Implementa as leituras da Discogs e a conversao para os DTOs da aplicacao.
/// </summary>
public sealed class DiscogsService(DiscogsClient client) : IDiscogsService
{
    /// <inheritdoc />
    public async Task<DiscogsPagedResponse<DiscogsArtistSearchItem>> SearchArtistsAsync(
        ArtistSearchQuery query,
        CancellationToken cancellationToken)
    {
        DiscogsRequestValidator.ValidateArtistSearch(query);
        var normalizedQuery = DiscogsRequestValidator.NormalizeSearchQuery(query);
        using var document = await client.SearchArtistsAsync(
            normalizedQuery,
            query.Page,
            query.PerPage,
            cancellationToken);
        return DiscogsMapper.MapArtistSearch(document);
    }

    /// <inheritdoc />
    public async Task<DiscogsArtistDetails> GetArtistAsync(
        int artistId,
        CancellationToken cancellationToken)
    {
        ValidateId(artistId);
        using var document = await client.GetArtistAsync(artistId, cancellationToken);
        return DiscogsMapper.MapArtistDetails(document);
    }

    /// <inheritdoc />
    public async Task<DiscogsArtistReleasesResponse> GetArtistReleasesAsync(
        int artistId,
        ArtistReleasesQuery query,
        CancellationToken cancellationToken)
    {
        ValidateId(artistId);
        var expand = DiscogsRequestValidator.ValidateArtistReleases(query);
        using var document = await client.GetArtistReleasesAsync(
            artistId,
            query.Page,
            query.PerPage,
            expand,
            cancellationToken);
        return DiscogsMapper.MapArtistReleases(document, artistId);
    }

    /// <inheritdoc />
    public async Task<DiscogsReleaseDetails> GetReleaseAsync(
        int releaseId,
        CancellationToken cancellationToken)
    {
        ValidateId(releaseId);
        using var document = await client.GetReleaseAsync(releaseId, cancellationToken);
        return DiscogsMapper.MapReleaseDetails(document);
    }

    /// <inheritdoc />
    public async Task<DiscogsMasterDetails> GetMasterAsync(
        int masterId,
        CancellationToken cancellationToken)
    {
        ValidateId(masterId);
        using var document = await client.GetMasterAsync(masterId, cancellationToken);
        return DiscogsMapper.MapMasterDetails(document);
    }

    private static void ValidateId(int id)
    {
        if (id <= 0)
        {
            throw new DiscogsValidationException("O ID Discogs deve ser positivo.");
        }
    }
}
