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
  const t1: BudgetTransaction = {
    id: '1',
    date: '2024-01-01',
    payee: 'A',
    categoryId: 'c1',
    entityId: 'e1',
    budgetId: 'b1',
    amount: -10,
    status: 'U',
  };
  const t2: BudgetTransaction = { ...t1, id: '2' };
  assert.equal(isDuplicate(t1, [t1, t2]), true);
});

test('link/unlink', () => {
  const t: BudgetTransaction = {
    id: '3',
    date: '2024-01-01',
    payee: 'B',
    categoryId: 'c1',
    entityId: 'e1',
    budgetId: 'b1',
    amount: 5,
    status: 'U',
  };
  link(t, 'imp1');
  assert.equal(t.linkId, 'imp1');
  unlink(t);
  assert.equal(t.linkId, undefined);
});
