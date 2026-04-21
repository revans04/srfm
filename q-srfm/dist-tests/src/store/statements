import { defineStore } from 'pinia';
import { ref } from 'vue';
import { dataAccess } from '../dataAccess';
export const useStatementStore = defineStore('statements', () => {
    const statements = ref({});
    async function loadStatements(familyId, accountNumber) {
        const list = await dataAccess.getStatements(familyId, accountNumber);
        const key = `${familyId}_${accountNumber}`;
        statements.value[key] = list.sort((a, b) => a.startDate.localeCompare(b.startDate));
    }
    function getStatements(familyId, accountNumber) {
        return statements.value[`${familyId}_${accountNumber}`] || [];
    }
    async function saveStatement(familyId, accountNumber, statement, transactionRefs) {
        const saved = await dataAccess.saveStatement(familyId, accountNumber, statement, transactionRefs);
        await loadStatements(familyId, accountNumber);
        return saved;
    }
    async function deleteStatement(familyId, accountNumber, statementId, transactionRefs) {
        await dataAccess.deleteStatement(familyId, accountNumber, statementId, transactionRefs);
        await loadStatements(familyId, accountNumber);
    }
    async function unreconcileStatement(familyId, accountNumber, statementId, transactionRefs) {
        await dataAccess.unreconcileStatement(familyId, accountNumber, statementId, transactionRefs);
        await loadStatements(familyId, accountNumber);
    }
    return { statements, loadStatements, getStatements, saveStatement, deleteStatement, unreconcileStatement };
});
