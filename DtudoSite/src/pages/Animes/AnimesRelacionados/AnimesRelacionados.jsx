import { useContext, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import CardAnime from '../../../components/componentsAnimes/CardAnime/CardAnime';
import AuthContext from '../../../context_api/AuthContext/AuthContext';
import AnimesContext from '../../../context_api/AnimesContext/AnimesContext';
import { buscarTodasColecoesMyAnimeDaApiLocal } from '../../../services/apiMyAnimes';
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
    const { listObjsDetalhesAnimes, isLoading: animesCarregando } = useContext(AnimesContext);
    const [colecoes, setColecoes] = useState([]);
    const [isLoadingColecoes, setIsLoadingColecoes] = useState(true);
    const [error, setError] = useState(null);
    const myAnimeIdNumerico = Number(myAnimeId);

    useEffect(() => {
        if (!Number.isInteger(myAnimeIdNumerico) || myAnimeIdNumerico <= 0) {
            navigate('/animes', { replace: true });
        }
    }, [myAnimeIdNumerico, navigate]);

    useEffect(() => {
        const controller = new AbortController();
        let ativo = true;

        async function carregarColecoes() {
            setIsLoadingColecoes(true);
            setError(null);

            try {
                const colecoesDaApi = await buscarTodasColecoesMyAnimeDaApiLocal(controller.signal);
                if (ativo) setColecoes(colecoesDaApi);
            } catch (erro) {
                if (erro.code === 'ERR_CANCELED' || !ativo) return;
                setError('Nao foi possivel carregar as colecoes MyAnimes.');
            } finally {
                if (ativo) setIsLoadingColecoes(false);
            }
        }

        carregarColecoes();
        return () => {
            ativo = false;
            controller.abort();
        };
    }, []);

    const colecoesDoAnime = useMemo(() => colecoes.filter((colecao) => (
        Number(colecao.id) === myAnimeIdNumerico
    )), [colecoes, myAnimeIdNumerico]);

    const colecoesComAnimes = useMemo(() => colecoesDoAnime.map((colecao) => ({
        colecao,
        animes: (colecao.animesMalId || [])
            .map((malId) => listObjsDetalhesAnimes.find((anime) => Number(obterIdAnime(anime)) === Number(malId)))
            .filter((anime) => anime && (isAuthenticated || !ehAnimeAdulto(anime))),
    })).filter(({ animes }) => animes.length > 0), [colecoesDoAnime, isAuthenticated, listObjsDetalhesAnimes]);
            const colecaoAtual = colecoesDoAnime[0];

    if (animesCarregando || isLoadingColecoes) {
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
                <H2SubTitulo className={styles.subtituloColecao}>{colecaoAtual?.titulo || `MyAnime ID ${myAnimeIdNumerico}`}</H2SubTitulo>
            </HeaderPage>
            <main className={styles.mainRelacionados}>
                {colecoesComAnimes.length > 0 ? (
                    <section className={styles.sectionColecoes}>
                        {colecoesComAnimes.map(({ colecao, animes }) => (
                            <div key={colecao.id ?? colecao.titulo} className={styles.divColecao}>
                                <div className={styles.estatisticasColecao}>
                                    {obterEstatisticasColecao(animes).filter(({ icone }) => !icone).map(({ rotulo, valor, sufixo }, indice) => (
                                        <p className={indice === 0 ? styles.estatisticaPrincipal : undefined} key={rotulo}>
                                            <span className={styles.rotuloEstatistica}>{rotulo}:</span> {valor}{sufixo ? ` ${sufixo}` : ''}
                                        </p>
                                    ))}
                                    <div className={styles.estatisticasTipos}>
                                        {obterEstatisticasColecao(animes).filter(({ icone }) => icone).map(({ rotulo, valor, icone }) => (
                                            <span key={rotulo}>
                                                <span className={styles.iconeEstatistica}>{icone}</span>{' '}
                                                <span className={styles.tipoEstatistica}>{rotulo}:</span>{' '}
                                                <span className={styles.numeroEstatistica}>{valor}</span>
                                            </span>
                                        ))}
                                    </div>
                                </div>
                                <div className={styles.sectionCards}>
                                    {animes.map((anime) => (
                                        <Link key={obterIdAnime(anime)} to={`/animes/animes-detalhes/${obterIdAnime(anime)}`}>
                                            <CardAnime anime={anime} />
                                        </Link>
                                    ))}
                                </div>
                            </div>
                        ))}
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
