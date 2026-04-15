import styles from './ButtonPadrao.module.css';

export default function ButtonPadrao({children, type, styleExterno, onClick}) {
  return (
    <button type={type} className={`${styles.btnPadrao} ${styleExterno}`} onClick={onClick}>
        {children}
    </button>
  );
};
