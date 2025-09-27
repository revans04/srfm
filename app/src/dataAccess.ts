import { auth } from "./firebase";
import {
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
} from "./types";
import { useBudgetStore } from "./store/budget";
import { Timestamp } from "firebase/firestore";

export class DataAccess {
  private apiBaseUrl = process.env.VUE_APP_API_BASE_URL || "http://localhost:8080/api";

  private async getAuthHeaders(): Promise<HeadersInit> {
    const token = await auth.currentUser?.getIdToken();
    if (!token) throw new Error("User not authenticated");
    return {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    };
  }

  // Budget Functions
  async loadAccessibleBudgets(userId: string, entityId?: string): Promise<BudgetInfo[]> {
    if (!userId) throw new Error("User ID is required to load budgets");

    const headers = await this.getAuthHeaders();
    const url = entityId ? `${this.apiBaseUrl}/budget/accessible?entityId=${entityId}` : `${this.apiBaseUrl}/budget/accessible`;
    const response = await fetch(url, { headers });
    if (!response.ok) throw new Error(`Failed to load accessible budgets: ${response.statusText}`);
    const budgets: BudgetInfo[] = await response.json();
    return budgets.map((b) => ({ ...b, transactions: [] }));
  }

  async getBudget(budgetId: string): Promise<Budget | null> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}`, { headers });
    if (!response.ok) {
      if (response.status === 404) return null;
      throw new Error(`Failed to get budget ${budgetId}: ${response.statusText}`);
    }
    return await response.json();
  }

  async saveBudget(budgetId: string, budget: Budget, options?: { skipCarryoverRecalc?: boolean }): Promise<void> {
    const headers = await this.getAuthHeaders();
    const params = options?.skipCarryoverRecalc ? "?skipCarryover=true" : "";
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}${params}`, {
      method: "POST",
      headers,
      body: JSON.stringify(budget),
    });
    if (!response.ok) throw new Error(`Failed to save budget: ${response.statusText}`);
  }

  async recalculateCarryover(budgetId: string, categoryNames: string[]): Promise<void> {
    if (categoryNames.length === 0) {
      return;
    }
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/carryover/recalculate`, {
      method: "POST",
      headers,
      body: JSON.stringify({ categoryNames }),
    });
    if (!response.ok) {
      throw new Error(`Failed to recalculate carryover: ${response.statusText}`);
    }
  }

  async deleteBudget(budgetId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}`, {
      method: "DELETE",
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
    return budget?.transactions || [];
  }

  async addTransaction(budgetId: string, transaction: Transaction): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/transactions`, {
      method: "POST",
      headers,
      body: JSON.stringify(transaction),
    });
    if (!response.ok) throw new Error(`Failed to add transaction: ${response.statusText}`);
    const { transactionId } = await response.json();
    return transactionId;
  }

  async saveTransaction(budget: Budget, transaction: Transaction): Promise<Transaction> {
    const headers = await this.getAuthHeaders();
    let retValue = null;
    if (!transaction.id) {
      const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions`, {
        method: "POST",
        headers,
        body: JSON.stringify(transaction),
      });
      if (!response.ok) throw new Error(`Failed to add transaction: ${response.statusText}`);
      const { transactionId } = await response.json();
      retValue = { ...transaction, id: transactionId };
    } else {
      const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transaction.id}`, {
        method: "PUT",
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
    }
    return retValue;
  }

  async batchSaveTransactions(
    budgetId: string,
    budget: Budget,
    transactions: Transaction[],
    options?: { skipCarryoverRecalc?: boolean },
  ): Promise<void> {
    if (transactions.length === 0) {
      return;
    }

    const headers = await this.getAuthHeaders();
    const query = options?.skipCarryoverRecalc ? "?skipCarryover=true" : "";

    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/transactions/batch${query}`, {
      method: "POST",
      headers,
      body: JSON.stringify(transactions),
    });
    if (!response.ok) throw new Error(`Failed to batch save transactions: ${response.statusText}`);

    if (budget.budgetId) {
      budget.transactions = [...budget.transactions, ...transactions];
    }
  }

  async deleteTransaction(budget: Budget, transactionId: string): Promise<void> {
    const headers = await this.getAuthHeaders();

    const transactionToDelete = budget.transactions?.find((t) => t.id === transactionId);
    if (!transactionToDelete) throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);

    const updatedTransaction = { ...transactionToDelete, deleted: true };
    const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
      method: "PUT",
      headers,
      body: JSON.stringify(updatedTransaction),
    });
    if (!response.ok) throw new Error(`Failed to soft delete transaction: ${response.statusText}`);

    if (budget.budgetId) {
      budget.transactions = budget.transactions.map((t) => (t.id === transactionId ? updatedTransaction : t));
    }
  }

  async restoreTransaction(budget: Budget, transactionId: string): Promise<void> {
    const headers = await this.getAuthHeaders();

    const transactionToRestore = budget.transactions?.find((t) => t.id === transactionId);
    if (!transactionToRestore) throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);
    if (!transactionToRestore.deleted) throw new Error(`Transaction ${transactionId} is not deleted`);

    const updatedTransaction = { ...transactionToRestore, deleted: false };
    const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
      method: "PUT",
      headers,
      body: JSON.stringify(updatedTransaction),
    });
    if (!response.ok) throw new Error(`Failed to restore transaction: ${response.statusText}`);

    if (budget.budgetId) {
      budget.transactions = budget.transactions.map((t) => (t.id === transactionId ? updatedTransaction : t));
    }
  }

  async permanentlyDeleteTransaction(budget: Budget, transactionId: string): Promise<void> {
    const headers = await this.getAuthHeaders();

    const transactionToDelete = budget.transactions?.find((t) => t.id === transactionId);
    if (!transactionToDelete) throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);

    const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
      method: "DELETE",
      headers,
    });
    if (!response.ok) throw new Error(`Failed to permanently delete transaction: ${response.statusText}`);

    if (budget.budgetId) {
      budget.transactions = budget.transactions.filter((t) => t.id !== transactionId);
    }
  }

  async calculateCarryOver(budget: Budget): Promise<Record<string, number>> {
    const nextCarryover: Record<string, number> = {};
    const currTrx = budget.transactions || [];

    const curSpend = currTrx
      .filter((t) => !t.deleted && !t.isIncome)
      .reduce((acc, t) => {
        t.categories.forEach((split) => {
          acc[split.category] = (acc[split.category] || 0) + split.amount;
        });
        return acc;
      }, {} as Record<string, number>);

    const curIncome = currTrx
      .filter((t) => !t.deleted && t.isIncome)
      .reduce((acc, t) => {
        t.categories.forEach((split) => {
          acc[split.category] = (acc[split.category] || 0) + split.amount;
        });
        return acc;
      }, {} as Record<string, number>);

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
    userId: string,
    entityId: string,
    startBudgetMonth: string,
    affectedCategories: { category: string }[]
  ): Promise<void> {
    const budgetStore = useBudgetStore();
    const budgets = Array.from(budgetStore.budgets.values());

    const [startYear, startMonth] = startBudgetMonth.split("-").map(Number);
    const affectedCategoryNames = affectedCategories.map((c) => c.category);

    const futureBudgets = budgets
      .filter((b) => {
        const [year, month] = b.month.split("-").map(Number);
        return year > startYear || (year === startYear && month > startMonth);
      })
      .sort((a, b) => a.month.localeCompare(b.month));

    // Ensure category data is available; load budget details when missing
    const budgetList: Budget[] = [];
    for (const b of futureBudgets) {
      const budgetWithCats = b.categories && b.categories.length > 0 ? b : (await this.getBudget(b.budgetId!))!;
      if (budgetWithCats && budgetWithCats.categories.some((cat) => cat.isFund && affectedCategoryNames.includes(cat.name))) {
        budgetList.push(budgetWithCats);
      }
    }

    if (budgetList.length === 0) return;

    const startBudget =
      budgetStore.getBudget(`${userId}_${entityId}_${startBudgetMonth}`) || (await this.getBudget(`${userId}_${entityId}_${startBudgetMonth}`));
    if (!startBudget) return;

    const startCarryover = await this.calculateCarryOver(startBudget);

    for (const categoryName of affectedCategoryNames) {
      let cumulativeCarryover = startCarryover[categoryName] || 0;

      for (let i = 0; i < budgetList.length; i++) {
        const budget = budgetList[i];
        const isFirstFutureBudget = i === 0;

        const updatedCategories = budget.categories.map((cat) => {
          if (cat.isFund && cat.name === categoryName) {
            const newCarryover = isFirstFutureBudget ? cumulativeCarryover : cumulativeCarryover;
            cumulativeCarryover = newCarryover;
            return { ...cat, carryover: newCarryover };
          }
          return cat;
        });

        await this.saveBudget(budget.budgetId!, { ...budget, categories: updatedCategories });
        budgetStore.updateBudget(budget.budgetId!, { ...budget, categories: updatedCategories });
      }
    }
  }

  async saveImportedTransactions(doc: ImportedTransactionDoc): Promise<string> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions`, {
      method: "POST",
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
      method: "DELETE",
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
    return importedDocs.flatMap((doc) => doc.importedTransactions);
  }

  async updateImportedTransaction(docId: string, transaction: ImportedTransaction): Promise<void>;
  async updateImportedTransaction(docId: string, transactionId: string, matched?: boolean, ignored?: boolean): Promise<void>;
  async updateImportedTransaction(docId: string, transactionOrId: ImportedTransaction | string, matched?: boolean, ignored?: boolean): Promise<void> {
    const headers = await this.getAuthHeaders();
    let transactionId: string;
    let payload: any;

    if (typeof transactionOrId === "string") {
      transactionId = transactionOrId;
      payload = { matched, ignored };
    } else {
      transactionId = transactionOrId.id;
      payload = transactionOrId;
    }

    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/${docId}/${transactionId}`, {
      method: "PUT",
      headers,
      body: JSON.stringify(payload),
    });
    if (!response.ok) throw new Error(`Failed to update imported transaction: ${response.statusText}`);
  }

  async deleteImportedTransaction(docId: string, transactionId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/${docId}/${transactionId}`, {
      method: "PUT",
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
    }
  ): boolean {
    return existingDocs.some((doc) =>
      doc.importedTransactions.some(
        (tx) =>
          tx.accountNumber === key.accountNumber &&
          tx.postedDate === key.postedDate &&
          tx.payee === key.payee &&
          (tx.debitAmount ?? 0) === key.debitAmount &&
          (tx.creditAmount ?? 0) === key.creditAmount
      )
    );
  }

  async getImportedTransactionsByAccountId(accountId: string): Promise<ImportedTransaction[]> {
    console.log(`Fetching imported transactions for accountId: ${accountId}`);
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/by-account/${accountId}`, { headers });
    if (!response.ok) {
      const errorText = await response.text();
      console.error(`Failed to fetch imported transactions: ${response.status} ${response.statusText} - ${errorText}`);
      throw new Error(`Failed to fetch imported transactions: ${response.statusText}`);
    }
    const importedTxs = await response.json();
    return importedTxs;
  }

  async updateImportedTransactions(transactions: ImportedTransaction[]): Promise<void> {
    console.log(`Updating imported transactions:`, transactions);
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/batch-update`, {
      method: "POST",
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

  async updateBudgetTransactions(
    transactions: { budgetId: string; transaction: Transaction; oldId?: string }[]
  ): Promise<void> {
    console.log(`Updating budget transactions:`, transactions);
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/budget/transactions/batch-update`, {
      method: "POST",
      headers,
      body: JSON.stringify(transactions),
    });
    if (!response.ok) {
      const errorText = await response.text();
      console.error(`Failed to update budget transactions: ${response.status} ${response.statusText} - ${errorText}`);
      throw new Error(`Failed to update budget transactions: ${response.statusText}`);
    }
    console.log(`Budget transactions updated successfully`);
  }

  async getUserFamily(uid: string): Promise<Family | null> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${uid}`, { headers });
    if (!response.ok || response.statusText == "No Content") return null;
    const data = await response.json();
    return data;
  }

  async createFamily(uid: string, name: string, email: string): Promise<{ familyId: string }> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/create`, {
      method: "POST",
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
      method: "POST",
      headers,
      body: JSON.stringify({ uid: memberUid, email: memberEmail }),
    });
    if (!response.ok) throw new Error(`Failed to add family member: ${response.statusText}`);
  }

  async removeFamilyMember(familyId: string, memberUid: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/members/${memberUid}`, {
      method: "DELETE",
      headers,
    });
    if (!response.ok) throw new Error(`Failed to remove family member: ${response.statusText}`);
  }

  async renameFamily(familyId: string, newName: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/rename`, {
      method: "PUT",
      headers,
      body: JSON.stringify({ name: newName }),
    });
    if (!response.ok) throw new Error(`Failed to rename family: ${response.statusText}`);
  }

  async inviteUser(invite: { inviterUid: string; inviterEmail: string; inviteeEmail: string }): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/invite`, {
      method: "POST",
      headers,
      body: JSON.stringify(invite),
    });
    if (!response.ok) throw new Error(`Failed to invite user: ${response.statusText}`);
  }

  async acceptInvite(token: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/accept-invite`, {
      method: "POST",
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
      method: "POST",
      headers,
      body: JSON.stringify(userData),
    });
    if (!response.ok) throw new Error(`Failed to save user: ${response.statusText}`);
  }

  async resendVerificationEmail(): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/auth/resend-verification-email`, {
      method: "POST",
      headers,
    });
    if (!response.ok) throw new Error(`Failed to resend verification email: ${response.statusText}`);
  }

  async batchReconcileTransactions(
    budgetId: string,
    budget: Budget,
    reconcileData: { budgetId: string; reconciliations: Array<{ budgetTransactionId: string; importedTransactionId: string; match: boolean; ignore: boolean }> }
  ): Promise<void> {
    const headers = await this.getAuthHeaders();
    console.log("batchReconcileTransactions payload", budgetId, reconcileData);
    const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/batch-reconcile`, {
      method: "POST",
      headers,
      body: JSON.stringify(reconcileData),
    });
    if (!response.ok) {
      const text = await response.text();
      console.error("batchReconcileTransactions failed", response.status, text);
      throw new Error(`Failed to batch reconcile transactions: ${response.statusText}`);
    }
  }

  unsubscribeAll(): void {
    // Placeholder for future subscription cleanup if needed
  }

  async getAccounts(familyId: string): Promise<Account[]> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts`, { headers });
    if (!response.ok) throw new Error(`Failed to fetch accounts: ${response.statusText}`);
    return await response.json();
  }

  async getAccount(familyId: string, accountId: string): Promise<Account | null> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountId}`, { headers });
    if (!response.ok) {
      if (response.status === 404) return null;
      throw new Error(`Failed to fetch account ${accountId}: ${response.statusText}`);
    }
    return await response.json();
  }

  async saveAccount(familyId: string, account: Account): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${account.id}`, {
      method: "PUT",
      headers,
      body: JSON.stringify(account),
    });
    if (!response.ok) throw new Error(`Failed to save account: ${response.statusText}`);
  }

  async deleteAccount(familyId: string, accountId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountId}`, {
      method: "DELETE",
      headers,
    });
    if (!response.ok) throw new Error(`Failed to delete account: ${response.statusText}`);
  }

  async importAccounts(familyId: string, entries: any[]): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/import`, {
      method: "POST",
      headers,
      body: JSON.stringify(entries),
    });
    if (!response.ok) throw new Error(`Failed to import accounts: ${response.statusText}`);
  }

  async getSnapshots(familyId: string): Promise<Snapshot[]> {
    try {
      const headers = await this.getAuthHeaders();
      const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots`, {
        method: "GET",
        headers,
      });
      if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      const data = await response.json();
      return data.map((s: any) => ({
        ...s,
        date:
          typeof s.date === "string"
            ? Timestamp.fromDate(new Date(s.date))
            : new Timestamp(s.date.seconds, s.date.nanoseconds),
        createdAt:
          typeof s.createdAt === "string"
            ? Timestamp.fromDate(new Date(s.createdAt))
            : new Timestamp(s.createdAt.seconds, s.createdAt.nanoseconds),
      }));
    } catch (error) {
      console.error("Error fetching snapshots:", error);
      return [];
    }
  }

  async saveSnapshot(familyId: string, snapshot: Snapshot): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/${snapshot.id}`, {
      method: "PUT",
      headers,
      body: JSON.stringify(snapshot),
    });
    if (!response.ok) throw new Error(`Failed to save snapshot: ${response.statusText}`);
  }

  async deleteSnapshot(familyId: string, snapshotId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/${snapshotId}`, {
      method: "DELETE",
      headers,
    });
    if (!response.ok) throw new Error(`Failed to delete snapshot: ${response.statusText}`);
  }

  async batchDeleteSnapshots(familyId: string, snapshotIds: string[]): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/batch-delete`, {
      method: "POST",
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

  async saveStatement(
    familyId: string,
    accountNumber: string,
    statement: Statement,
    transactionRefs: { budgetId: string; transactionId: string }[]
  ): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statement.id}`, {
      method: "PUT",
      headers,
      body: JSON.stringify({ statement, transactions: transactionRefs }),
    });
    if (!response.ok) throw new Error(`Failed to save statement: ${response.statusText}`);
  }

  async deleteStatement(
    familyId: string,
    accountNumber: string,
    statementId: string,
    transactionRefs: { budgetId: string; transactionId: string }[]
  ): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statementId}`, {
      method: "DELETE",
      headers,
      body: JSON.stringify({ transactions: transactionRefs }),
    });
    if (!response.ok) throw new Error(`Failed to delete statement: ${response.statusText}`);
  }

  async unreconcileStatement(
    familyId: string,
    accountNumber: string,
    statementId: string,
    transactionRefs: { budgetId: string; transactionId: string }[]
  ): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statementId}/unreconcile`, {
      method: "POST",
      headers,
      body: JSON.stringify({ transactions: transactionRefs }),
    });
    if (!response.ok) throw new Error(`Failed to unreconcile statement: ${response.statusText}`);
  }

  // Entity Functions
  async createEntity(familyId: string, entity: Entity): Promise<{ entityId: string }> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities`, {
      method: "POST",
      headers,
      body: JSON.stringify(entity),
    });
    if (!response.ok) throw new Error(`Failed to create entity: ${response.statusText}`);
    return await response.json();
  }

  async updateEntity(familyId: string, entity: Entity): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entity.id}`, {
      method: "PUT",
      headers,
      body: JSON.stringify(entity),
    });
    if (!response.ok) throw new Error(`Failed to update entity: ${response.statusText}`);
  }

  async deleteEntity(familyId: string, entityId: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}`, {
      method: "DELETE",
      headers,
    });
    if (!response.ok) throw new Error(`Failed to delete entity: ${response.statusText}`);
  }

  async addEntityMember(familyId: string, entityId: string, member: { uid: string; email: string; role: string }): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}/members`, {
      method: "POST",
      headers,
      body: JSON.stringify(member),
    });
    if (!response.ok) throw new Error(`Failed to add entity member: ${response.statusText}`);
  }

  async removeEntityMember(familyId: string, entityId: string, memberUid: string): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}/members/${memberUid}`, {
      method: "DELETE",
      headers,
    });
    if (!response.ok) throw new Error(`Failed to remove entity member: ${response.statusText}`);
  }

  async syncFirestoreToSupabase(): Promise<void> {
    const headers = await this.getAuthHeaders();
    const response = await fetch(`${this.apiBaseUrl}/sync/full`, {
      method: "POST",
      headers,
    });
    if (!response.ok) throw new Error(`Failed to sync data: ${response.statusText}`);
  }
}

export const dataAccess = new DataAccess();
