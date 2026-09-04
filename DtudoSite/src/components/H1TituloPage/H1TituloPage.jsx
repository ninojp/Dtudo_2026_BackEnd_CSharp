import styles from './H1TituloPage.module.css';

export default function H1TituloPage({children, className}) {
    return ( <h1 className={`${styles.h1TituloPadrao} ${className || ''}`}> {children} </h1> )
};
