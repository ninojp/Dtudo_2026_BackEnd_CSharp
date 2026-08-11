import MyMusicXDetalhesContext from "../../../context_api/MyMusicXDetalhesContext/MyMusicXDetalhesContext";
import { use } from "react";
import styles from './MyMusicXDetalhes.module.css';
import Spinner from "../../../components/Spinner/Spinner";
import CardRelease from "../../../components/componentsMyMusicx/CardRelease/CardRelease";
import CardReleaseDetalhes from "../../../components/componentsMyMusicx/CardReleaseDetalhes/CardReleaseDetalhes";
import ButtonPadrao from "../../../components/ButtonPadrao/ButtonPadrao";

function getReleaseCategory(releaseType) {
    const normalizedType = typeof releaseType === 'string'
        ? releaseType.toLowerCase()
        : releaseType;

    if (normalizedType === 1 || normalizedType === 'album') return 'Álbuns';
    if (normalizedType === 2 || normalizedType === 'single') return 'Singles & EPs';
    if (normalizedType === 3 || normalizedType === 'ep') return 'Singles & EPs';
    if (normalizedType === 4 || normalizedType === 'compilation') return 'Compilações';
    if (normalizedType === 5 || normalizedType === 'video') return 'Vídeos';
    return 'Outros releases';
}

export default function MyMusicXDetalhes() {
    const {
        myMusicXDetalhes,
        isLoading,
        currentDisplayId,
        setCurrentDisplayId,
        errorMessage,
        fetchCollectionDetails,
    } = use(MyMusicXDetalhesContext);

    if (isLoading) {
        return <Spinner />;
    }

    if (errorMessage) {
        return (
            <main className={styles.mainContainerMyMusicXDetalhes}>
                <div className={styles.apiStateContainer} role="alert">
                    <p>{errorMessage}</p>
                    <ButtonPadrao onClick={() => fetchCollectionDetails()}>Tentar novamente</ButtonPadrao>
                </div>
            </main>
        );
    }

    if (!myMusicXDetalhes) {
        return (
            <main className={styles.mainContainerMyMusicXDetalhes}>
                <div className={styles.apiStateContainer}>
                    <p>A Coleção local não está disponível.</p>
                </div>
            </main>
        );
    }

    const releases = myMusicXDetalhes.releases || [];
    const categories = ['Álbuns', 'Singles & EPs', 'Compilações', 'Vídeos', 'Outros releases'];

    const renderReleaseCategory = (releases, categoryTitle) => {
        if (!releases || releases.length === 0) return null;

        return (
            <>
                <h3 className={styles.h3CategoriaTitulo}>{categoryTitle}</h3>
                <div className={styles.divContainerListaCardsMyMusicx}>
                    {releases.map((item) => {
                        return (
                            <div
                                key={item.musicReleaseId}
                                onClick={() => setCurrentDisplayId(item.musicReleaseId)}
                                style={{ cursor: 'pointer' }}
                            >
                                <CardRelease
                                    cdTitulo={item.title}
                                    cdAno={item.releaseYear || ''}
                                />
                            </div>
                        );
                    })}
                </div>
            </>
        );
    };

    const renderLocalFiles = (releases, categoryTitle) => {
        if (!releases || releases.length === 0) return null;

        const itemsWithFiles = releases
            .map(release => ({
                release,
                files: [
                    ...(release.localFileReferences || []),
                    ...(release.tracks || []).flatMap(track => track.localFileReferences || []),
                ],
            }))
            .filter(item => item.files.length > 0);
        if (itemsWithFiles.length === 0) return null;

        return (
            <>
                <h3 className={styles.h3CategoriaTitulo}>{categoryTitle}</h3>
                {itemsWithFiles.map(({ release, files }) => (
                    <div className={styles.divContainerSubsMyAnimes} key={release.musicReleaseId}>
                        <h4>{release.title}</h4>
                        <ul>
                            {files.map((file) => (
                                <li className={styles.liSubpastasMyAnimes} key={file.musicLocalFileReferenceId}>
                                    {file.relativePath}
                                </li>
                            ))}
                        </ul>
                    </div>
                ))}
            </>
        );
    };

    return (
        <main className={styles.mainContainerMyMusicXDetalhes}>
            <section className={styles.sectionMyMusicXDetalhes}>
                <div className={styles.divTituloEMiniCards}>
                    <h2>{myMusicXDetalhes.displayName}</h2>
                    <p>{(myMusicXDetalhes.artists || []).map(artist => artist.displayName).join(', ')}</p>
                    {categories.map(category => renderReleaseCategory(
                        releases.filter(release => getReleaseCategory(release.releaseType) === category),
                        category,
                    ))}
                </div>
            </section>
            <div className={styles.divContainerReleaseDetalhes}>
                {currentDisplayId ? (<CardReleaseDetalhes releaseId={currentDisplayId} />)
                    : (<p>Selecione um Release da coleção para ver os detalhes.</p>)}
            </div>
            <h3 className={styles.h3TituloDescricao}>Coleção Local</h3>
            <section className={styles.sectionMyMusicXLocais}>
                {categories.map(category => renderLocalFiles(
                    releases.filter(release => getReleaseCategory(release.releaseType) === category),
                    category,
                ))}
            </section>
        </main>
    )
};
