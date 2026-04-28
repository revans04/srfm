/* eslint-disable @typescript-eslint/no-floating-promises */
// node:test's `test()` and `describe()` return a handle that ESLint sees as
// a floating promise. The runner manages await internally; the existing
// test files in this directory follow the same pattern.
import { strict as assert } from 'node:assert';
import { test, describe } from 'node:test';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore explicit .js import for Node after compilation
import { computeChecklistCompletion } from '../src/utils/onboardingChecklist.js';
import { Timestamp } from 'firebase/firestore';
// ---------------------------------------------------------------------------
// Builders
// ---------------------------------------------------------------------------
function makeBudget(overrides = {}) {
    return {
        budgetId: 'b-1',
        familyId: 'f-1',
        entityId: 'e-1',
        label: 'Test Budget',
        month: '2026-04',
        incomeTarget: 0,
        merchants: [],
        categories: [],
        transactions: [],
        ...overrides,
    };
}
function makeCategory(overrides = {}) {
    return {
        name: 'Misc',
        target: 0,
        isFund: false,
        groupName: 'Other',
        ...overrides,
    };
}
function makeTransaction(overrides = {}) {
    return {
        id: 't-1',
        date: '2026-04-15',
        merchant: 'Store',
        categories: [{ category: 'Misc', amount: 0 }],
        amount: 0,
        notes: '',
        recurring: false,
        recurringInterval: 'Monthly',
        userId: 'u-1',
        isIncome: false,
        taxMetadata: [],
        ...overrides,
    };
}
const NOW = Timestamp.fromDate(new Date('2026-04-21T00:00:00Z'));
function makeAccount(overrides = {}) {
    return {
        id: 'a-1',
        name: 'Checking',
        type: 'Bank',
        category: 'Asset',
        createdAt: NOW,
        updatedAt: NOW,
        ...overrides,
    };
}
function makeFamily(overrides = {}) {
    return {
        id: 'f-1',
        name: 'Test Family',
        ownerUid: 'owner-uid',
        members: [{ uid: 'owner-uid', email: 'owner@example.com', role: 'owner' }],
        accounts: [],
        snapshots: [],
        entities: [],
        ...overrides,
    };
}
// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------
describe('computeChecklistCompletion — empty state', () => {
    test('a brand-new user with no data has zero ticks', () => {
        const result = computeChecklistCompletion({ budgets: [] });
        assert.deepEqual(result, {
            'create-budget': false,
            'enter-transaction': false,
            'import-transactions': false,
            'setup-goal': false,
            'reconcile-account': false,
            'verify-email': false,
            'invite-partner': false,
        });
    });
    test('handles undefined family + null authUser without throwing', () => {
        const result = computeChecklistCompletion({ budgets: [], family: undefined, authUser: null });
        assert.equal(result['verify-email'], false);
        assert.equal(result['invite-partner'], false);
    });
});
describe('computeChecklistCompletion — create-budget', () => {
    test('ticks once any budget exists', () => {
        const result = computeChecklistCompletion({ budgets: [makeBudget()] });
        assert.equal(result['create-budget'], true);
    });
});
describe('computeChecklistCompletion — enter-transaction', () => {
    test('ticks for any non-deleted transaction', () => {
        const budget = makeBudget({ transactions: [makeTransaction()] });
        const result = computeChecklistCompletion({ budgets: [budget] });
        assert.equal(result['enter-transaction'], true);
    });
    test('does NOT tick when the only transaction is soft-deleted', () => {
        const budget = makeBudget({ transactions: [makeTransaction({ deleted: true })] });
        const result = computeChecklistCompletion({ budgets: [budget] });
        assert.equal(result['enter-transaction'], false);
    });
});
describe('computeChecklistCompletion — setup-goal', () => {
    test('ticks for a fund category in any budget', () => {
        const budget = makeBudget({
            categories: [makeCategory({ name: 'Vacation', isFund: true })],
        });
        const result = computeChecklistCompletion({ budgets: [budget] });
        assert.equal(result['setup-goal'], true);
    });
    test('does NOT tick when no category is a fund', () => {
        const budget = makeBudget({
            categories: [makeCategory({ name: 'Groceries', isFund: false })],
        });
        const result = computeChecklistCompletion({ budgets: [budget] });
        assert.equal(result['setup-goal'], false);
    });
});
describe('computeChecklistCompletion — reconcile-account', () => {
    test('ticks for any transaction with status R', () => {
        const budget = makeBudget({ transactions: [makeTransaction({ status: 'R' })] });
        const result = computeChecklistCompletion({ budgets: [budget] });
        assert.equal(result['reconcile-account'], true);
    });
    test('does NOT tick for cleared (status C) or unmatched (U) transactions alone', () => {
        const budget = makeBudget({
            transactions: [
                makeTransaction({ id: 't-c', status: 'C' }),
                makeTransaction({ id: 't-u', status: 'U' }),
            ],
        });
        const result = computeChecklistCompletion({ budgets: [budget] });
        assert.equal(result['reconcile-account'], false);
    });
});
describe('computeChecklistCompletion — import-transactions', () => {
    test('ticks for an imported transaction (importedMerchant set)', () => {
        const budget = makeBudget({
            transactions: [makeTransaction({ importedMerchant: 'AMAZON' })],
        });
        const result = computeChecklistCompletion({ budgets: [budget] });
        assert.equal(result['import-transactions'], true);
    });
    test('ticks for any transaction tagged with an account number', () => {
        const budget = makeBudget({
            transactions: [makeTransaction({ accountNumber: '****1234' })],
        });
        const result = computeChecklistCompletion({ budgets: [budget] });
        assert.equal(result['import-transactions'], true);
    });
    test('falls back to "family has a linked account" signal', () => {
        const family = makeFamily({
            accounts: [makeAccount({ accountNumber: '****5678' })],
        });
        const result = computeChecklistCompletion({ budgets: [], family });
        assert.equal(result['import-transactions'], true);
    });
    test('does NOT fall back when accounts have no accountNumber', () => {
        const family = makeFamily({ accounts: [makeAccount()] });
        const result = computeChecklistCompletion({ budgets: [], family });
        assert.equal(result['import-transactions'], false);
    });
});
describe('computeChecklistCompletion — verify-email', () => {
    test('ticks when authUser.emailVerified is true', () => {
        const result = computeChecklistCompletion({
            budgets: [],
            authUser: { emailVerified: true },
        });
        assert.equal(result['verify-email'], true);
    });
    test('does NOT tick when authUser.emailVerified is false', () => {
        const result = computeChecklistCompletion({
            budgets: [],
            authUser: { emailVerified: false },
        });
        assert.equal(result['verify-email'], false);
    });
    test('does NOT tick when authUser is null', () => {
        const result = computeChecklistCompletion({ budgets: [], authUser: null });
        assert.equal(result['verify-email'], false);
    });
});
describe('computeChecklistCompletion — invite-partner', () => {
    test('does NOT tick when family has just the owner (members.length === 1)', () => {
        const family = makeFamily();
        const result = computeChecklistCompletion({ budgets: [], family });
        assert.equal(result['invite-partner'], false);
    });
    test('ticks when family has at least one accepted invite (members.length >= 2)', () => {
        const members = [
            { uid: 'owner-uid', email: 'owner@example.com', role: 'owner' },
            { uid: 'partner-uid', email: 'partner@example.com', role: 'member' },
        ];
        const family = makeFamily({ members });
        const result = computeChecklistCompletion({ budgets: [], family });
        assert.equal(result['invite-partner'], true);
    });
    test('does NOT tick when family is undefined', () => {
        const result = computeChecklistCompletion({ budgets: [], family: undefined });
        assert.equal(result['invite-partner'], false);
    });
});
describe('computeChecklistCompletion — composite scenarios', () => {
    test('a fully-onboarded user has all 7 items ticked', () => {
        const partner = { uid: 'partner-uid', email: 'p@example.com', role: 'member' };
        const family = makeFamily({
            members: [
                { uid: 'owner-uid', email: 'owner@example.com', role: 'owner' },
                partner,
            ],
            accounts: [makeAccount({ accountNumber: '****1111' })],
        });
        const budget = makeBudget({
            categories: [makeCategory({ name: 'Vacation', isFund: true })],
            transactions: [
                makeTransaction({ status: 'R', importedMerchant: 'BANK' }),
            ],
        });
        const result = computeChecklistCompletion({
            budgets: [budget],
            family,
            authUser: { emailVerified: true },
        });
        for (const key of Object.keys(result)) {
            assert.equal(result[key], true, `${key} should be true`);
        }
    });
    test('a freshly-seeded user has create-budget but nothing else (until they verify + invite + reconcile + tag a fund)', () => {
        const family = makeFamily();
        const budget = makeBudget({
            categories: [makeCategory({ name: 'Income', groupName: 'Income' })],
        });
        const result = computeChecklistCompletion({
            budgets: [budget],
            family,
            authUser: { emailVerified: false },
        });
        assert.equal(result['create-budget'], true);
        assert.equal(result['enter-transaction'], false);
        assert.equal(result['import-transactions'], false);
        assert.equal(result['setup-goal'], false);
        assert.equal(result['reconcile-account'], false);
        assert.equal(result['verify-email'], false);
        assert.equal(result['invite-partner'], false);
    });
});
