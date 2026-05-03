import { strict as assert } from 'node:assert';
import { test, describe } from 'node:test';
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore explicit .js import for Node after compilation
import { inferGoalFundingFromCategory } from '../src/utils/transactionFunding.js';
function makeGoal(overrides) {
    return {
        totalTarget: 0,
        monthlyTarget: 0,
        savedToDate: 0,
        spentToDate: 0,
        status: 'in_progress',
        createdAt: { seconds: 0, nanoseconds: 0 },
        updatedAt: { seconds: 0, nanoseconds: 0 },
        entityId: 'e-1',
        ...overrides,
    };
}
const vacationGoal = makeGoal({ id: 'g-vac', name: 'Vacation' });
const carGoal = makeGoal({ id: 'g-car', name: 'Car Replacement' });
const cats = [
    { name: 'Travel', target: 0, isFund: false, groupName: 'Expenses', fundingSourceGoalId: 'g-vac' },
    { name: 'Lodging', target: 0, isFund: false, groupName: 'Expenses', fundingSourceGoalId: 'g-vac' },
    { name: 'Car Repair', target: 0, isFund: false, groupName: 'Expenses', fundingSourceGoalId: 'g-car' },
    { name: 'Groceries', target: 0, isFund: false, groupName: 'Expenses' },
];
describe('inferGoalFundingFromCategory', () => {
    test('returns the matching goal when one destination has a single goal source', () => {
        const result = inferGoalFundingFromCategory({ categories: cats }, ['Travel'], [vacationGoal, carGoal]);
        assert.equal(result?.id, 'g-vac');
    });
    test('returns the matching goal when multiple destinations agree on the same goal', () => {
        const result = inferGoalFundingFromCategory({ categories: cats }, ['Travel', 'Lodging'], [vacationGoal, carGoal]);
        assert.equal(result?.id, 'g-vac');
    });
    test('returns null when destinations disagree on goal', () => {
        const result = inferGoalFundingFromCategory({ categories: cats }, ['Travel', 'Car Repair'], [vacationGoal, carGoal]);
        assert.equal(result, null);
    });
    test('returns null when any destination has no goal funding source', () => {
        const result = inferGoalFundingFromCategory({ categories: cats }, ['Travel', 'Groceries'], [vacationGoal, carGoal]);
        assert.equal(result, null);
    });
    test('returns null when no destination categories provided', () => {
        const result = inferGoalFundingFromCategory({ categories: cats }, [], [vacationGoal, carGoal]);
        assert.equal(result, null);
    });
    test('returns null when destination category does not exist in the budget', () => {
        const result = inferGoalFundingFromCategory({ categories: cats }, ['Phantom'], [vacationGoal, carGoal]);
        assert.equal(result, null);
    });
    test('returns null when goal id resolves to no goal in the provided list', () => {
        const result = inferGoalFundingFromCategory({ categories: cats }, ['Travel'], [carGoal]);
        assert.equal(result, null);
    });
    test('ignores empty/blank destination names', () => {
        const result = inferGoalFundingFromCategory({ categories: cats }, ['', 'Travel'], [vacationGoal]);
        assert.equal(result?.id, 'g-vac');
    });
});
