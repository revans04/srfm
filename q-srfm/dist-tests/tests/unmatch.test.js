import { strict as assert } from 'node:assert';
import { test } from 'node:test';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore explicit .js import for Node after compilation
import { buildUnmatchImportedPath } from '../src/utils/unmatch.js';
test('buildUnmatchImportedPath keeps a composite docId-txId id in a single segment', () => {
    const id = '11111111-2222-3333-4444-555555555555-aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
    assert.equal(buildUnmatchImportedPath(id), `budget/imported-transactions/${id}/unmatch`);
    // The composite id must not be split across path segments.
    assert.equal(buildUnmatchImportedPath(id).split('/').length, 4);
});
test('buildUnmatchImportedPath encodes URL-significant characters', () => {
    // Defensive: ids should be hex+dashes, but a stray slash/space/percent must
    // never break out of the path segment or it would hit the wrong route.
    assert.equal(buildUnmatchImportedPath('a/b').split('/').length, 4);
    assert.ok(buildUnmatchImportedPath('a/b').includes('a%2Fb'));
    assert.ok(buildUnmatchImportedPath('a b').includes('a%20b'));
    assert.ok(buildUnmatchImportedPath('a%b').includes('a%25b'));
});
test('buildUnmatchImportedPath has no leading slash and the expected shape', () => {
    const path = buildUnmatchImportedPath('imp-1');
    assert.equal(path.startsWith('/'), false);
    assert.match(path, /^budget\/imported-transactions\/.+\/unmatch$/);
});
