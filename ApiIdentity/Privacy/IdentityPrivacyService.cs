using System.Security.Cryptography;
using System.Text;
using ApiIdentity.Data;
using ApiIdentity.Models;
using ApiIdentity.Provisioning;
using Microsoft.EntityFrameworkCore;

namespace ApiIdentity.Privacy;

public sealed class IdentityPrivacyService
{
    private static readonly HashSet<string> AllowedPreferenceKeys = new(StringComparer.Ordinal)
    {
        "catalog-sort",
        "language",
        "notifications",
        "theme"
    };

    private static readonly HashSet<string> AllowedResourceTypes = new(StringComparer.Ordinal)
    {
        PersonalResourceTypes.Anime,
        PersonalResourceTypes.MyAnime
    };

    private static readonly TimeSpan DeletionGracePeriod = TimeSpan.FromDays(7);

    private readonly IdentityDbContext _context;
    private readonly IdentityProvisioningAuditWriter _auditWriter;
    private readonly TimeProvider _timeProvider;

    public IdentityPrivacyService(
        IdentityDbContext context,
        IdentityProvisioningAuditWriter auditWriter,
        TimeProvider timeProvider)
    {
        _context = context;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
    }

    public async Task<AdultAgeConfirmationView?> GetAdultAgeConfirmationAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);

        return account is null
            ? null
            : new AdultAgeConfirmationView(
                account.HasConfirmedAdultAge,
                account.AdultAgeConfirmedAtUtc);
    }

    public async Task<AdultAgeConfirmationView?> ConfirmAdultAgeAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Users
            .SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        if (!account.HasConfirmedAdultAge)
        {
            account.HasConfirmedAdultAge = true;
            account.AdultAgeConfirmedAtUtc = _timeProvider.GetUtcNow();
        }

        _auditWriter.Record(
            accountId,
            "privacy.age-confirmation",
            $"account:{accountId}",
            "succeeded",
            "self-service",
            "Adultidade confirmada sem armazenar nascimento completo.");
        await _context.SaveChangesAsync(cancellationToken);

        return new AdultAgeConfirmationView(
            account.HasConfirmedAdultAge,
            account.AdultAgeConfirmedAtUtc);
    }

    public async Task<TermsDocumentView?> GetActiveTermsAsync(
        string documentType,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeDocumentType(documentType, out var normalizedDocumentType))
        {
            return null;
        }

        return await _context.TermsDocuments
            .AsNoTracking()
            .Where(document => document.DocumentType == normalizedDocumentType && document.IsActive)
            .OrderByDescending(document => document.PublishedAtUtc)
            .Select(document => new TermsDocumentView(
                document.Id,
                document.DocumentType,
                document.Version,
                document.Content,
                document.ContentHashSha256,
                document.PublishedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TermsAcceptanceView?> AcceptTermsAsync(
        string accountId,
        Guid termsDocumentId,
        CancellationToken cancellationToken = default)
    {
        var accountExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(account => account.Id == accountId, cancellationToken);
        if (!accountExists)
        {
            return null;
        }

        var document = await _context.TermsDocuments
            .SingleOrDefaultAsync(item => item.Id == termsDocumentId && item.IsActive, cancellationToken);
        if (document is null || !HasExpectedContentHash(document))
        {
            return null;
        }

        var existingAcceptance = await _context.TermsAcceptances
            .AsNoTracking()
            .Where(acceptance => acceptance.AccountId == accountId
                && acceptance.TermsDocumentId == termsDocumentId)
            .Select(acceptance => new TermsAcceptanceView(
                acceptance.Id,
                document.Id,
                document.DocumentType,
                document.Version,
                document.ContentHashSha256,
                acceptance.AcceptedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
        if (existingAcceptance is not null)
        {
            return existingAcceptance;
        }

        var acceptance = new TermsAcceptance
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            TermsDocumentId = termsDocumentId,
            AcceptedAtUtc = _timeProvider.GetUtcNow()
        };
        _context.TermsAcceptances.Add(acceptance);
        _auditWriter.Record(
            accountId,
            "privacy.terms-accepted",
            $"terms:{termsDocumentId:D}",
            "succeeded",
            "self-service",
            "Aceite vinculado ao documento versionado e ao hash publicado.");
        await _context.SaveChangesAsync(cancellationToken);

        return new TermsAcceptanceView(
            acceptance.Id,
            document.Id,
            document.DocumentType,
            document.Version,
            document.ContentHashSha256,
            acceptance.AcceptedAtUtc);
    }

    public async Task<IReadOnlyList<PersonalFavoriteView>> GetFavoritesAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PersonalFavorites
            .AsNoTracking()
            .Where(favorite => favorite.AccountId == accountId)
            .OrderBy(favorite => favorite.CreatedAtUtc)
            .ThenBy(favorite => favorite.Id)
            .Select(favorite => new PersonalFavoriteView(
                favorite.Id,
                favorite.ResourceType,
                favorite.ResourceKey,
                favorite.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PersonalFavoriteView?> AddFavoriteAsync(
        string accountId,
        PersonalResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeResource(request.ResourceType, request.ResourceKey, out var resourceType, out var resourceKey)
            || !await AccountExistsAsync(accountId, cancellationToken))
        {
            return null;
        }

        var existingFavorite = await _context.PersonalFavorites
            .SingleOrDefaultAsync(favorite => favorite.AccountId == accountId
                && favorite.ResourceType == resourceType
                && favorite.ResourceKey == resourceKey, cancellationToken);
        if (existingFavorite is not null)
        {
            return ToFavoriteView(existingFavorite);
        }

        var favorite = new PersonalFavorite
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            ResourceType = resourceType,
            ResourceKey = resourceKey,
            CreatedAtUtc = _timeProvider.GetUtcNow()
        };
        _context.PersonalFavorites.Add(favorite);
        _auditWriter.Record(
            accountId,
            "privacy.favorite-created",
            $"favorite:{favorite.Id:D}",
            "succeeded",
            "self-service",
            "Favorito criado no escopo do proprietario.");
        await _context.SaveChangesAsync(cancellationToken);

        return ToFavoriteView(favorite);
    }

    public async Task<bool> RemoveFavoriteAsync(
        string accountId,
        Guid favoriteId,
        CancellationToken cancellationToken = default)
    {
        var favorite = await _context.PersonalFavorites
            .SingleOrDefaultAsync(item => item.Id == favoriteId && item.AccountId == accountId, cancellationToken);
        if (favorite is null)
        {
            return false;
        }

        _context.PersonalFavorites.Remove(favorite);
        _auditWriter.Record(
            accountId,
            "privacy.favorite-deleted",
            $"favorite:{favoriteId:D}",
            "succeeded",
            "self-service",
            "Favorito removido no escopo do proprietario.");
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PersonalPreferenceView>> GetPreferencesAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PersonalPreferences
            .AsNoTracking()
            .Where(preference => preference.AccountId == accountId)
            .OrderBy(preference => preference.Key)
            .Select(preference => new PersonalPreferenceView(
                preference.Key,
                preference.Value,
                preference.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PersonalPreferenceView?> SetPreferenceAsync(
        string accountId,
        PersonalPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizePreference(request.Key, request.Value, out var key, out var value)
            || !await AccountExistsAsync(accountId, cancellationToken))
        {
            return null;
        }

        var preference = await _context.PersonalPreferences
            .SingleOrDefaultAsync(item => item.AccountId == accountId && item.Key == key, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        if (preference is null)
        {
            preference = new PersonalPreference
            {
                AccountId = accountId,
                Key = key,
                Value = value,
                UpdatedAtUtc = now
            };
            _context.PersonalPreferences.Add(preference);
        }
        else
        {
            preference.Value = value;
            preference.UpdatedAtUtc = now;
        }

        _auditWriter.Record(
            accountId,
            "privacy.preference-updated",
            $"preference:{key}",
            "succeeded",
            "self-service",
            "Preferencia limitada ao catalogo de configuracoes permitidas.");
        await _context.SaveChangesAsync(cancellationToken);

        return new PersonalPreferenceView(preference.Key, preference.Value, preference.UpdatedAtUtc);
    }

    public async Task<bool> RemovePreferenceAsync(
        string accountId,
        string key,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizePreferenceKey(key, out var normalizedKey))
        {
            return false;
        }

        var preference = await _context.PersonalPreferences
            .SingleOrDefaultAsync(item => item.AccountId == accountId && item.Key == normalizedKey, cancellationToken);
        if (preference is null)
        {
            return false;
        }

        _context.PersonalPreferences.Remove(preference);
        _auditWriter.Record(
            accountId,
            "privacy.preference-deleted",
            $"preference:{normalizedKey}",
            "succeeded",
            "self-service",
            "Preferencia removida no escopo do proprietario.");
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PersonalListView?> CreateListAsync(
        string accountId,
        PersonalListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeName(request.Name, out var name)
            || !await AccountExistsAsync(accountId, cancellationToken))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var list = new PersonalList
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = name,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        _context.PersonalLists.Add(list);
        _auditWriter.Record(
            accountId,
            "privacy.list-created",
            $"list:{list.Id:D}",
            "succeeded",
            "self-service",
            "Lista criada no escopo do proprietario.");
        await _context.SaveChangesAsync(cancellationToken);

        return ToListView(list, []);
    }

    public async Task<IReadOnlyList<PersonalListView>> GetListsAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var lists = await _context.PersonalLists
            .AsNoTracking()
            .Where(list => list.AccountId == accountId)
            .OrderBy(list => list.CreatedAtUtc)
            .ThenBy(list => list.Id)
            .ToListAsync(cancellationToken);
        if (lists.Count == 0)
        {
            return [];
        }

        var listIds = lists.Select(list => list.Id).ToList();
        var items = await _context.PersonalListItems
            .AsNoTracking()
            .Where(item => item.AccountId == accountId && listIds.Contains(item.ListId))
            .OrderBy(item => item.Position)
            .ThenBy(item => item.AddedAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                item.ListId,
                View = new PersonalListItemView(
                    item.Id,
                    item.ResourceType,
                    item.ResourceKey,
                    item.Position,
                    item.AddedAtUtc)
            })
            .ToListAsync(cancellationToken);
        var itemsByList = items
            .GroupBy(item => item.ListId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PersonalListItemView>)group.Select(item => item.View).ToList());

        return lists
            .Select(list => new PersonalListView(
                list.Id,
                list.Name,
                list.CreatedAtUtc,
                list.UpdatedAtUtc,
                itemsByList.TryGetValue(list.Id, out var listItems) ? listItems : []))
            .ToList();
    }

    public async Task<bool> RemoveListAsync(
        string accountId,
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        var list = await _context.PersonalLists
            .SingleOrDefaultAsync(item => item.Id == listId && item.AccountId == accountId, cancellationToken);
        if (list is null)
        {
            return false;
        }

        _context.PersonalLists.Remove(list);
        _auditWriter.Record(
            accountId,
            "privacy.list-deleted",
            $"list:{listId:D}",
            "succeeded",
            "self-service",
            "Lista removida no escopo do proprietario.");
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PersonalListItemView?> AddListItemAsync(
        string accountId,
        Guid listId,
        PersonalListItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeResource(request.ResourceType, request.ResourceKey, out var resourceType, out var resourceKey)
            || request.Position < 0
            || request.Position > 100_000)
        {
            return null;
        }

        var list = await _context.PersonalLists
            .SingleOrDefaultAsync(item => item.Id == listId && item.AccountId == accountId, cancellationToken);
        if (list is null)
        {
            return null;
        }

        var listItem = await _context.PersonalListItems
            .SingleOrDefaultAsync(item => item.AccountId == accountId
                && item.ListId == listId
                && item.ResourceType == resourceType
                && item.ResourceKey == resourceKey, cancellationToken);
        if (listItem is null)
        {
            listItem = new PersonalListItem
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                ListId = listId,
                ResourceType = resourceType,
                ResourceKey = resourceKey,
                Position = request.Position,
                AddedAtUtc = _timeProvider.GetUtcNow()
            };
            _context.PersonalListItems.Add(listItem);
        }
        else
        {
            listItem.Position = request.Position;
        }

        list.UpdatedAtUtc = _timeProvider.GetUtcNow();
        _auditWriter.Record(
            accountId,
            "privacy.list-item-updated",
            $"list-item:{listItem.Id:D}",
            "succeeded",
            "self-service",
            "Item de lista gravado no escopo do proprietario.");
        await _context.SaveChangesAsync(cancellationToken);

        return ToListItemView(listItem);
    }

    public async Task<bool> RemoveListItemAsync(
        string accountId,
        Guid listId,
        Guid listItemId,
        CancellationToken cancellationToken = default)
    {
        var listItem = await _context.PersonalListItems
            .SingleOrDefaultAsync(item => item.Id == listItemId
                && item.ListId == listId
                && item.AccountId == accountId, cancellationToken);
        if (listItem is null)
        {
            return false;
        }

        var list = await _context.PersonalLists
            .SingleOrDefaultAsync(item => item.Id == listId && item.AccountId == accountId, cancellationToken);
        if (list is null)
        {
            return false;
        }

        _context.PersonalListItems.Remove(listItem);
        list.UpdatedAtUtc = _timeProvider.GetUtcNow();
        _auditWriter.Record(
            accountId,
            "privacy.list-item-deleted",
            $"list-item:{listItemId:D}",
            "succeeded",
            "self-service",
            "Item de lista removido no escopo do proprietario.");
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PersonalDataDeletionRequestView?> RequestDeletionAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        if (!await AccountExistsAsync(accountId, cancellationToken)
            || await IsBootstrappedAccountAsync(accountId, cancellationToken))
        {
            return null;
        }

        var existingRequest = await _context.PersonalDataDeletionRequests
            .Where(request => request.AccountId == accountId
                && request.Status == PersonalDataDeletionStatuses.Pending)
            .OrderByDescending(request => request.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingRequest is not null)
        {
            return ToDeletionRequestView(existingRequest);
        }

        var requestedAtUtc = _timeProvider.GetUtcNow();
        var deletionRequest = new PersonalDataDeletionRequest
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Status = PersonalDataDeletionStatuses.Pending,
            RequestedAtUtc = requestedAtUtc,
            ScheduledForUtc = requestedAtUtc.Add(DeletionGracePeriod)
        };
        _context.PersonalDataDeletionRequests.Add(deletionRequest);
        _auditWriter.Record(
            accountId,
            "privacy.deletion-requested",
            $"account:{accountId}",
            "succeeded",
            "self-service",
            "Solicitacao criada com janela de sete dias; auditoria sera retida por doze meses.");
        await _context.SaveChangesAsync(cancellationToken);

        return ToDeletionRequestView(deletionRequest);
    }

    public async Task<bool> ProcessDueDeletionAsync(
        Guid deletionRequestId,
        CancellationToken cancellationToken = default)
    {
        var deletionRequest = await _context.PersonalDataDeletionRequests
            .SingleOrDefaultAsync(request => request.Id == deletionRequestId
                && request.Status == PersonalDataDeletionStatuses.Pending, cancellationToken);
        if (deletionRequest is null
            || deletionRequest.ScheduledForUtc > _timeProvider.GetUtcNow()
            || await IsBootstrappedAccountAsync(deletionRequest.AccountId, cancellationToken))
        {
            return false;
        }

        var account = await _context.Users
            .SingleOrDefaultAsync(item => item.Id == deletionRequest.AccountId, cancellationToken);
        if (account is null)
        {
            return false;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await RemoveAccountDataAsync(account.Id, cancellationToken);

        var processedAtUtc = _timeProvider.GetUtcNow();
        deletionRequest.Status = PersonalDataDeletionStatuses.Completed;
        deletionRequest.ProcessedAtUtc = processedAtUtc;
        deletionRequest.RetentionUntilUtc = processedAtUtc.AddMonths(12);
        _auditWriter.Record(
            account.Id,
            "privacy.deletion-completed",
            $"account:{account.Id}",
            "succeeded",
            "privacy-worker",
            "Dados pessoais removidos; pedido e auditoria minima permanecem sob retencao legal.");
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<PersonalDataExport?> ExportAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var acceptedTerms = await _context.TermsAcceptances
            .AsNoTracking()
            .Where(acceptance => acceptance.AccountId == accountId)
            .Include(acceptance => acceptance.TermsDocument)
            .OrderBy(acceptance => acceptance.AcceptedAtUtc)
            .Select(acceptance => new TermsAcceptanceExport(
                acceptance.Id,
                acceptance.TermsDocument!.DocumentType,
                acceptance.TermsDocument.Version,
                acceptance.TermsDocument.Content,
                acceptance.TermsDocument.ContentHashSha256,
                acceptance.AcceptedAtUtc))
            .ToListAsync(cancellationToken);
        var favorites = await GetFavoritesAsync(accountId, cancellationToken);
        var preferences = await GetPreferencesAsync(accountId, cancellationToken);
        var lists = await GetListsAsync(accountId, cancellationToken);
        var deletionRequests = await _context.PersonalDataDeletionRequests
            .AsNoTracking()
            .Where(request => request.AccountId == accountId)
            .OrderBy(request => request.RequestedAtUtc)
            .Select(request => new PersonalDataDeletionRequestView(
                request.Id,
                request.Status,
                request.RequestedAtUtc,
                request.ScheduledForUtc,
                request.ProcessedAtUtc,
                request.RetentionUntilUtc))
            .ToListAsync(cancellationToken);

        var generatedAtUtc = _timeProvider.GetUtcNow();
        _auditWriter.Record(
            accountId,
            "privacy.data-export",
            $"account:{accountId}",
            "succeeded",
            "self-service",
            "Exportacao completa dos dados pessoais sob controle da conta; segredos de autenticacao foram omitidos.");
        await _context.SaveChangesAsync(cancellationToken);

        return new PersonalDataExport(
            "1",
            generatedAtUtc,
            account.Id,
            account.UserName,
            account.Email,
            account.HasConfirmedAdultAge,
            account.AdultAgeConfirmedAtUtc,
            acceptedTerms,
            favorites,
            preferences,
            lists,
            deletionRequests);
    }

    private async Task<bool> AccountExistsAsync(
        string accountId,
        CancellationToken cancellationToken) =>
        await _context.Users.AnyAsync(account => account.Id == accountId, cancellationToken);

    private async Task<bool> IsBootstrappedAccountAsync(
        string accountId,
        CancellationToken cancellationToken) =>
        await _context.BootstrapStates.AnyAsync(
            state => state.BootstrappedAccountId == accountId,
            cancellationToken);

    private async Task RemoveAccountDataAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        _context.TermsAcceptances.RemoveRange(await _context.TermsAcceptances
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.PersonalListItems.RemoveRange(await _context.PersonalListItems
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.PersonalFavorites.RemoveRange(await _context.PersonalFavorites
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.PersonalPreferences.RemoveRange(await _context.PersonalPreferences
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.PersonalLists.RemoveRange(await _context.PersonalLists
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.InitialAccountSecrets.RemoveRange(await _context.InitialAccountSecrets
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.SecurityChallenges.RemoveRange(await _context.SecurityChallenges
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.StepUpGrants.RemoveRange(await _context.StepUpGrants
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.SecurityTokens.RemoveRange(await _context.SecurityTokens
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.SecuritySessions.RemoveRange(await _context.SecuritySessions
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.SecurityDevices.RemoveRange(await _context.SecurityDevices
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.SecuritySnapshots.RemoveRange(await _context.SecuritySnapshots
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.RecoveryTickets.RemoveRange(await _context.RecoveryTickets
            .Where(item => item.AccountId == accountId)
            .ToListAsync(cancellationToken));
        _context.UserClaims.RemoveRange(await _context.UserClaims
            .Where(item => item.UserId == accountId)
            .ToListAsync(cancellationToken));
        _context.UserLogins.RemoveRange(await _context.UserLogins
            .Where(item => item.UserId == accountId)
            .ToListAsync(cancellationToken));
        _context.UserTokens.RemoveRange(await _context.UserTokens
            .Where(item => item.UserId == accountId)
            .ToListAsync(cancellationToken));
        _context.UserRoles.RemoveRange(await _context.UserRoles
            .Where(item => item.UserId == accountId)
            .ToListAsync(cancellationToken));

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [OpenIddictTokens] WHERE [Subject] = {accountId}",
            cancellationToken);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [OpenIddictAuthorizations] WHERE [Subject] = {accountId}",
            cancellationToken);

        var account = await _context.Users
            .SingleAsync(item => item.Id == accountId, cancellationToken);
        _context.Users.Remove(account);
    }

    private static bool HasExpectedContentHash(TermsDocument document)
    {
        var contentBytes = Encoding.UTF8.GetBytes(document.Content);
        var expectedHash = Convert.ToHexString(SHA256.HashData(contentBytes));
        return string.Equals(expectedHash, document.ContentHashSha256, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryNormalizeResource(
        string? resourceType,
        string? resourceKey,
        out string normalizedResourceType,
        out string normalizedResourceKey)
    {
        normalizedResourceType = resourceType?.Trim().ToLowerInvariant() ?? string.Empty;
        normalizedResourceKey = resourceKey?.Trim() ?? string.Empty;
        return AllowedResourceTypes.Contains(normalizedResourceType)
            && normalizedResourceKey.Length is >= 1 and <= 200
            && normalizedResourceKey.All(IsSafeIdentifierCharacter);
    }

    private static bool TryNormalizePreference(
        string? key,
        string? value,
        out string normalizedKey,
        out string normalizedValue)
    {
        normalizedKey = key?.Trim().ToLowerInvariant() ?? string.Empty;
        normalizedValue = value?.Trim() ?? string.Empty;
        return AllowedPreferenceKeys.Contains(normalizedKey)
            && normalizedValue.Length is >= 1 and <= 2_000
            && !normalizedValue.Any(char.IsControl);
    }

    private static bool TryNormalizePreferenceKey(string? key, out string normalizedKey)
    {
        normalizedKey = key?.Trim().ToLowerInvariant() ?? string.Empty;
        return AllowedPreferenceKeys.Contains(normalizedKey);
    }

    private static bool TryNormalizeName(string? name, out string normalizedName)
    {
        normalizedName = name?.Trim() ?? string.Empty;
        return normalizedName.Length is >= 1 and <= 120
            && !normalizedName.Any(char.IsControl);
    }

    private static bool TryNormalizeDocumentType(string? documentType, out string normalizedDocumentType)
    {
        normalizedDocumentType = documentType?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalizedDocumentType.Length is >= 1 and <= 80
            && normalizedDocumentType.All(IsSafeIdentifierCharacter);
    }

    private static bool IsSafeIdentifierCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':';

    private static PersonalFavoriteView ToFavoriteView(PersonalFavorite favorite) =>
        new(favorite.Id, favorite.ResourceType, favorite.ResourceKey, favorite.CreatedAtUtc);

    private static PersonalListItemView ToListItemView(PersonalListItem item) =>
        new(item.Id, item.ResourceType, item.ResourceKey, item.Position, item.AddedAtUtc);

    private static PersonalListView ToListView(
        PersonalList list,
        IReadOnlyList<PersonalListItemView> items) =>
        new(list.Id, list.Name, list.CreatedAtUtc, list.UpdatedAtUtc, items);

    private static PersonalDataDeletionRequestView ToDeletionRequestView(
        PersonalDataDeletionRequest request) =>
        new(
            request.Id,
            request.Status,
            request.RequestedAtUtc,
            request.ScheduledForUtc,
            request.ProcessedAtUtc,
            request.RetentionUntilUtc);

}
