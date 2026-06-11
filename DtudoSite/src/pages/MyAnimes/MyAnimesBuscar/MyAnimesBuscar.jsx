import { useEffect, useState } from 'react';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import styles from './MyAnimesBuscar.module.css';
// import { useContext } from 'react';
// import MyAnimesObjsListContext from '../../../context_api/MyAnimesObjsListContext/MyAnimesObjsListContext';

export default function MyAnimesBuscar() {
    // const { listObjsMyAnimes } = useContext(MyAnimesObjsListContext);
    const API_BASE_URL = 'https://localhost:7279/api/anime';
    const placeholderImage = 'https://via.placeholder.com/250x350?text=No+Image';

    const [searchInput, setSearchInput] = useState('');
    const [currentQuery, setCurrentQuery] = useState('');
    const [currentPage, setCurrentPage] = useState(1);
    const [hasNextPage, setHasNextPage] = useState(false);
    const [totalPages, setTotalPages] = useState(0);
    const [results, setResults] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [hasSearched, setHasSearched] = useState(false);

    useEffect(() => {
        async function searchAnime() {
            if (!currentQuery.trim()) {
                setLoading(false);
                setError('');
                setResults([]);
                setHasNextPage(false);
                setTotalPages(0);
                return;
            }

            try {
                setLoading(true);
                setError('');
                setResults([]);
                const url = `${API_BASE_URL}/search?q=${encodeURIComponent(currentQuery)}&page=${currentPage}`;
                const response = await fetch(url);
                if (!response.ok) {
                    throw new Error(`Erro na requisição: ${response.status}`);
                }
                const data = await response.json();
                setResults(data.results || []);
                setHasNextPage(Boolean(data.hasNextPage));
                setTotalPages(data.totalPages || 0);
                setHasSearched(true);
            } catch (requestError) {
                setError('Erro ao buscar animes. Verifique se a API está rodando.');
                console.error('Erro:', requestError);
            } finally {
                setLoading(false);
            }
        }
        searchAnime();
    }, [currentQuery, currentPage]);

    function handleSubmit(e) {
        e.preventDefault();
        const trimmedQuery = searchInput.trim();

        if (!trimmedQuery) {
            setCurrentPage(1);
            setCurrentQuery('');
            setHasSearched(false);
            setResults([]);
            setError('');
            setHasNextPage(false);
            setTotalPages(0);
            return;
        }

        setCurrentPage(1);
        setCurrentQuery(trimmedQuery);
    }

    function handlePrevPage() {
        if (currentPage > 1) {
            setCurrentPage((prev) => prev - 1);
        }
    }

    function handleNextPage() {
        if (hasNextPage) {
            setCurrentPage((prev) => prev + 1);
        }
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
                        <button type="submit" className={styles.searchButton} disabled={loading}>Buscar</button>
                    </form>
                </div>

                {error && <div className={styles.error}>{error}</div>}
                {loading && <div className={styles.loading}>🔄 Carregando...</div>}

                <div className={styles.results}>
                    {!loading && hasSearched && results.length === 0 && (
                        <p className={styles.emptyMessage}>Nenhum anime encontrado.</p>
                    )}

                    {results.map((anime) => (
                        <a
                            key={anime.malId || anime.url || anime.title}
                            className={styles.animeLink}
                            href={anime.url || '#'}
                            target="_blank"
                            rel="noopener noreferrer"
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
                        </a>
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
