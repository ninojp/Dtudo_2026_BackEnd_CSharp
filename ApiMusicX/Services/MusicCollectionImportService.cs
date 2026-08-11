using ApiMusicX.Data;
using ApiMusicX.Dtos;
using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiMusicX.Services;

/// <summary>
/// Aplica importacoes normalizadas sem depender da Discogs, do ApiNode ou do disco local.
/// </summary>
public sealed class MusicCollectionImportService(
    MusicContext context,
    IMusicCollectionService collectionService,
    ILogger<MusicCollectionImportService> logger) : IMusicCollectionImportService
{


    private async Task<MusicTrack> ResolveTrackAsync(
        MusicRelease release,
        MusicTrackImportRequest request,
        IReadOnlyList<ExternalIdentifierValue> identifiers,
        ImportState state,
        CancellationToken cancellationToken)
    {
        MusicTrack? track = null;
        foreach (var identifierValue in identifiers)
        {
            var identifier = await FindIdentifierAsync(identifierValue, cancellationToken);
            if (identifier is null)
            {
                continue;
            }

            if (identifier.MusicTrackId is null && identifier.MusicTrack is null)
            {
                throw new MusicConflictException(
                    $"O identificador {identifierValue.Provider}/{identifierValue.ResourceType}/{identifierValue.ExternalId} esta associado a outro tipo de recurso.");
            }

            var identifiedTrackId = identifier.MusicTrackId
                ?? identifier.MusicTrack!.MusicTrackId;
            var identifiedTrack = identifier.MusicTrack
                ?? await LoadTrackAsync(identifiedTrackId, cancellationToken)
                ?? throw new MusicConflictException(
                    "O identificador externo aponta para uma faixa que nao pode ser carregada.");
            if (identifiedTrack.MusicReleaseId > 0
                && release.MusicReleaseId > 0
                && identifiedTrack.MusicReleaseId != release.MusicReleaseId)
            {
                throw new MusicConflictException(
                    "O identificador da faixa aponta para outro release local.");
            }

            if (track is not null && !ReferenceEquals(track, identifiedTrack))
            {
                throw new MusicConflictException(
                    "Os identificadores da faixa apontam para faixas locais diferentes.");
            }

            track = identifiedTrack;
        }

        if (track is null)
        {
            var normalizedTitle = NormalizeRequiredSearchText(request.Title, "O titulo da faixa e obrigatorio.");
            var candidates = release.Tracks
                .Where(item => item.NormalizedTitle == normalizedTitle)
                .Where(item => request.PositionLabel is null || item.PositionLabel == request.PositionLabel.Trim())
                .Where(item => request.Sequence is null || item.Sequence == request.Sequence)
                .ToList();

            if (candidates.Count > 1)
            {
                throw new MusicConflictException(
                    $"Ha mais de uma faixa candidata para '{request.Title}'. Informe um identificador externo e a posicao.");
            }

            if (candidates.Count == 1)
            {
                if (request.PositionLabel is null && request.Sequence is null)
                {
                    throw new MusicConflictException(
                        $"A faixa '{request.Title}' ja existe, mas a importacao nao informou posicao ou sequencia para confirmar a mesclagem.");
                }

                track = candidates[0];
            }
        }

        if (track is null)
        {
            track = new MusicTrack(
                release,
                request.Title,
                request.PositionLabel,
                request.Sequence,
                request.DurationSeconds,
                request.DurationText,
                request.Notes);
            release.Tracks.Add(track);
            context.MusicTracks.Add(track);
            state.TracksAdded++;
            state.Changed = true;
        }
        else
        {
            ApplyTrackValues(track, request, state);
        }

        return track;
    }

    private async Task<MusicArtist> ResolveArtistAsync(
        long? artistId,
        string? displayName,
        MusicArtistType artistType,
        string? sortName,
        IEnumerable<string> aliases,
        IReadOnlyList<ExternalIdentifierValue> identifiers,
        ImportState state,
        CancellationToken cancellationToken)
    {
        ValidateArtistType(artistType);
        MusicArtist? artist = null;
        if (artistId is not null)
        {
            artist = await LoadArtistAsync(artistId.Value, cancellationToken);
            if (artist is null)
            {
                throw new MusicNotFoundException($"Artista com ID {artistId.Value} nao encontrado.");
            }
        }

        foreach (var identifierValue in identifiers)
        {
            var identifier = await FindIdentifierAsync(identifierValue, cancellationToken);
            if (identifier is null)
            {
                continue;
            }

            if (identifier.MusicArtistId is null && identifier.MusicArtist is null)
            {
                throw new MusicConflictException(
                    $"O identificador {identifierValue.Provider}/{identifierValue.ResourceType}/{identifierValue.ExternalId} esta associado a outro tipo de recurso.");
            }

            var identifiedArtistId = identifier.MusicArtistId
                ?? identifier.MusicArtist!.MusicArtistId;
            if (artist is not null && artist.MusicArtistId != identifiedArtistId)
            {
                throw new MusicConflictException(
                    "Os identificadores do artista apontam para artistas locais diferentes.");
            }

            artist ??= identifier.MusicArtist
                ?? await LoadArtistAsync(identifiedArtistId, cancellationToken)
                ?? throw new MusicConflictException(
                    "O identificador externo aponta para um artista que nao pode ser carregado.");
        }

        if (artist is null && !string.IsNullOrWhiteSpace(displayName))
        {
            var normalizedName = NormalizeRequiredSearchText(
                displayName,
                "O nome do artista e obrigatorio para criar um artista.");
            var candidates = await context.MusicArtists
                .Include(item => item.Aliases)
                .Include(item => item.ExternalIdentifiers)
                .Where(item => item.NormalizedName == normalizedName
                    || item.Aliases.Any(alias => alias.NormalizedValue == normalizedName))
                .ToListAsync(cancellationToken);
            if (candidates.Count > 1)
            {
                throw new MusicConflictException(
                    $"Ha mais de um artista candidato para '{displayName}'. Informe um identificador local ou externo.");
            }

            artist = candidates.SingleOrDefault();
        }

        if (artist is null)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new MusicValidationException(
                    "Cada artista novo precisa de DisplayName, MusicArtistId ou identificador externo.");
            }

            artist = new MusicArtist(displayName, artistType, sortName);
            context.MusicArtists.Add(artist);
            state.ArtistsAdded++;
            state.Changed = true;
        }
        else
        {
            ApplyArtistValues(artist, displayName, artistType, sortName, state);
        }

        foreach (var aliasValue in aliases.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var normalizedAlias = NormalizeRequiredSearchText(aliasValue, "O alias do artista e invalido.");
            if (artist.Aliases.Any(alias => alias.NormalizedValue == normalizedAlias))
            {
                continue;
            }

            artist.Aliases.Add(new MusicArtistAlias(artist, aliasValue));
            state.Changed = true;
        }

        return artist;
    }

    private static void ApplyCollectionValues(
        MusicCollection collection,
        ImportMusicCollectionRequest request,
        ImportState state)
    {
        var displayName = request.DisplayName.Trim();
        if (!string.Equals(collection.DisplayName, displayName, StringComparison.Ordinal))
        {
            throw new MusicConflictException(
                $"A Colecao local '{collection.DisplayName}' diverge do nome importado '{displayName}'.");
        }

        var incomingDescription = request.Description?.Trim();
        if (collection.Description is not null
            && incomingDescription is not null
            && !string.Equals(collection.Description, incomingDescription, StringComparison.Ordinal))
        {
            throw new MusicConflictException(
                $"A descricao local da Colecao '{collection.DisplayName}' diverge do valor importado.");
        }

        if (collection.Description is null && incomingDescription is not null)
        {
            collection.UpdateDetails(collection.DisplayName, incomingDescription);
            state.Changed = true;
        }
    }

    private static void ApplyCollectionArtistRole(
        MusicCollectionArtist link,
        MusicCollectionArtistRole incomingRole,
        ImportState state)
    {
        if (incomingRole == MusicCollectionArtistRole.Unknown)
        {
            return;
        }

        if (link.Role == MusicCollectionArtistRole.Unknown)
        {
            link.UpdateRole(incomingRole);
            state.Changed = true;
            return;
        }

        if (link.Role != incomingRole)
        {
            throw new MusicConflictException(
                "O papel do artista na Colecao local diverge do papel importado.");
        }
    }

    private static void ApplyReleaseValues(
        MusicRelease release,
        MusicReleaseImportRequest request,
        ImportState state)
    {
        var title = request.Title.Trim();
        if (!string.Equals(release.Title, title, StringComparison.Ordinal))
        {
            throw new MusicConflictException(
                $"O release local '{release.Title}' diverge do titulo importado '{title}'.");
        }

        var releaseType = release.ReleaseType;
        if (request.ReleaseType != MusicReleaseType.Unknown)
        {
            if (releaseType == MusicReleaseType.Unknown)
            {
                releaseType = request.ReleaseType;
            }
            else if (releaseType != request.ReleaseType)
            {
                throw new MusicConflictException(
                    $"O tipo do release '{release.Title}' diverge do valor importado.");
            }
        }

        var releaseYear = release.ReleaseYear;
        if (request.ReleaseYear is not null)
        {
            if (releaseYear is null)
            {
                releaseYear = request.ReleaseYear;
            }
            else if (releaseYear != request.ReleaseYear)
            {
                throw new MusicConflictException(
                    $"O ano do release '{release.Title}' diverge do valor importado.");
            }
        }

        var notes = release.Notes;
        if (request.Notes is not null)
        {
            var incomingNotes = request.Notes.Trim();
            if (notes is null)
            {
                notes = incomingNotes;
            }
            else if (!string.Equals(notes, incomingNotes, StringComparison.Ordinal))
            {
                throw new MusicConflictException(
                    $"As observacoes do release '{release.Title}' divergem do valor importado.");
            }
        }

        if (releaseType != release.ReleaseType
            || releaseYear != release.ReleaseYear
            || !string.Equals(notes, release.Notes, StringComparison.Ordinal))
        {
            release.UpdateDetails(release.Title, releaseType, releaseYear, notes);
            state.Changed = true;
        }
    }

    private static void ApplyCollectionReleaseMetadata(
        MusicCollectionRelease link,
        MusicReleaseImportRequest request,
        ImportState state)
    {
        var sourceCategory = link.SourceCategory;
        if (request.SourceCategory is not null)
        {
            var incomingCategory = request.SourceCategory.Trim();
            if (sourceCategory is null)
            {
                sourceCategory = incomingCategory;
            }
            else if (!string.Equals(sourceCategory, incomingCategory, StringComparison.Ordinal))
            {
                throw new MusicConflictException(
                    $"A categoria do release '{link.MusicRelease.Title}' diverge do valor importado.");
            }
        }

        var displayOrder = link.DisplayOrder;
        if (request.DisplayOrder is not null)
        {
            if (displayOrder is null)
            {
                displayOrder = request.DisplayOrder;
            }
            else if (displayOrder != request.DisplayOrder)
            {
                throw new MusicConflictException(
                    $"A ordem do release '{link.MusicRelease.Title}' diverge do valor importado.");
            }
        }

        if (!string.Equals(sourceCategory, link.SourceCategory, StringComparison.Ordinal)
            || displayOrder != link.DisplayOrder)
        {
            link.UpdateMetadata(sourceCategory, displayOrder);
            state.Changed = true;
        }
    }

    private static void ApplyArtistValues(
        MusicArtist artist,
        string? displayName,
        MusicArtistType incomingType,
        string? sortName,
        ImportState state)
    {
        var incomingName = displayName?.Trim();
        if (incomingName is not null
            && incomingName.Length > 0
            && !string.Equals(artist.DisplayName, incomingName, StringComparison.Ordinal))
        {
            throw new MusicConflictException(
                $"O artista local '{artist.DisplayName}' diverge do nome importado '{incomingName}'.");
        }

        var artistType = artist.ArtistType;
        if (incomingType != MusicArtistType.Unknown)
        {
            if (artistType == MusicArtistType.Unknown)
            {
                artistType = incomingType;
            }
            else if (artistType != incomingType)
            {
                throw new MusicConflictException(
                    $"O tipo do artista '{artist.DisplayName}' diverge do valor importado.");
            }
        }

        var targetSortName = artist.SortName;
        if (sortName is not null)
        {
            var incomingSortName = sortName.Trim();
            if (targetSortName is null)
            {
                targetSortName = incomingSortName;
            }
            else if (!string.Equals(targetSortName, incomingSortName, StringComparison.Ordinal))
            {
                throw new MusicConflictException(
                    $"O nome de ordenacao do artista '{artist.DisplayName}' diverge do valor importado.");
            }
        }

        if (artistType != artist.ArtistType
            || !string.Equals(targetSortName, artist.SortName, StringComparison.Ordinal))
        {
            artist.UpdateDetails(artist.DisplayName, artistType, targetSortName);
            state.Changed = true;
        }
    }

    private static void ApplyTrackValues(
        MusicTrack track,
        MusicTrackImportRequest request,
        ImportState state)
    {
        var title = request.Title.Trim();
        if (!string.Equals(track.Title, title, StringComparison.Ordinal))
        {
            throw new MusicConflictException(
                $"A faixa local '{track.Title}' diverge do titulo importado '{title}'.");
        }

        var positionLabel = track.PositionLabel;
        if (request.PositionLabel is not null)
        {
            var incomingPosition = request.PositionLabel.Trim();
            if (positionLabel is null)
            {
                positionLabel = incomingPosition;
            }
            else if (!string.Equals(positionLabel, incomingPosition, StringComparison.Ordinal))
            {
                throw new MusicConflictException(
                    $"A posicao da faixa '{track.Title}' diverge do valor importado.");
            }
        }

        var sequence = track.Sequence;
        if (request.Sequence is not null)
        {
            if (sequence is null)
            {
                sequence = request.Sequence;
            }
            else if (sequence != request.Sequence)
            {
                throw new MusicConflictException(
                    $"A sequencia da faixa '{track.Title}' diverge do valor importado.");
            }
        }

        var durationSeconds = track.DurationSeconds;
        if (request.DurationSeconds is not null)
        {
            if (durationSeconds is null)
            {
                durationSeconds = request.DurationSeconds;
            }
            else if (durationSeconds != request.DurationSeconds)
            {
                throw new MusicConflictException(
                    $"A duracao numerica da faixa '{track.Title}' diverge do valor importado.");
            }
        }

        var durationText = track.DurationText;
        if (request.DurationText is not null)
        {
            var incomingDurationText = request.DurationText.Trim();
            if (durationText is null)
            {
                durationText = incomingDurationText;
            }
            else if (!string.Equals(durationText, incomingDurationText, StringComparison.Ordinal))
            {
                throw new MusicConflictException(
                    $"A duracao textual da faixa '{track.Title}' diverge do valor importado.");
            }
        }

        var notes = track.Notes;
        if (request.Notes is not null)
        {
            var incomingNotes = request.Notes.Trim();
            if (notes is null)
            {
                notes = incomingNotes;
            }
            else if (!string.Equals(notes, incomingNotes, StringComparison.Ordinal))
            {
                throw new MusicConflictException(
                    $"As observacoes da faixa '{track.Title}' divergem do valor importado.");
            }
        }

        if (!string.Equals(positionLabel, track.PositionLabel, StringComparison.Ordinal)
            || sequence != track.Sequence
            || durationSeconds != track.DurationSeconds
            || !string.Equals(durationText, track.DurationText, StringComparison.Ordinal)
            || !string.Equals(notes, track.Notes, StringComparison.Ordinal))
        {
            track.UpdateDetails(track.Title, positionLabel, sequence, durationSeconds, durationText, notes);
            state.Changed = true;
        }
    }

    private static void ApplyCreditRole(
        MusicReleaseArtist credit,
        MusicCreditRole incomingRole,
        ImportState state)
    {
        if (incomingRole == MusicCreditRole.Unknown)
        {
            return;
        }

        if (credit.Role == MusicCreditRole.Unknown)
        {
            credit.UpdateRole(incomingRole);
            state.Changed = true;
            return;
        }

        if (credit.Role != incomingRole)
        {
            throw new MusicConflictException(
                "O papel do artista no release local diverge do papel importado.");
        }
    }

    private static void ApplyCreditRole(
        MusicTrackArtist credit,
        MusicCreditRole incomingRole,
        ImportState state)
    {
        if (incomingRole == MusicCreditRole.Unknown)
        {
            return;
        }

        if (credit.Role == MusicCreditRole.Unknown)
        {
            credit.UpdateRole(incomingRole);
            state.Changed = true;
            return;
        }

        if (credit.Role != incomingRole)
        {
            throw new MusicConflictException(
                "O papel do artista na faixa local diverge do papel importado.");
        }
    }

    private async Task AttachCollectionIdentifiersAsync(
        MusicCollection collection,
        IReadOnlyList<ExternalIdentifierValue> identifiers,
        ImportState state,
        CancellationToken cancellationToken)
    {
        foreach (var identifierValue in identifiers)
        {
            var identifier = await FindIdentifierAsync(identifierValue, cancellationToken);
            if (identifier is not null)
            {
                if (!Owns(identifier, collection))
                {
                    throw new MusicConflictException(
                        $"O identificador {identifierValue.Provider}/{identifierValue.ResourceType}/{identifierValue.ExternalId} ja pertence a outro recurso.");
                }

                continue;
            }

            var newIdentifier = new ExternalSourceIdentifier(
                identifierValue.Provider,
                identifierValue.ResourceType,
                identifierValue.ExternalId)
            {
                MusicCollection = collection
            };
            collection.ExternalIdentifiers.Add(newIdentifier);
            context.ExternalSourceIdentifiers.Add(newIdentifier);
            state.Changed = true;
        }
    }

    private async Task AttachArtistIdentifiersAsync(
        MusicArtist artist,
        IReadOnlyList<ExternalIdentifierValue> identifiers,
        ImportState state,
        CancellationToken cancellationToken)
    {
        foreach (var identifierValue in identifiers)
        {
            var identifier = await FindIdentifierAsync(identifierValue, cancellationToken);
            if (identifier is not null)
            {
                if (!Owns(identifier, artist))
                {
                    throw new MusicConflictException(
                        $"O identificador {identifierValue.Provider}/{identifierValue.ResourceType}/{identifierValue.ExternalId} ja pertence a outro recurso.");
                }

                continue;
            }

            var newIdentifier = new ExternalSourceIdentifier(
                identifierValue.Provider,
                identifierValue.ResourceType,
                identifierValue.ExternalId)
            {
                MusicArtist = artist
            };
            artist.ExternalIdentifiers.Add(newIdentifier);
            context.ExternalSourceIdentifiers.Add(newIdentifier);
            state.Changed = true;
        }
    }

    private async Task AttachReleaseIdentifiersAsync(
        MusicRelease release,
        IReadOnlyList<ExternalIdentifierValue> identifiers,
        ImportState state,
        CancellationToken cancellationToken)
    {
        foreach (var identifierValue in identifiers)
        {
            var identifier = await FindIdentifierAsync(identifierValue, cancellationToken);
            if (identifier is not null)
            {
                if (!Owns(identifier, release))
                {
                    throw new MusicConflictException(
                        $"O identificador {identifierValue.Provider}/{identifierValue.ResourceType}/{identifierValue.ExternalId} ja pertence a outro recurso.");
                }

                continue;
            }

            var newIdentifier = new ExternalSourceIdentifier(
                identifierValue.Provider,
                identifierValue.ResourceType,
                identifierValue.ExternalId)
            {
                MusicRelease = release
            };
            release.ExternalIdentifiers.Add(newIdentifier);
            context.ExternalSourceIdentifiers.Add(newIdentifier);
            state.Changed = true;
        }
    }

    private async Task AttachTrackIdentifiersAsync(
        MusicTrack track,
        IReadOnlyList<ExternalIdentifierValue> identifiers,
        ImportState state,
        CancellationToken cancellationToken)
    {
        foreach (var identifierValue in identifiers)
        {
            var identifier = await FindIdentifierAsync(identifierValue, cancellationToken);
            if (identifier is not null)
            {
                if (!Owns(identifier, track))
                {
                    throw new MusicConflictException(
                        $"O identificador {identifierValue.Provider}/{identifierValue.ResourceType}/{identifierValue.ExternalId} ja pertence a outro recurso.");
                }

                continue;
            }

            var newIdentifier = new ExternalSourceIdentifier(
                identifierValue.Provider,
                identifierValue.ResourceType,
                identifierValue.ExternalId)
            {
                MusicTrack = track
            };
            track.ExternalIdentifiers.Add(newIdentifier);
            context.ExternalSourceIdentifiers.Add(newIdentifier);
            state.Changed = true;
        }
    }

    private async Task AttachLocalFileReferenceAsync(
        MusicRelease release,
        MusicTrack? track,
        MusicLocalFileReferenceImportRequest request,
        ImportState state,
        CancellationToken cancellationToken)
    {
        if (request.MusicTrackId is not null)
        {
            track = release.Tracks.FirstOrDefault(item => item.MusicTrackId == request.MusicTrackId.Value);
            if (track is null)
            {
                throw new MusicNotFoundException(
                    $"A faixa com ID {request.MusicTrackId.Value} nao pertence ao release importado.");
            }
        }

        ValidateMediaValues(request.MediaKind, request.Role);
        string normalizedPath;
        try
        {
            normalizedPath = MusicTextNormalizer.NormalizeRelativePath(request.RelativePath);
        }
        catch (ArgumentException exception)
        {
            throw new MusicValidationException(exception.Message);
        }

        var references = context.ChangeTracker
            .Entries<MusicLocalFileReference>()
            .Select(entry => entry.Entity)
            .Where(reference => reference.NormalizedPath == normalizedPath)
            .ToList();
        var databaseReferences = await context.MusicLocalFileReferences
            .Where(reference => reference.NormalizedPath == normalizedPath)
            .ToListAsync(cancellationToken);
        foreach (var databaseReference in databaseReferences)
        {
            if (!references.Contains(databaseReference))
            {
                references.Add(databaseReference);
            }
        }

        foreach (var existing in references)
        {
            var sameRelease = ReferenceEquals(existing.MusicRelease, release)
                || existing.MusicReleaseId > 0
                    && release.MusicReleaseId > 0
                    && existing.MusicReleaseId == release.MusicReleaseId;
            var sameTrack = track is null
                ? existing.MusicTrackId is null
                : ReferenceEquals(existing.MusicTrack, track)
                    || existing.MusicTrackId > 0
                        && track.MusicTrackId > 0
                        && existing.MusicTrackId == track.MusicTrackId;
            if (!sameRelease || !sameTrack)
            {
                throw new MusicConflictException(
                    $"A referencia local '{normalizedPath}' ja pertence a outro release ou faixa.");
            }

            if (existing.MediaKind != request.MediaKind || existing.Role != request.Role)
            {
                throw new MusicConflictException(
                    $"Os metadados da referencia local '{normalizedPath}' divergem do valor importado.");
            }

            return;
        }

        var newReference = new MusicLocalFileReference(
            release,
            request.RelativePath,
            request.MediaKind,
            request.Role,
            track);
        release.LocalFileReferences.Add(newReference);
        if (track is not null)
        {
            track.LocalFileReferences.Add(newReference);
        }

        context.MusicLocalFileReferences.Add(newReference);
        state.LocalFileReferencesAdded++;
        state.Changed = true;
    }

    private Task<MusicCollection?> LoadCollectionAsync(
        long id,
        CancellationToken cancellationToken)
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
            .AsSplitQuery()
            .SingleOrDefaultAsync(collection => collection.MusicCollectionId == id, cancellationToken);

    private Task<MusicArtist?> LoadArtistAsync(
        long id,
        CancellationToken cancellationToken)
        => context.MusicArtists
            .Include(artist => artist.Aliases)
            .Include(artist => artist.ExternalIdentifiers)
            .SingleOrDefaultAsync(artist => artist.MusicArtistId == id, cancellationToken);

    private Task<MusicRelease?> LoadReleaseAsync(
        long id,
        CancellationToken cancellationToken)
        => context.MusicReleases
            .Include(release => release.ExternalIdentifiers)
            .Include(release => release.CollectionLinks)
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
            .AsSplitQuery()
            .SingleOrDefaultAsync(release => release.MusicReleaseId == id, cancellationToken);

    private Task<MusicTrack?> LoadTrackAsync(
        long id,
        CancellationToken cancellationToken)
        => context.MusicTracks
            .Include(track => track.ExternalIdentifiers)
            .Include(track => track.ArtistCredits)
                .ThenInclude(credit => credit.MusicArtist)
            .Include(track => track.LocalFileReferences)
            .SingleOrDefaultAsync(track => track.MusicTrackId == id, cancellationToken);

    private async Task<ExternalSourceIdentifier?> FindIdentifierAsync(
        ExternalIdentifierValue value,
        CancellationToken cancellationToken)
    {
        var tracked = context.ChangeTracker
            .Entries<ExternalSourceIdentifier>()
            .Select(entry => entry.Entity)
            .FirstOrDefault(identifier => SameIdentifier(identifier, value));
        return tracked ?? await context.ExternalSourceIdentifiers
            .SingleOrDefaultAsync(identifier =>
                identifier.Provider == value.Provider
                && identifier.ResourceType == value.ResourceType
                && identifier.ExternalId == value.ExternalId,
                cancellationToken);
    }

    private static bool SameIdentifier(
        ExternalSourceIdentifier identifier,
        ExternalIdentifierValue value)
        => string.Equals(identifier.Provider, value.Provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(identifier.ResourceType, value.ResourceType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(identifier.ExternalId, value.ExternalId, StringComparison.OrdinalIgnoreCase);

    private static bool Owns(
        ExternalSourceIdentifier identifier,
        MusicCollection collection)
        => ReferenceEquals(identifier.MusicCollection, collection)
            || identifier.MusicCollectionId is not null
                && collection.MusicCollectionId > 0
                && identifier.MusicCollectionId == collection.MusicCollectionId;

    private static bool Owns(
        ExternalSourceIdentifier identifier,
        MusicArtist artist)
        => ReferenceEquals(identifier.MusicArtist, artist)
            || identifier.MusicArtistId is not null
                && artist.MusicArtistId > 0
                && identifier.MusicArtistId == artist.MusicArtistId;

    private static bool Owns(
        ExternalSourceIdentifier identifier,
        MusicRelease release)
        => ReferenceEquals(identifier.MusicRelease, release)
            || identifier.MusicReleaseId is not null
                && release.MusicReleaseId > 0
                && identifier.MusicReleaseId == release.MusicReleaseId;

    private static bool Owns(
        ExternalSourceIdentifier identifier,
        MusicTrack track)
        => ReferenceEquals(identifier.MusicTrack, track)
            || identifier.MusicTrackId is not null
                && track.MusicTrackId > 0
                && identifier.MusicTrackId == track.MusicTrackId;

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Constraint rejeitou a importacao da Colecao local.");
            throw new MusicConflictException(
                "A importacao encontrou uma identidade ou referencia duplicada criada por outra operacao.");
        }
    }

    private static IReadOnlyList<ExternalIdentifierValue> NormalizeIdentifiers(
        IEnumerable<ExternalSourceIdentifierRequest> requests)
    {
        var values = new List<ExternalIdentifierValue>();
        foreach (var request in requests)
        {
            try
            {
                var normalized = new ExternalSourceIdentifier(
                    request.Provider,
                    request.ResourceType,
                    request.ExternalId);
                var value = new ExternalIdentifierValue(
                    normalized.Provider,
                    normalized.ResourceType,
                    normalized.ExternalId);
                if (!values.Any(existing => SameIdentifier(existing, value)))
                {
                    values.Add(value);
                }
            }
            catch (ArgumentException exception)
            {
                throw new MusicValidationException(exception.Message);
            }
        }

        return values;
    }

    private static bool SameIdentifier(
        ExternalIdentifierValue left,
        ExternalIdentifierValue right)
        => string.Equals(left.Provider, right.Provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.ResourceType, right.ResourceType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.ExternalId, right.ExternalId, StringComparison.OrdinalIgnoreCase);

    private static bool IsCollectionIdentifier(ExternalIdentifierValue value)
        => string.Equals(value.ResourceType, "Collection", StringComparison.OrdinalIgnoreCase);

    private static void ValidateImportIdentity(
        ImportMusicCollectionRequest request,
        IReadOnlyList<ExternalIdentifierValue> identifiers)
    {
        if (request.MusicCollectionId is null && !identifiers.Any(IsCollectionIdentifier))
        {
            throw new MusicValidationException(
                "A importacao precisa informar MusicCollectionId ou um identificador externo de recurso Collection.");
        }

        if (request.Artists.Count == 0)
        {
            throw new MusicValidationException("A importacao precisa informar pelo menos um artista.");
        }
    }

    private static string NormalizeRequiredSearchText(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new MusicValidationException(message);
        }

        try
        {
            return MusicTextNormalizer.NormalizeSearchText(value);
        }
        catch (ArgumentException exception)
        {
            throw new MusicValidationException(exception.Message);
        }
    }

    private static void ValidateArtistType(MusicArtistType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new MusicValidationException("O tipo do artista e invalido.");
        }
    }

    private static void ValidateCollectionArtistRole(MusicCollectionArtistRole value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new MusicValidationException("O papel do artista na Colecao e invalido.");
        }
    }

    private static void ValidateReleaseType(MusicReleaseType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new MusicValidationException("O tipo do release e invalido.");
        }
    }

    private static void ValidateCreditRole(MusicCreditRole value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new MusicValidationException("O papel do credito e invalido.");
        }
    }

    private static void ValidateMediaValues(MusicMediaKind mediaKind, MusicLocalFileRole role)
    {
        if (!Enum.IsDefined(mediaKind) || !Enum.IsDefined(role))
        {
            throw new MusicValidationException("O tipo ou papel da referencia local e invalido.");
        }
    }

    private sealed class ImportState
    {
        public bool Created { get; set; }

        public bool Changed { get; set; }

        public int ArtistsAdded { get; set; }

        public int ReleasesAdded { get; set; }

        public int TracksAdded { get; set; }

        public int LocalFileReferencesAdded { get; set; }
    }

    private sealed record ExternalIdentifierValue(
        string Provider,
        string ResourceType,
        string ExternalId);

    /// <inheritdoc />
    public async Task<ImportMusicCollectionResponse> ImportAsync(
        ImportMusicCollectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var collectionIdentifiers = NormalizeIdentifiers(request.ExternalIdentifiers);
        ValidateImportIdentity(request, collectionIdentifiers);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var state = new ImportState();
        var collection = await ResolveCollectionAsync(
            request,
            collectionIdentifiers,
            state,
            cancellationToken);

        await AttachCollectionIdentifiersAsync(collection, collectionIdentifiers, state, cancellationToken);

        foreach (var artistRequest in request.Artists)
        {
            await ResolveCollectionArtistAsync(collection, artistRequest, state, cancellationToken);
        }

        foreach (var releaseRequest in request.Releases)
        {
            await ImportReleaseAsync(collection, releaseRequest, state, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var collectionDto = await collectionService.GetCollectionAsync(
            collection.MusicCollectionId,
            cancellationToken);
        if (collectionDto is null)
        {
            throw new MusicNotFoundException(
                $"Colecao com ID {collection.MusicCollectionId} nao encontrada apos a importacao.");
        }

        logger.LogInformation(
            "Importacao da Colecao concluida. CollectionId {MusicCollectionId} Created {Created} Changed {Changed} ArtistsAdded {ArtistsAdded} ReleasesAdded {ReleasesAdded} TracksAdded {TracksAdded} LocalFilesAdded {LocalFilesAdded}",
            collection.MusicCollectionId,
            state.Created,
            state.Changed,
            state.ArtistsAdded,
            state.ReleasesAdded,
            state.TracksAdded,
            state.LocalFileReferencesAdded);

        return new ImportMusicCollectionResponse(
            collectionDto,
            state.Created,
            state.Changed,
            state.ArtistsAdded,
            state.ReleasesAdded,
            state.TracksAdded,
            state.LocalFileReferencesAdded);
    }

    private async Task<MusicCollection> ResolveCollectionAsync(
        ImportMusicCollectionRequest request,
        IReadOnlyList<ExternalIdentifierValue> identifiers,
        ImportState state,
        CancellationToken cancellationToken)
    {
        MusicCollection? collection = null;
        if (request.MusicCollectionId is not null)
        {
            collection = await LoadCollectionAsync(request.MusicCollectionId.Value, cancellationToken);
            if (collection is null)
            {
                throw new MusicNotFoundException(
                    $"Colecao com ID {request.MusicCollectionId.Value} nao encontrada.");
            }
        }

        foreach (var identifierValue in identifiers.Where(IsCollectionIdentifier))
        {
            var identifier = await FindIdentifierAsync(identifierValue, cancellationToken);
            if (identifier is null)
            {
                continue;
            }

            if (identifier.MusicCollectionId is null && identifier.MusicCollection is null)
            {
                throw new MusicConflictException(
                    $"O identificador {identifierValue.Provider}/{identifierValue.ResourceType}/{identifierValue.ExternalId} esta associado a outro tipo de recurso.");
            }

            var identifiedCollectionId = identifier.MusicCollectionId
                ?? identifier.MusicCollection!.MusicCollectionId;
            if (collection is not null && collection.MusicCollectionId != identifiedCollectionId)
            {
                throw new MusicConflictException(
                    "Os identificadores da importacao apontam para Colecoes locais diferentes.");
            }

            collection ??= await LoadCollectionAsync(identifiedCollectionId, cancellationToken)
                ?? throw new MusicConflictException(
                    "O identificador externo aponta para uma Colecao que nao pode ser carregada.");
        }

        if (collection is null)
        {
            collection = new MusicCollection(request.DisplayName, request.Description);
            context.MusicCollections.Add(collection);
            state.Created = true;
            state.Changed = true;
        }
        else
        {
            ApplyCollectionValues(collection, request, state);
        }

        return collection;
    }

    private async Task ResolveCollectionArtistAsync(
        MusicCollection collection,
        MusicArtistImportRequest request,
        ImportState state,
        CancellationToken cancellationToken)
    {
        ValidateCollectionArtistRole(request.CollectionRole);
        var identifiers = NormalizeIdentifiers(request.ExternalIdentifiers);
        var artist = await ResolveArtistAsync(
            request.MusicArtistId,
            request.DisplayName,
            request.ArtistType,
            request.SortName,
            request.Aliases,
            identifiers,
            state,
            cancellationToken);

        var link = collection.ArtistLinks.SingleOrDefault(item =>
            ReferenceEquals(item.MusicArtist, artist)
            || item.MusicArtistId == artist.MusicArtistId && artist.MusicArtistId > 0);
        if (link is null)
        {
            collection.ArtistLinks.Add(new MusicCollectionArtist(collection, artist, request.CollectionRole));
            state.Changed = true;
        }
        else
        {
            ApplyCollectionArtistRole(link, request.CollectionRole, state);
        }

        await AttachArtistIdentifiersAsync(artist, identifiers, state, cancellationToken);
    }

    private async Task ImportReleaseAsync(
        MusicCollection collection,
        MusicReleaseImportRequest request,
        ImportState state,
        CancellationToken cancellationToken)
    {
        ValidateReleaseType(request.ReleaseType);
        var identifiers = NormalizeIdentifiers(request.ExternalIdentifiers);
        var release = await ResolveReleaseAsync(
            collection,
            request,
            identifiers,
            state,
            cancellationToken);

        await AttachReleaseIdentifiersAsync(release, identifiers, state, cancellationToken);
        await ImportReleaseCreditsAsync(release, request.ArtistCredits, state, cancellationToken);

        foreach (var trackRequest in request.Tracks)
        {
            await ImportTrackAsync(release, trackRequest, state, cancellationToken);
        }

        foreach (var referenceRequest in request.LocalFileReferences)
        {
            await AttachLocalFileReferenceAsync(
                release,
                track: null,
                referenceRequest,
                state,
                cancellationToken);
        }
    }

    private async Task<MusicRelease> ResolveReleaseAsync(
        MusicCollection collection,
        MusicReleaseImportRequest request,
        IReadOnlyList<ExternalIdentifierValue> identifiers,
        ImportState state,
        CancellationToken cancellationToken)
    {
        MusicRelease? release = null;
        if (request.MusicReleaseId is not null)
        {
            release = await LoadReleaseAsync(request.MusicReleaseId.Value, cancellationToken);
            if (release is null)
            {
                throw new MusicNotFoundException(
                    $"Release com ID {request.MusicReleaseId.Value} nao encontrado.");
            }
        }

        foreach (var identifierValue in identifiers)
        {
            var identifier = await FindIdentifierAsync(identifierValue, cancellationToken);
            if (identifier is null)
            {
                continue;
            }

            if (identifier.MusicReleaseId is null && identifier.MusicRelease is null)
            {
                throw new MusicConflictException(
                    $"O identificador {identifierValue.Provider}/{identifierValue.ResourceType}/{identifierValue.ExternalId} esta associado a outro tipo de recurso.");
            }

            var identifiedReleaseId = identifier.MusicReleaseId
                ?? identifier.MusicRelease!.MusicReleaseId;
            if (release is not null && release.MusicReleaseId != identifiedReleaseId)
            {
                throw new MusicConflictException(
                    "Os identificadores do release apontam para releases locais diferentes.");
            }

            release ??= await LoadReleaseAsync(identifiedReleaseId, cancellationToken)
                ?? throw new MusicConflictException(
                    "O identificador externo aponta para um release que nao pode ser carregado.");
        }

        if (release is null)
        {
            var normalizedTitle = NormalizeRequiredSearchText(request.Title, "O titulo do release e obrigatorio.");
            var candidates = collection.ReleaseLinks
                .Select(link => link.MusicRelease)
                .Where(item => item.NormalizedTitle == normalizedTitle)
                .Where(item => request.ReleaseType == MusicReleaseType.Unknown
                    || item.ReleaseType == MusicReleaseType.Unknown
                    || item.ReleaseType == request.ReleaseType)
                .Where(item => request.ReleaseYear is null || item.ReleaseYear == request.ReleaseYear)
                .ToList();

            if (candidates.Count > 1)
            {
                throw new MusicConflictException(
                    $"Ha mais de um release candidato para '{request.Title}'. Informe um identificador local ou externo.");
            }

            if (candidates.Count == 1)
            {
                if (request.ReleaseYear is null)
                {
                    throw new MusicConflictException(
                        $"O release '{request.Title}' ja existe, mas a importacao nao informou ano ou identificador para confirmar a mesclagem.");
                }

                release = candidates[0];
            }
        }

        if (release is null)
        {
            release = new MusicRelease(
                request.Title,
                request.ReleaseType,
                request.ReleaseYear,
                request.Notes);
            context.MusicReleases.Add(release);
            state.ReleasesAdded++;
            state.Changed = true;
        }
        else
        {
            ApplyReleaseValues(release, request, state);
        }

        var link = collection.ReleaseLinks.SingleOrDefault(item =>
            ReferenceEquals(item.MusicRelease, release)
            || item.MusicReleaseId == release.MusicReleaseId && release.MusicReleaseId > 0);
        if (link is null)
        {
            collection.ReleaseLinks.Add(new MusicCollectionRelease(
                collection,
                release,
                request.SourceCategory,
                request.DisplayOrder));
            state.Changed = true;
        }
        else
        {
            ApplyCollectionReleaseMetadata(link, request, state);
        }

        return release;
    }

    private async Task ImportReleaseCreditsAsync(
        MusicRelease release,
        IEnumerable<MusicArtistCreditImportRequest> requests,
        ImportState state,
        CancellationToken cancellationToken)
    {
        foreach (var request in requests)
        {
            ValidateCreditRole(request.Role);
            var identifiers = NormalizeIdentifiers(request.ExternalIdentifiers);
            var artist = await ResolveArtistAsync(
                request.MusicArtistId,
                request.DisplayName,
                request.ArtistType,
                sortName: null,
                aliases: [],
                identifiers,
                state,
                cancellationToken);
            var credit = release.ArtistCredits.SingleOrDefault(item =>
                ReferenceEquals(item.MusicArtist, artist)
                || item.MusicArtistId == artist.MusicArtistId && artist.MusicArtistId > 0);
            if (credit is null)
            {
                release.ArtistCredits.Add(new MusicReleaseArtist(release, artist, request.Role));
                state.Changed = true;
            }
            else
            {
                ApplyCreditRole(credit, request.Role, state);
            }

            await AttachArtistIdentifiersAsync(artist, identifiers, state, cancellationToken);
        }
    }

    private async Task ImportTrackAsync(
        MusicRelease release,
        MusicTrackImportRequest request,
        ImportState state,
        CancellationToken cancellationToken)
    {
        var identifiers = NormalizeIdentifiers(request.ExternalIdentifiers);
        var track = await ResolveTrackAsync(release, request, identifiers, state, cancellationToken);
        await AttachTrackIdentifiersAsync(track, identifiers, state, cancellationToken);

        foreach (var creditRequest in request.ArtistCredits)
        {
            ValidateCreditRole(creditRequest.Role);
            var creditIdentifiers = NormalizeIdentifiers(creditRequest.ExternalIdentifiers);
            var artist = await ResolveArtistAsync(
                creditRequest.MusicArtistId,
                creditRequest.DisplayName,
                creditRequest.ArtistType,
                sortName: null,
                aliases: [],
                creditIdentifiers,
                state,
                cancellationToken);
            var credit = track.ArtistCredits.SingleOrDefault(item =>
                ReferenceEquals(item.MusicArtist, artist)
                || item.MusicArtistId == artist.MusicArtistId && artist.MusicArtistId > 0);
            if (credit is null)
            {
                track.ArtistCredits.Add(new MusicTrackArtist(track, artist, creditRequest.Role));
                state.Changed = true;
            }
            else
            {
                ApplyCreditRole(credit, creditRequest.Role, state);
            }

            await AttachArtistIdentifiersAsync(artist, creditIdentifiers, state, cancellationToken);
        }

        foreach (var referenceRequest in request.LocalFileReferences)
        {
            await AttachLocalFileReferenceAsync(
                release,
                track,
                referenceRequest,
                state,
                cancellationToken);
        }
    }
}
