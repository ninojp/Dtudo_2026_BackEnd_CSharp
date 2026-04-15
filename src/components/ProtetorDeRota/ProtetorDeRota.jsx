import { use, useEffect } from "react";
import AuthContext from "../../context_api/AuthContext/AuthContext";
import Spinner from "../Spinner/Spinner";
import { useNavigate } from "react-router-dom";

export default function ProtetorDeRota({ children }) {
    const { isAuthenticated, isLoading } = use(AuthContext);
    const navegarPara = useNavigate();
    //------------------------------------------------------
    useEffect(() => {
        if(!isLoading && !isAuthenticated){
            navegarPara('/auth/login');
        }
    }, [isAuthenticated, isLoading, navegarPara]);
    //------------------------------------------------------
    if (isLoading) {
        return <Spinner />;
    };
    if (!isAuthenticated) {
        return null;
    };
    //==============
    return children
};
