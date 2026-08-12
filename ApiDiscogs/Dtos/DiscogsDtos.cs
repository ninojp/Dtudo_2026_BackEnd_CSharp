using Microsoft.AspNetCore.Mvc;

namespace ApiDiscogs.Dtos;

/// <summary>
/// Parametros normalizados para a busca externa de artistas e bandas.
/// </summary>
public sealed class ArtistSearchQuery
{
    /// <summary>
    /// Termo textual enviado para a busca da Discogs.
    /// </summary>
    [FromQuery(Name = "q")]
    public string? Query { get; set; }

    /// <summary>
    /// Numero da pagina solicitada.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Quantidade maxima de resultados por pagina.
    /// </summary>
    public int PerPage { get; set; } = 10;
}

/// <summary>
/// Parametros normalizados para a discografia paginada de um artista.
/// </summary>
public sealed class ArtistReleasesQuery
{
    /// <summary>
    /// Numero da pagina solicitada.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Quantidade maxima de releases por pagina.
    /// </summary>
    public int PerPage { get; set; } = 50;

    /// <summary>
    /// Expansao opcional de dados de master release.
    /// </summary>
    public string Expand { get; set; } = "none";
}

/// <summary>
/// Identifica um recurso preservando sua origem externa.
/// </summary>
public sealed record DiscogsSourceReference(
    string Provider,
    string ResourceType,
    string Id,
    string? ResourceUrl);

/// <summary>
/// Paginacao normalizada de uma resposta externa.
/// </summary>
public sealed record DiscogsPagination(
    int Page,
    int PerPage,
    int? TotalItems,
    int? TotalPages,
    bool HasNextPage,
    int? UniqueItemsInPage);

/// <summary>
/// Resposta paginada comum aos recursos de leitura da Discogs.
/// </summary>
public sealed record DiscogsPagedResponse<T>(
    string Source,
    IReadOnlyList<T> Items,
    DiscogsPagination Pagination,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Artista retornado pela busca externa.
/// </summary>
public sealed record DiscogsArtistSearchItem(
    DiscogsSourceReference Source,
    string Name,
    string Type,
    string? ThumbnailUrl,
    string? ImageUrl);

/// <summary>
/// Referencia nominal a um artista, alias ou membro.
/// </summary>
public sealed record DiscogsNameReference(string? Id, string Name);

/// <summary>
/// Detalhes normalizados de um artista ou banda.
/// </summary>
public sealed record DiscogsArtistDetails(
    DiscogsSourceReference Source,
    string Name,
    string? RealName,
    string? Profile,
    IReadOnlyList<DiscogsNameReference> Aliases,
    IReadOnlyList<DiscogsNameReference> Members,
    IReadOnlyList<string> Urls,
    IReadOnlyList<DiscogsImage> Images,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Release ou master resumido na discografia de um artista.
/// </summary>
public sealed record DiscogsReleaseSummary(
    DiscogsSourceReference Source,
    string CanonicalId,
    string ResourceType,
    string Title,
    string? ArtistName,
    string? ArtistId,
    int? Year,
    string? MasterId,
    string? MainReleaseId,
    string? Role,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Formats,
    string Category,
    string? ThumbnailUrl,
    string? ImageUrl,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Discografia paginada normalizada de um artista.
/// </summary>
public sealed record DiscogsArtistReleasesResponse(
    string Source,
    DiscogsNameReference Artist,
    IReadOnlyList<DiscogsReleaseSummary> Items,
    DiscogsPagination Pagination,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Imagem externa validada para uso como dado de apresentacao.
/// </summary>
public sealed record DiscogsImage(
    string? Type,
    string? Uri,
    int? Width,
    int? Height);

/// <summary>
/// Credito de artista em release ou faixa.
/// </summary>
public sealed record DiscogsCredit(string? Id, string Name, string? Role);

/// <summary>
/// Label associada a um release.
/// </summary>
public sealed record DiscogsLabel(string Name, string? CatalogNumber, string? Id);

/// <summary>
/// Faixa normalizada de um release.
/// </summary>
public sealed record DiscogsTrack(
    string? Position,
    string Title,
    int? DurationSeconds,
    string? DurationText,
    IReadOnlyList<DiscogsCredit> Artists,
    IReadOnlyList<DiscogsCredit> ExtraArtists);

/// <summary>
/// Detalhes normalizados de um release concreto.
/// </summary>
public sealed record DiscogsReleaseDetails(
    DiscogsSourceReference Source,
    string Title,
    int? Year,
    string? Released,
    string? Country,
    string? Status,
    string? MasterId,
    IReadOnlyList<DiscogsCredit> Artists,
    IReadOnlyList<DiscogsLabel> Labels,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Styles,
    IReadOnlyList<string> Formats,
    IReadOnlyList<DiscogsTrack> Tracklist,
    IReadOnlyList<DiscogsImage> Images,
    string? Notes,
    bool IsComplete,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Detalhes normalizados de um master release.
/// </summary>
public sealed record DiscogsMasterDetails(
    DiscogsSourceReference Source,
    string Title,
    string? MainReleaseId,
    int? Year,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Styles,
    IReadOnlyList<DiscogsCredit> Artists,
    IReadOnlyList<DiscogsReleaseSummary> Versions,
    IReadOnlyList<DiscogsImage> Images,
    bool IsComplete,
    IReadOnlyList<string> Warnings);
