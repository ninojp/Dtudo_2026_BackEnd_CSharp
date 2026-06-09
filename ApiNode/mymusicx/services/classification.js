import { classifyFromFormat, classifyTitle } from '../utils/helpers.js';

// Classificação de releases
export const classifyReleases = (allItems, hasMainRoleAggregate, getAggregatedFormat) => {
  console.log(`[ETAPA 4] Classificando ${allItems.length} releases...`);
  const stage4Start = Date.now();

  const enrichedItems = [];

  for (const item of allItems) {
    const role = (item.role || '').toLowerCase();
    
    // Filtrar: manter apenas items com role='main' OU que aggregado tem 'main'
    if (role !== 'main' && !hasMainRoleAggregate(item)) continue;
    
    const aggregatedFormat = getAggregatedFormat(item);
    const formatText = (item.format && String(item.format).trim()) ? String(item.format) : aggregatedFormat;
    let category = null;

    // 1. Checar role='video'
    if (role === 'video') {
      category = 'video';
    } else {
      // 2. Tentar formato
      category = classifyFromFormat(formatText, role);
      
      // 3. Se falhar, tentar título
      if (!category) {
        category = classifyTitle(item.title);
      }
      
      // 4. Fallback
      if (!category) {
        // Se é release com formato vazio → single
        if (item.type !== 'master' && !formatText) {
          category = 'singleep';
        } else {
          category = 'album'; // Masters sem pistas → album
        }
      }
    }

    enrichedItems.push({ ...item, format: formatText, _category: category });
  }
  
  console.log(`[ETAPA 4 ✓] ${enrichedItems.length} releases classificados | ${Date.now() - stage4Start}ms`);
  return enrichedItems;
};

// Categorização final
export const categorizeReleases = (officialItems) => {
  console.log(`[ETAPA 5] Categorizando por tipo...`);
  const stage5Start = Date.now();
  
  const categorized = {
    albums: [], singlesEPs: [], compilations: [], videos: [],
  };

  officialItems.forEach(item => {
    const role = (item.role || '').toLowerCase();
    const formatText = (item.format || '').toLowerCase();
    const finalCategory = item._category || classifyFromFormat(formatText, role) || null;

    const itemWithMasterId = {
      ...item,
      master_release_id: item.master_id || item.id
    };

    if (finalCategory === 'video') {
      categorized.videos.push(itemWithMasterId);
    } else if (finalCategory === 'compilation') {
      categorized.compilations.push(itemWithMasterId);
    } else if (finalCategory === 'album') {
      categorized.albums.push(itemWithMasterId);
    } else if (finalCategory === 'singleep') {
      categorized.singlesEPs.push(itemWithMasterId);
    } else {
      categorized.singlesEPs.push(itemWithMasterId);
    }
  });
  
  console.log(`[ETAPA 5 ✓] Albums: ${categorized.albums.length} | Singles: ${categorized.singlesEPs.length} | Compilações: ${categorized.compilations.length} | Vídeos: ${categorized.videos.length} | ${Date.now() - stage5Start}ms`);
  return categorized;
};
