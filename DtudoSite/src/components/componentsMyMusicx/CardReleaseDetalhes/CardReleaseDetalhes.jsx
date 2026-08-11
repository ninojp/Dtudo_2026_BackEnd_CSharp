import { useEffect, useState } from "react";
import styles from './CardReleaseDetalhes.module.css';
import Spinner from "../../Spinner/Spinner";
import ButtonPadrao from "../../ButtonPadrao/ButtonPadrao";
import {
    getApiMusicXErrorMessage,
    getMusicRelease,
} from "../../../services/apiMusicX";

export default function CardReleaseDetalhes({ releaseId, id }) {
    const [releaseDetails, setReleaseDetails] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);
    const [retryNumber, setRetryNumber] = useState(0);
    const selectedReleaseId = releaseId ?? id;

    useEffect(() => {
        if (!selectedReleaseId) {
            setReleaseDetails(null);
            return;
        }

        const controller = new AbortController();
        const fetchReleaseDetails = async () => {
            setIsLoading(true);
            setError(null);
            
            try {
                const data = await getMusicRelease(selectedReleaseId, { signal: controller.signal });
                if (!controller.signal.aborted) {
                    setReleaseDetails(data);
                }
            } catch (requestError) {
                if (requestError.name === 'AbortError') return;
                console.error('Erro ao buscar detalhes do release na ApiMusicX:', requestError);
                if (!controller.signal.aborted) {
                    setReleaseDetails(null);
                    setError(requestError);
                }
            } finally {
                if (!controller.signal.aborted) {
                    setIsLoading(false);
                }
            }
        };

        fetchReleaseDetails();
        return () => controller.abort();
    }, [selectedReleaseId, retryNumber]);

    if (isLoading) {
        return <Spinner />;
    }

    if (error) {
        return (
            <div className={styles.errorContainer}>
                <p>{getApiMusicXErrorMessage(error)}</p>
                <ButtonPadrao onClick={() => setRetryNumber(current => current + 1)}>Tentar novamente</ButtonPadrao>
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
                        src="/mymusicx/NotaMusica.png"
                        alt={releaseDetails.title}
                        className={styles.coverImage}
                    />
                </figure>
                <div className={styles.infoSection}>
                    <h3 className={styles.title}>{releaseDetails.title}</h3>
                    {releaseDetails.releaseYear && (
                        <p className={styles.year}>Ano: {releaseDetails.releaseYear}</p>
                    )}
                    {releaseDetails.artists?.length > 0 && (
                        <p className={styles.genres}>
                            Artistas: {releaseDetails.artists.map(artist => artist.displayName).join(', ')}
                        </p>
                    )}
                    {releaseDetails.notes && (
                        <p className={styles.styles}>
                            Observações: {releaseDetails.notes}
                        </p>
                    )}
                </div>
            </div>

            {releaseDetails.tracks?.length > 0 && (
                <div className={styles.tracklistSection}>
                    <h4 className={styles.tracklistTitle}>Faixas:</h4>
                    <ol className={styles.tracklist}>
                        {releaseDetails.tracks.map((track, index) => (
                            <li key={track.musicTrackId} className={styles.track}>
                                <span className={styles.trackPosition}>
                                    {track.positionLabel || track.sequence || index + 1}.
                                </span>
                                <span className={styles.trackTitle}>
                                    {track.title}
                                </span>
                                {(track.durationText || track.durationSeconds) && (
                                    <span className={styles.trackDuration}>
                                        {track.durationText || formatDuration(track.durationSeconds)}
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

function formatDuration(durationSeconds) {
    const seconds = Number(durationSeconds);
    if (!Number.isFinite(seconds) || seconds < 0) return '';

    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = String(seconds % 60).padStart(2, '0');
    return `${minutes}:${remainingSeconds}`;
}
