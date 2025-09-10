import { useAuthStore } from './store/auth';
import type {
  Budget,
  BudgetInfo,
  Transaction,
  EditEvent,
  ImportedTransactionDoc,
  ImportedTransaction,
  UserData,
  Family,
  PendingInvite,
  Account,
  Snapshot,
  Entity,
  Statement,
  Goal,
  GoalContribution,
  GoalSpend,
} from './types';
import { useBudgetStore } from './store/budget';
import { Timestamp } from 'firebase/firestore';

type RawAccount = Account & { createdAt?: unknown; updatedAt?: unknown };

export class DataAccess {
  private apiBaseUrl = ((): string => {
    const env = (import.meta as unknown as { env?: Record<string, string> }).env || {};
    if (env.VITE_API_BASE_URL) return env.VITE_API_BASE_URL;
    const isLocal = typeof window !== 'undefined' && window.location.hostname === 'localhost';
    return isLocal ? 'http://localhost:8080/api' : '/api';
  })();
  // Lazily access the auth store to ensure Pinia is active
  private get authStore() {
    return useAuthStore();
  }

  private async getAuthHeaders(): Promise<HeadersInit> {
    const token = await this.authStore.user?.getIdToken();
    if (!token) throw new Error('User not authenticated');
    return {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    };
  }

  // -----------------
  // Mapping helpers
  // -----------------
  private toTimestamp(input: unknown): Timestamp | undefined {
    if (!input) return undefined;
    if (input instanceof Timestamp) return input;
    if (typeof input === 'string') {
      const d = new Date(input);
      return isNaN(d.getTime()) ? undefined : Timestamp.fromDate(d);
    }
    if (typeof input === 'object' && input !== null && ('seconds' in input || 'nanoseconds' in input || 'nanosecond' in input)) {
      const obj = input as {
        seconds?: unknown;
        nanoseconds?: unknown;
        nanosecond?: unknown;
      };
      const seconds = Number(obj.seconds ?? 0);
      const nanos = Number(obj.nanoseconds ?? obj.nanosecond ?? 0);
      return new Timestamp(seconds, nanos);
    }
    return undefined;
  }

  private mapTransaction(apiTx: unknown, budgetId?: string): Transaction {
    const tx = (apiTx ?? {}) as Record<string, unknown>;
    const categories = Array.isArray(tx.categories)
      ? (tx.categories as unknown[]).map((c) => {
          const cat = c as Record<string, unknown>;
          return {
            category: typeof cat.category === 'string' ? cat.category : '',
            amount: Number(cat.amount ?? 0),
          };
        })
      : [];

    return {
      id: typeof tx.id === 'string' ? tx.id : '',
      budgetId: typeof tx.budgetId === 'string' ? tx.budgetId : budgetId,
      date: typeof tx.date === 'string' ? tx.date : '',
      budgetMonth: typeof tx.budgetMonth === 'string' ? tx.budgetMonth : undefined,
      merchant: typeof tx.merchant === 'string' ? tx.merchant : '',
      categories,
      amount: Number(tx.amount ?? 0),
      notes: typeof tx.notes === 'string' ? tx.notes : '',
      recurring: Boolean(tx.recurring ?? false),
      recurringInterval: (tx.recurringInterval as Transaction['recurringInterval']) ?? 'Monthly',
      userId: typeof tx.userId === 'string' ? tx.userId : '',
      familyId: typeof tx.familyId === 'string' ? tx.familyId : undefined,
      isIncome: Boolean(tx.isIncome ?? false),
      accountNumber: typeof tx.accountNumber === 'string' ? tx.accountNumber : undefined,
      accountSource: typeof tx.accountSource === 'string' ? tx.accountSource : undefined,
      postedDate: typeof tx.postedDate === 'string' ? tx.postedDate : undefined,
      importedMerchant: typeof tx.importedMerchant === 'string' ? tx.importedMerchant : undefined,
      status: typeof tx.status === 'string' ? (tx.status as Transaction['status']) : undefined,
      checkNumber: typeof tx.checkNumber === 'string' ? tx.checkNumber : undefined,
      deleted: Boolean(tx.deleted ?? false),
      entityId: typeof tx.entityId === 'string' ? tx.entityId : undefined,
      taxMetadata: Array.isArray(tx.taxMetadata) ? (tx.taxMetadata as Transaction['taxMetadata']) : [],
      receiptUrl: typeof tx.receiptUrl === 'string' ? tx.receiptUrl : undefined,
    };
  }

