import { use } from 'react';
import { Link } from 'react-router-dom';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import MyAnimesBuscarJikanContext from '../../../context_api/MyAnimesBuscarJikanContext/MyAnimesBuscarJikanContext';
import styles from './MyAnimesBuscar.module.css';

export default function MyAnimesBuscar() {
    const placeholderImage = 'https://via.placeholder.com/250x350?text=No+Image';

    const {
        searchInput,
        setSearchInput,
        currentPage,
        totalPages,
        hasNextPage,
        results,
        isLoading,
        error,
        hasSearched,
        handleSearch,
        handlePrevPage,
        handleNextPage,
    } = use(MyAnimesBuscarJikanContext);

    function handleSubmit(e) {
        e.preventDefault();
        handleSearch(searchInput);
    }

    return (
        <>
            <HeaderPage>
                <H1TituloPage>MyAnimesBuscar</H1TituloPage>
                <H2SubTitulo>Nova página para procurar Animes via <span className={styles.spanTotalAnimes}>ApiJikanC#</span></H2SubTitulo>
            </HeaderPage>
            <main className={styles.mainCardsMyAnimesList}>
                <h3>🎌 Busca de Animes</h3>
                <div className={styles.searchBox}>
                    <form className={styles.searchForm} onSubmit={handleSubmit}>
                        <input
                            type="text"
                            className={styles.searchInput}
                            placeholder="Digite o nome do anime (ex: Naruto, One Piece)..."
                            autoComplete="off"
                            value={searchInput}
                            onChange={(e) => setSearchInput(e.target.value)}
                        />
                        <button type="submit" className={styles.searchButton} disabled={isLoading}>Buscar</button>
                    </form>
                </div>

                {error && <div className={styles.error}>{error}</div>}
                {isLoading && <div className={styles.loading}>🔄 Carregando...</div>}

                <div className={styles.results}>
                    {!isLoading && hasSearched && results.length === 0 && (
                        <p className={styles.emptyMessage}>Nenhum anime encontrado.</p>
                    )}

                    {results.map((anime) => (
                        <Link
                            key={anime.malId || anime.url || anime.title}
                            className={styles.animeLink}
                            to={`/myanimes/myanimes-buscar-detalhes?animeId=${anime.malId}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            state={{ anime }}
                        >
                            <article className={styles.animeCard}>
                                <img
                                    src={anime.imageUrl || placeholderImage}
                                    alt={anime.title || 'Anime'}
                                    onError={(e) => {
                                        e.currentTarget.src = placeholderImage;
                                    }}
                                />
                                <div className={styles.animeInfo}>
                                    <div className={styles.animeTitle}>{anime.title || 'Título não disponível'}</div>
                                    <div className={styles.animeDetails}>
                                        <span>{anime.type || 'N/A'} {anime.episodes ? `• ${anime.episodes} eps` : ''}</span>
                                        <span className={styles.animeScore}>⭐ {anime.score || 'N/A'}</span>
                                    </div>
                                    <div className={styles.animeDetails}>
                                        <span>{anime.status || 'N/A'}</span>
                                        <span>{anime.year || 'N/A'}</span>
                                    </div>
                                    {anime.genres && anime.genres.length > 0 && (
                                        <div className={styles.animeGenres}>
                                            {anime.genres.slice(0, 3).map((genre) => (
                                                <span key={genre} className={styles.genreTag}>{genre}</span>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </article>
                        </Link>
                    ))}
                </div>

                {totalPages > 1 && (
                    <div className={styles.pagination}>
                        <button onClick={handlePrevPage} disabled={currentPage === 1}>← Anterior</button>
                        <span>Página {currentPage} de {totalPages}</span>
                        <button onClick={handleNextPage} disabled={!hasNextPage}>Próxima →</button>
                    </div>
                )}
            </main>
        </>
    );
}
