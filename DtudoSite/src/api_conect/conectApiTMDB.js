'use strist';

async function conectaApiTMDB(movie_id) {
    try {
        const apiKey = '38574258670385dca5e0428ff5430190';
        const response = await fetch(`https://api.themoviedb.org/3/movie/${movie_id}?api_key=${apiKey}&language=pt-BR`);
        if (!response.ok) {
            throw new Error(`conectaApiTMDB(): Erro na requisição: ${response.statusText}`);
        };
        const filmeRetornado = await response.json();
        return filmeRetornado;
    } catch (error) {
        console.log(`conectaApiTMDB(): Erro ao conectar com API para o ID ${movie_id}:`, error);
        throw error;
    };
};
export default conectaApiTMDB;
//===========================================================================================================================================

// Exemplo de uso
// conectaApiTMDB('1241982-moana-2').then(filme => console.log(filme)).catch(error => console.error(error));
// async function conectaApiTMDB() {
//     try {
//         const response = await fetch(`https://api.themoviedb.org/3/movie/1241982-moana-2?api_key=38574258670385dca5e0428ff5430190&language=pt-BR`);
//         if (!response.ok) {
//             throw new Error(`conectaApiTMDB(): Erro na requisição: ${response.statusText}`);
//         };
//         const filmeRetornado = await response.json();
//         console.log('conectaApiTMDB(): ', filmeRetornado);
//         return filmeRetornado;
//     } catch (error) {
//         console.log(`conectaApiTMDB(): Erro ao conectar com API para o ID:`, error);
//         throw error;
//     };
// };
