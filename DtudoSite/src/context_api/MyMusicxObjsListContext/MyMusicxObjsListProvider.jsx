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

    const fetchAllObjsMyMusicx = useCallback(async (signal) => {
        setIsLoading(true);
        setError(null);

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

    useEffect(() => {
        const controller = new AbortController();
        fetchAllObjsMyMusicx(controller.signal).catch(() => undefined);

        return () => controller.abort();
    }, [fetchAllObjsMyMusicx]);

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
