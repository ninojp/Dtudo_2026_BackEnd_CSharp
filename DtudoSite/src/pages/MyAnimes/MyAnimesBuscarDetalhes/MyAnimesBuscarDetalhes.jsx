import { Link, useLocation } from 'react-router-dom';
import { useEffect, useMemo, useState } from 'react';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import styles from "./MyAnimesBuscarDetalhes.module.css";

const placeholderImage = `data:image/svg+xml;utf8,${encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="300" height="420" viewBox="0 0 300 420">
        <defs>
            <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
                <stop offset="0%" stop-color="#143a66"/>
                <stop offset="100%" stop-color="#0b1d33"/>
            </linearGradient>
        </defs>
        <rect width="300" height="420" fill="url(#g)"/>
        <text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" fill="#ffffff" font-size="22" font-family="Arial, sans-serif">Sem imagem</text>
    </svg>`
)}`;

function resolveAnimeImage(data, fallbackImage = placeholderImage) {
    const images = data?.images;
    const jpg = images?.jpg || images?.Jpg;

    return (
        jpg?.smallImageUrl ||
        jpg?.small_image_url ||
        jpg?.small_Image_Url ||
        jpg?.imageUrl ||
        jpg?.image_url ||
        jpg?.image_Url ||
        jpg?.largeImageUrl ||
        jpg?.large_image_url ||
        jpg?.large_Image_Url ||
        data?.imageUrl ||
        data?.image_url ||
        data?.image_Url ||
        fallbackImage
    );
}

function resolveRelatedImage(item) {
    return resolveAnimeImage(item);
}

export default function MyAnimesBuscarDetalhes() {
    const API_LOCAL_JIKAN_BASE_URL = import.meta.env.VITE_API_LOCAL_JIKAN_BASE_URL || 'https://localhost:63982/ApiJikan';
    const location = useLocation();
    const animeFromState = location.state?.anime;
    const animeIdFromQuery = Number(new URLSearchParams(location.search).get('animeId')) || 0;

    const [animeDetalhes, setAnimeDetalhes] = useState(null);
    const [animeRelations, setAnimeRelations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    const animeId = animeFromState?.malId || animeFromState?.mal_Id || animeIdFromQuery;
    // Determina a URL da imagem do anime, priorizando os detalhes completos carregados da API.
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
    // useEffect para carregar os detalhes do anime quando o componente é montado ou quando o animeId muda.
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
                const [detalhesResponse, relacoesResponse] = await Promise.allSettled([
                    fetch(`${API_LOCAL_JIKAN_BASE_URL}/${animeId}`),
                    fetch(`${API_LOCAL_JIKAN_BASE_URL}/${animeId}/relations`),
                ]);

                if (detalhesResponse.status !== 'fulfilled' || !detalhesResponse.value.ok) {
                    const status = detalhesResponse.status === 'fulfilled'
                        ? detalhesResponse.value.status
                        : 'sem resposta';
                    throw new Error(`Erro ao buscar detalhes: ${status}`);
                }

                const data = await detalhesResponse.value.json();
                setAnimeDetalhes(data);

                if (relacoesResponse.status === 'fulfilled' && relacoesResponse.value.ok) {
                    const relacoesData = await relacoesResponse.value.json();
                    setAnimeRelations(Array.isArray(relacoesData) ? relacoesData : []);
                } else {
                    setAnimeRelations([]);
                }
            } catch (requestError) {
                console.error('Erro ao carregar detalhes do anime:', requestError);
                setError('Nao foi possivel carregar os detalhes completos do anime.');
                setAnimeRelations([]);
            } finally {
                setLoading(false);
            }
        }
        carregarDetalhes();
    }, [API_LOCAL_JIKAN_BASE_URL, animeId]);
    // Função auxiliar para renderizar listas de itens (como gêneros, estúdios, etc.) de forma consistente.
    function renderList(items) {
        if (!items || items.length === 0) {
            return '';
        }
        return items.map((item) => {
                if (typeof item === 'string') {
                    return item;
                }
                return item?.name || item?.Name || item?.title || JSON.stringify(item);
            })
            .filter(Boolean)
            .join(', ');
    }
    // Verifica se um valor é considerado "presente" para exibição.
    function hasValue(value) {
        if (value === null || value === undefined) {
            return false;
        }
        if (typeof value === 'string') {
            return value.trim().length > 0;
        }
        if (Array.isArray(value)) {
            return value.length > 0;
        }
        return true;
    }
    // Formata valores booleanos para exibição mais amigável (texto Sim/Não).
    function formatBoolean(value) {
        if (value === true) {
            return 'Sim';
        }
        if (value === false) {
            return 'Nao';
        }
        return '';
    }
    // Determina qual fonte de dados usar para exibir os detalhes do anime, priorizando os dados completos carregados da API.
    const dados = animeDetalhes || animeFromState;
    const titulosAlternativos = dados?.titleSynonyms || dados?.title_Synonyms;
    const aired = dados?.aired;
    const trailer = dados?.trailer;
    // Processa as relações do anime para exibição, garantindo que o formato seja consistente independentemente de como os dados foram retornados pela API.
    const relations = useMemo(() => {
        const relationGroups = animeRelations.length > 0
            ? animeRelations
            : (dados?.relations || []);
        if (!Array.isArray(relationGroups)) return [];
        return relationGroups.flatMap((group, groupIndex) => {
            const entries = group?.entry || group?.Entry;
            if (!Array.isArray(entries)) return [];
            return entries.map((entry, entryIndex) => ({
                relationType: group?.relation || group?.Relation || 'Relacionamento',
                malId: entry?.malId || entry?.mal_id || entry?.mal_Id || null,
                title: entry?.name || entry?.title || entry?.Name || 'Sem titulo',
                url: entry?.url || entry?.Url || '',
                imageUrl: entry?.imageUrl || entry?.image_url || resolveRelatedImage(entry),
                key: `${groupIndex}-${entryIndex}-${entry?.malId || entry?.mal_id || entry?.name || entry?.title || 'relation'}`,
            }));
        });
    }, [animeRelations, dados]);
    // O componente retorna a estrutura JSX para exibir os detalhes do anime, incluindo o título, imagem, sinopse, informações adicionais e relações com outros animes.
    return (
        <>
            <HeaderPage>
                <H1TituloPage>MyAnimesBuscar Detalhes</H1TituloPage>
                <br/>
                <H2SubTitulo>
                    <span className={styles.spanTotalAnimes}> {dados?.title || 'Nome do Anime'}</span>
                </H2SubTitulo>
                <div className={styles.divContainerSubTitulos}>
                    {hasValue(dados?.titleEnglish || dados?.title_English) && (
                        <p className={styles.subtitle}> {dados?.titleEnglish || dados?.title_English} </p>
                    )}                    
                    {hasValue(dados?.titleJapanese || dados?.title_Japanese) && (
                        <p className={styles.subtitle}> {dados?.titleJapanese || dados?.title_Japanese} </p>
                    )}
                    {hasValue(renderList(titulosAlternativos)) && (
                        <p className={styles.subtitle}> {renderList(titulosAlternativos)} </p>
                    )}
                </div>
            </HeaderPage>
            <main className={styles.mainCardsMyAnimesList}>
                {loading && <p className={styles.loading}>Carregando detalhes...</p>}
                {error && <p className={styles.error}>{error}</p>}

                {!loading && !error && dados && (
                    <section className={styles.detailsContainer}>
                        <div className={styles.posterArea}>
                            <img src={imageUrl}
                                alt={dados?.title || 'Anime'}
                                className={styles.poster}
                                onError={(e) => {
                                    e.currentTarget.src = placeholderImage;
                                }}
                            />
                            {hasValue(dados?.score) && (
                                <div className={styles.scoreBox}>
                                    <strong>Score:</strong> {dados.score}
                                </div>
                            )}
                            {hasValue(dados?.synopsis) && (
                                <div className={styles.synopsisBlock}>
                                    <h4>Sinopse</h4>
                                    <p>{dados.synopsis}</p>
                                </div>
                            )}
                        </div>
                        <div className={styles.infoArea}>
                            <div className={styles.divInfoDetalhesTop}>
                                {hasValue(dados?.malId || dados?.mal_Id) && <div><strong>Mal_id:</strong> {dados?.malId || dados?.mal_Id}</div>}
                                {hasValue(dados?.year) && <div><strong>Ano:</strong> {dados.year}</div>}
                                {hasValue(dados?.type) && <div><strong>Tipo:</strong> {dados.type}</div>}
                                {hasValue(aired?.string || aired?.String) && <div><strong>Data Lançamento:</strong> {aired?.string || aired?.String}</div>}
                            </div>
                            <div className={styles.divInfoDetalhesTop}>
                                {hasValue(dados?.episodes) && <div><strong>Episodios:</strong> {dados.episodes}</div>}                            
                                {hasValue(dados?.duration) && <div><strong>Duracao:</strong> {dados.duration}</div>}
                            </div>
                            <div className={styles.divInfoDetalhesTop}>    
                                {hasValue(renderList(dados?.genres)) && <div><strong>Generos:</strong> {renderList(dados?.genres)}</div>}
                                {hasValue(dados?.rating) && <div><strong>Classificacao:</strong> {dados.rating}</div>}
                            </div>
                            <div className={styles.divInfoDetalhesTop}>
                                <button >
                                    Cadastrar como MyAnime
                                </button>
                                <button >
                                    Cadastrar como Anime
                                </button>
                            </div>
                            {relations.length > 0 && (
                                <div className={styles.relationsSection}>
                                    <h4>Animes Relacionados</h4>
                                    <div className={styles.relationsCarouselShell}>
                                        {relations.map((item) => {
                                            const CardLink = item.malId ? Link : 'a';
                                            const cardProps = item.malId
                                                ? { to: `/myanimes/myanimes-buscar-detalhes?animeId=${item.malId}`,
                                                    state: {
                                                        anime: {
                                                            malId: item.malId,
                                                            title: item.title,
                                                            imageUrl: item.imageUrl,
                                                        },
                                                    },
                                                }
                                                : { href: item.url, target: '_blank', rel: 'noopener noreferrer', };
                                            return (
                                                <CardLink key={item.key} className={styles.relationCard} title={item.title} {...cardProps}>
                                                    <span className={styles.relationLabel}>{item.relationType}</span>
                                                    <img src={item.imageUrl} alt={item.title}
                                                        className={styles.relationImage}
                                                        onError={(e) => { e.currentTarget.src = placeholderImage;}}
                                                    />
                                                    <span className={styles.relationTitle}>{item.title}</span>
                                                </CardLink>
                                            );
                                        })}
                                    </div>
                                </div>
                            )}
                            <div className={styles.gridInfo}>
                                {hasValue(dados?.status) && <div><strong>Status:</strong> {dados.status}</div>}
                                {hasValue(dados?.airing) && <div><strong>Em Exibição:</strong> {formatBoolean(dados.airing)}</div>}
                                {hasValue(dados?.season) && <div><strong>Temporada:</strong> {dados.season}</div>}
                                {hasValue(dados?.rank) && <div><strong>Rank:</strong> {dados.rank}</div>}
                                {hasValue(dados?.popularity) && <div><strong>Popularidade:</strong> {dados.popularity}</div>}
                                {hasValue(dados?.members) && <div><strong>Membros:</strong> {dados.members}</div>}
                                {hasValue(dados?.favorites) && <div><strong>Favoritos:</strong> {dados.favorites}</div>}
                                {hasValue(dados?.scoredBy ?? dados?.scored_By) && <div><strong>Scored By:</strong> {dados?.scoredBy ?? dados?.scored_By}</div>}
                                {hasValue(dados?.source) && <div><strong>Source:</strong> {dados.source}</div>}
                                {hasValue(dados?.approved) && <div><strong>Aprovado:</strong> {formatBoolean(dados.approved)}</div>}
                            </div>

                            <div className={styles.gridInfo}>
                                {(hasValue(aired?.string || aired?.String) || hasValue(aired?.from || aired?.From) || hasValue(aired?.to || aired?.To)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Periodo de Exibicao</h4>
                                        {hasValue(aired?.string || aired?.String) && <p>{aired?.string || aired?.String}</p>}
                                        {(hasValue(aired?.from || aired?.From) || hasValue(aired?.to || aired?.To)) && (
                                            <p> Inicio: {aired?.from || aired?.From || '-'} | Fim: {aired?.to || aired?.To || '-'} </p>
                                        )}
                                    </div>
                                )}

                                {hasValue(dados?.background) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Background</h4>
                                        <p>{dados.background}</p>
                                    </div>
                                )}

                                {hasValue(renderList(dados?.explicitGenres || dados?.explicit_Genres)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Generos Explicitos</h4>
                                        <p>{renderList(dados?.explicitGenres || dados?.explicit_Genres)}</p>
                                    </div>
                                )}

                                {hasValue(renderList(dados?.themes)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Temas</h4>
                                        <p>{renderList(dados?.themes)}</p>
                                    </div>
                                )}

                                {hasValue(renderList(dados?.demographics)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Demografia</h4>
                                        <p>{renderList(dados?.demographics)}</p>
                                    </div>
                                )}

                                {hasValue(renderList(dados?.studios)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Studios</h4>
                                        <p>{renderList(dados?.studios)}</p>
                                    </div>
                                )}

                                {hasValue(renderList(dados?.producers)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Produtores</h4>
                                        <p>{renderList(dados?.producers)}</p>
                                    </div>
                                )}

                                {hasValue(renderList(dados?.licensors)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Licensors</h4>
                                        <p>{renderList(dados?.licensors)}</p>
                                    </div>
                                )}

                                {(trailer?.url || trailer?.Url) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Trailer</h4>
                                        <a href={trailer?.url || trailer?.Url} target="_blank" rel="noopener noreferrer">
                                            Abrir trailer
                                        </a>
                                    </div>
                                )}

                                {hasValue(dados?.url) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Link MyAnimeList</h4>
                                        <a href={dados.url} target="_blank" rel="noopener noreferrer">
                                            {dados.url}
                                        </a>
                                    </div>
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
