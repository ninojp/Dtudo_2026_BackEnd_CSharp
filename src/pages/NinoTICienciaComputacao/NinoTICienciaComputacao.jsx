import { Link } from 'react-router-dom';
import LogoRede from '../../components/componentsNinoTI/areasTI/LogoRede';
import styles from './NinoTICienciaComputacao.module.css';
import LogoIA from '../../components/componentsNinoTI/areasTI/LogoIA';
import LogoHardware from '../../components/componentsNinoTI/areasTI/logoHardware';
import LogoOS from '../../components/componentsNinoTI/areasTI/LogoOS';
import LogoDataScience from '../../components/componentsNinoTI/areasTI/LogoDataScience';
import LogoCyberSecurity from '../../components/componentsNinoTI/areasTI/LogoCyberSecurity';

export default function NinoTICienciaComputacao() {
    return (
        <main className={styles.mainContainerPage}>
            <h1>Ciência da Computação</h1>
            <LogoRede largura={'500px'} altura={'200px'} />
            <h2>Ciência da Computação</h2>
            <p>Ciência da Computação<br /> é o estudo teórico e prático da computação, focando em algoritmos, programação, inteligência artificial, redes, segurança e desenvolvimento de softwares e sistemas para resolver problemas e inovar tecnologicamente, combinando fundamentos matemáticos e lógicos com aplicações práticas para criar soluções digitais. A área abrange desde a criação de softwares e aplicativos até o gerenciamento de bancos de dados e redes, impulsionando a transformação digital em diversas indústrias.
                Principais áreas de estudo
                Algoritmos e Estruturas de Dados: Como criar soluções eficientes e organizar informações.
                Programação: A linguagem para construir softwares e sistemas.
                Inteligência Artificial (IA): Desenvolvimento de sistemas inteligentes.
                Redes de Computadores: Conectividade e comunicação.
                Segurança da Informação: Proteção de dados e sistemas.
                Sistemas Operacionais: Funcionamento interno dos computadores.
                Banco de Dados: Armazenamento e gerenciamento de dados.
                Assista a este vídeo para conhecer a perspectiva de um aluno sobre o curso:
                O que faz um cientista da computação?
                Desenvolve softwares, aplicativos e sistemas.
                Cria e otimiza algoritmos.
                Trabalha com segurança digital e proteção de dados.
                Atua com análise de dados e inteligência artificial.
                Projeta e gerencia sistemas de informação.
                Habilidades importantes
                Raciocínio lógico e matemático.
                Capacidade de resolução de problemas.
                Pensamento analítico.
                Criatividade e inovação.
                O curso de graduação
                Duração: Geralmente 4 anos (8 semestres).
                Foco: Teoria, prática, projetos e vivência de mercado.
                Áreas de atuação: Análise de sistemas, arquitetura de software, pesquisa, gerência de TI, entre outras.
            </p>
            <div className={styles.divContainerAreasTI}>
                <ul>
                    <Link className={styles.linkMenuAreasTI} to={''} >
                        <li className='styleRedes' id="1">
                            Redes de Computadores
                            <LogoCyberSecurity largura={'35px'} altura={'35px'} />
                        </li>
                    </Link>
                    <Link className={styles.linkMenuAreasTI} to={'/ninoti/hardware'} >
                        <li className='styleHardware' id="7">
                            Hardware e Manutenção
                            <LogoHardware largura={'35px'} altura={'35px'} />
                        </li>
                    </Link>
                    <Link className={styles.linkMenuAreasTI} to={'/ninoti/os'} >
                        <li className='styleOS' id="5">
                            Sistemas Operacionais
                            <LogoOS largura={'35px'} altura={'35px'} className={styles.menuLogoOS} />
                        </li>
                    </Link>
                    <Link className={styles.linkMenuAreasTI} to={'/ninoti/data-science'} >
                        <li className='styleDataScience' id="9">
                            Data Science
                            <LogoDataScience largura={'30px'} altura={'30px'} />
                        </li>
                    </Link>
                </ul>
            </div>
        </main>
    );
};
