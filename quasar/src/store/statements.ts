import { defineStore } from "pinia";
import { ref } from "vue";
import type { Statement } from "../types";
import { dataAccess } from "../dataAccess";

export const useStatementStore = defineStore("statements", () => {
  const statements = ref<Record<string, Statement[]>>({});

  async function loadStatements(familyId: string, accountNumber: string) {
    const list = await dataAccess.getStatements(familyId, accountNumber);
    const key = `${familyId}_${accountNumber}`;
    statements.value[key] = list.sort((a, b) => a.startDate.localeCompare(b.startDate));
  }

  function getStatements(familyId: string, accountNumber: string): Statement[] {
    return statements.value[`${familyId}_${accountNumber}`] || [];
  }

  async function saveStatement(
    familyId: string,
    accountNumber: string,
    statement: Statement,
    transactionRefs: { budgetId: string; transactionId: string }[]
  ) {
    await dataAccess.saveStatement(familyId, accountNumber, statement, transactionRefs);
    await loadStatements(familyId, accountNumber);
  }

  async function deleteStatement(
    familyId: string,
    accountNumber: string,
    statementId: string,
    transactionRefs: { budgetId: string; transactionId: string }[]
  ) {
    await dataAccess.deleteStatement(familyId, accountNumber, statementId, transactionRefs);
    await loadStatements(familyId, accountNumber);
  }

  async function unreconcileStatement(
    familyId: string,
    accountNumber: string,
    statementId: string,
    transactionRefs: { budgetId: string; transactionId: string }[]
  ) {
    await dataAccess.unreconcileStatement(familyId, accountNumber, statementId, transactionRefs);
    await loadStatements(familyId, accountNumber);
  }

  return { statements, loadStatements, getStatements, saveStatement, deleteStatement, unreconcileStatement };
});

