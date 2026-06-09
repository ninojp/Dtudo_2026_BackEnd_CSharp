'use strict';
const URLanimacoesAPI = 'http://localhost:3004/animacoes/';
const URLanimesAPI = 'http://localhost:3004/animes/';
const apiLocal = {
    async _fetch(url, options = {}) {
        try {
            const response = await fetch(url, options);
            if (!response.ok) {
                throw new Error(`ERRO! Ao tentar acessar API_Local: status - ${response.status}`);
            }
            return await response.json();
        } catch (error) {
            console.error(`Erro ao acessar ${url}:`, error);
            throw error;
        }
    },
    //=============================================================================================
    async consultarTodasAnimacao() {
        return this._fetch(URLanimacoesAPI);
    },
    //=============================================================================================
    async consultaAnimacaoPorId(id) {
        return this._fetch(`${URLanimacoesAPI}${id}`);
    },
    //=============================================================================================
    async consultarAnimacaoPorNome(termoBuscado) {
        try {
            const todasAnimacao = await this.consultarTodasAnimacao();
            const animacaoFiltradas = todasAnimacao.filter(animacao => {
                return (animacao.nome.toLowerCase().includes(termoBuscado.toLowerCase()));
            });
            return animacaoFiltradas; // Retorne apenas as animações filtradas
        } catch (error) {
            console.error('Erro! consultarAnimacaoPorNome()', error);
            throw error;
        }
    },
    //=============================================================================================
    async criaNovaAnimacao(animacao) {
        return this._fetch(URLanimacoesAPI, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(animacao)
        });
    },
    //=============================================================================================
    async atualizaAnimacao(animacao) {
        return this._fetch(`${URLanimacoesAPI}${animacao.id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(animacao)
        });
    },
    //=============================================================================================
    async deletaAnimacao(id) {
        try {
            const response = await fetch(`${URLanimacoesAPI}${id}`, {
                method: 'DELETE'
            });
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response;
        } catch (error) {
            console.error('deletaAnimacao(id) - Erro ao deletar animação:', error);
            throw error;
        };
    },
    //=============================================================================================
    //Captura da APIJikan o Objeto Anime Atual, através da página anime-detalhes.html e Cria um novo registro do anime na APILocal.
    async criaNovoAnime(anime) {
        return this._fetch(URLanimesAPI, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(anime)
        });
    }
    //=============================================================================================
};
export default apiLocal;
// MEU CODIGO ANTES
// const apiLocal = {
//     async consultarAnimacoes() {
//         try {
//             const response = await fetch(URLAPILocal);
//             return await response.json();
//         } catch (error) {
//             console.error('Erro ao receber animações da API:', error);
//             return [];
//         };
//     },
//     //=====================================================================
//     async consultarAnimacaoPorId(id) {
//         try {
//             const response = await fetch(`${URLAPILocal}${id}`);
//             return await response.json();
//         } catch (error) {
//             console.error('Erro ao receber animações da API:', error);
//             return [];
//         };
//     },
//     //=====================================================================
//     async criarNovaAnimacao(animacao) {
//         try {
//             const response = await fetch(URLAPILocal, {
//                 method: 'POST',
//                 headers: {
//                     'Content-Type': 'application/json'
//                 },
//                 body: JSON.stringify(animacao)
//             });
//             return await response.json();
//         } catch (error) {
//             console.error('Erro ao criar nova animação:', error);
//             throw error;
//         };
//     },
//     //=====================================================================
//     async atualizarAnimacao(id, animacao) {
//         try {
//             const response = await fetch(`${URLAPILocal}${id}`, {
//                 method: 'PUT',
//                 headers: {
//                     'Content-Type': 'application/json'
//                 },
//                 body: JSON.stringify(animacao)
//             });
//             return await response.json();
//         } catch (error) {
//             console.error('Erro ao atualizar animação:', error);
//             throw error;
//         };
//     },
//     //=====================================================================
//     async deletarAnimacao(id) {
//         try {
//             const resposta = await fetch(`${URLAPILocal}${id}`, { method: 'DELETE' });
//             return await resposta.json();
//         } catch (error) {
//             console.error('Erro ao deletar animação:', error);
//             throw error;
//         };
//     }
// };
// export default apiLocal;
//==========================================================================================

//Gemini-2
// const apiLocal = {
//     async _fetch(url, options = {}) {
//         try {
//             const response = await fetch(url, options);
//             if (!response.ok) {
//                 throw new Error(`ERRO! Ao tentar acessar API_Local: status - ${response.status}`);
//             }
//             return await response.json();
//         } catch (error) {
//             console.error(`Erro ao acessar ${url}:`, error);
//             throw error;
//         }
//     },
//     async consultarAnimacoes() {
//         return this._fetch(URLAPILocal);
//     },
//     async consultarAnimacaoPorId(id) {
//         return this._fetch(`${URLAPILocal}${id}`);
//     },
//     async criarNovaAnimacao(animacao) {
//         return this._fetch(URLAPILocal, {
//             method: 'POST',
//             headers: {
//                 'Content-Type': 'application/json'
//             },
//             body: JSON.stringify(animacao)
//         });
//     },
//     async atualizarAnimacao(id, animacao) {
//         return this._fetch(`${URLAPILocal}${id}`, {
//             method: 'PUT',
//             headers: {
//                 'Content-Type': 'application/json'
//             },
//             body: JSON.stringify(animacao)
//         });
//     },
//     async deletarAnimacao(id) {
//         try {
//             const response = await fetch(`${URLAPILocal}${id}`, {
//                 method: 'DELETE'
//             });
//             if (!response.ok) {
//                 throw new Error(`HTTP error! status: ${response.status}`);
//             }
//             return response; // Retorna a resposta para verificar o status
//         } catch (error) {
//             console.error('Erro ao deletar animação:', error);
//             throw error;
//         }
//     }
// };
// ==========================================================================================
//Luri - Alura IA
// const apiLocal = {
//     async consultarAnimacoes() {
//         return this._fetchData(URLAPILocal);
//     },

//     async consultarAnimacaoPorId(id) {
//         return this._fetchData(URLAPILocal + id);
//     },

//     async criarNovaAnimacao(animacao) {
//         return this._sendData(URLAPILocal, 'POST', animacao);
//     },

//     async atualizarAnimacao(id, animacao) {
//         return this._sendData(URLAPILocal + id, 'PUT', animacao);
//     },

//     async deletarAnimacao(id) {
//         try {
//             await fetch(URLAPILocal + id, {
//                 method: 'DELETE'
//             });
//         } catch (error) {
//             this._handleError('Erro ao deletar animação:', error);
//             throw error;
//         }
//     },
//     async _fetchData(url) {
//         try {
//             const response = await fetch(url);
//             if (!response.ok) {
//                 throw new Error(`Erro: ${response.status} ${response.statusText}`);
//             }
//             return await response.json();
//         } catch (error) {
//             this._handleError('Erro ao receber animações da API:', error);
//             return [];
//         }
//     },
//     async _sendData(url, method, data) {
//         try {
//             const response = await fetch(url, {
//                 method: method,
//                 headers: {
//                     'Content-Type': 'application/json'
//                 },
//                 body: JSON.stringify(data)
//             });
//             if (!response.ok) {
//                 throw new Error(`Erro: ${response.status} ${response.statusText}`);
//             }
//             return await response.json();
//         } catch (error) {
//             this._handleError('Erro ao enviar dados para a API:', error);
//             throw error;
//         }
//     },
//     _handleError(message, error) {
//         console.error(message, error);
//     }
// };
