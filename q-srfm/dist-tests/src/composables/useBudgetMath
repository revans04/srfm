import { useGoals } from './useGoals';
export function useBudgetMath() {
    const { monthlySavingsTotal } = useGoals();
    function availableToBudget(income, expenses, entityId, month) {
        const savings = monthlySavingsTotal(entityId, month);
        return income - expenses - savings;
    }
    function excludeGoalFunded(tx) {
        return Boolean(tx.fundedByGoalId);
    }
    return { availableToBudget, excludeGoalFunded };
}
