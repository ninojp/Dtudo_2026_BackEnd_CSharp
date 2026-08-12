using System.Globalization;
using System.Text.Json;
using ApiDiscogs.Dtos;
using ApiDiscogs.Services;

namespace ApiDiscogs.Mappers;

/// <summary>
/// Converte payloads da Discogs em contratos estaveis da Dtudo2026.
/// </summary>
public static class DiscogsMapper
{
    private const string Provider = "Discogs";

    /// <summary>
    /// Mapeia a busca de artistas e exige envelope e paginacao validos.
    /// </summary>
    public static DiscogsPagedResponse<DiscogsArtistSearchItem> MapArtistSearch(
        JsonDocument document)
    {
        var root = RequireObject(document);
        var results = RequireArray(root, "results");
        var items = new List<DiscogsArtistSearchItem>();

        foreach (var result in results.EnumerateArray())
        {
            var id = RequiredId(result, "id");
            var name = RequiredString(result, "title");
            items.Add(new DiscogsArtistSearchItem(
                CreateSource(result, "artist", id),
                name,
                "artist",
                PublicUrl(result, "thumb"),
                PublicUrl(result, "cover_image", "image")));
        }

        var pagination = MapPagination(root, items.Count);
        return new DiscogsPagedResponse<DiscogsArtistSearchItem>(
            Provider,
            items,
            pagination,
            true,
            []);
    }

    /// <summary>
    /// Mapeia os detalhes de um artista, alias, membros, URLs e imagens.
    /// </summary>
    public static DiscogsArtistDetails MapArtistDetails(JsonDocument document)
    {
        var root = RequireObject(document);
        var id = RequiredId(root, "id");
        var name = RequiredString(root, "name");
        var warnings = new List<string>();

        return new DiscogsArtistDetails(
            CreateSource(root, "artist", id),
            name,
            OptionalString(root, "realname"),
            OptionalString(root, "profile"),
            MapNameReferences(root, warnings, "aliases"),
            MapNameReferences(root, warnings, "members"),
            StringValues(root, "urls"),
            MapImages(root, warnings),
            warnings.Count == 0,
            warnings);
    }

    /// <summary>
    /// Mapeia uma pagina de discografia, removendo unofficial e agregando duplicatas.
    /// </summary>
    public static DiscogsArtistReleasesResponse MapArtistReleases(
        JsonDocument document,
        int artistId)
    {
        var root = RequireObject(document);
        var releases = RequireArray(root, "releases");
        var warnings = new List<string>();
        var aggregates = new Dictionary<string, ReleaseSummaryAccumulator>(StringComparer.OrdinalIgnoreCase);
        string? artistName = OptionalString(root, "artist");

        foreach (var release in releases.EnumerateArray())
        {
            var summary = MapReleaseSummary(release);
            artistName ??= summary.ArtistName;
            if (summary.Formats.Any(IsUnofficial))
            {
                continue;
            }

            if (!aggregates.TryGetValue(summary.CanonicalId, out var aggregate))
            {
                aggregates.Add(summary.CanonicalId, new ReleaseSummaryAccumulator(summary));
            }
            else
            {
                aggregate.Merge(summary);
            }
        }

        var items = aggregates.Values.Select(x => x.ToSummary()).ToList();
        var pagination = MapPagination(root, items.Count);
        var artistReference = new DiscogsNameReference(
            artistId.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(artistName)
                ? artistId.ToString(CultureInfo.InvariantCulture)
                : artistName);

        return new DiscogsArtistReleasesResponse(
            Provider,
            artistReference,
            items,
            pagination,
            warnings.Count == 0,
            warnings);
    }

