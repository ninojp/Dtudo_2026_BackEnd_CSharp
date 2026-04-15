import { useState, useEffect, useCallback, useRef } from "react";
import styles from "./CampoBuscar.module.css";

const CampoBuscar = ({ onSearch = () => {} }) => {
    const [valor, setValor] = useState("");
    const timerRef = useRef(null);
    //------------------------------------------------------------//
    const triggerSearch = useCallback(
        (value = valor) => {
            const trimmed = String(value).trim();
            if (!trimmed) {
                onSearch("");
                return;
            }
            if (trimmed.length >= 3) onSearch(trimmed);
        },
        [onSearch, valor]
    );
    //------------------------------------------------------------//
    useEffect(() => {
        // debounce: só dispara quando tiver >= 3 caracteres
        const trimmedLen = valor.trim().length;
        if (trimmedLen >= 3) {
            clearTimeout(timerRef.current);
            timerRef.current = setTimeout(() => triggerSearch(valor), 300);
            return () => clearTimeout(timerRef.current);
        }
        // se o campo ficou vazio, limpa imediatamente
        if (trimmedLen === 0) {
            onSearch("");
        }
        return () => {};
    }, [valor, triggerSearch, onSearch]);
    //------------------------------------------------------------//
    useEffect(() => {
        return () => clearTimeout(timerRef.current);
    }, []);
    //------------------------------------------------------------//
    const handleChange = (e) => setValor(e.target.value);
    //------------------------------------------------------------//
    const handleKeyDown = (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            triggerSearch();
        }
    };
    //------------------------------------------------------------//
    const handleBlur = () => triggerSearch();
    //------------------------------------------------------------//
    return (
        <fieldset className={styles.fieldsetCampoBuscar}>
            <label htmlFor="campo-buscar" className={styles.labelInput}>
                Busque pelo nome do que deseja procurar.
            </label>
            <input
                className={styles.inputCampoBuscar}
                id="campo-buscar"
                type="search"
                placeholder="Digite o termo, evite nomes completos"
                value={valor}
                onChange={handleChange}
                onKeyDown={handleKeyDown}
                onBlur={handleBlur}
                aria-label="campo de busca"
                name="campoBuscar"
            />
        </fieldset>
    );
};
export default CampoBuscar;
