import { useEffect, useState } from "react";
import axiosHttpRequest from "../../api_conect/conectApiLocal";
import MyAnimesObjsListContext from "./MyAnimesObjsListContext";

export default function MyAnimesObjsListProvider({ children }) {
    const [listObjsMyAnimes, setListObjsMyAnimes] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    async function fetchAllObjsMyAnimes() {
        setIsLoading(true);
        try {
            const response = await axiosHttpRequest.get('/myanimes');
            setListObjsMyAnimes(response.data);
            return response.data;
        } catch (error) {
            console.error("Erro ao buscar objetos MyAnimes: ", error);
            throw error;
        } finally {
            setIsLoading(false);
        }
        }
    //Total de objetos, adicionar, atualizar, deletar...
    useEffect(() => {
        fetchAllObjsMyAnimes();
    }, []);
    return (
        <MyAnimesObjsListContext.Provider
            value={{ 
                listObjsMyAnimes,
                isLoading,
            }}
        >
            {children}
        </MyAnimesObjsListContext.Provider>
    );
};