    /// <summary>
    /// Mapeia os detalhes de um release, incluindo tracklist quando fornecida.
    /// </summary>
    public static DiscogsReleaseDetails MapReleaseDetails(JsonDocument document)
    {
        var root = RequireObject(document);
        var id = RequiredId(root, "id");
        var title = RequiredString(root, "title");
        var warnings = new List<string>();

        return new DiscogsReleaseDetails(
            CreateSource(root, "release", id),
            title,
            OptionalInt(root, "year"),
            OptionalString(root, "released"),
            OptionalString(root, "country"),
            OptionalString(root, "status"),
            OptionalId(root, "master_id"),
            MapCredits(root, "artists", warnings),
            MapLabels(root, warnings),
            StringValues(root, "genres"),
            StringValues(root, "styles"),
            FormatValues(root),
            MapTracks(root, warnings),
            MapImages(root, warnings),
            OptionalString(root, "notes"),
            warnings.Count == 0,
            warnings);
    }

    /// <summary>
    /// Mapeia os detalhes de um master release sem realizar fallback para release.
    /// </summary>
    public static DiscogsMasterDetails MapMasterDetails(JsonDocument document)
    {
        var root = RequireObject(document);
        var id = RequiredId(root, "id");
        var title = RequiredString(root, "title");
        var warnings = new List<string>();
        var versions = MapVersions(root, warnings);

        return new DiscogsMasterDetails(
            CreateSource(root, "master", id),
            title,
            OptionalId(root, "main_release"),
            OptionalInt(root, "year"),
            StringValues(root, "genres"),
            StringValues(root, "styles"),
            MapCredits(root, "artists", warnings),
            versions,
            MapImages(root, warnings),
            warnings.Count == 0,
            warnings);
    }

    private static IReadOnlyList<DiscogsReleaseSummary> MapVersions(
        JsonElement root,
        ICollection<string> warnings)
    {
        if (!root.TryGetProperty("versions", out var versionsElement))
        {
            return [];
        }

        if (versionsElement.ValueKind == JsonValueKind.Object
            && versionsElement.TryGetProperty("versions", out var nestedVersions))
        {
            versionsElement = nestedVersions;
        }

        if (versionsElement.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("A lista de versoes do master foi ignorada por estar incompleta.");
            return [];
        }

        var versions = new List<DiscogsReleaseSummary>();
        foreach (var version in versionsElement.EnumerateArray())
        {
            try
            {
                versions.Add(MapReleaseSummary(version));
            }
            catch (DiscogsInvalidResponseException)
            {
                warnings.Add("Uma versao do master foi ignorada por estar incompleta.");
            }
        }

        return versions;
    }

    private static DiscogsReleaseSummary MapReleaseSummary(JsonElement element)
    {
        EnsureObject(element);
        var id = RequiredId(element, "id");
        var title = RequiredString(element, "title");
        var rawType = OptionalString(element, "type")?.Trim().ToLowerInvariant();
        var resourceType = rawType == "master" ? "master" : "release";
        var masterId = OptionalId(element, "master_id");
        if (resourceType == "master")
        {
            masterId ??= id;
        }

        var mainReleaseId = OptionalId(element, "main_release");
        var canonicalId = masterId is not null
            ? $"master:{masterId}"
            : resourceType == "master"
                ? $"master:{id}"
                : $"release:{id}";
        var formats = FormatValues(element);
        var roles = StringValues(element, "role");

        return new DiscogsReleaseSummary(
            CreateSource(element, resourceType, id),
            canonicalId,
            resourceType,
            title,
            OptionalString(element, "artist"),
            OptionalId(element, "artist_id"),
            OptionalInt(element, "year"),
            masterId,
            mainReleaseId,
            roles.FirstOrDefault(),
            roles,
            formats,
            Classify(resourceType, title, formats, roles),
            PublicUrl(element, "thumb"),
            PublicUrl(element, "cover_image", "main_image", "image"),
            true,
            []);
    }

