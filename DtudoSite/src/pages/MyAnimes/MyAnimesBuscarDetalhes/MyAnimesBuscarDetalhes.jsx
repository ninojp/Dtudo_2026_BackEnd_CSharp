import { Link, useLocation } from 'react-router-dom';
import { useEffect, useMemo, useState } from 'react';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import styles from "./MyAnimesBuscarDetalhes.module.css";
import ModalDialog from '../../../components/ModalDialog/ModalDialog';

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
    const API_LOCAL_MYANIMES_BASE_URL = import.meta.env.VITE_API_LOCAL_MYANIMES_BASE_URL || 'https://localhost:63980/apiLocal';
    const location = useLocation();
    const animeFromState = location.state?.anime;
    const animeIdFromQuery = Number(new URLSearchParams(location.search).get('animeId')) || 0;

    const [animeDetalhes, setAnimeDetalhes] = useState(null);
    const [animeRelations, setAnimeRelations] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [isMyAnimeModalOpen, setIsMyAnimeModalOpen] = useState(false);
    const [isAnimeModalOpen, setIsAnimeModalOpen] = useState(false);
    const [myAnimeTitulo, setMyAnimeTitulo] = useState('');
    const [myAnimeMalIdsText, setMyAnimeMalIdsText] = useState('');
    const [animeMyAnimeId, setAnimeMyAnimeId] = useState('');
    const [submittingMyAnime, setSubmittingMyAnime] = useState(false);
    const [submittingAnime, setSubmittingAnime] = useState(false);
    const [feedbackMessage, setFeedbackMessage] = useState('');
    const [feedbackType, setFeedbackType] = useState('');

    const animeId = animeFromState?.malId || animeFromState?.mal_id || animeFromState?.mal_Id || animeIdFromQuery;
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
            animeDetalhes.image_url ||
            animeDetalhes.image_Url ||
            animeFromState?.imageUrl ||
            animeFromState?.image_url ||
            animeFromState?.image_Url ||
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
    function renderizarLista(items) {
        if (!items || items.length === 0) return '';
        return items.map((item) => {
                if (typeof item === 'string') return item;
                return item?.name || item?.Name || item?.title || JSON.stringify(item);
            })
            .filter(Boolean)
            .join(', ');
    }
    // Verifica se um valor é considerado "presente" para exibição.
    function temValorDentro(value) {
        if (value === null || value === undefined) return false;
        if (typeof value === 'string') return value.trim().length > 0;
        if (Array.isArray(value)) return value.length > 0;
        return true;
    }
    // Formata valores booleanos para exibição mais amigável (texto Sim/Não).
    function formatBoolean(value) {
        if (value === true) return 'Sim';
        if (value === false) return 'Nao';
        return '';
    }
    // Determina qual fonte de dados usar para exibir os detalhes do anime, priorizando os dados completos carregados da API.
    const dados = animeDetalhes || animeFromState;
    const titulosAlternativos = dados?.titleSynonyms || dados?.title_Synonyms;
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

    function resetFeedback() {
        setFeedbackMessage('');
        setFeedbackType('');
    }
    function closeMyAnimeModal() {
        setIsMyAnimeModalOpen(false);
    }
    function closeAnimeModal() {
        setIsAnimeModalOpen(false);
    }
    function parseApiError(errorText, fallbackMessage) {
        if (!errorText) return fallbackMessage;
        try {
            const parsed = JSON.parse(errorText);
            if (typeof parsed === 'string' && parsed.trim()) return parsed;
            if (parsed?.title) return parsed.title;
            if (parsed?.message) return parsed.message;
            return fallbackMessage;
        } catch {
            return errorText;
        }
    }

    function parseMalIdsList(malIdsText) {
        const parsedMalIds = malIdsText
            .split(',')
            .map((item) => Number(item.trim()))
            .filter((item) => Number.isInteger(item) && item > 0);

        const uniqueMalIds = [...new Set(parsedMalIds)];
        return uniqueMalIds;
    }

    function CadastrarMyAnimeNoDB(){
        const malIdAtual = Number(dados?.malId || dados?.mal_id || animeId || 0);
        setMyAnimeTitulo((dados?.title || dados?.titulo || '').trim());
        setMyAnimeMalIdsText(malIdAtual > 0 ? String(malIdAtual) : '');
        resetFeedback();
        setIsMyAnimeModalOpen(true);
    }

    async function submitCadastrarMyAnime(event) {
        event.preventDefault();
        const titulo = myAnimeTitulo.trim();
        const animesMalId = parseMalIdsList(myAnimeMalIdsText);
        if (!titulo) {
            setFeedbackType('error');
            setFeedbackMessage('Informe um titulo para cadastrar MyAnime.');
            return;
        };
        if (animesMalId.length === 0) {
            setFeedbackType('error');
            setFeedbackMessage('Informe ao menos um MalId valido.');
            return;
        };
        try {
            setSubmittingMyAnime(true);
            resetFeedback();
            const response = await fetch(`${API_LOCAL_MYANIMES_BASE_URL}/myanime`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    titulo,
                    animesMalId,
                }),
            });
            if (!response.ok) {
                const responseText = await response.text();
                throw new Error(parseApiError(responseText, `Falha ao cadastrar MyAnime (HTTP ${response.status}).`));
            };
            setFeedbackType('success');
            setFeedbackMessage('MyAnime cadastrado com sucesso no banco local.');
            setIsMyAnimeModalOpen(false);
        } catch (requestError) {
            setFeedbackType('error');
            setFeedbackMessage(requestError?.message || 'Nao foi possivel cadastrar MyAnime.');
        } finally {
            setSubmittingMyAnime(false);
        }
    }

    function CadastrarAnimeNoDB(){
        setAnimeMyAnimeId('');
        resetFeedback();
        setIsAnimeModalOpen(true);
    }

    async function submitCadastrarAnime(event) {
        event.preventDefault();
        const myAnimeIdParsed = Number(animeMyAnimeId);
        const malIdAtual = Number(dados?.malId || dados?.mal_id || animeId || 0);
        if (!Number.isInteger(myAnimeIdParsed) || myAnimeIdParsed <= 0) {
            setFeedbackType('error');
            setFeedbackMessage('Informe um ID MyAnime valido (numero inteiro positivo).');
            return;
        }
        if (!Number.isInteger(malIdAtual) || malIdAtual <= 0) {
            setFeedbackType('error');
            setFeedbackMessage('Nao foi possivel identificar o malId do anime atual.');
            return;
        }
        const animePayload = {
            malId: malIdAtual,
            titulo: (dados?.title || dados?.titulo || '').trim() || `Anime ${malIdAtual}`,
            episodios: Number(dados?.episodes) > 0 ? Number(dados.episodes) : 1,
            myAnimeID: myAnimeIdParsed,
        };

        try {
            setSubmittingAnime(true);
            resetFeedback();

            const response = await fetch(`${API_LOCAL_MYANIMES_BASE_URL}/anime?jikanId=${malIdAtual}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(animePayload),
            });

            if (!response.ok) {
                const responseText = await response.text();
                throw new Error(parseApiError(responseText, `Falha ao cadastrar Anime (HTTP ${response.status}).`));
            }

            setFeedbackType('success');
            setFeedbackMessage('Anime cadastrado com sucesso no banco local.');
            setIsAnimeModalOpen(false);
        } catch (requestError) {
            setFeedbackType('error');
            setFeedbackMessage(requestError?.message || 'Nao foi possivel cadastrar Anime.');
        } finally {
            setSubmittingAnime(false);
        }
    }
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
                    {temValorDentro(dados?.titleEnglish) && (
                        <p className={styles.subtitle}> {dados?.titleEnglish} </p>
                    )}                    
                    {temValorDentro(dados?.titleJapanese) && (
                        <p className={styles.subtitle}> {dados?.titleJapanese} </p>
                    )}
                    {temValorDentro(renderizarLista(titulosAlternativos)) && (
                        <p className={styles.subtitle}> {renderizarLista(titulosAlternativos)} </p>
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
                            {temValorDentro(dados?.score) && (
                                <div className={styles.scoreBox}>
                                    <strong>Score:</strong> {dados.score}
                                </div>
                            )}
                            {temValorDentro(dados?.synopsis) && (
                                <div className={styles.synopsisBlock}>
                                    <h4>Sinopse</h4>
                                    <p>{dados.synopsis}</p>
                                </div>
                            )}
                        </div>
                        <div className={styles.infoArea}>
                            <div className={styles.divInfoDetalhesTop}>
                                {temValorDentro(dados?.malId) && <div><strong>Mal_id:</strong> {dados?.malId}</div>}
                                {temValorDentro(dados?.type) && <div><strong>Tipo:</strong> {dados.type}</div>}
                                {temValorDentro(dados?.aired) && <div><strong>Data Lançamento:</strong> {dados?.aired}</div>}
                            </div>
                            <div className={styles.divInfoDetalhesTop}>
                                {temValorDentro(dados?.year) && <div><strong>Ano:</strong> {dados.year}</div>}
                                {temValorDentro(dados?.episodes) && <div><strong>Episodios:</strong> {dados.episodes}</div>}                            
                                {temValorDentro(dados?.duration) && <div><strong>Duracao:</strong> {dados.duration}</div>}
                            </div>
                            <div className={styles.divInfoDetalhesTop}>    
                                {temValorDentro(renderizarLista(dados?.genres)) && <div><strong>Generos:</strong> {renderizarLista(dados?.genres)}</div>}
                                {temValorDentro(dados?.rating) && <div><strong>Classificacao:</strong> {dados.rating}</div>}
                            </div>
                            <div className={styles.divInfoDetalhesTop}>
                                <button onClick={() => CadastrarMyAnimeNoDB()}>
                                    Cadastrar como MyAnime
                                </button>
                                <button onClick={() => CadastrarAnimeNoDB()}>
                                    Cadastrar como Anime
                                </button>
                            </div>
                            {feedbackMessage && (
                                <div className={feedbackType === 'error' ? styles.feedbackError : styles.feedbackSuccess}>
                                    {feedbackMessage}
                                </div>
                            )}
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
                                {temValorDentro(dados?.status) && <div><strong>Status:</strong> {dados.status}</div>}
                                {temValorDentro(dados?.airing) && <div><strong>Em Exibição:</strong> {formatBoolean(dados.airing)}</div>}
                                {temValorDentro(dados?.season) && <div><strong>Temporada:</strong> {dados.season}</div>}
                                {temValorDentro(dados?.rank) && <div><strong>Rank:</strong> {dados.rank}</div>}
                                {temValorDentro(dados?.popularity) && <div><strong>Popularidade:</strong> {dados.popularity}</div>}
                                {temValorDentro(dados?.members) && <div><strong>Membros:</strong> {dados.members}</div>}
                                {temValorDentro(dados?.favorites) && <div><strong>Favoritos:</strong> {dados.favorites}</div>}
                                {temValorDentro(dados?.scoredBy) && <div><strong>Scored By:</strong> {dados?.scoredBy}</div>}
                                {temValorDentro(dados?.source) && <div><strong>Source:</strong> {dados.source}</div>}
                                {temValorDentro(dados?.approved) && <div><strong>Aprovado:</strong> {formatBoolean(dados.approved)}</div>}
                            </div>

                            <div className={styles.gridInfo}>
                                {temValorDentro(dados?.background) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Background</h4>
                                        <p>{dados.background}</p>
                                    </div>
                                )}

                                {temValorDentro(renderizarLista(dados?.explicitGenres)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Generos Explicitos</h4>
                                        <p>{renderizarLista(dados?.explicitGenres)}</p>
                                    </div>
                                )}

                                {temValorDentro(renderizarLista(dados?.themes)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Temas</h4>
                                        <p>{renderizarLista(dados?.themes)}</p>
                                    </div>
                                )}

                                {temValorDentro(renderizarLista(dados?.demographics)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Demografia</h4>
                                        <p>{renderizarLista(dados?.demographics)}</p>
                                    </div>
                                )}

                                {temValorDentro(renderizarLista(dados?.studios)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Studios</h4>
                                        <p>{renderizarLista(dados?.studios)}</p>
                                    </div>
                                )}

                                {temValorDentro(renderizarLista(dados?.producers)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Produtores</h4>
                                        <p>{renderizarLista(dados?.producers)}</p>
                                    </div>
                                )}

                                {temValorDentro(renderizarLista(dados?.licensors)) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Licensors</h4>
                                        <p>{renderizarLista(dados?.licensors)}</p>
                                    </div>
                                )}

                                {(dados?.trailer) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Trailer</h4>
                                        <a href={dados?.trailer} target="_blank" rel="noopener noreferrer">
                                            Embed Url trailer
                                        </a>
                                    </div>
                                )}

                                {temValorDentro(dados?.url) && (
                                    <div className={styles.sectionBlock}>
                                        <h4>Link MyAnimeList</h4>
                                        <a href={dados.url} target="_blank" rel="noopener noreferrer">
                                            {dados.url}
                                        </a>
                                    </div>
                                )}
                            </div>
                        </div>
                    </section>
                )}
                <ModalDialog
                    isOpen={isMyAnimeModalOpen}
                    onClose={closeMyAnimeModal}
                    title="Cadastrar como MyAnime"
                >
                    <form className={styles.modalForm} onSubmit={submitCadastrarMyAnime}>
                        <label className={styles.modalLabel} htmlFor="myanime-titulo">
                            Titulo
                        </label>
                        <input
                            id="myanime-titulo"
                            type="text"
                            className={styles.modalInput}
                            value={myAnimeTitulo}
                            onChange={(e) => setMyAnimeTitulo(e.target.value)}
                            placeholder="Digite o titulo da colecao"
                            required
                        />

                        <label className={styles.modalLabel} htmlFor="myanime-malids">
                            Lista de MalId (separados por virgula)
                        </label>
                        <input
                            id="myanime-malids"
                            type="text"
                            className={styles.modalInput}
                            value={myAnimeMalIdsText}
                            onChange={(e) => setMyAnimeMalIdsText(e.target.value)}
                            placeholder="Ex.: 5114, 9253"
                            required
                        />

                        <div className={styles.modalActions}>
                            <button type="button" onClick={closeMyAnimeModal} disabled={submittingMyAnime}>
                                Cancelar
                            </button>
                            <button type="submit" disabled={submittingMyAnime}>
                                {submittingMyAnime ? 'Cadastrando...' : 'Confirmar cadastro'}
                            </button>
                        </div>
                    </form>
                </ModalDialog>

                <ModalDialog
                    isOpen={isAnimeModalOpen}
                    onClose={closeAnimeModal}
                    title="Cadastrar como Anime"
                >
                    <form className={styles.modalForm} onSubmit={submitCadastrarAnime}>
                        <div className={styles.modalAnimePreview}>
                            <img
                                src={imageUrl}
                                alt={dados?.title || 'Anime'}
                                className={styles.modalAnimePreviewImage}
                                onError={(e) => {
                                    e.currentTarget.src = placeholderImage;
                                }}
                            />
                            <div className={styles.modalAnimePreviewInfo}>
                                <p><strong>Titulo:</strong> {dados?.title || dados?.titulo || 'Sem titulo'}</p>
                                <p><strong>MalId:</strong> {Number(dados?.malId || dados?.mal_id || animeId || 0) || 'Nao encontrado'}</p>
                            </div>
                        </div>

                        <label className={styles.modalLabel} htmlFor="anime-myanimeid">
                            ID MyAnime
                        </label>
                        <input
                            id="anime-myanimeid"
                            type="number"
                            min="1"
                            step="1"
                            className={styles.modalInput}
                            value={animeMyAnimeId}
                            onChange={(e) => setAnimeMyAnimeId(e.target.value)}
                            placeholder="Ex.: 3"
                            required
                        />

                        <div className={styles.modalActions}>
                            <button type="button" onClick={closeAnimeModal} disabled={submittingAnime}>
                                Cancelar
                            </button>
                            <button type="submit" disabled={submittingAnime}>
                                {submittingAnime ? 'Cadastrando...' : 'Confirmar cadastro'}
                            </button>
                        </div>
                    </form>
                </ModalDialog>
                <div className={styles.rawJsonContainer}>
                    <details className={styles.rawJson}>
                        <summary>Ver objeto completo (JSON)</summary>
                        <pre>{JSON.stringify(dados, null, 2)}</pre>
                    </details>                            
                </div>                        
            </main>
        </>
    );
};
