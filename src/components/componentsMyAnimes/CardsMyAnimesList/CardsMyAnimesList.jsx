import styles from './CardsMyAnimesList.module.css';
import { useState, useMemo, useCallback, useContext } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import CampoBuscar from '../../CampoBuscar/CampoBuscar';
import PaginationButtons from '../../PaginationButtons/PaginationButtons';
import QtdExibirPorPage from '../../QtdExibirPorPage/QtdExibirPorPage';
import ModalDialog from '../../ModalDialog/ModalDialog';
import CardMyAnimes from '../CardMyAnimes/CardMyAnimes';
import FiltrarPorLetra from '../../FiltrarPorLetra/FiltrarPorLetra';
import FiltrarPorAno from '../../FiltrarPorAno/FiltrarPorAno';
import MyAnimesObjsListContext from '../../../context_api/MyAnimesObjsListContext/MyAnimesObjsListContext';

export default function CardsMyAnimesList() {
    //Contexto, lista completa MyAnimes (json-server: http://localhost:3666/animacoes)
    const { listObjsMyAnimes } = useContext(MyAnimesObjsListContext);
    const navigate = useNavigate();
    //Filtro por Letra
    const [letraSelecionada, setLetraSelecionada] = useState('');
    //Filtro por Ano
    const [anoSelecionado, setAnoSelecionado] = useState('');
    //Paginação, estado local
    const [page, setPage] = useState(1);
    const [limit, setLimit] = useState(48);
    //Modal, subpastas, item selecionado
    const [selectedItem, setSelectedItem] = useState(null);
    //Estado local do campo Busca
    const [searchTerm, setSearchTerm] = useState('');
    //Filtros combinados e busca
    const filteredItems = useMemo(() => {
        let animesList = listObjsMyAnimes;
        // Campo de busca
        if (searchTerm) {
            animesList = animesList.filter(item =>
                String(item.nome).toLowerCase().includes(searchTerm.toLowerCase())
            );
        };
        // Filtro por letra inicial do título
        if (letraSelecionada) {
            animesList = animesList.filter(anime =>
                String(anime.nome).toUpperCase().startsWith(letraSelecionada)
            );
        };
        // Filtro por ano
        if (anoSelecionado) {
            animesList = animesList.filter(anime => {
                const anoAnime = anime.subpastas[0].ano;
                return String(anoAnime) === anoSelecionado;
            });
        };
        return animesList;
    }, [listObjsMyAnimes, searchTerm, letraSelecionada, anoSelecionado]);

    //Paginação, cálculo totalPages e itens paginados
    const totalPages = Math.max(1, Math.ceil(filteredItems.length / limit));
    const paginatedItems = useMemo(() => {
        const start = (page - 1) * limit;
        return filteredItems.slice(start, start + limit);
    }, [filteredItems, page, limit]);
    //Função de busca
    const handleSearch = useCallback((valor) => {
        setSearchTerm(valor);
        setPage(1);
    }, []);
    //Função ao clicar na imagem do card
    const handleImageClick = useCallback((item) => {
        // Se não houver subpastas, navega para a página de detalhes.
        if (!item.subpastas || item.subpastas.length === 0) {
            navigate(`/animex/animex-detalhes/${item.slug}`);
        } else {
            // Se houver uma ou mais subpastas, abre o modal.
            setSelectedItem(item);
        }
    }, [navigate]);
    //=======================================================
    return (
        <main className={styles.mainCardsMyAnimesList}>
            <CampoBuscar onSearch={handleSearch} />
            <div className={styles.divPaginacaoEFiltro}>
                <div className={styles.divContainerFiltros}>
                    <h4>Filtrar por: </h4>
                    <FiltrarPorLetra letraSelecionada={letraSelecionada} setLetraSelecionada={setLetraSelecionada} />
                    <FiltrarPorAno anoSelecionado={anoSelecionado} setAnoSelecionado={setAnoSelecionado} />
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
                        <strong className={styles.strongTotalAnimes}>{filteredItems.length}</strong> Animes encontrados
                    </span>
                )}
            </div>
            <div className={styles.divContainerListaCardsMyaAnimes}>
                {paginatedItems.map((item) => (
                    <CardMyAnimes
                        key={item.id}
                        item={item}
                        onImageClick={handleImageClick}
                    />
                ))}
            </div>
            <PaginationButtons
                currentPage={page}
                totalPages={totalPages}
                onPageChange={setPage}
            />
            {selectedItem && (<ModalDialog
                isOpen={!!selectedItem}
                onClose={() => setSelectedItem(null)}
                title={selectedItem.nome}
            >
                {selectedItem.subpastas?.map(item => (
                    <Link key={item.nome} to={`/myanimes/myanimes-detalhes/${selectedItem.slug}`} target='_blank'>
                        <p className={styles.pListMiniAnimes}>
                            <img className={styles.imgListMiniAnimes} src={`/myanimes/animes/${item.mal_id}.jpg`} alt={item.nomeSemAno} />
                            {item.nome}
                        </p>
                    </Link>
                ))}
            </ModalDialog>
            )}
        </main>
    );
};
