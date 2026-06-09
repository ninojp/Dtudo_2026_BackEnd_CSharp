import LogoHardware from '../../components/componentsNinoTI/areasTI/logoHardware';
import styles from './NinoTIHardware.module.css';

export default function NinoTIHardware() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>Hardware</h1>
            <LogoHardware largura={'300px'} altura={'300px'}/>
            <h2>Breve descrição sobre a area...</h2>
            <p>Mais detalhes sobre a area...</p>
            <div className={styles.divContainerAreasTI}>
                ICONES da area...
            </div>
        </main>
    );
};
