import { use, useState, useMemo } from "react";
import { useParams } from "react-router-dom";
import AnimexDetalhesContext from "./AnimexDetalhesContext";
import AnimexObjsListContext from "../AnimexObjsListContext/AnimexObjsListContext";

export default function AnimexDetalhesProvider({ children }) {
    const { slug } = useParams();
    const { listObjsAnimex, isLoading: isListLoading } = use(AnimexObjsListContext);
    //------------------------------------------------------------------------------
    const animexDetalhes = useMemo(() => {
        if (isListLoading || !listObjsAnimex) return null;
        return listObjsAnimex.find(item => item.slug === slug);
    }, [listObjsAnimex, slug, isListLoading]);
    //----------------------------------------
    const [currentDisplayIdState, setCurrentDisplayId] = useState(null);
    //------------------------------------------------------------------
    const currentDisplayId = useMemo(() => {
        if (currentDisplayIdState) return currentDisplayIdState;
        return animexDetalhes?.subpastas?.[0]?.id ?? null;
    }, [animexDetalhes, currentDisplayIdState]);
    //==========================================
    return (
        <AnimexDetalhesContext.Provider
            value={{
                isLoading: isListLoading,
                animexDetalhes,
                currentDisplayId,
                setCurrentDisplayId, // Agora você pode usar isso para definir um ID manualmente
            }}
        >
            {children}
        </AnimexDetalhesContext.Provider>
    );
};
