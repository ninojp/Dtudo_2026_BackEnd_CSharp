import { axiosHttpApiLocalMyAnimes } from "../../api_conect/conectApiLocal";
import { useEffect, useState } from "react";
import MyAnimesBDLocalContext from "./MyAnimesBDLocalContext";

export default function MyAnimesBDLocalProvider({ children }) {
    const [iCollectionObjsMyAnimes, setiCollectionObjsMyAnimes] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const controller = new AbortController();
        let ativo = true;

        async function fetchAllObjsDBLocalMyAnimes() {
            setIsLoading(true);
            setError(null);

            try {
                const response = await axiosHttpApiLocalMyAnimes().get('/apiLocal/myanime', {
                    signal: controller.signal,
                });
                if (ativo) setiCollectionObjsMyAnimes(response);
            } catch (erro) {
                if (erro.code === 'ERR_CANCELED' || !ativo) return;

                console.error("Erro ao buscar objetos MyAnimes: ", erro);
                setError('Nao foi possivel carregar as colecoes locais.');
            } finally {
                if (ativo) setIsLoading(false);
            }
        }

        fetchAllObjsDBLocalMyAnimes();
        return () => {
            ativo = false;
            controller.abort();
        };
    }, []);

    return (
        <MyAnimesBDLocalContext.Provider
            value={{
                iCollectionObjsMyAnimes,
                isLoading,
                error,
            }}
        >
            {children}
        </MyAnimesBDLocalContext.Provider>
    );
};
