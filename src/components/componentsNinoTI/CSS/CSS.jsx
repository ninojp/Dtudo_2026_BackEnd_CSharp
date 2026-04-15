import CSSlogo from '/CSS_300h.webp';
import styles from './CSS3.module.css';

export default function CSS() {
  return (
    <div>
        <h3>CSS3</h3>
        <img src={CSSlogo} className={styles.logoCss} alt="CSS3 logo" />
    </div>
  )
};
