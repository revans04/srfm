import { strict as assert } from 'node:assert';
import { test, describe } from 'node:test';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore explicit .js import for Node after compilation
import { calculateCarryOver, cascadeCarryover } from '../src/utils/carryover.js';
// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
function makeBudget(overrides) {
    return {
        budgetId: 'b-1',
        familyId: 'f-1',
        entityId: 'e-1',
        label: 'Test Budget',
        month: '2025-01',
        incomeTarget: 0,
        merchants: [],
        transactions: [],
        ...overrides,
    };
}
function makeTransaction(overrides) {
    return {
        id: 't-1',
        date: '2025-01-15',
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
// ---------------------------------------------------------------------------
// calculateCarryOver – basic formula
// ---------------------------------------------------------------------------
describe('calculateCarryOver', () => {
    test('returns empty object when no fund categories exist', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Groceries', target: 500, isFund: false, group: 'Expenses' },
            ],
        });
        const result = calculateCarryOver(budget);
        assert.deepEqual(result, {});
    });
    test('fund with no transactions carries target + previous carryover', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Emergency', target: 200, isFund: true, group: 'Savings', carryover: 300 },
            ],
            transactions: [],
        });
        const result = calculateCarryOver(budget);
        // nextCarryover = 300 + 200 + 0 - 0 = 500
        assert.equal(result['Emergency'], 500);
    });
    test('fund with spending reduces carryover', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Car Fund', target: 100, isFund: true, group: 'Savings', carryover: 400 },
            ],
            transactions: [
                makeTransaction({
                    id: 't-1',
                    categories: [{ category: 'Car Fund', amount: 150 }],
                    amount: 150,
                    isIncome: false,
                }),
            ],
        });
        const result = calculateCarryOver(budget);
        // nextCarryover = 400 + 100 + 0 - 150 = 350
        assert.equal(result['Car Fund'], 350);
    });
    test('fund with income adds to carryover', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Side Hustle', target: 0, isFund: true, group: 'Savings', carryover: 100 },
            ],
            transactions: [
                makeTransaction({
                    id: 't-1',
                    categories: [{ category: 'Side Hustle', amount: 75 }],
                    amount: 75,
                    isIncome: true,
                }),
            ],
        });
        const result = calculateCarryOver(budget);
        // nextCarryover = 100 + 0 + 75 - 0 = 175
        assert.equal(result['Side Hustle'], 175);
    });
    test('carryover floors at zero (never negative)', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Vacation', target: 50, isFund: true, group: 'Savings', carryover: 20 },
            ],
            transactions: [
                makeTransaction({
                    id: 't-1',
                    categories: [{ category: 'Vacation', amount: 200 }],
                    amount: 200,
                    isIncome: false,
                }),
            ],
        });
        const result = calculateCarryOver(budget);
        // nextCarryover = max(0, 20 + 50 + 0 - 200) = max(0, -130) = 0
        assert.equal(result['Vacation'], 0);
    });
    test('handles zero carryover (undefined) gracefully', () => {
        const budget = makeBudget({
            categories: [
                { name: 'New Fund', target: 100, isFund: true, group: 'Savings' },
                // carryover is undefined
            ],
            transactions: [],
        });
        const result = calculateCarryOver(budget);
        // nextCarryover = 0 + 100 + 0 - 0 = 100
        assert.equal(result['New Fund'], 100);
    });
    test('deleted transactions are excluded', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Fund A', target: 100, isFund: true, group: 'Savings', carryover: 0 },
            ],
            transactions: [
                makeTransaction({
                    id: 't-1',
                    categories: [{ category: 'Fund A', amount: 50 }],
                    amount: 50,
                    isIncome: false,
                    deleted: true,
                }),
            ],
        });
        const result = calculateCarryOver(budget);
        // Deleted tx excluded: nextCarryover = 0 + 100 + 0 - 0 = 100
        assert.equal(result['Fund A'], 100);
    });
    test('multiple fund categories calculated independently', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Fund A', target: 100, isFund: true, group: 'Savings', carryover: 50 },
                { name: 'Fund B', target: 200, isFund: true, group: 'Savings', carryover: 0 },
                { name: 'Groceries', target: 500, isFund: false, group: 'Expenses' },
            ],
            transactions: [
                makeTransaction({
                    id: 't-1',
                    categories: [{ category: 'Fund A', amount: 30 }],
                    amount: 30,
                    isIncome: false,
                }),
                makeTransaction({
                    id: 't-2',
                    categories: [{ category: 'Fund B', amount: 50 }],
                    amount: 50,
                    isIncome: false,
                }),
            ],
        });
        const result = calculateCarryOver(budget);
        // Fund A: 50 + 100 + 0 - 30 = 120
        assert.equal(result['Fund A'], 120);
        // Fund B: 0 + 200 + 0 - 50 = 150
        assert.equal(result['Fund B'], 150);
        // Groceries: not a fund, not in result
        assert.equal(result['Groceries'], undefined);
    });
    test('split transactions allocate correctly per category', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Fund A', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                { name: 'Fund B', target: 100, isFund: true, group: 'Savings', carryover: 0 },
            ],
            transactions: [
                makeTransaction({
                    id: 't-1',
                    categories: [
                        { category: 'Fund A', amount: 25 },
                        { category: 'Fund B', amount: 75 },
                    ],
                    amount: 100,
                    isIncome: false,
                }),
            ],
        });
        const result = calculateCarryOver(budget);
        // Fund A: 0 + 100 + 0 - 25 = 75
        assert.equal(result['Fund A'], 75);
        // Fund B: 0 + 100 + 0 - 75 = 25
        assert.equal(result['Fund B'], 25);
    });
    test('negative amounts are treated as absolute values', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 0 },
            ],
            transactions: [
                makeTransaction({
                    id: 't-1',
                    categories: [{ category: 'Fund', amount: -50 }],
                    amount: -50,
                    isIncome: false,
                }),
            ],
        });
        const result = calculateCarryOver(budget);
        // Math.abs(-50) = 50; nextCarryover = 0 + 100 + 0 - 50 = 50
        assert.equal(result['Fund'], 50);
    });
    test('both income and spending in same category', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 200 },
            ],
            transactions: [
                makeTransaction({
                    id: 't-1',
                    categories: [{ category: 'Fund', amount: 80 }],
                    amount: 80,
                    isIncome: false,
                }),
                makeTransaction({
                    id: 't-2',
                    categories: [{ category: 'Fund', amount: 30 }],
                    amount: 30,
                    isIncome: true,
                }),
            ],
        });
        const result = calculateCarryOver(budget);
        // nextCarryover = 200 + 100 + 30 - 80 = 250
        assert.equal(result['Fund'], 250);
    });
    test('handles budget with no transactions array', () => {
        const budget = makeBudget({
            categories: [
                { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 50 },
            ],
        });
        budget.transactions = undefined;
        const result = calculateCarryOver(budget);
        // nextCarryover = 50 + 100 + 0 - 0 = 150
        assert.equal(result['Fund'], 150);
    });
});
// ---------------------------------------------------------------------------
// cascadeCarryover – multi-month propagation
// ---------------------------------------------------------------------------
describe('cascadeCarryover', () => {
    test('propagates carryover through a 3-month chain', () => {
        const budgets = [
            makeBudget({
                budgetId: 'b-1',
                month: '2025-01',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [
                    makeTransaction({
                        id: 't-1',
                        categories: [{ category: 'Fund', amount: 30 }],
                        amount: 30,
                        isIncome: false,
                    }),
                ],
            }),
            makeBudget({
                budgetId: 'b-2',
                month: '2025-02',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [
                    makeTransaction({
                        id: 't-2',
                        categories: [{ category: 'Fund', amount: 20 }],
                        amount: 20,
                        isIncome: false,
                    }),
                ],
            }),
            makeBudget({
                budgetId: 'b-3',
                month: '2025-03',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [],
            }),
        ];
        const result = cascadeCarryover(budgets, 0);
        // Month 1 carryover (applied to Month 2): 0 + 100 - 30 = 70
        assert.equal(result[1].categories[0].carryover, 70);
        // Month 2 carryover (applied to Month 3): 70 + 100 - 20 = 150
        assert.equal(result[2].categories[0].carryover, 150);
    });
    test('does not modify the source budget at startIndex', () => {
        const budgets = [
            makeBudget({
                budgetId: 'b-1',
                month: '2025-01',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 999 },
                ],
                transactions: [],
            }),
            makeBudget({
                budgetId: 'b-2',
                month: '2025-02',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [],
            }),
        ];
        const result = cascadeCarryover(budgets, 0);
        // Source budget carryover unchanged
        assert.equal(result[0].categories[0].carryover, 999);
        // Next budget gets computed value: 999 + 100 = 1099
        assert.equal(result[1].categories[0].carryover, 1099);
    });
    test('does not mutate original budgets array', () => {
        const budgets = [
            makeBudget({
                month: '2025-01',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 50 },
                ],
                transactions: [],
            }),
            makeBudget({
                month: '2025-02',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [],
            }),
        ];
        cascadeCarryover(budgets, 0);
        // Original should be untouched
        assert.equal(budgets[1].categories[0].carryover, 0);
    });
    test('handles cascade starting from middle of array', () => {
        const budgets = [
            makeBudget({
                month: '2025-01',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 500 },
                ],
                transactions: [],
            }),
            makeBudget({
                month: '2025-02',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 200 },
                ],
                transactions: [
                    makeTransaction({
                        id: 't-1',
                        categories: [{ category: 'Fund', amount: 50 }],
                        amount: 50,
                        isIncome: false,
                    }),
                ],
            }),
            makeBudget({
                month: '2025-03',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [],
            }),
        ];
        const result = cascadeCarryover(budgets, 1);
        // Month 1 untouched (before startIndex)
        assert.equal(result[0].categories[0].carryover, 500);
        // Month 2 carryover unchanged (source)
        assert.equal(result[1].categories[0].carryover, 200);
        // Month 3 carryover from Month 2: 200 + 100 - 50 = 250
        assert.equal(result[2].categories[0].carryover, 250);
    });
    test('non-fund categories get carryover set to 0', () => {
        const budgets = [
            makeBudget({
                month: '2025-01',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 50 },
                    { name: 'Groceries', target: 500, isFund: false, group: 'Expenses', carryover: 999 },
                ],
                transactions: [],
            }),
            makeBudget({
                month: '2025-02',
                categories: [
                    { name: 'Fund', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                    { name: 'Groceries', target: 500, isFund: false, group: 'Expenses', carryover: 123 },
                ],
                transactions: [],
            }),
        ];
        const result = cascadeCarryover(budgets, 0);
        // Fund category gets proper carryover
        assert.equal(result[1].categories[0].carryover, 150); // 50 + 100
        // Non-fund always set to 0
        assert.equal(result[1].categories[1].carryover, 0);
    });
    test('carryover floors at zero mid-cascade', () => {
        const budgets = [
            makeBudget({
                month: '2025-01',
                categories: [
                    { name: 'Fund', target: 50, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [
                    makeTransaction({
                        id: 't-1',
                        categories: [{ category: 'Fund', amount: 200 }],
                        amount: 200,
                        isIncome: false,
                    }),
                ],
            }),
            makeBudget({
                month: '2025-02',
                categories: [
                    { name: 'Fund', target: 50, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [],
            }),
            makeBudget({
                month: '2025-03',
                categories: [
                    { name: 'Fund', target: 50, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [],
            }),
        ];
        const result = cascadeCarryover(budgets, 0);
        // Month 1: max(0, 0 + 50 - 200) = 0 → Month 2 carryover
        assert.equal(result[1].categories[0].carryover, 0);
        // Month 2: max(0, 0 + 50 - 0) = 50 → Month 3 carryover (recovery!)
        assert.equal(result[2].categories[0].carryover, 50);
    });
    test('multiple fund categories cascade independently', () => {
        const budgets = [
            makeBudget({
                month: '2025-01',
                categories: [
                    { name: 'Fund A', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                    { name: 'Fund B', target: 200, isFund: true, group: 'Savings', carryover: 100 },
                ],
                transactions: [
                    makeTransaction({
                        id: 't-1',
                        categories: [{ category: 'Fund A', amount: 40 }],
                        amount: 40,
                        isIncome: false,
                    }),
                ],
            }),
            makeBudget({
                month: '2025-02',
                categories: [
                    { name: 'Fund A', target: 100, isFund: true, group: 'Savings', carryover: 0 },
                    { name: 'Fund B', target: 200, isFund: true, group: 'Savings', carryover: 0 },
                ],
                transactions: [],
            }),
        ];
        const result = cascadeCarryover(budgets, 0);
        // Fund A: 0 + 100 - 40 = 60
        assert.equal(result[1].categories[0].carryover, 60);
        // Fund B: 100 + 200 - 0 = 300
        assert.equal(result[1].categories[1].carryover, 300);
    });
});
