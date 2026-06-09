import styles from './H1TituloPage.module.css';

export default function H1TituloPage({children}) {
    return ( <h1 className={styles.h1TituloPadrao}> {children} </h1> )
};
