import type { Budget } from '../types';

/**
 * Calculate the carryover values that should be applied to the NEXT month's
 * budget, based on the current month's categories and transactions.
 *
 * Formula per fund category:
 *   nextCarryover = previousCarryover + target + income - spent
 *
 * Negative results propagate forward as a deficit ("the fund is in the hole"),
 * so an overspent fund stays visibly overspent in subsequent months instead
 * of silently resetting to zero.
 *
 * Transfers (`transactionType === 'transfer'`) are evaluated per split by
 * sign: a negative split debits the source category (counts as spend), a
 * positive split credits the destination category (counts as income). The
 * transaction-level `isIncome` flag is ignored for transfers because both
 * sides of a transfer share that flag yet the directions are opposite.
 *
 * Only categories with `isFund: true` participate. Non-fund categories are
 * excluded from the result.
 */
export function calculateCarryOver(budget: Budget): Record<string, number> {
  const nextCarryover: Record<string, number> = {};
  const currTrx = budget.transactions || [];

  const curSpend: Record<string, number> = {};
  const curIncome: Record<string, number> = {};

  for (const t of currTrx) {
    if (t.deleted) continue;
    const isTransfer = t.transactionType === 'transfer';
    for (const split of t.categories || []) {
      const name = split.category;
      if (!name) continue;
      const raw = Number(split.amount) || 0;
      if (isTransfer) {
        // Per-split signed amount. Negative = money leaving this category;
        // positive = money arriving. Avoids double-counting a fund→fund
        // transfer as spend on both sides.
        if (raw < 0) {
          curSpend[name] = (curSpend[name] || 0) + Math.abs(raw);
        } else if (raw > 0) {
          curIncome[name] = (curIncome[name] || 0) + raw;
        }
      } else if (t.isIncome) {
        curIncome[name] = (curIncome[name] || 0) + Math.abs(raw);
      } else {
        curSpend[name] = (curSpend[name] || 0) + Math.abs(raw);
      }
    }
  }

  budget.categories.forEach((cat) => {
    if (cat.isFund) {
      const spent = curSpend[cat.name] || 0;
      const income = curIncome[cat.name] || 0;
      const prevCarryover = cat.carryover || 0;
      // Allow negatives — see jsdoc above. An overspent fund carries the
      // deficit forward so the user sees they are still in the hole.
      nextCarryover[cat.name] = prevCarryover + cat.target + income - spent;
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