  private mapBudget(apiBudget: unknown): Budget {
    const b = (apiBudget ?? {}) as Record<string, unknown>;
    const budgetId = typeof b.budgetId === 'string' ? b.budgetId : undefined;
    const categories = Array.isArray(b.categories)
      ? (b.categories as unknown[]).map((c) => {
          const cat = c as Record<string, unknown>;
          return {
            name: typeof cat.name === 'string' ? cat.name : '',
            target: Number(cat.target ?? 0),
            isFund: Boolean(cat.isFund ?? false),
            group: typeof cat.group === 'string' ? cat.group : '',
            carryover: typeof cat.carryover === 'number' ? cat.carryover : undefined,
          };
        })
      : [];

    const transactions = Array.isArray(b.transactions) ? (b.transactions as unknown[]).map((t) => this.mapTransaction(t, budgetId)) : [];

    return {
      budgetId,
      familyId: typeof b.familyId === 'string' ? b.familyId : '',
      entityId: typeof b.entityId === 'string' ? b.entityId : undefined,
      label: typeof b.label === 'string' ? b.label : '',
      month: typeof b.month === 'string' ? b.month : '',
      incomeTarget: Number(b.incomeTarget ?? 0),
      categories,
      transactions,
      originalBudgetId: typeof b.originalBudgetId === 'string' ? b.originalBudgetId : undefined,
      merchants: Array.isArray(b.merchants)
        ? (b.merchants as unknown[]).map((m) => {
            const merch = m as Record<string, unknown>;
            return {
              name: typeof merch.name === 'string' ? merch.name : '',
              usageCount: Number(merch.usageCount ?? 0),
            };
          })
        : [],
    };
  }

  // Budget Functions
  async loadAccessibleBudgets(userId: string, entityId?: string): Promise<BudgetInfo[]> {
    console.log('loadAccessibleBudgets', entityId);
    if (!userId) throw new Error('User ID is required to load budgets');

    const headers = await this.getAuthHeaders();
    const url = entityId ? `${this.apiBaseUrl}/budget/accessible?entityId=${entityId}` : `${this.apiBaseUrl}/budget/accessible`;
    const response = await fetch(url, { headers });
    if (!response.ok) throw new Error(`Failed to load accessible budgets: ${response.statusText}`);
    return await response.json();
  }

