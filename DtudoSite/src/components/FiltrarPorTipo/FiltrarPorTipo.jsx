import styles from './FiltrarPorTipo.module.css';

export const TIPO_FILTRO_PADRAO = 'PADRAO';
export const TIPO_FILTRO_TODOS = 'TODOS';

export const OPCOES_TIPO_ANIME = [
    ['TV', 'Series (TV)'],
    ['OVA', 'OVA'],
    ['ONA', 'ONA'],
    ['MOVIE', 'Filme'],
    ['SPECIAL', 'Special / TV Special'],
    ['MUSIC', 'Music'],
    ['CM', 'CM'],
    ['PV', 'PV'],
];

export default function FiltrarPorTipo({ tipoSelecionado, setTipoSelecionado }) {
    return (
        <div className={styles.divFiltrarTipo}>
            <select name='selectTipo' className={styles.selectOptionsTipo}
                value={tipoSelecionado}
                onChange={(e) => setTipoSelecionado(e.target.value)}
            >
                <option value={TIPO_FILTRO_PADRAO}>Series, OVA, ONA e Filme</option>
                <option value={TIPO_FILTRO_TODOS}>Tudo</option>
                {OPCOES_TIPO_ANIME.map(([valor, rotulo]) => (
                    <option key={valor} value={valor}>
                        {rotulo}
                    </option>
                ))}
            </select>
        </div>
    );
};
