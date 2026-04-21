export function toStatementFinalizeRequestBody(payload) {
    return {
        statementId: payload.statementId,
        startDate: payload.startDate,
        endDate: payload.endDate,
        beginningBalance: payload.beginningBalance,
        endingBalance: payload.endingBalance,
        importedTransactionIds: payload.importedTransactionIds ?? payload.matchedTransactionIds ?? [],
        budgetTransactionRefs: payload.budgetTransactionRefs ?? [],
    };
}
