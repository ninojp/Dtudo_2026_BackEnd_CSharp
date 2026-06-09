Master_ID (430849)
✅ Agrupa todas as versões de um álbum
✅ Tem info geral: título, ano, artista, imagem
❌ NÃO tem tracklist detalhada
Exemplo: "Holocausto Urbano" (agrupa Vinil, CD, etc.)
Release_ID (8354952)
✅ Uma versão específica do Master
✅ Tem TUDO: tracklist completa, duração de cada faixa, formato específico
✅ É o que você precisa para exibir as faixas
Exemplo: "Holocausto Urbano" em Vinil LP específico


Perguntar a IA:
VAMOS PASSO A PASSO! PRIMEIRO!
Como disse antes minha estrutura de dadoscdsDB, mymusicx, discogsProxy, está com ERRO! Vamos refazer toda a estrutura, para depois voltar a exibir os dados! Vamos analizar os dados exibidos no site da API official Discogs. Quando busco por Raciosnais Mc's: https://www.discogs.com/pt_BR/artist/131139-Racionais-MCs?superFilter=Releases. 
O site me exibe os seguintes dados: Releases 17, Albums 8 (1991 Holocausto Urbano '12 versões', 1993 Raio X Brasil '8 versões', 1997 Sobrevivendo no Inferno '20 versões' ....), Singles & EPs 5, Compilations 4.
Ou seja, os releases são agrupados em versões diferentes do mesmo álbum. Agora veja que ao buscar: https://www.discogs.com/master/430849-Racionais-MCs-Holocausto-Urbano. Temos as informações CORRETAS sobre o release Holocausto Urbano, com todas as versões listadas. Então, para refazer a estrutura de dados, preciso que você me ajude a entender como organizar os dados e salva-los no arquivo cdsDB.json (primeiro vamos apaga-lo, e refaze-lo) corretamente, com todas as informações dos artistas, releases, álbuns, singles e EPs, considerando as diferentes versões (mas registrando na minha base de dados cdsDB.json apenas a versão 'master' ou principal) de cada lançamento.
Me pergunte qualquer coisa que precise para me ajudar a refazer essa estrutura de dados corretamente!

Resposta busca releases: {artist: "Racionais MC's", items: Array(17), summary: {…}}
MyMusicXBuscar.jsx:90 Salvando dados para: Racionais MC's
MyMusicXBuscar.jsx:91 Data structure: {artist: "Racionais MC's", summary: {…}}
MyMusicXBuscar.jsx:105 Sending payload: {
  "artist": "Racionais MC's",
  "summary": {
    "Total": 17,
    "categories": {
      "albums": {
        "count": 8,
        "items": [
          {
            "id": 430849,
            "title": "Holocausto Urbano",
            "type": "master",
            "main_release": 8354952,
            "artist": "Racionais MC's",
            "role": "Main",
            "resource_url": "https://api.discogs.com/masters/430849",
            "year": 1990,
            "thumb": "https://i.discogs.com/6cmh
MyMusicXBuscar.jsx:107 Save response: {message: 'Dados do artista salvos com sucesso.', stats: {…}}
