import { currentMonthISO } from '../utils/helpers';
import { useGoals } from './useGoals';

export function useGoalNudges() {
  const { listGoals, contributionsForMonth } = useGoals();

  function getNudges(entityId: string, month = currentMonthISO()): Promise<string[]> {
    const today = new Date();
    if (today.getDate() <= 20) return Promise.resolve([]);
    const goals = listGoals(entityId);
    const msgs = goals
      .map((g) => {
        const contrib = contributionsForMonth(g.id, month);
        const diff = g.monthlyTarget - contrib;
        return diff > 0 ? `You're behind on ${g.name} by $${diff.toFixed(2)}` : null;
      })
      .filter((m): m is string => Boolean(m));
    return Promise.resolve(msgs);
  }

  return { getNudges };
}
