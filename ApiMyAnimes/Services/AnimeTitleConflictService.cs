using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Models;
using LibDtudo.Shared.Search;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Services;

/// <summary>
/// Localiza títulos equivalentes entre um anime candidato e os registros locais.
/// </summary>
public sealed class AnimeTitleConflictService(MyAnimesContext context)
{
    public async Task<ConflitoTituloAnimeDto?> BuscarAsync(
        Anime animeCandidato,
        CancellationToken cancellationToken,
        int? malIdIgnorado = null)
    {
        var titulosCandidato = ObterTitulos(animeCandidato).ToList();
        if (titulosCandidato.Count == 0)
            return null;

        var animesExistentes = await context.Animes
            .AsNoTracking()
            .Where(anime => !malIdIgnorado.HasValue || anime.MalId != malIdIgnorado.Value)
            .ToListAsync(cancellationToken);

        foreach (var animeExistente in animesExistentes)
        {
            var tituloEmConflito = AnimeTitleEquivalence.FindEquivalentTitle(
                titulosCandidato,
                ObterTitulos(animeExistente));

            if (tituloEmConflito is not null)
            {
                return new ConflitoTituloAnimeDto
                {
                    MalId = animeExistente.MalId,
                    Titulo = animeExistente.Titulo,
                    TituloEmConflito = tituloEmConflito
                };
            }
        }

        return null;
    }

    public static IEnumerable<string?> ObterTitulos(Anime anime)
    {
        yield return anime.Titulo;
        yield return anime.Title;
        yield return anime.TitleEnglish;
        yield return anime.TitleJapanese;

        foreach (var titulo in anime.TitleSynonyms)
            yield return titulo;

        foreach (var titulo in anime.SubTitulos)
            yield return titulo;
    }
}
