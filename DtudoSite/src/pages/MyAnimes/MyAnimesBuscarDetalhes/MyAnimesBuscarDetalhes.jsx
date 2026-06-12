import { Link, useLocation } from 'react-router-dom';
import { useEffect, useMemo, useState } from 'react';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import styles from "./MyAnimesBuscarDetalhes.module.css";

export default function MyAnimesBuscarDetalhes() {
    const API_BASE_URL = 'https://localhost:7279/api/anime';
    const placeholderImage = 'https://via.placeholder.com/300x420?text=No+Image';
    const location = useLocation();
    const animeFromState = location.state?.anime;

    const [animeDetalhes, setAnimeDetalhes] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    const animeId = animeFromState?.malId || animeFromState?.mal_Id || 0;

    const imageUrl = useMemo(() => {
        if (!animeDetalhes) {
            return animeFromState?.imageUrl || placeholderImage;
        }

        const images = animeDetalhes.images;
        const jpg = images?.jpg || images?.Jpg;

        return (
            jpg?.largeImageUrl ||
            jpg?.large_image_url ||
            jpg?.large_Image_Url ||
            jpg?.imageUrl ||
            jpg?.image_url ||
            jpg?.image_Url ||
            animeDetalhes.imageUrl ||
            animeFromState?.imageUrl ||
            placeholderImage
        );
    }, [animeDetalhes, animeFromState]);

    useEffect(() => {
        async function carregarDetalhes() {
            if (!animeId) {
                setError('Nenhum anime selecionado. Volte para a busca e clique em um card.');
                setLoading(false);
                return;
            }

            try {
                setLoading(true);
                setError('');

                const response = await fetch(`${API_BASE_URL}/${animeId}`);
                if (!response.ok) {
                    throw new Error(`Erro ao buscar detalhes: ${response.status}`);
                }

                const data = await response.json();
                setAnimeDetalhes(data);
            } catch (requestError) {
                console.error('Erro ao carregar detalhes do anime:', requestError);
                setError('Nao foi possivel carregar os detalhes completos do anime.');
            } finally {
                setLoading(false);
            }
        }

        carregarDetalhes();
    }, [API_BASE_URL, animeId]);

    function renderList(items) {
        if (!items || items.length === 0) {
            return 'N/A';
        }

        return items
            .map((item) => {
                if (typeof item === 'string') {
                    return item;
                }

                return item?.name || item?.Name || item?.title || JSON.stringify(item);
            })
            .filter(Boolean)
            .join(', ');
    }

    const dados = animeDetalhes || animeFromState;
    const titulosAlternativos = dados?.titleSynonyms || dados?.title_Synonyms;
    const aired = dados?.aired;
    const trailer = dados?.trailer;

    return (
        <>
            <HeaderPage>
                <H1TituloPage>MyAnimesBuscar</H1TituloPage>
                <H2SubTitulo>
                    Pagina para exibir os detalhes completos do anime selecionado em
                    <span className={styles.spanTotalAnimes}> MyAnimesBuscarDetalhes</span>
                </H2SubTitulo>
            </HeaderPage>
            <main className={styles.mainCardsMyAnimesList}>
                <h3>Detalhes do Anime resultado da Busca</h3>

                <div className={styles.actionsBar}>
                    <Link className={styles.backButton} to="/myanimes/myanimes-buscar">
                        ← Voltar para busca
                    </Link>
                </div>

                {loading && <p className={styles.loading}>Carregando detalhes...</p>}
                {error && <p className={styles.error}>{error}</p>}

                {!loading && !error && dados && (
                    <section className={styles.detailsContainer}>
                        <div className={styles.posterArea}>
                            <img
                                src={imageUrl}
                                alt={dados?.title || 'Anime'}
                                className={styles.poster}
                                onError={(e) => {
                                    e.currentTarget.src = placeholderImage;
                                }}
                            />
                            <div className={styles.scoreBox}>
                                <strong>Score:</strong> {dados?.score ?? 'N/A'}
                            </div>
                        </div>

                        <div className={styles.infoArea}>
                            <h2>{dados?.title || 'Titulo nao disponivel'}</h2>
                            <p className={styles.subtitle}>
                                {dados?.titleEnglish || dados?.title_English || 'Sem titulo em ingles'}
                            </p>

                            <div className={styles.gridInfo}>
                                <div><strong>ID MAL:</strong> {dados?.malId || dados?.mal_Id || 'N/A'}</div>
                                <div><strong>Tipo:</strong> {dados?.type || 'N/A'}</div>
                                <div><strong>Episodios:</strong> {dados?.episodes ?? 'N/A'}</div>
                                <div><strong>Status:</strong> {dados?.status || 'N/A'}</div>
                                <div><strong>Airing:</strong> {String(dados?.airing ?? 'N/A')}</div>
                                <div><strong>Ano:</strong> {dados?.year ?? 'N/A'}</div>
                                <div><strong>Temporada:</strong> {dados?.season || 'N/A'}</div>
                                <div><strong>Duracao:</strong> {dados?.duration || 'N/A'}</div>
                                <div><strong>Classificacao:</strong> {dados?.rating || 'N/A'}</div>
                                <div><strong>Rank:</strong> {dados?.rank ?? 'N/A'}</div>
                                <div><strong>Popularidade:</strong> {dados?.popularity ?? 'N/A'}</div>
                                <div><strong>Membros:</strong> {dados?.members ?? 'N/A'}</div>
                                <div><strong>Favoritos:</strong> {dados?.favorites ?? 'N/A'}</div>
                                <div><strong>Scored By:</strong> {dados?.scoredBy ?? dados?.scored_By ?? 'N/A'}</div>
                                <div><strong>Source:</strong> {dados?.source || 'N/A'}</div>
                                <div><strong>Aprovado:</strong> {String(dados?.approved ?? 'N/A')}</div>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Titulo Japones</h4>
                                <p>{dados?.titleJapanese || dados?.title_Japanese || 'N/A'}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Titulos Alternativos</h4>
                                <p>{renderList(titulosAlternativos)}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Periodo de Exibicao</h4>
                                <p>{aired?.string || aired?.String || 'N/A'}</p>
                                <p>Inicio: {aired?.from || aired?.From || 'N/A'} | Fim: {aired?.to || aired?.To || 'N/A'}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Sinopse</h4>
                                <p>{dados?.synopsis || 'N/A'}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Background</h4>
                                <p>{dados?.background || 'N/A'}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Generos</h4>
                                <p>{renderList(dados?.genres)}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Generos Explicitos</h4>
                                <p>{renderList(dados?.explicitGenres || dados?.explicit_Genres)}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Temas</h4>
                                <p>{renderList(dados?.themes)}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Demografia</h4>
                                <p>{renderList(dados?.demographics)}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Studios</h4>
                                <p>{renderList(dados?.studios)}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Produtores</h4>
                                <p>{renderList(dados?.producers)}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Licensors</h4>
                                <p>{renderList(dados?.licensors)}</p>
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Trailer</h4>
                                {trailer?.url || trailer?.Url ? (
                                    <a href={trailer?.url || trailer?.Url} target="_blank" rel="noopener noreferrer">
                                        Abrir trailer
                                    </a>
                                ) : (
                                    <p>N/A</p>
                                )}
                            </div>

                            <div className={styles.sectionBlock}>
                                <h4>Link MyAnimeList</h4>
                                {dados?.url ? (
                                    <a href={dados.url} target="_blank" rel="noopener noreferrer">
                                        {dados.url}
                                    </a>
                                ) : (
                                    <p>N/A</p>
                                )}
                            </div>

                            <details className={styles.rawJson}>
                                <summary>Ver objeto completo (JSON)</summary>
                                <pre>{JSON.stringify(dados, null, 2)}</pre>
                            </details>
                        </div>
                    </section>
                )}
            </main>
        </>
    );
};
