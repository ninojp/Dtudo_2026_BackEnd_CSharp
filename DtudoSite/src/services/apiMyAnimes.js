import { axiosHttpBffCatalog } from "../api_conect/conectApiCatalog";

const TAMANHO_PAGINA_API_LOCAL = 500;
const MAX_RESULTADOS_BUSCA_LOCAL = 100;
const MAX_IDS_STATUS_POR_REQUISICAO = 200;
const CABECALHO_CSRF = 'X-CSRF-TOKEN';
const ESTADOS_STATUS_VALIDOS = new Set(['none', 'partial', 'complete']);

let promessaTokenCsrf = null;

export function limparTokenCsrfCatalogo() {
    promessaTokenCsrf = null;
}

async function obterTokenCsrf(cliente, signal, renovar = false) {
    if (renovar || !promessaTokenCsrf) {
        const promessa = cliente.get('/bff/antiforgery', { signal }).then((response) => {
            const token = response.data?.token;
            if (typeof token !== 'string' || !token) {
                throw new TypeError('O gateway retornou um token anti-CSRF invalido.');
            }

            return token;
        });

        promessaTokenCsrf = promessa;
        // Falha (inclusive cancelamento) nao pode ficar em cache.
        promessa.catch(() => {
            if (promessaTokenCsrf === promessa) promessaTokenCsrf = null;
        });
    }

    return promessaTokenCsrf;
}

function foiRejeitadoPorCsrf(erro) {
    return erro?.response?.status === 400 && erro.response.data?.error === 'invalid_csrf';
}

async function enviarComCsrf(cliente, signal, requisicao) {
    const enviar = async (renovarToken) => {
        const token = await obterTokenCsrf(cliente, signal, renovarToken);
        return requisicao({ [CABECALHO_CSRF]: token });
    };

    try {
        return await enviar(false);
    } catch (erro) {
        if (!foiRejeitadoPorCsrf(erro)) throw erro;
        return enviar(true);
    }
}

function postarStatusAnimes(cliente, malIds, signal) {
    return enviarComCsrf(cliente, signal, (headers) =>
        cliente.post('/api/catalog/animes/status', { malIds }, { headers, signal }));
}

function normalizarEstadoStatus(estado) {
    return ESTADOS_STATUS_VALIDOS.has(estado) ? estado : 'none';
}

/**
 * Busca, em lote, o status (visto, pasta, gostei) dos animes informados para o usuario autenticado.
 * Retorna um objeto malId -> { watch, folder, like }; animes ausentes na resposta nao aparecem no objeto.
 * Aceita mais de 200 ids dividindo em requisicoes sequenciais (limite do contrato por requisicao).
 */
export async function buscarStatusAnimes(malIds, signal) {
    const idsUnicos = [...new Set((malIds || []).filter((malId) => Number.isInteger(malId) && malId > 0))];
    const statusPorMalId = {};
    if (idsUnicos.length === 0) return statusPorMalId;

    const cliente = axiosHttpBffCatalog();

    for (let inicio = 0; inicio < idsUnicos.length; inicio += MAX_IDS_STATUS_POR_REQUISICAO) {
        const lote = idsUnicos.slice(inicio, inicio + MAX_IDS_STATUS_POR_REQUISICAO);
        const response = await postarStatusAnimes(cliente, lote, signal);

        if (!Array.isArray(response.data?.items)) {
            throw new TypeError('A ApiMyAnimes retornou uma resposta de status invalida.');
        }

        for (const item of response.data.items) {
            if (!Number.isInteger(item?.malId) || !lote.includes(item.malId)) continue;

            statusPorMalId[item.malId] = {
                watch: normalizarEstadoStatus(item.watch?.state),
                folder: normalizarEstadoStatus(item.folder?.state),
                like: typeof item.like === 'string' ? item.like : null,
            };
        }
    }

    return statusPorMalId;
}

function normalizarProgressoAnime(dados, malId) {
    if (!dados || typeof dados !== 'object' || !Array.isArray(dados.episodes)) {
        throw new TypeError('A ApiMyAnimes retornou uma resposta de progresso invalida.');
    }

    const episodios = [...new Set(dados.episodes.filter((numero) => Number.isInteger(numero) && numero > 0))]
        .sort((a, b) => a - b);

    return {
        malId,
        total: Number.isInteger(dados.total) && dados.total > 0 ? dados.total : null,
        watched: episodios.length,
        state: normalizarEstadoStatus(dados.state),
        episodes: episodios,
    };
}

function normalizarNumerosEpisodios(episodios) {
    return [...new Set((episodios || []).filter((numero) => Number.isInteger(numero) && numero > 0))]
        .sort((a, b) => a - b);
}

/**
 * Progresso de episodios vistos do usuario autenticado para um anime.
 * Retorna { malId, total, watched, state, episodes }; total e null quando o anime nao tem total conhecido.
 */
export async function buscarProgressoAnime(malId, signal) {
    const response = await axiosHttpBffCatalog().get(`/api/catalog/animes/${malId}/watch`, { signal });
    return normalizarProgressoAnime(response.data, malId);
}

/** Marca episodios como vistos (idempotente). A lista e enviada ordenada e sem repeticoes. */
export async function marcarEpisodiosVistos(malId, episodios, signal) {
    const cliente = axiosHttpBffCatalog();
    const lista = normalizarNumerosEpisodios(episodios);
    const response = await enviarComCsrf(cliente, signal, (headers) =>
        cliente.put(`/api/catalog/animes/${malId}/watch/episodes`, { episodes: lista }, { headers, signal }));
    return normalizarProgressoAnime(response.data, malId);
}

/** Desmarca episodios. Lista vazia nao chama a API: nada e apagado. */
export async function desmarcarEpisodiosVistos(malId, episodios, signal) {
    const lista = normalizarNumerosEpisodios(episodios);
    if (lista.length === 0) return buscarProgressoAnime(malId, signal);

    const cliente = axiosHttpBffCatalog();
    const response = await enviarComCsrf(cliente, signal, (headers) =>
        cliente.delete(`/api/catalog/animes/${malId}/watch/episodes`, { data: { episodes: lista }, headers, signal }));
    return normalizarProgressoAnime(response.data, malId);
}

/** Marca todos os episodios (exige total conhecido; a API responde 409 caso contrario). */
export async function marcarTodosEpisodiosVistos(malId, signal) {
    const cliente = axiosHttpBffCatalog();
    const response = await enviarComCsrf(cliente, signal, (headers) =>
        cliente.put(`/api/catalog/animes/${malId}/watch/all`, null, { headers, signal }));
    return normalizarProgressoAnime(response.data, malId);
}

/** Remove todo o progresso do usuario neste anime. */
export async function limparProgressoAnime(malId, signal) {
    const cliente = axiosHttpBffCatalog();
    const response = await enviarComCsrf(cliente, signal, (headers) =>
        cliente.delete(`/api/catalog/animes/${malId}/watch`, { headers, signal }));
    return normalizarProgressoAnime(response.data, malId);
}

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

