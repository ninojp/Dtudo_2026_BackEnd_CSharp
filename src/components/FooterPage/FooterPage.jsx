import { useEffect, useState } from "react";
import styles from './FooterPage.module.css';
import reactLogo from '/react.svg';
import viteLogo from '/vite.svg';
import JSLogo from '/JavaScript_300h.webp';
import HTMLogo from '/HTML_300h.webp';
import GitHubLogo from '/GitHub_300h.png';
import CSSLogo from '/CSS_300h.webp';
import NodeLogo from '/Node_300h.webp';
//-----------------------------------------
const dataAtualFormatada = () => {
    const now = new Date();
    return {
        date: now.toLocaleDateString("pt-BR"),
        weekday: now.toLocaleDateString("pt-BR", { weekday: "long" }),
        time: now.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" }),
    };
};
//=================================================================
export default function FooterPage({ children }) {
    const [dataAtual, setDataAtual] = useState(dataAtualFormatada());
    useEffect(() => {
        const timer = setInterval(() => setDataAtual(dataAtualFormatada()), 60_000); // atualiza a cada minuto
        return () => clearInterval(timer);// limpa o timer ao desmontar o componente
    }, []);

    return (
        <footer className={styles.footerContainer}>
            <div>
                <p>Criado em 13/11/2025 - by NinoJP</p>
            </div>
            <div className={styles.divLogosTecContainer}>
                <a href="https://developer.mozilla.org/pt-BR/docs/Web/HTML" target="_blank" rel="external noopener noreferrer nofollow">
                    <img src={HTMLogo} className={styles.logoHTM} alt="HTML5 logo" />
                </a>
                <a href="https://www.w3schools.com/css/" target="_blank" rel="external noopener noreferrer nofollow">
                    <img src={CSSLogo} className={styles.logoCSS} alt="CSS3 logo" />
                </a>
                <a href="https://ecma-international.org/publications-and-standards/standards/ecma-262/" target="_blank" rel="external noopener noreferrer nofollow">
                    <img src={JSLogo} className={styles.logoJS} alt="JavaScript logo" />
                </a>
                <a href="https://nodejs.org/pt" target="_blank" rel="external noopener noreferrer nofollow">
                    <img src={NodeLogo} className={styles.logoNode} alt="NodeJS logo" />
                </a>
                <a href="https://react.dev" target="_blank" rel="external noopener noreferrer nofollow">
                    <img src={reactLogo} className={styles.logoReact} alt="React logo" />
                </a>
                <a href="https://vite.dev" target="_blank" rel="external noopener noreferrer nofollow">
                    <img src={viteLogo} className={styles.logoVite} alt="Vite logo" />
                </a>
                <a href="https://github.com/ninojp/dtudo2026" target="_blank" rel="external noopener noreferrer nofollow">
                    <img src={GitHubLogo} className={styles.logoGitHub} alt="GitHub logo" />
                </a>
            </div>
            <div>
                {`${dataAtual.date} - ${dataAtual.weekday} às ${dataAtual.time}`}
                {children && <div>{children}</div>}
            </div>
        </footer>
    );
};
/**
 * Vamos ver as opções em detalhe:

rel="noreferrer": Esta é a diretiva principal para a sua necessidade. Ela instrui o navegador a não enviar o cabeçalho HTTP Referer para a página de destino. Isso significa que o site externo não saberá que o usuário veio do seu site.
rel="noopener": Esta é uma medida de segurança crucial ao abrir links em uma nova aba (target="_blank"). Ela impede que a nova página tenha acesso ao objeto window.opener da sua página, protegendo contra ataques de phishing conhecidos como "tabnabbing", onde a página de destino poderia, maliciosamente, alterar o conteúdo da sua página original.
rel="nofollow": Esta opção é mais voltada para SEO (Search Engine Optimization). Ela informa aos mecanismos de busca para não "seguirem" o link, ou seja, para não transferir "autoridade" da sua página para a página de destino. Embora não esteja diretamente relacionada à privacidade do clique, é comum usá-la para links externos em que você não tem controle ou não endossa completamente o conteúdo.
 */
