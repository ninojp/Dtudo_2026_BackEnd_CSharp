import { createElement } from 'react';
import { FaBullhorn, FaCirclePlay, FaFilm, FaMusic, FaTv, FaVideo } from 'react-icons/fa6';
import { MdMovieCreation, MdOndemandVideo } from 'react-icons/md';

const normalizarValor = (valor) => String(valor || '').trim().toLocaleLowerCase('pt-BR');
const mesesEmIngles = {
    jan: 0,
    january: 0,
    feb: 1,
    february: 1,
    mar: 2,
    march: 2,
    apr: 3,
    april: 3,
    may: 4,
    jun: 5,
    june: 5,
    jul: 6,
    july: 6,
    aug: 7,
    august: 7,
    sep: 8,
    sept: 8,
    september: 8,
    oct: 9,
    october: 9,
    nov: 10,
    november: 10,
    dec: 11,
    december: 11,
};

export function obterValoresAnime(valores) {
    if (!Array.isArray(valores)) return [];

    return valores
        .map((valor) => typeof valor === 'string' ? valor : valor?.name)
        .filter(Boolean);
}

export function obterTituloAnime(anime) {
    return anime?.title || anime?.titulo || anime?.nome || 'Anime sem titulo';
}

export function obterTituloAlternativoAnime(anime) {
    const sinonimos = [
        ...obterValoresAnime(anime?.titleSynonyms || anime?.title_synonyms),
        ...obterValoresAnime(anime?.alternativeTitles?.synonyms || anime?.alternative_titles?.synonyms),
        ...obterValoresAnime(anime?.synonyms),
        ...obterValoresAnime(anime?.subTitulos || anime?.sub_titulos),
    ];
    const titulos = [
        anime?.titleEnglish,
        anime?.title_english,
        anime?.alternativeTitles?.english,
        anime?.alternative_titles?.english,
        ...sinonimos,
        anime?.titleJapanese,
        anime?.title_japanese,
        anime?.alternativeTitles?.japanese,
        anime?.alternative_titles?.japanese,
    ];

    return titulos.find((titulo) => typeof titulo === 'string' && titulo.trim())?.trim() || null;
}

export function obterIdAnime(anime) {
    return anime?.malId ?? anime?.mal_id;
}

export function obterAnoAnime(anime) {
    if (anime?.year) return anime.year;
    if (anime?.aired?.prop?.from?.year) return anime.aired.prop.from.year;

    const anoNoPeriodo = String(anime?.aired || '').match(/\b(19|20)\d{2}\b/);
    return anoNoPeriodo?.[0] || null;
}

export function obterTimestampLancamentoAnime(anime) {
    const dataInicial = anime?.aired?.prop?.from;
    if (dataInicial?.year) {
        return Date.UTC(dataInicial.year, (dataInicial.month || 1) - 1, dataInicial.day || 1);
    }

    const dataIso = anime?.aired?.from || anime?.from;
    const timestampIso = Date.parse(dataIso);
    if (Number.isFinite(timestampIso)) return timestampIso;

    const aired = String(anime?.aired || '');
    const dataIsoNoTexto = aired.match(/\b(19|20)\d{2}-\d{2}-\d{2}\b/)?.[0];
    const timestampIsoNoTexto = Date.parse(dataIsoNoTexto);
    if (Number.isFinite(timestampIsoNoTexto)) return timestampIsoNoTexto;

    const dataComMes = aired.match(/\b([A-Za-z]+)\.?\s+(\d{1,2})?,?\s*((?:19|20)\d{2})\b/);
    if (dataComMes) {
        const mes = mesesEmIngles[dataComMes[1].toLocaleLowerCase('en-US')];
        const dia = Number(dataComMes[2]) || 1;
        const ano = Number(dataComMes[3]);
        if (mes !== undefined) return Date.UTC(ano, mes, dia);
    }

    const ano = Number(obterAnoAnime(anime));
    if (Number.isInteger(ano)) return Date.UTC(ano, 0, 1);

    return Number.MAX_SAFE_INTEGER;
}

export function obterGenerosAnime(anime) {
    return [
        ...obterValoresAnime(anime?.genres),
        ...obterValoresAnime(anime?.explicitGenres || anime?.explicit_genres),
        ...obterValoresAnime(anime?.themes),
        ...obterValoresAnime(anime?.demographics),
    ];
}

