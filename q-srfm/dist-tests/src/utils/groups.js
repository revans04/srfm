/**
 * Return the group kind for a category given the entity's group taxonomy.
 * Falls back to inferring from `groupName` when groups aren't loaded yet
 * (e.g., during initial mount before the family store has hydrated).
 */
export function categoryGroupKind(cat, groups) {
    if (groups && cat.groupId) {
        const g = groups.find((x) => x.id === cat.groupId);
        if (g)
            return g.kind;
    }
    // Fallback heuristic by name (matches what the migration backfill does).
    const n = (cat.groupName ?? '').trim().toLowerCase();
    if (n === 'income')
        return 'income';
    if (n === 'savings')
        return 'savings';
    return 'expense';
}
export function isIncomeCategory(cat, groups) {
    return categoryGroupKind(cat, groups) === 'income';
}
export function isSavingsCategory(cat, groups) {
    return categoryGroupKind(cat, groups) === 'savings';
}
/**
 * Resolve the display name for a category's group, preferring the live
 * BudgetGroup taxonomy and falling back to the per-category snapshot.
 */
export function categoryGroupName(cat, groups) {
    if (groups && cat.groupId) {
        const g = groups.find((x) => x.id === cat.groupId);
        if (g)
            return g.name;
    }
    return cat.groupName ?? '';
}
