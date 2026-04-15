import styles from './BadgesTI.module.css';
import { Link } from 'react-router-dom';

export default function BadgesTI({ icon, text, backgroundColor, textColor = '#ffffff', to }) {
    // Gerar slug a partir do texto (ex: "HTML5" -> "html5", "Node.js" -> "nodejs")
    // Remove qualquer caractere que não seja letra ou número
    const slug = to || text.toLowerCase().replace(/[^a-z0-9]+/g, '');

    return (
        <Link to={`/ninoti/front-end/${slug}`} style={{ textDecoration: 'none' }}>
            <div className={styles.badgeContainer} style={{ backgroundColor: backgroundColor, color: textColor, cursor: 'pointer' }}>
                <span className={styles.icon}>
                    {icon}
                </span>
                <span className={styles.text}>
                    {text}
                </span>
            </div>
        </Link>
    );
}
