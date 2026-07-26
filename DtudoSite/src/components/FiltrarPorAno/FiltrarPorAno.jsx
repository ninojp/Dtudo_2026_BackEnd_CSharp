import { useContext, useMemo } from 'react';
import AnimesObjsListDetalhesContext from '../../context_api/AnimesDetalhesObjsListContext/AnimesDetalhesObjsListContext';
import styles from './FiltrarPorAno.module.css';

export default function FiltrarPorAno({ anoSelecionado, setAnoSelecionado, animes }) {
    const { listObjsDetalhesAnimes } = useContext(AnimesObjsListDetalhesContext);
    const listaParaFiltrar = animes || listObjsDetalhesAnimes;
    const anosUnicos = useMemo(() => {
        if (listaParaFiltrar.length > 0) {
            const allYears = listaParaFiltrar.map(anime => {
                if (anime.year) return anime.year;
                if (anime.aired?.prop?.from?.year) return anime.aired.prop.from.year;
                return String(anime.aired || '').match(/\b(19|20)\d{2}\b/)?.[0];
            }).filter(year => year); // Remove valores falsy
            return [...new Set(allYears)].sort((a, b) => b - a); // Ordena decrescente
        }
        return [];
    }, [listaParaFiltrar]);
    //=======================================================
    return (
        <div className={styles.divFiltrarAno}>
            <select name='selectAno' className={styles.selectOptionsAno}
                value={anoSelecionado}
                onChange={(e) => setAnoSelecionado(e.target.value)}
            >
                <option value="">Ano</option>
                {anosUnicos.map(ano => (
                    <option key={ano} value={ano}>
                        {ano}
                    </option>
                ))}
            </select>
        </div>
    );
};
