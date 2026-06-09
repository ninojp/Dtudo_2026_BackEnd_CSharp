/* eslint-env node */
import { fileURLToPath } from 'url';
import fs from 'node:fs';
import fsPromises from 'node:fs/promises';
import path from 'node:path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Altere aqui para os diretórios que deseja monitorar
const CONFIG = {
    pastasParaMonitorar: [
        'F:\\#', 'F:\\A', 'F:\\B', 'F:\\C', 'F:\\D', 'F:\\E', 'F:\\F', 'F:\\G', 'F:\\H', 'F:\\I', 'F:\\J',
        'F:\\K', 'F:\\L', 'F:\\M', 'F:\\N', 'F:\\O', 'F:\\P', 'F:\\Q', 'H:\\R', 'H:\\S', 'H:\\T', 'H:\\U', 'H:\\V', 'H:\\W', 'H:\\X', 'H:\\Y', 'H:\\Z'
    ],
    arquivoSaida: path.resolve(__dirname, './myanimes.json'),
    pastaDestinoImagens: path.resolve(__dirname, '../../public/myanimes/animes'),
    pastaDestinoCapas: path.resolve(__dirname, '../../public/myanimes'),
};

/**
 * Varre recursivamente até 2 níveis de subpasta dos diretórios de origem, 
 * encontra imagens com nomes numéricos (ex: 123.jpg) e as copia para a pasta de destino,
 * verificando se já existem para evitar sobrescrições.
 */
async function copiarCapasAnimes(pastasOrigem, pastaDestino) {
    console.log(`Iniciando cópia de imagens para: ${pastaDestino}`);
    await ensureDir(pastaDestino);
    let imagensCopiadas = 0;
    let imagensPuladas = 0;
    const MAX_DEPTH = 2; // Limita a busca a 2 níveis de subpasta
    async function walk(diretorio, depth) {
        // Garante que o diretório é uma string válida antes de prosseguir.
        if (typeof diretorio !== 'string' || !fs.existsSync(diretorio)) {
            return;
        }
        if (depth > MAX_DEPTH) {
            return; // Para a recursão se a profundidade máxima for atingida
        }
        const entradas = await readDirSafe(diretorio);
        for (const entrada of entradas) {
            const caminhoCompleto = path.join(diretorio, entrada.name);
            if (entrada.isDirectory()) {
                await walk(caminhoCompleto, depth + 1); // Recursão para subpastas, incrementando a profundidade
            } else if (entrada.isFile() && /^\d+\.jpg$/i.test(entrada.name)) {
                const caminhoDestino = path.join(pastaDestino, entrada.name);
                if (!fs.existsSync(caminhoDestino)) {
                    try {
                        await fsPromises.copyFile(caminhoCompleto, caminhoDestino);
                        imagensCopiadas++;
                    } catch (err) {
                        console.error(`Erro ao copiar ${caminhoCompleto}:`, err);
                    }
                } else {
                    imagensPuladas++;
                }
            }
        }
    }
    await Promise.all(pastasOrigem.map(pasta => walk(pasta, 0)));
    console.log(`Cópia de imagens concluída. Total de ${imagensCopiadas} imagens copiadas, ${imagensPuladas} puladas (já existiam).`);
}

/**
 * Copia a primeira imagem numérica .jpg da primeira subpasta de nível 2 de cada pasta de anime para a pasta de capas,
 * verificando se já existem para evitar sobrescrições.
 */
