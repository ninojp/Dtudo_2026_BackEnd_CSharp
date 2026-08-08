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
