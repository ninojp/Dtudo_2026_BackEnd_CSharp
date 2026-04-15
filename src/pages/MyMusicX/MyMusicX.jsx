import { useContext } from 'react';
import H1TituloPage from '../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../components/H2SubTitulo/H2SubTitulo';
import HeaderPage from '../../components/HeaderPage/HeaderPage';
import styles from './MyMusicX.module.css';
import MyMusicxObjsListContext from '../../context_api/MyMusicxObjsListContext/MyMusicxObjsListContext';
import { Link } from 'react-router-dom';
import CardsMyMusicxList from '../../components/componentsMyMusicx/CardsMyMusicxList/CardsMyMusicxList';

export default function MyMusicX() {
    const { listObjsMyMusicx } = useContext(MyMusicxObjsListContext);
    //=========================================================
    return (
        <>
            <HeaderPage>
                <H1TituloPage>MyMusicX</H1TituloPage>
                <H2SubTitulo><span className={styles.spanTotalAnimes}>{listObjsMyMusicx.length}</span> Artistas e seus 'Releases', agrupadas por Albuns, Singles & EPs, Compilações e Videos.</H2SubTitulo>
                <div>
                    Buscar Artista na API do DB Discogs - <Link to="/mymusicx/mymusicx-buscar" className={styles.linkBuscar}>MyMusicX-Buscar</Link>
                </div>
            </HeaderPage>
            <CardsMyMusicxList />
        </>
    );
};
