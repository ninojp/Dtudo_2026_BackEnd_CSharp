import fs from 'fs';
import axios from 'axios';

// Arquivo origem dos dados locais das coleções de animes
const ANIMACOES_FILE = './myanimes.json';
// Arquivo de destino para salvar os detalhes dos animes
const ANIME_DETAILS_FILE = './animesDetalhes.json';
// API externa Jikan para buscar detalhes dos animes
const JIKAN_BASE_URL = 'https://api.jikan.moe/v4/anime';

// Função para buscar detalhes de um anime
async function fetchAnimeDetails(malId) {
    try {
        const response = await axios.get(`${JIKAN_BASE_URL}/${malId}/full`);
        return response.data.data;
    } catch (error) {
        console.error(`Erro ao buscar detalhes para ID ${malId}:`, error.message);
        return null;
    }
}

// Função para adicionar delay entre requests (1 segundo para respeitar rate limit)
function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// Função principal
async function populateAnimeDetails() {
    // Ler o arquivo JSON, parsear o conteúdo
    const data = JSON.parse(fs.readFileSync(ANIMACOES_FILE, 'utf8'));
    
    // Inicializar a lista de detalhes dos animes
    let animeDetails = [];
    if (fs.existsSync(ANIME_DETAILS_FILE)) {
        animeDetails = JSON.parse(fs.readFileSync(ANIME_DETAILS_FILE, 'utf8'));
    }
    
    let totalProcessados = 0;
    let duplicatas = [];
    
    // Iterar sobre cada animação
    for (const animacao of data.myanimes) {
        console.log(`Processando coleção: ${animacao.nome}`);
        // Iterar sobre cada subpasta (anime), limitado
        for (const subpasta of animacao.subpastas) { 
            if (!subpasta.detalhes) { // Só buscar se não tiver detalhes
                console.log(`Buscando detalhes para: ${subpasta.nome} (ID: ${subpasta.mal_id})`);
                const detalhes = await fetchAnimeDetails(subpasta.mal_id);
                if (detalhes) {
                    // Verificar se o anime já existe na lista de detalhes
                    const existing = animeDetails.find(a => a.mal_id === detalhes.mal_id);
                    if (!existing) {
                        animeDetails.push(detalhes);
                        subpasta.detalhes = detalhes; // Ainda salvar no myanimes.json
                        console.log(`Detalhes salvos para: ${subpasta.nome}`);
                        totalProcessados++;
                    } else {
                        duplicatas.push(detalhes.mal_id);
                        console.log(`Anime duplicado encontrado: ${subpasta.nome} (ID: ${subpasta.mal_id})`);
                    }
                } else {
                    console.log(`Falha ao buscar detalhes para: ${subpasta.nome}`);
                }
                // Delay de 1 segundo entre requests
                await delay(1000);
            } else {
                console.log(`Detalhes já existem para: ${subpasta.nome}`);
            }
        }
    }
    
    // Ordenar a lista por mal_id crescente
    animeDetails.sort((a, b) => a.mal_id - b.mal_id);
    
    // Salvar o arquivo de detalhes dos animes
    fs.writeFileSync(ANIME_DETAILS_FILE, JSON.stringify(animeDetails, null, 2));
    
    // Salvar o arquivo myanimes.json atualizado
    fs.writeFileSync(ANIMACOES_FILE, JSON.stringify(data, null, 2));
    
    console.log(`Processo concluído. ${totalProcessados} animes processados. Arquivo atualizado.`);
    
    if (duplicatas.length > 0) {
        console.log('Animes duplicados encontrados (não adicionados):', duplicatas);
    }
}

populateAnimeDetails().catch(console.error);
