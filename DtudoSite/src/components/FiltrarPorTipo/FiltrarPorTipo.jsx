import styles from './FiltrarPorTipo.module.css';
import { OPCOES_TIPO_ANIME, TIPO_FILTRO_PADRAO, TIPO_FILTRO_TODOS } from './tiposAnime';

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
