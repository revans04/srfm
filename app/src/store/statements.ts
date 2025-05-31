import { defineStore } from "pinia";
import { ref } from "vue";
import { Statement } from "@/types";
import { dataAccess } from "@/dataAccess";

export const useStatementStore = defineStore("statements", () => {
  const statements = ref<Record<string, Statement[]>>({});

  async function loadStatements(accountNumber: string) {
    const list = await dataAccess.getStatements(accountNumber);
    statements.value[accountNumber] = list.sort((a, b) => a.startDate.localeCompare(b.startDate));
  }

  function getStatements(accountNumber: string): Statement[] {
    return statements.value[accountNumber] || [];
  }

  async function saveStatement(
    accountNumber: string,
    statement: Statement,
    transactionRefs: { budgetId: string; transactionId: string }[]
  ) {
    await dataAccess.saveStatement(accountNumber, statement, transactionRefs);
    await loadStatements(accountNumber);
  }

  return { statements, loadStatements, getStatements, saveStatement };
});
