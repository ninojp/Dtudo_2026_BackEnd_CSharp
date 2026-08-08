import { Link } from 'react-router-dom';
import styles from '../NavBarPage/NavBarPage.module.css';

export default function CatalogNavBarPage() {
    return (
        <nav className={styles.navBarPageContainer}>
            <div className={styles.divContainerLogoTituloMenu}>
                <Link to="/">
                    <div className={styles.divContainerImgLogo}>
                        <img src="/Logo_Dtudo_300p.png" alt="Imagem Logo Dtudo" />
                    </div>
                </Link>
                <ul className={styles.ulMenuLinksContanier}>
                    <Link to="/animes">
                        <li className={styles.liMenuLink}>Animes</li>
                    </Link>
                </ul>
            </div>
        </nav>
    );
}
