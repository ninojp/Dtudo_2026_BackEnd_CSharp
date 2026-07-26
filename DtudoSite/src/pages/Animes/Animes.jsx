import CardsAnimesList from '../../components/componentsAnimes/CardsAnimesList/CardsAnimesList';
import H1TituloPage from '../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../components/H2SubTitulo/H2SubTitulo';
import HeaderPage from '../../components/HeaderPage/HeaderPage';
import styles from './Animes.module.css';
import { useContext, useMemo } from 'react';
import AnimesObjsListDetalhesContext from '../../context_api/AnimesDetalhesObjsListContext/AnimesDetalhesObjsListContext';
import { ehAnimeAdulto } from '../../utils/animeContentUtils';


export default function Animes() {
  const { listObjsDetalhesAnimes } = useContext(AnimesObjsListDetalhesContext);
  const totalAnimesExibiveis = useMemo(
    () => listObjsDetalhesAnimes.filter((anime) => !ehAnimeAdulto(anime)).length,
    [listObjsDetalhesAnimes]
  );

  return (
    <>
      <HeaderPage>
        <H1TituloPage>Animes</H1TituloPage>
        <H2SubTitulo>Lista completa de todos os <span className={styles.spanTotalAnimes}>{totalAnimesExibiveis}</span> Animes que tenho registrado.</H2SubTitulo>
        </HeaderPage>
      <CardsAnimesList />
    </>
  );
};
