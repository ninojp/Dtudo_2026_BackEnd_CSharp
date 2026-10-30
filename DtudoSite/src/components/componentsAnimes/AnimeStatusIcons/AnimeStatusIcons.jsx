import styles from './AnimeStatusIcons.module.css';
import { FaEye, FaFolder, FaPlay, FaThumbsUp } from 'react-icons/fa';

const ESTADOS_VALIDOS = ['none', 'partial', 'complete'];

const ROTULOS_VISTO = {
    none: 'Visto: nenhum episódio',
    partial: 'Visto: parcialmente',
    complete: 'Visto: todos os episódios',
};

const ROTULOS_PASTA = {
    none: 'Pasta: não encontrada',
    partial: 'Pasta: encontrada, faltam episódios',
    complete: 'Pasta: encontrada, episódios completos',
};

const CLASSES_ESTADO = {
    none: styles.estadoNone,
    partial: styles.estadoPartial,
    complete: styles.estadoComplete,
};

function normalizarEstado(estado) {
    return ESTADOS_VALIDOS.includes(estado) ? estado : 'none';
}

function obterRotuloGostei(like) {
    if (like === true) return 'Gostei: sim';
    if (like === false) return 'Gostei: não';
    return 'Gostei: não avaliado';
}

export default function AnimeStatusIcons({ watchState = 'none', folderState = 'none', like = null, onPlay }) {
    const estadoVisto = normalizarEstado(watchState);
    const estadoPasta = normalizarEstado(folderState);
    const rotuloVisto = ROTULOS_VISTO[estadoVisto];
    const rotuloPasta = ROTULOS_PASTA[estadoPasta];
    const rotuloGostei = obterRotuloGostei(like);
    const rotuloPlay = typeof onPlay === 'function' ? 'Assistir' : 'Em breve';

    // O componente fica dentro de um <Link>: o clique nunca pode navegar.
    const criarManipulador = (acao) => (evento) => {
        evento.preventDefault();
        evento.stopPropagation();
        if (typeof acao === 'function') acao(evento);
    };

    return (
        <div className={styles.divStatusIcons} role="group" aria-label="Status do anime">
            <button
                type="button"
                className={`${styles.btnIcone} ${CLASSES_ESTADO[estadoVisto]}`}
                title={rotuloVisto}
                aria-label={rotuloVisto}
                onClick={criarManipulador()}
            >
                <FaEye aria-hidden="true" />
            </button>
            <button
                type="button"
                className={`${styles.btnIcone} ${CLASSES_ESTADO[estadoPasta]}`}
                title={rotuloPasta}
                aria-label={rotuloPasta}
                onClick={criarManipulador()}
            >
                <FaFolder aria-hidden="true" />
            </button>
            <button
                type="button"
                className={`${styles.btnIcone} ${styles.estadoNone}`}
                title={rotuloGostei}
                aria-label={rotuloGostei}
                onClick={criarManipulador()}
            >
                <FaThumbsUp aria-hidden="true" />
            </button>
            <button
                type="button"
                className={`${styles.btnIcone} ${styles.estadoNone}`}
                title={rotuloPlay}
                aria-label={rotuloPlay}
                onClick={criarManipulador(onPlay)}
            >
                <FaPlay aria-hidden="true" />
            </button>
        </div>
    );
};
