import { strict as assert } from 'node:assert';
import { test, describe } from 'node:test';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore explicit .js import for Node after compilation
import { computeCategoryAverages } from '../src/utils/reportAggregations.js';
import type { Budget, BudgetCategory, BudgetGroup, Transaction } from '../src/types.js';

const groups: BudgetGroup[] = [
  { id: 'g-inc', entityId: 'e-1', name: 'Income', sortOrder: 0, archived: false, kind: 'income' },
  { id: 'g-exp', entityId: 'e-1', name: 'Expenses', sortOrder: 1, archived: false, kind: 'expense' },
  { id: 'g-sav', entityId: 'e-1', name: 'Savings', sortOrder: 2, archived: false, kind: 'savings' },
];

function makeBudget(overrides: Partial<Budget> & { categories: BudgetCategory[]; transactions: Transaction[] }): Budget {
  return {
    budgetId: 'b-1',
    familyId: 'f-1',
    entityId: 'e-1',
    label: 'Test',
    month: '2025-01',
    incomeTarget: 0,
    merchants: [],
    ...overrides,
  };
}

function makeTransaction(overrides: Partial<Transaction> & { categories: { category: string; amount: number }[] }): Transaction {
  return {
    id: 't-1',
    date: '2025-01-15',
    merchant: 'Store',
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

const noExclusions = { groups: new Set<string>(), categories: new Set<string>() };

describe('computeCategoryAverages', () => {
  test('classifies a standard expense into expenseTotal', () => {
    const budget = makeBudget({
      categories: [{ name: 'Groceries', target: 0, isFund: false, groupId: 'g-exp', groupName: 'Expenses' }],
      transactions: [
        makeTransaction({
          id: 't-1',
          merchant: 'Market',
          categories: [{ category: 'Groceries', amount: 100 }],
          amount: 100,
          isIncome: false,
        }),
      ],
    });
    const result = computeCategoryAverages([budget], groups, noExclusions);
    const groceries = result.categoryAverages.find((c) => c.name === 'Groceries');
    assert.equal(groceries?.avgIncome, 0);
    assert.equal(groceries?.avgExpense, 100);
    const drill = result.categoryTransactions['Groceries'];
    assert.equal(drill?.length, 1);
    assert.equal(drill?.[0]?.amount, -100);
    assert.equal(drill?.[0]?.isIncome, false);
  });

  test('classifies a standard income into incomeTotal', () => {
    const budget = makeBudget({
      categories: [{ name: 'Salary', target: 0, isFund: false, groupId: 'g-inc', groupName: 'Income' }],
      transactions: [
        makeTransaction({
          id: 't-2',
          merchant: 'Acme',
          categories: [{ category: 'Salary', amount: 500 }],
          amount: 500,
          isIncome: true,
        }),
      ],
    });
    const result = computeCategoryAverages([budget], groups, noExclusions);
    const salary = result.categoryAverages.find((c) => c.name === 'Salary');
    assert.equal(salary?.avgIncome, 500);
    assert.equal(salary?.avgExpense, 0);
  });

  test('skips transfer splits from totals; preserves them in drill-down with signed amounts', () => {
    const budget = makeBudget({
      categories: [
        { name: 'Vacation Fund', target: 0, isFund: true, groupId: 'g-sav', groupName: 'Savings' },
        { name: 'Travel', target: 0, isFund: false, groupId: 'g-exp', groupName: 'Expenses' },
      ],
      transactions: [
        makeTransaction({
          id: 't-3',
          merchant: 'Transfer',
          // Form saves transfers with both splits sharing transaction.isIncome=false
          // and amounts already signed: source negative, destination positive.
          categories: [
            { category: 'Vacation Fund', amount: -100 },
            { category: 'Travel', amount: 100 },
          ],
          amount: 100,
          isIncome: false,
          transactionType: 'transfer',
        }),
      ],
    });
    const result = computeCategoryAverages([budget], groups, noExclusions);

    const fund = result.categoryAverages.find((c) => c.name === 'Vacation Fund');
    const travel = result.categoryAverages.find((c) => c.name === 'Travel');
    // Neither side leaks into income/expense averages — that's the bug fix.
    assert.equal(fund?.avgIncome, 0);
    assert.equal(fund?.avgExpense, 0);
    assert.equal(travel?.avgIncome, 0);
    assert.equal(travel?.avgExpense, 0);

    // But both splits remain visible in the drill-down with their signed amounts.
    const fundDrill = result.categoryTransactions['Vacation Fund'];
    const travelDrill = result.categoryTransactions['Travel'];
    assert.equal(fundDrill?.length, 1);
    assert.equal(fundDrill?.[0]?.amount, -100);
    assert.equal(fundDrill?.[0]?.isIncome, false);
    assert.equal(travelDrill?.length, 1);
    assert.equal(travelDrill?.[0]?.amount, 100);
    assert.equal(travelDrill?.[0]?.isIncome, true);
  });

  test('mixed standard + transfer in same category does not double-count', () => {
    const budget = makeBudget({
      categories: [
        { name: 'Travel', target: 0, isFund: false, groupId: 'g-exp', groupName: 'Expenses' },
        { name: 'Vacation Fund', target: 0, isFund: true, groupId: 'g-sav', groupName: 'Savings' },
      ],
      transactions: [
        makeTransaction({
          id: 't-4',
          merchant: 'Hotel',
          categories: [{ category: 'Travel', amount: 200 }],
          amount: 200,
          isIncome: false,
        }),
        makeTransaction({
          id: 't-5',
          merchant: 'Transfer',
          categories: [
            { category: 'Vacation Fund', amount: -50 },
            { category: 'Travel', amount: 50 },
          ],
          amount: 50,
          isIncome: false,
          transactionType: 'transfer',
        }),
      ],
    });
    const result = computeCategoryAverages([budget], groups, noExclusions);
    const travel = result.categoryAverages.find((c) => c.name === 'Travel');
    // Standard expense contributes; transfer split does not inflate income.
    assert.equal(travel?.avgExpense, 200);
    assert.equal(travel?.avgIncome, 0);
    // Drill-down shows both rows.
    const drill = result.categoryTransactions['Travel'];
    assert.equal(drill?.length, 2);
  });

  test('excluded category is dropped from totals and drill-down', () => {
    const budget = makeBudget({
      categories: [{ name: 'Groceries', target: 0, isFund: false, groupId: 'g-exp', groupName: 'Expenses' }],
      transactions: [
        makeTransaction({
          id: 't-6',
          categories: [{ category: 'Groceries', amount: 100 }],
          amount: 100,
          isIncome: false,
        }),
      ],
    });
    const result = computeCategoryAverages([budget], groups, {
      groups: new Set<string>(),
      categories: new Set(['Groceries']),
    });
    const groceries = result.categoryAverages.find((c) => c.name === 'Groceries');
    // Category is still in the all-names set (it has a category row), so it
    // appears in averages with zeroed totals; the transaction itself is excluded.
    assert.equal(groceries?.avgExpense, 0);
    assert.equal(groceries?.avgIncome, 0);
    assert.equal(result.categoryTransactions['Groceries'], undefined);
  });

  test('averages divide by budget count', () => {
    const b1 = makeBudget({
      budgetId: 'b-1',
      month: '2025-01',
      categories: [{ name: 'Groceries', target: 0, isFund: false, groupId: 'g-exp', groupName: 'Expenses' }],
      transactions: [
        makeTransaction({ id: 't-a', categories: [{ category: 'Groceries', amount: 100 }], amount: 100 }),
      ],
    });
    const b2 = makeBudget({
      budgetId: 'b-2',
      month: '2025-02',
      categories: [{ name: 'Groceries', target: 0, isFund: false, groupId: 'g-exp', groupName: 'Expenses' }],
      transactions: [
        makeTransaction({ id: 't-b', categories: [{ category: 'Groceries', amount: 300 }], amount: 300 }),
      ],
    });
    const result = computeCategoryAverages([b1, b2], groups, noExclusions);
    const groceries = result.categoryAverages.find((c) => c.name === 'Groceries');
    assert.equal(groceries?.avgExpense, 200);
  });
});
