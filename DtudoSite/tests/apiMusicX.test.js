import assert from 'node:assert/strict';
import test from 'node:test';
import {
    ApiMusicXRequestError,
    getApiMusicXErrorMessage,
    listAllMusicCollections,
    requestApiMusicX,
} from '../src/services/apiMusicX.js';

const originalFetch = globalThis.fetch;

test.afterEach(() => {
    globalThis.fetch = originalFetch;
});

test('carrega todas as paginas de Colecoes sem enviar bearer ao browser', async () => {
    const requests = [];
    globalThis.fetch = async (input, options) => {
        requests.push({ url: String(input), options });
        const page = new URL(input).searchParams.get('page');
        const body = page === '1'
            ? { items: [{ musicCollectionId: 1 }], page: 1, pageSize: 100, totalCount: 2, totalPages: 2 }
            : { items: [{ musicCollectionId: 2 }], page: 2, pageSize: 100, totalCount: 2, totalPages: 2 };

        return new Response(JSON.stringify(body), {
            status: 200,
            headers: { 'content-type': 'application/json' },
        });
    };

    const response = await listAllMusicCollections();

    assert.deepEqual(response.items.map(item => item.musicCollectionId), [1, 2]);
    assert.equal(requests.length, 2);
    assert.equal(requests[0].options.credentials, 'include');
    assert.equal(requests[0].options.headers.Authorization, undefined);
    assert.match(requests[0].url, /\/api\/catalog\/music\/collections\?page=1&pageSize=100$/);
});

test('normaliza status HTTP da ApiMusicX para a interface', async () => {
    for (const [status, expectedMessage] of [
        [401, 'sessao expirou'],
        [403, 'permissao'],
        [404, 'nao foi encontrado'],
        [500, 'indisponivel'],
    ]) {
        globalThis.fetch = async () => new Response(null, { status });

        await assert.rejects(
            () => requestApiMusicX('/collections/1'),
            error => error instanceof ApiMusicXRequestError && error.status === status,
        );

        await assert.rejects(
            () => requestApiMusicX('/collections/1'),
            error => getApiMusicXErrorMessage(error).toLowerCase().includes(expectedMessage),
        );
    }
});
