import CardsMyAnimesList from '../../components/componentsMyAnimes/CardsMyAnimesList/CardsMyAnimesList';
import H1TituloPage from '../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../components/H2SubTitulo/H2SubTitulo';
import HeaderPage from '../../components/HeaderPage/HeaderPage';
import styles from './MyAnimes.module.css';
import { useContext } from 'react';
import MyAnimesObjsListContext from '../../context_api/MyAnimesObjsListContext/MyAnimesObjsListContext';

export default function MyAnimes() {
    const { listObjsMyAnimes } = useContext(MyAnimesObjsListContext);
    //=========================================================
    return (
        <>
            <HeaderPage>
                <H1TituloPage>MyAnimes</H1TituloPage>
                <H2SubTitulo>Lista de todas as <span className={styles.spanTotalAnimes}>{listObjsMyAnimes.length}</span> Animações que eu já assisti, Agrupadas por título.</H2SubTitulo>
            </HeaderPage>
            <CardsMyAnimesList />
        </>
    );
};
