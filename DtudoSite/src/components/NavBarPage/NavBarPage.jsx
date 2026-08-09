import styles from './NavBarPage.module.css';
import { Link } from 'react-router-dom';
import { IconAccount } from '../Icons/IconAccount';
import { IconLogout } from '../Icons/IconLogout';
import AuthContext from '../../context_api/AuthContext/AuthContext';
import { IconLogin } from "../Icons/IconLogin";
import { useContext, useEffect, useRef, useState } from 'react';

export default function NavBarPage() {
    const { isAuthenticated, user } = useContext(AuthContext);
    const userName = user?.name?.trim() || user?.email?.trim() || 'Usuario';
    const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
    const userMenuRef = useRef(null);

    useEffect(() => {
        if (!isUserMenuOpen) {
            return undefined;
        }

        const closeMenuWhenClickingOutside = (event) => {
            if (!userMenuRef.current?.contains(event.target)) {
                setIsUserMenuOpen(false);
            }
        };

        const closeMenuWithEscape = (event) => {
            if (event.key === 'Escape') {
                setIsUserMenuOpen(false);
            }
        };

        document.addEventListener('pointerdown', closeMenuWhenClickingOutside);
        document.addEventListener('keydown', closeMenuWithEscape);

        return () => {
            document.removeEventListener('pointerdown', closeMenuWhenClickingOutside);
            document.removeEventListener('keydown', closeMenuWithEscape);
        };
    }, [isUserMenuOpen]);

    return (
        <nav className={styles.navBarPageContainer}>
            <div className={styles.divContainerLogoTituloMenu}>
                <Link to="/">
                    <div className={styles.divContainerImgLogo}>
                        <img src="/Logo_Dtudo_300p.png" alt="Imagem Logo Dtudo" />
                    </div>
                </Link>
                <ul className={styles.ulMenuLinksContanier}>
                    <Link to='animes'>
                        <li className={styles.liMenuLink}> Animes </li>
                    </Link>
                    <Link to='ninoti'>
                        <li className={styles.liMenuLink}> NinoT.I </li>
                    </Link>
                    <Link to='mymusicx'>
                        <li className={styles.liMenuLink}> MyMusicX </li>
                    </Link>
                </ul>
            </div>
            <div className={styles.divContainerIconsLogin}>
                {!isAuthenticated && <Link to='/auth/login' title='Fazer Login'><IconLogin cor={'#ffffffc0'} largura={'24px'} altura={'24px'} /></Link>}
                {isAuthenticated && (
                    <div ref={userMenuRef} className={styles.authenticatedUser}>
                        <button
                            type='button'
                            className={styles.accountButton}
                            title={userName}
                            aria-label={`Usuário conectado: ${userName}`}
                            aria-haspopup='menu'
                            aria-expanded={isUserMenuOpen}
                            onClick={() => setIsUserMenuOpen((isOpen) => !isOpen)}
                        >
                            <IconAccount cor={'#ffffffc0'} largura={'24px'} altura={'24px'} />
                        </button>
                        {isUserMenuOpen && (
                            <div className={styles.userMenu} role='menu'>
                                <Link to='/auth/logout' role='menuitem' onClick={() => setIsUserMenuOpen(false)}>
                                    <IconLogout cor={'#ffffffc0'} largura={'18px'} altura={'18px'} />
                                    <span>Sair</span>
                                </Link>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </nav>
    )
};
