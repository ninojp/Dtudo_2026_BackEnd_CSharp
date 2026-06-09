import MyMusicXDetalhesContext from "../../context_api/MyMusicXDetalhesContext/MyMusicXDetalhesContext";
import { use, useEffect } from "react"
import { useNavigate, Link } from "react-router-dom";
import styles from './MyMusicXDetalhes.module.css';
import Spinner from "../../components/Spinner/Spinner";
import CardRelease from "../../components/componentsMyMusicx/CardRelease/CardRelease";
import CardReleaseDetalhes from "../../components/componentsMyMusicx/CardReleaseDetalhes/CardReleaseDetalhes";


export default function MyMusicXDetalhes() {
    const { myMusicXDetalhes, isLoading, currentDisplayId, setCurrentDisplayId } = use(MyMusicXDetalhesContext);
    const navegaPara = useNavigate();
    //-------------------------------
    useEffect(() => {
        // Só navega se o carregamento terminou (isLoading é false) E o artista não foi encontrado
        if (!isLoading && !myMusicXDetalhes) {
            navegaPara('/not-found');
        }
    }, [isLoading, myMusicXDetalhes, navegaPara]);
    // Se estiver carregando, exibe o Spinner
    if (isLoading) {
        return <Spinner />;
    }
    // Se o carregamento terminou e o item não existe, retorna null para não tentar renderizar o 
    // resto do componente enquanto a navegação do useEffect acontece.
    if (!myMusicXDetalhes) {
        return null;
    }
    // Função para renderizar uma categoria de releases
    const renderReleaseCategory = (releases, categoryTitle) => {
        if (!releases || releases.length === 0) return null;

        return (
            <>
                <h3 className={styles.h3CategoriaTitulo}>{categoryTitle}</h3>
                <div className={styles.divContainerListaCardsMyMusicx}>
                    {releases.map((item, index) => {
                        // Usar master_release_id se disponível, senão usar discogs_id
                        const discogsId = item.master_release_id || item.discogs_id;
                        if (!discogsId) return null;

                        const imgSrc = discogsId && discogsId.trim() !== ''
                            ? `/mymusicx/releases/${discogsId}.jpg`
                            : '/mymusicx/NotaMusica.png';

                        return (
                            <div
                                key={index}
                                onClick={() => setCurrentDisplayId(discogsId)}
                                style={{ cursor: 'pointer' }}
                            >
                                <CardRelease
                                    cdTitulo={item.titulo}
                                    cdImgSrc={imgSrc}
                                    cdAno={item.ano || ''}
                                />
                            </div>
                        );
                    })}
                </div>
            </>
        );
    };

    // Função para renderizar arquivos locais de uma categoria
    const renderLocalFiles = (releases, categoryTitle) => {
        if (!releases || releases.length === 0) return null;

        const itemsWithFiles = releases.filter(item => item.arquivosLocais?.length > 0);
        if (itemsWithFiles.length === 0) return null;

        return (
            <>
                <h3 className={styles.h3CategoriaTitulo}>{categoryTitle}</h3>
                {itemsWithFiles.map((item, index) => (
                    <div className={styles.divContainerSubsMyAnimes} key={index}>
                        <h4>{item.titulo}</h4>
                        <ul>
                            {item.arquivosLocais.map((arquivo, idx) => (
                                <li className={styles.liSubpastasMyAnimes} key={idx}>{arquivo}</li>
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
                    <h2>{myMusicXDetalhes.artista}</h2>
                    {renderReleaseCategory(myMusicXDetalhes.releases?.albums, 'Álbuns')}
                    {renderReleaseCategory(myMusicXDetalhes.releases?.['singles-EP'], 'Singles & EPs')}
                    {renderReleaseCategory(myMusicXDetalhes.releases?.compilations, 'Compilações')}
                    {renderReleaseCategory(myMusicXDetalhes.releases?.videos, 'Vídeos')}
                </div>
            </section>
            <div className={styles.divContainerReleaseDetalhes}>
                {currentDisplayId ? (<CardReleaseDetalhes id={currentDisplayId} />)
                    : (<p>Selecione um Release da coleção para ver os detalhes.</p>)}
            </div>
            <h3 className={styles.h3TituloDescricao}>Coleção Local</h3>
            <section className={styles.sectionMyMusicXLocais}>
                {renderLocalFiles(myMusicXDetalhes.releases?.albums, 'Álbuns')}
                {renderLocalFiles(myMusicXDetalhes.releases?.['singles-EP'], 'Singles & EPs')}
                {renderLocalFiles(myMusicXDetalhes.releases?.compilations, 'Compilações')}
                {renderLocalFiles(myMusicXDetalhes.releases?.videos, 'Vídeos')}
            </section>
        </main>
    )
};
