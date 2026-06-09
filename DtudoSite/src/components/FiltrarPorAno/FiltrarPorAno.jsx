import { useContext, useMemo } from 'react';
import AnimesObjsListDetalhesContext from '../../context_api/AnimesDetalhesObjsListContext/AnimesDetalhesObjsListContext';
import styles from './FiltrarPorAno.module.css';

export default function FiltrarPorAno({ anoSelecionado, setAnoSelecionado }) {
    const { listObjsDetalhesAnimes } = useContext(AnimesObjsListDetalhesContext);
    const anosUnicos = useMemo(() => {
        if (listObjsDetalhesAnimes.length > 0) {
            // Extrai todos os anos únicos
            const allYears = listObjsDetalhesAnimes.map(anime => {
                // Tenta pegar o ano de diferentes campos
                return anime.year || (anime.aired && anime.aired.from && anime.aired.from.year);
            }).filter(year => year); // Remove valores falsy
            return [...new Set(allYears)].sort((a, b) => b - a); // Ordena decrescente
        }
        return [];
    }, [listObjsDetalhesAnimes]);
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
