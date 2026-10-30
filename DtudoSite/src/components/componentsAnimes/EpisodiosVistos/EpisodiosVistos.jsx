import { useEpisodiosVistos } from '../../../hooks/useEpisodiosVistos';
import styles from './EpisodiosVistos.module.css';

const ROTULOS_ESTADO = {
    none: 'Nenhum episodio visto',
    partial: 'Em andamento',
    complete: 'Completo',
};

/**
 * Controle sobrio de episodios vistos: grade 1..total com marcar/desmarcar, "Marcar todos" e "Limpar".
 * Visivel apenas para usuario autenticado. Use `key={malId}` para reiniciar ao trocar de anime.
 */
export default function EpisodiosVistos({ malId }) {
    const {
        autenticado,
        progresso,
        carregando,
        ocupado,
        erro,
        alternarEpisodio,
        marcarTodos,
        limpar,
    } = useEpisodiosVistos(malId);

    if (!autenticado) return null;

    const total = progresso?.total ?? null;
    const vistos = progresso?.episodes ?? [];
    const conjuntoVistos = new Set(vistos);
    const numeros = total ? Array.from({ length: total }, (_, indice) => indice + 1) : [];

    return (
        <section className={styles.sectionEpisodios} aria-busy={carregando || ocupado}>
            <p className={styles.rotuloSecao}>Episodios vistos</p>

            {carregando && <p className={styles.aviso}>Carregando progresso...</p>}
            {erro && <p className={styles.erro} role="alert">{erro}</p>}

            {progresso && (
                <>
                    <div className={styles.divResumo}>
                        <span>
                            {total ? `${vistos.length} / ${total}` : `${vistos.length}`} · {ROTULOS_ESTADO[progresso.state]}
                        </span>
                        <div className={styles.divBotoes}>
                            {total && (
                                <button
                                    type="button"
                                    className={styles.btnAcao}
                                    onClick={marcarTodos}
                                    disabled={ocupado || vistos.length >= total}
                                >
                                    Marcar todos
                                </button>
                            )}
                            <button
                                type="button"
                                className={styles.btnAcao}
                                onClick={limpar}
                                disabled={ocupado || vistos.length === 0}
                            >
                                Limpar
                            </button>
                        </div>
                    </div>

                    {total ? (
                        <ul className={styles.gradeEpisodios}>
                            {numeros.map((numero) => {
                                const visto = conjuntoVistos.has(numero);
                                return (
                                    <li key={numero}>
                                        <button
                                            type="button"
                                            className={`${styles.btnEpisodio} ${visto ? styles.episodioVisto : ''}`}
                                            aria-pressed={visto}
                                            aria-label={`Episodio ${numero}${visto ? ', visto' : ''}`}
                                            onClick={() => alternarEpisodio(numero)}
                                            disabled={ocupado}
                                        >
                                            {numero}
                                        </button>
                                    </li>
                                );
                            })}
                        </ul>
                    ) : (
                        <p className={styles.aviso}>
                            O total de episodios deste anime e desconhecido; por isso nao ha grade de episodios
                            nem a opcao de marcar todos.
                            {vistos.length > 0 && ` Episodios ja registrados: ${vistos.join(', ')}.`}
                        </p>
                    )}
                </>
            )}
        </section>
    );
}
