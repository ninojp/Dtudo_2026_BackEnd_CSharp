import LogoProgramacao from '../../components/componentsNinoTI/areasTI/LogoProgramacao';
import styles from './NinoTIProgramacao.module.css';

export default function NinoTIProgramacao() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>Programação</h1>
            <LogoProgramacao largura={'300px'} altura={'300px'}/>
            <h2>Breve descrição sobre a area...</h2>
            <p>Mais detalhes sobre a area...</p>
            <div className={styles.divContainerAreasTI}>
                ICONES da area...
            </div>
        </main>
    );
};