    private static string Classify(
        string resourceType,
        string title,
        IReadOnlyList<string> formats,
        IReadOnlyList<string> roles)
    {
        var roleText = string.Join(' ', roles).ToLowerInvariant();
        var formatText = string.Join(' ', formats).ToLowerInvariant();
        var titleText = title.ToLowerInvariant();

        if (roleText.Contains("video", StringComparison.Ordinal)
            || formatText.Contains("video", StringComparison.Ordinal)
            || formatText.Contains("dvd", StringComparison.Ordinal)
            || formatText.Contains("vhs", StringComparison.Ordinal))
        {
            return "video";
        }

        if (formatText.Contains("compilation", StringComparison.Ordinal))
        {
            return "compilation";
        }

        if (formatText.Contains("single", StringComparison.Ordinal)
            || formatText.Contains("ep", StringComparison.Ordinal)
            || titleText.Contains("single", StringComparison.Ordinal)
            || titleText.Contains(" ep", StringComparison.Ordinal))
        {
            return "singleEp";
        }

        if (resourceType == "master"
            || formatText.Contains("album", StringComparison.Ordinal)
            || formatText.Contains("lp", StringComparison.Ordinal)
            || formatText.Contains("cd", StringComparison.Ordinal))
        {
            return "album";
        }

        return "unknown";
    }

    private static List<DiscogsTrack> MapTracks(
        JsonElement root,
        ICollection<string> warnings)
    {
        if (!root.TryGetProperty("tracklist", out var tracklist)
            || tracklist.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (tracklist.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("A tracklist do release foi ignorada por estar incompleta.");
            return [];
        }

        var tracks = new List<DiscogsTrack>();
        foreach (var item in tracklist.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                warnings.Add("Uma faixa do release foi ignorada por estar incompleta.");
                continue;
            }

            var title = OptionalString(item, "title");
            if (string.IsNullOrWhiteSpace(title))
            {
                warnings.Add("Uma faixa do release foi ignorada por estar incompleta.");
                continue;
            }

            var durationText = OptionalString(item, "duration");
            tracks.Add(new DiscogsTrack(
                OptionalString(item, "position"),
                title,
                ParseDuration(durationText),
                durationText,
                MapCredits(item, "artists", warnings),
                MapCredits(item, "extraartists", warnings)));
        }

        return tracks;
    }

    private static List<DiscogsCredit> MapCredits(
        JsonElement root,
        string propertyName,
        ICollection<string> warnings)
    {
        if (!root.TryGetProperty(propertyName, out var credits)
            || credits.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (credits.ValueKind != JsonValueKind.Array)
        {
            warnings.Add($"Os creditos de {propertyName} foram ignorados por estarem incompletos.");
            return [];
        }

        var result = new List<DiscogsCredit>();
        foreach (var credit in credits.EnumerateArray())
        {
            if (credit.ValueKind != JsonValueKind.Object)
            {
                warnings.Add($"Um credito de {propertyName} foi ignorado por estar incompleto.");
                continue;
            }

            var name = OptionalString(credit, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                warnings.Add($"Um credito de {propertyName} foi ignorado por estar incompleto.");
                continue;
            }

            result.Add(new DiscogsCredit(
                OptionalId(credit, "id"),
                name,
                OptionalString(credit, "role")));
        }

        return result;
    }

    private static List<DiscogsLabel> MapLabels(
        JsonElement root,
        ICollection<string> warnings)
    {
        if (!root.TryGetProperty("labels", out var labels)
            || labels.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (labels.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("Os labels do release foram ignorados por estarem incompletos.");
            return [];
        }

        var result = new List<DiscogsLabel>();
        foreach (var label in labels.EnumerateArray())
        {
            if (label.ValueKind != JsonValueKind.Object)
            {
                warnings.Add("Um label do release foi ignorado por estar incompleto.");
                continue;
            }

            var name = OptionalString(label, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                warnings.Add("Um label do release foi ignorado por estar incompleto.");
                continue;
            }

            result.Add(new DiscogsLabel(
                name,
                OptionalString(label, "catno", "catalog_number"),
                OptionalId(label, "id")));
        }

        return result;
    }

    private static List<DiscogsNameReference> MapNameReferences(
        JsonElement root,
        ICollection<string> warnings,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var values)
            || values.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (values.ValueKind != JsonValueKind.Array)
        {
            warnings.Add($"A lista de {propertyName} foi ignorada por estar incompleta.");
            return [];
        }

        var result = new List<DiscogsNameReference>();
        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                warnings.Add($"Uma referencia de {propertyName} foi ignorada por estar incompleta.");
                continue;
            }

            var name = OptionalString(value, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                warnings.Add($"Uma referencia de {propertyName} foi ignorada por estar incompleta.");
                continue;
            }

            result.Add(new DiscogsNameReference(OptionalId(value, "id"), name));
        }

