using ApiMusicX.Data;
using ApiMusicX.Dtos;
using ApiMusicX.Mappers;
using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiMusicX.Services;

/// <summary>
/// Implementa consultas e operacoes administrativas da Colecao local.
/// </summary>
public sealed class MusicCollectionService(
    MusicContext context,
    ILogger<MusicCollectionService> logger) : IMusicCollectionService
{
    private const int MaximumPageSize = 100;

    /// <inheritdoc />
    public async Task<PagedResponse<MusicCollectionSummaryDto>> ListCollectionsAsync(
        MusicCollectionQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidatePage(query.Page, query.PageSize);
        var normalizedSearch = NormalizeSearch(query.Search);

        var collectionsQuery = context.MusicCollections.AsNoTracking();
        if (normalizedSearch is not null)
        {
            collectionsQuery = collectionsQuery.Where(collection =>
                collection.NormalizedName.Contains(normalizedSearch)
                || collection.ArtistLinks.Any(link =>
                    link.MusicArtist.NormalizedName.Contains(normalizedSearch)
                    || link.MusicArtist.Aliases.Any(alias => alias.NormalizedValue.Contains(normalizedSearch))));
        }

        var totalCount = await collectionsQuery.CountAsync(cancellationToken);
        var collections = await collectionsQuery
            .Include(collection => collection.ArtistLinks)
                .ThenInclude(link => link.MusicArtist)
            .Include(collection => collection.ReleaseLinks)
            .OrderBy(collection => collection.NormalizedName)
            .ThenBy(collection => collection.MusicCollectionId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResponse<MusicCollectionSummaryDto>(
            collections.Select(MusicMapper.ToSummary).ToList(),
            query.Page,
            query.PageSize,
            totalCount);
    }

    /// <inheritdoc />
    public async Task<MusicCollectionDto?> GetCollectionAsync(
        long id,
        CancellationToken cancellationToken)
    {
        ValidateId(id, "Colecao");
        var collection = await CollectionDetailsQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.MusicCollectionId == id, cancellationToken);

        return collection is null ? null : MusicMapper.ToDto(collection);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<MusicReleaseDto>> ListCollectionReleasesAsync(
        long collectionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidateId(collectionId, "Colecao");
        ValidatePage(page, pageSize);

        var collectionExists = await context.MusicCollections
            .AsNoTracking()
            .AnyAsync(collection => collection.MusicCollectionId == collectionId, cancellationToken);
        if (!collectionExists)
        {
            throw new MusicNotFoundException($"Colecao com ID {collectionId} nao encontrada.");
        }

        var releasesQuery = ReleaseDetailsQuery()
            .Where(release => release.CollectionLinks.Any(link => link.MusicCollectionId == collectionId));
        var totalCount = await releasesQuery.CountAsync(cancellationToken);
        var releases = await releasesQuery
            .OrderBy(release => release.Title)
            .ThenBy(release => release.MusicReleaseId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<MusicReleaseDto>(
            releases.Select(MusicMapper.ToDto).ToList(),
            page,
            pageSize,
            totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<MusicArtistSummaryDto>> SearchArtistsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var normalizedSearch = NormalizeSearch(search);
        var artistsQuery = context.MusicArtists.AsNoTracking();
        if (normalizedSearch is not null)
        {
            artistsQuery = artistsQuery.Where(artist =>
                artist.NormalizedName.Contains(normalizedSearch)
                || artist.Aliases.Any(alias => alias.NormalizedValue.Contains(normalizedSearch)));
        }

        var totalCount = await artistsQuery.CountAsync(cancellationToken);
        var artists = await artistsQuery
            .OrderBy(artist => artist.NormalizedName)
            .ThenBy(artist => artist.MusicArtistId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<MusicArtistSummaryDto>(
            artists.Select(MusicMapper.ToSummary).ToList(),
            page,
            pageSize,
            totalCount);
    }

    /// <inheritdoc />
    public async Task<MusicArtistDto?> GetArtistAsync(
        long id,
        CancellationToken cancellationToken)
    {
        ValidateId(id, "Artista");
        var artist = await context.MusicArtists
            .Include(item => item.Aliases)
            .Include(item => item.ExternalIdentifiers)
            .Include(item => item.CollectionLinks)
                .ThenInclude(link => link.MusicCollection)
                    .ThenInclude(collection => collection.ArtistLinks)
                        .ThenInclude(link => link.MusicArtist)
            .Include(item => item.CollectionLinks)
                .ThenInclude(link => link.MusicCollection)
                    .ThenInclude(collection => collection.ReleaseLinks)
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.MusicArtistId == id, cancellationToken);

        return artist is null ? null : MusicMapper.ToDto(artist);
    }

    /// <inheritdoc />
    public async Task<MusicReleaseDto?> GetReleaseAsync(
        long id,
        CancellationToken cancellationToken)
    {
        ValidateId(id, "Release");
        var release = await ReleaseDetailsQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.MusicReleaseId == id, cancellationToken);

        return release is null ? null : MusicMapper.ToDto(release);
    }

    /// <inheritdoc />
    public async Task<MusicCollectionDto> CreateCollectionAsync(
        CreateMusicCollectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCollectionArtistRequests(request.Artists);

        var artistIds = request.Artists
            .Select(artist => artist.MusicArtistId)
            .Distinct()
            .ToList();
        var artists = await context.MusicArtists
            .Where(artist => artistIds.Contains(artist.MusicArtistId))
            .ToDictionaryAsync(artist => artist.MusicArtistId, cancellationToken);
        var missingArtistId = artistIds.FirstOrDefault(id => !artists.ContainsKey(id));
        if (missingArtistId > 0)
        {
            throw new MusicNotFoundException($"Artista com ID {missingArtistId} nao encontrado.");
        }

        var collection = new MusicCollection(request.DisplayName, request.Description);
        foreach (var artistRequest in request.Artists.DistinctBy(artist => artist.MusicArtistId))
        {
            collection.ArtistLinks.Add(new MusicCollectionArtist(
                collection,
                artists[artistRequest.MusicArtistId],
                artistRequest.Role));
        }

        context.MusicCollections.Add(collection);
        await SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Colecao local criada. CollectionId {MusicCollectionId} ArtistCount {ArtistCount}",
            collection.MusicCollectionId,
            collection.ArtistLinks.Count);

        return (await GetCollectionAsync(collection.MusicCollectionId, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task UpdateCollectionAsync(
        long id,
        UpdateMusicCollectionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateId(id, "Colecao");
        ArgumentNullException.ThrowIfNull(request);

        var collection = await context.MusicCollections
            .SingleOrDefaultAsync(item => item.MusicCollectionId == id, cancellationToken);
        if (collection is null)
        {
            throw new MusicNotFoundException($"Colecao com ID {id} nao encontrada.");
        }

        var changed = !string.Equals(collection.DisplayName, request.DisplayName.Trim(), StringComparison.Ordinal)
            || !string.Equals(collection.Description, request.Description?.Trim(), StringComparison.Ordinal);
        collection.UpdateDetails(request.DisplayName, request.Description);
        await SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Colecao local atualizada. CollectionId {MusicCollectionId} Changed {Changed}",
            id,
            changed);
    }

    /// <inheritdoc />
    public async Task DeleteCollectionAsync(long id, CancellationToken cancellationToken)
    {
        ValidateId(id, "Colecao");
        var collection = await context.MusicCollections
            .Include(item => item.ExternalIdentifiers)
            .SingleOrDefaultAsync(item => item.MusicCollectionId == id, cancellationToken);
        if (collection is null)
        {
            throw new MusicNotFoundException($"Colecao com ID {id} nao encontrada.");
        }

        context.ExternalSourceIdentifiers.RemoveRange(collection.ExternalIdentifiers);
        context.MusicCollections.Remove(collection);
        await SaveChangesAsync(cancellationToken);

        logger.LogInformation("Colecao local removida. CollectionId {MusicCollectionId}", id);
    }

    private IQueryable<MusicCollection> CollectionDetailsQuery()
        => context.MusicCollections
            .Include(collection => collection.ExternalIdentifiers)
            .Include(collection => collection.ArtistLinks)
                .ThenInclude(link => link.MusicArtist)
                    .ThenInclude(artist => artist.Aliases)
            .Include(collection => collection.ArtistLinks)
                .ThenInclude(link => link.MusicArtist)
                    .ThenInclude(artist => artist.ExternalIdentifiers)
            .Include(collection => collection.ReleaseLinks)
                .ThenInclude(link => link.MusicRelease)
                    .ThenInclude(release => release.ExternalIdentifiers)
            .Include(collection => collection.ReleaseLinks)
                .ThenInclude(link => link.MusicRelease)
                    .ThenInclude(release => release.ArtistCredits)
                        .ThenInclude(credit => credit.MusicArtist)
            .Include(collection => collection.ReleaseLinks)
                .ThenInclude(link => link.MusicRelease)
                    .ThenInclude(release => release.Tracks)
                        .ThenInclude(track => track.ExternalIdentifiers)
            .Include(collection => collection.ReleaseLinks)
                .ThenInclude(link => link.MusicRelease)
                    .ThenInclude(release => release.Tracks)
                        .ThenInclude(track => track.ArtistCredits)
                            .ThenInclude(credit => credit.MusicArtist)
            .Include(collection => collection.ReleaseLinks)
                .ThenInclude(link => link.MusicRelease)
                    .ThenInclude(release => release.Tracks)
                        .ThenInclude(track => track.LocalFileReferences)
            .Include(collection => collection.ReleaseLinks)
                .ThenInclude(link => link.MusicRelease)
                    .ThenInclude(release => release.LocalFileReferences)
            .AsSplitQuery();

    private IQueryable<MusicRelease> ReleaseDetailsQuery()
        => context.MusicReleases
            .Include(release => release.ExternalIdentifiers)
            .Include(release => release.ArtistCredits)
                .ThenInclude(credit => credit.MusicArtist)
            .Include(release => release.Tracks)
                .ThenInclude(track => track.ExternalIdentifiers)
            .Include(release => release.Tracks)
                .ThenInclude(track => track.ArtistCredits)
                    .ThenInclude(credit => credit.MusicArtist)
            .Include(release => release.Tracks)
                .ThenInclude(track => track.LocalFileReferences)
            .Include(release => release.LocalFileReferences)
            .AsSplitQuery();

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Constraint da Colecao local rejeitou a operacao.");
            throw new MusicConflictException("A operacao viola uma identidade ou vinculacao ja existente.");
        }
    }

    private static void ValidateCollectionArtistRequests(
        IReadOnlyCollection<MusicCollectionArtistRequest> requests)
    {
        foreach (var request in requests)
        {
            if (!Enum.IsDefined(request.Role))
            {
                throw new MusicValidationException("O papel do artista na Colecao e invalido.");
            }
        }
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw new MusicValidationException(
                $"A pagina deve ser positiva e pageSize deve estar entre 1 e {MaximumPageSize}.");
        }
    }

    private static void ValidateId(long id, string resourceName)
    {
        if (id <= 0)
        {
            throw new MusicValidationException($"O ID de {resourceName} deve ser positivo.");
        }
    }

    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        try
        {
            return MusicTextNormalizer.NormalizeSearchText(search);
        }
        catch (ArgumentException exception)
        {
            throw new MusicValidationException(exception.Message);
        }
    }
}
