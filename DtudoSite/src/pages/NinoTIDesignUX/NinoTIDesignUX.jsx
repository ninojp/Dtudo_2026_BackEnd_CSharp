import LogoDesignUx from '../../components/componentsNinoTI/areasTI/LogoDesignUx';
import styles from './NinoTIDesignUX.module.css';

export default function NinoTIDesignUX() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>Design e UX</h1>
            <LogoDesignUx largura={'300px'} altura={'300px'}/>
            <h2>Breve descrição sobre a area...</h2>
            <p>Mais detalhes sobre a area...</p>
            <div className={styles.divContainerAreasTI}>
                ICONES da area...
            </div>
        </main>
    );
};
