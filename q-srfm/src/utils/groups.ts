/**
 * Group-related helpers. Replaces ad-hoc string comparisons against the
 * literal "Income" group name with a kind-based check that survives renames.
 */
import type { BudgetCategory, BudgetGroup, BudgetGroupKind } from '../types';

/**
 * Return the group kind for a category given the entity's group taxonomy.
 * Falls back to inferring from `groupName` when groups aren't loaded yet
 * (e.g., during initial mount before the family store has hydrated).
 */
export function categoryGroupKind(
  cat: Pick<BudgetCategory, 'groupId' | 'groupName'>,
  groups: BudgetGroup[] | undefined | null,
): BudgetGroupKind {
  if (groups && cat.groupId) {
    const g = groups.find((x) => x.id === cat.groupId);
    if (g) return g.kind;
  }
  // Fallback heuristic by name (matches what the migration backfill does).
  const n = (cat.groupName ?? '').trim().toLowerCase();
  if (n === 'income') return 'income';
  if (n === 'savings') return 'savings';
  return 'expense';
}

export function isIncomeCategory(
  cat: Pick<BudgetCategory, 'groupId' | 'groupName'>,
  groups: BudgetGroup[] | undefined | null,
): boolean {
  return categoryGroupKind(cat, groups) === 'income';
}

export function isSavingsCategory(
  cat: Pick<BudgetCategory, 'groupId' | 'groupName'>,
  groups: BudgetGroup[] | undefined | null,
): boolean {
  return categoryGroupKind(cat, groups) === 'savings';
}

/**
 * Resolve the display name for a category's group, preferring the live
 * BudgetGroup taxonomy and falling back to the per-category snapshot.
 */
export function categoryGroupName(
  cat: Pick<BudgetCategory, 'groupId' | 'groupName'>,
  groups: BudgetGroup[] | undefined | null,
): string {
  if (groups && cat.groupId) {
    const g = groups.find((x) => x.id === cat.groupId);
    if (g) return g.name;
  }
  return cat.groupName ?? '';
}
