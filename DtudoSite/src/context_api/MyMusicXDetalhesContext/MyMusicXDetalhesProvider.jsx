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
    const [idCarregado, setIdCarregado] = useState(id);

    if (idCarregado !== id) {
        setIdCarregado(id);
        setCurrentDisplayId(null);
        setIsLoading(true);
        setError(null);
    }

    const carregarColecao = useCallback(async (signal) => {
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

    const fetchCollectionDetails = useCallback((signal) => {
        setIsLoading(true);
        setError(null);
        return carregarColecao(signal);
    }, [carregarColecao]);

    useEffect(() => {
        const controller = new AbortController();
        const carregar = async () => {
            try {
                await carregarColecao(controller.signal);
            } catch {
                // Requisicao cancelada ao trocar de colecao ou desmontar.
            }
        };
        carregar();

        return () => controller.abort();
    }, [carregarColecao]);

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
