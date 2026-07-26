import { useCallback, useEffect, useState } from "react";
import { axiosHttpApiLocalMyAnimes } from "../../api_conect/conectApiLocal";
import AnimesDetalhesObjsListContext from "./AnimesDetalhesObjsListContext";

const TAMANHO_PAGINA_API_LOCAL = 500;

async function buscarTodosAnimesDaApiLocal(signal) {
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
