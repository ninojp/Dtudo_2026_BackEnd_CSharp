import { useCallback, useEffect, useMemo, useState } from "react";
import MyMusicXDetalhesContext from "./MyMusicXDetalhesContext";
import { useParams } from "react-router-dom";
import {
    getApiMusicXErrorMessage,
    getMusicCollection,
} from "../../services/apiMusicX";

export default function MyMusicXDetalhesProvider({ children }) {
    const { id } = useParams();
    const [myMusicXDetalhes, setMyMusicXDetalhes] = useState(null);
    const [currentDisplayIdState, setCurrentDisplayId] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);

    const fetchCollectionDetails = useCallback(async (signal) => {
        setIsLoading(true);
        setError(null);

        try {
            const collection = await getMusicCollection(id, { signal });
            if (!signal?.aborted) {
                setMyMusicXDetalhes(collection);
            }
            return collection;
        } catch (requestError) {
            if (requestError.name === 'AbortError') {
                throw requestError;
            }

            console.error("Erro ao buscar detalhes da Colecao na ApiMusicX: ", requestError);
            if (!signal?.aborted) {
                setMyMusicXDetalhes(null);
                setError(requestError);
            }
            return null;
        } finally {
            if (!signal?.aborted) {
                setIsLoading(false);
            }
        }
    }, [id]);

    useEffect(() => {
        setCurrentDisplayId(null);
        const controller = new AbortController();
        fetchCollectionDetails(controller.signal).catch(() => undefined);

        return () => controller.abort();
    }, [fetchCollectionDetails]);

    const currentDisplayId = useMemo(() => {
        if (currentDisplayIdState) return currentDisplayIdState;
        return myMusicXDetalhes?.releases?.[0]?.musicReleaseId || null;
    }, [myMusicXDetalhes, currentDisplayIdState]);

    return (
        <MyMusicXDetalhesContext.Provider 
            value={{
                myMusicXDetalhes,
                currentDisplayId,
                setCurrentDisplayId,
                isLoading,
                error,
                errorMessage: error ? getApiMusicXErrorMessage(error) : null,
                fetchCollectionDetails,
            }}
        >
            {children}
        </MyMusicXDetalhesContext.Provider>
    );
};
