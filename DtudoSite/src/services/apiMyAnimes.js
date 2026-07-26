import { axiosHttpApiLocalMyAnimes } from "../api_conect/conectApiLocal";

const TAMANHO_PAGINA_API_LOCAL = 500;

export async function buscarTodosAnimesDaApiLocal(signal) {
    const cliente = axiosHttpApiLocalMyAnimes();
    let skip = 0;
    let todosOsAnimes = [];

    while (true) {
        const response = await cliente.get('/apiLocal/Anime', {
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
    const response = await axiosHttpApiLocalMyAnimes().get(`/apiLocal/Anime/${malId}`, { signal });
    return response.data;
}

export async function buscarTodasColecoesMyAnimeDaApiLocal(signal) {
    const cliente = axiosHttpApiLocalMyAnimes();
    let skip = 0;
    let colecoes = [];

    while (true) {
        const response = await cliente.get('/apiLocal/MyAnime', {
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
