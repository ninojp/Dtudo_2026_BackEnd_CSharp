import { use, useState, useMemo } from "react";
import { useParams } from "react-router-dom";
import MyAnimesObjsListContext from "../MyAnimesObjsListContext/MyAnimesObjsListContext";
import MyAnimesDetalhesContext from "./MyAnimesDetalhesContext";

export default function MyAnimesDetalhesProvider({ children }) {
    const { slug } = useParams();
    const { listObjsMyAnimes, isLoading: isListLoading } = use(MyAnimesObjsListContext);
    //------------------------------------------------------------------------------
    const myAnimesDetalhes = useMemo(() => {
        if (isListLoading || !listObjsMyAnimes) return null;
        
        // Primeiro tenta encontrar pelo slug
        const foundBySlug = listObjsMyAnimes.find(item => item.slug === slug);
        if (foundBySlug) return foundBySlug;
        
        // Se não encontrou pelo slug, tenta encontrar pelo mal_id nas subpastas
        // Converte o slug para número para comparar com mal_id
        const malId = Number(slug);
        if (!isNaN(malId)) {
            const foundByMalId = listObjsMyAnimes.find(item => 
                item.subpastas?.some(subpasta => subpasta.mal_id === malId)
            );
            if (foundByMalId) return foundByMalId;
        }
        
        return null;
    }, [listObjsMyAnimes, slug, isListLoading]);
    //----------------------------------------
    const [currentDisplayIdState, setCurrentDisplayId] = useState(null);
    //------------------------------------------------------------------
    const currentDisplayId = useMemo(() => {
        if (currentDisplayIdState) return currentDisplayIdState;
        
        // Se o parâmetro for um mal_id válido, usa ele como display ID
        const malId = Number(slug);
        if (!isNaN(malId) && myAnimesDetalhes?.subpastas?.some(s => s.mal_id === malId)) {
            return malId;
        }
        
        // Caso contrário, usa o primeiro mal_id das subpastas
        return myAnimesDetalhes?.subpastas?.[0]?.mal_id ?? null;
    }, [myAnimesDetalhes, currentDisplayIdState, slug]);
    //==========================================
    return (
        <MyAnimesDetalhesContext.Provider
            value={{
                isLoading: isListLoading,
                myAnimesDetalhes,
                currentDisplayId,
                setCurrentDisplayId, // Agora você pode usar isso para definir um ID manualmente
            }}
        >
            {children}
        </MyAnimesDetalhesContext.Provider>
    );
};
