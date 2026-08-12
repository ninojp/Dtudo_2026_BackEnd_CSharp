import { useEffect, useRef, useState } from 'react';
import styles from './MyMusicXBuscar.module.css';
import notaFireMusical from '/mymusicx/NotaMusica.png';
import InputPadrao from '../../../components/InputPadrao/InputPadrao';
import FieldsetPadrao from '../../../components/FieldsetPadrao/FieldsetPadrao';
import LabelPadrao from '../../../components/LabelPadrao/LabelPadrao';
import ButtonPadrao from '../../../components/ButtonPadrao/ButtonPadrao';
import CardRelease from '../../../components/componentsMyMusicx/CardRelease/CardRelease';
import Spinner from '../../../components/Spinner/Spinner';
import HeaderPage from '../../../components/HeaderPage/HeaderPage';
import H1TituloPage from '../../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../../components/H2SubTitulo/H2SubTitulo';
import {
    getApiDiscogsErrorMessage,
    getDiscogsImageUrl,
    getDiscogsMaster,
    getDiscogsRelease,
    listDiscogsArtistReleases,
    searchDiscogsArtists,
} from '../../../services/apiDiscogs';

export default function MyMusicXBuscar() {
    const [artistQuery, setArtistQuery] = useState('');
    const [artistSuggestions, setArtistSuggestions] = useState([]);
    const [selectedArtist, setSelectedArtist] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);
    const [results, setResults] = useState(null);
    const [isSearchingArtists, setIsSearchingArtists] = useState(false);
    const [artistSearchError, setArtistSearchError] = useState(null);
    const [artistSearchRetry, setArtistSearchRetry] = useState(0);
    const [releasePage, setReleasePage] = useState(1);
    const [selectedRelease, setSelectedRelease] = useState(null);
    const [releaseDetails, setReleaseDetails] = useState(null);
    const [isLoadingReleaseDetails, setIsLoadingReleaseDetails] = useState(false);
    const [releaseDetailsError, setReleaseDetailsError] = useState(null);
    const artistSearchAbortRef = useRef(null);
    const releasesAbortRef = useRef(null);
    const releaseDetailsAbortRef = useRef(null);

    useEffect(() => {
        const query = artistQuery.trim();
        artistSearchAbortRef.current?.abort();

        if (query.length < 2 || selectedArtist) {
            setArtistSuggestions([]);
            setIsSearchingArtists(false);
            return undefined;
        }

        const controller = new AbortController();
        artistSearchAbortRef.current = controller;
        const timeoutId = setTimeout(async () => {
            setIsSearchingArtists(true);
            setArtistSearchError(null);

            try {
                const response = await searchDiscogsArtists({
                    query,
                    page: 1,
                    perPage: 10,
                    signal: controller.signal,
                });
                if (!controller.signal.aborted) {
                    setArtistSuggestions(response.items);
                }
            } catch (requestError) {
                if (requestError.name !== 'AbortError' && !controller.signal.aborted) {
                    console.error('Erro ao buscar artistas externos:', requestError);
                    setArtistSuggestions([]);
                    setArtistSearchError(requestError);
                }
            } finally {
                if (!controller.signal.aborted) {
                    setIsSearchingArtists(false);
                }
            }
        }, 250);

        return () => {
            clearTimeout(timeoutId);
            controller.abort();
        };
    }, [artistQuery, selectedArtist, artistSearchRetry]);

    useEffect(() => () => {
        artistSearchAbortRef.current?.abort();
        releasesAbortRef.current?.abort();
        releaseDetailsAbortRef.current?.abort();
    }, []);

    const handleSearch = async (artist, page = 1) => {
        const artistId = artist?.source?.id;
        if (!artistId) {
            setError(new Error('O resultado externo nao possui um ID de artista valido.'));
            return;
        }

        releasesAbortRef.current?.abort();
        const controller = new AbortController();
        releasesAbortRef.current = controller;
        setError(null);
        setResults(null);
        setReleasePage(page);
        setSelectedRelease(null);
        setReleaseDetails(null);
        setReleaseDetailsError(null);
        setIsLoading(true);

        try {
            const response = await listDiscogsArtistReleases(artistId, {
                page,
                perPage: 50,
                expand: 'none',
                signal: controller.signal,
            });
            if (!controller.signal.aborted) {
                setResults(response);
            }
        } catch (requestError) {
            if (requestError.name !== 'AbortError' && !controller.signal.aborted) {
                console.error('Erro ao buscar discografia externa:', requestError);
                setError(requestError);
            }
        } finally {
            if (!controller.signal.aborted) {
                setIsLoading(false);
            }
        }
    };

    const selectArtist = (artist) => {
        setSelectedArtist(artist);
        setArtistQuery(artist.name);
        setArtistSuggestions([]);
        handleSearch(artist, 1);
    };

    const handleReleaseSelect = async (release) => {
        const releaseId = release?.source?.id;
        if (!releaseId) {
            setReleaseDetailsError(new Error('O resultado externo nao possui um ID de release valido.'));
            return;
        }

        releaseDetailsAbortRef.current?.abort();
        const controller = new AbortController();
        releaseDetailsAbortRef.current = controller;
        setSelectedRelease(release);
        setReleaseDetails(null);
        setReleaseDetailsError(null);
        setIsLoadingReleaseDetails(true);

        try {
            const resourceType = String(release.source?.resourceType || '').toLowerCase();
            const response = resourceType === 'master'
                ? await getDiscogsMaster(releaseId, { signal: controller.signal })
                : await getDiscogsRelease(releaseId, { signal: controller.signal });
            if (!controller.signal.aborted) {
                setReleaseDetails(response);
            }
        } catch (requestError) {
            if (requestError.name !== 'AbortError' && !controller.signal.aborted) {
                console.error('Erro ao buscar detalhes externos:', requestError);
                setReleaseDetailsError(requestError);
            }
        } finally {
            if (!controller.signal.aborted) {
                setIsLoadingReleaseDetails(false);
            }
        }
    };

    const renderCategory = (categoryKey, title) => {
        const categoryItems = (results?.items || []).filter((release) => {
            const normalizedCategory = String(release.category || 'unknown')
                .replace(/[^a-z]/gi, '')
                .toLowerCase();
            return normalizedCategory === categoryKey;
        });
        if (categoryItems.length === 0) return null;

        return (
            <div className={styles.divCategoryGroup}>
                <h3>{title} ({categoryItems.length})</h3>
                <div className={styles.divContainerCardsCds}>
                    {categoryItems.map((release, index) => (
                        <CardRelease
                            key={`${release.canonicalId || release.source?.id}-${index}`}
                            cdTitulo={release.title}
                            cdImgSrc={getDiscogsImageUrl(release)}
                            cdAno={release.year || ''}
                            onClick={() => handleReleaseSelect(release)}
                        />
                    ))}
                </div>
            </div>
        );
    };

    const totalDisplayed = results?.items?.length || 0;
    const totalAvailable = results?.pagination?.totalItems ?? totalDisplayed;
    const selectedReleaseImage = getDiscogsImageUrl(releaseDetails || selectedRelease);

    //=========================================================
    return (
        <>
            <HeaderPage>
                <H1TituloPage>MyMusicX</H1TituloPage>
                <H2SubTitulo>Buscando por artista na API do DB Discogs</H2SubTitulo>
            </HeaderPage>
            <main className={styles.mainContainerPgMusicx}>
                <form className={styles.formBuscarCds} onSubmit={(e) => e.preventDefault()}>
                    <FieldsetPadrao>
                        <LabelPadrao htmlFor='inputArtist'>Buscar por Artista</LabelPadrao>
                        <InputPadrao
                            itId='inputArtist'
                            itTipo="search"
                            itValue={artistQuery}
                            itOnChange={(e) => {
                                setArtistQuery(e.target.value);
                                setSelectedArtist(null);
                                setArtistSearchError(null);
                            }}
                            itPlaceholder="Nome do artista (ex: Racionais)"
                        />
                        {artistSuggestions.length > 0 && (
                            <ul className={styles.listaSuspencaArtistas} >
                                {artistSuggestions.map((artist) => (
                                    <li key={artist.source?.id} className={styles.liSuspencaArtistas} onClick={() => selectArtist(artist)}>
                                        {artist.name}
                                    </li>
                                ))}
                            </ul>
                        )}
                        {isSearchingArtists && <Spinner />}
                        {artistSearchError && (
                            <div role="alert">
                                <p>{getApiDiscogsErrorMessage(artistSearchError)}</p>
                                <ButtonPadrao onClick={() => setArtistSearchRetry(current => current + 1)}>
                                    Tentar novamente
                                </ButtonPadrao>
                            </div>
                        )}
                        {!isSearchingArtists
                            && !artistSearchError
                            && !selectedArtist
                            && artistQuery.trim().length >= 2
                            && artistSuggestions.length === 0
                            && <p>Nenhum artista externo foi encontrado.</p>}
                    </FieldsetPadrao>
                </form>
                {isLoading && <Spinner />}
                {error && (
                    <div role="alert">
                        <p>{getApiDiscogsErrorMessage(error)}</p>
                        {selectedArtist && (
                            <ButtonPadrao onClick={() => handleSearch(selectedArtist, releasePage)}>
                                Tentar novamente
                            </ButtonPadrao>
                        )}
                    </div>
                )}
                {!results && !selectedArtist && <img className={styles.imgPgMusicx} src={notaFireMusical} alt='Imagem nota musical em chamas' />}

                {results && (
                    <section className={styles.sectionResultadosCds}>
                        <div className={styles.divContainerBtnSalvar}>
                            <h4>Discografia de {results.artist?.name || selectedArtist?.name} ({totalDisplayed} de {totalAvailable} lançamentos exibidos)</h4>
                            {!results.isComplete && <p role="status">A resposta externa está incompleta. {results.warnings?.join(' ')}</p>}
                        </div>

                        {renderCategory('album', 'Álbuns')}
                        {renderCategory('singleep', 'Singles & EPs')}
                        {renderCategory('compilation', 'Compilações')}
                        {renderCategory('video', 'Vídeos')}
                        {renderCategory('unknown', 'Outros releases')}
                        {totalDisplayed === 0 && <p>Nenhum release externo foi encontrado para este artista.</p>}
                        {(results.pagination?.page > 1 || results.pagination?.hasNextPage) && (
                            <div>
                                {results.pagination.page > 1 && (
                                    <ButtonPadrao onClick={() => handleSearch(selectedArtist, results.pagination.page - 1)}>
                                        Página anterior
                                    </ButtonPadrao>
                                )}
                                {results.pagination.hasNextPage && (
                                    <ButtonPadrao onClick={() => handleSearch(selectedArtist, results.pagination.page + 1)}>
                                        Próxima página
                                    </ButtonPadrao>
                                )}
                            </div>
                        )}

                        {selectedRelease && (
                            <section className={styles.sectionDetalheExterno}>
                                <h3>Detalhes de {selectedRelease.title}</h3>
                                {isLoadingReleaseDetails && <Spinner />}
                                {releaseDetailsError && (
                                    <div role="alert">
                                        <p>{getApiDiscogsErrorMessage(releaseDetailsError)}</p>
                                        <ButtonPadrao onClick={() => handleReleaseSelect(selectedRelease)}>
                                            Tentar novamente
                                        </ButtonPadrao>
                                    </div>
                                )}
                                {!isLoadingReleaseDetails && !releaseDetailsError && !releaseDetails && (
                                    <p>Os detalhes deste release ainda não estão disponíveis.</p>
                                )}
                                {releaseDetails && (
                                    <article className={styles.articleDetalheExterno}>
                                        <img
                                            src={selectedReleaseImage || notaFireMusical}
                                            alt={releaseDetails.title}
                                            className={styles.imgDetalheExterno}
                                        />
                                        <div>
                                            <h4>{releaseDetails.title}</h4>
                                            {releaseDetails.year && <p>Ano: {releaseDetails.year}</p>}
                                            {releaseDetails.artists?.length > 0 && (
                                                <p>Artistas: {releaseDetails.artists.map(artist => artist.name).join(', ')}</p>
                                            )}
                                            {releaseDetails.formats?.length > 0 && (
                                                <p>Formatos: {releaseDetails.formats.join(', ')}</p>
                                            )}
                                            {releaseDetails.tracklist?.length > 0 && (
                                                <ol>
                                                    {releaseDetails.tracklist.map((track, index) => (
                                                        <li key={`${track.position || index}-${track.title}`}>
                                                            {track.title}{track.durationText ? ` (${track.durationText})` : ''}
                                                        </li>
                                                    ))}
                                                </ol>
                                            )}
                                        </div>
                                    </article>
                                )}
                            </section>
                        )}
                    </section>
                )}
            </main>
        </>
    );
};
