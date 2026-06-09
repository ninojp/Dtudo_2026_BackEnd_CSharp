import { useEffect, useContext } from "react";
import { useNavigate } from "react-router-dom";
import Spinner from "../../components/Spinner/Spinner";
import styles from './MyAnimesDetalhes.module.css';
import MyAnimesDetalhesContext from "../../context_api/MyAnimesDetalhesContext/MyAnimesDetalhesContext";
import CardMyAnimesMini from "../../components/componentsMyAnimes/CardMyAnimesMini/CardMyAnimesMini";
import CardAnimeDetalhesApiJikan from "../../components/CardAnimeDetalhesApiJikan/CardAnimeDetalhesApiJikan";

export default function MyAnimesDetalhes() {
    const { isLoading, myAnimesDetalhes, currentDisplayId } = useContext(MyAnimesDetalhesContext);
    const navegaPara = useNavigate();
    //-------------------------------
    useEffect(() => {
        // Só navega se o carregamento terminou (isLoading é false) E o anime não foi encontrado
        if (!isLoading && !myAnimesDetalhes) {
            navegaPara('/not-found');
        }
    }, [isLoading, myAnimesDetalhes, navegaPara]);
    // Se estiver carregando, exibe o Spinner
    if (isLoading) {
        return <Spinner />;
    }
    // Se o carregamento terminou e o item não existe, retorna null para não tentar renderizar o resto do componente enquanto a navegação do useEffect acontece.
    if (!myAnimesDetalhes) {
        return null;
    }
    //===============================================
    return (
        <main className={styles.mainContainerMyAnimesDetalhes}>
            <section className={styles.sectionMyAnimesDetalhes}>
                <div className={styles.divTituloEMiniCards}>
                    {myAnimesDetalhes.subpastas.length >= 2 && <h2>{myAnimesDetalhes.nome}</h2>}
                    {myAnimesDetalhes.subpastas.length >= 2 && <CardMyAnimesMini />}
                </div>
                <div className={styles.divContainerDetalhesCarrossel}>
                    {currentDisplayId ? (<CardAnimeDetalhesApiJikan id_mal={currentDisplayId} />)
                        : (<p>Selecione um anime da coleção para ver os detalhes.</p>)}
                </div>
            </section>
            <h3 className={styles.h3TituloDescricao}>Coleção Local</h3>
            <section className={styles.sectionMyAnimesLocais}>
                {myAnimesDetalhes.subpastas.map((subpasta) => (
                    <div className={styles.divContainerSubsMyAnimes} key={subpasta.nome}>
                        <h3>{subpasta.nome} - ID: {subpasta.mal_id}</h3>
                        <ul>
                            {subpasta.arquivos.map((arquivo) => (
                                <li className={styles.liSubpastasMyAnimes} key={arquivo.nome}>{arquivo.nome}</li>
                            ))}
                        </ul>
                    </div>
                ))}
            </section>
        </main>
    );
};
