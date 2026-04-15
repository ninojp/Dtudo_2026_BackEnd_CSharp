import { useEffect, useContext } from "react";
import { useNavigate } from "react-router-dom";
import styles from "./animexDetalhes.module.css";
import AnimexDetalhesContext from "../../context_api/AnimexDetalhesContext/AnimexDetalhesContext";
import Spinner from "../../components/Spinner/Spinner";
import CardAnimexMini from "../../components/componentsAnimex/CardAnimexMini/CardAnimexMini";
import CardAnimeDetalhesApiJikan from "../../components/CardAnimeDetalhesApiJikan/CardAnimeDetalhesApiJikan";

function AnimexDetalhesContent() {
    const { isLoading, animexDetalhes, currentDisplayId } = useContext(AnimexDetalhesContext);
    const navegaPara = useNavigate();
    //-------------------------------
    useEffect(() => {
        // Só navega se o carregamento terminou (isLoading é false) E o anime não foi encontrado (animexDetalhes é undefined ou null)
        if (!isLoading && !animexDetalhes) {
            navegaPara('/not-found');
        }
    }, [isLoading, animexDetalhes, navegaPara]);
    // Se estiver carregando, exibe o Spinner
    if (isLoading) {
        return <Spinner />;
    }
    // Se o carregamento terminou e o item não existe, retorna null para não tentar renderizar o resto do componente enquanto a navegação do useEffect acontece.
    if (!animexDetalhes) {
        return null;
    }
    //===============================================
    return (
        <main className={styles.mainContainerAnimex}>
            <h2>{animexDetalhes.nome}</h2>
            <section className={styles.sectionAnimeDetalhes}>
                {animexDetalhes.subpastas.length >= 2 && <CardAnimexMini />}
                <div className={styles.divContainerDetalhesCarrossel}>
                    {currentDisplayId ? (<CardAnimeDetalhesApiJikan id_mal={currentDisplayId} />)
                        : (<p>Selecione um anime da coleção para ver os detalhes.</p>)}
                </div>
            </section>
            <section className={styles.sectionAnimexLocais}>
            <h3 className={styles.h3TituloDescricao}>Coleção Local</h3>
                {animexDetalhes.subpastas.map((subpasta) => (
                    <div className={styles.divContainerSubsAnimex} key={subpasta.nome}>
                        <h3>{subpasta.nome} - ID: {subpasta.id}</h3>
                        <ul>
                            {subpasta.arquivos.map((arquivo) => (
                                <li className={styles.liSubpastasAnimex} key={arquivo.nome}>{arquivo.nome}</li>
                            ))}
                        </ul>
                    </div>
                ))}
            </section>
        </main>
    );
};
//========================================
export default function AnimexDetalhes() {
    return <AnimexDetalhesContent />;
};
