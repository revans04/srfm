import type { Budget } from '../types';

/**
 * Calculate the carryover values that should be applied to the NEXT month's
 * budget, based on the current month's categories and transactions.
 *
 * Formula per fund category:
 *   nextCarryover = max(0, previousCarryover + target + income - spent)
 *
 * Only categories with `isFund: true` participate. Non-fund categories are
 * excluded from the result.
 */
export function calculateCarryOver(budget: Budget): Record<string, number> {
  const nextCarryover: Record<string, number> = {};
  const currTrx = budget.transactions || [];

  const curSpend = currTrx
    .filter((t) => !t.deleted && !t.isIncome)
    .reduce(
      (acc, t) => {
        t.categories.forEach((split) => {
          acc[split.category] = (acc[split.category] || 0) + Math.abs(split.amount);
        });
        return acc;
      },
      {} as Record<string, number>,
    );

  const curIncome = currTrx
    .filter((t) => !t.deleted && t.isIncome)
    .reduce(
      (acc, t) => {
        t.categories.forEach((split) => {
          acc[split.category] = (acc[split.category] || 0) + Math.abs(split.amount);
        });
        return acc;
      },
      {} as Record<string, number>,
    );

  budget.categories.forEach((cat) => {
    if (cat.isFund) {
      const spent = curSpend[cat.name] || 0;
      const income = curIncome[cat.name] || 0;
      const prevCarryover = cat.carryover || 0;
      const rem = prevCarryover + cat.target + income - spent;
      nextCarryover[cat.name] = rem > 0 ? rem : 0;
    }
  });

  return nextCarryover;
}

/**
 * Given an ordered array of budgets (sorted by month ascending), walk forward
 * from `startIndex` and compute + apply carryover values to each subsequent
 * budget. Returns a new array of budgets with updated carryover values.
 *
 * The budget at `startIndex` is the "source" — its carryover is computed and
 * applied to `startIndex + 1`, then that budget's carryover is applied to the
 * next, and so on.
 */
export function cascadeCarryover(budgets: Budget[], startIndex: number): Budget[] {
  const result = budgets.map((b) => ({
    ...b,
    categories: b.categories.map((c) => ({ ...c })),
  }));

  for (let i = startIndex; i < result.length - 1; i++) {
    const curr = result[i];
    const next = result[i + 1];

    const nextCarry = calculateCarryOver(curr);
    next.categories = next.categories.map((cat) => ({
      ...cat,
      carryover: cat.isFund ? nextCarry[cat.name] || 0 : 0,
    }));
  }

  return result;
}
