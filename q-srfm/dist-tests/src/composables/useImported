import { ref } from 'vue';
function generateImported(count = 100) {
    const arr = [];
    for (let i = 0; i < count; i++) {
        arr.push({
            id: `imp-${i}`,
            bankDate: new Date(Date.now() - i * 86400000).toISOString().slice(0, 10),
            bankAmount: parseFloat((Math.random() * 200 - 100).toFixed(2)),
            type: 'debit',
            bankPayee: `BankPayee ${i}`,
        });
    }
    return arr;
}
const imported = generateImported();
export function useImported() {
    const smartMatches = ref(imported.slice(0, 20));
    const remaining = ref(imported.slice(20));
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function listByAccount(_accountId) {
        return imported.filter(() => true);
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    function smartMatchesFn(_windowDays) {
        return smartMatches.value;
    }
    function confirmMatches(ids) {
        // placeholder to remove confirmed ids
        remaining.value = remaining.value.filter((r) => !ids.includes(r.id));
        smartMatches.value = smartMatches.value.filter((r) => !ids.includes(r.id));
    }
    return { smartMatches, remaining, listByAccount, smartMatchesFn, confirmMatches };
}
