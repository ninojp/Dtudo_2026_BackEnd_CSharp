const configuredBffBaseUrl = import.meta.env?.VITE_BFF_BASE_URL;

export class BffRequestError extends Error {
    constructor(message, status, data = null) {
        super(message);
        this.name = 'BffRequestError';
        this.status = status;
        this.data = data;
    }
}

function getBffBaseUrl() {
    if (configuredBffBaseUrl) {
        return configuredBffBaseUrl;
    }

    if (typeof window !== 'undefined') {
        return window.location.origin;
    }

    return 'https://localhost:7120';
}

export function buildBffUrl(path) {
    const baseUrl = getBffBaseUrl().replace(/\/+$/, '');
    return new URL(path.replace(/^\/+/, ''), `${baseUrl}/`).toString();
}

export function getSafeReturnPath(candidate = '/') {
    if (typeof candidate !== 'string'
        || !candidate.startsWith('/')
        || candidate.startsWith('//')
        || candidate.includes('\\')) {
        return '/';
    }

    return candidate.split('#', 1)[0] || '/';
}

export function submitBffPostNavigation(path, fields = {}) {
    if (typeof document === 'undefined') {
        throw new Error('A navegacao BFF exige um documento do navegador.');
    }

    const form = document.createElement('form');
    form.method = 'post';
    form.action = buildBffUrl(path);
    form.style.display = 'none';

    for (const [name, value] of Object.entries(fields)) {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        form.appendChild(input);
    }

    document.body.appendChild(form);
    form.submit();
}

function publishSessionExpiredEvent() {
    if (typeof window !== 'undefined') {
        window.dispatchEvent(new Event('dtudo:bff-session-expired'));
    }
}

export async function requestBff(path, options = {}) {
    const headers = new Headers(options.headers);
    headers.set('Accept', 'application/json');

    const response = await fetch(buildBffUrl(path), {
        ...options,
        credentials: 'include',
        headers,
    });

    const contentType = response.headers.get('content-type') || '';
    const data = contentType.includes('application/json')
        ? await response.json().catch(() => null)
        : null;

    if (response.status === 401 || response.status === 403) {
        publishSessionExpiredEvent();
    }

    if (!response.ok) {
        throw new BffRequestError(
            data?.error || data?.message || 'Nao foi possivel concluir a requisicao.',
            response.status,
            data,
        );
    }

    return data;
}
