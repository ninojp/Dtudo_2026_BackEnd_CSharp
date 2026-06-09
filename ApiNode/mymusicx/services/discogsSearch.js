import axios from 'axios';
import { sleep, scoreItem } from '../utils/helpers.js';
import { enrichMasters } from './enrichment.js';
import { classifyReleases, categorizeReleases } from './classification.js';

// Busca principal do Discogs
export const searchArtistReleases = async (artistId, artistName, headers) => {
  const startTime = Date.now();
  console.log(`[🔍 SEARCH START] ${artistName} (ID: ${artistId})`);

  // Etapa 1: Buscar todos os lançamentos com paginação
  console.log(`[ETAPA 1] Buscando releases com paginação...`);
  const stage1Start = Date.now();
  let allRawReleases = [];
  let nextUrl = `https://api.discogs.com/artists/${artistId}/releases`;
  
  while (nextUrl) {
    const response = await axios.get(nextUrl, { 
      params: (allRawReleases.length === 0 ? { per_page: 100 } : {}), 
      headers 
    });
    allRawReleases.push(...response.data.releases);
    nextUrl = response.data.pagination.urls.next;
    if (nextUrl) await sleep(200);
  }
  console.log(`[ETAPA 1 ✓] ${allRawReleases.length} releases brutos | ${Date.now() - stage1Start}ms`);

  // Etapa 2: Agregar + Desduplicar + Filtrar unofficial
  console.log(`[ETAPA 2] Agregando, deduplicando e filtrando...`);
  const stage2Start = Date.now();
  
  const aggregateMap = new Map();
  const masterIdMap = new Map();

  for (const release of allRawReleases) {
    // Pular releases com 'unofficial' já aqui
    const fmt = release.format ? String(release.format).toLowerCase() : '';
    if (fmt.includes('unofficial')) continue;

    const key = release.master_id || release.id;
    
    // Agregar: formatos e papéis
    const entry = aggregateMap.get(key) || { formats: new Set(), roles: new Set() };
    if (fmt && fmt !== 'undefined') {
      entry.formats.add(fmt);
    }
    const roleVal = (release.role || '').toLowerCase();
    if (roleVal) entry.roles.add(roleVal);
    aggregateMap.set(key, entry);

    // Desduplicar: manter item com melhor score
    const current = masterIdMap.get(key);
    if (!current || scoreItem(release) > scoreItem(current)) {
      masterIdMap.set(key, release);
    }
  }
  
  const allItems = Array.from(masterIdMap.values());
  console.log(`[ETAPA 2 ✓] ${allItems.length} items únicos | ${Date.now() - stage2Start}ms`);
  
  const getAggregatedFormat = (item) => {
    const key = item.type === 'master' ? item.id : (item.master_id || item.id);
    const entry = aggregateMap.get(key);
    if (!entry) return '';
    return Array.from(entry.formats).join('; ');
  };
  
  const hasMainRoleAggregate = (item) => {
    const key = item.type === 'master' ? item.id : (item.master_id || item.id);
    const entry = aggregateMap.get(key);
    return entry ? entry.roles.has('main') : false;
  };
  
  // Etapa 3: Enriquecimento
  await enrichMasters(allItems, aggregateMap, headers);
  
  // Etapa 4: Classificação
  const enrichedItems = classifyReleases(allItems, hasMainRoleAggregate, getAggregatedFormat);
  const officialItems = enrichedItems;
  
  // Etapa 5: Categorização
  const categorized = categorizeReleases(officialItems);
  
  const totalTime = Date.now() - startTime;
  console.log(`[🎉 SEARCH COMPLETE] ${artistName} | Total: ${officialItems.length} | Tempo: ${totalTime}ms\n`);
  
  const organizedResults = {
    artist: artistName,
    items: officialItems,
    summary: {
      Total: officialItems.length,
      categories: {
        albums: { count: categorized.albums.length, items: categorized.albums },
        singlesEPs: { count: categorized.singlesEPs.length, items: categorized.singlesEPs },
        compilations: { count: categorized.compilations.length, items: categorized.compilations },
        videos: { count: categorized.videos.length, items: categorized.videos }
      }
    }
  };
  
  console.log(`[DISCOGS] Artist: ${artistName} | Total: ${organizedResults.summary.Total} | Albums: ${categorized.albums.length} | Singles/EPs: ${categorized.singlesEPs.length} | Compilations: ${categorized.compilations.length} | Videos: ${categorized.videos.length}`);
  
  return organizedResults;
};
