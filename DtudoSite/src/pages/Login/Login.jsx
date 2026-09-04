import { useLocation, useNavigate } from "react-router-dom"
import { use, useEffect, useRef } from 'react';
import AuthContext from '../../context_api/AuthContext/AuthContext';
import { getSafeReturnPath } from '../../services/bffClient';

export default function Login() {
    const { isAuthenticated, isLoading, login } = use(AuthContext);
    const navigate = useNavigate();
    const location = useLocation();
    const loginStarted = useRef(false);
    const returnUrl = getSafeReturnPath(location.state?.returnUrl || '/animes');

    useEffect(() => {
        if (isLoading) {
            return;
        }

        if (isAuthenticated) {
            navigate(returnUrl, { replace: true });
            return;
        }

        if (!loginStarted.current) {
            loginStarted.current = true;
            login(returnUrl);
        }
    }, [isAuthenticated, isLoading, login, navigate, returnUrl]);

    return null;
};
