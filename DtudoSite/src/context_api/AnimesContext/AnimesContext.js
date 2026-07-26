import { createContext } from "react";

const AnimesContext = createContext({
    listObjsDetalhesAnimes: [],
    isLoading: true,
    error: null,
    recarregarAnimes: () => { },
});

export default AnimesContext;
