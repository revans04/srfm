/**
 * Resolve the single goal that should default-fund an expense given the
 * destination categories' `fundingSourceGoalId` settings.
 *
 * Returns the matching Goal when:
 *   - every destination category exists in the budget AND has a
 *     `fundingSourceGoalId` set,
 *   - all those goal ids are the same, AND
 *   - that goal id resolves to a goal in the provided `goals` list.
 *
 * Returns `null` if any destination is missing the field or the destinations
 * disagree on which goal to fund from. Conservative on purpose — disagreement
 * means the user has to pick explicitly via the transaction-level
 * `fundedByGoalId` field.
 */
export function inferGoalFundingFromCategory(budget, txCategoryNames, goals) {
    const names = txCategoryNames.filter((n) => !!n);
    if (names.length === 0)
        return null;
    const goalIds = new Set();
    for (const name of names) {
        const cat = budget.categories.find((c) => c.name === name);
        if (cat?.fundingSourceGoalId)
            goalIds.add(cat.fundingSourceGoalId);
        else
            return null;
    }
    if (goalIds.size !== 1)
        return null;
    const id = Array.from(goalIds)[0];
    if (!id)
        return null;
    return goals.find((g) => g.id === id) ?? null;
}
