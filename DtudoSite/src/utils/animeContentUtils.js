const normalizarValor = (valor) => String(valor || '').trim().toLocaleLowerCase('pt-BR');

export function obterValoresAnime(valores) {
    if (!Array.isArray(valores)) return [];

    return valores
        .map((valor) => typeof valor === 'string' ? valor : valor?.name)
        .filter(Boolean);
}

export function obterTituloAnime(anime) {
    return anime?.title || anime?.titulo || anime?.nome || 'Anime sem titulo';
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

export function obterGenerosAnime(anime) {
    return [
        ...obterValoresAnime(anime?.genres),
        ...obterValoresAnime(anime?.explicitGenres || anime?.explicit_genres),
        ...obterValoresAnime(anime?.themes),
        ...obterValoresAnime(anime?.demographics),
    ];
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
    colecoesComAnime,
    incluirAdultos,
    listObjsDetalhesAnimes,
    malId,
}) {
    const malIdNumerico = Number(malId);
    const idsRelacionados = new Set(
        colecoesComAnime.flatMap(idsDaColecao)
            .map(Number)
            .filter((id) => id !== malIdNumerico)
    );

    let relacionados = listObjsDetalhesAnimes.filter((anime) => idsRelacionados.has(Number(obterIdAnime(anime))));

    if (relacionados.length === 0 && animeAtual) {
        const generosAtual = new Set(obterGenerosAnime(animeAtual));
        relacionados = listObjsDetalhesAnimes
            .filter((anime) => Number(obterIdAnime(anime)) !== malIdNumerico)
            .filter((anime) => obterGenerosAnime(anime).some((genero) => generosAtual.has(genero)))
            .slice(0, 24);
    }

    if (!incluirAdultos) {
        relacionados = relacionados.filter((anime) => !ehAnimeAdulto(anime));
    }

    return relacionados;
}
