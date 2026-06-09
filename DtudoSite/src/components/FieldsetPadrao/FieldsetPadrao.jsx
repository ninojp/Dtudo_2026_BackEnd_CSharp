import styles from './FieldsetPadrao.module.css';

export default function FieldsetPadrao({ children }) {
    return (
        <fieldset className={styles.fieldsetPadrao}>
            {children}
        </fieldset>
    );
};
