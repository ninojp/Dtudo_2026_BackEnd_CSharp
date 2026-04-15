import fs from 'fs';
import path from 'path';
import axios from 'axios';
import { fileURLToPath } from 'url';

// Define __dirname para módulos ES
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
// Define o caminho do arquivo de origem, JSON e a estrutura de diretórios de saída.
const dbPath = path.join(__dirname, 'db', 'cdsDB.json');
const baseDir = 'E:\\DiscografiasPasta';
// Função para remover caracteres inválidos de nomes de arquivos/pastas no Windows
function sanitizeName(name) {
    if (typeof name !== 'string') {
        name = String(name);
    }
    return name.replace(/[<>:"/\\|?*%.!@#$¨']/g, '-').trim();
}
// Função para traduzir categoria para português no singular
function translateCategory(category, format = '') {
    const translations = {
        'album': 'Album',
        'singleep': 'Single',
        'singel': 'Single',
        'ep': 'EP',
        'compilation': 'Compilação',
        'comp': 'Compilação',
        'video': 'Video',
        'cover': 'Cover',
        'live': 'Live',
        'remix': 'Remix',
        'bootleg': 'Bootleg',
        'outros': 'Outro'
    };
    const result = translations[category?.toLowerCase()] || category;
    // Se ainda não encontrou, tenta inferir do format
    if (result === category && format) {
        const formatLower = format.toLowerCase();
        if (formatLower.includes('single')) return 'Single';
        if (formatLower.includes('ep')) return 'EP';
        if (formatLower.includes('comp')) return 'Compilação';
        if (formatLower.includes('lp') || formatLower.includes('album')) return 'Album';
        if (formatLower.includes('video')) return 'Video';
    }
    return result || 'Album';
}
// Função para criar pastas recursivamente
function createDir(dirPath) {
    if (!fs.existsSync(dirPath)) {
        fs.mkdirSync(dirPath, { recursive: true });
    }
}
// Função para baixar imagem
async function downloadImage(url, filePath) {
    const headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
    };
    try {
        const response = await axios.get(url, { responseType: 'stream', headers });
        const writer = fs.createWriteStream(filePath);
        response.data.pipe(writer);
        return new Promise((resolve, reject) => {
            writer.on('finish', resolve);
            writer.on('error', reject);
        });
    } catch (error) {
        console.error(`Erro ao baixar ${url}:`, error.message);
        const originalUrl = url.replace(/\/rs:fit\/g:sm\/q:\d+\/h:\d+\/w:\d+\//, '/');
        try {
            console.log(`Tentando URL original: ${originalUrl}`);
            const response = await axios.get(originalUrl, { responseType: 'stream', headers });
            const writer = fs.createWriteStream(filePath);
            response.data.pipe(writer);
            return new Promise((resolve, reject) => {
                writer.on('finish', resolve);
                writer.on('error', reject);
            });
        } catch (secondError) {
            console.error(`Erro ao baixar URL original ${originalUrl}:`, secondError.message);
            // Rejeita a promessa principal se a segunda tentativa também falhar
            throw secondError;
        }
    }
};
//===========================
async function run() {
    // Ler e analisar o JSON
    let dbData;
    try {
        const fileContent = fs.readFileSync(dbPath, 'utf8');
        dbData = JSON.parse(fileContent);
    } catch (error) {
        console.error(`Erro ao ler ou analisar o arquivo JSON em ${dbPath}:`, error);
        process.exit(1);
    }
    // Validar a estrutura do JSON
    if (!dbData || !Array.isArray(dbData.cdsDB)) {
        console.error(`Erro Crítico: O arquivo JSON não tem a estrutura esperada. Verifique se ele contém a chave "cdsDB" com um array de artistas.`);
        process.exit(1);
    }
    // Processar cada artista sequencialmente
    for (const artistData of dbData.cdsDB) {
        const sanitizedArtistName = sanitizeName(artistData.artist);
        const artistDir = path.join(baseDir, sanitizedArtistName);
        createDir(artistDir);
        console.log(`Processando artista: ${sanitizedArtistName}`);
        // Processar cada categoria
        for (const [category, items] of Object.entries(artistData)) {
            if (category === 'artist' || !Array.isArray(items)) continue;

            for (const item of items) {
                const sanitizedTitle = sanitizeName(item.title);
                const categoryPortugues = translateCategory(item._category || category, item.format);
                const folderName = `${item.year || 'Unknown'} - ${sanitizedTitle} - ${categoryPortugues}`;
                const itemDir = path.join(artistDir, folderName);
                createDir(itemDir);

                // Baixar thumb se existir, aguardando a conclusão
                if (item.thumb) {
                    const sanitizedId = sanitizeName(item.id);
                    const imagePath = path.join(itemDir, `${sanitizedId}.jpg`);
                    try {
                        await downloadImage(item.thumb, imagePath);
                        console.log(`  - Download concluído para: ${folderName}`);
                    } catch (err) {
                        console.error(`  - Falha no download para ${folderName}:`, err.message);
                    }
                }
            }
        }
    }
    console.log('\nEstrutura de pastas criada com sucesso!');
}
run();
