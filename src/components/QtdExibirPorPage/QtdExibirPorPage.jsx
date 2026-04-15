import ParagrafoPage from '../ParagrafoPage/ParagrafoPage';
import styles from './QtdExibirPorPage.module.css';

export default function QtdExibirPorPage({ value, onChange, options = [ 12, 24, 48, 96] }) {
    return (
        <fieldset className={styles.containerParagQtd}>
            <label htmlFor='selectQtdPorPgId' className={styles.labelQtdPorPg}>
                <select id='selectQtdPorPgId' className={styles.selectQtdPorPg}
                    value={value}
                    onChange={(e) => onChange(Number(e.target.value))}
                >
                    {options.map(size => (
                        <option key={size} value={size}>
                            {size}
                        </option>
                    ))}
                </select>
                <span className={styles.spanQtdPorPg}> Animes por página.</span>
            </label>
        </fieldset >
    );
}
