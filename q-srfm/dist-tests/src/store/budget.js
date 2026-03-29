/** budget.ts */
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { dataAccess } from '../dataAccess';
export const useBudgetStore = defineStore('budgets', () => {
    const budgets = ref(new Map());
    async function loadBudgets(userId, entityId) {
        try {
            const accessibleBudgets = await dataAccess.loadAccessibleBudgets(userId, entityId);
            // Update the store only after all budgets are fetched
            const newBudgets = new Map();
            for (const b of accessibleBudgets) {
                if (b) {
                    newBudgets.set(b.budgetId, b);
                }
            }
            budgets.value = newBudgets;
        }
        catch (error) {
            console.error('Error loading budgets:', error);
        }
    }
    function unsubscribeAll() {
        budgets.value.clear();
    }
    function getBudget(budgetId) {
        const ret = budgets.value.get(budgetId);
        if (ret && !ret.budgetId)
            ret.budgetId = budgetId;
        return ret;
    }
    function updateBudget(budgetId, budget) {
        budgets.value.set(budgetId, budget);
    }
    function removeBudget(budgetId) {
        budgets.value.delete(budgetId);
    }
    // Add a computed property to expose available budget months
    const availableBudgetMonths = computed(() => {
        const months = Array.from(budgets.value.values())
            .map((budget) => budget.month)
            .sort();
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
