import styles from './ParagrafoPage.module.css';

export default function ParagrafoPage({children}) {
  return ( <p className={styles.paragrafoPage}>{children} </p> )
};
