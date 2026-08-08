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
    obterAnimesRelacionados,
    obterColecoesComAnime,
    obterIdAnime,
    obterTituloAnime,
    idsDaColecao,
} from '@dtudo-anime-content';
import styles from './AnimesRelacionados.module.css';

const PUBLIC_CATALOG_ONLY = import.meta.env.MODE === 'homologation';
const formatarTotalAnimesColecao = (total) => `${total} ${total === 1 ? 'Anime' : 'Animes'}`;

export default function AnimesRelacionados() {
    const { malId } = useParams();
    const navigate = useNavigate();
    const { isAuthenticated } = useContext(AuthContext);
    const { listObjsDetalhesAnimes, isLoading: animesCarregando } = useContext(AnimesContext);
    const [colecoes, setColecoes] = useState([]);
    const [isLoadingColecoes, setIsLoadingColecoes] = useState(true);
    const [error, setError] = useState(null);
    const malIdNumerico = Number(malId);

    const animeAtual = useMemo(
        () => listObjsDetalhesAnimes.find((anime) => Number(obterIdAnime(anime)) === malIdNumerico),
        [listObjsDetalhesAnimes, malIdNumerico]
    );

    useEffect(() => {
        if (!Number.isInteger(malIdNumerico) || malIdNumerico <= 0) {
            navigate('/animes', { replace: true });
        }
    }, [malIdNumerico, navigate]);

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
                setError('Nao foi possivel carregar as colecoes relacionadas.');
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

    const colecoesComAnime = useMemo(() => obterColecoesComAnime(colecoes, malIdNumerico), [colecoes, malIdNumerico]);

    const animesRelacionados = useMemo(() => obterAnimesRelacionados({
        colecoesComAnime,
        incluirAdultos: !PUBLIC_CATALOG_ONLY && isAuthenticated,
        listObjsDetalhesAnimes,
    }), [colecoesComAnime, isAuthenticated, listObjsDetalhesAnimes]);

    if (animesCarregando || isLoadingColecoes) {
        return <main className={styles.mainRelacionados}>Loading...</main>;
    }

    if (error) {
        return (
            <main className={styles.mainRelacionados} role="alert">
                <p>{error}</p>
                <Link to={`/animes/animes-detalhes/${malIdNumerico}`} className={styles.linkAcao}>Voltar aos detalhes</Link>
            </main>
        );
    }

    return (
        <>
            <HeaderPage>
                <H1TituloPage>Animes Relacionados</H1TituloPage>
                <H2SubTitulo>{animeAtual ? obterTituloAnime(animeAtual) : `MalId ${malIdNumerico}`}</H2SubTitulo>
            </HeaderPage>
            <main className={styles.mainRelacionados}>
                <div className={styles.divAcoes}>
                    <Link to="/animes" className={styles.linkAcao}>Lista</Link>
                    <Link to={`/animes/animes-detalhes/${malIdNumerico}`} className={styles.linkAcao}>Detalhes</Link>
                </div>

                {colecoesComAnime.length > 0 && (
                    <section className={styles.sectionColecoes}>
                        {colecoesComAnime.map((colecao) => {
                            const totalAnimes = idsDaColecao(colecao).length;

                            return (
                                <div key={colecao.id ?? colecao.titulo} className={styles.divColecao}>
                                    <p><strong>Coleção MyAnime:</strong> {colecao.titulo}</p>
                                    <p>Esta coleção tem {formatarTotalAnimesColecao(totalAnimes)}.</p>
                                </div>
                            );
                        })}
                    </section>
                )}

                <section className={styles.sectionCards}>
                    {animesRelacionados.length > 0 ? (
                        animesRelacionados.map((anime) => (
                            <Link key={obterIdAnime(anime)} to={`/animes/animes-detalhes/${obterIdAnime(anime)}`}>
                                <CardAnime anime={anime} />
                            </Link>
                        ))
                    ) : (
                        <p>Nenhum anime relacionado encontrado.</p>
                    )}
                </section>
            </main>
        </>
    );
}
