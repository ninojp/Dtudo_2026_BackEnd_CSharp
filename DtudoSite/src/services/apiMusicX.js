const configuredBaseUrl = import.meta.env?.VITE_API_MUSICX_BASE_URL
    || import.meta.env?.VITE_BFF_BASE_URL;
const configuredPathPrefix = import.meta.env?.VITE_API_MUSICX_PATH_PREFIX;

const DEFAULT_PATH_PREFIX = '/api/catalog/music';
const DEFAULT_BROWSER_BASE_URL = 'https://localhost:51376';

export class ApiMusicXRequestError extends Error {
    constructor(message, status, data = null) {
        super(message);
        this.name = 'ApiMusicXRequestError';
        this.status = status;
        this.data = data;
    }
}

export function getApiMusicXBaseUrl() {
    if (configuredBaseUrl) {
        return configuredBaseUrl.replace(/\/+$/, '');
    }

    if (typeof window !== 'undefined') {
        return window.location.origin;
    }

    return DEFAULT_BROWSER_BASE_URL;
}

export function getApiMusicXPathPrefix() {
    const pathPrefix = configuredPathPrefix || DEFAULT_PATH_PREFIX;
    return `/${pathPrefix.replace(/^\/+|\/+$/g, '')}`;
}

export function buildApiMusicXUrl(path, searchParams) {
    const url = new URL(
        `${getApiMusicXBaseUrl()}${getApiMusicXPathPrefix()}${path.startsWith('/') ? path : `/${path}`}`,
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

export function getApiMusicXErrorMessage(error) {
    if (error instanceof ApiMusicXRequestError || Number.isInteger(error?.status)) {
        if (error.status === 401) {
            return 'Sua sessao expirou. Entre novamente para consultar a Colecao.';
        }

        if (error.status === 403) {
            return 'Sua conta nao tem permissao para consultar a Colecao local.';
        }

        if (error.status === 404) {
            return 'A Colecao ou o release solicitado nao foi encontrado.';
        }

        if (error.status >= 500) {
            return 'A ApiMusicX esta indisponivel no momento. Tente novamente em instantes.';
        }

        return error.message || 'Nao foi possivel consultar a Colecao local.';
    }

    if (error instanceof Error && error.name === 'AbortError') {
        return 'A consulta foi cancelada.';
    }

    if (error instanceof Error && error.message) {
        return error.message;
    }

    return 'Nao foi possivel consultar a Colecao local.';
}

async function parseResponseBody(response) {
    const contentType = response.headers.get('content-type') || '';
    if (!contentType.includes('application/json')) {
        return null;
    }

    return response.json().catch(() => null);
}

export async function requestApiMusicX(path, { signal, searchParams } = {}) {
    const response = await fetch(buildApiMusicXUrl(path, searchParams), {
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
        throw new ApiMusicXRequestError(
            data?.error || data?.message || data?.detail || getApiMusicXErrorMessage({ status: response.status }),
            response.status,
            data,
        );
    }

    return data;
}

function validatePagedResponse(data, resourceName) {
    if (!data || !Array.isArray(data.items)) {
        throw new TypeError(`A ApiMusicX retornou uma resposta de ${resourceName} invalida.`);
    }

    return data;
}

function validateObjectResponse(data, resourceName) {
    if (!data || typeof data !== 'object' || Array.isArray(data)) {
        throw new TypeError(`A ApiMusicX retornou uma resposta de ${resourceName} invalida.`);
    }

    return data;
}

export async function listMusicCollections({ page = 1, pageSize = 100, search, signal } = {}) {
    const data = await requestApiMusicX('/collections', {
        signal,
        searchParams: { page, pageSize, search },
    });

    return validatePagedResponse(data, 'Colecoes');
}

export async function listAllMusicCollections({ search, signal } = {}) {
    const firstPage = await listMusicCollections({ page: 1, pageSize: 100, search, signal });
    const items = [...firstPage.items];
    const totalPages = Number.isInteger(firstPage.totalPages)
        ? firstPage.totalPages
        : Math.ceil((firstPage.totalCount || items.length) / (firstPage.pageSize || 100));

    for (let page = 2; page <= totalPages; page += 1) {
        const nextPage = await listMusicCollections({ page, pageSize: 100, search, signal });
        items.push(...nextPage.items);
    }

    return {
        ...firstPage,
        items,
        totalCount: firstPage.totalCount ?? items.length,
    };
}

export async function getMusicCollection(collectionId, { signal } = {}) {
    const data = await requestApiMusicX(`/collections/${encodeURIComponent(collectionId)}`, { signal });
    return validateObjectResponse(data, 'Colecao');
}

export async function listMusicCollectionReleases(collectionId, { page = 1, pageSize = 100, signal } = {}) {
    const data = await requestApiMusicX(`/collections/${encodeURIComponent(collectionId)}/releases`, {
        signal,
        searchParams: { page, pageSize },
    });

    return validatePagedResponse(data, 'releases da Colecao');
}

export async function searchMusicArtists({ search, page = 1, pageSize = 20, signal } = {}) {
    const data = await requestApiMusicX('/artists', {
        signal,
        searchParams: { search, page, pageSize },
    });

    return validatePagedResponse(data, 'artistas');
}

export async function getMusicArtist(artistId, { signal } = {}) {
    const data = await requestApiMusicX(`/artists/${encodeURIComponent(artistId)}`, { signal });
    return validateObjectResponse(data, 'artista');
}

export async function getMusicRelease(releaseId, { signal } = {}) {
    const data = await requestApiMusicX(`/releases/${encodeURIComponent(releaseId)}`, { signal });
    return validateObjectResponse(data, 'release');
}
