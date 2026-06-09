import { use } from 'react';
import styles from './CardMyAnimesMini.module.css';
import MyAnimesDetalhesContext from '../../../context_api/MyAnimesDetalhesContext/MyAnimesDetalhesContext';

export default function CardMyAnimesMini() {
    const { myAnimesDetalhes, currentDisplayId, setCurrentDisplayId } = use(MyAnimesDetalhesContext);
    return (
        <div className={styles.divContainerBtnsCarrossel}>
            {myAnimesDetalhes.subpastas.map((subpasta) => (
                <button
                    key={subpasta.mal_id}
                    className={`${styles.btnCarrosselItem} ${currentDisplayId === subpasta.mal_id ? styles.active : ''}`}
                    onClick={() => setCurrentDisplayId(subpasta.mal_id)}
                >
                    <span className={styles.spanTextCarrossel}>{subpasta.ano}</span>
                    <img className={styles.imgMiniAnimeBtnCarrossel} src={`/myanimes/animes/${subpasta.mal_id}.jpg`} />
                    <span className={styles.spanTextCarrossel}>{subpasta.nomeSemAno}</span>
                </button>
            ))}
        </div>
    )
}
