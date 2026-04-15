import { use, useMemo, useState } from "react";
import MyMusicXDetalhesContext from "./MyMusicXDetalhesContext";
import MyMusicxObjsListContext from "../MyMusicxObjsListContext/MyMusicxObjsListContext";
import { useParams } from "react-router-dom";

export default function MyMusicXDetalhesProvider({ children }) {
    const { id } = useParams();
    const [currentDisplayIdState, setCurrentDisplayId] = useState(null);
    const {listObjsMyMusicx, isLoading: isListLoading } = use(MyMusicxObjsListContext);
    // Retorna o objeto detalhes baseado no id ou id_slug
    const myMusicXDetalhes = useMemo(() => {
        if (isListLoading || !listObjsMyMusicx) return null;
        const foundBySlug = listObjsMyMusicx.find(item => item.id === id);
        if (foundBySlug) return foundBySlug;
        return null;
    }, [listObjsMyMusicx, id, isListLoading]);
    // verifica o currentDisplayIdState ou pega o primeiro discogs_id dos albums
    const currentDisplayId = useMemo(() => {
        if (currentDisplayIdState) return currentDisplayIdState;
        // Pega o primeiro album que tem master_release_id ou discogs_id válido
        const firstAlbumWithId = myMusicXDetalhes?.releases?.albums?.find(
            album => (album.master_release_id && album.master_release_id.trim() !== '') || 
                     (album.discogs_id && album.discogs_id.trim() !== '')
        );
        return firstAlbumWithId?.master_release_id || firstAlbumWithId?.discogs_id || null;
    }, [myMusicXDetalhes, currentDisplayIdState]);
    //==========================================
    return (
        <MyMusicXDetalhesContext.Provider 
            value={{ myMusicXDetalhes, currentDisplayId, setCurrentDisplayId, isLoading: isListLoading }}
        >
            {children}
        </MyMusicXDetalhesContext.Provider>
    );
};
