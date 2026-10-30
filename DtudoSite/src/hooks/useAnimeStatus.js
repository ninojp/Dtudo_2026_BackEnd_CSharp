import { useContext, useEffect, useMemo, useSyncExternalStore } from 'react';
import AuthContext from '../context_api/AuthContext/AuthContext';
import { buscarStatusAnimes, limparTokenCsrfCatalogo } from '../services/apiMyAnimes';

const ATRASO_BUSCA_MS = 200;

export const STATUS_ANIME_PADRAO = Object.freeze({
    watch: 'none',
    folder: 'none',
    like: null,
});

export function normalizarMalIds(malIds) {
    if (!Array.isArray(malIds)) return [];

    const ids = malIds
        .map(Number)
        .filter((id) => Number.isInteger(id) && id > 0);

    return [...new Set(ids)];
}

// Cache em memoria da sessao do navegador: malId -> status. Nao e persistido.
const cacheStatus = new Map();
const ouvintes = new Set();
let versaoCache = 0;

function notificar() {
    versaoCache += 1;
    ouvintes.forEach((ouvinte) => ouvinte());
}

function assinar(ouvinte) {
    ouvintes.add(ouvinte);
    return () => ouvintes.delete(ouvinte);
}

const obterVersaoCache = () => versaoCache;

/** Descarta o cache (e o token anti-CSRF) para que o proximo render busque os status novamente. */
export function limparCacheAnimeStatus() {
    limparTokenCsrfCatalogo();
    if (cacheStatus.size === 0) return;

    cacheStatus.clear();
    notificar();
}

/** Invalida apenas os animes informados (ex.: apos marcar episodios vistos). */
export function invalidarAnimeStatus(malIds) {
    let alterou = false;
    for (const malId of normalizarMalIds(malIds)) {
        alterou = cacheStatus.delete(malId) || alterou;
    }

    if (alterou) notificar();
}

if (typeof window !== 'undefined') {
    window.addEventListener('dtudo:bff-session-expired', limparCacheAnimeStatus);
}

/**
 * Recebe os malIds visiveis e devolve um mapa malId -> { watch, folder, like }.
 * Faz uma unica requisicao em lote por mudanca da lista (com cancelamento), apenas para ids ainda fora do cache.
 * Sem sessao autenticada ou em caso de falha, devolve o status padrao (icones cinza) sem quebrar o card.
 */
export function useAnimeStatus(malIds) {
    const autenticado = useContext(AuthContext)?.isAuthenticated === true;
    const chave = normalizarMalIds(malIds).join(',');
    const versao = useSyncExternalStore(assinar, obterVersaoCache, obterVersaoCache);

    useEffect(() => {
        if (!autenticado) {
            limparCacheAnimeStatus();
            return undefined;
        }

        const pendentes = chave
            ? chave.split(',').map(Number).filter((id) => !cacheStatus.has(id))
            : [];
        if (pendentes.length === 0) return undefined;

        const controller = new AbortController();
        const temporizador = setTimeout(async () => {
            try {
                const recebidos = await buscarStatusAnimes(pendentes, controller.signal);
                if (controller.signal.aborted) return;

                // Ids ausentes na resposta nao existem no banco local: guardam o padrao para nao repetir a consulta.
                for (const id of pendentes) {
                    cacheStatus.set(id, recebidos[id] ?? STATUS_ANIME_PADRAO);
                }
                notificar();
            } catch (erro) {
                if (controller.signal.aborted || erro?.code === 'ERR_CANCELED') return;

                console.error('Nao foi possivel carregar o status dos animes:', erro?.message ?? erro);
            }
        }, ATRASO_BUSCA_MS);

        return () => {
            clearTimeout(temporizador);
            controller.abort();
        };
    }, [autenticado, chave]);

    return useMemo(() => {
        const mapa = {};
        if (!chave) return mapa;

        for (const id of chave.split(',')) {
            mapa[id] = cacheStatus.get(Number(id)) ?? STATUS_ANIME_PADRAO;
        }

        return mapa;
    // versao muda sempre que o cache e alterado.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [chave, versao]);
}