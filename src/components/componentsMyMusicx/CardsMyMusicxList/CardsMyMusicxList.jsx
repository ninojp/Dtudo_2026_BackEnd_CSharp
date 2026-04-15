import { useContext, useState, useMemo, useCallback } from "react";
import MyMusicxObjsListContext from "../../../context_api/MyMusicxObjsListContext/MyMusicxObjsListContext";
import { Link, useNavigate } from 'react-router-dom';
import styles from './CardsMyMusicxList.module.css';
import CampoBuscar from "../../CampoBuscar/CampoBuscar";
import FiltrarPorLetra from "../../FiltrarPorLetra/FiltrarPorLetra";
import FiltrarPorAno from "../../FiltrarPorAno/FiltrarPorAno";
import QtdExibirPorPage from "../../QtdExibirPorPage/QtdExibirPorPage";
import PaginationButtons from "../../PaginationButtons/PaginationButtons";
import CardRelease from "../CardRelease/CardRelease";
export default function CardsMyMusicxList() {
    const { listObjsMyMusicx } = useContext(MyMusicxObjsListContext);
    // console.log('listObjsMyMusicx:', listObjsMyMusicx);
    const navigate = useNavigate();
    //Filtro por Letra
    const [letraSelecionada, setLetraSelecionada] = useState('');
    //Filtro por Ano
    const [anoSelecionado, setAnoSelecionado] = useState('');
    //Estado Local Paginação
    const [page, setPage] = useState(1);
    const [limit, setLimit] = useState(48);
    //Estado local do campo Busca
    const [searchTerm, setSearchTerm] = useState('');
    //Estado Local Modal, subpastas, item selecionado
    const [selectedItem, setSelectedItem] = useState(null);
    //Filtros combinados e busca
    const filteredItems = useMemo(() => {
        let myMusicxList = listObjsMyMusicx;
        // Campo de busca
        if (searchTerm) {
            myMusicxList = myMusicxList.filter(item =>
                String(item.artista).toLowerCase().includes(searchTerm.toLowerCase())
            );
        };
        // Filtro por letra inicial do título
        if (letraSelecionada) {
            myMusicxList = myMusicxList.filter(item =>
                String(item.artista).toUpperCase().startsWith(letraSelecionada)
            );
        };
        // Filtro por ano
        if (anoSelecionado) {
            myMusicxList = myMusicxList.filter(item => {
                const anoMusicx = item.releases.albums[0].ano;
                return String(anoMusicx) === anoSelecionado;
            });
        };
        return myMusicxList;
    }, [listObjsMyMusicx, searchTerm, letraSelecionada, anoSelecionado]);
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
            navigate(`/mymusicx/mymusicx-detalhes/${item.slug}`);
        } else {
            // Se houver uma ou mais subpastas, abre o modal.
            setSelectedItem(item);
        }
    }, [navigate]);
    //=========================================================
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
                        <strong className={styles.strongTotalAnimes}>{filteredItems.length}</strong> Artistas encontrados
                    </span>
                )}
            </div>
            <div className={styles.divContainerListaCardsMyaAnimes}>
                {paginatedItems.map((item) => {
                    // Calcular primeiro e último ano dos álbuns
                    const albums = item.releases?.albums || [];
                    // Filtrar apenas álbuns que têm ano válido (não vazio)
                    const albumsComAno = albums.filter(album => album.ano && album.ano.trim() !== '');
                    let anoExibir = '';
                    if (albumsComAno.length === 1) {
                        anoExibir = albumsComAno[0].ano;
                    } else if (albumsComAno.length > 1) {
                        const primeiroAno = albumsComAno[0].ano;
                        const ultimoAno = albumsComAno[albumsComAno.length - 1].ano;
                        // Se primeiro e último ano são iguais, exibe só um
                        if (primeiroAno === ultimoAno) {
                            anoExibir = primeiroAno;
                        } else {
                            anoExibir = `${primeiroAno} - ${ultimoAno}`;
                        }
                    }
                    return (
                        <Link key={item.id} to={`/mymusicx/mymusicx-detalhes/${item.id}`}>
                            <CardRelease
                                cdTitulo={item.artista}
                                cdImgSrc={`/mymusicx/${item.id}.jpg`}
                                cdAno={anoExibir}
                            />
                        </Link>
                    );
                })}
            </div>
            <PaginationButtons
                currentPage={page}
                totalPages={totalPages}
                onPageChange={setPage}
            />
        </main>
    )
};
