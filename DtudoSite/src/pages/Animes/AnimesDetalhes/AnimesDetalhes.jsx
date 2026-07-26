import { useContext, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import AuthContext from '../../../context_api/AuthContext/AuthContext';
import AnimesObjsListDetalhesContext from '../../../context_api/AnimesDetalhesObjsListContext/AnimesDetalhesObjsListContext';
import { buscarAnimePorMalId } from '../../../services/apiMyAnimes';
import {
    ehAnimeAdulto,
    obterAnoAnime,
    obterGenerosAnime,
    obterIdAnime,
    obterImagemAnime,
    obterTituloAnime,
} from '../../../utils/animeContentUtils';
import styles from './AnimesDetalhes.module.css';

const formatarNumero = (valor) => valor ? Number(valor).toLocaleString('pt-BR') : 'N/A';
const formatarLista = (valores) => Array.isArray(valores) && valores.length > 0 ? valores.join(', ') : 'N/A';

export default function AnimesDetalhes() {
    const { malId } = useParams();
    const navigate = useNavigate();
    const { isAuthenticated } = useContext(AuthContext);
    const { listObjsDetalhesAnimes, isLoading: listaCarregando } = useContext(AnimesObjsListDetalhesContext);
    const [animeRemoto, setAnimeRemoto] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);

    const malIdNumerico = Number(malId);
    const animeDaLista = useMemo(
        () => listObjsDetalhesAnimes.find((anime) => Number(obterIdAnime(anime)) === malIdNumerico),
        [listObjsDetalhesAnimes, malIdNumerico]
    );
    const anime = animeDaLista || animeRemoto;

    useEffect(() => {
        if (!Number.isInteger(malIdNumerico) || malIdNumerico <= 0) {
            navigate('/animes', { replace: true });
            return;
        }

        if (animeDaLista) {
            setAnimeRemoto(null);
            setIsLoading(false);
            setError(null);
            return;
        }

        if (listaCarregando) return;

        const controller = new AbortController();
        let ativo = true;

        async function carregarAnime() {
            setIsLoading(true);
            setError(null);

            try {
                const animeEncontrado = await buscarAnimePorMalId(malIdNumerico, controller.signal);
                if (ativo) setAnimeRemoto(animeEncontrado);
            } catch (erro) {
                if (erro.code === 'ERR_CANCELED' || !ativo) return;
                setError('Nao foi possivel carregar os detalhes deste anime.');
            } finally {
                if (ativo) setIsLoading(false);
            }
        }

        carregarAnime();
        return () => {
            ativo = false;
            controller.abort();
        };
    }, [animeDaLista, listaCarregando, malIdNumerico, navigate]);

    useEffect(() => {
        if (!isLoading && anime && ehAnimeAdulto(anime) && !isAuthenticated) {
            navigate('/animes', { replace: true });
        }
    }, [anime, isAuthenticated, isLoading, navigate]);

    if (isLoading || listaCarregando) return <main className={styles.mainDetalhes}>Loading...</main>;

    if (error) {
        return (
            <main className={styles.mainDetalhes} role="alert">
                <p>{error}</p>
                <Link to="/animes" className={styles.linkVoltar}>Voltar para Animes</Link>
            </main>
        );
    }

    if (!anime) {
        return (
            <main className={styles.mainDetalhes}>
                <p>Anime nao encontrado.</p>
                <Link to="/animes" className={styles.linkVoltar}>Voltar para Animes</Link>
            </main>
        );
    }

    const titulo = obterTituloAnime(anime);
    const imagem = obterImagemAnime(anime);
    const generos = obterGenerosAnime(anime);

    return (
        <>
            <HeaderPage>
                <H1TituloPage>{titulo}</H1TituloPage>
                <H2SubTitulo>{anime.titleJapanese || anime.titleEnglish || anime.type || 'Detalhes do anime'}</H2SubTitulo>
            </HeaderPage>
            <main className={styles.mainDetalhes}>
                <section className={styles.sectionResumo}>
                    <figure className={styles.figurePoster}>
                        {imagem ? (
                            <img className={styles.imgPoster} src={imagem} alt={titulo} />
                        ) : (
                            <div className={styles.posterIndisponivel}>Imagem indisponivel</div>
                        )}
                    </figure>

                    <div className={styles.divInfoPrincipal}>
                        <div className={styles.divAcoes}>
                            <Link to="/animes" className={styles.linkVoltar}>Voltar</Link>
                            <Link to={`/animes/animes-relacionados/${obterIdAnime(anime)}`} className={styles.linkAcao}>
                                Relacionados
                            </Link>
                            {anime.malUrl && (
                                <a href={anime.malUrl} target="_blank" rel="noreferrer" className={styles.linkAcao}>
                                    MyAnimeList
                                </a>
                            )}
                        </div>

                        <dl className={styles.dlMetadados}>
                            <div><dt>MalId</dt><dd>{obterIdAnime(anime)}</dd></div>
                            <div><dt>Ano</dt><dd>{obterAnoAnime(anime) || 'N/A'}</dd></div>
                            <div><dt>Episodios</dt><dd>{anime.episodes || anime.episodios || 'N/A'}</dd></div>
                            <div><dt>Status</dt><dd>{anime.status || 'N/A'}</dd></div>
                            <div><dt>Tipo</dt><dd>{anime.type || 'N/A'}</dd></div>
                            <div><dt>Fonte</dt><dd>{anime.source || 'N/A'}</dd></div>
                            <div><dt>Score</dt><dd>{anime.score ?? 'N/A'}</dd></div>
                            <div><dt>Popularidade</dt><dd>{formatarNumero(anime.popularity)}</dd></div>
                            <div><dt>Membros</dt><dd>{formatarNumero(anime.members)}</dd></div>
                            <div><dt>Favoritos</dt><dd>{formatarNumero(anime.favorites)}</dd></div>
                        </dl>
                    </div>
                </section>

                <section className={styles.sectionTexto}>
                    <h3>Sinopse</h3>
                    <p>{anime.synopsis || 'Sinopse nao informada.'}</p>
                </section>

                {anime.background && (
                    <section className={styles.sectionTexto}>
                        <h3>Background</h3>
                        <p>{anime.background}</p>
                    </section>
                )}

                <section className={styles.sectionListas}>
                    <p><strong>Generos:</strong> {formatarLista(generos)}</p>
                    <p><strong>Estudios:</strong> {formatarLista(anime.studios)}</p>
                    <p><strong>Produtores:</strong> {formatarLista(anime.producers)}</p>
                    <p><strong>Temas:</strong> {formatarLista(anime.themes)}</p>
                    <p><strong>Demografia:</strong> {formatarLista(anime.demographics)}</p>
                    <p><strong>Classificacao:</strong> {anime.rating || 'N/A'}</p>
                    <p><strong>Duracao:</strong> {anime.duration || 'N/A'}</p>
                    <p><strong>Exibicao:</strong> {anime.aired || 'N/A'}</p>
                </section>
            </main>
        </>
    );
}
