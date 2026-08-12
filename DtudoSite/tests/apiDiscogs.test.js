import assert from 'node:assert/strict';
import test from 'node:test';
import {
    ApiDiscogsRequestError,
    getApiDiscogsErrorMessage,
    getDiscogsImageUrl,
    getDiscogsRelease,
    listDiscogsArtistReleases,
    searchDiscogsArtists,
} from '../src/services/apiDiscogs.js';

const originalFetch = globalThis.fetch;

test.afterEach(() => {
    globalThis.fetch = originalFetch;
});

test('consulta artistas pela rota do gateway sem enviar bearer ao browser', async () => {
    const requests = [];
    globalThis.fetch = async (input, options) => {
        requests.push({ url: String(input), options });
        return new Response(JSON.stringify({
            source: 'Discogs',
            items: [{ source: { id: '42' }, name: 'Artista' }],
            pagination: { page: 1, perPage: 10, totalItems: 1, totalPages: 1, hasNextPage: false },
            isComplete: true,
            warnings: [],
        }), {
            status: 200,
            headers: { 'content-type': 'application/json' },
        });
    };

    const response = await searchDiscogsArtists({ query: 'Artista' });

    assert.equal(response.items[0].name, 'Artista');
    assert.equal(requests.length, 1);
    assert.equal(requests[0].options.credentials, 'include');
    assert.equal(requests[0].options.headers.Authorization, undefined);
    assert.match(requests[0].url, /\/api\/external\/discogs\/artists\/search\?q=Artista&page=1&perPage=10$/);
});

test('consulta discografia e detalhes usando os caminhos normalizados da ApiDiscogs', async () => {
    const urls = [];
    globalThis.fetch = async (input) => {
        urls.push(String(input));
        const body = urls.length === 1
            ? {
                source: 'Discogs',
                artist: { id: '42', name: 'Artista' },
                items: [],
                pagination: { page: 1, perPage: 50, totalItems: 0, totalPages: 1, hasNextPage: false },
                isComplete: true,
                warnings: [],
            }
            : { source: { id: '99' }, title: 'Release', images: [] };
        return new Response(JSON.stringify(body), {
            status: 200,
            headers: { 'content-type': 'application/json' },
        });
    };

    await listDiscogsArtistReleases('42');
    await getDiscogsRelease('99');

    assert.match(urls[0], /\/api\/external\/discogs\/artists\/42\/releases\?page=1&perPage=50&expand=none$/);
    assert.match(urls[1], /\/api\/external\/discogs\/releases\/99$/);
});

test('mapeia falhas externas sem expor o payload bruto como sucesso', async () => {
    for (const [status, expectedText] of [
        [429, 'limite'],
        [502, 'invalida'],
        [503, 'indisponivel'],
        [504, 'demorou'],
    ]) {
        globalThis.fetch = async () => new Response(null, { status });

        await assert.rejects(
            () => getDiscogsRelease('99'),
            error => error instanceof ApiDiscogsRequestError && error.status === status,
        );
        assert.match(getApiDiscogsErrorMessage({ status }).toLowerCase(), new RegExp(expectedText));
    }
});

test('aceita somente imagens HTTPS devolvidas pelo contrato externo', () => {
    assert.equal(getDiscogsImageUrl({ imageUrl: 'https://img.example/release.jpg' }), 'https://img.example/release.jpg');
    assert.equal(getDiscogsImageUrl({ thumbnailUrl: 'http://img.example/release.jpg' }), null);
    assert.equal(getDiscogsImageUrl({ imageUrl: 'not a url' }), null);
});
