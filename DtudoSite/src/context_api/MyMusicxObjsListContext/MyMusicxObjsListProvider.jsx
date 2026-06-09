import { useEffect, useState } from "react"
import axiosHttpRequest from "../../api_conect/conectApiLocal";
import MyMusicxObjsListContext from "./MyMusicxObjsListContext";

export default function MyMusicxObjsListProvider({ children }) {
    const [listObjsMyMusicx, setListObjsMyMusicx] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    async function fetchAllObjsMyMusicx() {
        setIsLoading(true);
        try {
            const response = await axiosHttpRequest.get('/mymusicx');
            //console.log(response.data);
            setListObjsMyMusicx(response.data);
            return response.data;
        } catch (error) {
            console.error("Erro ao buscar objetos MyMusicx: ", error);
            throw error;
        } finally {
            setIsLoading(false);
        }
    }
    //Total de objetos, adicionar, atualizar, deletar...
    useEffect(() => {
        fetchAllObjsMyMusicx();
    }, []);
    //===========================================================================
    return (
        <MyMusicxObjsListContext.Provider value={
            {
                listObjsMyMusicx,
                isLoading,
                fetchAllObjsMyMusicx
            }}>
            {children}
        </MyMusicxObjsListContext.Provider>
    )
}
