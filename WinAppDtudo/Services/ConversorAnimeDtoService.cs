using System.Globalization;
using System.Text.RegularExpressions;
using LibDtudo.Shared.Dtos;

namespace WinAppDtudo.Services;

public static class ConversorAnimeDtoService
{
    public static AdicionaAnimeDto CriarAdicionaAnimeDto(JikanAnimeDetalhes anime, int myAnimeId)
    {
        var episodios = anime.Episodes.HasValue && anime.Episodes.Value > 0
            ? anime.Episodes.Value
            : 1;

        var imagens = new List<string>();
        if (!string.IsNullOrWhiteSpace(anime.Images?.Jpg?.ImageUrl)) imagens.Add(anime.Images.Jpg.ImageUrl);
        if (!string.IsNullOrWhiteSpace(anime.Images?.Jpg?.SmallImageUrl)) imagens.Add(anime.Images.Jpg.SmallImageUrl);
        if (!string.IsNullOrWhiteSpace(anime.Images?.Jpg?.LargeImageUrl)) imagens.Add(anime.Images.Jpg.LargeImageUrl);

        var subtitulos = new List<string>();
        if (!string.IsNullOrWhiteSpace(anime.TitleEnglish)) subtitulos.Add(anime.TitleEnglish);
        if (!string.IsNullOrWhiteSpace(anime.TitleJapanese)) subtitulos.Add(anime.TitleJapanese);
        subtitulos.AddRange(anime.TitleSynonyms);

        return new AdicionaAnimeDto
        {
            MalId = anime.MalId,
            Titulo = !string.IsNullOrWhiteSpace(anime.Title) ? anime.Title : $"Anime_{anime.MalId}",
            Episodios = episodios,
            MyAnimeID = myAnimeId,
            MalUrl = anime.Url ?? string.Empty,
            ImagensUrlMal = imagens.Distinct().ToList(),
            SubTitulos = subtitulos.Distinct().ToList(),
            Trailer = anime.Trailer,
            Approved = anime.Approved,
            Title = anime.Title,
            TitleEnglish = anime.TitleEnglish,
            TitleJapanese = anime.TitleJapanese,
            TitleSynonyms = [.. anime.TitleSynonyms],
            Type = anime.Type,
            Source = anime.Source,
            Episodes = anime.Episodes,
            Status = anime.Status,
            Airing = anime.Airing,
            Aired = anime.Aired,
            Duration = anime.Duration,
            Rating = anime.Rating,
            Score = anime.Score,
            ScoredBy = anime.ScoredBy,
            Rank = anime.Rank,
            Popularity = anime.Popularity,
            Members = anime.Members,
            Favorites = anime.Favorites,
            Synopsis = anime.Synopsis,
            Background = anime.Background,
            Season = anime.Season,
            Year = DeterminarAnoLancamento(anime),
            Producers = [.. anime.Producers],
            Licensors = [.. anime.Licensors],
            Studios = [.. anime.Studios],
            Genres = [.. anime.Genres],
            ExplicitGenres = [.. anime.ExplicitGenres],
            Themes = [.. anime.Themes],
            Demographics = [.. anime.Demographics]
        };
    }

    private static int? DeterminarAnoLancamento(JikanAnimeDetalhes anime)
    {
        if (anime.Year.HasValue)
            return anime.Year.Value;

        if (string.IsNullOrWhiteSpace(anime.Aired))
            return null;

        var dataInicialTexto = anime.Aired.Split(" to ", StringSplitOptions.TrimEntries)[0];

        if (DateTime.TryParseExact(
            dataInicialTexto,
            ["MMM dd, yyyy", "MMM d, yyyy"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dataInicial))
        {
            return dataInicial.Year;
        }

        var matchAno = Regex.Match(dataInicialTexto, @"\b(19|20)\d{2}\b");
        if (matchAno.Success && int.TryParse(matchAno.Value, out var ano))
            return ano;

        return null;
    }
}
