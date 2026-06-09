/* eslint-disable no-undef */
import 'dotenv/config';
import express from 'express';
import axios from 'axios';
import fs from 'fs';
import path from 'path';
import { buildHeaders } from './utils/helpers.js';
import { getCached, setCache } from './utils/cache.js';
import { searchArtistReleases } from './services/discogsSearch.js';

const app = express();
const PORT = process.env.PORT || 4000;
const DISCOGS_TOKEN = process.env.DISCOGS_TOKEN || process.env.VITE_DISCOGS_TOKEN;

app.use(express.json());
// Simples CORS e preflight handling
app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', '*');
  res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept, Authorization');
  res.header('Access-Control-Allow-Methods', 'GET,POST,PUT,DELETE,OPTIONS');
  if (req.method === 'OPTIONS') return res.sendStatus(200);
  next();
});
// Busca no Endpoint pelo artista
app.get('/api/discogs/artists', async (req, res) => {
  try {
    const params = Object.assign({}, req.query);
    const headers = buildHeaders(DISCOGS_TOKEN);

    const artistSearchParams = {
      q: params.q,
      type: 'artist',
      per_page: 10, // Limitar a 10 resultados para a lista suspensa
    };
    // Buscar artistas no Discogs, ordenados por relevância
    const artistResponse = await axios.get('https://api.discogs.com/database/search', { params: artistSearchParams, headers });
    const artistResults = artistResponse.data.results || [];
    // Retornar lista de artistas com id e title
    const artists = artistResults.map(result => ({
      id: result.id,
      title: result.title,
    }));
    res.status(200).json({ artists });
  } catch (err) {
    console.error('Discogs artists search error:', err.message || err);
    if (err.response) {
      res.status(err.response.status).json(err.response.data);
    } else {
      res.status(500).json({ error: 'Proxy error', message: err.message });
    }
  }
});
app.get('/api/discogs/search', async (req, res) => {
  try {
    const artistId = req.query.artistId;
    const selectedArtistName = req.query.artistName || `Artist ${artistId}`;
    
    if (!artistId) {
      return res.status(400).json({ error: 'artistId is required' });
    }

    // Verificar cache
    const cached = getCached(artistId, selectedArtistName);
    if (cached) {
      return res.status(200).json(cached);
    }

    // Buscar releases
    const headers = buildHeaders(DISCOGS_TOKEN);
    const organizedResults = await searchArtistReleases(artistId, selectedArtistName, headers);
    
    // Salvar em cache
    setCache(artistId, selectedArtistName, organizedResults);
    res.status(200).json(organizedResults);
  } catch (err) {
    console.error('Discogs proxy error:', err.message || err, err.stack);
    if (err.response) {
      res.status(err.response.status).json(err.response.data);
    } else {
      res.status(500).json({ error: 'Proxy error', message: err.message });
    }
  }
});

