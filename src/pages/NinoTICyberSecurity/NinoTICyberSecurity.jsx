import LogoDevops from '../../components/componentsNinoTI/areasTI/LogoDevops';
import styles from './NinoTICyberSecurity.module.css';

export default function NinoTICyberSecurity() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>Cyber Security</h1>
            <LogoDevops largura={'300px'} altura={'300px'}/>
            <h2>Breve descrição sobre a area...</h2>
            <p>Mais detalhes sobre a area...</p>
            <div className={styles.divContainerAreasTI}>
                ICONES da area...
            </div>
        </main>
    );
};
