# Olá! Que legal que você está aprendendo a exibir arquivos Markdown no seu site!

Para exibir um arquivo Markdown no seu site usando HTML, CSS e JavaScript, você pode seguir estes passos:

Criar o arquivo Markdown: Crie um arquivo com a extensão .md e escreva seu conteúdo em Markdown.
Incluir o arquivo Markdown no HTML: Use a tag <script> para incluir um arquivo JavaScript que irá converter o Markdown em HTML.
Converter o Markdown em HTML: Utilize uma biblioteca JavaScript como o Marked.js para converter o conteúdo Markdown em HTML.
Exibir o HTML: Insira o HTML gerado pela conversão dentro de um elemento HTML, como uma <div>.
Exemplo:

index.html:

xml
Copiar código

```html
<!DOCTYPE html>
<html>
<head>
  <title>Meu Site de Teste</title>
  <link rel="stylesheet" href="style.css">
</head>
<body>
  <div id="markdown-content"></div>

  <script src="https://cdnjs.cloudflare.com/ajax/libs/marked/3.0.4/marked.min.js"></script>
  <script src="script.js"></script>
</body>
</html>
```

script.js:

javascript
Copiar código

```javascript
const markdownContent = document.getElementById('markdown-content');

fetch('meu-arquivo.md')
  .then(response => response.text())
  .then(markdown => {
    const html = marked(markdown);
    markdownContent.innerHTML = html;
  });
```

## meu-arquivo.md:

markdown
Copiar código
# Título do meu arquivo Markdown

Este é um exemplo de conteúdo em Markdown.

* Item 1
* Item 2
* Item 3
style.css:

css
Copiar código
/* Estilos para o conteúdo Markdown */
#markdown-content {
  font-family: sans-serif;
  line-height: 1.6;
}
Explicando o código:

index.html:
Inclui o arquivo style.css para aplicar estilos ao conteúdo Markdown.
Inclui o arquivo script.js que contém o código JavaScript para converter o Markdown.
Cria uma <div> com o ID markdown-content para exibir o conteúdo convertido.
script.js:
Seleciona o elemento markdown-content usando document.getElementById().
Utiliza fetch() para buscar o conteúdo do arquivo meu-arquivo.md.
Converte o conteúdo Markdown em HTML usando marked().
Define o conteúdo HTML da <div> markdown-content usando innerHTML.
meu-arquivo.md:
Contém o conteúdo em Markdown que será convertido.
style.css:
Define estilos para o conteúdo Markdown, como fonte e espaçamento entre linhas.
Lembre-se:

## Você precisa incluir o arquivo marked.min.js na sua página HTML. Você pode baixá-lo do [site oficial do Marked.js](https://marked.js.org/) ou usar a versão CDN

Você pode personalizar os estilos do conteúdo Markdown no arquivo style.css de acordo com suas necessidades.
Exercício:

Tente criar um arquivo Markdown com um título, uma lista e um parágrafo. Depois, inclua esse arquivo no seu site e use o código JavaScript para exibir o conteúdo convertido.

Dica:

Você pode usar o [site](https://marked.js.org/) para testar seu código Markdown e ver como ele será renderizado.

Espero que isso ajude!

=========================================================================================================

no arquivo script.js, posso usar o async e await? se possivel como ficaria

Que bom que você está explorando o uso de async e await! Sim, você pode usar async e await no seu script.js para tornar o código mais legível e fácil de entender.

### Veja como ficaria o código com async e await:

```javascript
const markdownContent = document.getElementById('markdown-content');

async function renderMarkdown() {
  try {
    const response = await fetch('meu-arquivo.md');
    const markdown = await response.text();
    const html = marked(markdown);
    markdownContent.innerHTML = html;
  } catch (error) {
    console.error('Erro ao carregar o arquivo Markdown:', error);
  }
}

renderMarkdown();
```

## Explicando o código:

async function renderMarkdown(): A função renderMarkdown() é declarada como async, o que significa que ela pode usar await.
await fetch('meu-arquivo.md'): O await é usado para esperar que a promessa retornada por fetch() seja resolvida. Isso significa que o código irá pausar até que o arquivo Markdown seja carregado.
await response.text(): Da mesma forma, await é usado para esperar que a promessa retornada por response.text() seja resolvida. Isso significa que o código irá pausar até que o conteúdo do arquivo Markdown seja convertido em texto.
try...catch: O bloco try...catch é usado para lidar com erros que podem ocorrer durante o carregamento do arquivo Markdown. Se ocorrer um erro, a mensagem de erro será exibida no console.
renderMarkdown(): A função renderMarkdown() é chamada para iniciar o processo de renderização do Markdown.
Vantagens de usar async e await:

Código mais legível: O código fica mais fácil de entender, pois você pode escrever código síncrono que parece assíncrono.
Melhor tratamento de erros: O bloco try...catch permite lidar com erros de forma mais organizada.
Melhor desempenho: O código pode ser otimizado para melhor desempenho, pois o JavaScript pode executar outras tarefas enquanto espera que as promessas sejam resolvidas.
Exercício:

Tente modificar o código para que, em caso de erro, seja exibida uma mensagem amigável para o usuário no lugar do console.

Dica:

Você pode usar o alert() para exibir uma mensagem para o usuário.

Espero que isso ajude!
