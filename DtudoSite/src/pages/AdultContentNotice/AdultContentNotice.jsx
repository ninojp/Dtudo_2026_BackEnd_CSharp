import { use, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import AuthContext from '../../context_api/AuthContext/AuthContext';
import Spinner from '../../components/Spinner/Spinner';
import styles from './AdultContentNotice.module.css';

const AGE_CONFIRMATION_KEY = 'dtudo:age-confirmed';

function readAgeConfirmation() {
    try {
        return window.sessionStorage.getItem(AGE_CONFIRMATION_KEY) === 'true';
    } catch {
        return false;
    }
}

function saveAgeConfirmation() {
    try {
        window.sessionStorage.setItem(AGE_CONFIRMATION_KEY, 'true');
    } catch {
        return false;
    }

    return true;
}

export default function AdultContentNotice() {
    const { isAuthenticated, isLoading } = use(AuthContext);
    const navigate = useNavigate();
    const [ageConfirmed, setAgeConfirmed] = useState(readAgeConfirmation);
    const [isRedirecting, setIsRedirecting] = useState(false);

    useEffect(() => {
        if (isLoading || !ageConfirmed) return;

        navigate(isAuthenticated ? '/animes' : '/auth/login', {
            replace: true,
            state: isAuthenticated ? undefined : {
                returnUrl: '/animes',
            },
        });
    }, [ageConfirmed, isAuthenticated, isLoading, navigate]);

    function confirmarMaioridade() {
        saveAgeConfirmation();
        setIsRedirecting(true);
        setAgeConfirmed(true);
    }

    if (isLoading || ageConfirmed || isRedirecting) {
        return <Spinner />;
    }

    return (
        <main className={styles.mainNotice} aria-labelledby="adult-content-title">
            <section className={styles.sectionNotice}>
                <p className={styles.eyebrow}>Dtudo</p>
                <h1 id="adult-content-title">Conteúdo adulto (Hentai)</h1>
                <p className={styles.noticeText}>
                    Esta página contém conteúdo destinado exclusivamente a maiores de 18 anos.
                </p>
                <p className={styles.noticeText}>
                    O acesso é permitido somente para pessoas maiores de 18 anos com uma conta previamente criada.
                </p>
                <p className={styles.confirmationText}>
                    Você confirma ser maior de 18 anos e possuir uma conta?
                </p>
                <button
                    type="button"
                    className={styles.btnAccess}
                    onClick={confirmarMaioridade}
                    disabled={isRedirecting}
                >
                    Acessar o site
                </button>
                <p className={styles.sessionNote}>
                    Ao continuar, você será direcionado para a autenticação obrigatória.
                </p>
            </section>
        </main>
    );
}
