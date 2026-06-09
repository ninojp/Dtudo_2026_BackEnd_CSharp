
import { useState, useEffect } from "react";
import styles from './CardReleaseDetalhes.module.css';
import Spinner from "../../Spinner/Spinner";

export default function CardReleaseDetalhes({ id }) {
    const [releaseDetails, setReleaseDetails] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);

    useEffect(() => {
        if (!id) {
            setReleaseDetails(null);
            return;
        }

        const fetchReleaseDetails = async () => {
            setIsLoading(true);
            setError(null);
            
            try {
                const response = await fetch(`http://localhost:4000/api/discogs/release/${id}`);
                
                if (!response.ok) {
                    throw new Error('Erro ao buscar detalhes do release');
                }
                
                const data = await response.json();
                setReleaseDetails(data);
            } catch (err) {
                console.error('Erro ao buscar detalhes:', err);
                setError(err.message);
            } finally {
                setIsLoading(false);
            }
        };

        fetchReleaseDetails();
    }, [id]);

    if (isLoading) {
        return <Spinner />;
    }

    if (error) {
        return (
            <div className={styles.errorContainer}>
                <p>Erro ao carregar detalhes: {error}</p>
            </div>
        );
    }

    if (!releaseDetails) {
        return (
            <div className={styles.emptyContainer}>
                <p>Selecione um release para ver os detalhes</p>
            </div>
        );
    }

    return (
        <article className={styles.cardDetalhes}>
            <div className={styles.headerSection}>
                <figure className={styles.coverContainer}>
                    <img 
                        src={releaseDetails.images?.[0]?.uri || '/mymusicx/NotaMusica.png'} 
                        alt={releaseDetails.title}
                        className={styles.coverImage}
                    />
                </figure>
                <div className={styles.infoSection}>
                    <h3 className={styles.title}>{releaseDetails.title}</h3>
                    {releaseDetails.year && (
                        <p className={styles.year}>Ano: {releaseDetails.year}</p>
                    )}
                    {releaseDetails.genres?.length > 0 && (
                        <p className={styles.genres}>
                            Gênero: {releaseDetails.genres.join(', ')}
                        </p>
                    )}
                    {releaseDetails.styles?.length > 0 && (
                        <p className={styles.styles}>
                            Estilo: {releaseDetails.styles.join(', ')}
                        </p>
                    )}
                    {releaseDetails.formats?.length > 0 && (
                        <p className={styles.format}>
                            Formato: {releaseDetails.formats.map(f => 
                                `${f.name}${f.descriptions ? ` (${f.descriptions.join(', ')})` : ''}`
                            ).join(', ')}
                        </p>
                    )}
                </div>
            </div>

            {releaseDetails.tracklist?.length > 0 && (
                <div className={styles.tracklistSection}>
                    <h4 className={styles.tracklistTitle}>Faixas:</h4>
                    <ol className={styles.tracklist}>
                        {releaseDetails.tracklist.map((track, index) => (
                            <li key={index} className={styles.track}>
                                <span className={styles.trackPosition}>
                                    {track.position || index + 1}.
                                </span>
                                <span className={styles.trackTitle}>
                                    {track.title}
                                </span>
                                {track.duration && (
                                    <span className={styles.trackDuration}>
                                        {track.duration}
                                    </span>
                                )}
                            </li>
                        ))}
                    </ol>
                </div>
            )}
        </article>
    );
};
