import HTMLogo from '/HTML_300h.webp';
import styles from './HTML.module.css';
import H2SubTitulo from '../../H2SubTitulo/H2SubTitulo';

export default function HTML() {
    return (
        <main className={styles.mainContainerPage}>
            <div className={styles.divContainerAreasTI}>
                <div className={styles.divContainerLogoDescricao}>
                    <div>
                        <img src={HTMLogo} className={styles.imgLogoHTM} alt="HTML5 logo" />
                    </div>
                    <div className={styles.divTextoDescricao}>
                        <H2SubTitulo className={styles.h2SubTituloTI}>HTML</H2SubTitulo>
                        <p>HTML (Linguagem de Marcação de Hipertexto) é a linguagem padrão para criar e estruturar o conteúdo de páginas web. Não se trata de uma linguagem de programação, mas sim de uma linguagem de marcação que utiliza "tags" para organizar os elementos, definindo a hierarquia e o significado de cada parte do conteúdo. Essencialmente, o HTML fornece o esqueleto de um site, que depois é estilizado com CSS e se torna interativo com JavaScript.</p>
                    </div>
                </div>
                <h3></h3>
                <p>Carreiras, Formações, Certificados, Diplomas, Sites, Artigos, Vídeos e Livros(E-Books).</p>
                <div>
                </div>
            </div>
        </main>
    );
};
