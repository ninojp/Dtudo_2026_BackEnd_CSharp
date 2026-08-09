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
        <main className={styles.loginPage}>
            <section className={styles.brandPanel} aria-label="DtudoSite">
                <div className={styles.brandBar}>
                    <span className={styles.brandMark} aria-hidden="true">D</span>
                    <span className={styles.brandName}>DtudoSite</span>
                </div>
                <div className={styles.brandCopy}>
                    <p className={styles.eyebrow}>CATALOGO DTUDO</p>
                    <h1>Seu catalogo. Sua proxima descoberta.</h1>
                    <p className={styles.brandLead}>
                        Continue para explorar animes, colecoes e detalhes reunidos no Dtudo.
                    </p>
                </div>
                <div className={styles.brandFooter}>
                    <span className={styles.statusDot} aria-hidden="true" />
                    <span>Experiencia DtudoSite</span>
                </div>
            </section>
            <section className={styles.authPanel} aria-labelledby="login-title">
                <div className={styles.panelHeading}>
                    <p className={styles.panelKicker}>ACESSO SEGURO</p>
                    <h2 id="login-title">Entrar no DtudoSite</h2>
                    <p className={styles.panelSubtitle}>
                        Use sua conta para continuar.
                    </p>
                </div>
                {(callbackError || authError) && (
                    <p className={styles.alert} role="alert">
                        <span className={styles.alertIcon} aria-hidden="true">!</span>
                        <span>{callbackError || authError}</span>
                    </p>
                )}
                <button
                    className={styles.submitButton}
                    type="button"
                    onClick={iniciarLogin}
                    disabled={isLoading || isRedirecting}
                >
                    <span>{isRedirecting ? 'Redirecionando...' : 'Entrar'}</span>
                    <span className={styles.buttonArrow} aria-hidden="true">&rarr;</span>
                </button>
                <p className={styles.securityNote}>
                    <span aria-hidden="true">&#9679;</span> As contas sao criadas pelo procedimento administrativo.
                </p>
            </section>
        </main>
    );
};
