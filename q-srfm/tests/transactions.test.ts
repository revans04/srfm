import { strict as assert } from 'node:assert';
import { test } from 'node:test';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore explicit .js import for Node after compilation
import { withinDateWindow, isDuplicate, link, unlink, BudgetTransaction } from '../src/composables/useTransactions.js';

test('withinDateWindow', () => {
  assert.equal(withinDateWindow('2024-01-01', '2024-01-03', 3), true);
  assert.equal(withinDateWindow('2024-01-01', '2024-01-05', 3), false);
});

test('isDuplicate', () => {
  const base: BudgetTransaction = {
    id: '1',
    date: '2024-01-01',
    merchant: 'A',
    categories: [],
    amount: -10,
    notes: '',
    recurring: false,
    recurringInterval: 'Monthly',
    userId: 'u',
    isIncome: false,
    taxMetadata: [],
    status: 'U',
  };
  const t1: BudgetTransaction = base;
  const t2: BudgetTransaction = { ...base, id: '2' };
  assert.equal(isDuplicate(t1, [t1, t2]), true);
});

test('link/unlink', () => {
  const t: BudgetTransaction = {
    id: '3',
    date: '2024-01-01',
    merchant: 'B',
    categories: [],
    amount: 5,
    notes: '',
    recurring: false,
    recurringInterval: 'Monthly',
    userId: 'u',
    isIncome: false,
    taxMetadata: [],
    status: 'U',
  };
  link(t, 'imp1');
  assert.equal(t.linkId, 'imp1');
  unlink(t);
  assert.equal(t.linkId, undefined);
});
