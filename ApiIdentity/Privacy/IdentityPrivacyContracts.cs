namespace ApiIdentity.Privacy;

public static class PersonalResourceTypes
{
    public const string Anime = "anime";

    public const string MyAnime = "my-anime";
}

public sealed record PersonalResourceRequest(string ResourceType, string ResourceKey);

public sealed record PersonalPreferenceRequest(string Key, string Value);

public sealed record PersonalListRequest(string Name);

public sealed record PersonalListItemRequest(string ResourceType, string ResourceKey, int Position);

public sealed record TermsAcceptanceRequest(Guid TermsDocumentId);

public sealed record AdultAgeConfirmationView(
    bool HasConfirmedAdultAge,
    DateTimeOffset? AdultAgeConfirmedAtUtc);

public sealed record TermsDocumentView(
    Guid Id,
    string DocumentType,
    string Version,
    string Content,
    string ContentHashSha256,
    DateTimeOffset PublishedAtUtc);

public sealed record TermsAcceptanceView(
    Guid AcceptanceId,
    Guid TermsDocumentId,
    string DocumentType,
    string Version,
    string ContentHashSha256,
    DateTimeOffset AcceptedAtUtc);

public sealed record PersonalFavoriteView(
    Guid Id,
    string ResourceType,
    string ResourceKey,
    DateTimeOffset CreatedAtUtc);

public sealed record PersonalPreferenceView(
    string Key,
    string Value,
    DateTimeOffset UpdatedAtUtc);

public sealed record PersonalListItemView(
    Guid Id,
    string ResourceType,
    string ResourceKey,
    int Position,
    DateTimeOffset AddedAtUtc);

public sealed record PersonalListView(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<PersonalListItemView> Items);

public sealed record PersonalDataDeletionRequestView(
    Guid Id,
    string Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset ScheduledForUtc,
    DateTimeOffset? ProcessedAtUtc,
    DateTimeOffset? RetentionUntilUtc);

public sealed record PersonalDataExport(
    string ExportVersion,
    DateTimeOffset GeneratedAtUtc,
    string AccountId,
    string? UserName,
    string? Email,
    bool HasConfirmedAdultAge,
    DateTimeOffset? AdultAgeConfirmedAtUtc,
    IReadOnlyList<TermsAcceptanceExport> AcceptedTerms,
    IReadOnlyList<PersonalFavoriteView> Favorites,
    IReadOnlyList<PersonalPreferenceView> Preferences,
    IReadOnlyList<PersonalListView> Lists,
    IReadOnlyList<PersonalDataDeletionRequestView> DeletionRequests);

public sealed record TermsAcceptanceExport(
    Guid AcceptanceId,
    string DocumentType,
    string Version,
    string Content,
    string ContentHashSha256,
    DateTimeOffset AcceptedAtUtc);
