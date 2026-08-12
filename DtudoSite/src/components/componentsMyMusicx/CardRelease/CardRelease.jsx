import styles from './CardRelease.module.css';
import notaFireMusical from '/mymusicx/NotaMusica.png';

export default function CardRelease({ cdTitulo, cdImgSrc, cdAno, onClick }) {
    const thumb = cdImgSrc || notaFireMusical;
    const isInteractive = typeof onClick === 'function';

    const handleKeyDown = (event) => {
        if (isInteractive && (event.key === 'Enter' || event.key === ' ')) {
            event.preventDefault();
            onClick();
        }
    };

    return (
        <article
            className={styles.animesCardArticle}
            onClick={onClick}
            onKeyDown={handleKeyDown}
            role={isInteractive ? 'button' : undefined}
            tabIndex={isInteractive ? 0 : undefined}
        >
            <div className={styles.divContainerTitulo}>
                <h3 className={styles.h3Titulo}>{cdTitulo}</h3>
            </div>
            <figure className={styles.figureImagemAnimacao} title="Clique para abrir uma nova aba com mais informações">
                <img className={styles.imgAnimacao}
                    src={thumb}
                    alt={cdTitulo}
                    onError={(event) => {
                        if (!event.currentTarget.src.endsWith('/mymusicx/NotaMusica.png')) {
                            event.currentTarget.src = notaFireMusical;
                        }
                    }}
                />
            </figure>
            <div className={styles.divContainerData}>
                <span className={styles.pTextoData}>{cdAno}</span>
            </div>
        </article>
    );
};
