import { useCallback, useEffect, useState } from "react";
import {
    getApiMusicXErrorMessage,
    listAllMusicCollections,
} from "../../services/apiMusicX";
import MyMusicxObjsListContext from "./MyMusicxObjsListContext";

export default function MyMusicxObjsListProvider({ children }) {
    const [listObjsMyMusicx, setListObjsMyMusicx] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);
    const [totalCount, setTotalCount] = useState(0);

    const carregarColecoes = useCallback(async (signal) => {
        try {
            const response = await listAllMusicCollections({ signal });
            if (!signal?.aborted) {
                setListObjsMyMusicx(response.items);
                setTotalCount(response.totalCount ?? response.items.length);
            }
            return response.items;
        } catch (requestError) {
            if (requestError.name === 'AbortError') {
                throw requestError;
            }

            console.error("Erro ao buscar Colecoes da ApiMusicX: ", requestError);
            if (!signal?.aborted) {
                setListObjsMyMusicx([]);
                setTotalCount(0);
                setError(requestError);
            }
            return null;
        } finally {
            if (!signal?.aborted) {
                setIsLoading(false);
            }
        }
    }, []);

    const fetchAllObjsMyMusicx = useCallback((signal) => {
        setIsLoading(true);
        setError(null);
        return carregarColecoes(signal);
    }, [carregarColecoes]);

    useEffect(() => {
        const controller = new AbortController();
        const carregar = async () => {
            try {
                await carregarColecoes(controller.signal);
            } catch {
                // Requisicao cancelada ao desmontar.
            }
        };
        carregar();

        return () => controller.abort();
    }, [carregarColecoes]);

    return (
        <MyMusicxObjsListContext.Provider value={
            {
                listObjsMyMusicx,
                isLoading,
                error,
                errorMessage: error ? getApiMusicXErrorMessage(error) : null,
                totalCount,
                fetchAllObjsMyMusicx,
            }}>
            {children}
        </MyMusicxObjsListContext.Provider>
    )
}
