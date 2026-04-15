import { useEffect, useState } from "react";
import conectaAPIJikan from "../../api_conect/conectAPIJikam";
import styles from "./CardAnimeDetalhesApiJikan.module.css";
import Spinner from "../Spinner/Spinner";

export default function CardAnimeDetalhesApiJikan({ id_mal }) {
    const [detalhesAnime, setDetalhesAnime] = useState(null);
    //-------------------------------------------------------
    useEffect(() => {
        async function fetchDetails() {
            const detalhes = await conectaAPIJikan(id_mal);
            setDetalhesAnime(detalhes);
        }
        fetchDetails();
    }, [id_mal]);
    if (!detalhesAnime) {
        return (<Spinner />);
    }
    return (
        <article className={styles.articleCardAnime} key={String(detalhesAnime.mal_id)}>
            <div className={styles.divTitulosAnime}>
                <h3>{detalhesAnime.title}</h3>
            </div>
            <div className={styles.divContainerImgEDetalhes}>
                <div className={styles.divImgEDetalhes}>
                    <img src={detalhesAnime?.images.jpg.image_url} alt={`Capa de ${detalhesAnime?.title}`} />
                </div>
                <div className={styles.divInfoDetalhes}>
                    {detalhesAnime?.title_english && (<p><strong>Inglês:</strong> {detalhesAnime?.title_english}</p>)}
                    <p><strong>Japonês:</strong> {detalhesAnime?.title_japanese}</p>
                    {detalhesAnime?.title_synonyms.length > 0 && (<p><strong>Sinônimos:</strong> {detalhesAnime?.title_synonyms.join(', ')}</p>)}
                    <p><strong>Tipo:</strong> {detalhesAnime?.type}</p>
                    <p><strong>Episódios:</strong> {detalhesAnime?.episodes}</p>
                    <p><strong>Duração:</strong> {detalhesAnime?.duration}</p>
                    <p><strong>Status:</strong> {detalhesAnime?.status}</p>
                    <p><strong>Data:</strong> {detalhesAnime?.aired.string}</p>
                    <p><strong>Score:</strong> {detalhesAnime?.score}</p>
                    <p><strong>Gêneros:</strong> {detalhesAnime?.genres.map(genre => genre.name).join(', ')}</p>
                    {detalhesAnime?.themes.length > 0 && (<p><strong>Temas:</strong> {detalhesAnime?.themes.map(theme => theme.name).join(', ')}</p>)}
                </div>
            </div>
            <p className={styles.pSynopisDetalhes}>{detalhesAnime?.synopsis}</p>
        </article>
    );
};
