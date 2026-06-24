import { axiosHttpApiLocalMyAnimes } from "../../api_conect/conectApiLocal";
import { useEffect, useState } from "react";
import MyAnimesBDLocalContext from "./MyAnimesBDLocalContext";

export default function MyAnimesBDLocalProvider({ children }) {
    const [iCollectionObjsMyAnimes, setiCollectionObjsMyAnimes] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    async function fetchAllObjsDBLocalMyAnimes() {
        setIsLoading(true);
        try {
            const response = await axiosHttpApiLocalMyAnimes().get('/apiLocal/myanime');
            // setiCollectionObjsMyAnimes(response.data);
            setiCollectionObjsMyAnimes(response);
            // console.log("fetchAllObjsDBLocalMyAnimes: ", response.data);
            // return response.data;
            return response;
        } catch (error) {
            console.error("Erro ao buscar objetos MyAnimes: ", error);
            throw error;
        } finally {
            setIsLoading(false);
        }
    };
    //Total de objetos, adicionar, atualizar, deletar...
    useEffect(() => {
        fetchAllObjsDBLocalMyAnimes();
    }, []);
    //===============================================================
    return (
        <MyAnimesBDLocalContext.Provider
            value={{
                iCollectionObjsMyAnimes,
                isLoading,
            }}
        >
            {children}
        </MyAnimesBDLocalContext.Provider>
    );
};
