import { ref } from 'vue';

export interface Statement {
  id: string;
  accountId: string;
  startDate: string;
  endDate: string;
  beginBalance: number;
  endBalance: number;
  matchedIds: string[];
}

export function useStatements() {
  const current = ref<Statement | null>(null);

  function getCurrent() {
    return current.value;
  }

  function begin(accountId: string, range: { start: string; end: string }, beginBalance: number) {
    current.value = {
      id: 'stmt-1',
      accountId,
      startDate: range.start,
      endDate: range.end,
      beginBalance,
      endBalance: 0,
      matchedIds: [],
    };
    return current.value;
  }

  function finalize(statementId: string, endBalance: number) {
    if (current.value && current.value.id === statementId) {
      current.value.endBalance = endBalance;
    }
  }

  return { current, getCurrent, begin, finalize } as const;
}
