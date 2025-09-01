import { v4 as uuidv4 } from 'uuid';
import { dataAccess } from '../dataAccess';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { DEFAULT_BUDGET_TEMPLATES } from '../constants/budgetTemplates';
import { adjustTransactionDate } from './helpers';
import type { Budget, BudgetCategory, Transaction } from '../types';

/**
 * Creates a budget for the specified month and entity. If a budget already
 * exists it is returned. Otherwise a new budget is created by copying from a
 * template or the most recent existing budget. Recurring transactions from the
 * source budget are added to the new budget.
 */
export async function createBudgetForMonth(month: string, familyId: string, ownerUid: string, entityId: string): Promise<Budget> {
  const budgetStore = useBudgetStore();
  const familyStore = useFamilyStore();
  const budgetId = `${ownerUid}_${entityId}_${month}`;

  // Return existing budget if it exists
  const existingBudget = await dataAccess.getBudget(budgetId);
  if (existingBudget) {
    budgetStore.updateBudget(budgetId, existingBudget);
    return existingBudget;
  }

  const entity = familyStore.family?.entities?.find((e) => e.id === entityId);
  const templateBudget = entity?.templateBudget;

  if (templateBudget && templateBudget.categories.length > 0) {
    const newBudget: Budget = {
      familyId,
      entityId,
      month,
      incomeTarget: 0,
      categories: templateBudget.categories.map((cat: BudgetCategory) => ({
        ...cat,
        carryover: cat.isFund ? 0 : 0,
      })),
      transactions: [],
      label: `Template Budget for ${month}`,
      merchants: [],
      budgetId,
    };
    await dataAccess.saveBudget(budgetId, newBudget);
    budgetStore.updateBudget(budgetId, newBudget);
    return newBudget;
  }

  if (entity && DEFAULT_BUDGET_TEMPLATES[entity.type]) {
    const predefinedTemplate = DEFAULT_BUDGET_TEMPLATES[entity.type];
    const newBudget: Budget = {
      familyId,
      entityId,
      month,
      incomeTarget: 0,
      categories: (predefinedTemplate?.categories ?? []).map((cat: BudgetCategory) => ({
        ...cat,
        carryover: cat.isFund ? 0 : 0,
      })),
      transactions: [],
      label: `Default ${entity.type} Budget for ${month}`,
      merchants: [],
      budgetId,
    };
    await dataAccess.saveBudget(budgetId, newBudget);
    budgetStore.updateBudget(budgetId, newBudget);
    return newBudget;
  }

  const availableBudgets = Array.from(budgetStore.budgets.values()).sort((a, b) => a.month.localeCompare(b.month));
  let sourceBudget = availableBudgets.filter((b) => b.month < month && b.entityId === entityId).pop();
  if (!sourceBudget) {
    sourceBudget = availableBudgets.find((b) => b.month > month && b.entityId === entityId);
  }

  if (sourceBudget) {
    const [newYear, newMonthNum] = month.split('-').map(Number);
    const [sourceYear, sourceMonthNum] = sourceBudget.month.split('-').map(Number);
    const isFutureMonth = newYear > sourceYear || (newYear === sourceYear && newMonthNum > sourceMonthNum);

    let newCarryover: Record<string, number> = {};
    if (isFutureMonth) {
      newCarryover = dataAccess.calculateCarryOver(sourceBudget);
    }

    const newBudget: Budget = {
      familyId,
      entityId,
      month,
      incomeTarget: sourceBudget.incomeTarget,
      categories: sourceBudget.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? newCarryover[cat.name] || 0 : 0,
      })),
      label: sourceBudget.label || `Budget for ${month}`,
      merchants: sourceBudget.merchants || [],
      transactions: [],
      budgetId,
    };

    const recurringTransactions: Transaction[] = [];
    if (sourceBudget.transactions) {
      const recurringGroups: Record<string, Transaction[]> = sourceBudget.transactions.reduce(
        (groups, trx) => {
          if (!trx.deleted && trx.recurring) {
            const key = `${trx.merchant}-${trx.amount}-${trx.recurringInterval}-${trx.userId}-${trx.isIncome}`;
            (groups[key] = groups[key] || []).push(trx);
          }
          return groups;
        },
        {} as Record<string, Transaction[]>,
      );

      Object.values(recurringGroups).forEach((group) => {
        const firstInstance = group.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())[0];
        if (firstInstance.recurringInterval === 'Monthly') {
          const newDate = adjustTransactionDate(firstInstance.date, month, 'Monthly');
          recurringTransactions.push({
            ...firstInstance,
            id: uuidv4(),
            date: newDate,
            budgetMonth: month,
            entityId,
          });
        }
      });
    }

    newBudget.transactions = recurringTransactions;
    await dataAccess.saveBudget(budgetId, newBudget);
    budgetStore.updateBudget(budgetId, newBudget);
    return newBudget;
  }

  const defaultBudget: Budget = {
    familyId,
    entityId,
    month,
    incomeTarget: 0,
    categories: [
      { name: 'Income', target: 0, isFund: false, group: 'Income' },
      { name: 'Miscellaneous', target: 0, isFund: false, group: 'Expenses' },
    ],
    transactions: [],
    label: `Default Budget for ${month}`,
    merchants: [],
    budgetId,
  };
  await dataAccess.saveBudget(budgetId, defaultBudget);
  budgetStore.updateBudget(budgetId, defaultBudget);
  return defaultBudget;
}

export function sortBudgetsByMonthDesc(budgets: Budget[]): Budget[] {
  return budgets.slice().sort((a, b) => b.month.localeCompare(a.month));
}