async function copiarImagensCapaColecao(pastasOrigem, pastaDestino) {
    console.log(`Iniciando cópia de imagens de capa para: ${pastaDestino}`);
    await ensureDir(pastaDestino);
    let imagensDeCapaCopiadas = 0;
    let imagensDeCapaPuladas = 0;
    for (const pastaRaiz of pastasOrigem) {
        const itensRaiz = await readDirSafe(pastaRaiz);
        const pastasDeAnime = itensRaiz.filter((it) => it.isDirectory());
        for (const item of pastasDeAnime) {
            const fullPath = path.join(pastaRaiz, item.name);
            const subdirs = (await readDirSafe(fullPath)).filter((it) => it.isDirectory());
            if (subdirs.length === 0) continue;
            const primeiraSubpasta = subdirs[0];
            const subFullPath = path.join(fullPath, primeiraSubpasta.name);
            const conteudo = await listContents(subFullPath);
            const jpgs = conteudo.filter((arq) => arq.tipo === 'arquivo' && /^\d+\.jpg$/i.test(arq.nome)).sort((a, b) => parseInt(a.nome) - parseInt(b.nome));
            if (jpgs.length === 0) continue;
            const primeiraJpg = jpgs[0];
            const caminhoOrigem = path.join(subFullPath, primeiraJpg.nome);
            const nomeDestino = `${normalizeName(item.name)}.jpg`;
            const caminhoDestino = path.join(pastaDestino, nomeDestino);
            if (!fs.existsSync(caminhoDestino)) {
                try {
                    await fsPromises.copyFile(caminhoOrigem, caminhoDestino);
                    imagensDeCapaCopiadas++;
                    console.log(`Imagem de capa copiada: ${nomeDestino}`);
                } catch (err) {
                    console.error(`Erro ao copiar imagem de capa ${caminhoOrigem}:`, err);
                }
            } else {
                imagensDeCapaPuladas++;
                console.log(`Imagem de capa pulada (já existe): ${nomeDestino}`);
            }
        }
    }
    console.log(`Cópia de imagens de capa concluída. Total de ${imagensDeCapaCopiadas} imagens de capa copiadas, ${imagensDeCapaPuladas} puladas (já existiam).`);
}
/** SLUGIFY: do curso alura, testar pra ver se é melhor que o atual
 * .toLowerCase()
    .normalize('NFD').replace(/\p{Diacritic}/gu, '') // Remove acentuação
    .replace(/[^a-z0-9\s-]/g, '') // Remove caracteres especiais
    .trim() // Remove espaços do início e fim
    .replace(/\s+/g, '-'); // Troca espaços por hífens
 */
/**
 * Normaliza o nome do arquivo, removendo acentos, convertendo para minúsculas e substituindo espaços por underscores.
 * @param {string} name - O nome a ser normalizado.
 * @returns {string} O nome normalizado.
 */
function normalizeName(name = '') {
    return String(name)
        .normalize('NFKD')
        .replace(/\p{M}/gu, '') // remove diacríticos
        .toLowerCase()
        .replace(/\s+/g, '_')
        .replace(/[^a-z0-9._-]+/g, '_')
        .replace(/_+/g, '_')
        .replace(/^_+|_+$/g, '');
}
/**
 * Gera um slug a partir do nome normalizado.
 * @param {string} name - O nome a ser convertido em slug.
 * @returns {string} O slug gerado.
 */
function slugify(name = '') {
    return normalizeName(name).replace(/[_]+/g, '-');
}
/**
 * Garante que o diretório exista, criando-o recursivamente se necessário.
 * @param {string} dirPath - O caminho do diretório.
 */
async function ensureDir(dirPath) {
    try {
        await fsPromises.mkdir(dirPath, { recursive: true });
    } catch (err) {
        // Se a pasta já existir em condições de corrida, ignore
        if (err.code !== 'EEXIST') throw err;
    }
}
/**
 * Lê o conteúdo de um diretório de forma segura, retornando um array vazio em caso de erro.
 * @param {string} dir - O caminho do diretório.
 * @returns {Promise<fs.Dirent[]>} Array de entradas do diretório.
 */
async function readDirSafe(dir) {
    try { // Garante que 'dir' seja uma string antes de usar
        if (typeof dir !== 'string') return [];
        return await fsPromises.readdir(dir, { withFileTypes: true });
    } catch (err) {
        console.warn(`Não foi possível ler ${dir}: ${err.message}`);
        return [];
    }
}
/**
 * Lista o conteúdo de um diretório, retornando objetos com nome e tipo.
 * @param {string} dir - O caminho do diretório.
 * @returns {Promise<Array<{nome: string, tipo: 'arquivo'|'pasta'}>>} Array de objetos representando o conteúdo.
 */
async function listContents(dir) {
    const entries = await readDirSafe(dir);
    return entries.map((entry) => ({ nome: entry.name, tipo: entry.isDirectory() ? 'pasta' : 'arquivo' }));
}
/**
 * Gera a estrutura personalizada de dados a partir das pastas listadas.
 * Processa cada pasta raiz, extraindo informações de animes e subpastas.
 * @param {string[]} pastas - Array de caminhos das pastas raiz a serem processadas.
 * @returns {Promise<Array<Object>>} Array de objetos representando a estrutura de animes.
 */
