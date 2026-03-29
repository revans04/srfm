import { ref } from 'vue';
import { v4 as uuidv4 } from 'uuid';
import { dataAccess } from '../dataAccess';
import { useFamilyStore } from '../store/family';
import { useBudgetStore } from '../store/budget';
/**
 * Composable for managing savings goals.
 *
 * Sample seed in console:
 * const { createGoal, addContribution } = useGoals();
 * const goal = await createGoal({ entityId: 'demo', name: 'Vacation', totalTarget: 5000, monthlyTarget: 500 });
 * await addContribution(goal.id, 500, '2025-08');
 */
const goals = ref([]);
const contributions = ref({});
const spends = ref({});
const loadedEntities = new Set();
export function useGoals() {
    const familyStore = useFamilyStore();
    const budgetStore = useBudgetStore();
    function listGoals(entityId) {
        return goals.value.filter((g) => g.entityId === entityId && !g.archived);
    }
    function listContributions(goalId) {
        return contributions.value[goalId] || [];
    }
    function listGoalSpends(goalId) {
        return spends.value[goalId] || [];
    }
    function getGoal(goalId) {
        return goals.value.find((g) => g.id === goalId);
    }
    async function loadGoals(entityId) {
        if (!entityId || loadedEntities.has(entityId))
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
        console.log('Loading goal details', goalId);
        const details = await dataAccess.getGoalDetails(goalId);
        console.log('Loaded goal details', goalId, details);
        contributions.value[goalId] = details.contributions;
        spends.value[goalId] = details.spend;
        recomputeRollups(goalId);
    }
    function addContribution(goalId, amount, month, note) {
        const list = contributions.value[goalId] || (contributions.value[goalId] = []);
        list.push({ goalId, amount, month, note });
        recomputeRollups(goalId);
    }
    function addGoalSpend(goalId, txId, amount, txDate, note) {
        const list = spends.value[goalId] || (spends.value[goalId] = []);
        list.push({ goalId, txId, amount, txDate, note });
        recomputeRollups(goalId);
    }
    function recomputeRollups(goalId) {
        const goal = goals.value.find((g) => g.id === goalId);
        if (!goal)
            return;
        const saved = (contributions.value[goalId] || []).reduce((s, c) => s + c.amount, 0);
        const spent = (spends.value[goalId] || []).reduce((s, c) => s + c.amount, 0);
        goal.savedToDate = saved;
        goal.spentToDate = spent;
        goal.status = saved >= goal.totalTarget ? 'reached' : goal.status;
        goal.updatedAt = new Date();
    }
    function contributionsForMonth(goalId, month) {
        return (contributions.value[goalId] || [])
            .filter((c) => c.month === month)
            .reduce((s, c) => s + c.amount, 0);
    }
    function monthlySavingsTotal(entityId, _month) {
        void _month;
        const activeGoals = listGoals(entityId);
        return activeGoals.reduce((sum, g) => sum + g.monthlyTarget, 0);
    }
    return {
        listGoals,
        getGoal,
        loadGoals,
        createGoal,
        updateGoal,
        archiveGoal,
        loadGoalDetails,
        addContribution,
        addGoalSpend,
        recomputeRollups,
        monthlySavingsTotal,
        contributionsForMonth,
        listContributions,
        listGoalSpends,
    };
}
