import styles from './FiltrarPorLetra.module.css';

export default function FiltrarPorLetra({ letraSelecionada, setLetraSelecionada }) {
    const letras = Array.from({ length: 26 }, (_, i) => String.fromCharCode(65 + i));
    return (
        <div className={styles.divFiltrarLetra}>
            <select className={styles.selectLetra}
                value={letraSelecionada}
                onChange={(e) => setLetraSelecionada(e.target.value)}
            >
                <option value="">Letra</option>
                {letras.map(letra => (
                    <option key={letra} value={letra}>
                        {letra}
                    </option>
                ))}
            </select>
        </div>
    );
};
