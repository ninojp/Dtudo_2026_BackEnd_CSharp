using System.ComponentModel.DataAnnotations;
using ApiMusicX.Models;

namespace ApiMusicX.Dtos;

/// <summary>
/// Parametros de paginacao e filtro para consultas de Colecoes locais.
/// </summary>
public sealed class MusicCollectionQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [StringLength(256)]
    public string? Search { get; init; }
}

/// <summary>
/// Resultado paginado de uma consulta.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Resumo de uma Colecao para listagens e vinculos.
/// </summary>
public sealed record MusicCollectionSummaryDto(
    long MusicCollectionId,
    string DisplayName,
    string? Description,
    IReadOnlyList<MusicArtistSummaryDto> Artists,
    int ReleaseCount);

/// <summary>
/// Representacao completa de uma Colecao local.
/// </summary>
public sealed record MusicCollectionDto(
    long MusicCollectionId,
    string DisplayName,
    string? Description,
    IReadOnlyList<MusicArtistSummaryDto> Artists,
    IReadOnlyList<MusicReleaseDto> Releases,
    IReadOnlyList<ExternalSourceIdentifierDto> ExternalIdentifiers);

/// <summary>
/// Resumo de um artista, banda ou grupo.
/// </summary>
public sealed record MusicArtistSummaryDto(
    long MusicArtistId,
    string DisplayName,
    MusicArtistType ArtistType);

/// <summary>
/// Detalhes de um artista, incluindo aliases e Colecoes relacionadas.
/// </summary>
public sealed record MusicArtistDto(
    long MusicArtistId,
    string DisplayName,
    MusicArtistType ArtistType,
    string? SortName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<MusicCollectionSummaryDto> Collections,
    IReadOnlyList<ExternalSourceIdentifierDto> ExternalIdentifiers);

/// <summary>
/// Release local com artistas, faixas e referencias de arquivos.
/// </summary>
public sealed record MusicReleaseDto(
    long MusicReleaseId,
    string Title,
    MusicReleaseType ReleaseType,
    int? ReleaseYear,
    string? Notes,
    IReadOnlyList<MusicArtistSummaryDto> Artists,
    IReadOnlyList<MusicTrackDto> Tracks,
    IReadOnlyList<MusicLocalFileReferenceDto> LocalFileReferences,
    IReadOnlyList<ExternalSourceIdentifierDto> ExternalIdentifiers);

/// <summary>
/// Faixa local de um release.
/// </summary>
public sealed record MusicTrackDto(
    long MusicTrackId,
    string? PositionLabel,
    int? Sequence,
    string Title,
    int? DurationSeconds,
    string? DurationText,
    string? Notes,
    IReadOnlyList<MusicArtistSummaryDto> Artists,
    IReadOnlyList<MusicLocalFileReferenceDto> LocalFileReferences,
    IReadOnlyList<ExternalSourceIdentifierDto> ExternalIdentifiers);

/// <summary>
/// Referencia relativa a um arquivo ja existente fora da API.
/// </summary>
public sealed record MusicLocalFileReferenceDto(
    long MusicLocalFileReferenceId,
    string RelativePath,
    MusicMediaKind MediaKind,
    MusicLocalFileRole Role,
    long? MusicTrackId);

/// <summary>
/// Identificador de uma fonte externa associado a uma entidade local.
/// </summary>
public sealed record ExternalSourceIdentifierDto(
    string Provider,
    string ResourceType,
    string ExternalId);

/// <summary>
/// Dados para criar uma Colecao vinculada a artistas existentes.
/// </summary>
public sealed class CreateMusicCollectionRequest
{
    [Required, StringLength(256)]
    public string DisplayName { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [MaxLength(100)]
    public List<MusicCollectionArtistRequest> Artists { get; init; } = [];
}

/// <summary>
/// Dados para atualizar os metadados de uma Colecao.
/// </summary>
public sealed class UpdateMusicCollectionRequest
{
    [Required, StringLength(256)]
    public string DisplayName { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }
}

/// <summary>
/// Vinculo entre uma Colecao e um artista ja persistido.
/// </summary>
public sealed class MusicCollectionArtistRequest
{
    [Range(1, long.MaxValue)]
    public long MusicArtistId { get; init; }

    public MusicCollectionArtistRole Role { get; init; } = MusicCollectionArtistRole.Primary;
}

/// <summary>
/// Conjunto normalizado que pode ser importado pelo WinAppDtudo.
/// </summary>
public sealed class ImportMusicCollectionRequest
{
    [Range(1, long.MaxValue)]
    public long? MusicCollectionId { get; init; }

