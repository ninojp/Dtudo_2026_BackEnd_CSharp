import { useContext } from 'react';
import CardsAnimexList from '../../components/componentsAnimex/CardsAnimexList/CardsAnimexList';
import H1TituloPage from '../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../components/H2SubTitulo/H2SubTitulo';
import HeaderPage from '../../components/HeaderPage/HeaderPage';
import AnimexObjsListContext from '../../context_api/AnimexObjsListContext/AnimexObjsListContext';
import styles from './Animex.module.css';

export default function Animex() {
    const { listObjsAnimex } = useContext(AnimexObjsListContext);
    return (
        <>
            <HeaderPage>
                <H1TituloPage>AnimeX</H1TituloPage>
                <H2SubTitulo>Minha coleção atualmente tem <span className={styles.spanTotalAnimes}>{listObjsAnimex.length}</span> animes Hentai.</H2SubTitulo>
            </HeaderPage>
            <CardsAnimexList />
        </>
    );
};
