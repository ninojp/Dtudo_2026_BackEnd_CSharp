import { use, useEffect, useRef } from 'react';
import AuthContext from '../../context_api/AuthContext/AuthContext';
import Spinner from '../../components/Spinner/Spinner';

export default function Logout() {
    const { error, isLoggingOut, logout } = use(AuthContext);
    const logoutStarted = useRef(false);

    useEffect(() => {
        if (logoutStarted.current) {
            return;
        }

        logoutStarted.current = true;
        logout('/').catch(() => undefined);
    }, [logout]);

    if (isLoggingOut) {
        return <Spinner />;
    }

    if (error) {
        return (
            <div role="alert">
                <p>{error}</p>
                <button type="button" onClick={() => logout('/')}>
                    Tentar novamente
                </button>
            </div>
        );
    }

    return <Spinner />;
};
