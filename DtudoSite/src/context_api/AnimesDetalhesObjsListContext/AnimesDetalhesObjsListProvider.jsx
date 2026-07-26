import { useCallback, useEffect, useState } from "react";
import { buscarTodosAnimesDaApiLocal } from "../../services/apiMyAnimes";
import AnimesDetalhesObjsListContext from "./AnimesDetalhesObjsListContext";

export default function AnimesDetalhesObjsListProvider({ children }) {
    const [listObjsDetalhesAnimes, setListObjsDetalhesAnimes] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);
    const [tentativa, setTentativa] = useState(0);
    const recarregarAnimes = useCallback(() => setTentativa((valor) => valor + 1), []);

    useEffect(() => {
        const controller = new AbortController();
        let ativo = true;

        async function carregarAnimes() {
            setIsLoading(true);
            setError(null);

            try {
                const todosOsAnimes = await buscarTodosAnimesDaApiLocal(controller.signal);
                if (ativo) setListObjsDetalhesAnimes(todosOsAnimes);
            } catch (erro) {
                if (erro.code === 'ERR_CANCELED' || !ativo) return;

                console.error('Erro ao buscar animes da ApiMyAnimes:', erro);
                setError('Nao foi possivel carregar os animes. Verifique a ApiMyAnimes e tente novamente.');
            } finally {
                if (ativo) setIsLoading(false);
            }
        }

        carregarAnimes();
        return () => {
            ativo = false;
            controller.abort();
        };
    }, [tentativa]);

    return (
        <AnimesDetalhesObjsListContext.Provider
            value={{
                listObjsDetalhesAnimes,
                isLoading,
                error,
                recarregarAnimes,
            }}
        >
            {children}
        </AnimesDetalhesObjsListContext.Provider>
    );
};
