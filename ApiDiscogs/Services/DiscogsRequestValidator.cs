using System.Globalization;
using ApiDiscogs.Dtos;

namespace ApiDiscogs.Services;

/// <summary>
/// Centraliza os limites de entrada publicados pela ApiDiscogs.
/// </summary>
public static class DiscogsRequestValidator
{
    /// <summary>
    /// Normaliza e valida o termo da busca de artistas.
    /// </summary>
    public static string NormalizeSearchQuery(ArtistSearchQuery? query)
    {
        var value = query?.Query;
        var normalized = string.Join(
            ' ',
            value?.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []);

        if (normalized.Length is < 2 or > 120)
        {
            throw new DiscogsValidationException(
                "O parametro q deve possuir entre 2 e 120 caracteres apos a normalizacao.");
        }

        return normalized;
    }

    /// <summary>
    /// Valida pagina e limite da busca de artistas.
    /// </summary>
    public static void ValidateArtistSearch(ArtistSearchQuery? query)
    {
        _ = NormalizeSearchQuery(query);
        ValidatePage(query?.Page ?? 0);
        ValidatePageSize(query?.PerPage ?? 0, 20);
    }

    /// <summary>
    /// Valida pagina, limite e expansao da discografia.
    /// </summary>
    public static string ValidateArtistReleases(ArtistReleasesQuery? query)
    {
        ValidatePage(query?.Page ?? 0);
        ValidatePageSize(query?.PerPage ?? 0, 100);

        var expand = query?.Expand?.Trim().ToLowerInvariant();
        if (expand is not ("none" or "master"))
        {
            throw new DiscogsValidationException("O parametro expand deve ser none ou master.");
        }

        return expand;
    }

    /// <summary>
    /// Valida um identificador de recurso recebido na rota.
    /// </summary>
    public static int ParseResourceId(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Any(character => character is < '0' or > '9')
            || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            || id <= 0)
        {
            throw new DiscogsValidationException(
                $"O parametro {parameterName} deve ser um ID Discogs decimal positivo.");
        }

        return id;
    }

    private static void ValidatePage(int page)
    {
        if (page < 1)
        {
            throw new DiscogsValidationException("O parametro page deve ser maior ou igual a 1.");
        }
    }

    private static void ValidatePageSize(int pageSize, int maximum)
    {
        if (pageSize is < 1 || pageSize > maximum)
        {
            throw new DiscogsValidationException(
                $"O parametro perPage deve estar entre 1 e {maximum}.");
        }
    }
}
