import { useEffect, useState } from "react";
import AnimexObjsListContext from "./AnimexObjsListContext";
import axiosHttpRequest from "../../api_conect/conectApiLocal";

export default function AnimexObjsListProvider({ children }) {
    const [listObjsAnimex, setListObjsAnimex] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    async function fetchAllObjsAnimex() {
        setIsLoading(true);
        try {
            const response = await axiosHttpRequest.get('/animex');
            setListObjsAnimex(response.data);
            return response.data;
        } catch (error) {
            console.error("Erro ao buscar objetos Animex: ", error);
            throw error;
        } finally {
            setIsLoading(false);
        }
        }
    //Total de objetos, adicionar, atualizar, deletar...
    useEffect(() => {
        fetchAllObjsAnimex();
    }, []);
    return (
        <AnimexObjsListContext
            value={{ 
                listObjsAnimex,
                isLoading,
            }}
        >
            {children}
        </AnimexObjsListContext>
    );
};

//Para usar o contexto no React 19+, no lugar do useContext() usamos o novo hoook use(),
// import { use } from "react";
// const { listObjsAnimex, addObjAnimex, updateObjAnimex, deleteObjAnimex } = await use(AnimexObjsListContext);
//o await é necessário pois o use() retorna uma Promise.
