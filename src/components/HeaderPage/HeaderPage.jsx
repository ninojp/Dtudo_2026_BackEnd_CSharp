import styles from './HeaderPage.module.css';

export default function HeaderPage({children}) {
    return (
        <header className={styles.headerPage}>
            {children}
        </header>
    );
};
