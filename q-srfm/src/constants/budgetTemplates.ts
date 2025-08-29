import { EntityType } from '../types';
import type { BudgetCategory, TemplateBudget } from '../types';

type TemplatesMap = Partial<Record<EntityType, TemplateBudget>>;

const makeCats = (cats: Array<Partial<BudgetCategory> & Pick<BudgetCategory, 'name'>>): BudgetCategory[] =>
  cats.map((c) => {
    const base: Omit<BudgetCategory, 'carryover'> & Partial<Pick<BudgetCategory, 'carryover'>> = {
      name: c.name,
      group: c.group ?? '',
      target: c.target ?? 0,
      isFund: c.isFund ?? false,
    };
    if (c.carryover !== undefined) {
      base.carryover = c.carryover;
    }
    return base as BudgetCategory;
  });

export const DEFAULT_BUDGET_TEMPLATES: TemplatesMap = {
  [EntityType.Family]: {
    categories: makeCats([
      { name: 'Primary Income', group: 'Income', target: 0 },
      { name: 'Rent/Mortgage', group: 'Housing', target: 0 },
      { name: 'Groceries', group: 'Daily Needs', target: 0 },
      { name: 'Utilities', group: 'Housing', target: 0 },
      { name: 'Emergency Fund', group: 'Savings', target: 0, isFund: true },
    ]),
  },
  [EntityType.Business]: {
    categories: makeCats([
      { name: 'Revenue', group: 'Income', target: 0 },
      { name: 'COGS', group: 'Operations', target: 0 },
      { name: 'Rent', group: 'Overhead', target: 0 },
      { name: 'Taxes', group: 'Overhead', target: 0 },
    ]),
  },
  [EntityType.RentalProperty]: {
    categories: makeCats([
      { name: 'Rent Received', group: 'Income', target: 0 },
      { name: 'Maintenance', group: 'Expenses', target: 0 },
      { name: 'Property Tax', group: 'Expenses', target: 0 },
      { name: 'Insurance', group: 'Expenses', target: 0 },
      { name: 'Reserve Fund', group: 'Savings', target: 0, isFund: true },
    ]),
  },
  [EntityType.Other]: {
    categories: makeCats([
      { name: 'General Income', group: 'Income', target: 0 },
      { name: 'General Expense', group: 'Expenses', target: 0 },
    ]),
  },
};
