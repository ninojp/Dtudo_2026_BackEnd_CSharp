import styles from './InputPadrao.module.css';

export default function InputPadrao({
    itTipo = "text",
    itId,
    itName,
    itPlaceholder = "",
    itValue = "",
    itOnChange = null,
    itOnKeyDown = null,
    itOnBlur = null,
    itAriaLabel = ""
}) {
    return (
        <input
            className={styles.inputCampoGenerico}
            type={itTipo}
            id={itId}
            name={itName}
            placeholder={itPlaceholder}
            value={itValue}
            onChange={itOnChange}
            onKeyDown={itOnKeyDown}
            onBlur={itOnBlur}
            aria-label={itAriaLabel}
        />
    )
};