    [Required, StringLength(256)]
    public string DisplayName { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [MaxLength(20)]
    public List<ExternalSourceIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    [MinLength(1), MaxLength(100)]
    public List<MusicArtistImportRequest> Artists { get; init; } = [];

    [MaxLength(1000)]
    public List<MusicReleaseImportRequest> Releases { get; init; } = [];
}

/// <summary>
/// Artista normalizado para importacao.
/// </summary>
public sealed class MusicArtistImportRequest
{
    [Range(1, long.MaxValue)]
    public long? MusicArtistId { get; init; }

    [StringLength(256)]
    public string? DisplayName { get; init; }

    public MusicArtistType ArtistType { get; init; } = MusicArtistType.Unknown;

    [StringLength(256)]
    public string? SortName { get; init; }

    [MaxLength(50)]
    public List<string> Aliases { get; init; } = [];

    [MaxLength(20)]
    public List<ExternalSourceIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    public MusicCollectionArtistRole CollectionRole { get; init; } = MusicCollectionArtistRole.Primary;
}

/// <summary>
/// Release normalizado para importacao.
/// </summary>
public sealed class MusicReleaseImportRequest
{
    [Range(1, long.MaxValue)]
    public long? MusicReleaseId { get; init; }

    [Required, StringLength(512)]
    public string Title { get; init; } = string.Empty;

    public MusicReleaseType ReleaseType { get; init; } = MusicReleaseType.Unknown;

    [Range(1000, 9999)]
    public int? ReleaseYear { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }

    [StringLength(64)]
    public string? SourceCategory { get; init; }

    [Range(0, int.MaxValue)]
    public int? DisplayOrder { get; init; }

    [MaxLength(20)]
    public List<ExternalSourceIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    [MaxLength(100)]
    public List<MusicArtistCreditImportRequest> ArtistCredits { get; init; } = [];

    [MaxLength(1000)]
    public List<MusicTrackImportRequest> Tracks { get; init; } = [];

    [MaxLength(1000)]
    public List<MusicLocalFileReferenceImportRequest> LocalFileReferences { get; init; } = [];
}

/// <summary>
/// Credito de artista normalizado para um release ou faixa.
/// </summary>
public sealed class MusicArtistCreditImportRequest
{
    [Range(1, long.MaxValue)]
    public long? MusicArtistId { get; init; }

    [StringLength(256)]
    public string? DisplayName { get; init; }

    public MusicArtistType ArtistType { get; init; } = MusicArtistType.Unknown;

    [MaxLength(20)]
    public List<ExternalSourceIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    public MusicCreditRole Role { get; init; } = MusicCreditRole.Unknown;
}

/// <summary>
/// Faixa normalizada para importacao.
/// </summary>
public sealed class MusicTrackImportRequest
{
    [Required, StringLength(512)]
    public string Title { get; init; } = string.Empty;

    [StringLength(64)]
    public string? PositionLabel { get; init; }

    [Range(0, int.MaxValue)]
    public int? Sequence { get; init; }

    [Range(0, int.MaxValue)]
    public int? DurationSeconds { get; init; }

    [StringLength(32)]
    public string? DurationText { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }

    [MaxLength(20)]
    public List<ExternalSourceIdentifierRequest> ExternalIdentifiers { get; init; } = [];

    [MaxLength(100)]
    public List<MusicArtistCreditImportRequest> ArtistCredits { get; init; } = [];

    [MaxLength(1000)]
    public List<MusicLocalFileReferenceImportRequest> LocalFileReferences { get; init; } = [];
}

/// <summary>
/// Referencia relativa a arquivo recebida em uma importacao.
/// </summary>
public sealed class MusicLocalFileReferenceImportRequest
{
    [Required, StringLength(1024)]
    public string RelativePath { get; init; } = string.Empty;

    public MusicMediaKind MediaKind { get; init; } = MusicMediaKind.Other;

    public MusicLocalFileRole Role { get; init; } = MusicLocalFileRole.Unknown;

    [Range(1, long.MaxValue)]
    public long? MusicTrackId { get; init; }
}

/// <summary>
/// Identificador externo recebido em um contrato normalizado.
/// </summary>
public sealed class ExternalSourceIdentifierRequest
{
    [Required, StringLength(64)]
    public string Provider { get; init; } = string.Empty;

    [Required, StringLength(64)]
    public string ResourceType { get; init; } = string.Empty;

    [Required, StringLength(256)]
    public string ExternalId { get; init; } = string.Empty;
}

/// <summary>
/// Resultado de uma importacao idempotente e nao destrutiva.
/// </summary>
public sealed record ImportMusicCollectionResponse(
    MusicCollectionDto Collection,
    bool Created,
    bool Changed,
    int ArtistsAdded,
    int ReleasesAdded,
    int TracksAdded,
    int LocalFileReferencesAdded);
