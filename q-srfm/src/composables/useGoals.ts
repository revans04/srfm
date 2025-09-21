import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { v4 as uuidv4 } from 'uuid';
import type { Goal, GoalContribution, GoalSpend } from '../types';
import type { Timestamp } from 'firebase/firestore';
import { dataAccess } from '../dataAccess';
import { useFamilyStore } from '../store/family';

const useGoalsStore = defineStore('goals', () => {
  const goals = ref<Goal[]>([]);
  const contributions = ref<Record<string, GoalContribution[]>>({});
  const spends = ref<Record<string, GoalSpend[]>>({});
  const loadedEntities = ref<Set<string>>(new Set());

  const familyStore = useFamilyStore();

  function listGoals(entityId: string): Goal[] {
    return goals.value.filter((g) => g.entityId === entityId && !g.archived);
  }

  function listContributions(goalId: string): GoalContribution[] {
    return contributions.value[goalId] || [];
  }

  function listGoalSpends(goalId: string): GoalSpend[] {
    return spends.value[goalId] || [];
  }

  function getGoal(goalId: string): Goal | undefined {
    return goals.value.find((g) => g.id === goalId);
  }

  async function loadGoals(entityId: string): Promise<void> {
    if (!entityId || loadedEntities.value.has(entityId)) return;
    const fetched = await dataAccess.getGoals(entityId);
    goals.value = goals.value.filter((g) => g.entityId !== entityId);
    goals.value.push(...fetched);
    loadedEntities.value.add(entityId);
  }

  async function ensureEntity(entityId?: string): Promise<string> {
    let resolvedEntityId = entityId || familyStore.selectedEntityId;
    if (!resolvedEntityId) {
      await familyStore.loadFamily();
      resolvedEntityId = familyStore.selectedEntityId || familyStore.family?.entities?.[0]?.id || '';
    }
    if (!resolvedEntityId) {
      throw new Error('Entity ID is required to create a goal');
    }
    return resolvedEntityId;
  }

  async function createGoal(data: Partial<Goal>): Promise<Goal> {
    const now = new Date();
    const entityId = await ensureEntity(data.entityId);
    const goal: Goal = {
      id: uuidv4(),
      name: data.name || '',
      totalTarget: data.totalTarget || 0,
      monthlyTarget: data.monthlyTarget || 0,
      savedToDate: 0,
      spentToDate: 0,
      status: 'in_progress',
      targetDate: data.targetDate,
      createdAt: now as unknown as Timestamp,
      updatedAt: now as unknown as Timestamp,
      entityId,
      notes: data.notes,
      archived: false,
    };
    goals.value.push(goal);
    await dataAccess.insertGoal(goal);
    return goal;
  }

  async function updateGoal(goalId: string, data: Partial<Goal>): Promise<void> {
    const goal = goals.value.find((g) => g.id === goalId);
    if (goal) {
      Object.assign(goal, data, { updatedAt: new Date() as unknown as Timestamp });
      await dataAccess.updateGoal(goal);
    }
  }

  async function archiveGoal(goalId: string): Promise<void> {
    const goal = goals.value.find((g) => g.id === goalId);
    if (goal) {
      goal.archived = true;
      goal.updatedAt = new Date() as unknown as Timestamp;
      await dataAccess.updateGoal(goal);
    }
  }

  async function loadGoalDetails(goalId: string): Promise<void> {
    const details = await dataAccess.getGoalDetails(goalId);
    contributions.value[goalId] = details.contributions;
    spends.value[goalId] = details.spend;
    recomputeRollups(goalId);
  }

  function addContribution(goalId: string, amount: number, month: string, note?: string): void {
    const list = contributions.value[goalId] || (contributions.value[goalId] = []);
    list.push({ goalId, amount, month, note });
    recomputeRollups(goalId);
  }

  function addGoalSpend(goalId: string, txId: string, amount: number, txDate: string, note?: string): void {
    const list = spends.value[goalId] || (spends.value[goalId] = []);
    list.push({ goalId, txId, amount, txDate, note });
    recomputeRollups(goalId);
  }

  function recomputeRollups(goalId: string): void {
    const goal = goals.value.find((g) => g.id === goalId);
    if (!goal) return;
    const saved = (contributions.value[goalId] || []).reduce((s, c) => s + c.amount, 0);
    const spent = (spends.value[goalId] || []).reduce((s, c) => s + c.amount, 0);
    goal.savedToDate = saved;
    goal.spentToDate = spent;
    if (goal.totalTarget > 0 && saved >= goal.totalTarget) {
      goal.status = 'reached';
    }
    goal.updatedAt = new Date() as unknown as Timestamp;
  }

  function contributionsForMonth(goalId: string, month: string): number {
    return (contributions.value[goalId] || [])
      .filter((c) => c.month === month)
      .reduce((s, c) => s + c.amount, 0);
  }

  function monthlySavingsTotal(entityId: string, _month: string): number {
    void _month;
    const activeGoals = listGoals(entityId);
    return activeGoals.reduce((sum, g) => sum + g.monthlyTarget, 0);
  }

  function resetForFamilyChange() {
    goals.value = [];
    contributions.value = {};
    spends.value = {};
    loadedEntities.value = new Set();
  }

  const goalCount = computed(() => goals.value.length);

  return {
    goals,
    goalCount,
    listGoals,
    listContributions,
    listGoalSpends,
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
    resetForFamilyChange,
  };
});

export function useGoals() {
  return useGoalsStore();
}

