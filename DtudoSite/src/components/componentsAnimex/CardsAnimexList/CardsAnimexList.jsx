import styles from './CardsAnimexList.module.css';
import { use, useState, useMemo, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import CampoBuscar from '../../CampoBuscar/CampoBuscar';
import AnimexObjsListContext from '../../../context_api/AnimexObjsListContext/AnimexObjsListContext';
import PaginationButtons from '../../PaginationButtons/PaginationButtons';
import QtdExibirPorPage from '../../QtdExibirPorPage/QtdExibirPorPage';
import ModalDialog from '../../ModalDialog/ModalDialog';
import CardAnimex from '../CardAnimex/CardAnimex';
import FiltrarPorLetra from '../../FiltrarPorLetra/FiltrarPorLetra';
import FiltrarPorAno from '../../FiltrarPorAno/FiltrarPorAno';


export default function CardsAnimexList() {
    // Lembrar que AQUI usei o hook use() do React 19+, para consumir o contexto de forma síncrona.
    const { listObjsAnimex } = use(AnimexObjsListContext);
    const navigate = useNavigate();

    // Estados de filtros
    const [letraSelecionada, setLetraSelecionada] = useState('');
    //Filtro por Ano
    const [anoSelecionado, setAnoSelecionado] = useState('');
    //Paginação
    const [page, setPage] = useState(1);
    const [limit, setLimit] = useState(48);
    //Busca
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedItem, setSelectedItem] = useState(null);

    // Filtrar lista com múltiplos critérios no lado do cliente
    const filteredItems = useMemo(() => {
        let animesList = listObjsAnimex;

        // Filtro: busca por nome
        if (searchTerm) {
            animesList = animesList.filter(item =>
                String(item.nome).toLowerCase().includes(searchTerm.toLowerCase())
            );
        }

        // Filtro: letra inicial do título
        if (letraSelecionada) {
            animesList = animesList.filter(anime =>
                String(anime.nome).toUpperCase().startsWith(letraSelecionada)
            );
        }

        // Filtro: ano com verificação segura de propriedades
        if (anoSelecionado) {
            animesList = animesList.filter(anime => {
                const primeiraSubpasta = anime.subpastas?.[0];
                const anoAnime = primeiraSubpasta?.ano;
                return anoAnime && String(anoAnime) === anoSelecionado;
            });
        }

        return animesList;
    }, [listObjsAnimex, searchTerm, letraSelecionada, anoSelecionado]);
    //Paginação, cálculo de total de páginas e itens paginados
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

    // Handler: clique na imagem do card
    const handleImageClick = useCallback((item) => {
        // Se não houver subpastas, navega para a página de detalhes.
        if (!item.subpastas || item.subpastas.length === 0) {
            navigate(`/animex/animex-detalhes/${item.slug}`);
        } else {
            // Se houver uma ou mais subpastas, abre o modal.
            setSelectedItem(item);
        }
    }, [navigate]);
    //=============================================
    return (
        <main className={styles.mainCardsAnimexList}>
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
                    textoParagrafo='Esta é uma seção para listar em ordem alfabética minha coleção de Hentai!S.'
                />
            </div>
            {(searchTerm || letraSelecionada || anoSelecionado) && (
                <div>
                    <span className={styles.spanTotalAnimes}>
                        <strong className={styles.strongTotalAnimes}>{filteredItems.length}</strong> Animes encontrados
                    </span>
                </div>
            )}
            <div className={styles.divContainerCardsListAnimex}>
                {paginatedItems.map((item) => (
                    <CardAnimex
                        key={String(item.id)}
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
            {selectedItem && (
                <ModalDialog
                    isOpen={!!selectedItem}
                    onClose={() => setSelectedItem(null)}
                    title={selectedItem.nome}
                >
                    {selectedItem.subpastas?.map(item => (
                        <Link key={item.id} to={`/animex/animex-detalhes/${selectedItem.slug}`} target='_blank'>
                            <p className={styles.pListMiniAnimes}>
                                <img className={styles.imgListMiniAnimes} src={`/animex/animes/${item.id}.jpg`} alt={item.nomeSemAno} />
                                {item.nome}
                            </p>
                        </Link>
                    ))}
                </ModalDialog>
            )}
        </main>
    );
};