// Detalhes de um release específico
app.get('/api/discogs/release/:id', async (req, res) => {
  try {
    const releaseId = req.params.id;
    
    if (!releaseId) {
      return res.status(400).json({ error: 'Release ID is required' });
    }

    const headers = {};
    if (DISCOGS_TOKEN) headers['Authorization'] = `Discogs token=${DISCOGS_TOKEN}`;

    try {
      // Primeiro tenta buscar como Master Release
      const masterResponse = await axios.get(`https://api.discogs.com/masters/${releaseId}`, { headers });
      
      // Se for master, buscar o main_release para ter todos os detalhes
      if (masterResponse.data.main_release) {
        const releaseResponse = await axios.get(`https://api.discogs.com/releases/${masterResponse.data.main_release}`, { headers });
        return res.status(200).json(releaseResponse.data);
      }
      
      return res.status(200).json(masterResponse.data);
    } catch {
      // Se falhar como master, tenta como release normal
      const releaseResponse = await axios.get(`https://api.discogs.com/releases/${releaseId}`, { headers });
      return res.status(200).json(releaseResponse.data);
    }
  } catch (err) {
    console.error('Discogs release details error:', err.message || err);
    if (err.response) {
      res.status(err.response.status).json(err.response.data);
    } else {
      res.status(500).json({ error: 'Proxy error', message: err.message });
    }
  }
});
// Endpoint para salvar os dados do artista no DB.JSON local (NOVA ESTRUTURA)
app.post('/api/discogs/save', (req, res) => {
  try {
    const { artist, summary } = req.body;
    console.log('[SAVE] Received request with artist:', artist);
    console.log('[SAVE] Summary structure:', summary ? Object.keys(summary) : 'null');
    
    if (!artist || !summary) {
      return res.status(400).json({ error: 'Missing artist or summary data' });
    }

    const dbPath = path.join(process.cwd(), 'api', 'mymusicx', 'artistasDiscografia.json');
    let dbData = { cdsDB: [] };
    
    if (fs.existsSync(dbPath)) {
      const dbContent = fs.readFileSync(dbPath, 'utf8');
      dbData = JSON.parse(dbContent);
    }

    // Função para formatar item para salvar
    const formatReleaseForSave = (item) => {
      if (!item) {
        console.warn('[SAVE] Null or undefined item encountered');
        return null;
      }
      try {
        return {
          master_id: item.type === 'master' ? item.id : (item.master_id || item.id),
          release_id: item.type === 'master' ? item.main_release : item.id,
          title: item.title || 'Unknown',
          year: item.year || null,
          format: item.format || '',
          image: item.main_image || item.thumb || null,
          type: item.type || 'release'
        };
      } catch (err) {
        console.error('[SAVE] Error formatting item:', err, item);
        return null;
      }
    };

    console.log('[SAVE] Processing categories...');
    console.log('[SAVE] Categories keys:', summary.categories ? Object.keys(summary.categories) : 'no categories');

    // Organizar releases por categoria
    const releases = {
      albums: (summary.categories?.albums?.items || []).filter(Boolean).map(formatReleaseForSave).filter(Boolean),
      singles: (summary.categories?.singlesEPs?.items || []).filter(Boolean).map(formatReleaseForSave).filter(Boolean),
      compilations: (summary.categories?.compilations?.items || []).filter(Boolean).map(formatReleaseForSave).filter(Boolean),
      videos: (summary.categories?.videos?.items || []).filter(Boolean).map(formatReleaseForSave).filter(Boolean)
    };

    console.log('[SAVE] Releases organized:', { albums: releases.albums.length, singles: releases.singles.length, compilations: releases.compilations.length, videos: releases.videos.length });

    // Verifica se o artista já existe
    const existingIndex = dbData.cdsDB.findIndex(item => item.artist.toLowerCase() === artist.toLowerCase());
    const artistData = {
      artist,
      releases
    };

    if (existingIndex !== -1) {
      // Atualiza o existente
      dbData.cdsDB[existingIndex] = artistData;
      console.log(`[SAVE] Artista "${artist}" atualizado no banco de dados.`);
    } else {
      // Adiciona um novo
      dbData.cdsDB.push(artistData);
      console.log(`[SAVE] Artista "${artist}" adicionado ao banco de dados.`);
    }

    fs.writeFileSync(dbPath, JSON.stringify(dbData, null, 2));
    
    res.json({ 
      message: 'Dados do artista salvos com sucesso.',
      stats: {
        albums: releases.albums.length,
        singles: releases.singles.length,
        compilations: releases.compilations.length,
        videos: releases.videos.length
      }
    });
  } catch (err) {
    console.error('Save error:', err);
    console.error('Stack:', err.stack);
    res.status(500).json({ error: 'ERRO! ao Salvar dados do artista.', details: err.message });
  }
});

app.listen(PORT, () => {
  console.log(`discogsProxy.js listening on http://localhost:${PORT}`);
});
