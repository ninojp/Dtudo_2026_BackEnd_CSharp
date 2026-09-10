import { useContext, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import CardAnime from '../../../components/componentsAnimes/CardAnime/CardAnime';
import AuthContext from '../../../context_api/AuthContext/AuthContext';
import { buscarColecaoMyAnimePorId, buscarAnimesPorMalIds } from '../../../services/apiMyAnimes';
import {
    ehAnimeAdulto,
    obterIdAnime,
    obterAnoAnime,
    obterIconeTipoAnime,
    obterTipoCanonicoAnime,
} from '@dtudo-anime-content';
import styles from './AnimesRelacionados.module.css';


const TIPOS_COLECAO = [
    ['TV', 'TV Series'],
    ['OVA', 'OVA'],
    ['ONA', 'ONA'],
    ['MOVIE', 'Movies'],
    ['SPECIAL', 'Specials'],
    ['MUSIC', 'Music'],
    ['CM', 'CM'],
    ['PV', 'PV'],
];

function obterEstatisticasColecao(animes) {
    const anos = animes
        .map((anime) => Number(obterAnoAnime(anime)))
        .filter((ano) => Number.isInteger(ano) && ano >= 1000 && ano <= 9999);
    const estatisticas = [
        { rotulo: 'Esta coleção possui', valor: animes.length, sufixo: animes.length === 1 ? 'anime' : 'animes' },
        ...(anos.length > 0 ? [
            { rotulo: 'Primeiro anime lançado em', valor: Math.min(...anos) },
            { rotulo: 'Seu Ultimo anime lançado em', valor: Math.max(...anos) },
        ] : []),
        ...TIPOS_COLECAO
            .map(([tipo, rotulo]) => ({
                rotulo,
                icone: obterIconeTipoAnime({ type: tipo }),
                valor: animes.filter((anime) => obterTipoCanonicoAnime(anime) === tipo).length,
            }))
            .filter(({ valor }) => valor > 0),
    ];

    return estatisticas;
}

export default function AnimesRelacionados() {
    const { isAuthenticated } = useContext(AuthContext);
    const { myAnimeId } = useParams();
    const navigate = useNavigate();
    const [colecao, setColecao] = useState(null);
    const [animesEncontrados, setAnimesEncontrados] = useState([]);
    const [isLoadingColecao, setIsLoadingColecao] = useState(true);
    const [error, setError] = useState(null);
    const myAnimeIdNumerico = Number(myAnimeId);

    useEffect(() => {
        if (!Number.isInteger(myAnimeIdNumerico) || myAnimeIdNumerico <= 0) {
            navigate('/animes', { replace: true });
        }
    }, [myAnimeIdNumerico, navigate]);

    useEffect(() => {
        if (!Number.isInteger(myAnimeIdNumerico) || myAnimeIdNumerico <= 0) return undefined;

        const controller = new AbortController();
        let ativo = true;

        async function carregarColecao() {
            setIsLoadingColecao(true);
            setError(null);

            try {
                const colecaoDaApi = await buscarColecaoMyAnimePorId(myAnimeIdNumerico, controller.signal);
                const animesDaColecao = await buscarAnimesPorMalIds(colecaoDaApi?.animesMalId, controller.signal);

                if (ativo) {
                    setColecao(colecaoDaApi);
                    setAnimesEncontrados(animesDaColecao);
                }
            } catch (erro) {
                if (erro.code === 'ERR_CANCELED' || !ativo) return;

                if (erro.response?.status === 404) {
                    setError(`Nenhuma coleção MyAnimes encontrada para o ID ${myAnimeIdNumerico}.`);
                } else {
                    setError('Nao foi possivel carregar a colecao MyAnimes.');
                }
            } finally {
                if (ativo) setIsLoadingColecao(false);
            }
        }

        carregarColecao();
        return () => {
            ativo = false;
            controller.abort();
        };
    }, [myAnimeIdNumerico]);

    const animesDaColecao = useMemo(() => {
        const idsDaColecao = colecao?.animesMalId || [];

        return idsDaColecao
            .map((malId) => {
                const animeEncontrado = animesEncontrados.find((anime) => Number(obterIdAnime(anime)) === Number(malId));
                if (animeEncontrado) return { malId, anime: animeEncontrado, disponivelLocalmente: true };
                return { malId, anime: null, disponivelLocalmente: false };
            })
            .filter(({ anime }) => !anime || isAuthenticated || !ehAnimeAdulto(anime));
    }, [colecao, animesEncontrados, isAuthenticated]);

    const animesVisiveis = useMemo(() => (
        animesDaColecao.filter(({ anime }) => anime !== null).map(({ anime }) => anime)
    ), [animesDaColecao]);

    if (isLoadingColecao) {
        return <main className={styles.mainRelacionados}>Loading...</main>;
    }

    if (error) {
        return (
            <main className={styles.mainRelacionados} role="alert">
                <p>{error}</p>
                <Link to="/animes" className={styles.linkAcao}>Voltar para Animes</Link>
            </main>
        );
    }

    return (
        <>
            <HeaderPage>
                <H1TituloPage className={styles.tituloColecao}>Coleção Completa</H1TituloPage>
                <H2SubTitulo className={styles.subtituloColecao}>{colecao?.titulo || `MyAnime ID ${myAnimeIdNumerico}`}</H2SubTitulo>
            </HeaderPage>
            <main className={styles.mainRelacionados}>
                {animesDaColecao.length > 0 ? (
                    <section className={styles.sectionColecoes}>
                        <div className={styles.divColecao}>
                            <div className={styles.estatisticasColecao}>
                                {obterEstatisticasColecao(animesVisiveis).filter(({ icone }) => !icone).map(({ rotulo, valor, sufixo }, indice) => (
                                    <p className={indice === 0 ? styles.estatisticaPrincipal : undefined} key={rotulo}>
                                        <span className={styles.rotuloEstatistica}>{rotulo}:</span> {valor}{sufixo ? ` ${sufixo}` : ''}
                                    </p>
                                ))}
                                <div className={styles.estatisticasTipos}>
                                    {obterEstatisticasColecao(animesVisiveis).filter(({ icone }) => icone).map(({ rotulo, valor, icone }) => (
                                        <span key={rotulo}>
                                            <span className={styles.iconeEstatistica}>{icone}</span>{' '}
                                            <span className={styles.tipoEstatistica}>{rotulo}:</span>{' '}
                                            <span className={styles.numeroEstatistica}>{valor}</span>
                                        </span>
                                    ))}
                                </div>
                            </div>
                            <div className={styles.sectionCards}>
                                {animesDaColecao.map(({ malId, anime, disponivelLocalmente }) => (
                                    disponivelLocalmente ? (
                                        <Link key={malId} to={`/animes/animes-detalhes/${obterIdAnime(anime)}`}>
                                            <CardAnime anime={anime} />
                                        </Link>
                                    ) : (
                                        <div key={malId} className={styles.cardPlaceholder} title={`MalId ${malId} ainda nao foi importado para o banco local.`}>
                                            <p>Anime ainda nao importado localmente</p>
                                            <p className={styles.placeholderMalId}>MalId: {malId}</p>
                                        </div>
                                    )
                                ))}
                            </div>
                        </div>
                    </section>
                ) : (
                    <section className={styles.sectionColecoes}>
                        <p>Nenhuma coleção MyAnimes encontrada.</p>
                    </section>
                )}
            </main>
        </>
    );
}

