import { EntityType } from '../types';
import type { BudgetCategory, TemplateBudget } from '../types';

type TemplatesMap = Partial<Record<EntityType, TemplateBudget>>;

// Template categories use `groupName` (a free-form string) — the backend's
// SaveBudget upserts the corresponding budget_groups row per entity on first
// save. The legacy `group` text column is gone.
type SeedCategory = Pick<BudgetCategory, 'name'> &
  Partial<Pick<BudgetCategory, 'target' | 'isFund' | 'carryover' | 'groupName'>>;

const makeCats = (cats: SeedCategory[]): BudgetCategory[] =>
  cats.map((c) => {
    const base: BudgetCategory = {
      name: c.name,
      groupName: c.groupName ?? '',
      target: c.target ?? 0,
      isFund: c.isFund ?? false,
    };
    if (c.carryover !== undefined) {
      base.carryover = c.carryover;
    }
    return base;
  });

export const DEFAULT_BUDGET_TEMPLATES: TemplatesMap = {
  [EntityType.Family]: {
    categories: makeCats([
      { name: 'Primary Income', groupName: 'Income', target: 0 },
      { name: 'Rent/Mortgage', groupName: 'Housing', target: 0 },
      { name: 'Groceries', groupName: 'Daily Needs', target: 0 },
      { name: 'Utilities', groupName: 'Housing', target: 0 },
      { name: 'Emergency Fund', groupName: 'Savings', target: 0, isFund: true },
    ]),
  },
  [EntityType.Business]: {
    categories: makeCats([
      { name: 'Revenue', groupName: 'Income', target: 0 },
      { name: 'COGS', groupName: 'Operations', target: 0 },
      { name: 'Rent', groupName: 'Overhead', target: 0 },
      { name: 'Taxes', groupName: 'Overhead', target: 0 },
    ]),
  },
  [EntityType.RentalProperty]: {
    categories: makeCats([
      { name: 'Rent Received', groupName: 'Income', target: 0 },
      { name: 'Maintenance', groupName: 'Expenses', target: 0 },
      { name: 'Property Tax', groupName: 'Expenses', target: 0 },
      { name: 'Insurance', groupName: 'Expenses', target: 0 },
      { name: 'Reserve Fund', groupName: 'Savings', target: 0, isFund: true },
    ]),
  },
  [EntityType.Other]: {
    categories: makeCats([
      { name: 'General Income', groupName: 'Income', target: 0 },
      { name: 'General Expense', groupName: 'Expenses', target: 0 },
    ]),
  },
};
