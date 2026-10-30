import { useCallback, useContext, useEffect, useRef, useState } from 'react';
import AuthContext from '../context_api/AuthContext/AuthContext';
import {
    buscarProgressoAnime,
    desmarcarEpisodiosVistos,
    limparProgressoAnime,
    marcarEpisodiosVistos,
    marcarTodosEpisodiosVistos,
} from '../services/apiMyAnimes';
import { invalidarAnimeStatus } from './useAnimeStatus';

function descreverErro(erro) {
    switch (erro?.response?.status) {
        case 401:
            return 'Sua sessao expirou. Entre novamente para registrar os episodios.';
        case 403:
            return 'Sem permissao para o progresso pessoal. Saia e entre novamente para atualizar as permissoes.';
        case 404:
            return 'Este anime nao existe no banco local.';
        case 409:
            return 'O total de episodios deste anime e desconhecido.';
        case 413:
        case 400:
            return 'A solicitacao foi recusada. Atualize a pagina e tente novamente.';
        default:
            return 'Nao foi possivel atualizar o progresso de episodios.';
    }
}

/**
 * Carrega e altera o progresso de episodios vistos (usuario autenticado) de um anime.
 * Cada alteracao e confirmada pela API antes de atualizar a tela e invalida o cache de status do card.
 * Use `key={malId}` no componente que chama o hook para reiniciar o estado ao trocar de anime.
 */
export function useEpisodiosVistos(malId) {
    const autenticado = useContext(AuthContext)?.isAuthenticated === true;
    const [progresso, setProgresso] = useState(null);
    const [carregando, setCarregando] = useState(autenticado);
    const [ocupado, setOcupado] = useState(false);
    const [erro, setErro] = useState(null);
    const controllerRef = useRef(null);
    const ocupadoRef = useRef(false);

    useEffect(() => {
        if (!autenticado) return undefined;

        const controller = new AbortController();
        controllerRef.current = controller;

        async function carregar() {
            setCarregando(true);
            setErro(null);

            try {
                const recebido = await buscarProgressoAnime(malId, controller.signal);
                if (!controller.signal.aborted) setProgresso(recebido);
            } catch (falha) {
                if (controller.signal.aborted || falha?.code === 'ERR_CANCELED') return;
                setErro(descreverErro(falha));
            } finally {
                if (!controller.signal.aborted) setCarregando(false);
            }
        }

        carregar();
        return () => controller.abort();
    }, [autenticado, malId]);

    // Uma alteracao por vez: evita respostas fora de ordem sobrescrevendo o estado mais recente.
    const executar = useCallback(async (operacao) => {
        if (ocupadoRef.current || !controllerRef.current) return;

        ocupadoRef.current = true;
        setOcupado(true);
        setErro(null);

        try {
            const atualizado = await operacao(controllerRef.current.signal);
            if (controllerRef.current.signal.aborted) return;

            setProgresso(atualizado);
            invalidarAnimeStatus([malId]);
        } catch (falha) {
            if (falha?.code === 'ERR_CANCELED') return;
            setErro(descreverErro(falha));
        } finally {
            ocupadoRef.current = false;
            setOcupado(false);
        }
    }, [malId]);

    const alternarEpisodio = useCallback((episodio) => executar((signal) => (
        progresso?.episodes.includes(episodio)
            ? desmarcarEpisodiosVistos(malId, [episodio], signal)
            : marcarEpisodiosVistos(malId, [episodio], signal)
    )), [executar, malId, progresso]);

    const marcarTodos = useCallback(
        () => executar((signal) => marcarTodosEpisodiosVistos(malId, signal)),
        [executar, malId],
    );

    const limpar = useCallback(
        () => executar((signal) => limparProgressoAnime(malId, signal)),
        [executar, malId],
    );

    return { autenticado, progresso, carregando, ocupado, erro, alternarEpisodio, marcarTodos, limpar };
}
