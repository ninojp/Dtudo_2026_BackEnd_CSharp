import LogoIA from '../../../components/componentsNinoTI/areasTI/LogoIA';
import styles from './NinoTIIA.module.css';

export default function NinoTIIA() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>Inteligência Artificial</h1>
            <LogoIA largura={'300px'} altura={'300px'}/>
            <h2>Breve descrição sobre a area...</h2>
            <p>Mais detalhes sobre a area...</p>
            <div className={styles.divContainerAreasTI}>
                ICONES da area...
            </div>
        </main>
    );
};
