using ApiMyAnimes.Data;
using LibDtudo.Shared.Models;
using LibDtudo.Shared.Search;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Services;

/// <summary>
/// Executa a busca local de animes com prioridade para colecoes MyAnime.
/// </summary>
/// <param name="context">Contexto do banco local.</param>
public sealed class AnimeBuscaLocalService(MyAnimesContext context)
{
    private const int MaxTake = 500;

    /// <summary>
    /// Busca animes pelo termo informado.
    /// </summary>
    /// <param name="termo">Termo digitado pelo usuario.</param>
    /// <param name="take">Quantidade maxima de resultados.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Lista de animes encontrados.</returns>
    public async Task<List<Anime>> BuscarAsync(string? termo, int take, CancellationToken cancellationToken)
    {
        var termoNormalizado = AnimeSearchTextNormalizer.Normalize(termo);
        if (termoNormalizado.IsEmpty) return [];

        var takeSeguro = Math.Clamp(take, 1, MaxTake);
        var colecoesEncontradas = await BuscarColecoesAsync(termoNormalizado, cancellationToken);

        if (colecoesEncontradas.Count > 0)
        {
            var animesDasColecoes = await BuscarAnimesDasColecoesAsync(colecoesEncontradas, takeSeguro, cancellationToken);
            if (animesDasColecoes.Count > 0) return animesDasColecoes;
        }

        return await BuscarAnimesPorTitulosAsync(termoNormalizado, takeSeguro, cancellationToken);
    }

    private async Task<List<MyAnime>> BuscarColecoesAsync(
        AnimeSearchText termoNormalizado,
        CancellationToken cancellationToken)
    {
        var colecoes = await context.MyAnimes
            .AsNoTracking()
            .Where(colecao => colecao.Titulo != string.Empty)
            .ToListAsync(cancellationToken);

        return colecoes
            .Where(colecao => AnimeSearchTextNormalizer.Normalize(colecao.Titulo).Matches(termoNormalizado))
            .OrderBy(colecao => colecao.Titulo)
            .ToList();
    }

    private async Task<List<Anime>> BuscarAnimesDasColecoesAsync(
        IReadOnlyList<MyAnime> colecoes,
        int take,
        CancellationToken cancellationToken)
    {
        var idsOrdenados = colecoes
            .SelectMany(colecao => colecao.AnimesMalId)
            .Select(NumberOrNull)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .Take(take)
            .ToList();

        if (idsOrdenados.Count == 0) return [];

        var animesPorId = await context.Animes
            .AsNoTracking()
            .Where(anime => idsOrdenados.Contains(anime.MalId))
            .ToDictionaryAsync(anime => anime.MalId, cancellationToken);

        return idsOrdenados
            .Where(animesPorId.ContainsKey)
            .Select(id => animesPorId[id])
            .ToList();
    }

    private async Task<List<Anime>> BuscarAnimesPorTitulosAsync(
        AnimeSearchText termoNormalizado,
        int take,
        CancellationToken cancellationToken)
    {
        var animes = await context.Animes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return animes
            .Select(anime => new
            {
                Anime = anime,
                Score = CalcularScoreAnime(anime, termoNormalizado)
            })
            .Where(resultado => resultado.Score > 0)
            .OrderByDescending(resultado => resultado.Score)
            .ThenBy(resultado => resultado.Anime.Titulo)
            .Take(take)
            .Select(resultado => resultado.Anime)
            .ToList();
    }

    private static int CalcularScoreAnime(Anime anime, AnimeSearchText termoNormalizado)
    {
        var campos = ObterCamposBuscaAnime(anime).ToList();
        var melhorScore = 0;

        foreach (var campo in campos)
        {
            var textoNormalizado = AnimeSearchTextNormalizer.Normalize(campo.Texto);
            if (!textoNormalizado.Matches(termoNormalizado)) continue;

            var score = campo.Peso;
            if (textoNormalizado.Value == termoNormalizado.Value) score += 50;
            if (textoNormalizado.Value.StartsWith(termoNormalizado.Value, StringComparison.Ordinal)) score += 25;
            if (textoNormalizado.CompactValue == termoNormalizado.CompactValue) score += 20;

            melhorScore = Math.Max(melhorScore, score);
        }

        return melhorScore;
    }

    private static IEnumerable<(string? Texto, int Peso)> ObterCamposBuscaAnime(Anime anime)
    {
        yield return (anime.Titulo, 100);
        yield return (anime.Title, 95);
        yield return (anime.TitleEnglish, 90);
        yield return (anime.TitleJapanese, 90);

        foreach (var sinonimo in anime.TitleSynonyms)
        {
            yield return (sinonimo, 80);
        }

        foreach (var subTitulo in anime.SubTitulos)
        {
            yield return (subTitulo, 70);
        }
    }

    private static int? NumberOrNull(int value) => value > 0 ? value : null;
}
