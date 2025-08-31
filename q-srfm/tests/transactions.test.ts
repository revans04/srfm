import { strict as assert } from 'node:assert';
import { test } from 'node:test';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore explicit .js import for Node after compilation
import { withinDateWindow, isDuplicate, link, unlink, BudgetTransaction } from '../src/composables/useTransactions.js';
// @ts-ignore explicit .js import for Node after compilation
import { sortBudgetsByMonthDesc } from '../src/utils/budget.js';
import type { Budget } from '../src/types.js';

test('withinDateWindow', () => {
  assert.equal(withinDateWindow('2024-01-01', '2024-01-03', 3), true);
  assert.equal(withinDateWindow('2024-01-01', '2024-01-05', 3), false);
});

test('isDuplicate', () => {
  const base: BudgetTransaction = {
    id: '1',
    date: '2024-01-01',
    merchant: 'Starbucks',
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
  const t2: BudgetTransaction = { ...base, id: '2', merchant: 'Starbucks Store #123' };
  const t3: BudgetTransaction = { ...base, id: '3', merchant: 'Local Cafe' };
  assert.equal(isDuplicate(t1, [t1, t2]), true);
  assert.equal(isDuplicate(t1, [t3]), false);
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

test('sortBudgetsByMonthDesc orders by month descending', () => {
  const budgets: Budget[] = [
    { budgetId: '1', familyId: 'f', label: '', month: '2024-03', incomeTarget: 0, categories: [], transactions: [], merchants: [] },
    { budgetId: '2', familyId: 'f', label: '', month: '2024-01', incomeTarget: 0, categories: [], transactions: [], merchants: [] },
    { budgetId: '3', familyId: 'f', label: '', month: '2023-12', incomeTarget: 0, categories: [], transactions: [], merchants: [] },
  ];
  const sorted = sortBudgetsByMonthDesc(budgets);
  assert.deepEqual(sorted.map((b) => b.month), ['2024-03', '2024-01', '2023-12']);
});
