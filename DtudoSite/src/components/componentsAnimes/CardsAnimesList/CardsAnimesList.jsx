import styles from './CardsAnimesList.module.css';
import { useState, useMemo, useCallback, useContext, useEffect } from 'react';
import { Link } from 'react-router-dom';
import CampoBuscar from '../../CampoBuscar/CampoBuscar';
import PaginationButtons from '../../PaginationButtons/PaginationButtons';
import QtdExibirPorPage from '../../QtdExibirPorPage/QtdExibirPorPage';
import AnimesContext from '../../../context_api/AnimesContext/AnimesContext';
import AuthContext from '../../../context_api/AuthContext/AuthContext';
import FiltrarPorGenero from '../../FiltrarPorGenero/FiltrarPorGenero';
import FiltrarPorLetra from '../../FiltrarPorLetra/FiltrarPorLetra';
import FiltrarPorAno from '../../FiltrarPorAno/FiltrarPorAno';
import CardAnime from '../CardAnime/CardAnime';
import { ehAnimeAdulto, obterAnoAnime, obterGenerosAnime, obterIdAnime, obterTituloAnime } from '@dtudo-anime-content';
import { buscarAnimesDaApiLocalPorTermo } from '../../../services/apiMyAnimes';

export default function CardsAnimesList() {
    const { listObjsDetalhesAnimes, isLoading, error, recarregarAnimes } = useContext(AnimesContext);
    const { isAuthenticated } = useContext(AuthContext);
    const [generoSelecionado, setGeneroSelecionado] = useState('');
    const [letraSelecionada, setLetraSelecionada] = useState('');
    const [anoSelecionado, setAnoSelecionado] = useState('');
    const [page, setPage] = useState(1);
    const [limit, setLimit] = useState(48);
    const [searchTerm, setSearchTerm] = useState('');
    const [searchResults, setSearchResults] = useState([]);
    const [isSearching, setIsSearching] = useState(false);
    const [searchError, setSearchError] = useState(null);
    const [mostrarAdultos, setMostrarAdultos] = useState(false);

    useEffect(() => {
        const termo = searchTerm.trim();

        if (!termo) {
            setSearchResults([]);
            setSearchError(null);
            setIsSearching(false);
            return undefined;
        }

        const controller = new AbortController();
        let ativo = true;

        async function buscarAnimesPorTermo() {
            setIsSearching(true);
            setSearchError(null);

            try {
                const resultados = await buscarAnimesDaApiLocalPorTermo(termo, controller.signal);
                if (ativo) setSearchResults(resultados);
            } catch (erro) {
                if (erro.code === 'ERR_CANCELED' || !ativo) return;

                console.error('Erro ao buscar animes por termo na ApiMyAnimes:', erro);
                setSearchResults([]);
                setSearchError('Nao foi possivel buscar animes por esse termo. Tente novamente.');
            } finally {
                if (ativo) setIsSearching(false);
            }
        }

        buscarAnimesPorTermo();

        return () => {
            ativo = false;
            controller.abort();
        };
    }, [searchTerm]);

    const animesBase = searchTerm ? searchResults : listObjsDetalhesAnimes;

    const animesPermitidos = useMemo(() => {
        if (isAuthenticated && mostrarAdultos) {
            return animesBase.filter(ehAnimeAdulto);
        }

        return animesBase.filter((anime) => !ehAnimeAdulto(anime));
    }, [animesBase, isAuthenticated, mostrarAdultos]);

    const filteredItems = useMemo(() => {
        let animesList = animesPermitidos;
        if (generoSelecionado) {
            animesList = animesList.filter(anime =>
                obterGenerosAnime(anime).some((genero) => genero === generoSelecionado)
            );
        }
        if (letraSelecionada) {
            animesList = animesList.filter((anime) => {
                const titulo = obterTituloAnime(anime).trim();
                if (letraSelecionada === '#') {
                    return /^[0-9.]/.test(titulo);
                }

                return titulo.toLocaleUpperCase('pt-BR').startsWith(letraSelecionada);
            });
        }
        if (anoSelecionado) {
            animesList = animesList.filter((anime) => String(obterAnoAnime(anime)) === anoSelecionado);
        }

        if (searchTerm) return [...animesList];

        return [...animesList].sort((a, b) =>
            obterTituloAnime(a).localeCompare(obterTituloAnime(b), 'pt-BR', {
                numeric: true,
                sensitivity: 'base'
            })
        );
    }, [animesPermitidos, searchTerm, generoSelecionado, letraSelecionada, anoSelecionado]);

    const totalPages = Math.max(1, Math.ceil(filteredItems.length / limit));
    const paginatedItems = useMemo(() => {
        const start = (Math.max(1, Math.min(page, totalPages)) - 1) * limit;
        return filteredItems.slice(start, start + limit);
    }, [filteredItems, page, limit, totalPages]);

    const handleSearch = useCallback((valor) => {
        setSearchTerm(valor);
        setPage(1);
    }, []);

    const atualizarFiltro = useCallback((atualizar) => (valor) => {
        atualizar(valor);
        setPage(1);
    }, []);

    const alternarConteudoAdulto = useCallback(() => {
        setMostrarAdultos((valor) => !valor);
        setPage(1);
    }, []);

    if (isLoading) {
        return <div>Loading...</div>;
    }
    if (error) {
        return (
            <div role='alert'>
                <p>{error}</p>
                <button type='button' onClick={recarregarAnimes}>Tentar novamente</button>
            </div>
        );
    }

    return (
        <main className={styles.mainCardsAnimesList}>
            <CampoBuscar onSearch={handleSearch} />
            <div className={styles.divPaginacaoEFiltro}>
                <div className={styles.divContainerFiltros}>
                    <h4>Filtrar por: </h4>
                    <FiltrarPorLetra letraSelecionada={letraSelecionada} setLetraSelecionada={atualizarFiltro(setLetraSelecionada)} exibirNumericos />
                    <FiltrarPorGenero generoSelecionado={generoSelecionado} setGeneroSelecionado={atualizarFiltro(setGeneroSelecionado)} animes={animesPermitidos} />
                    <FiltrarPorAno anoSelecionado={anoSelecionado} setAnoSelecionado={atualizarFiltro(setAnoSelecionado)} animes={animesPermitidos} />
                    {isAuthenticated && (
                        <button
                            type='button'
                            className={styles.btnConteudoAdulto}
                            onClick={alternarConteudoAdulto}
                            aria-pressed={mostrarAdultos}
                        >
                            Hentai +18
                        </button>
                    )}
                </div>
                <QtdExibirPorPage
                    value={limit}
                    onChange={(newLimit) => { setLimit(newLimit); setPage(1); }}
                    options={[12, 24, 48, 96]}
                />
            </div>
            <div>
                {(searchTerm || generoSelecionado || letraSelecionada || anoSelecionado || mostrarAdultos) && (
                    <span className={styles.spanTotalAnimes}>
                        <strong className={styles.strongTotalAnimes}>{filteredItems.length}</strong> Animes encontrados
                    </span>
                )}
                {isSearching && <span className={styles.spanTotalAnimes}>Buscando...</span>}
                {searchError && <span className={styles.spanTotalAnimes} role='alert'>{searchError}</span>}
            </div>
            <div className={styles.divContainerListaCardsAnimes}>
                {paginatedItems?.map((animePg) => (
                    <Link key={obterIdAnime(animePg)} to={`/animes/animes-detalhes/${obterIdAnime(animePg)}`}>
                        <CardAnime anime={animePg} />
                    </Link>
                ))}
            </div>
            <PaginationButtons
                currentPage={page}
                totalPages={totalPages}
                onPageChange={setPage}
            />
        </main>
    );
};
