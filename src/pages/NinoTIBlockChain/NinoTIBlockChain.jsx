import LogoBlockchain from '../../components/componentsNinoTI/areasTI/LogoBlockchain';
import styles from './NinoTIBlockChain.module.css';
import btcImagem from '/ninoti/bitcoin-fundo-digital_1366.webp';

export default function NinoTIBlockChain() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>BlockChain</h1>
        <img src={btcImagem} alt='imgam bitcoin com fundo digital' style={{width: '600px'}} />
            <h2>Breve descrição sobre a area...</h2>
            <p>Mais detalhes sobre a area...</p>
            <div className={styles.divContainerAreasTI}>
                ICONES da area...
            </div>
        </main>
    );
};
