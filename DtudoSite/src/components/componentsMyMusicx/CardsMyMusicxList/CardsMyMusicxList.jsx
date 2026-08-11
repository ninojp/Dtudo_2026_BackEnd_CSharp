import { useContext, useState, useMemo, useCallback } from "react";
import MyMusicxObjsListContext from "../../../context_api/MyMusicxObjsListContext/MyMusicxObjsListContext";
import { Link } from 'react-router-dom';
import styles from './CardsMyMusicxList.module.css';
import CampoBuscar from "../../CampoBuscar/CampoBuscar";
import FiltrarPorLetra from "../../FiltrarPorLetra/FiltrarPorLetra";
import FiltrarPorAno from "../../FiltrarPorAno/FiltrarPorAno";
import QtdExibirPorPage from "../../QtdExibirPorPage/QtdExibirPorPage";
import PaginationButtons from "../../PaginationButtons/PaginationButtons";
import CardRelease from "../CardRelease/CardRelease";
import Spinner from "../../Spinner/Spinner";
import ButtonPadrao from "../../ButtonPadrao/ButtonPadrao";
export default function CardsMyMusicxList() {
    const {
        listObjsMyMusicx,
        isLoading,
        errorMessage,
        fetchAllObjsMyMusicx,
    } = useContext(MyMusicxObjsListContext);
    const [letraSelecionada, setLetraSelecionada] = useState('');
    const [anoSelecionado, setAnoSelecionado] = useState('');
    const [page, setPage] = useState(1);
    const [limit, setLimit] = useState(48);
    const [searchTerm, setSearchTerm] = useState('');

    const filteredItems = useMemo(() => {
        let myMusicxList = listObjsMyMusicx;

        if (searchTerm) {
            myMusicxList = myMusicxList.filter(item =>
                [item.displayName, ...(item.artists || []).map(artist => artist.displayName)]
                    .filter(Boolean)
                    .some(value => String(value).toLowerCase().includes(searchTerm.toLowerCase()))
            );
        };

        if (letraSelecionada) {
            myMusicxList = myMusicxList.filter(item =>
                String(item.displayName).toUpperCase().startsWith(letraSelecionada)
            );
        };

        if (anoSelecionado) {
            myMusicxList = myMusicxList.filter(item => {
                const releaseYears = item.releaseYears || [];
                return releaseYears.map(String).includes(anoSelecionado);
            });
        };

        return myMusicxList;
    }, [listObjsMyMusicx, searchTerm, letraSelecionada, anoSelecionado]);

    const totalPages = Math.max(1, Math.ceil(filteredItems.length / limit));
    const paginatedItems = useMemo(() => {
        const start = (page - 1) * limit;
        return filteredItems.slice(start, start + limit);
    }, [filteredItems, page, limit]);

    const handleSearch = useCallback((valor) => {
        setSearchTerm(valor);
        setPage(1);
    }, []);

    if (isLoading) {
        return <main className={styles.mainCardsMyAnimesList}><Spinner /></main>;
    }

    if (errorMessage) {
        return (
            <main className={styles.mainCardsMyAnimesList}>
                <div className={styles.apiStateContainer} role="alert">
                    <p>{errorMessage}</p>
                    <ButtonPadrao onClick={() => fetchAllObjsMyMusicx()}>Tentar novamente</ButtonPadrao>
                </div>
            </main>
        );
    }

    return (
        <main className={styles.mainCardsMyAnimesList}>
            <CampoBuscar onSearch={handleSearch} />
            <div className={styles.divPaginacaoEFiltro}>
                <div className={styles.divContainerFiltros}>
                    <h4>Filtrar por: </h4>
                    <FiltrarPorLetra letraSelecionada={letraSelecionada} setLetraSelecionada={setLetraSelecionada} />
                    <FiltrarPorAno
                        anoSelecionado={anoSelecionado}
                        setAnoSelecionado={setAnoSelecionado}
                        animes={listObjsMyMusicx}
                    />
                </div>
                <QtdExibirPorPage
                    value={limit}
                    onChange={(newLimit) => { setLimit(newLimit); setPage(1); }}
                    options={[12, 24, 48, 96]}
                />
            </div>
            <div>
                {(searchTerm || letraSelecionada || anoSelecionado) && (
                    <span className={styles.spanTotalAnimes}>
                        <strong className={styles.strongTotalAnimes}>{filteredItems.length}</strong> Artistas encontrados
                    </span>
                )}
            </div>
            <div className={styles.divContainerListaCardsMyaAnimes}>
                {paginatedItems.length === 0 ? (
                    <div className={styles.apiStateContainer}>
                        <p>Nenhuma Coleção local foi encontrada.</p>
                    </div>
                ) : paginatedItems.map((item) => (
                    <Link key={item.musicCollectionId} to={`/mymusicx/mymusicx-detalhes/${item.musicCollectionId}`}>
                        <CardRelease
                            cdTitulo={item.displayName}
                            cdImgSrc={`/mymusicx/${String(item.displayName).toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')}.jpg`}
                            cdAno={item.releaseCount ? `${item.releaseCount} releases` : ''}
                        />
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
