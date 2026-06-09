import styles from './H2SubTitulo.module.css';

export default function H2SubTitulo({children, className}) {
  return (
    <h2 className={`${styles.h2SubTitulo} ${className || ''}`}>{children}</h2>
  );
};
