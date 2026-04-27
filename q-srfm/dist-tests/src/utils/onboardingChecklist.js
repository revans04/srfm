/**
 * Returns the boolean completion map for every checklist item. Pure: same
 * inputs always produce the same output. The store layers a "sticky" set
 * on top so previously-completed items don't un-tick if data is removed.
 */
export function computeChecklistCompletion(inputs) {
    const budgets = inputs.budgets ?? [];
    const family = inputs.family;
    const authUser = inputs.authUser ?? null;
    const hasBudget = budgets.length > 0;
    let hasTransaction = false;
    let hasGoal = false;
    let hasReconciled = false;
    let hasImported = false;
    for (const b of budgets) {
        const txs = b.transactions || [];
        for (const t of txs) {
            if (t.deleted)
                continue;
            hasTransaction = true;
            if (t.status === 'R')
                hasReconciled = true;
            if (t.importedMerchant || t.accountNumber)
                hasImported = true;
        }
        const cats = b.categories || [];
        for (const c of cats) {
            if (c.isFund) {
                hasGoal = true;
                break;
            }
        }
        if (hasTransaction && hasGoal && hasReconciled && hasImported)
            break;
    }
    // Fallback signal for imports: the family has at least one linked account
    // with an account number (the user has gone through account setup even
    // if no transactions have been imported into a budget yet).
    if (!hasImported) {
        const accounts = family?.accounts || [];
        hasImported = accounts.some((a) => Boolean(a.accountNumber));
    }
    // `verify-email` ticks once the Firebase user record reports the
    // verification flag. Mirrors `auth.user.emailVerified` from the auth store.
    const emailVerified = authUser?.emailVerified === true;
    // `invite-partner` ticks when the family has more than just the owner.
    // `Family.members` includes the owner, so >= 2 means at least one
    // accepted invite.
    const inviteAccepted = (family?.members?.length ?? 0) >= 2;
    return {
        'create-budget': hasBudget,
        'enter-transaction': hasTransaction,
        'import-transactions': hasImported,
        'setup-goal': hasGoal,
        'reconcile-account': hasReconciled,
        'verify-email': emailVerified,
        'invite-partner': inviteAccepted,
    };
}
