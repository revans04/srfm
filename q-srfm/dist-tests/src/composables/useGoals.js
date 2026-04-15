import { ref } from 'vue';
import { v4 as uuidv4 } from 'uuid';
import { dataAccess } from '../dataAccess';
import { useFamilyStore } from '../store/family';
import { useBudgetStore } from '../store/budget';
/**
 * Composable for managing savings goals.
 *
 * Contributions and goal spends are no longer tracked in-memory — they are
 * derived from transfer transactions stored on monthly budgets. Use the
 * TransactionForm or BudgetPage.saveContribution to persist them. This
 * composable caches backend-derived roll-ups (savedToDate / spentToDate) and
 * the detail lists returned by `loadGoalDetails` for display.
 */
const goals = ref([]);
// Cache of backend-derived contributions/spends keyed by goalId. Populated by
// loadGoalDetails and kept around so the details panel can render without
// refetching on every open.
const contributionsCache = ref({});
const spendsCache = ref({});
const loadedEntities = new Set();
export function useGoals() {
    const familyStore = useFamilyStore();
    const budgetStore = useBudgetStore();
    function listGoals(entityId) {
        return goals.value.filter((g) => g.entityId === entityId && !g.archived);
    }
    function listContributions(goalId) {
        return contributionsCache.value[goalId] || [];
    }
    function listGoalSpends(goalId) {
        return spendsCache.value[goalId] || [];
    }
    function getGoal(goalId) {
        return goals.value.find((g) => g.id === goalId);
    }
    async function loadGoals(entityId, force = false) {
        if (!entityId)
            return;
        if (loadedEntities.has(entityId) && !force)
            return;
        const fetched = await dataAccess.getGoals(entityId);
        goals.value = goals.value.filter((g) => g.entityId !== entityId);
        goals.value.push(...fetched);
        loadedEntities.add(entityId);
    }
    async function createGoal(data) {
        const now = new Date();
        let entityId = data.entityId || familyStore.selectedEntityId;
        if (!entityId) {
            await familyStore.loadFamily();
            entityId = familyStore.selectedEntityId || familyStore.family?.entities?.[0]?.id || '';
        }
        if (!entityId) {
            throw new Error('Entity ID is required to create a goal');
        }
        const goal = {
            id: uuidv4(),
            name: data.name || '',
            totalTarget: data.totalTarget || 0,
            monthlyTarget: data.monthlyTarget || 0,
            savedToDate: 0,
            spentToDate: 0,
            status: 'in_progress',
            targetDate: data.targetDate,
            createdAt: now,
            updatedAt: now,
            entityId,
            notes: data.notes,
            archived: false,
        };
        goals.value.push(goal);
        await dataAccess.insertGoal(goal);
        // Remove the legacy category from loaded budgets so it no longer shows in the UI
        for (const [id, b] of budgetStore.budgets.entries()) {
            if (!b.entityId || b.entityId === goal.entityId) {
                const filtered = b.categories.filter((c) => c.name !== goal.name);
                budgetStore.updateBudget(b.budgetId || id, { ...b, categories: filtered });
            }
        }
        return goal;
    }
    async function updateGoal(goalId, data) {
        const goal = goals.value.find((g) => g.id === goalId);
        if (goal) {
            Object.assign(goal, data, { updatedAt: new Date() });
            await dataAccess.updateGoal(goal);
        }
    }
    async function archiveGoal(goalId) {
        const goal = goals.value.find((g) => g.id === goalId);
        if (goal) {
            goal.archived = true;
            goal.updatedAt = new Date();
            await dataAccess.updateGoal(goal);
        }
    }
    async function loadGoalDetails(goalId) {
        const details = await dataAccess.getGoalDetails(goalId);
        contributionsCache.value[goalId] = details.contributions;
        spendsCache.value[goalId] = details.spend;
    }
    /**
     * Sum of planned monthly savings across active goals. The month parameter
     * is accepted for API stability but is currently unused — planned savings
     * come from each goal's `monthlyTarget`, which is not per-month. Actual
     * contributions for a given month should be read from transaction data
     * instead.
     */
    function monthlySavingsTotal(entityId, _month) {
        void _month;
        const activeGoals = listGoals(entityId);
        return activeGoals.reduce((sum, g) => sum + (g.monthlyTarget || 0), 0);
    }
    /**
     * Actual contributions to a goal in a given month, computed from transfer
     * transactions in the loaded budgets. A contribution is any positive
     * transaction_category amount whose category name matches the goal. If the
     * relevant month's budget isn't loaded, the cache may not be populated; the
     * caller should ensure the budget is loaded or fall back to
     * loadGoalDetails which hits the backend directly.
     */
    function contributionsForMonth(goalId, month) {
        const goal = goals.value.find((g) => g.id === goalId);
        if (!goal)
            return 0;
        let total = 0;
        for (const b of budgetStore.budgets.values()) {
            if (b.month !== month)
                continue;
            if (goal.entityId && b.entityId && b.entityId !== goal.entityId)
                continue;
            for (const t of b.transactions) {
                if (t.deleted)
                    continue;
                for (const tc of t.categories || []) {
                    if (tc.category === goal.name && (tc.amount || 0) > 0) {
                        total += tc.amount;
                    }
                }
            }
        }
        return total;
    }
    return {
        listGoals,
        getGoal,
        loadGoals,
        createGoal,
        updateGoal,
        archiveGoal,
        loadGoalDetails,
        monthlySavingsTotal,
        contributionsForMonth,
        listContributions,
        listGoalSpends,
    };
}
