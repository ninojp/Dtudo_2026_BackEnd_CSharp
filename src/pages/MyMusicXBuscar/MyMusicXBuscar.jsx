import { useState } from 'react';
import styles from './MyMusicXBuscar.module.css';
import notaFireMusical from '/mymusicx/NotaMusica.png';
import axios from 'axios';
import InputPadrao from '../../components/InputPadrao/InputPadrao';
import FieldsetPadrao from '../../components/FieldsetPadrao/FieldsetPadrao';
import LabelPadrao from '../../components/LabelPadrao/LabelPadrao';
import ButtonPadrao from '../../components/ButtonPadrao/ButtonPadrao';
import CardRelease from '../../components/componentsMyMusicx/CardRelease/CardRelease';
import Spinner from '../../components/Spinner/Spinner';
import HeaderPage from '../../components/HeaderPage/HeaderPage';
import H1TituloPage from '../../components/H1TituloPage/H1TituloPage';
import H2SubTitulo from '../../components/H2SubTitulo/H2SubTitulo';

export default function MyMusicXBuscar() {
    const [artistQuery, setArtistQuery] = useState('');
    const [artistSuggestions, setArtistSuggestions] = useState([]);
    const [selectedArtist, setSelectedArtist] = useState(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);
    const [results, setResults] = useState(null);

    // Lê o token do Discogs a partir da variável de ambiente Vite
    const discogsToken = typeof import.meta !== 'undefined' ? import.meta.env.VITE_DISCOGS_TOKEN : undefined;

    const handleArtistSearch = async (q) => {
        if (!q.trim()) {
            setArtistSuggestions([]);
            return;
        }
        try {
            const resp = await axios.get('http://localhost:4000/api/discogs/artists', { params: { q } });
            setArtistSuggestions(resp.data.artists);
        } catch (err) {
            console.error('Erro ao buscar artistas:', err);
            setArtistSuggestions([]);
        }
    };

    const selectArtist = (artist) => {
        setSelectedArtist(artist);
        setArtistQuery(artist.title);
        setArtistSuggestions([]);
        handleSearch(artist);
    };

    const handleSearch = async (artist) => {
        setError(null);
        setResults(null);
        setIsLoading(true);
        try {
            const searchUrl = 'http://localhost:4000/api/discogs/search';
            const searchParams = {
                artistId: artist.id,
                artistName: artist.title,
            };
            const searchResp = await axios.get(searchUrl, { params: searchParams });
            console.log('Resposta busca releases:', searchResp.data);
            if (!searchResp.data.summary || searchResp.data.summary.Total === 0) {
                setError('Nenhum CD encontrado para este artista.');
                setResults(null);
                setIsLoading(false);
                return;
            }

            setResults(searchResp.data); // Use single state
        } catch (err) {
            console.error('Erro ao buscar discografia no Discogs: ', err);
            setError('Erro ao buscar no Discogs. Verifique o token ou a rede. ');
        } finally {
            setIsLoading(false);
        }
    };

    const handleSave = async (data) => {
        console.log('Salvando dados para:', data.artist);
        console.log('Data structure:', {
            artist: data.artist,
            summary: data.summary ? {
                Total: data.summary.Total,
                categories: data.summary.categories ? Object.keys(data.summary.categories) : null
            } : null
        });
        try {
            const saveUrl = 'http://localhost:4000/api/discogs/save';
            // Enviar artist e summary (nova estrutura)
            const payload = {
                artist: data.artist,
                summary: data.summary
            };
            console.log('Sending payload:', JSON.stringify(payload, null, 2).substring(0, 500));
            const response = await axios.post(saveUrl, payload);
            console.log('Save response:', response.data);
            alert('Dados salvos com sucesso!');
        } catch (err) {
            console.error('Erro ao salvar dados:', err);
            console.error('Error response:', err.response?.data);
            alert('Erro ao salvar dados.');
        }
    };

    // Helper function to render a category
    const renderCategory = (category, title) => {
        if (!results || !category || category.count === 0) return null;

        return (
            <div className={styles.divCategoryGroup}>
                <h3>{title} ({category.items.length})</h3>
                <div className={styles.divContainerCardsCds}>
                    {category.items.map((r, index) => (
                        <CardRelease
                            key={`${r.id}-${index}`}
                            cdTitulo={r.title}
                            cdArtist={r.artist}
                            cdImgSrc={r.thumb}
                            cdAno={r.year}
                        />
                    ))}
                </div>
            </div>
        );
    };

    // Calculate total displayed items
    let totalDisplayed = results?.summary?.Total || 0;

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
                                handleArtistSearch(e.target.value);
                            }}
                            itPlaceholder="Nome do artista (ex: Racionais)"
                        />
                        {artistSuggestions.length > 0 && (
                            <ul className={styles.listaSuspencaArtistas} >
                                {artistSuggestions.map((artist) => (
                                    <li key={artist.id} className={styles.liSuspencaArtistas} onClick={() => selectArtist(artist)}>
                                        {artist.title}
                                    </li>
                                ))}
                            </ul>
                        )}
                    </FieldsetPadrao>
                </form>
                {!discogsToken && (<p style={{ color: 'red' }}>
                    Atenção: nenhum token encontrado. Requisições sem autenticação são limitadas.
                </p>
                )}

                {isLoading && <Spinner />}
                {error && <p style={{ color: 'red' }}>{error}</p>}
                {!results && !selectedArtist && <img className={styles.imgPgMusicx} src={notaFireMusical} alt='Imagem nota musical em chamas' />}

                {results && (
                    <section className={styles.sectionResultadosCds}>
                        <div className={styles.divContainerBtnSalvar}>
                            <h4>Discografia de {results.artist} ({totalDisplayed} de {results.summary.Total} lançamentos exibidos)</h4>
                            {results.summary.categories && (
                                <div className={styles.divCategoriasResumo}>
                                    {/* Display original counts from backend */}
                                    {results.summary.categories.albums.count > 0 && (
                                        <span className={styles.spanCategoria}>{results.summary.categories.albums.count} - Álbuns | </span>
                                    )}
                                    {results.summary.categories.singlesEPs.count > 0 && (
                                        <span className={styles.spanCategoria}>{results.summary.categories.singlesEPs.count} - Singles & EPs | </span>
                                    )}
                                    {results.summary.categories.compilations.count > 0 && (
                                        <span className={styles.spanCategoria}>{results.summary.categories.compilations.count} - Compilações | </span>
                                    )}
                                    {results.summary.categories.videos.count > 0 && (
                                        <span className={styles.spanCategoria}>{results.summary.categories.videos.count} - Vídeos</span>
                                    )}
                                </div>
                            )}
                            <ButtonPadrao
                                styleExterno={styles.btnSalvarDados}
                                onClick={() => handleSave(results)}
                            >
                                Salvar Dados do Artista
                            </ButtonPadrao>
                        </div>

                        {/* Render using the helper function */}
                        {renderCategory(results.summary.categories.albums, 'Álbuns')}
                        {renderCategory(results.summary.categories.singlesEPs, 'Singles & EPs')}
                        {renderCategory(results.summary.categories.compilations, 'Compilações')}
                        {renderCategory(results.summary.categories.videos, 'Vídeos')}
                    </section>
                )}
            </main>
        </>
    );
};
