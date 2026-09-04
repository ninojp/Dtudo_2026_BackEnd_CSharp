import { useContext, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { FaCalendarAlt, FaStar } from 'react-icons/fa';
import { FaClock, FaTv, FaMasksTheater } from 'react-icons/fa6';
import { MdMovieCreation } from 'react-icons/md';
import AuthContext from '../../../context_api/AuthContext/AuthContext';
import AnimesContext from '../../../context_api/AnimesContext/AnimesContext';
import { buscarAnimePorMalId } from '../../../services/apiMyAnimes';
import {
    ehAnimeAdulto,
    obterAnimesRelacionados,
    obterAnoAnime,
    obterGenerosAnime,
    obterIdAnime,
    obterImagemAnime,
    obterTituloAnime,
    obterValoresAnime,
} from '@dtudo-anime-content';
import styles from './AnimesDetalhes.module.css';

const formatarNumero = (valor) => valor === null || valor === undefined || valor === '' ? null : Number(valor).toLocaleString('pt-BR');
const formatarLista = (valores) => Array.isArray(valores) && valores.length > 0 ? valores.join(', ') : null;
const possuiValor = (valor) => {
    if (Array.isArray(valor)) return valor.length > 0;
    return valor !== null && valor !== undefined && String(valor).trim() !== '';
};
const formatarValorMetadado = (valor) => {
    if (Array.isArray(valor)) return formatarLista(valor);
    if (typeof valor === 'boolean') return valor ? 'Sim' : 'Nao';
    if (valor instanceof Date) return valor.toLocaleString('pt-BR');
    return String(valor);
};
const obterTituloIngles = (anime) => anime.titleEnglish || anime.title_english || anime.alternativeTitles?.english || anime.alternative_titles?.english || null;
const obterSinonimos = (anime) => [
    ...obterValoresAnime(anime.titleSynonyms || anime.title_synonyms),
    ...obterValoresAnime(anime.alternativeTitles?.synonyms || anime.alternative_titles?.synonyms),
    ...obterValoresAnime(anime.synonyms),
].join(' • ') || null;
const obterTituloRelacionado = (anime) => obterTituloIngles(anime) || obterSinonimos(anime);
const obterTituloJapones = (anime) => anime.titleJapanese || anime.title_japanese || anime.alternativeTitles?.japanese || anime.alternative_titles?.japanese || null;
const obterMyAnimeId = (anime) => anime.myAnimeID ?? anime.myAnimeId ?? anime.MyAnimeID ?? anime.MyAnimeId;

export default function AnimesDetalhes() {
    const { malId } = useParams();
    const navigate = useNavigate();
    const { isAuthenticated } = useContext(AuthContext);
    const { listObjsDetalhesAnimes, isLoading: listaCarregando } = useContext(AnimesContext);
    const [animeRemoto, setAnimeRemoto] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);

    const malIdNumerico = Number(malId);
    const animeDaLista = useMemo(
        () => listObjsDetalhesAnimes.find((anime) => Number(obterIdAnime(anime)) === malIdNumerico),
        [listObjsDetalhesAnimes, malIdNumerico]
    );
    const anime = animeDaLista || animeRemoto;

    const animesRelacionados = useMemo(() => obterAnimesRelacionados({
        animeAtual: anime,
        incluirAdultos: isAuthenticated,
        listObjsDetalhesAnimes,
    }).filter((animeRelacionado) => Number(obterIdAnime(animeRelacionado)) !== malIdNumerico), [
        anime,
        isAuthenticated,
        listObjsDetalhesAnimes,
        malIdNumerico,
    ]);

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
    const titulosAlternativos = [obterTituloIngles(anime), obterSinonimos(anime), obterTituloJapones(anime)].filter(Boolean);
    const metadados = [
        ['Mal ID', obterIdAnime(anime)],
        ['MyAnime ID', obterMyAnimeId(anime)],
        ['Aprovado', anime.approved],
        ['Fonte', anime.source],
        ['Status', anime.status],
        ['Em exibicao', anime.airing],
        ['Exibicao', anime.aired],
        ['Classificacao', anime.rating],
        ['Votos', formatarNumero(anime.scoredBy)],
        ['Rank', formatarNumero(anime.rank)],
        ['Popularidade', formatarNumero(anime.popularity)],
        ['Membros', formatarNumero(anime.members)],
        ['Favoritos', formatarNumero(anime.favorites)],
        ['Temporada', anime.season],
        ['Produtores', anime.producers],
        ['Licenciadores', anime.licensors],
        ['Estudios', anime.studios],
        ['IDs de animes relacionados', anime.animesRelacionadosIds || anime.AnimesRelacionadosIds],
        ['Trailer', anime.trailer],
    ].filter(([, valor]) => possuiValor(valor));

    return (
        <main className={styles.mainDetalhes}>
            <header className={styles.gradeTitulos}>
                <h1 className={`${styles.tituloSecao} ${styles.tituloPrincipal}`}>{titulo}</h1>
                {titulosAlternativos.map((tituloAlternativo, indice) => (
                    <p
                        className={`${styles.tituloSecao} ${indice === 0 ? styles.tituloSecundario : styles.tituloAlternativo}`}
                        key={tituloAlternativo}
                    >
                        {tituloAlternativo}
                    </p>
                ))}
            </header>
            <aside className={styles.painelEsquerdo}>
                <figure className={styles.figurePoster}>
                    {imagem ? <img className={styles.imgPoster} src={imagem} alt={titulo} /> : <div className={styles.posterIndisponivel}>Imagem indisponivel</div>}
                </figure>
                <div className={styles.estatisticasRapidas}>
                    <div>
                        <span><FaCalendarAlt aria-hidden="true" /> {obterAnoAnime(anime) || 'N/A'}</span>
                        <span><MdMovieCreation aria-hidden="true" /> {anime.type || 'N/A'}</span>
                        <span><FaStar aria-hidden="true" /> {anime.score ?? 'N/A'}</span>
                    </div>
                    <div><span><FaTv aria-hidden="true" /> {anime.episodes || anime.episodios || 'N/A'} ep.</span></div>
                    <div><span><FaClock aria-hidden="true" /> {anime.duration || 'N/A'}</span></div>
                    <p><FaMasksTheater aria-hidden="true" /> {generos.length > 0 ? generos.join(' • ') : 'Generos nao informados'}</p>
                </div>
                <div className={styles.divAcoes}>
                    <Link to={`/animes/myanimes-colecao/${obterMyAnimeId(anime)}`} className={styles.linkAcao}>Coleção Completa</Link>
                    {anime.malUrl && <a href={anime.malUrl} target="_blank" rel="noreferrer" className={styles.linkAcao}>MyAnimeList</a>}
                </div>
            </aside>

            <section className={styles.painelDireito}>
                <section className={styles.sectionRelacionados}>
                    <p className={styles.rotuloSecao}>Animes relacionados</p>
                    {animesRelacionados.length > 0 ? (
                        <div className={styles.divCardsRelacionados}>
                            {animesRelacionados.map((animeRelacionado) => (
                                <Link key={obterIdAnime(animeRelacionado)} to={`/animes/animes-detalhes/${obterIdAnime(animeRelacionado)}`}>
                                    <article className={styles.cardRelacionado}>
                                        <strong>{obterTituloAnime(animeRelacionado)}</strong>
                                        {obterImagemAnime(animeRelacionado) ? <img src={obterImagemAnime(animeRelacionado)} alt={obterTituloAnime(animeRelacionado)} /> : <div className={styles.imagemRelacionadoIndisponivel}>Imagem indisponivel</div>}
                                        <span>{obterTituloRelacionado(animeRelacionado) || 'Titulo alternativo nao informado'}</span>
                                    </article>
                                </Link>
                            ))}
                        </div>
                    ) : <p>Nenhum anime relacionado encontrado.</p>}
                </section>

                <dl className={styles.dlMetadados}>
                    {metadados.map(([rotulo, valor]) => (
                        <div key={rotulo}>
                            <dt>{rotulo}</dt>
                            <dd>{formatarValorMetadado(valor)}</dd>
                        </div>
                    ))}
                </dl>

                <section className={styles.sectionTexto}>
                    <p className={styles.rotuloSecao}>Sinopse</p>
                    <p>{anime.synopsis || 'Sinopse nao informada.'}</p>
                    {anime.background && <p>{anime.background}</p>}
                </section>
            </section>
        </main>
    );
}
