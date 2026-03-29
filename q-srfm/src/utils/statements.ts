import type { StatementFinalizePayload } from '../types';

export interface StatementFinalizeRequestBody {
  statementId?: string;
  startDate: string;
  endDate: string;
  beginningBalance: number;
  endingBalance: number;
  importedTransactionIds: string[];
  budgetTransactionRefs: Array<{ budgetId: string; transactionId: string }>;
}

export function toStatementFinalizeRequestBody(payload: StatementFinalizePayload): StatementFinalizeRequestBody {
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
