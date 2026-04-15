import { use } from 'react';
import styles from './CardAnimexMini.module.css';
import AnimexDetalhesContext from '../../../context_api/AnimexDetalhesContext/AnimexDetalhesContext';

export default function CardAnimexMini() {
    const { animexDetalhes, currentDisplayId, setCurrentDisplayId } = use(AnimexDetalhesContext);

    return (
        <div className={styles.divContainerBtnsCarrossel}>
            {animexDetalhes.subpastas.map((subpasta) => (
                <button
                    key={subpasta.id}
                    className={`${styles.btnCarrosselItem} ${currentDisplayId === subpasta.id ? styles.active : ''}`}
                    onClick={() => setCurrentDisplayId(subpasta.id)}
                >
                    <span className={styles.spanTextCarrossel}>{subpasta.ano}</span>
                    <img className={styles.imgMiniAnimeBtnCarrossel} src={`/animex/animes/${subpasta.id}.jpg`} />
                    <span className={styles.spanTextCarrossel}>{subpasta.nomeSemAno}</span>
                </button>
            ))}
        </div>
    )
}
