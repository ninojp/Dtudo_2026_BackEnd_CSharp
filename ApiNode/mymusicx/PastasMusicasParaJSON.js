import { fileURLToPath } from 'url';
import fs from 'node:fs';
import fsPromises from 'node:fs/promises';
import path from 'node:path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const CONFIG = {
    // Pasta raiz onde estão as músicas organizadas por artista/releases
    pastasParaMonitorar: ['E:\\MUSICAS\\ROCK NACIONAL'],
    // Pasta onde deve ser gravado o arquivo JSON gerado
    arquivoSaida: path.resolve(__dirname, '../api/db/mymusicx.json'),
    // Pasta onde as imagens numeradas de cada release serão copiadas
    pastaDestinoImagens: path.resolve(__dirname, '../public/mymusicx/releases'),
    // Pasta onde as capas dos artistas serão copiadas
    pastaDestinoCapas: path.resolve(__dirname, '../public/mymusicx'),
};
const IMAGE_REGEX = /^(\d+)\.(jpe?g|png|webp|avif)$/i;
const slugify = (text) => {
    if (!text) return '';
    return text
        .normalize('NFD')
        .replace(/\p{Diacritic}/gu, '')
        .replace(/[^a-zA-Z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        .toLowerCase();
};

const ensureDir = async (dirPath) => {
    await fsPromises.mkdir(dirPath, { recursive: true });
};

const mapCategoria = (rawType = '') => {
    const tipo = rawType.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
    if (tipo.includes('single') || tipo.includes('ep')) return 'singles-EP';
    if (tipo.includes('compilacao') || tipo.includes('compilation') || tipo.includes('comp')) return 'compilations';
    if (tipo.includes('video')) return 'videos';
    return 'albums';
};

const parseReleaseNome = (folderName) => {
    const match = folderName.match(/^(\d{4})\s*-\s*(.+?)\s*-\s*(.+)$/);
    if (!match) {
        return { ano: '', titulo: folderName.trim(), categoria: 'albums' };
    }
    const [, ano, titulo, tipo] = match;
    return { ano: ano.trim(), titulo: titulo.trim(), categoria: mapCategoria(tipo.trim()) };
};

const copiarImagemNumeric = async (srcPath, discogsId) => {
    const ext = path.extname(srcPath).toLowerCase();
    const destino = path.join(CONFIG.pastaDestinoImagens, `${discogsId}${ext}`);
    await ensureDir(CONFIG.pastaDestinoImagens);
    const jaExiste = fs.existsSync(destino);
    if (!jaExiste) {
        await fsPromises.copyFile(srcPath, destino);
    }
    return destino;
};

const walkReleaseFiles = async (releasePath, depth = 0, maxDepth = 2, prefix = '') => {
    const dirents = await fsPromises.readdir(releasePath, { withFileTypes: true });
    dirents.sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'));

    const arquivos = [];
    let primeiraImagemNumerica = '';

    for (const dirent of dirents) {
        const fullPath = path.join(releasePath, dirent.name);
        const relPath = prefix ? path.posix.join(prefix, dirent.name) : dirent.name;

        if (dirent.isFile()) {
            arquivos.push(relPath.replace(/\\/g, '/'));
            if (!primeiraImagemNumerica && IMAGE_REGEX.test(dirent.name)) {
                primeiraImagemNumerica = fullPath;
            }
        } else if (dirent.isDirectory() && depth < maxDepth) {
            const nested = await walkReleaseFiles(fullPath, depth + 1, maxDepth, relPath.replace(/\\/g, '/'));
            arquivos.push(...nested.arquivos);
            if (!primeiraImagemNumerica && nested.primeiraImagemNumerica) {
                primeiraImagemNumerica = nested.primeiraImagemNumerica;
            }
        }
    }

    return { arquivos, primeiraImagemNumerica };
};

const lerArquivosRelease = async (releasePath) => {
    const { arquivos, primeiraImagemNumerica } = await walkReleaseFiles(releasePath);
    let discogsId = '';
    if (primeiraImagemNumerica) {
        const [, id] = path.basename(primeiraImagemNumerica).match(IMAGE_REGEX);
        discogsId = id;
        await copiarImagemNumeric(primeiraImagemNumerica, discogsId);
    }
    return { arquivos, discogsId, primeiraImagemNumerica };
};

const montarArtistObj = (nomeArtista) => ({
    id: slugify(nomeArtista) || nomeArtista,
    artista: nomeArtista,
    releases: {
        albums: [],
        'singles-EP': [],
        compilations: [],
        videos: [],
    },
});

const isImageFile = (name) => /\.(jpe?g|png|webp|avif)$/i.test(name);

const findFirstImageInDir = async (dirPath) => {
    const dirents = await fsPromises.readdir(dirPath, { withFileTypes: true });
    const files = dirents.filter((d) => d.isFile()).map((d) => d.name).sort();
    const firstImage = files.find((n) => isImageFile(n));
    return firstImage ? path.join(dirPath, firstImage) : '';
};

const processarArtista = async (baseDir, direntArtista) => {
    const caminhoArtista = path.join(baseDir, direntArtista.name);
    const artistObj = montarArtistObj(direntArtista.name);
    const releases = (await fsPromises.readdir(caminhoArtista, { withFileTypes: true }))
        .filter((d) => d.isDirectory())
        .sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
    let capaCopiada = false;

    for (const releaseDir of releases) {
        const caminhoRelease = path.join(caminhoArtista, releaseDir.name);
        const { ano, titulo, categoria } = parseReleaseNome(releaseDir.name);
        const { arquivos, discogsId, primeiraImagemNumerica } = await lerArquivosRelease(caminhoRelease);
        const releaseObj = {
            discogs_id: discogsId,
            titulo,
            ano,
            arquivosLocais: arquivos,
        };
        artistObj.releases[categoria] = artistObj.releases[categoria] || [];
        artistObj.releases[categoria].push(releaseObj);

        if (!capaCopiada && categoria === 'albums' && primeiraImagemNumerica) {
            await ensureDir(CONFIG.pastaDestinoCapas);
            const destinoCapa = path.join(CONFIG.pastaDestinoCapas, `${artistObj.id}.jpg`);
            if (!fs.existsSync(destinoCapa)) {
                await fsPromises.copyFile(primeiraImagemNumerica, destinoCapa);
            }
            capaCopiada = true;
        }
    }

    return artistObj;
};

const varrerPastas = async () => {
    const artistasMap = new Map();
    for (const pastaBase of CONFIG.pastasParaMonitorar) {
        if (!fs.existsSync(pastaBase)) {
            console.warn(`Diretório não encontrado: ${pastaBase}`);
            continue;
        }
        const dirents = await fsPromises.readdir(pastaBase, { withFileTypes: true });
        for (const dirent of dirents) {
            if (!dirent.isDirectory()) continue;
            const artista = await processarArtista(pastaBase, dirent);
            if (artistasMap.has(artista.artista)) {
                const existente = artistasMap.get(artista.artista);
                for (const categoria of Object.keys(artista.releases)) {
                    existente.releases[categoria] = [
                        ...existente.releases[categoria],
                        ...artista.releases[categoria],
                    ];
                }
            } else {
                artistasMap.set(artista.artista, artista);
            }
        }
    }
    return Array.from(artistasMap.values());
};

const salvarJson = async (dados) => {
    await ensureDir(path.dirname(CONFIG.arquivoSaida));
    const payload = { mymusicx: dados };
    await fsPromises.writeFile(CONFIG.arquivoSaida, JSON.stringify(payload, null, 4), 'utf8');
};

const executar = async () => {
    const artistas = await varrerPastas();
    await salvarJson(artistas);
    console.log(`Arquivo JSON Gerado com Sucesso: ${CONFIG.arquivoSaida}`);
};

executar().catch((err) => {
    console.error('Erro ao gerar JSON:', err);
    process.exit(1);
});





