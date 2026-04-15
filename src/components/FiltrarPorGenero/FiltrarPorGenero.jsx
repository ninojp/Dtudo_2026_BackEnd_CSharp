import { useContext, useMemo } from 'react';
import AnimesObjsListDetalhesContext from '../../context_api/AnimesDetalhesObjsListContext/AnimesDetalhesObjsListContext';
import styles from './FiltrarPorGenero.module.css';

export default function FiltrarPorGenero({ generoSelecionado, setGeneroSelecionado }) {
    const { listObjsDetalhesAnimes } = useContext(AnimesObjsListDetalhesContext);
    const generosUnicos = useMemo(() => {
        if (listObjsDetalhesAnimes.length > 0) {
            // Extrai todos os genres, explicit_genres, themes, demographics e cria um conjunto único
            const allGenres = listObjsDetalhesAnimes.flatMap(anime => [
                ...(anime.genres || []).map(g => g.name),
                ...(anime.explicit_genres || []).map(g => g.name),
                ...(anime.themes || []).map(t => t.name),
                ...(anime.demographics || []).map(d => d.name)
            ]);
            return [...new Set(allGenres)].sort();
        }
        return [];
    }, [listObjsDetalhesAnimes]);
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