  async getBudget(budgetId: string): Promise<Budget | null> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}`, { headers });
    if (!response.ok) {
      if (response.status === 404) return null;
      throw new Error(`Failed to get budget ${budgetId}: ${response.statusText}`);
    }
    const raw = await response.json();
    // Ensure we always map to our strict frontend Budget shape
    const mapped = this.mapBudget(raw);
    // Backfill budgetId from the path if missing
    if (!mapped.budgetId) mapped.budgetId = budgetId;
    return mapped;
  }

  async saveBudget(budgetId: string, budget: Budget): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}`, {
      method: 'POST',
      headers,
      body: JSON.stringify(budget),
    });
    if (!response.ok) throw new Error(`Failed to save budget: ${response.statusText}`);
  }

  async deleteBudget(budgetId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}`, {
      method: 'DELETE',
      headers,
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to delete budget ${budgetId}: ${response.statusText} - ${errorText}`);
    }
    // Update local budget store
    const budgetStore = useBudgetStore();
    budgetStore.removeBudget(budgetId);
  }

  async getEditHistory(budgetId: string): Promise<EditEvent[]> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/edit-history`, { headers });
    if (!response.ok) throw new Error(`Failed to fetch edit history: ${response.statusText}`);
    return await response.json();
  }

  async getTransactions(budgetId: string): Promise<Transaction[]> {
    const budget = await this.getBudget(budgetId);
    // Always map transactions ensuring defaults
    const txs = budget?.transactions || [];
    return txs.map((t) => this.mapTransaction(t, budgetId));
  }

  async addTransaction(budgetId: string, transaction: Transaction): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/transactions`, {
      method: 'POST',
      headers,
      body: JSON.stringify(transaction),
    });
    if (!response.ok) throw new Error(`Failed to add transaction: ${response.statusText}`);
    const { transactionId } = await response.json();
    return transactionId;
  }

  async saveTransaction(budget: Budget, transaction: Transaction, futureBudgetsExist = true): Promise<Transaction> {
    const headers = await this.getAuthHeaders();
    const budgetStore = useBudgetStore();
    let retValue = null;
    if (!transaction.id) {
      const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions`, {
        method: 'POST',
        headers,
        body: JSON.stringify(transaction),
      });
      if (!response.ok) throw new Error(`Failed to add transaction: ${response.statusText}`);
      const { transactionId } = await response.json();
      retValue = { ...transaction, id: transactionId };
    } else {
      const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transaction.id}`, {
        method: 'PUT',
        headers,
        body: JSON.stringify(transaction),
      });
      if (!response.ok) throw new Error(`Failed to save transaction: ${response.statusText}`);
      retValue = transaction;
    }

    if (budget.budgetId) {
      const idx = budget.transactions.findIndex((tx) => tx.id === retValue.id);
      if (idx >= 0) {
        budget.transactions[idx] = retValue;
      } else {
        budget.transactions.push(retValue);
      }
      budgetStore.updateBudget(budget.budgetId, { ...budget });

      if (budget && budget.budgetId && futureBudgetsExist && this.hasFundCategory(transaction, budget)) {
        if (budget.entityId) {
          await this.recalculateCarryoverForFutureBudgets(
            budget.entityId,
            budget.month,
            transaction.categories,
          );
        }
      }
    }
    return retValue;
  }

  async batchSaveTransactions(budgetId: string, budget: Budget, transactions: Transaction[]): Promise<void> {
    const headers = await this.getAuthHeaders();
    const budgetStore = useBudgetStore();

    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/transactions/batch`, {
      method: 'POST',
      headers,
      body: JSON.stringify(transactions),
    });
    if (!response.ok) throw new Error(`Failed to batch save transactions: ${response.statusText}`);

    if (budget.budgetId) {
      budget.transactions = [...budget.transactions, ...transactions];
      budgetStore.updateBudget(budget.budgetId, budget);

      for (const transaction of transactions) {
        if (this.hasFundCategory(transaction, budget) && budget.budgetId && budget.entityId) {
          await this.recalculateCarryoverForFutureBudgets(
            budget.entityId,
            budget.month,
            transaction.categories,
          );
        }
      }
    }
  }

  async deleteTransaction(budget: Budget, transactionId: string, futureBudgetsExist = true): Promise<void> {
    const headers = await this.getAuthHeaders();
    const budgetStore = useBudgetStore();

    const transactionToDelete = budget.transactions?.find((t) => t.id === transactionId);
    if (!transactionToDelete) throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);

    const updatedTransaction = { ...transactionToDelete, deleted: true };
    const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
      method: 'PUT',
      headers,
      body: JSON.stringify(updatedTransaction),
    });
    if (!response.ok) throw new Error(`Failed to soft delete transaction: ${response.statusText}`);

    if (budget.budgetId) {
      budget.transactions = budget.transactions.map((t) => (t.id === transactionId ? updatedTransaction : t));
      budgetStore.updateBudget(budget.budgetId, budget);

      if (futureBudgetsExist && this.hasFundCategory(transactionToDelete, budget) && budget.entityId) {
        await this.recalculateCarryoverForFutureBudgets(
          budget.entityId,
          budget.month,
          transactionToDelete.categories,
        );
      }
    }
  }

  async restoreTransaction(budget: Budget, transactionId: string, futureBudgetsExist = true): Promise<void> {
    const headers = await this.getAuthHeaders();
    const budgetStore = useBudgetStore();

    const transactionToRestore = budget.transactions?.find((t) => t.id === transactionId);
    if (!transactionToRestore) throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);
    if (!transactionToRestore.deleted) throw new Error(`Transaction ${transactionId} is not deleted`);

    const updatedTransaction = { ...transactionToRestore, deleted: false };
    const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
      method: 'PUT',
      headers,
      body: JSON.stringify(updatedTransaction),
    });
    if (!response.ok) throw new Error(`Failed to restore transaction: ${response.statusText}`);

    if (budget.budgetId) {
      budget.transactions = budget.transactions.map((t) => (t.id === transactionId ? updatedTransaction : t));
      budgetStore.updateBudget(budget.budgetId, budget);

      if (futureBudgetsExist && this.hasFundCategory(transactionToRestore, budget) && budget.entityId) {
        await this.recalculateCarryoverForFutureBudgets(
          budget.entityId,
          budget.month,
          transactionToRestore.categories,
        );
      }
    }
  }

  async permanentlyDeleteTransaction(budget: Budget, transactionId: string, futureBudgetsExist = true): Promise<void> {
    const headers = await this.getAuthHeaders();
    const budgetStore = useBudgetStore();

    const transactionToDelete = budget.transactions?.find((t) => t.id === transactionId);
    if (!transactionToDelete) throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);

    const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
      method: 'DELETE',
      headers,
    });
    if (!response.ok) throw new Error(`Failed to permanently delete transaction: ${response.statusText}`);

    if (budget.budgetId) {
      budget.transactions = budget.transactions.filter((t) => t.id !== transactionId);
      budgetStore.updateBudget(budget.budgetId, budget);

      if (futureBudgetsExist && this.hasFundCategory(transactionToDelete, budget) && budget.entityId) {
        await this.recalculateCarryoverForFutureBudgets(
          budget.entityId,
          budget.month,
          transactionToDelete.categories,
        );
      }
    }
  }

  private hasFundCategory(transaction: Transaction, budget: Budget): boolean {
    return transaction.categories.some((cat) => budget.categories.find((bc) => bc.name === cat.category)?.isFund || false);
  }

  calculateCarryOver(budget: Budget): Record<string, number> {
    const nextCarryover: Record<string, number> = {};
    const currTrx = budget.transactions || [];

    const curSpend = currTrx
      .filter((t) => !t.deleted && !t.isIncome)
      .reduce(
        (acc, t) => {
          t.categories.forEach((split) => {
            acc[split.category] = (acc[split.category] || 0) + split.amount;
          });
          return acc;
        },
        {} as Record<string, number>,
      );

    const curIncome = currTrx
      .filter((t) => !t.deleted && t.isIncome)
      .reduce(
        (acc, t) => {
          t.categories.forEach((split) => {
            acc[split.category] = (acc[split.category] || 0) + split.amount;
          });
          return acc;
        },
        {} as Record<string, number>,
      );

    budget.categories.forEach((cat) => {
      if (cat.isFund) {
        const spent = curSpend[cat.name] || 0;
        const income = curIncome[cat.name] || 0;
        const prevCarryover = cat.carryover || 0;
        const rem = prevCarryover + cat.target + income - spent;
        nextCarryover[cat.name] = rem > 0 ? rem : 0;
      }
    });

    return nextCarryover;
  }

  async recalculateCarryoverForFutureBudgets(
    entityId: string,
    startBudgetMonth: string,
    affectedCategories: { category: string }[],
  ): Promise<void> {
    const budgetStore = useBudgetStore();
    const budgets = Array.from(budgetStore.budgets.values());

    const [startYear, startMonth] = startBudgetMonth.split('-').map(Number);
    const affectedCategoryNames = affectedCategories.map((c) => c.category);

    const futureBudgets = budgets
      .filter((b) => {
        if (b.entityId !== entityId) return false;
        const [year, month] = b.month.split('-').map(Number);
        return year > startYear || (year === startYear && month > startMonth);
      })
      .sort((a, b) => a.month.localeCompare(b.month));

    // Ensure we have category data for candidate budgets; load on demand
    const budgetList: Budget[] = [];
    for (const b of futureBudgets) {
      const id = b.budgetId;
      const budgetWithCats = b.categories && b.categories.length > 0 ? b : id ? await this.getBudget(id) : null;
      if (
        budgetWithCats &&
        budgetWithCats.categories.some((cat) => cat.isFund && affectedCategoryNames.includes(cat.name))
      ) {
        budgetList.push(budgetWithCats);
      }
    }

    if (budgetList.length === 0) return;

    // Find the starting budget for carryover calculations
    let startBudget = budgets.find((b) => b.entityId === entityId && b.month === startBudgetMonth);
    if (!startBudget) {
      // Attempt to load the budget from the API if it's not already in the store
      try {
        const infos = await this.loadAccessibleBudgets(this.authStore.user?.uid || '', entityId);
        const match = infos.find((b) => b.month === startBudgetMonth && b.budgetId);
        if (match?.budgetId) {
          startBudget = await this.getBudget(match.budgetId);
          if (startBudget) budgetStore.updateBudget(match.budgetId, startBudget);
        }
      } catch {
        /* ignore */
      }
    }

    if (!startBudget) return;

    const startCarryover = this.calculateCarryOver(startBudget);

    for (const categoryName of affectedCategoryNames) {
      let cumulativeCarryover = startCarryover[categoryName] || 0;

      for (let i = 0; i < budgetList.length; i++) {
        const budget = budgetList[i];
        if (!budget?.budgetId) continue;
        const isFirstFutureBudget = i === 0;

        const updatedCategories = budget.categories.map((cat) => {
          if (cat.isFund && cat.name === categoryName) {
            const newCarryover = isFirstFutureBudget ? cumulativeCarryover : cumulativeCarryover;
            cumulativeCarryover = newCarryover;
            return { ...cat, carryover: newCarryover };
          }
          return cat;
        });

        await this.saveBudget(budget.budgetId, { ...budget, categories: updatedCategories });
        budgetStore.updateBudget(budget.budgetId, { ...budget, categories: updatedCategories });
      }
    }
  }

  /**
   * Recalculate carryover values for all fund categories starting from the provided
   * budget month (exclusive) through the latest available budget for the same entity.
   *
   * Algorithm:
   * - Identify budgets for the entity sorted by month
   * - Starting at `startBudgetMonth`, compute the next-month carryover from each budget
   *   using calculateCarryOver(currentBudget), then write those values into the next
   *   budget's categories.carryover. Repeat sequentially until the last budget.
   */
  async recalculateCarryoverFrom(entityId: string, startBudgetMonth: string): Promise<void> {
    const budgetStore = useBudgetStore();
    // Get all budgets for the entity, sorted
    const all = Array.from(budgetStore.budgets.values()).filter((b) => b.entityId === entityId);
    all.sort((a, b) => a.month.localeCompare(b.month));

    // Ensure each budget has categories + transactions; fetch if missing
    const fullBudgets: Budget[] = [];
    for (const b of all) {
      if (b.categories?.length && Array.isArray(b.transactions)) {
        fullBudgets.push(b);
      } else if (b.budgetId) {
        const loaded = await this.getBudget(b.budgetId);
        if (loaded) {
          fullBudgets.push(loaded);
          budgetStore.updateBudget(b.budgetId, loaded);
        }
      }
    }

    if (fullBudgets.length === 0) return;

    const startIdx = fullBudgets.findIndex((b) => b.month === startBudgetMonth);
    if (startIdx === -1) return; // Nothing to do if not found

    // Walk forward, computing carryover month-by-month and applying to the next month
    for (let i = startIdx; i < fullBudgets.length - 1; i++) {
      const curr = fullBudgets[i];
      const next = fullBudgets[i + 1];
      if (!next?.budgetId) continue;

      const nextCarry = this.calculateCarryOver(curr);
      const updatedCategories = next.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? nextCarry[cat.name] || 0 : 0,
      }));

      const updatedBudget: Budget = { ...next, categories: updatedCategories };
      await this.saveBudget(next.budgetId, updatedBudget);
      budgetStore.updateBudget(next.budgetId, updatedBudget);
      // Ensure the next iteration sees the updated carryover values
      fullBudgets[i + 1] = updatedBudget;
    }
  }

  async saveImportedTransactions(doc: ImportedTransactionDoc): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions`, {
      method: 'POST',
      headers,
      body: JSON.stringify(doc),
    });
    if (!response.ok) throw new Error(`Failed to save imported transactions: ${response.statusText}`);
    const result = await response.json();
    return result.docId;
  }

  async getImportedTransactionDocs(): Promise<ImportedTransactionDoc[]> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions`, { headers });
    if (!response.ok) throw new Error(`Failed to get imported transactions: ${response.statusText}`);
    return await response.json();
  }

  async deleteImportedTransactionDoc(id: string): Promise<void> {
    console.log(`Deleting imported transactions:`, id);
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/${id}`, {
      method: 'DELETE',
      headers,
    });
    if (!response.ok) {
      const errorText = await response.text();
      console.error(`Failed to update imported transaction doc: ${response.status} ${response.statusText} - ${errorText}`);
      throw new Error(`Failed to update imported transaction doc: ${response.statusText}`);
    }
    console.log(`Imported transactions updated successfully`);
  }

  async getImportedTransactions(): Promise<ImportedTransaction[]> {
    const importedDocs = await this.getImportedTransactionDocs();
    return importedDocs.flatMap((doc) => doc.importedTransactions).sort((a, b) => b.postedDate.localeCompare(a.postedDate));
  }

  async updateImportedTransaction(docId: string, transaction: ImportedTransaction): Promise<void>;
  async updateImportedTransaction(docId: string, transactionId: string, matched?: boolean, ignored?: boolean): Promise<void>;
  async updateImportedTransaction(docId: string, transactionOrId: ImportedTransaction | string, matched?: boolean, ignored?: boolean): Promise<void> {
    const headers = await this.getAuthHeaders();
    let transactionId: string;
    let payload: unknown;

    if (typeof transactionOrId === 'string') {
      transactionId = transactionOrId;
      payload = { matched, ignored };
    } else {
      transactionId = transactionOrId.id;
      payload = transactionOrId;
    }

    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/${docId}/${transactionId}`, {
      method: 'PUT',
      headers,
      body: JSON.stringify(payload),
    });
    if (!response.ok) throw new Error(`Failed to update imported transaction: ${response.statusText}`);
  }

  async deleteImportedTransaction(docId: string, transactionId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/${docId}/${transactionId}`, {
      method: 'PUT',
      headers,
      body: JSON.stringify({ deleted: true }),
    });
    if (!response.ok) throw new Error(`Failed to delete imported transaction: ${response.statusText}`);
  }

  findImportedTransactionByKey(
    existingDocs: ImportedTransactionDoc[],
    key: {
      accountNumber: string;
      postedDate: string;
      payee: string;
      debitAmount: number;
      creditAmount: number;
    },
  ): boolean {
    return existingDocs.some((doc) =>
      doc.importedTransactions.some(
        (tx) =>
          tx.accountNumber === key.accountNumber &&
          tx.postedDate === key.postedDate &&
          tx.payee === key.payee &&
          (tx.debitAmount ?? 0) === key.debitAmount &&
          (tx.creditAmount ?? 0) === key.creditAmount,
      ),
    );
  }

  async getImportedTransactionsByAccountId(accountId: string, offset = 0, limit = 100): Promise<ImportedTransaction[]> {
    console.log(`Fetching imported transactions for accountId: ${accountId}`);
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/by-account/${accountId}?offset=${offset}&limit=${limit}`, { headers });
    if (!response.ok) {
      const errorText = await response.text();
      console.error(`Failed to fetch imported transactions: ${response.status} ${response.statusText} - ${errorText}`);
      throw new Error(`Failed to fetch imported transactions: ${response.statusText}`);
    }
    const importedTxs = await response.json();
    return importedTxs.sort((a: ImportedTransaction, b: ImportedTransaction) => b.postedDate.localeCompare(a.postedDate));
  }

  async updateImportedTransactions(transactions: ImportedTransaction[]): Promise<void> {
    console.log(`Updating imported transactions:`, transactions);
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/batch-update`, {
      method: 'POST',
      headers,
      body: JSON.stringify(transactions),
    });
    if (!response.ok) {
      const errorText = await response.text();
      console.error(`Failed to update imported transactions: ${response.status} ${response.statusText} - ${errorText}`);
      throw new Error(`Failed to update imported transactions: ${response.statusText}`);
    }
    console.log(`Imported transactions updated successfully`);
  }

  async getBudgetTransactionsMatchedToImported(accountId: string): Promise<{ budgetId: string; transaction: Transaction }[]> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/transactions/matched-to-imported/${accountId}`, { headers });
    if (!response.ok) {
      const errorText = await response.text();
      console.error(`Failed to fetch matched budget transactions: ${response.status} ${response.statusText} - ${errorText}`);
      throw new Error(`Failed to fetch matched budget transactions: ${response.statusText}`);
    }
    const budgetTxs = await response.json();
    return budgetTxs;
  }

  async updateBudgetTransactions(transactions: { budgetId: string; transaction: Transaction; oldId?: string }[]): Promise<void> {
    console.log(`Updating budget transactions:`, transactions);
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/transactions/batch-update`, {
      method: 'POST',
      headers,
      body: JSON.stringify(transactions),
    });
    if (!response.ok) {
      const errorText = await response.text();
      console.error(`Failed to update budget transactions: ${response.status} ${response.statusText} - ${errorText}`);
      throw new Error(`Failed to update budget transactions: ${response.statusText}`);
    }
    console.log(`Budget transactions updated successfully`);

    const budgetStore = useBudgetStore();
    for (const { budgetId, transaction } of transactions) {
      const budget = budgetStore.getBudget(budgetId) || (await this.getBudget(budgetId));
      if (!budget || !budget.budgetId) continue;

      const futureBudgetsExist = budgetStore.availableBudgetMonths.some((m) => m > budget.month);

      if (futureBudgetsExist && this.hasFundCategory(transaction, budget) && budget.entityId) {
        await this.recalculateCarryoverForFutureBudgets(
          budget.entityId,
          budget.month,
          transaction.categories,
        );
      }
    }
  }

  async getUserFamily(uid: string): Promise<Family | null> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${uid}`, { headers });
    if (!response.ok || response.statusText == 'No Content') return null;
    const data = await response.json();
    return data;
  }

  async createFamily(uid: string, name: string, email: string): Promise<{ familyId: string }> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/create`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ name, email }),
    });
    if (!response.ok) throw new Error(`Failed to create family: ${response.statusText}`);
    const f = await response.json();
    return f;
  }

  async addFamilyMember(familyId: string, memberUid: string, memberEmail: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/members`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ uid: memberUid, email: memberEmail }),
    });
    if (!response.ok) throw new Error(`Failed to add family member: ${response.statusText}`);
  }

  async removeFamilyMember(familyId: string, memberUid: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/members/${memberUid}`, {
      method: 'DELETE',
      headers,
    });
    if (!response.ok) throw new Error(`Failed to remove family member: ${response.statusText}`);
  }

  async renameFamily(familyId: string, newName: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/rename`, {
      method: 'PUT',
      headers,
      body: JSON.stringify({ name: newName }),
    });
    if (!response.ok) throw new Error(`Failed to rename family: ${response.statusText}`);
  }

  async inviteUser(invite: { inviterUid: string; inviterEmail: string; inviteeEmail: string }): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/invite`, {
      method: 'POST',
      headers,
      body: JSON.stringify(invite),
    });
    if (!response.ok) throw new Error(`Failed to invite user: ${response.statusText}`);
  }

  async acceptInvite(token: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/accept-invite`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ token }),
    });
    if (!response.ok) throw new Error(`Failed to accept invite: ${response.statusText}`);
  }

  async getPendingInvites(inviterUid: string): Promise<PendingInvite[]> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/pending-invites/${inviterUid}`, { headers });
    if (!response.ok) throw new Error(`Failed to get pending invites: ${response.statusText}`);
    return await response.json();
  }

  async getLastAccessed(uid: string): Promise<Timestamp | undefined> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/last-accessed/${uid}`, { headers });
    if (!response.ok) return undefined;
    const { lastAccessed } = await response.json();
    return lastAccessed ? Timestamp.fromDate(new Date(lastAccessed)) : undefined;
  }

  async getUser(userId: string): Promise<UserData | null> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/user/${userId}`, { headers });
    if (!response.ok) return null;
    return await response.json();
  }

  async getUserByEmail(email: string): Promise<UserData | null> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/user/by-email/${email}`, { headers });
    if (!response.ok) return null;
    return await response.json();
  }

  async saveUser(userId: string, userData: UserData): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/user/${userId}`, {
      method: 'POST',
      headers,
      body: JSON.stringify(userData),
    });
    if (!response.ok) throw new Error(`Failed to save user: ${response.statusText}`);
  }

  async resendVerificationEmail(): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/auth/resend-verification-email`, {
      method: 'POST',
      headers,
    });
    if (!response.ok) throw new Error(`Failed to resend verification email: ${response.statusText}`);
  }

  async batchReconcileTransactions(
    budgetId: string,
    budget: Budget,
    reconcileData: {
      budgetId: string;
      reconciliations: Array<{ budgetTransactionId: string; importedTransactionId: string; match: boolean; ignore: boolean }>;
    },
  ): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/batch-reconcile`, {
      method: 'POST',
      headers,
      body: JSON.stringify(reconcileData),
    });
    if (!response.ok) throw new Error(`Failed to batch reconcile transactions: ${response.statusText}`);

    const budgetStore = useBudgetStore();
    budgetStore.updateBudget(budgetId, budget);
  }

  unsubscribeAll(): void {
    // Placeholder for future subscription cleanup if needed
  }

  async getAccounts(familyId: string): Promise<Account[]> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts`, { headers });
    if (!response.ok) throw new Error(`Failed to fetch accounts: ${response.statusText}`);
    const raw = (await response.json()) as RawAccount[];
    return raw.map(
      (a): Account => ({
        ...a,
        createdAt: this.toTimestamp(a.createdAt) ?? Timestamp.fromDate(new Date()),
        updatedAt: this.toTimestamp(a.updatedAt) ?? Timestamp.fromDate(new Date()),
      }),
    );
  }

  async getAccount(familyId: string, accountId: string): Promise<Account | null> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountId}`, { headers });
    if (!response.ok) {
      if (response.status === 404) return null;
      throw new Error(`Failed to fetch account ${accountId}: ${response.statusText}`);
    }
    const a = (await response.json()) as RawAccount | null;
    if (!a) return null;
    const account: Account = {
      ...a,
      createdAt: this.toTimestamp(a.createdAt) ?? Timestamp.fromDate(new Date()),
      updatedAt: this.toTimestamp(a.updatedAt) ?? Timestamp.fromDate(new Date()),
    };
    return account;
  }

  async saveAccount(familyId: string, account: Account): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${account.id}`, {
      method: 'PUT',
      headers,
      body: JSON.stringify(account),
    });
    if (!response.ok) throw new Error(`Failed to save account: ${response.statusText}`);
  }

  async deleteAccount(familyId: string, accountId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountId}`, {
      method: 'DELETE',
      headers,
    });
    if (!response.ok) throw new Error(`Failed to delete account: ${response.statusText}`);
  }

  async importAccounts(familyId: string, entries: Array<Record<string, unknown>>): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/import`, {
      method: 'POST',
      headers,
      body: JSON.stringify(entries),
    });
    if (!response.ok) throw new Error(`Failed to import accounts: ${response.statusText}`);
  }

  async getSnapshots(familyId: string): Promise<Snapshot[]> {
    try {
      const headers = await this.getAuthHeaders();
      const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots`, {
        method: 'GET',
        headers,
      });
      if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      const data = (await response.json()) as Record<string, unknown>[];
      return data.map((raw) => {
        const s = raw;
        const date = this.toTimestamp(s.date ?? s.Date) ?? Timestamp.fromDate(new Date());
        const createdAt = this.toTimestamp(s.createdAt ?? s.CreatedAt) ?? Timestamp.fromDate(new Date());
        const netWorth = Number(s.netWorth ?? s.NetWorth ?? 0);
        const id =
          typeof s.id === 'string' || typeof s.id === 'number' ? String(s.id) : typeof s.Id === 'string' || typeof s.Id === 'number' ? String(s.Id) : '';
        // Keep accounts as-is if present
        const accounts = Array.isArray(s.accounts ?? s.Accounts) ? ((s.accounts ?? s.Accounts) as unknown[]) : [];
        return { id, date, netWorth, createdAt, accounts } as unknown as Snapshot;
      });
    } catch (error) {
      console.error('Error fetching snapshots:', error);
      return [];
    }
  }

  async saveSnapshot(familyId: string, snapshot: Snapshot): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/${snapshot.id}`, {
      method: 'PUT',
      headers,
      body: JSON.stringify(snapshot),
    });
    if (!response.ok) throw new Error(`Failed to save snapshot: ${response.statusText}`);
  }

  async deleteSnapshot(familyId: string, snapshotId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/${snapshotId}`, {
      method: 'DELETE',
      headers,
    });
    if (!response.ok) throw new Error(`Failed to delete snapshot: ${response.statusText}`);
  }

  async batchDeleteSnapshots(familyId: string, snapshotIds: string[]): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/batch-delete`, {
      method: 'POST',
      headers,
      body: JSON.stringify(snapshotIds),
    });
    if (!response.ok) throw new Error(`Failed to batch delete snapshots: ${response.statusText}`);
  }

  // Statement Functions
  async getStatements(familyId: string, accountNumber: string): Promise<Statement[]> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements`, { headers });
    if (!response.ok) throw new Error(`Failed to fetch statements: ${response.statusText}`);
    return await response.json();
  }

  // -----------------
  // Sync Functions
  // -----------------
  async syncFull(): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/sync/full`, {
      method: 'POST',
      headers,
    });
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to run full sync: ${response.status} ${response.statusText} - ${text}`);
    }
    try {
      const body = (await response.json()) as { message?: string };
      return body?.message || 'Full sync completed';
    } catch {
      return 'Full sync completed';
    }
  }

  async syncIncremental(sinceISO: string): Promise<string> {
    const headers = await this.getAuthHeaders();
    const url = `${this.apiBaseUrl}/sync/incremental?since=${encodeURIComponent(sinceISO)}`;
    const response = await fetch(url, { method: 'POST', headers });
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to run incremental sync: ${response.status} ${response.statusText} - ${text}`);
    }
    try {
      const body = (await response.json()) as { message?: string };
      return body?.message || 'Incremental sync completed';
    } catch {
      return 'Incremental sync completed';
    }
  }

  async syncUsers(): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/sync/users`, { method: 'POST', headers });
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to sync users: ${response.status} ${response.statusText} - ${text}`);
    }
    try {
      const body = (await response.json()) as { message?: string };
      return body?.message || 'User sync completed';
    } catch {
      return 'User sync completed';
    }
  }

  async syncFamilies(): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/sync/families`, { method: 'POST', headers });
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to sync families: ${response.status} ${response.statusText} - ${text}`);
    }
    try {
      const body = (await response.json()) as { message?: string };
      return body?.message || 'Family sync completed';
    } catch {
      return 'Family sync completed';
    }
  }

  async syncAccounts(): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/sync/accounts`, { method: 'POST', headers });
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to sync accounts: ${response.status} ${response.statusText} - ${text}`);
    }
    try {
      const body = (await response.json()) as { message?: string };
      return body?.message || 'Account sync completed';
    } catch {
      return 'Account sync completed';
    }
  }

  async syncSnapshots(): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/sync/snapshots`, { method: 'POST', headers });
    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to sync snapshots: ${response.status} ${response.statusText} - ${text}`);
    }
    try {
      const body = (await response.json()) as { message?: string };
      return body?.message || 'Snapshot sync completed';
    } catch {
      return 'Snapshot sync completed';
    }
  }

  async saveStatement(
    familyId: string,
    accountNumber: string,
    statement: Statement,
    transactionRefs: { budgetId: string; transactionId: string }[],
  ): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statement.id}`, {
      method: 'PUT',
      headers,
      body: JSON.stringify({ statement, transactions: transactionRefs }),
    });
    if (!response.ok) throw new Error(`Failed to save statement: ${response.statusText}`);
  }

  async deleteStatement(
    familyId: string,
    accountNumber: string,
    statementId: string,
    transactionRefs: { budgetId: string; transactionId: string }[],
  ): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statementId}`, {
      method: 'DELETE',
      headers,
      body: JSON.stringify({ transactions: transactionRefs }),
    });
    if (!response.ok) throw new Error(`Failed to delete statement: ${response.statusText}`);
  }

  async unreconcileStatement(
    familyId: string,
    accountNumber: string,
    statementId: string,
    transactionRefs: { budgetId: string; transactionId: string }[],
  ): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statementId}/unreconcile`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ transactions: transactionRefs }),
    });
    if (!response.ok) throw new Error(`Failed to unreconcile statement: ${response.statusText}`);
  }

  // Goal Functions
  async getGoals(entityId: string): Promise<Goal[]> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/goals?entityId=${entityId}`, { headers });
    if (!response.ok) throw new Error(`Failed to load goals: ${response.statusText}`);
    return await response.json();
  }

  private buildGoalPayload(
    goal: Goal,
  ): Omit<Goal, 'targetDate'> & { targetDate?: string } {
    let targetDate: string | undefined;
    if (goal.targetDate) {
      const td = goal.targetDate as unknown;
      if (
        typeof td === 'object' &&
        td !== null &&
        'toDate' in td &&
        typeof (td as { toDate: () => Date }).toDate === 'function'
      ) {
        targetDate = (td as { toDate: () => Date }).toDate().toISOString();
      } else if (td instanceof Date) {
        targetDate = td.toISOString();
      } else {
        targetDate = new Date(td as string).toISOString();
      }
    }
    return { ...goal, targetDate };
  }

  async insertGoal(goal: Goal): Promise<void> {
    const headers = await this.getAuthHeaders();
    const payload = this.buildGoalPayload(goal);
    const response = await fetch(`${this.apiBaseUrl}/goals`, {
      method: 'POST',
      headers,
      body: JSON.stringify(payload),
    });
    if (!response.ok) {
      const msg = await response.text();
      throw new Error(`Failed to insert goal: ${msg || response.statusText}`);
    }
  }

  async updateGoal(goal: Goal): Promise<void> {
    const headers = await this.getAuthHeaders();
    const payload = this.buildGoalPayload(goal);
    const response = await fetch(`${this.apiBaseUrl}/goals`, {
      method: 'PUT',
      headers,
      body: JSON.stringify(payload),
    });
    if (!response.ok) {
      const msg = await response.text();
      throw new Error(`Failed to update goal: ${msg || response.statusText}`);
    }
  }

  async getGoalDetails(goalId: string): Promise<{ contributions: GoalContribution[]; spend: GoalSpend[] }> {
    const headers = await this.getAuthHeaders();
    console.log('Fetching goal details from API', goalId);
    const response = await fetch(`${this.apiBaseUrl}/goals/${goalId}/details`, { headers });
    if (!response.ok) {
      console.error('Failed goal details response', response.status, await response.text());
      throw new Error(`Failed to load goal details: ${response.statusText}`);
    }
    const json = await response.json();
    console.log('Received goal details', goalId, json);
    return json;
  }

  // Entity Functions
  async createEntity(familyId: string, entity: Entity): Promise<{ entityId: string }> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities`, {
      method: 'POST',
      headers,
      body: JSON.stringify(entity),
    });
    if (!response.ok) throw new Error(`Failed to create entity: ${response.statusText}`);
    return await response.json();
  }

  async updateEntity(familyId: string, entity: Entity): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entity.id}`, {
      method: 'PUT',
      headers,
      body: JSON.stringify(entity),
    });
    if (!response.ok) throw new Error(`Failed to update entity: ${response.statusText}`);
  }

  async deleteEntity(familyId: string, entityId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}`, {
      method: 'DELETE',
      headers,
    });
    if (!response.ok) throw new Error(`Failed to delete entity: ${response.statusText}`);
  }

  async addEntityMember(familyId: string, entityId: string, member: { uid: string; email: string; role: string }): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}/members`, {
      method: 'POST',
      headers,
      body: JSON.stringify(member),
    });
    if (!response.ok) throw new Error(`Failed to add entity member: ${response.statusText}`);
  }

  async removeEntityMember(familyId: string, entityId: string, memberUid: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}/members/${memberUid}`, {
      method: 'DELETE',
      headers,
    });
    if (!response.ok) throw new Error(`Failed to remove entity member: ${response.statusText}`);
  }
}

export const dataAccess = new DataAccess();