export function obterTipoAnime(anime) {
    const tipo = anime?.type || anime?.mediaType || anime?.media_type;
    return typeof tipo === 'string' && tipo.trim() ? tipo.trim().toLocaleUpperCase('pt-BR') : null;
}

function normalizarTipoAnime(tipo) {
    return String(tipo || '')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toLocaleLowerCase('pt-BR')
        .trim()
        .replace(/[\s-]+/g, '_');
}

export function obterTipoCanonicoAnime(anime) {
    const tipo = normalizarTipoAnime(obterTipoAnime(anime));

    if (tipo === 'tv' || tipo === 'tv_series' || tipo === 'series') return 'TV';
    if (tipo === 'ova') return 'OVA';
    if (tipo === 'ona') return 'ONA';
    if (tipo === 'movie' || tipo === 'movies' || tipo === 'film' || tipo === 'filme') return 'MOVIE';
    if (tipo === 'special' || tipo === 'specials' || tipo === 'tv_special') return 'SPECIAL';
    if (tipo === 'music' || tipo === 'musics') return 'MUSIC';
    if (tipo === 'cm' || tipo === 'commercial' || tipo === 'commercials') return 'CM';
    if (tipo === 'pv' || tipo === 'promotional_video' || tipo === 'promotional_videos') return 'PV';

    return tipo.toLocaleUpperCase('pt-BR');
}

export function obterIconeTipoAnime(anime) {
    const icones = {
        TV: FaTv,
        OVA: MdOndemandVideo,
        ONA: FaCirclePlay,
        MOVIE: FaFilm,
        SPECIAL: MdMovieCreation,
        MUSIC: FaMusic,
        CM: FaBullhorn,
        PV: FaVideo,
    };

    const Icone = icones[obterTipoCanonicoAnime(anime)] || MdMovieCreation;
    return createElement(Icone, { 'aria-hidden': 'true' });
}

export function obterScoreAnime(anime) {
    const score = anime?.score ?? anime?.mean;
    const scoreNumerico = Number(score);

    if (!Number.isFinite(scoreNumerico)) return null;

    return scoreNumerico.toLocaleString('pt-BR', {
        minimumFractionDigits: scoreNumerico % 1 === 0 ? 0 : 1,
        maximumFractionDigits: 2,
    });
}

export function obterImagemAnime(anime) {
    return anime?.imagensUrlMal?.[0]
        || anime?.images?.webp?.image_url
        || anime?.image
        || null;
}

export function ehAnimeAdulto(anime) {
    const possuiGeneroHentai = obterGenerosAnime(anime)
        .some((genero) => normalizarValor(genero) === 'hentai');
    const classificacao = normalizarValor(anime?.rating);

    return possuiGeneroHentai
        || classificacao.includes('hentai')
        || classificacao.startsWith('rx');
}

export function idsDaColecao(colecao) {
    return Array.isArray(colecao?.animesMalId) ? colecao.animesMalId : [];
}

export function obterColecoesComAnime(colecoes, malId) {
    const malIdNumerico = Number(malId);
    return colecoes.filter((colecao) => idsDaColecao(colecao).some((id) => Number(id) === malIdNumerico));
}

export function obterAnimesRelacionados({
    animeAtual,
    incluirAdultos,
    listObjsDetalhesAnimes,
}) {
    const idsRelacionados = new Set(
        (animeAtual?.animesRelacionadosIds || animeAtual?.AnimesRelacionadosIds || [])
            .map(Number)
    );

    let relacionados = listObjsDetalhesAnimes.filter((anime) => idsRelacionados.has(Number(obterIdAnime(anime))));

    if (!incluirAdultos) {
        relacionados = relacionados.filter((anime) => !ehAnimeAdulto(anime));
    }

    return relacionados.toSorted((animeA, animeB) => {
        const diferencaData = obterTimestampLancamentoAnime(animeA) - obterTimestampLancamentoAnime(animeB);
        if (diferencaData !== 0) return diferencaData;

        return obterTituloAnime(animeA).localeCompare(obterTituloAnime(animeB), 'pt-BR');
    });
}
