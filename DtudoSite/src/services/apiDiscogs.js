const configuredBaseUrl = import.meta.env?.VITE_API_DISCOGS_BASE_URL
    || import.meta.env?.VITE_BFF_BASE_URL;
const configuredPathPrefix = import.meta.env?.VITE_API_DISCOGS_PATH_PREFIX;

const DEFAULT_PATH_PREFIX = '/api/external/discogs';
const DEFAULT_BROWSER_BASE_URL = 'https://localhost:51376';

export class ApiDiscogsRequestError extends Error {
    constructor(message, status, data = null) {
        super(message);
        this.name = 'ApiDiscogsRequestError';
        this.status = status;
        this.data = data;
    }
}

export function getApiDiscogsBaseUrl() {
    if (configuredBaseUrl) {
        return configuredBaseUrl.replace(/\/+$/, '');
    }

    if (typeof window !== 'undefined') {
        return window.location.origin;
    }

    return DEFAULT_BROWSER_BASE_URL;
}

export function getApiDiscogsPathPrefix() {
    const pathPrefix = configuredPathPrefix || DEFAULT_PATH_PREFIX;
    return `/${pathPrefix.replace(/^\/+|\/+$/g, '')}`;
}

export function buildApiDiscogsUrl(path, searchParams) {
    const url = new URL(
        `${getApiDiscogsBaseUrl()}${getApiDiscogsPathPrefix()}${path.startsWith('/') ? path : `/${path}`}`,
    );

    if (searchParams) {
        Object.entries(searchParams).forEach(([key, value]) => {
            if (value !== undefined && value !== null && value !== '') {
                url.searchParams.set(key, String(value));
            }
        });
    }

    return url;
}

export function getApiDiscogsErrorMessage(error) {
    if (error instanceof ApiDiscogsRequestError || Number.isInteger(error?.status)) {
        if (error.status === 401) {
            return 'Sua sessao expirou. Entre novamente para consultar a Discogs.';
        }

        if (error.status === 403) {
            return 'Sua conta nao tem permissao para consultar dados externos.';
        }

        if (error.status === 404) {
            return 'O artista ou release externo nao foi encontrado.';
        }

        if (error.status === 429) {
            return 'A busca externa atingiu o limite temporario. Tente novamente em instantes.';
        }

        if (error.status === 502) {
            return 'A Discogs retornou uma resposta invalida ou indisponivel.';
        }

        if (error.status === 503) {
            return 'A ApiDiscogs esta indisponivel no momento. Tente novamente em instantes.';
        }

        if (error.status === 504) {
            return 'A Discogs demorou para responder. Tente novamente.';
        }

        if (error.status >= 500) {
            return 'A ApiDiscogs esta indisponivel no momento. Tente novamente em instantes.';
        }

        return error.message || 'Nao foi possivel consultar a Discogs.';
    }

    if (error instanceof Error && error.name === 'AbortError') {
        return 'A consulta foi cancelada.';
    }

    if (error instanceof Error && error.message) {
        return error.message;
    }

    return 'Nao foi possivel consultar a Discogs.';
}

async function parseResponseBody(response) {
    const contentType = response.headers.get('content-type') || '';
    if (!contentType.includes('application/json')) {
        return null;
    }

    return response.json().catch(() => null);
}

export async function requestApiDiscogs(path, { signal, searchParams } = {}) {
    const response = await fetch(buildApiDiscogsUrl(path, searchParams), {
        method: 'GET',
        credentials: 'include',
        headers: {
            Accept: 'application/json',
        },
        signal,
    });
    const data = await parseResponseBody(response);

    if (response.status === 401 && typeof window !== 'undefined') {
        window.dispatchEvent(new Event('dtudo:bff-session-expired'));
    }

    if (!response.ok) {
        throw new ApiDiscogsRequestError(
            data?.error || data?.message || data?.detail || getApiDiscogsErrorMessage({ status: response.status }),
            response.status,
            data,
        );
    }

    return data;
}

function validatePagedResponse(data, resourceName) {
    if (!data || !Array.isArray(data.items) || !data.pagination) {
        throw new TypeError(`A ApiDiscogs retornou uma resposta de ${resourceName} invalida.`);
    }

    return data;
}

function validateObjectResponse(data, resourceName) {
    if (!data || typeof data !== 'object' || Array.isArray(data)) {
        throw new TypeError(`A ApiDiscogs retornou uma resposta de ${resourceName} invalida.`);
    }

    return data;
}

export async function searchDiscogsArtists({ query, page = 1, perPage = 10, signal } = {}) {
    const data = await requestApiDiscogs('/artists/search', {
        signal,
        searchParams: { q: query, page, perPage },
    });

    return validatePagedResponse(data, 'artistas externos');
}

export async function getDiscogsArtist(artistId, { signal } = {}) {
    const data = await requestApiDiscogs(`/artists/${encodeURIComponent(artistId)}`, { signal });
    return validateObjectResponse(data, 'artista externo');
}

export async function listDiscogsArtistReleases(
    artistId,
    { page = 1, perPage = 50, expand = 'none', signal } = {},
) {
    const data = await requestApiDiscogs(`/artists/${encodeURIComponent(artistId)}/releases`, {
        signal,
        searchParams: { page, perPage, expand },
    });

    return validatePagedResponse(data, 'discografia externa');
}

export async function getDiscogsRelease(releaseId, { signal } = {}) {
    const data = await requestApiDiscogs(`/releases/${encodeURIComponent(releaseId)}`, { signal });
    return validateObjectResponse(data, 'release externo');
}

export async function getDiscogsMaster(masterId, { signal } = {}) {
    const data = await requestApiDiscogs(`/masters/${encodeURIComponent(masterId)}`, { signal });
    return validateObjectResponse(data, 'master externo');
}

export function getDiscogsImageUrl(resource) {
    const candidate = resource?.imageUrl
        || resource?.thumbnailUrl
        || resource?.images?.find(image => image?.uri)?.uri;
    if (!candidate) {
        return null;
    }

    try {
        const url = new URL(candidate);
        return url.protocol === 'https:' ? url.toString() : null;
    } catch {
        return null;
    }
}
