'use strict';
// import conectaApiTMDB from "./conectApiTMDB.js";
const requestQueue = [];
let isProcessingQueue = false;
let requestCount = 0;
const maxRequestsPerMinute = 60;

async function processQueue() {
    if (isProcessingQueue) return;
    isProcessingQueue = true;
    while (requestQueue.length > 0) {
        if (requestCount >= maxRequestsPerMinute) {
            console.log('Atingido o limite de requisições por minuto. Aguardando...');
            alert('Atingido o limite de requisições por minuto. Aguardando...');
            await delay(60000); // Aguardar 1 minuto
            requestCount = 0; // Resetar o contador de requisições
        }
        const { id_mal, resolve, reject } = requestQueue.shift();
        try {
            const data = await fetchAnimeData(id_mal);
            resolve(data);
            requestCount++;
        } catch (error) {
            reject(error);
        }
        await delay(350); // Delay de 350ms para garantir no máximo 3 requisições por segundo
    }
    isProcessingQueue = false;
}

async function fetchAnimeData(id_mal) {
    if (typeof id_mal !== "number" && typeof id_mal !== "string") {
        throw new Error(`ID inválido passado para fetchAnimeData: ${JSON.stringify(id_mal)}`);
    }
    if (typeof id_mal === "number") {
        const respostaAPIJikan = await fetch(`https://api.jikan.moe/v4/anime/${id_mal}/full`);
        if (!respostaAPIJikan.ok) {
            throw new Error(`Erro na requisição: ${respostaAPIJikan.statusText}`);
        }
        const objDadosJikam = await respostaAPIJikan.json();
        return objDadosJikam.data;
    }
}
function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function conectaAPIJikan(id_mal) {
    if (typeof id_mal !== "number" && typeof id_mal !== "string") {
        console.error(`ID inválido passado para conectaAPIJikan: ${JSON.stringify(id_mal)}`);
        return Promise.reject(new Error(`ID inválido: ${JSON.stringify(id_mal)}`));
    }
    return new Promise((resolve, reject) => {
        requestQueue.push({ id_mal, resolve, reject });
        processQueue();
    });
}
export default conectaAPIJikan;
