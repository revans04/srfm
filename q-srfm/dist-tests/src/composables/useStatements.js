import { ref } from 'vue';
export function useStatements() {
    const current = ref(null);
    function getCurrent() {
        return current.value;
    }
    function begin(accountId, range, beginBalance) {
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
    function finalize(statementId, endBalance) {
        if (current.value && current.value.id === statementId) {
            current.value.endBalance = endBalance;
        }
    }
    return { current, getCurrent, begin, finalize };
}
