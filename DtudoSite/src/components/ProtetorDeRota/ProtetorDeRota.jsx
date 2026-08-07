import { use, useEffect } from "react";
import AuthContext from "../../context_api/AuthContext/AuthContext";
import Spinner from "../Spinner/Spinner";
import { useLocation, useNavigate } from "react-router-dom";

export default function ProtetorDeRota({ children }) {
    const { isAuthenticated, isLoading } = use(AuthContext);
    const navegarPara = useNavigate();
    const location = useLocation();
    //------------------------------------------------------
    useEffect(() => {
        if(!isLoading && !isAuthenticated){
            navegarPara('/auth/login', {
                replace: true,
                state: {
                    returnUrl: `${location.pathname}${location.search}`,
                },
            });
        }
    }, [isAuthenticated, isLoading, location.pathname, location.search, navegarPara]);
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
