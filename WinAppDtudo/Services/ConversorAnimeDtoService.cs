using System.Globalization;
using LibDtudo.Shared.Dtos.MyAnimeList;
using System.Text.RegularExpressions;
using LibDtudo.Shared.Dtos;

namespace WinAppDtudo.Services;

public static class ConversorAnimeDtoService
{
    public static AdicionaAnimeDto CriarAdicionaAnimeDto(AnimeDetails anime, int myAnimeId)
    {
        var episodiosInformados = anime.Episodes is > 0 ? anime.Episodes : null;
        var episodios = episodiosInformados ?? 1;

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
            Episodes = episodiosInformados,
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

    public static AtualizaAnimeDto CriarAtualizaAnimeDto(AnimeDetails anime, int myAnimeId)
    {
        var adicionaAnimeDto = CriarAdicionaAnimeDto(anime, myAnimeId);
        return new AtualizaAnimeDto
        {
            Titulo = adicionaAnimeDto.Titulo,
            Episodios = adicionaAnimeDto.Episodios,
            MyAnimeID = adicionaAnimeDto.MyAnimeID,
            MalUrl = adicionaAnimeDto.MalUrl,
            ImagensUrlMal = [.. adicionaAnimeDto.ImagensUrlMal],
            SubTitulos = [.. adicionaAnimeDto.SubTitulos],
            Trailer = adicionaAnimeDto.Trailer,
            Approved = adicionaAnimeDto.Approved,
            Title = adicionaAnimeDto.Title,
            TitleEnglish = adicionaAnimeDto.TitleEnglish,
            TitleJapanese = adicionaAnimeDto.TitleJapanese,
            TitleSynonyms = [.. adicionaAnimeDto.TitleSynonyms],
            Type = adicionaAnimeDto.Type,
            Source = adicionaAnimeDto.Source,
            Episodes = adicionaAnimeDto.Episodes,
            Status = adicionaAnimeDto.Status,
            Airing = adicionaAnimeDto.Airing,
            Aired = adicionaAnimeDto.Aired,
            Duration = adicionaAnimeDto.Duration,
            Rating = adicionaAnimeDto.Rating,
            Score = adicionaAnimeDto.Score,
            ScoredBy = adicionaAnimeDto.ScoredBy,
            Rank = adicionaAnimeDto.Rank,
            Popularity = adicionaAnimeDto.Popularity,
            Members = adicionaAnimeDto.Members,
            Favorites = adicionaAnimeDto.Favorites,
            Synopsis = adicionaAnimeDto.Synopsis,
            Background = adicionaAnimeDto.Background,
            Season = adicionaAnimeDto.Season,
            Year = adicionaAnimeDto.Year,
            Producers = [.. adicionaAnimeDto.Producers],
            Licensors = [.. adicionaAnimeDto.Licensors],
            Studios = [.. adicionaAnimeDto.Studios],
            Genres = [.. adicionaAnimeDto.Genres],
            ExplicitGenres = [.. adicionaAnimeDto.ExplicitGenres],
            Themes = [.. adicionaAnimeDto.Themes],
            Demographics = [.. adicionaAnimeDto.Demographics]
        };
    }

    public static AtualizaAnimeDto CriarAtualizaAnimeDto(ObterAnimeDto anime)
    {
        ArgumentNullException.ThrowIfNull(anime);

        return new AtualizaAnimeDto
        {
            Titulo = anime.Titulo,
            Episodios = anime.Episodios,
            MyAnimeID = anime.MyAnimeID,
            MalUrl = anime.MalUrl,
            ImagensUrlMal = [.. anime.ImagensUrlMal],
            SubTitulos = [.. anime.SubTitulos],
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
            Year = anime.Year,
            Producers = [.. anime.Producers],
            Licensors = [.. anime.Licensors],
            Studios = [.. anime.Studios],
            Genres = [.. anime.Genres],
            ExplicitGenres = [.. anime.ExplicitGenres],
            Themes = [.. anime.Themes],
            Demographics = [.. anime.Demographics]
        };
    }

    private static int? DeterminarAnoLancamento(AnimeDetails anime)
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
