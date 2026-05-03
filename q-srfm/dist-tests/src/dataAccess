import { useAuthStore } from './store/auth';
import { useBudgetStore } from './store/budget';
import { Timestamp } from 'firebase/firestore';
import { toStatementFinalizeRequestBody } from './utils/statements';
import { calculateCarryOver } from './utils/carryover';
export class DataAccess {
    constructor() {
        this.apiBaseUrl = (() => {
            const env = import.meta.env || {};
            if (env.VITE_API_BASE_URL)
                return env.VITE_API_BASE_URL;
            const isLocal = typeof window !== 'undefined' && window.location.hostname === 'localhost';
            return isLocal ? 'http://localhost:8080/api' : '/api';
        })();
    }
    // Lazily access the auth store to ensure Pinia is active
    get authStore() {
        return useAuthStore();
    }
    async getAuthHeaders() {
        const token = await this.authStore.user?.getIdToken();
        if (!token)
            throw new Error('User not authenticated');
        return {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
        };
    }
    // -----------------
    // Mapping helpers
    // -----------------
    toTimestamp(input) {
        if (!input)
            return undefined;
        if (input instanceof Timestamp)
            return input;
        if (typeof input === 'string') {
            const d = new Date(input);
            return isNaN(d.getTime()) ? undefined : Timestamp.fromDate(d);
        }
        if (typeof input === 'object' && input !== null && ('seconds' in input || 'nanoseconds' in input || 'nanosecond' in input)) {
            const obj = input;
            const seconds = Number(obj.seconds ?? 0);
            const nanos = Number(obj.nanoseconds ?? obj.nanosecond ?? 0);
            return new Timestamp(seconds, nanos);
        }
        return undefined;
    }
    toIsoString(input) {
        const ts = this.toTimestamp(input);
        if (ts) {
            return ts.toDate().toISOString();
        }
        if (typeof input === 'string') {
            const parsed = new Date(input);
            return Number.isNaN(parsed.getTime()) ? undefined : parsed.toISOString();
        }
        return undefined;
    }
    mapTransaction(apiTx, budgetId) {
        const tx = (apiTx ?? {});
        const categories = Array.isArray(tx.categories)
            ? tx.categories.map((c) => {
                const cat = c;
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
            recurringInterval: tx.recurringInterval ?? 'Monthly',
            userId: typeof tx.userId === 'string' ? tx.userId : '',
            familyId: typeof tx.familyId === 'string' ? tx.familyId : undefined,
            isIncome: Boolean(tx.isIncome ?? false),
            accountNumber: typeof tx.accountNumber === 'string' ? tx.accountNumber : undefined,
            accountSource: typeof tx.accountSource === 'string' ? tx.accountSource : undefined,
            transactionDate: typeof tx.transactionDate === 'string' ? tx.transactionDate : undefined,
            postedDate: typeof tx.postedDate === 'string' ? tx.postedDate : undefined,
            importedMerchant: typeof tx.importedMerchant === 'string' ? tx.importedMerchant : undefined,
            status: typeof tx.status === 'string' ? tx.status : undefined,
            checkNumber: typeof tx.checkNumber === 'string' ? tx.checkNumber : undefined,
            deleted: Boolean(tx.deleted ?? false),
            entityId: typeof tx.entityId === 'string' ? tx.entityId : undefined,
            taxMetadata: Array.isArray(tx.taxMetadata) ? tx.taxMetadata : [],
            receiptUrl: typeof tx.receiptUrl === 'string' ? tx.receiptUrl : undefined,
            transactionType: typeof tx.transactionType === 'string'
                ? tx.transactionType
                : 'standard',
            fundedByGoalId: typeof tx.fundedByGoalId === 'string' && tx.fundedByGoalId.length > 0
                ? tx.fundedByGoalId
                : undefined,
        };
    }
    mapBudget(apiBudget) {
        const b = (apiBudget ?? {});
        const budgetId = typeof b.budgetId === 'string' ? b.budgetId : undefined;
        const categories = Array.isArray(b.categories)
            ? b.categories.map((c) => {
                const cat = c;
                return {
                    id: typeof cat.id === 'number' ? cat.id : undefined,
                    name: typeof cat.name === 'string' ? cat.name : '',
                    target: Number(cat.target ?? 0),
                    isFund: Boolean(cat.isFund ?? false),
                    groupId: typeof cat.groupId === 'string' ? cat.groupId : undefined,
                    groupName: typeof cat.groupName === 'string' ? cat.groupName : undefined,
                    sortOrder: typeof cat.sortOrder === 'number' ? cat.sortOrder : 0,
                    carryover: typeof cat.carryover === 'number' ? cat.carryover : undefined,
                    favorite: typeof cat.favorite === 'boolean'
                        ? (cat.favorite)
                        : undefined,
                    fundingSourceCategory: typeof cat.fundingSourceCategory === 'string' && cat.fundingSourceCategory.length > 0
                        ? cat.fundingSourceCategory
                        : undefined,
                    fundingSourceGoalId: typeof cat.fundingSourceGoalId === 'string' && cat.fundingSourceGoalId.length > 0
                        ? cat.fundingSourceGoalId
                        : undefined,
                };
            })
            : [];
        const transactions = Array.isArray(b.transactions) ? b.transactions.map((t) => this.mapTransaction(t, budgetId)) : [];
        const groups = Array.isArray(b.groups) ? b.groups.map((g) => this.mapGroup(g)) : [];
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
                ? b.merchants.map((m) => {
                    const merch = m;
                    return {
                        name: typeof merch.name === 'string' ? merch.name : '',
                        usageCount: Number(merch.usageCount ?? 0),
                    };
                })
                : [],
            groups,
        };
    }
    mapGroup(raw) {
        const g = (raw ?? {});
        const kindRaw = typeof g.kind === 'string' ? g.kind : 'expense';
        const kind = kindRaw === 'income' || kindRaw === 'savings' ? kindRaw : 'expense';
        return {
            id: typeof g.id === 'string' ? g.id : '',
            entityId: typeof g.entityId === 'string' ? g.entityId : '',
            name: typeof g.name === 'string' ? g.name : '',
            sortOrder: typeof g.sortOrder === 'number' ? g.sortOrder : 0,
            archived: Boolean(g.archived ?? false),
            kind,
            color: typeof g.color === 'string' ? g.color : undefined,
            icon: typeof g.icon === 'string' ? g.icon : undefined,
            collapsedDefault: typeof g.collapsedDefault === 'boolean' ? g.collapsedDefault : undefined,
        };
    }
    // Budget Functions
    async loadAccessibleBudgets(userId, entityId) {
        console.log('loadAccessibleBudgets', entityId);
        if (!userId)
            throw new Error('User ID is required to load budgets');
        const headers = await this.getAuthHeaders();
        const url = entityId ? `${this.apiBaseUrl}/budget/accessible?entityId=${entityId}` : `${this.apiBaseUrl}/budget/accessible`;
        const response = await fetch(url, { headers });
        if (!response.ok)
            throw new Error(`Failed to load accessible budgets: ${response.statusText}`);
        return await response.json();
    }
    async getBudgetsBatch(budgetIds) {
        if (budgetIds.length === 0)
            return [];
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/batch?ids=${budgetIds.join(',')}`, { headers });
        if (!response.ok)
            throw new Error(`Failed to get budgets batch: ${response.statusText}`);
        const raw = await response.json();
        return raw.map((r) => this.mapBudget(r)).map((b, i) => {
            if (!b.budgetId)
                b.budgetId = budgetIds[i];
            return b;
        });
    }
    async getBudget(budgetId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}`, { headers });
        if (!response.ok) {
            if (response.status === 404)
                return null;
            throw new Error(`Failed to get budget ${budgetId}: ${response.statusText}`);
        }
        const raw = await response.json();
        // Ensure we always map to our strict frontend Budget shape
        const mapped = this.mapBudget(raw);
        // Backfill budgetId from the path if missing
        if (!mapped.budgetId)
            mapped.budgetId = budgetId;
        return mapped;
    }
    async saveBudget(budgetId, budget, options) {
        const headers = await this.getAuthHeaders();
        const params = options?.skipCarryoverRecalc ? '?skipCarryover=true' : '';
        const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}${params}`, {
            method: 'POST',
            headers,
            body: JSON.stringify(budget),
        });
        if (!response.ok)
            throw new Error(`Failed to save budget: ${response.statusText}`);
    }
    async recalculateCarryover(budgetId, categoryNames) {
        if (categoryNames.length === 0) {
            return;
        }
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/carryover/recalculate`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ categoryNames }),
        });
        if (!response.ok) {
            throw new Error(`Failed to recalculate carryover: ${response.statusText}`);
        }
    }
    async deleteBudget(budgetId) {
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
    async mergeBudgets(targetBudgetId, sourceBudgetId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/merge`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ targetBudgetId, sourceBudgetId }),
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to merge budgets: ${response.statusText}${errorText ? ` - ${errorText}` : ''}`);
        }
        return await response.json();
    }
    async getEditHistory(budgetId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/edit-history`, { headers });
        if (!response.ok)
            throw new Error(`Failed to fetch edit history: ${response.statusText}`);
        return await response.json();
    }
    async getTransactions(budgetId) {
        const budget = await this.getBudget(budgetId);
        // Always map transactions ensuring defaults
        const txs = budget?.transactions || [];
        return txs.map((t) => this.mapTransaction(t, budgetId));
    }
    async addTransaction(budgetId, transaction) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/transactions`, {
            method: 'POST',
            headers,
            body: JSON.stringify(transaction),
        });
        if (!response.ok)
            throw new Error(`Failed to add transaction: ${response.statusText}`);
        const { transactionId } = await response.json();
        return transactionId;
    }
    async saveTransaction(budget, transaction) {
        const headers = await this.getAuthHeaders();
        const budgetStore = useBudgetStore();
        let retValue = null;
        if (!transaction.id) {
            const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions`, {
                method: 'POST',
                headers,
                body: JSON.stringify(transaction),
            });
            if (!response.ok)
                throw new Error(`Failed to add transaction: ${response.statusText}`);
            const { transactionId } = await response.json();
            retValue = { ...transaction, id: transactionId };
        }
        else {
            const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transaction.id}`, {
                method: 'PUT',
                headers,
                body: JSON.stringify(transaction),
            });
            if (!response.ok)
                throw new Error(`Failed to save transaction: ${response.statusText}`);
            retValue = transaction;
        }
        if (budget.budgetId) {
            const idx = budget.transactions.findIndex((tx) => tx.id === retValue.id);
            if (idx >= 0) {
                budget.transactions[idx] = retValue;
            }
            else {
                budget.transactions.push(retValue);
            }
            budgetStore.updateBudget(budget.budgetId, { ...budget });
        }
        return retValue;
    }
    async batchSaveTransactions(budgetId, budget, transactions, options) {
        if (transactions.length === 0) {
            return;
        }
        const headers = await this.getAuthHeaders();
        const budgetStore = useBudgetStore();
        const chunkSize = options?.chunkSize ?? 50;
        const query = options?.skipCarryoverRecalc ? '?skipCarryover=true' : '';
        for (let i = 0; i < transactions.length; i += chunkSize) {
            const chunk = transactions.slice(i, i + chunkSize);
            const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/transactions/batch${query}`, {
                method: 'POST',
                headers,
                body: JSON.stringify(chunk),
            });
            if (!response.ok) {
                throw new Error(`Failed to batch save transactions: ${response.statusText}`);
            }
        }
        if (budget.budgetId) {
            budget.transactions = [...budget.transactions, ...transactions];
            budgetStore.updateBudget(budget.budgetId, { ...budget });
        }
    }
    async deleteTransaction(budget, transactionId) {
        const headers = await this.getAuthHeaders();
        const budgetStore = useBudgetStore();
        const transactionToDelete = budget.transactions?.find((t) => t.id === transactionId);
        if (!transactionToDelete)
            throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);
        const updatedTransaction = { ...transactionToDelete, deleted: true };
        const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify(updatedTransaction),
        });
        if (!response.ok)
            throw new Error(`Failed to soft delete transaction: ${response.statusText}`);
        if (budget.budgetId) {
            budget.transactions = budget.transactions.map((t) => (t.id === transactionId ? updatedTransaction : t));
            budgetStore.updateBudget(budget.budgetId, budget);
        }
    }
    async restoreTransaction(budget, transactionId) {
        const headers = await this.getAuthHeaders();
        const budgetStore = useBudgetStore();
        const transactionToRestore = budget.transactions?.find((t) => t.id === transactionId);
        if (!transactionToRestore)
            throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);
        if (!transactionToRestore.deleted)
            throw new Error(`Transaction ${transactionId} is not deleted`);
        const updatedTransaction = { ...transactionToRestore, deleted: false };
        const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify(updatedTransaction),
        });
        if (!response.ok)
            throw new Error(`Failed to restore transaction: ${response.statusText}`);
        if (budget.budgetId) {
            budget.transactions = budget.transactions.map((t) => (t.id === transactionId ? updatedTransaction : t));
            budgetStore.updateBudget(budget.budgetId, budget);
        }
    }
    async permanentlyDeleteTransaction(budget, transactionId) {
        const headers = await this.getAuthHeaders();
        const budgetStore = useBudgetStore();
        const transactionToDelete = budget.transactions?.find((t) => t.id === transactionId);
        if (!transactionToDelete)
            throw new Error(`Transaction ${transactionId} not found in budget ${budget.budgetId}`);
        const response = await fetch(`${this.apiBaseUrl}/budget/${budget.budgetId}/transactions/${transactionId}`, {
            method: 'DELETE',
            headers,
        });
        if (!response.ok)
            throw new Error(`Failed to permanently delete transaction: ${response.statusText}`);
        if (budget.budgetId) {
            budget.transactions = budget.transactions.filter((t) => t.id !== transactionId);
            budgetStore.updateBudget(budget.budgetId, budget);
        }
    }
    /**
     * Soft-delete a transaction when only the budget id is known (e.g. from the
     * goal details panel, where the transaction may belong to a budget that
     * isn't currently loaded in the store). Falls back to a hard DELETE because
     * we don't have the original transaction body to PUT back with deleted=true.
     *
     * If the budget IS loaded, also removes the transaction from the in-memory
     * store so the UI reflects the change without a full reload.
     */
    async deleteTransactionById(budgetId, transactionId) {
        const headers = await this.getAuthHeaders();
        const budgetStore = useBudgetStore();
        const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/transactions/${transactionId}`, {
            method: 'DELETE',
            headers,
        });
        if (!response.ok)
            throw new Error(`Failed to delete transaction: ${response.statusText}`);
        const loaded = budgetStore.getBudget(budgetId);
        if (loaded) {
            const next = {
                ...loaded,
                transactions: (loaded.transactions || []).filter((t) => t.id !== transactionId),
            };
            budgetStore.updateBudget(budgetId, next);
        }
    }
    calculateCarryOver(budget) {
        return calculateCarryOver(budget);
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
    async recalculateCarryoverFrom(entityId, startBudgetMonth) {
        const budgetStore = useBudgetStore();
        // Get all budgets for the entity, sorted
        const all = Array.from(budgetStore.budgets.values()).filter((b) => b.entityId === entityId);
        all.sort((a, b) => a.month.localeCompare(b.month));
        // Ensure each budget has categories + transactions; fetch if missing
        const fullBudgets = [];
        for (const b of all) {
            if (b.categories?.length && Array.isArray(b.transactions)) {
                fullBudgets.push(b);
            }
            else if (b.budgetId) {
                const loaded = await this.getBudget(b.budgetId);
                if (loaded) {
                    fullBudgets.push(loaded);
                    budgetStore.updateBudget(b.budgetId, loaded);
                }
            }
        }
        if (fullBudgets.length === 0)
            return;
        const startIdx = fullBudgets.findIndex((b) => b.month === startBudgetMonth);
        if (startIdx === -1)
            return; // Nothing to do if not found
        // Walk forward, computing carryover month-by-month and applying to the next month
        for (let i = startIdx; i < fullBudgets.length - 1; i++) {
            const curr = fullBudgets[i];
            const next = fullBudgets[i + 1];
            if (!next?.budgetId)
                continue;
            const nextCarry = this.calculateCarryOver(curr);
            const updatedCategories = next.categories.map((cat) => ({
                ...cat,
                carryover: cat.isFund ? nextCarry[cat.name] || 0 : 0,
            }));
            const updatedBudget = { ...next, categories: updatedCategories };
            await this.saveBudget(next.budgetId, updatedBudget, { skipCarryoverRecalc: true });
            budgetStore.updateBudget(next.budgetId, updatedBudget);
            // Ensure the next iteration sees the updated carryover values
            fullBudgets[i + 1] = updatedBudget;
        }
    }
    async saveImportedTransactions(doc) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions`, {
            method: 'POST',
            headers,
            body: JSON.stringify(doc),
        });
        if (!response.ok)
            throw new Error(`Failed to save imported transactions: ${response.statusText}`);
        const result = await response.json();
        return result.docId;
    }
    async getImportedTransactionDocs() {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions`, { headers });
        if (!response.ok)
            throw new Error(`Failed to get imported transactions: ${response.statusText}`);
        return await response.json();
    }
    async deleteImportedTransactionDoc(id) {
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
    async getImportedTransactions() {
        const importedDocs = await this.getImportedTransactionDocs();
        const allTransactions = importedDocs.flatMap((doc) => doc.importedTransactions);
        return allTransactions.sort((a, b) => {
            const aDate = (a.transactionDate || a.postedDate || '').toString();
            const bDate = (b.transactionDate || b.postedDate || '').toString();
            return bDate.localeCompare(aDate);
        });
    }
    async updateImportedTransaction(docId, transactionOrId, matched, ignored) {
        const headers = await this.getAuthHeaders();
        let transactionId;
        let payload;
        if (typeof transactionOrId === 'string') {
            transactionId = transactionOrId;
            payload = { matched, ignored };
        }
        else {
            transactionId = transactionOrId.id;
            payload = transactionOrId;
        }
        const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/${docId}/${transactionId}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify(payload),
        });
        if (!response.ok)
            throw new Error(`Failed to update imported transaction: ${response.statusText}`);
    }
    async deleteImportedTransaction(docId, transactionId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/${docId}/${transactionId}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify({ deleted: true }),
        });
        if (!response.ok)
            throw new Error(`Failed to delete imported transaction: ${response.statusText}`);
    }
    findImportedTransactionByKey(existingDocs, key) {
        return existingDocs.some((doc) => doc.importedTransactions.some((tx) => tx.accountNumber === key.accountNumber &&
            (tx.transactionDate || tx.postedDate || '') === (key.transactionDate || key.postedDate || '') &&
            tx.payee === key.payee &&
            (tx.debitAmount ?? 0) === key.debitAmount &&
            (tx.creditAmount ?? 0) === key.creditAmount));
    }
    async getImportedTransactionsByAccountId(accountId, offset = 0, limit = 100) {
        console.log(`Fetching imported transactions for accountId: ${accountId}`);
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/imported-transactions/by-account/${accountId}?offset=${offset}&limit=${limit}`, { headers });
        if (!response.ok) {
            const errorText = await response.text();
            console.error(`Failed to fetch imported transactions: ${response.status} ${response.statusText} - ${errorText}`);
            throw new Error(`Failed to fetch imported transactions: ${response.statusText}`);
        }
        const importedTxs = await response.json();
        return importedTxs.sort((a, b) => {
            const aDate = (a.transactionDate || a.postedDate || '').toString();
            const bDate = (b.transactionDate || b.postedDate || '').toString();
            return bDate.localeCompare(aDate);
        });
    }
    async updateImportedTransactions(transactions) {
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
    async getBudgetTransactionsMatchedToImported(accountId) {
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
    async updateBudgetTransactions(transactions) {
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
        for (const { budgetId } of transactions) {
            const budget = budgetStore.getBudget(budgetId) || (await this.getBudget(budgetId));
            if (!budget || !budget.budgetId)
                continue;
            budgetStore.updateBudget(budget.budgetId, budget);
        }
    }
    async getUserFamily(uid) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${uid}`, { headers });
        if (!response.ok || response.statusText == 'No Content')
            return null;
        const data = await response.json();
        return data;
    }
    async createFamily(uid, name, email) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/create`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ name, email }),
        });
        if (!response.ok)
            throw new Error(`Failed to create family: ${response.statusText}`);
        const f = await response.json();
        return f;
    }
    /**
     * One-shot transactional onboarding: creates the user's family,
     * `family_members` row, first entity (with persisted templateBudget +
     * taxFormIds), Income group, optional first budget, and optional starter
     * accounts in a single Postgres transaction. See
     * `api/Controllers/OnboardingController.cs`.
     *
     * Returns `{ created: true, ... }` on a fresh seed (HTTP 200) and
     * `{ created: false, ... }` when the user already had a family (HTTP 409
     * — surfaced as success here so the UI can short-circuit straight to the
     * existing budget). Throws on validation (400) or server errors (500).
     */
    /**
     * Trigger a fresh verification email for the currently-signed-in user.
     * Backend: `POST /api/auth/resend-verification-email` — no body, auth
     * derived from the Firebase token. Throws on the "already verified" case
     * (400) so the caller can show a tailored message.
     */
    async resendVerificationEmail() {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/auth/resend-verification-email`, {
            method: 'POST',
            headers,
        });
        if (!response.ok) {
            let msg = response.statusText;
            try {
                const body = await response.json();
                if (body?.Error)
                    msg = body.Error;
                else if (body?.error)
                    msg = body.error;
            }
            catch {
                // body wasn't JSON; fall through to statusText
            }
            throw new Error(msg);
        }
    }
    async seedOnboarding(payload) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/onboarding/seed`, {
            method: 'POST',
            headers,
            body: JSON.stringify(payload),
        });
        // 200 = fresh seed; 409 = already onboarded (still parse the body — it
        // carries the existing FamilyId / EntityId / BudgetId for navigation).
        if (response.status !== 200 && response.status !== 409) {
            const msg = await response.text();
            throw new Error(`Onboarding seed failed (${response.status}): ${msg || response.statusText}`);
        }
        return response.json();
    }
    async addFamilyMember(familyId, memberUid, memberEmail) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/members`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ uid: memberUid, email: memberEmail }),
        });
        if (!response.ok)
            throw new Error(`Failed to add family member: ${response.statusText}`);
    }
    async removeFamilyMember(familyId, memberUid) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/members/${memberUid}`, {
            method: 'DELETE',
            headers,
        });
        if (!response.ok)
            throw new Error(`Failed to remove family member: ${response.statusText}`);
    }
    async renameFamily(familyId, newName) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/rename`, {
            method: 'PUT',
            headers,
            body: JSON.stringify({ name: newName }),
        });
        if (!response.ok)
            throw new Error(`Failed to rename family: ${response.statusText}`);
    }
    async inviteUser(invite) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/invite`, {
            method: 'POST',
            headers,
            body: JSON.stringify(invite),
        });
        if (!response.ok)
            throw new Error(`Failed to invite user: ${response.statusText}`);
    }
    async acceptInvite(token) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/accept-invite`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ token }),
        });
        if (!response.ok)
            throw new Error(`Failed to accept invite: ${response.statusText}`);
    }
    async getPendingInvites(inviterUid) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/pending-invites/${inviterUid}`, { headers });
        if (!response.ok)
            throw new Error(`Failed to get pending invites: ${response.statusText}`);
        return await response.json();
    }
    async getLastAccessed(uid) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/last-accessed/${uid}`, { headers });
        if (!response.ok)
            return undefined;
        const { lastAccessed } = await response.json();
        return lastAccessed ? Timestamp.fromDate(new Date(lastAccessed)) : undefined;
    }
    async getUser(userId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/user/${userId}`, { headers });
        if (!response.ok)
            return null;
        return await response.json();
    }
    async getUserByEmail(email) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/user/by-email/${email}`, { headers });
        if (!response.ok)
            return null;
        return await response.json();
    }
    async saveUser(userId, userData) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/user/${userId}`, {
            method: 'POST',
            headers,
            body: JSON.stringify(userData),
        });
        if (!response.ok)
            throw new Error(`Failed to save user: ${response.statusText}`);
    }
    async batchReconcileTransactions(budgetId, budget, reconcileData) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/batch-reconcile`, {
            method: 'POST',
            headers,
            body: JSON.stringify(reconcileData),
        });
        if (!response.ok)
            throw new Error(`Failed to batch reconcile transactions: ${response.statusText}`);
        const budgetStore = useBudgetStore();
        budgetStore.updateBudget(budgetId, budget);
    }
    unsubscribeAll() {
        // Placeholder for future subscription cleanup if needed
    }
    async getAccounts(familyId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts`, { headers });
        if (!response.ok)
            throw new Error(`Failed to fetch accounts: ${response.statusText}`);
        const raw = (await response.json());
        return raw.map((a) => ({
            ...a,
            createdAt: this.toTimestamp(a.createdAt) ?? Timestamp.fromDate(new Date()),
            updatedAt: this.toTimestamp(a.updatedAt) ?? Timestamp.fromDate(new Date()),
        }));
    }
    async getAccount(familyId, accountId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountId}`, { headers });
        if (!response.ok) {
            if (response.status === 404)
                return null;
            throw new Error(`Failed to fetch account ${accountId}: ${response.statusText}`);
        }
        const a = (await response.json());
        if (!a)
            return null;
        const account = {
            ...a,
            createdAt: this.toTimestamp(a.createdAt) ?? Timestamp.fromDate(new Date()),
            updatedAt: this.toTimestamp(a.updatedAt) ?? Timestamp.fromDate(new Date()),
        };
        return account;
    }
    async saveAccount(familyId, account) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${account.id}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify(account),
        });
        if (!response.ok)
            throw new Error(`Failed to save account: ${response.statusText}`);
    }
    async deleteAccount(familyId, accountId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountId}`, {
            method: 'DELETE',
            headers,
        });
        if (!response.ok)
            throw new Error(`Failed to delete account: ${response.statusText}`);
    }
    async importAccounts(familyId, entries) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/import`, {
            method: 'POST',
            headers,
            body: JSON.stringify(entries),
        });
        if (!response.ok)
            throw new Error(`Failed to import accounts: ${response.statusText}`);
    }
    async getSnapshots(familyId) {
        try {
            const headers = await this.getAuthHeaders();
            const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots`, {
                method: 'GET',
                headers,
            });
            if (!response.ok)
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            const data = (await response.json());
            return data.map((raw) => {
                const s = raw;
                const date = this.toTimestamp(s.date ?? s.Date) ?? Timestamp.fromDate(new Date());
                const createdAt = this.toTimestamp(s.createdAt ?? s.CreatedAt) ?? Timestamp.fromDate(new Date());
                const netWorth = Number(s.netWorth ?? s.NetWorth ?? 0);
                const id = typeof s.id === 'string' || typeof s.id === 'number' ? String(s.id) : typeof s.Id === 'string' || typeof s.Id === 'number' ? String(s.Id) : '';
                // Keep accounts as-is if present
                const accounts = Array.isArray(s.accounts ?? s.Accounts) ? (s.accounts ?? s.Accounts) : [];
                return { id, date, netWorth, createdAt, accounts };
            });
        }
        catch (error) {
            console.error('Error fetching snapshots:', error);
            return [];
        }
    }
    async saveSnapshot(familyId, snapshot) {
        const headers = await this.getAuthHeaders();
        const payload = {
            ...snapshot,
            date: this.toIsoString(snapshot.date) ?? new Date().toISOString(),
            createdAt: this.toIsoString(snapshot.createdAt) ?? new Date().toISOString(),
            accounts: snapshot.accounts?.map((acc) => ({
                accountId: acc.accountId,
                accountName: acc.accountName,
                type: acc.type,
                value: Number(acc.value ?? 0),
            })),
            netWorth: Number(snapshot.netWorth ?? 0),
        };
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/${snapshot.id}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify(payload),
        });
        if (!response.ok)
            throw new Error(`Failed to save snapshot: ${response.statusText}`);
    }
    async deleteSnapshot(familyId, snapshotId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/${snapshotId}`, {
            method: 'DELETE',
            headers,
        });
        if (!response.ok)
            throw new Error(`Failed to delete snapshot: ${response.statusText}`);
    }
    async batchDeleteSnapshots(familyId, snapshotIds) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/snapshots/batch-delete`, {
            method: 'POST',
            headers,
            body: JSON.stringify(snapshotIds),
        });
        if (!response.ok)
            throw new Error(`Failed to batch delete snapshots: ${response.statusText}`);
    }
    // Statement Functions
    async getStatements(familyId, accountNumber) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements`, { headers });
        if (!response.ok)
            throw new Error(`Failed to fetch statements: ${response.statusText}`);
        return await response.json();
    }
    async finalizeStatement(payload) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${payload.familyId}/accounts/${payload.accountId}/statements/finalize`, {
            method: 'POST',
            headers,
            body: JSON.stringify(toStatementFinalizeRequestBody(payload)),
        });
        if (!response.ok) {
            let message = '';
            try {
                message = await response.text();
            }
            catch {
                /* ignore body read errors */
            }
            const suffix = message ? ` - ${message}` : '';
            throw new Error(`Failed to finalize statement: ${response.statusText}${suffix}`);
        }
    }
    async saveStatement(familyId, accountNumber, statement, transactionRefs) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statement.id}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify({ statement, transactions: transactionRefs }),
        });
        if (!response.ok)
            throw new Error(`Failed to save statement: ${response.statusText}`);
        return await response.json();
    }
    async deleteStatement(familyId, accountNumber, statementId, transactionRefs) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statementId}`, {
            method: 'DELETE',
            headers,
            body: JSON.stringify({ transactions: transactionRefs }),
        });
        if (!response.ok)
            throw new Error(`Failed to delete statement: ${response.statusText}`);
    }
    async unreconcileStatement(familyId, accountNumber, statementId, transactionRefs) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/families/${familyId}/accounts/${accountNumber}/statements/${statementId}/unreconcile`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ transactions: transactionRefs }),
        });
        if (!response.ok)
            throw new Error(`Failed to unreconcile statement: ${response.statusText}`);
    }
    // Goal Functions
    async getGoals(entityId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/goals?entityId=${entityId}`, { headers });
        if (!response.ok)
            throw new Error(`Failed to load goals: ${response.statusText}`);
        return await response.json();
    }
    buildGoalPayload(goal) {
        let targetDate;
        if (goal.targetDate) {
            const td = goal.targetDate;
            if (typeof td === 'object' &&
                td !== null &&
                'toDate' in td &&
                typeof td.toDate === 'function') {
                targetDate = td.toDate().toISOString();
            }
            else if (td instanceof Date) {
                targetDate = td.toISOString();
            }
            else {
                targetDate = new Date(td).toISOString();
            }
        }
        return { ...goal, targetDate };
    }
    async insertGoal(goal) {
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
    async updateGoal(goal) {
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
    async getGoalDetails(goalId) {
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
    // ===== Budget Group Functions =====
    async getGroups(entityId) {
        if (!entityId)
            return [];
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/entities/${entityId}/groups`, { headers });
        if (!response.ok)
            throw new Error(`Failed to load groups: ${response.statusText}`);
        const raw = await response.json();
        return raw.map((r) => this.mapGroup(r));
    }
    async createGroup(entityId, payload) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/entities/${entityId}/groups`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ ...payload, kind: payload.kind ?? 'expense' }),
        });
        if (!response.ok)
            throw new Error(`Failed to create group: ${response.statusText}`);
        return this.mapGroup(await response.json());
    }
    async updateGroup(entityId, groupId, payload) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/entities/${entityId}/groups/${groupId}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify(payload),
        });
        if (!response.ok)
            throw new Error(`Failed to update group: ${response.statusText}`);
        return this.mapGroup(await response.json());
    }
    async reorderGroups(entityId, groupIds) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/entities/${entityId}/groups/order`, {
            method: 'PUT',
            headers,
            body: JSON.stringify({ groupIds }),
        });
        if (!response.ok)
            throw new Error(`Failed to reorder groups: ${response.statusText}`);
    }
    async deleteGroup(entityId, groupId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/entities/${entityId}/groups/${groupId}`, {
            method: 'DELETE',
            headers,
        });
        if (response.status === 409) {
            const msg = await response.text();
            throw new Error(msg || 'Group still has categories — reassign or delete those first.');
        }
        if (!response.ok)
            throw new Error(`Failed to delete group: ${response.statusText}`);
    }
    async reorderCategories(budgetId, categories) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/budget/${budgetId}/categories/order`, {
            method: 'PUT',
            headers,
            body: JSON.stringify({ categories }),
        });
        if (!response.ok)
            throw new Error(`Failed to reorder categories: ${response.statusText}`);
    }
    // Entity Functions
    async createEntity(familyId, entity) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities`, {
            method: 'POST',
            headers,
            body: JSON.stringify(entity),
        });
        if (!response.ok)
            throw new Error(`Failed to create entity: ${response.statusText}`);
        return await response.json();
    }
    async updateEntity(familyId, entity) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entity.id}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify(entity),
        });
        if (!response.ok)
            throw new Error(`Failed to update entity: ${response.statusText}`);
    }
    async deleteEntity(familyId, entityId) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}`, {
            method: 'DELETE',
            headers,
        });
        if (!response.ok)
            throw new Error(`Failed to delete entity: ${response.statusText}`);
    }
    async addEntityMember(familyId, entityId, member) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}/members`, {
            method: 'POST',
            headers,
            body: JSON.stringify(member),
        });
        if (!response.ok)
            throw new Error(`Failed to add entity member: ${response.statusText}`);
    }
    async removeEntityMember(familyId, entityId, memberUid) {
        const headers = await this.getAuthHeaders();
        const response = await fetch(`${this.apiBaseUrl}/family/${familyId}/entities/${entityId}/members/${memberUid}`, {
            method: 'DELETE',
            headers,
        });
        if (!response.ok)
            throw new Error(`Failed to remove entity member: ${response.statusText}`);
    }
}
export const dataAccess = new DataAccess();
