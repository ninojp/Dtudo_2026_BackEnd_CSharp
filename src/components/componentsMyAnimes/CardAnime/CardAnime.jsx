import styles from './CardAnime.module.css';

export default function CardAnime({ anime }) {
    return (
        <article key={anime.mal_id} className={styles.animesCardArticle}>
            <div className={styles.divContainerTitulo}>
                <h3 className={styles.h3Titulo}>{anime.title || anime.nome}</h3>
            </div>
            <figure className={styles.figureImagemAnimacao}>
                <img className={styles.imgAnimacao}
                    src={anime.images.webp.image_url || anime.image} 
                    alt={anime.title || anime.nome}
                />
            </figure>
            <div className={styles.divContainerData}>
                {<span className={styles.spanTextoData}>{anime.aired.prop.from.year}</span>}
                <p className={styles.pTextoData}>{anime.genres ? anime.genres.map(g => g.name).join(', ') : 'N/A'}</p>
            </div>
        </article>
    );
};
