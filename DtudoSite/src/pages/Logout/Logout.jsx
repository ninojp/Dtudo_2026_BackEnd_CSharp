import { use, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import AuthContext from '../../context_api/AuthContext/AuthContext';
import Spinner from '../../components/Spinner/Spinner';

export default function Logout() {
    const { error, isLoading, logout } = use(AuthContext);
    const navigate = useNavigate();

    useEffect(() => {
        let ativo = true;

        logout('/auth/login').then((result) => {
            if (ativo && result.success) {
                navigate('/auth/login', { replace: true });
            }
        });

        return () => {
            ativo = false;
        };
    }, [logout, navigate]);

    if (isLoading) {
        return <Spinner />;
    }

    if (error) {
        return (
            <div role="alert">
                <p>{error}</p>
                <button type="button" onClick={() => window.location.reload()}>
                    Tentar novamente
                </button>
            </div>
        );
    }

    return null;
};
