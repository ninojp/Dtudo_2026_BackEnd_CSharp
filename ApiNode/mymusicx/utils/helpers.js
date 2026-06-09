// Funções auxiliares puras

export const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

export const buildHeaders = (token) => {
  const headers = {};
  if (token) headers['Authorization'] = `Discogs token=${token}`;
  return headers;
};

// Classificação baseada em texto de formato
export const classifyFromFormat = (formatText, role) => {
  if (!formatText) return null;
  const f = formatText.toLowerCase();
  if (role === 'video' || f.includes('video') || f.includes('dvd') || f.includes('vhs') || f.includes('blu-ray')) return 'video';
  if (/compilation|compilação|coletânea|\bcomp\b|,\s*comp($|,)|anthology|antologia/.test(f)) return 'compilation';
  if (f.includes('single') || /\bep\b/.test(f) || f.includes('maxi') || f.includes('7"') || f.includes('12"') || f.includes('cass') || f.includes('cassette')) return 'singleep';
  if (f.includes('album') || f.includes('lp')) return 'album';
  return null;
};

// Classificação baseada em título
export const classifyTitle = (title) => {
  const t = (title || '').toLowerCase();
  if (/compilation|compilação|coletânea|seleção essencial|best of|greatest hits|anthology|antologia|raridades|mega hits|perfil|quatro em um|sem limite|o melhor de|o melhor do|o melhor da|\d+\s+música/.test(t)) return 'compilation';
  if (/single|\bep\b|maxi|remix/.test(t)) return 'singleep';
  return null;
};

// Score para deduplicação
export const scoreItem = (item) => {
  let score = 0;
  if (item.type && item.type !== 'master') score += 2;
  if (item.format || item.formats) score += 2;
  if ((item.role || '').toLowerCase() === 'main') score += 1;
  return score;
};
