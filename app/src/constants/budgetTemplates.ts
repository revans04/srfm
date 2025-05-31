// src/constants/budgetTemplates.ts
import { TemplateBudget, BudgetCategory } from "@/types";

interface BudgetTemplateMap {
  [key: string]: TemplateBudget;
}

export const DEFAULT_BUDGET_TEMPLATES: BudgetTemplateMap = {
  Family: {
    categories: [
      { name: "Primary Income", target: 3000, isFund: false, group: "Income" }, 
      { name: "Groceries", target: 300, isFund: false, group: "Food" },
      { name: "Housing", target: 850, isFund: false, group: "Housing" },
      { name: "Generosity", target: 300, isFund: false, group: "Giving" },
      { name: "Electricity", target: 150, isFund: false, group: "Utilities" },
      { name: "Gas (Utilities)", target: 100, isFund: false, group: "Utilities" },
      { name: "Transportation", target: 250, isFund: false, group: "Transportation" },
      { name: "Health", target: 125, isFund: false, group: "Health" },
      { name: "Insurance", target: 250, isFund: false, group: "Insurance" },
      { name: "Entertainment", target: 125, isFund: false, group: "Lifestyle" },
      { name: "Personal Spending", target: 125, isFund: false, group: "Personal" }, 
      { name: "Savings", target: 300, isFund: true, group: "Savings" },
      { name: "Miscellaneous", target: 125, isFund: false, group: "Miscellaneous" }, 
    ],
  },
  Business: {
    categories: [
      { name: "Sales", target: 10000, isFund: false, group: "Income" }, // 100%
      { name: "Inventory Purchasing", target: 5500, isFund: false, group: "Operating Expenses" }, // 55%
      { name: "Payroll", target: 1500, isFund: false, group: "Operating Expenses" }, // 15%
      { name: "Rent", target: 800, isFund: false, group: "Operating Expenses" }, // 8%
      { name: "Platform Fees", target: 1000, isFund: false, group: "Operating Expenses" }, // 10%
      { name: "Payment Processing Fees", target: 300, isFund: false, group: "Operating Expenses" }, // 3%
      { name: "SaaS Fees", target: 300, isFund: false, group: "Operating Expenses" }, // 3%
      { name: "Shipping", target: 600, isFund: false, group: "Operating Expenses" }, // 6%
      { name: "Marketing", target: 800, isFund: false, group: "Growth" }, // 8%
      { name: "Taxes", target: 600, isFund: false, group: "Taxes" }, // 6%
      { name: "Capital Expenses Savings", target: 200, isFund: true, group: "Savings" }, // 2%
      { name: "Meals", target: 100, isFund: false, group: "Operating Expenses" }, // 1%
      { name: "Mileage", target: 100, isFund: false, group: "Operating Expenses" }, // 1%
      { name: "Office Supplies", target: 100, isFund: false, group: "Operating Expenses" }, // 1%
      { name: "Miscellaneous", target: 90, isFund: false, group: "Operating Expenses" }, // ~0.9%
    ],
  },
  RentalProperty: {
    categories: [
      { name: "Rent Received", target: 2400, isFund: false, group: "Income" },
      { name: "Mortgage", target: 960, isFund: false, group: "Fixed Costs" },
      { name: "Property Taxes", target: 240, isFund: true, group: "Fixed Costs" },
      { name: "Insurance", target: 120, isFund: true, group: "Fixed Costs" },
      { name: "Maintenance", target: 120, isFund: true, group: "Variable Costs" },
      { name: "Vacancy", target: 96, isFund: true, group: "Variable Costs" },
      { name: "Utilities", target: 144, isFund: false, group: "Variable Costs" },
      { name: "Landscaping", target: 40, isFund: true, group: "Variable Costs" },
      { name: "Pest Control", target: 25, isFund: true, group: "Variable Costs" },
      { name: "Capital Expenses Savings", target: 120, isFund: true, group: "Savings" },
    ],
  },
  Other: {
    categories: [
      { name: "Primary Income", target: 500, isFund: false, group: "Income" },
      { name: "General Expenses", target: 500, isFund: false, group: "General" },
      { name: "Miscellaneous", target: 300, isFund: false, group: "General" },
    ],
  },
};