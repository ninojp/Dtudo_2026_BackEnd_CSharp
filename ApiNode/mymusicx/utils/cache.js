// Gerenciamento de cache em memória

const SEARCH_CACHE = new Map();
const CACHE_TTL_MS = 10 * 60 * 1000; // 10 minutos

export const getCached = (artistId, artistName) => {
  const cacheKey = `${artistId}|${artistName}`;
  const cached = SEARCH_CACHE.get(cacheKey);
  if (cached && (Date.now() - cached.timestamp) < CACHE_TTL_MS) {
    console.log(`[⚡ CACHE HIT] ${artistName}`);
    return cached.data;
  }
  return null;
};

export const setCache = (artistId, artistName, data) => {
  const cacheKey = `${artistId}|${artistName}`;
  SEARCH_CACHE.set(cacheKey, { timestamp: Date.now(), data });
};
