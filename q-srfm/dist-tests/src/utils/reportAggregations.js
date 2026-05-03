import { categoryGroupName } from './groups';
export function computeCategoryAverages(budgets, groupList, excluded) {
    const categoryGroupMap = new Map();
    const allCategoryNames = new Set();
    budgets.forEach((budget) => {
        budget.categories.forEach((cat) => {
            const gName = categoryGroupName(cat, groupList);
            categoryGroupMap.set(cat.name, gName);
            allCategoryNames.add(cat.name);
        });
        budget.transactions.forEach((tx) => {
            tx.categories.forEach((cat) => {
                allCategoryNames.add(cat.category);
            });
        });
    });
    const categoryTotals = new Map();
    const categorySeenTxIds = new Map();
    const categoryTransactions = {};
    allCategoryNames.forEach((name) => {
        categoryTotals.set(name, { incomeTotal: 0, expenseTotal: 0 });
        categorySeenTxIds.set(name, new Set());
    });
    budgets.forEach((budget) => {
        budget.transactions.forEach((transaction) => {
            if (transaction.deleted)
                return;
            transaction.categories.forEach((cat) => {
                const groupName = categoryGroupMap.get(cat.category);
                if (groupName && excluded.groups.has(groupName))
                    return;
                if (excluded.categories.has(cat.category))
                    return;
                const seenIds = categorySeenTxIds.get(cat.category) ?? new Set();
                if (seenIds.has(transaction.id))
                    return;
                seenIds.add(transaction.id);
                categorySeenTxIds.set(cat.category, seenIds);
                const txList = categoryTransactions[cat.category] ?? [];
                const amount = cat.amount || 0;
                if (transaction.transactionType === 'transfer') {
                    // Transfers carry signed splits (negative on the source side, positive
                    // on the destination). Skip them from incomeTotal/expenseTotal — they'd
                    // double-classify by `transaction.isIncome` and inflate one column —
                    // but keep the split visible in the per-category drill-down so users
                    // still see the activity. The split's own sign tells the story.
                    txList.push({
                        id: transaction.id,
                        date: transaction.date,
                        merchant: transaction.merchant,
                        amount,
                        isIncome: amount > 0,
                    });
                    categoryTransactions[cat.category] = txList;
                    return;
                }
                const totals = categoryTotals.get(cat.category) ?? { incomeTotal: 0, expenseTotal: 0 };
                if (transaction.isIncome) {
                    totals.incomeTotal += amount;
                }
                else {
                    totals.expenseTotal += amount;
                }
                categoryTotals.set(cat.category, totals);
                txList.push({
                    id: transaction.id,
                    date: transaction.date,
                    merchant: transaction.merchant,
                    amount: amount * (transaction.isIncome ? 1 : -1),
                    isIncome: transaction.isIncome,
                });
                categoryTransactions[cat.category] = txList;
            });
        });
    });
    const budgetCount = budgets.length;
    const categoryAverages = Array.from(categoryTotals.entries())
        .map(([name, totals]) => ({
        name,
        avgIncome: budgetCount ? totals.incomeTotal / budgetCount : 0,
        avgExpense: budgetCount ? totals.expenseTotal / budgetCount : 0,
    }))
        .sort((a, b) => a.name.localeCompare(b.name));
    return { categoryAverages, categoryTransactions };
}
