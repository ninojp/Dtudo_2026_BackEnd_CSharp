import LogoDataScience from '../../components/componentsNinoTI/areasTI/LogoDataScience';
import LogoRede from '../../components/componentsNinoTI/areasTI/LogoRede';
import styles from './NinoTIDataScience.module.css';

export default function NinoTIDataScience() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>Data Science</h1>
            <LogoDataScience largura={'300px'} altura={'300px'}/>
            <h2>Breve descrição sobre a area...</h2>
            <p>Mais detalhes sobre a area...</p>
            <div className={styles.divContainerAreasTI}>
                ICONES da area...
            </div>
        </main>
    );
};
