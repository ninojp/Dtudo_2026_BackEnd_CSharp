import { useMemo } from 'react';
import styles from './FiltrarPorGenero.module.css';

export default function FiltrarPorGenero({ generoSelecionado, setGeneroSelecionado, animes }) {
    const generosUnicos = useMemo(() => {
        const listaParaFiltrar = animes || [];
        if (listaParaFiltrar.length > 0) {
            const allGenres = listaParaFiltrar.flatMap(anime => [
                ...(anime.genres || []).map(g => typeof g === 'string' ? g : g.name),
                ...(anime.explicitGenres || anime.explicit_genres || []).map(g => typeof g === 'string' ? g : g.name),
                ...(anime.themes || []).map(t => typeof t === 'string' ? t : t.name),
                ...(anime.demographics || []).map(d => typeof d === 'string' ? d : d.name)
            ]);
            return [...new Set(allGenres.filter(Boolean))].sort();
        }
        return [];
    }, [animes]);
    //=======================================================
    return (
        <div className={styles.divFiltrarGenero}>
            <select name='selectGenero' className={styles.selectOptionsGenero}
                value={generoSelecionado}
                onChange={(e) => setGeneroSelecionado(e.target.value)}
            >
                <option value="">Gênero</option>
                {generosUnicos.map(genero => (
                    <option key={genero} value={genero}>
                        {genero}
                    </option>
                ))}
            </select>
        </div>
    );
};
