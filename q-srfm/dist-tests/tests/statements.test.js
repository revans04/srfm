import { strict as assert } from 'node:assert';
import { test } from 'node:test';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore explicit .js import for Node after compilation
import { toStatementFinalizeRequestBody } from '../src/utils/statements.js';
test('toStatementFinalizeRequestBody prefers importedTransactionIds when provided', () => {
    const payload = {
        familyId: 'fam',
        accountId: 'acct',
        statementId: 'stmt',
        startDate: '2026-01-01',
        endDate: '2026-01-31',
        beginningBalance: 100,
        endingBalance: 125,
        importedTransactionIds: ['imp-1'],
        matchedTransactionIds: ['legacy-1'],
        budgetTransactionRefs: [{ budgetId: 'b1', transactionId: 't1' }],
    };
    const request = toStatementFinalizeRequestBody(payload);
    assert.deepEqual(request.importedTransactionIds, ['imp-1']);
    assert.deepEqual(request.budgetTransactionRefs, [{ budgetId: 'b1', transactionId: 't1' }]);
});
test('toStatementFinalizeRequestBody falls back to matchedTransactionIds', () => {
    const payload = {
        familyId: 'fam',
        accountId: 'acct',
        startDate: '2026-01-01',
        endDate: '2026-01-31',
        beginningBalance: 100,
        endingBalance: 125,
        matchedTransactionIds: ['legacy-1', 'legacy-2'],
    };
    const request = toStatementFinalizeRequestBody(payload);
    assert.deepEqual(request.importedTransactionIds, ['legacy-1', 'legacy-2']);
    assert.deepEqual(request.budgetTransactionRefs, []);
});
