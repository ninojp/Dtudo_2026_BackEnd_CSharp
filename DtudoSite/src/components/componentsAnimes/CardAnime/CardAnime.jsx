import styles from './CardAnime.module.css';
import { FaCalendarAlt, FaStar } from 'react-icons/fa';
import {
    obterAnoAnime,
    obterGenerosAnime,
    obterImagemAnime,
    obterIconeTipoAnime,
    obterScoreAnime,
    obterTipoAnime,
    obterTituloAlternativoAnime,
    obterTituloAnime
} from '@dtudo-anime-content';

export default function CardAnime({ anime }) {
    const titulo = obterTituloAnime(anime);
    const tituloAlternativo = obterTituloAlternativoAnime(anime);
    const imagem = obterImagemAnime(anime);
    const ano = obterAnoAnime(anime);
    const tipo = obterTipoAnime(anime);
    const score = obterScoreAnime(anime);
    const generos = obterGenerosAnime(anime);
    const itensResumo = [
        ano && { label: 'Ano', value: ano, variant: styles.spanIconeAno, icon: <FaCalendarAlt aria-hidden="true" /> },
        tipo && { label: 'Tipo', value: tipo, variant: styles.spanIconeTipo, icon: obterIconeTipoAnime(anime) },
        score && { label: 'Nota media', value: score, variant: styles.spanIconeScore, icon: <FaStar aria-hidden="true" /> },
    ].filter(Boolean);

    return (
        <article className={styles.animesCardArticle}>
            <div className={styles.divContainerTitulo}>
                <h3 className={styles.h3Titulo}>{titulo}</h3>
            </div>
            <figure className={styles.figureImagemAnimacao}>
                {imagem ? (
                    <img className={styles.imgAnimacao} src={imagem} alt={titulo} />
                ) : (
                    <div className={styles.imagemIndisponivel}>Imagem indisponivel</div>
                )}
            </figure>
            <div className={styles.divContainerInfo}>
                {tituloAlternativo && (
                    <p className={styles.pSubTitulo} title={tituloAlternativo}>{tituloAlternativo}</p>
                )}
                {itensResumo.length > 0 && (
                    <dl className={styles.dlResumo}>
                        {itensResumo.map((item) => (
                            <div className={styles.divResumoItem} key={item.label} title={`${item.label}: ${item.value}`}>
                                <dt>{item.label}</dt>
                                <dd>
                                    <span className={`${styles.spanIconeResumo} ${item.variant}`}>{item.icon}</span>
                                    <span>{item.value}</span>
                                </dd>
                            </div>
                        ))}
                    </dl>
                )}
                {generos.length > 0 && (
                    <p className={styles.pTextoGeneros} title={generos.join(', ')}>{generos.join(', ')}</p>
                )}
            </div>
        </article>
    );
};
