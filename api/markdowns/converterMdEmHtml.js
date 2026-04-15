'use strict';
import fs from 'fs';
import path from 'path';
import {marked} from 'marked';

//Criado pelo Copilot, Instruções (já testei e funcionou):'
//1 Instale a biblioteca marked: npm install marked
//2 Crie um script para converter os arquivos: Aqui está um exemplo de script em Node.js:
//3 Estrutura de diretórios:
//Coloque seus arquivos .md no diretório ./markdown.
//4 No terminal, execute: node seu-script.js
//Os arquivos convertidos serão salvos no diretório ./html.

const inputDir = './mds'; // Diretório com os arquivos .md
const outputDir = './htmls'; // Diretório onde os arquivos .html serão salvos
// Certifique-se de que o diretório de saída existe
if (!fs.existsSync(outputDir)) {
    fs.mkdirSync(outputDir);
}
// Leia todos os arquivos do diretório de entrada
fs.readdir(inputDir, (err, files) => {
    if (err) {
        console.error('Erro ao ler o diretório:', err);
        return;
    }
    files.forEach(file => {
        if (path.extname(file) === '.md') {
            const filePath = path.join(inputDir, file);
            const outputFilePath = path.join(outputDir, file.replace('.md', '.html'));
            // Leia o conteúdo do arquivo Markdown
            fs.readFile(filePath, 'utf8', (err, data) => {
                if (err) {
                    console.error(`Erro ao ler o arquivo ${file}:`, err);
                    return;
                }
                // Converta o Markdown para HTML
                const htmlContent = marked(data);
                // Salve o HTML no diretório de saída
                fs.writeFile(outputFilePath, htmlContent, err => {
                    if (err) {
                        console.error(`Erro ao salvar o arquivo ${outputFilePath}:`, err);
                    } else {
                        console.log(`Arquivo convertido: ${outputFilePath}`);
                    }
                });
            });
        }
    });
});
