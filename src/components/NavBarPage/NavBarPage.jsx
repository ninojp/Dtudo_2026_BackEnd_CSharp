import styles from './NavBarPage.module.css';
import { Link } from 'react-router-dom';
import { IconAccount } from '../Icons/IconAccount';
import { IconLogout } from '../Icons/IconLogout';
import IconRegister from '../Icons/IconRegister';
import AuthContext from '../../context_api/AuthContext/AuthContext';
import { IconLogin } from "../Icons/IconLogin";
import { useContext } from 'react';

export default function NavBarPage() {
    const { isAuthenticated } = useContext(AuthContext);
    return (
        <nav className={styles.navBarPageContainer}>
            <div className={styles.divContainerLogoTituloMenu}>
                <Link to="/">
                    <div className={styles.divContainerImgLogo}>
                        <img src="/Logo_Dtudo_300p.png" alt="Imagem Logo Dtudo" />
                    </div>
                </Link>
                <ul className={styles.ulMenuLinksContanier}>
                    <Link to='myanimes'>
                        <li className={styles.liMenuLink}> MyAnimes </li>
                    </Link>
                    <Link to='animes'>
                        <li className={styles.liMenuLink}> Animes </li>
                    </Link>
                    {isAuthenticated && <Link to='animex'>
                        <li className={styles.liMenuLink}> Animex </li>
                    </Link>}
                    <Link to='ninoti'>
                        <li className={styles.liMenuLink}> NinoT.I </li>
                    </Link>
                    <Link to='mymusicx'>
                        <li className={styles.liMenuLink}> MyMusicX </li>
                    </Link>
                </ul>
            </div>
            <div className={styles.divContainerIconsLogin}>
                {!isAuthenticated && <Link to='/auth/register' title='Criar Conta'><IconRegister cor={'#ffffffc0'} largura={'24px'} altura={'24px'} /></Link>}
                {!isAuthenticated && <Link to='/auth/login' title='Fazer Login'><IconLogin cor={'#ffffffc0'} largura={'24px'} altura={'24px'} /></Link>}
                {isAuthenticated && <Link to='#' title='Perfil Usuário'><IconAccount cor={'#ffffffc0'} largura={'24px'} altura={'24px'} /></Link>}
                {isAuthenticated && <Link to='/auth/logout' title='Fazer Logout'><IconLogout cor={'#ffffffc0'} largura={'24px'} altura={'24px'} /></Link>}
            </div>
        </nav>
    )
};
