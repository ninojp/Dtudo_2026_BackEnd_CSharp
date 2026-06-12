import styles from "./MyAnimesBuscarDetalhes.module.css";

export default function MyAnimesBuscarDetalhes() {
    return (
        <>
            <HeaderPage>
                <H1TituloPage>MyAnimesBuscar</H1TituloPage>
                <H2SubTitulo>Página para exibir os detalhes do anime que foi buscado na página anterior <span className={styles.spanTotalAnimes}> MyAnimesBuscarDetalhes</span></H2SubTitulo>
            </HeaderPage>
            <main className={styles.mainCardsMyAnimesList}>
                <h3>Detalhes do Anime resultado da Busca</h3>
            </main>
        </>
    );
};