async function gerarEstruturaPersonalizada(pastas) {
  const processarPastaRaiz = async (pastaRaiz) => {
    const itensRaiz = await readDirSafe(pastaRaiz);
    const pastasDeAnime = itensRaiz.filter((it) => it.isDirectory());
    return Promise.all(pastasDeAnime.map(async (item) => {
      const fullPath = path.join(pastaRaiz, item.name);
      const subdirs = (await readDirSafe(fullPath)).filter((it) => it.isDirectory());
      const subpastas = await Promise.all(subdirs.map(async (sub) => {
        const subFullPath = path.join(fullPath, sub.name);
        const conteudo = await listContents(subFullPath);
        const jpg = conteudo.find((arq) => arq.tipo === 'arquivo' && /^\d+\.jpg$/i.test(arq.nome));
        const idSubpasta = jpg ? parseInt(jpg.nome, 10) : null;
        const ano = sub.name.substring(0, 4);
        const nomeSemAno = sub.name.substring(5).trim();
        return { 
          mal_id: idSubpasta, 
          nome: sub.name, 
          ano, 
          nomeSemAno, 
          jaAssisti: false, 
          gostei: false, 
          arquivos: conteudo.map(arq => ({ nome: arq.nome })) 
        };
      }));
      return {
        nome: item.name,
        slug: slugify(item.name),
        imgSrc: `${normalizeName(item.name)}.jpg`,
        subpastas,
      };
    }));
  };
  const arraysDeResultados = await Promise.all(pastas.map(processarPastaRaiz));
  return arraysDeResultados.flat().map((item, index) => ({ ...item, id: index + 1 }));
}
/**
 * Escreve dados JSON de forma atômica, criando um arquivo temporário e renomeando para evitar corrupções.
 * @param {string} targetPath - O caminho do arquivo de destino.
 * @param {Object} data - Os dados a serem escritos em JSON.
 */
async function writeJsonAtomically(targetPath, data) {
    const dir = path.dirname(targetPath);
    await ensureDir(dir);
    const tmpPath = `${targetPath}.${Date.now()}.tmp`;
    await fsPromises.writeFile(tmpPath, JSON.stringify(data, null, 2), 'utf8');
    await fsPromises.rename(tmpPath, targetPath);
}
// Função principal que atualiza as animações: copia imagens, gera estrutura e salva JSON.
async function atualizarAnimacoes() {
    try {
        // Executa a cópia das imagens das capas dos Animes para a pasta public/myanimes/animes/, antes de gerar o JSON
        await copiarCapasAnimes(CONFIG.pastasParaMonitorar, CONFIG.pastaDestinoImagens);
        // Copia as imagens de capa de cada COLEÇÃO MyaAnimes, para a pasta public/myanimes/
        await copiarImagensCapaColecao(CONFIG.pastasParaMonitorar, CONFIG.pastaDestinoCapas);
        const estrutura = await gerarEstruturaPersonalizada(CONFIG.pastasParaMonitorar);
        let jsonAtual = {};
        try {
            if (fs.existsSync(CONFIG.arquivoSaida)) {
                const raw = await fsPromises.readFile(CONFIG.arquivoSaida, 'utf8');
                jsonAtual = raw ? JSON.parse(raw) : {};
            }
        } catch (readErr) {
            console.warn(`Falha ao ler ${CONFIG.arquivoSaida}: ${readErr.message}`);
            jsonAtual = { myanimes: [] };
        }
        jsonAtual.myanimes = estrutura;
        jsonAtual.ultimaAtualizacao = new Date().toISOString();
        await writeJsonAtomically(CONFIG.arquivoSaida, jsonAtual);
        console.log(`Arquivo ${CONFIG.arquivoSaida} atualizado com sucesso!`);
    } catch (err) {
        console.error('Erro ao atualizar myanimes.json:', err);
    }
}
// --- Execução Principal ---
console.log('Iniciando a geração do arquivo de animes...');
atualizarAnimacoes();
