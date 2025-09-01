import { ref } from 'vue';
import { v4 as uuidv4 } from 'uuid';
import type { Goal, GoalContribution, GoalSpend } from '../types';
import type { Timestamp } from 'firebase/firestore';

/**
 * Composable for managing savings goals.
 *
 * Sample seed in console:
 * const { createGoal, addContribution } = useGoals();
 * const goal = await createGoal({ entityId: 'demo', name: 'Vacation', totalTarget: 5000, monthlyTarget: 500 });
 * await addContribution(goal.id, 500, '2025-08');
 */
const goals = ref<Goal[]>([]);
const contributions = ref<Record<string, GoalContribution[]>>({});
const spends = ref<Record<string, GoalSpend[]>>({});

export function useGoals() {
  function listGoals(entityId: string): Goal[] {
    return goals.value.filter((g) => g.entityId === entityId && !g.archived);
  }

  function getGoal(goalId: string): Goal | undefined {
    return goals.value.find((g) => g.id === goalId);
  }

  function createGoal(data: Partial<Goal>): Goal {
    const now = new Date();
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
      entityId: data.entityId || '',
      archived: false,
    };
    goals.value.push(goal);
    return goal;
  }

  function updateGoal(goalId: string, data: Partial<Goal>): void {
    const goal = goals.value.find((g) => g.id === goalId);
    if (goal) {
      Object.assign(goal, data, { updatedAt: new Date() as unknown as Timestamp });
    }
  }

  function archiveGoal(goalId: string): void {
    const goal = goals.value.find((g) => g.id === goalId);
    if (goal) goal.archived = true;
  }

  function addContribution(goalId: string, amount: number, month: string, note?: string): void {
    const list = contributions.value[goalId] || (contributions.value[goalId] = []);
    list.push({ goalId, amount, month, note });
    recomputeRollups(goalId);
  }

  function addGoalSpend(
    goalId: string,
    txId: string,
    amount: number,
    txDate: string,
    note?: string,
  ): void {
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
    goal.status = saved >= goal.totalTarget ? 'reached' : goal.status;
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

  return {
    listGoals,
    getGoal,
    createGoal,
    updateGoal,
    archiveGoal,
    addContribution,
    addGoalSpend,
    recomputeRollups,
    monthlySavingsTotal,
    contributionsForMonth,
  };
}
