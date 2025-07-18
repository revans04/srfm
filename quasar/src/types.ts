/** types.ts */
import type { Timestamp } from "firebase/firestore";

export interface Family {
  id: string;
  name: string;
  ownerUid: string;
  members: { uid: string; email: string; role: string }[];
  accounts: Account[];
  snapshots: Snapshot[];
  entities: Entity[]; // Added for embedded entities
}

export interface Entity {
  id: string;
  familyId: string;
  name: string;
  type: EntityType;
  createdAt: Timestamp;
  updatedAt: Timestamp;
  ownerUid: string;
  members: { uid: string; email: string; role: string }[];
  templateBudget?: TemplateBudget;
  taxFormIds?: string[]; // e.g., ["form_1040", "schedule_e", "ca_form_540"]
}

export enum EntityType {
  Family = "Family",
  Business = "Business",
  RentalProperty = "RentalProperty",
  Other = "Other",
}

export interface Transaction {
  id: string;
  date: string;
  budgetMonth?: string;
  merchant: string;
  categories: Array<{
    category: string;
    amount: number;
  }>;
  amount: number;
  notes: string;
  recurring: boolean;
  recurringInterval:
    | "Daily"
    | "Weekly"
    | "Bi-Weekly"
    | "Monthly"
    | "Quarterly"
    | "Bi-Annually"
    | "Yearly";
  userId: string;
  isIncome: boolean;
  accountNumber?: string;
  accountSource?: string;
  postedDate?: string;
  importedMerchant?: string;
  status?: "U" | "C" | "R";
  checkNumber?: string;
  deleted?: boolean;
  entityId?: string;
  taxMetadata: TaxMetadata[]; // Array to support multiple forms/entities
  receiptUrl?: string; // Firebase Storage link for receipts
}

export interface ImportedTransaction {
  id: string;
  accountId: string;
  accountNumber?: string;
  accountSource?: string;
  payee: string;
  postedDate: string;
  debitAmount?: number;
  creditAmount?: number;
  checkNumber?: string;
  matched?: boolean;
  ignored?: boolean;
  status: "U" | "C" | "R";
  deleted?: boolean;
  taxMetadata?: TaxMetadata[]; // Optional, for manual tagging before matching
}

export interface ImportedTransactionDoc {
  id: string;
  userId: string;
  familyId: string;
  importedTransactions: ImportedTransaction[];
}

export interface BudgetCategory {
  name: string;
  target: number;
  isFund: boolean;
  group: string;
  carryover?: number;
}

export interface Budget {
  budgetId?: string;
  familyId: string;
  entityId?: string; // Added to link to an entity (null for primary family budget)
  label: string;
  month: string;
  incomeTarget: number;
  categories: BudgetCategory[];
  transactions: Transaction[];
  originalBudgetId?: string;
  merchants: Array<{ name: string; usageCount: number }>;
}

export interface BudgetInfo extends Budget {
  budgetId: string;
  isOwner: boolean;
}

export interface TemplateBudget {
  categories: BudgetCategory[];
}

export interface PendingInvite {
 inviterUid?: string;
 inviterEmail?: string;
 inviteeEmail?: string;
 budgetIds: string[];
 token?: string;
 createdAt: { seconds: number; nanoseconds: number };
 expiresAt: { seconds: number; nanoseconds: number };
}

export interface IncomeTarget {
  name: string;
  group: string;
  planned: number;
  received: number;
}

export interface BudgetCategoryTrx extends BudgetCategory {
  remaining: number;
  percentage: number;
  spent: number;
  transactions?: CategoryTrx[];
}

export interface CategoryTrx {
  id?: string;
  date: string; // Actual transaction date
  merchant: string;
  category: string;
  isSplit: boolean;
  amount: number;
  isIncome: boolean;
}

export interface EditEvent {
  userId: string;
  userEmail: string;
  timestamp: Timestamp;
  action: string; // e.g., "update_budget", "add_transaction", "delete_transaction"
}

export interface UserData {
  uid: string;
  email: string;
  sharedWithApp: Record<string, { canEdit: boolean }>;
}

export interface Account {
  id: string;
  userId?: string; // Optional, for personal accounts
  name: string;
  type: "Bank" | "CreditCard" | "Investment" | "Property" | "Loan";
  category: "Asset" | "Liability";
  accountNumber?: string; // For Bank/CreditCard, links to ImportedTransaction.accountNumber
  institution?: string;
  createdAt: Timestamp;
  updatedAt: Timestamp;
  balance?: number;
  details?: {
    interestRate?: number;
    appraisedValue?: number;
    maturityDate?: string;
    address?: string;
  };
}

export interface Snapshot {
  id: string; // Firestore doc ID
  date: Timestamp; // Snapshot date
  accounts: Array<{
    accountId: string;
    accountName: string;
    type: string;
    value: number; // Positive for assets, negative for liabilities
  }>;
  netWorth: number; // Precomputed sum
  createdAt: Timestamp;
}

export interface TaxMetadata {
  taxCategory: string; // e.g., "Repairs", "COGS"
  deductiblePercentage: number; // 0–100
  irsForm: string; // e.g., "Schedule E", "Schedule C"
  tags: string[]; // e.g., ["deductible", "rental_repair"]
  entityId?: string; // For split transactions across entities
}

// Tax rule for automatic categorization
export interface TaxRule {
  id: string;
  entityType: EntityType
  budgetCategory: string; // e.g., "Maintenance", "Inventory"
  taxCategory: string; // e.g., "Repairs", "COGS"
  irsForm: string; // e.g., "Schedule E", "Schedule C"
  defaultDeductiblePercentage: number; // 0–100
  tags: string[]; // e.g., ["deductible", "rental_repair"]
}

// IRS form template
export interface TaxForm {
  id: string;
  name: string; // e.g., "Schedule E", "Schedule C"
  description: string; // e.g., "Supplemental Income and Loss (Rentals)"
  applicableEntityTypes: EntityType[]; // e.g., [EntityType.RentalProperty]
  taxRules: string[]; // Array of TaxRule IDs
  state?: string; // e.g., "AL", "CA", or undefined for federal
}

export enum AccountType {
  Bank = "Bank",
  CreditCard = "CreditCard",
  Investment = "Investment",
  Property = "Property",
  Loan = "Loan",
}

export interface Statement {
  id: string;
  accountNumber: string;
  startDate: string;
  startingBalance: number;
  endDate: string;
  endingBalance: number;
  reconciled: boolean;
}
