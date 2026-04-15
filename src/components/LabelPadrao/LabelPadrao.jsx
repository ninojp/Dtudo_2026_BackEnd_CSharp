import styles from './LabelPadrao.module.css';

export default function LabelPadrao({children, htmlFor}) {
  return (
    <label className={styles.labelPadrao} htmlFor={htmlFor}>
        {children}
    </label>
  );
};
