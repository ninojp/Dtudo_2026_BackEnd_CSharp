import { axiosHttpBffCatalog } from "../api_conect/conectApiCatalog";

const TAMANHO_PAGINA_API_LOCAL = 500;
const MAX_RESULTADOS_BUSCA_LOCAL = 100;

export async function buscarTodosAnimesDaApiLocal(signal) {
    const cliente = axiosHttpBffCatalog();
    let skip = 0;
    let todosOsAnimes = [];

    while (true) {
        const response = await cliente.get('/api/catalog/animes', {
            params: { skip, take: TAMANHO_PAGINA_API_LOCAL },
            signal,
        });

        if (!Array.isArray(response.data)) {
            throw new TypeError('A ApiMyAnimes retornou uma resposta de lista invalida.');
        }

        const paginaAtual = response.data;
        todosOsAnimes = todosOsAnimes.concat(paginaAtual);

        if (paginaAtual.length < TAMANHO_PAGINA_API_LOCAL) break;
        skip += TAMANHO_PAGINA_API_LOCAL;
    }

    return todosOsAnimes;
}

export async function buscarAnimePorMalId(malId, signal) {
    const response = await axiosHttpBffCatalog().get(`/api/catalog/animes/${malId}`, { signal });
    return response.data;
}

export async function buscarAnimesDaApiLocalPorTermo(termo, signal) {
    const response = await axiosHttpBffCatalog().get('/api/catalog/animes/search', {
        params: { termo, take: MAX_RESULTADOS_BUSCA_LOCAL },
        signal,
    });

    if (!Array.isArray(response.data)) {
        throw new TypeError('A ApiMyAnimes retornou uma resposta de busca invalida.');
    }

    return response.data;
}

export async function buscarTodasColecoesMyAnimeDaApiLocal(signal) {
    const cliente = axiosHttpBffCatalog();
    let skip = 0;
    let colecoes = [];

    while (true) {
        const response = await cliente.get('/api/catalog/collections', {
            params: { skip, take: TAMANHO_PAGINA_API_LOCAL },
            signal,
        });

        if (!Array.isArray(response.data)) {
            throw new TypeError('A ApiMyAnimes retornou uma resposta de colecoes invalida.');
        }

        const paginaAtual = response.data;
        colecoes = colecoes.concat(paginaAtual);

        if (paginaAtual.length < TAMANHO_PAGINA_API_LOCAL) break;
        skip += TAMANHO_PAGINA_API_LOCAL;
    }

    return colecoes;
}

export async function buscarColecaoMyAnimePorId(myAnimeId, signal) {
    const response = await axiosHttpBffCatalog().get(`/api/catalog/collections/${myAnimeId}`, { signal });
    return response.data;
}

const TAMANHO_LOTE_ANIMES_RELACIONADOS = 200;

export async function buscarAnimesPorMalIds(malIds, signal) {
    const idsUnicos = [...new Set((malIds || []).filter((malId) => Number.isInteger(malId) && malId > 0))];
    if (idsUnicos.length === 0) return [];

    const cliente = axiosHttpBffCatalog();
    let animes = [];

    for (let inicio = 0; inicio < idsUnicos.length; inicio += TAMANHO_LOTE_ANIMES_RELACIONADOS) {
        const lote = idsUnicos.slice(inicio, inicio + TAMANHO_LOTE_ANIMES_RELACIONADOS);
        const response = await cliente.get('/api/catalog/animes/relacionados', {
            params: { ids: lote },
            paramsSerializer: { indexes: null },
            signal,
        });

        if (!Array.isArray(response.data)) {
            throw new TypeError('A ApiMyAnimes retornou uma resposta de animes relacionados invalida.');
        }

        animes = animes.concat(response.data);
    }

    return animes;
}

