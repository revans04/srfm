import type { Transaction } from '../types';
import { useGoals } from './useGoals';

export function useBudgetMath() {
  const { monthlySavingsTotal } = useGoals();

  function availableToBudget(
    income: number,
    expenses: number,
    entityId: string,
    month: string,
  ): number {
    const savings = monthlySavingsTotal(entityId, month);
    return income - expenses - savings;
  }

  function excludeGoalFunded(tx: Transaction): boolean {
    return Boolean(tx.fundedByGoalId);
  }

  return { availableToBudget, excludeGoalFunded };
}
