import LogoOS from '../../components/componentsNinoTI/areasTI/LogoOS';
import styles from './NinoTIOS.module.css';

export default function NinoTIOS() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>Sistemas Operacionais</h1>
            <LogoOS largura={'300px'} altura={'300px'}/>
            <h2>Breve descrição sobre a area...</h2>
            <p>Mais detalhes sobre a area...</p>
            <div className={styles.divContainerAreasTI}>
                ICONES da area...
            </div>
        </main>
    );
};