        return result;
    }

    private static List<DiscogsImage> MapImages(
        JsonElement root,
        ICollection<string> warnings)
    {
        if (!root.TryGetProperty("images", out var images)
            || images.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (images.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("As imagens foram ignoradas por estarem incompletas.");
            return [];
        }

        var result = new List<DiscogsImage>();
        foreach (var image in images.EnumerateArray())
        {
            if (image.ValueKind != JsonValueKind.Object)
            {
                warnings.Add("Uma imagem foi ignorada por estar incompleta.");
                continue;
            }

            result.Add(new DiscogsImage(
                OptionalString(image, "type"),
                PublicUrl(image, "uri"),
                OptionalInt(image, "width"),
                OptionalInt(image, "height")));
        }

        return result;
    }

    private static IReadOnlyList<string> FormatValues(JsonElement root)
    {
        if (root.TryGetProperty("formats", out var formats)
            && formats.ValueKind == JsonValueKind.Array)
        {
            var result = new List<string>();
            foreach (var format in formats.EnumerateArray())
            {
                if (format.ValueKind == JsonValueKind.String)
                {
                    AddDistinct(result, format.GetString());
                    continue;
                }

                if (format.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = OptionalString(format, "name");
                var descriptions = StringValues(format, "descriptions");
                var value = string.IsNullOrWhiteSpace(name)
                    ? string.Join(", ", descriptions)
                    : descriptions.Count == 0
                        ? name
                        : $"{name} ({string.Join(", ", descriptions)})";
                AddDistinct(result, value);
            }

            return result;
        }

        var rawFormat = OptionalString(root, "format");
        return string.IsNullOrWhiteSpace(rawFormat)
            ? []
            : [rawFormat];
    }

    private static List<string> StringValues(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() is { } text && !string.IsNullOrWhiteSpace(text)
                ? text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                AddDistinct(result, item.GetString());
            }
        }

        return result;
    }

    private static DiscogsPagination MapPagination(JsonElement root, int uniqueItemsInPage)
    {
        if (!root.TryGetProperty("pagination", out var pagination)
            || pagination.ValueKind != JsonValueKind.Object)
        {
            throw new DiscogsInvalidResponseException("A resposta da Discogs nao contem uma paginacao valida.");
        }

        var page = RequiredPositiveInt(pagination, "page");
        var perPage = RequiredPositiveInt(pagination, "per_page");
        var totalItems = OptionalInt(pagination, "items");
        var totalPages = OptionalInt(pagination, "pages");
        if (totalItems is < 0 || totalPages is < 0)
        {
            throw new DiscogsInvalidResponseException("A paginacao retornada pela Discogs e invalida.");
        }

        var hasNextPage = totalPages is > 0
            ? page < totalPages.Value
            : pagination.TryGetProperty("urls", out var urls)
                && urls.ValueKind == JsonValueKind.Object
                && !string.IsNullOrWhiteSpace(OptionalString(urls, "next"));

        return new DiscogsPagination(
            page,
            perPage,
            totalItems,
            totalPages,
            hasNextPage,
            uniqueItemsInPage);
    }

    private static DiscogsSourceReference CreateSource(
        JsonElement root,
        string resourceType,
        string id)
        => new(
            Provider,
            resourceType,
            id,
            PublicUrl(root, "resource_url"));

    private static JsonElement RequireObject(JsonDocument document)
    {
        if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new DiscogsInvalidResponseException("A resposta da Discogs nao possui um objeto JSON valido.");
        }

        return document.RootElement;
    }

    private static JsonElement RequireArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            throw new DiscogsInvalidResponseException("A resposta da Discogs nao possui a lista esperada.");
        }

        return value;
    }

    private static void EnsureObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new DiscogsInvalidResponseException("Um item da resposta da Discogs nao possui um objeto valido.");
        }
    }

    private static string RequiredId(JsonElement root, string propertyName)
        => OptionalId(root, propertyName)
            ?? throw new DiscogsInvalidResponseException("A resposta da Discogs nao possui o identificador obrigatorio.");

    private static string RequiredString(JsonElement root, string propertyName)
        => OptionalString(root, propertyName)
            ?? throw new DiscogsInvalidResponseException("A resposta da Discogs nao possui o texto obrigatorio.");

    private static int RequiredPositiveInt(JsonElement root, string propertyName)
    {
        var value = OptionalInt(root, propertyName);
        return value is > 0
            ? value.Value
            : throw new DiscogsInvalidResponseException("A resposta da Discogs possui uma paginacao invalida.");
    }

    private static string? OptionalId(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var value)
                || value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number
                && value.TryGetInt64(out var number)
                && number > 0)
            {
                return number.ToString(CultureInfo.InvariantCulture);
            }

            if (value.ValueKind == JsonValueKind.String
                && long.TryParse(value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out number)
                && number > 0)
            {
                return number.ToString(CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static int? OptionalInt(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var value)
                || value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number
                && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String
                && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return null;
    }

    private static string? OptionalString(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var value)
                || value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = value.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static string? PublicUrl(JsonElement root, params string[] propertyNames)
    {
        var value = OptionalString(root, propertyNames);
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo)
            ? uri.AbsoluteUri
            : null;
    }

    private static int? ParseDuration(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            return null;
        }

        var parts = duration.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length is < 2 or > 3
            || !parts.All(part => int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out _)))
        {
            return null;
        }

        var values = parts.Select(part => int.Parse(part, CultureInfo.InvariantCulture)).ToArray();
        var seconds = parts.Length == 2
            ? values[0] * 60L + values[1]
            : values[0] * 3600L + values[1] * 60L + values[2];
        return seconds is > 0 and <= int.MaxValue ? (int)seconds : null;
    }

    private static bool IsUnofficial(string value)
        => value.Contains("unofficial", StringComparison.OrdinalIgnoreCase);

    private static void AddDistinct(ICollection<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !values.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value.Trim());
        }
    }

    private sealed class ReleaseSummaryAccumulator(DiscogsReleaseSummary initial)
    {
        private readonly List<string> _formats = [.. initial.Formats];
        private readonly List<string> _roles = [.. initial.Roles];

        public DiscogsReleaseSummary Representative { get; private set; } = initial;

        public void Merge(DiscogsReleaseSummary summary)
        {
            foreach (var format in summary.Formats)
            {
                AddDistinct(_formats, format);
            }

            foreach (var role in summary.Roles)
            {
                AddDistinct(_roles, role);
            }

            if (Representative.ResourceType != "master" && summary.ResourceType == "master")
            {
                Representative = summary;
            }
        }

        public DiscogsReleaseSummary ToSummary()
            => Representative with
            {
                Formats = _formats,
                Roles = _roles,
                Role = _roles.FirstOrDefault(),
                Category = Classify(
                    Representative.ResourceType,
                    Representative.Title,
                    _formats,
                    _roles)
            };
    }
}
