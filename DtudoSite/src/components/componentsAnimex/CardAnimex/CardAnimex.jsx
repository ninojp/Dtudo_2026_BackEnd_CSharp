import { Link } from 'react-router-dom';
import styles from './CardAnimex.module.css';

const CardAnimex = ({ item, onImageClick }) => {
    return (
        <article key={String(item.id)} className={styles.animesCardArticle}>
            <Link to={`/animex/animex-detalhes/${item.slug}`} target='_blank' className={styles.cardLink} title="Clique para ver os detalhes do anime">
                <div className={styles.divContainerTitulo}>
                    <h3 className={styles.h3Titulo}>{item.nome}</h3>
                </div>
                <figure className={styles.figureImagemAnimacao} onClick={(e) => { e.preventDefault(); onImageClick(item); }} title="Clique para abrir o modal com mais informações">
                    <img className={styles.imgAnimacao}
                        src={item.imgSrc ? `/animex/${encodeURIComponent(String(item.imgSrc))}` : '/lupa.png'}
                        alt={item.nome}
                    />
                </figure>
                <div className={styles.divContainerData}>
                    {item.subpastas.map(subItem => <span className={styles.pTextoData} key={subItem.id}>{subItem.ano}</span>)}
                </div>
            </Link>
            <span className={styles.spanId}>ID: {item.id}</span>
        </article>
    );
};
export default CardAnimex;
