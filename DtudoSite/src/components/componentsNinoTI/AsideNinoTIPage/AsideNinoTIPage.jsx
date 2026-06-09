import { Link } from 'react-router-dom';
import LogoFronEnd from '../areasTI/LogoFronEnd';
import styles from './AsideNinoTIPage.module.css';
import LogoProgramacao from '../areasTI/LogoProgramacao';
import LogoCyberSecurity from '../areasTI/LogoCyberSecurity';
import LogoDesignUx from '../areasTI/LogoDesignUx';
import LogoDevops from '../areasTI/LogoDevops';
import LogoBlockchain from '../areasTI/LogoBlockchain';
import LogoIA from '../areasTI/LogoIA';
import LogoDataScience from '../areasTI/LogoDataScience';
// import LogoRede from '../areasTI/LogoRede';
export default function AsideNinoTIPage() {
    return (
        <aside className={styles.asideMenuAreasTI}>
            <p className="tituloDescricao">Estudando TI há 28 anos<br /> Trabalhando e vivendo dela há 17</p>
            <ul className={styles.ulMenuAreasTI}>
                <Link className={styles.linkMenuAreasTI} to={'/ninoti/front-end'} >
                    <li className='styleFrontEnd' id="2">
                        Front-End
                        <LogoFronEnd largura={'30px'} altura={'30px'} />
                    </li>
                </Link>
                <Link className={styles.linkMenuAreasTI} to={'/ninoti/programacao'} >
                    <li className='styleProgramacao' id="1">
                        Programação
                        <LogoProgramacao largura={'30px'} altura={'30px'} />
                    </li>
                </Link>
                <Link className={styles.linkMenuAreasTI} to={'/ninoti/ia'} >
                    <li className='styleIA' id="10">
                        Inteligência Artficial
                        <LogoIA largura={'30px'} altura={'30px'} />
                    </li>
                </Link>
                <Link className={styles.linkMenuAreasTI} to={'/ninoti/cyber-security'} >
                    <li className='styleCyberSecurity' id="3">
                        Cyber Security
                        <LogoDevops largura={'30px'} altura={'30px'} />
                    </li>
                </Link>
                <Link className={styles.linkMenuAreasTI} to={'/ninoti/blockchain'} >
                    <li className='styleBlockchain' id="6">
                        Blockchain
                        <LogoBlockchain largura={'30px'} altura={'30px'} />
                    </li>
                </Link>
                <Link className={styles.linkMenuAreasTI} to={'/ninoti/ciencia-computacao'} >
                    <li className='styleRedes' id="8">
                        Ciência Computação
                        <LogoDataScience largura={'30px'} altura={'30px'} />
                    </li>
                </Link>
                <Link className={styles.linkMenuAreasTI} to={'/ninoti/design-ux'} >
                    <li className='styleDesignEUX' id="4">
                        Design e UX
                        <LogoDesignUx largura={'30px'} altura={'30px'} />
                    </li>
                </Link>
            </ul>
        </aside>
    );
};
