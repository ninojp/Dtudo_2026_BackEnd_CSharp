/* eslint-env node */
import { fileURLToPath } from 'url';
import fs from 'node:fs';
import fsPromises from 'node:fs/promises';
import path from 'node:path';
import chokidar from 'chokidar';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// --- Configuração ---
// Altere aqui para os diretórios que deseja monitorar
const pastasParaMonitorar = ['H:\\AnimeX'];
const arquivoSaida = path.resolve(__dirname, '../../db/animex.json');

// --- Helpers ---
/** SLUGIFY: do curso alura, testar pra ver se é melhor que o atual
 * .toLowerCase()
    .normalize('NFD').replace(/\p{Diacritic}/gu, '') // Remove acentuação
    .replace(/[^a-z0-9\s-]/g, '') // Remove caracteres especiais
    .trim() // Remove espaços do início e fim
    .replace(/\s+/g, '-'); // Troca espaços por hífens
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
// Gera slug a partir do nome
function slugify(name = '') {
    return normalizeName(name).replace(/[_]+/g, '-');
}
// Garante que a pasta exista
async function ensureDir(dirPath) {
    try {
        await fsPromises.mkdir(dirPath, { recursive: true });
    } catch (err) {
        // Se a pasta já existir em condições de corrida, ignore
        if (err.code !== 'EEXIST') throw err;
    }
}
// Lê diretório de forma segura, retornando array vazio em caso de erro
async function readDirSafe(dir) {
    try {
        return await fsPromises.readdir(dir, { withFileTypes: true });
    } catch (err) {
        console.warn(`Não foi possível ler ${dir}: ${err.message}`);
        return [];
    }
}
/** Retorna array de objetos { nome, tipo: 'arquivo'|'pasta' } */
async function listContents(dir) {
  const entries = await readDirSafe(dir);
  return entries.map((entry) => ({ nome: entry.name, tipo: entry.isDirectory() ? 'pasta' : 'arquivo' }));
}
/** Gera a estrutura desejada a partir das pastas listadas */
async function gerarEstruturaPersonalizada(pastas) {
    let idGlobal = 1;
    const resultado = [];
    // Para cada pasta raiz
    for (const pastaRaiz of pastas) {
        const itensRaiz = await readDirSafe(pastaRaiz);
        for (const item of itensRaiz.filter((it) => it.isDirectory())) {
            const fullPath = path.join(pastaRaiz, item.name);
            // segundo nível: subpastas dentro de fullPath
            const subdirs = (await readDirSafe(fullPath)).filter((it) => it.isDirectory());
            const subpastas = [];
            // para cada subpasta, lista conteúdos
            for (const sub of subdirs) {
                const subFullPath = path.join(fullPath, sub.name);
                const conteudo = await listContents(subFullPath);
                // Procura arquivo .jpg com nome numérico (e.g., 1.jpg)
                const jpg = conteudo.find((arq) => arq.tipo === 'arquivo' && /^\d+\.jpg$/i.test(arq.nome));
                const idSubpasta = jpg ? parseInt(jpg.nome, 10) : null;
                // Pega os 4 primeiros digitos do nome da subpasta como ano
                const ano = sub.name.substring(0, 4);
                // Retira os 5 primeiros digitos do nome da subpasta como ano
                const nomeSemAno = sub.name.substring(5).trim();
                subpastas.push({ id: idSubpasta, nome: sub.name, ano, nomeSemAno, arquivos: conteudo });
            }
            resultado.push({
                id: idGlobal++,
                nome: item.name,
                slug: slugify(item.name),
                imgSrc: `${normalizeName(item.name)}.jpg`,
                subpastas,
            });
        }
    }
    return resultado;
}

/** Escreve JSON de forma atômica (escreve em arquivo temporário e renomeia) */
async function writeJsonAtomically(targetPath, data) {
    const dir = path.dirname(targetPath);
    await ensureDir(dir);
    const tmpPath = `${targetPath}.${Date.now()}.tmp`;
    await fsPromises.writeFile(tmpPath, JSON.stringify(data, null, 2), 'utf8');
    await fsPromises.rename(tmpPath, targetPath);
}

// --- Atualização / debounce ---
let debounceTimer = null;
function scheduleAtualizarAnimacoes(delay = 1000) {
    if (debounceTimer) clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => atualizarAnimacoes().catch((e) => console.error(e)), delay);
}

async function atualizarAnimacoes() {
    try {
        const estrutura = await gerarEstruturaPersonalizada(pastasParaMonitorar);
        let jsonAtual = {};

        try {
            if (fs.existsSync(arquivoSaida)) {
                const raw = await fsPromises.readFile(arquivoSaida, 'utf8');
                jsonAtual = raw ? JSON.parse(raw) : {};
            }
        } catch (readErr) {
            console.warn(`Falha ao ler ${arquivoSaida}: ${readErr.message}`);
            jsonAtual = {};
        }

        jsonAtual.objAnimex = estrutura;
        jsonAtual.ultimaAtualizacao = new Date().toISOString();

        await writeJsonAtomically(arquivoSaida, jsonAtual);
        console.log(`Arquivo ${arquivoSaida} atualizado com sucesso!`);
    } catch (err) {
        console.error('Erro ao atualizar animacoes.json:', err);
    }
}

// --- Inicialização do watcher ---
const watcher = chokidar.watch(pastasParaMonitorar, { ignoreInitial: false, persistent: true, depth: 2 });

watcher.on('error', (error) => console.warn('Erro ao monitorar:', error && error.message ? error.message : error));
['add', 'change', 'unlink', 'addDir', 'unlinkDir'].forEach((ev) => watcher.on(ev, () => scheduleAtualizarAnimacoes()));

console.log('Monitorando alterações em:', pastasParaMonitorar.join(', '));

// Garantia: atualiza imediatamente ao iniciar também (chokidar com ignoreInitial:false já dispara, mas chamamos explicitamente)
scheduleAtualizarAnimacoes(0);

// Limpeza em encerramento do processo (checa se `process` existe — util para alguns ambientes de teste)
if (typeof globalThis.process !== 'undefined' && typeof globalThis.process.on === 'function') {
    globalThis.process.on('SIGINT', async () => {
        console.log('Encerrando watcher...');
        if (debounceTimer) clearTimeout(debounceTimer);
        try {
            await watcher.close();
        } catch {
            // ignore
        }
        globalThis.process.exit(0);
    });
}
