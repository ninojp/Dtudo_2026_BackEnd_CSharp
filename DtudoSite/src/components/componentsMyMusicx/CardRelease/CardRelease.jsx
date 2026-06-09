import styles from './CardRelease.module.css';
import notaFireMusical from '/mymusicx/NotaMusica.png';

export default function CardRelease({ cdTitulo, cdImgSrc, cdAno }) {
    const thumb = cdImgSrc || notaFireMusical;
    return (
        <article className={styles.animesCardArticle}>
            <div className={styles.divContainerTitulo}>
                <h3 className={styles.h3Titulo}>{cdTitulo}</h3>
            </div>
            <figure className={styles.figureImagemAnimacao} title="Clique para abrir uma nova aba com mais informações">
                <img className={styles.imgAnimacao}
                    src={thumb}
                    alt={cdTitulo}
                />
            </figure>
            <div className={styles.divContainerData}>
                <span className={styles.pTextoData}>{cdAno}</span>
            </div>
        </article>
    );
};
