import styles from './CardAnime.module.css';
import { obterAnoAnime, obterGenerosAnime, obterImagemAnime, obterTituloAnime } from '../../../utils/animeContentUtils';

export default function CardAnime({ anime }) {
    const titulo = obterTituloAnime(anime);
    const imagem = obterImagemAnime(anime);
    const ano = obterAnoAnime(anime);
    const generos = obterGenerosAnime(anime);

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
            <div className={styles.divContainerData}>
                <span className={styles.spanTextoData}>{ano || 'Ano nao informado'}</span>
                <p className={styles.pTextoData}>{generos.join(', ') || 'N/A'}</p>
            </div>
        </article>
    );
};
