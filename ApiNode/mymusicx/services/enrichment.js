import axios from 'axios';

// Enriquecimento de masters em paralelo
export const enrichMasters = async (allItems, aggregateMap, headers) => {
  console.log(`[ETAPA 3] Enriquecendo masters...`);
  const stage3Start = Date.now();
  
  // Filtro mais agressivo: pula masters com hints de título
  const mastersNeedingFormats = allItems
    .filter(item => {
      if (item.type !== 'master' || item.format || !item.main_release) return false;
      if ((item.role || '').toLowerCase() !== 'main') return false;
      
      const title = (item.title || '').toLowerCase();
      // Pula enrichment se título tem pistas claras
      if (/single|\bep\b|maxi|remix|remix/.test(title)) return false;
      if (/compilation|compilação|best of|greatest hits/.test(title)) return false;
      return true;
    });
  
  if (mastersNeedingFormats.length > 0) {
    const batchSize = 10; // 10 requisições paralelas
    for (let i = 0; i < mastersNeedingFormats.length; i += batchSize) {
      const batch = mastersNeedingFormats.slice(i, i + batchSize);
      await Promise.all(batch.map(async (master) => {
        try {
          const releaseResponse = await axios.get(`https://api.discogs.com/releases/${master.main_release}`, { headers });
          const formats = releaseResponse.data.formats || [];
          const formatStrings = formats.map(f => {
            const parts = [f.name];
            if (f.descriptions?.length > 0) parts.push(...f.descriptions);
            return parts.join(', ');
          }).join('; ');
          
          if (formatStrings) {
            const entry = aggregateMap.get(master.id) || { formats: new Set(), roles: new Set() };
            entry.formats.add(formatStrings.toLowerCase());
            aggregateMap.set(master.id, entry);
          }
        } catch (err) {
          console.debug(`[DEBUG] Enrichment failed for master ${master.id}`, err.message || err);
        }
      }));
      if (i + batchSize < mastersNeedingFormats.length) {
        await new Promise(resolve => setTimeout(resolve, 400));
      }
    }
  }
  console.log(`[ETAPA 3 ✓] ${mastersNeedingFormats.length} masters enriquecidos | ${Date.now() - stage3Start}ms`);
};
