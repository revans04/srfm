/** budget.ts */
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { Budget } from "../types";
import { dataAccess } from "../dataAccess";

export const useBudgetStore = defineStore("budgets", () => {
  const budgets = ref<Map<string, Budget>>(new Map());

  async function loadBudgets(userId: string, entityId?: string) {
    try {
      const accessibleBudgets = await dataAccess.loadAccessibleBudgets(userId, entityId);

      // Update the store only after all budgets are fetched
      const newBudgets = new Map<string, Budget>();
      for (const b of accessibleBudgets) {
        if (b) {
          newBudgets.set(b.budgetId, b);
        }
      }
      budgets.value = newBudgets;
    } catch (error: any) {
      console.error("Error loading budgets:", error);
    }
  }

  function unsubscribeAll() {
    budgets.value.clear();
  }

  function getBudget(budgetId: string): Budget | undefined {
    const ret = budgets.value.get(budgetId);
    if (ret && !ret.budgetId) ret.budgetId = budgetId;
    return ret;
  }

  function updateBudget(budgetId: string, budget: Budget) {
    budgets.value.set(budgetId, budget);
  }

  function removeBudget(budgetId: string) {
    budgets.value.delete(budgetId);
  }

  // Add a computed property to expose available budget months
  const availableBudgetMonths = computed(() => {
    const months = Array.from(budgets.value.values()).map(budget => budget.month).sort();
    return months;
  });

  return {
    budgets,
    loadBudgets,
    unsubscribeAll,
    getBudget,
    removeBudget,
    updateBudget,
    availableBudgetMonths,
  };
});
