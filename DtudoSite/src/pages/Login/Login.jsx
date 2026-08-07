import styles from './login.module.css'
import { useLocation, useNavigate } from "react-router-dom"
import { use, useEffect, useState } from 'react';
import AuthContext from '../../context_api/AuthContext/AuthContext';
import { getSafeReturnPath } from '../../services/bffClient';

export default function Login() {
    const { error: authError, isAuthenticated, isLoading, login } = use(AuthContext);
    const navigate = useNavigate();
    const location = useLocation();
    const [isRedirecting, setIsRedirecting] = useState(false);
    const returnUrl = getSafeReturnPath(location.state?.returnUrl || '/animes');
    const callbackError = new URLSearchParams(location.search).has('error')
        ? 'Nao foi possivel concluir o login.'
        : null;

    useEffect(() => {
        if (!isLoading && isAuthenticated) {
            navigate(returnUrl, { replace: true });
        }
    }, [isAuthenticated, isLoading, navigate, returnUrl]);

    const iniciarLogin = () => {
        setIsRedirecting(true);
        login(returnUrl);
    };

    return (
        <div className={styles.divContainerLogin}>
            <h3 className={styles.h3RegisterUser}>Login</h3>
            <h4 className={styles.h4RegisterUser}>Boas-vindas! Entre pela sessao segura.</h4>
            {(callbackError || authError) && (
                <p role="alert">{callbackError || authError}</p>
            )}
            <button
                className={styles.btnRegister}
                type="button"
                onClick={iniciarLogin}
                disabled={isLoading || isRedirecting}
            >
                {isRedirecting ? 'Redirecionando...' : 'Entrar'}
            </button>
            <div className={styles.divRodape}>
                <p>As contas sao criadas pelo procedimento administrativo.</p>
            </div>
        </div>
    );
};
