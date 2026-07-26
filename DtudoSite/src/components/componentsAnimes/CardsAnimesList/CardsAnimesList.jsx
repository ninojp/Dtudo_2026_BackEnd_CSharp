import styles from './CardsAnimesList.module.css';
import { useState, useMemo, useCallback, useContext } from 'react';
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
import { ehAnimeAdulto, obterAnoAnime, obterGenerosAnime, obterIdAnime, obterTituloAnime } from '../../../utils/animeContentUtils';

export default function CardsAnimesList() {
    const { listObjsDetalhesAnimes, isLoading, error, recarregarAnimes } = useContext(AnimesContext);
    const { isAuthenticated } = useContext(AuthContext);
    const [generoSelecionado, setGeneroSelecionado] = useState('');
    const [letraSelecionada, setLetraSelecionada] = useState('');
    const [anoSelecionado, setAnoSelecionado] = useState('');
    const [page, setPage] = useState(1);
    const [limit, setLimit] = useState(48);
    const [searchTerm, setSearchTerm] = useState('');
    const [mostrarAdultos, setMostrarAdultos] = useState(false);

    const animesPermitidos = useMemo(() => {
        if (isAuthenticated && mostrarAdultos) {
            return listObjsDetalhesAnimes.filter(ehAnimeAdulto);
        }

        return listObjsDetalhesAnimes.filter((anime) => !ehAnimeAdulto(anime));
    }, [isAuthenticated, listObjsDetalhesAnimes, mostrarAdultos]);

    const filteredItems = useMemo(() => {
        let animesList = animesPermitidos;
        if (searchTerm) {
            animesList = animesList.filter(item =>
                obterTituloAnime(item).toLocaleLowerCase('pt-BR').includes(searchTerm.toLocaleLowerCase('pt-BR'))
            );
        }
        if (generoSelecionado) {
            animesList = animesList.filter(anime =>
                obterGenerosAnime(anime).some((genero) => genero === generoSelecionado)
            );
        }
        if (letraSelecionada) {
            animesList = animesList.filter(anime =>
                obterTituloAnime(anime).toLocaleUpperCase('pt-BR').startsWith(letraSelecionada)
            );
        }
        if (anoSelecionado) {
            animesList = animesList.filter((anime) => String(obterAnoAnime(anime)) === anoSelecionado);
        }
        if (searchTerm || generoSelecionado || letraSelecionada || anoSelecionado || mostrarAdultos) {
            animesList = [...animesList].sort((a, b) =>
                obterTituloAnime(a).localeCompare(obterTituloAnime(b), 'pt-BR', {
                    sensitivity: 'base'
                })
            );
        }
        return animesList;
    }, [animesPermitidos, searchTerm, generoSelecionado, letraSelecionada, anoSelecionado, mostrarAdultos]);

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
                    <FiltrarPorLetra letraSelecionada={letraSelecionada} setLetraSelecionada={atualizarFiltro(setLetraSelecionada)} />
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
