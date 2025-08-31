import { ref, computed, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useUIStore } from '../store/ui';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { dataAccess } from '../dataAccess';
import { auth } from '../firebase/init';
import type { Budget, Transaction, ImportedTransaction } from '../types';

export type Status = 'C' | 'U' | 'R';

export interface LedgerRow {
  id: string;
  date: string; // ISO
  payee: string;
  category: string;
  entityName: string;
  budgetId: string;
  amount: number;
  status: Status;
  importedMerchant?: string;
  isDuplicate?: boolean;
  linkId?: string;
  notes?: string;
  accountId?: string;
}

export type BudgetTransaction = Transaction & { linkId?: string };

export function withinDateWindow(date1: string, date2: string, windowDays: number): boolean {
  const d1 = new Date(date1).getTime();
  const d2 = new Date(date2).getTime();
  const diff = Math.abs(d1 - d2);
  return diff <= windowDays * 86400000;
}

export function isDuplicate(tx: BudgetTransaction, list: BudgetTransaction[]): boolean {
  return list.some(
    (other) =>
      other.id !== tx.id &&
      other.amount === tx.amount &&
      merchantSimilar(other.merchant, tx.merchant) &&
      withinDateWindow(tx.date, other.date, 3),
  );
}

function merchantSimilar(a?: string | null, b?: string | null): boolean {
  const normalize = (s?: string | null) =>
    (s || '').toLowerCase().replace(/[^a-z0-9]/g, '');
  const na = normalize(a);
  const nb = normalize(b);
  if (!na || !nb) return true;
  return na.includes(nb) || nb.includes(na);
}

export function link(tx: BudgetTransaction, linkId: string): void {
  tx.linkId = linkId;
}

export function unlink(tx: BudgetTransaction): void {
  delete tx.linkId;
}

export interface LedgerFilters {
  search: string;
  importedMerchant: string;
  cleared: boolean;
  uncleared: boolean;
  reconciled: boolean;
  duplicatesOnly: boolean;
  minAmt: number | null;
  maxAmt: number | null;
  start: string | null; // ISO date
  end: string | null;   // ISO date
  accountId: string | null;
}

export function useTransactions() {
  const budgetStore = useBudgetStore();
  const familyStore = useFamilyStore();
  const { selectedBudgetIds } = storeToRefs(useUIStore());

  const rows = ref<LedgerRow[]>([]);
  const registerRows = ref<LedgerRow[]>([]);
  const loading = ref(false);
  const loadingRegister = ref(false);
  const importedLoaded = ref(false);
  const importedOffset = ref(0);
  const pageSize = 100;
  const hasMoreImported = ref(true);
  const loadingMoreRegister = ref(false);

  const filters = ref<LedgerFilters>({
    search: '',
    importedMerchant: '',
    cleared: false,
    uncleared: false,
    reconciled: false,
    duplicatesOnly: false,
    minAmt: null,
    maxAmt: null,
    start: null,
    end: null,
    accountId: null,
  });

  function mapTxToRow(tx: Transaction, budget: Budget): LedgerRow {
    const categoryNames = tx.categories?.map((c) => c.category || '').filter(Boolean) || [];
    const category = categoryNames.length > 1 ? 'Split' : categoryNames[0] || '';
    const entityName =
      familyStore.family?.entities?.find((e) => e.id === (tx.entityId || budget.entityId))?.name ||
      'N/A';
    let notes = tx.notes || '';
    if (categoryNames.length > 1) {
      const noteCats = categoryNames.join(', ');
      notes = notes ? `${notes} (${noteCats})` : noteCats;
    }
    return {
      id: tx.id,
      date: tx.date,
      payee: tx.merchant || '',
      category,
      entityName,
      budgetId: budget.budgetId || '',
      amount: Number(tx.amount || 0),
      status: tx.status === 'C' ? 'C' : tx.status === 'R' ? 'R' : 'U',
      importedMerchant: tx.importedMerchant || '',
      linkId: tx.accountNumber ? `${tx.accountSource || ''}:${tx.accountNumber}` : undefined,
      notes,
    };
  }

  function mapImportedToRow(tx: ImportedTransaction): LedgerRow {
    const normalizeNum = (n?: string | null) => (n || '').replace(/\D/g, '');
    const account = familyStore.family?.accounts?.find((a) => {
      if (tx.accountId && String(a.id) === String(tx.accountId)) return true;
      if (tx.accountNumber) {
        const numberMatch = normalizeNum(a.accountNumber) === normalizeNum(tx.accountNumber);
        if (!numberMatch) return false;
        return tx.accountSource
          ? (a.institution || '').toLowerCase() === tx.accountSource.toLowerCase()
          : true;
      }
      return false;
    });
    const accountName = tx.accountName || account?.name || '';
    const entityName = tx.accountNumber
      ? `${accountName} (${tx.accountNumber})`
      : accountName;
    return {
      id: tx.id,
      date: tx.postedDate,
      payee: tx.payee,
      category: '',
      entityName,
      budgetId: '',
      amount: (tx.creditAmount ?? 0) - (tx.debitAmount ?? 0),
      status: tx.status,
      linkId: tx.accountNumber ? `${tx.accountSource || ''}:${tx.accountNumber}` : undefined,
      notes: '',
      accountId: account?.id ? String(account.id) : tx.accountId ? String(tx.accountId) : undefined,
    };
  }

  async function hydrateBudgets(budgetIds: string[]) {
    const out: LedgerRow[] = [];
    for (const id of budgetIds) {
      let full = budgetStore.getBudget(id);
      if (!full || !full.transactions || full.transactions.length === 0) {
        full = await dataAccess.getBudget(id);
        if (full) {
          budgetStore.updateBudget(id, full);
        }
      }
      if (full) {
        const mapped = (full.transactions || [])
          .filter((t) => !t.deleted)
          .map((t) => mapTxToRow(t, full));
        out.push(...mapped);
      }
    }
    // Sort desc by date
    out.sort((a, b) => b.date.localeCompare(a.date));
    return out;
  }

  async function loadInitial(budgetIdOrIds: string | string[]) {
    loading.value = true;
    try {
      const ids = Array.isArray(budgetIdOrIds) ? budgetIdOrIds : [budgetIdOrIds];
      rows.value = await hydrateBudgets(ids);
    } finally {
      loading.value = false;
    }
  }

  async function loadImportedTransactions(reset = false) {
    if (loadingMoreRegister.value) return;
    if (reset) {
      importedOffset.value = 0;
      registerRows.value = [];
      hasMoreImported.value = true;
      loadingRegister.value = true;
    }
    if (!hasMoreImported.value) {
      loadingRegister.value = false;
      return;
    }
    loadingMoreRegister.value = true;
    try {
      if (!familyStore.family?.accounts || familyStore.family.accounts.length === 0) {
        const fid = familyStore.family?.id;
        if (fid) {
          try {
            const accounts = await dataAccess.getAccounts(fid);
            if (familyStore.family) familyStore.family.accounts = accounts;
          } catch {
            /* ignore */
          }
        }
      }
      let imported: ImportedTransaction[];
      if (filters.value.accountId) {
        imported = await dataAccess.getImportedTransactionsByAccountId(
          filters.value.accountId,
          importedOffset.value,
          pageSize,
        );
      } else {
        imported = await dataAccess.getImportedTransactions();
        imported = imported.slice(importedOffset.value, importedOffset.value + pageSize);
      }
      const mapped = imported
        .filter((t) => !t.deleted)
        .map((t) => mapImportedToRow(t));
      registerRows.value = [...registerRows.value, ...mapped].sort((a, b) =>
        b.date.localeCompare(a.date),
      );
      importedOffset.value += imported.length;
      if (imported.length < pageSize) {
        hasMoreImported.value = false;
      }
      importedLoaded.value = true;
    } finally {
      loadingMoreRegister.value = false;
      loadingRegister.value = false;
    }
  }

  async function fetchMore() { /* pagination placeholder for API cursors */ }
  async function fetchMoreRegister() {
    await loadImportedTransactions();
  }

  function scrollToDate(iso: string) {
    // The page can use this to scroll via QTable / VirtualScroll API later.
    console.log('scrollToDate', iso);
  }

  const filtered = computed(() => {
    const f = filters.value;
    return rows.value.filter((r) => {
      if (f.search) {
        const s = f.search.toLowerCase();
        if (
          !(
            r.payee.toLowerCase().includes(s) ||
            r.category.toLowerCase().includes(s) ||
            r.entityName.toLowerCase().includes(s)
          )
        )
          return false;
      }
      if (
        f.importedMerchant &&
        !r.importedMerchant?.toLowerCase().includes(f.importedMerchant.toLowerCase())
      )
        return false;
      const statusFilters: Status[] = [];
      if (f.cleared) statusFilters.push('C');
      if (f.uncleared) statusFilters.push('U');
      if (f.reconciled) statusFilters.push('R');
      if (statusFilters.length && !statusFilters.includes(r.status)) return false;
      if (f.duplicatesOnly && !r.isDuplicate) return false;
      if (f.minAmt != null && r.amount < f.minAmt) return false;
      if (f.maxAmt != null && r.amount > f.maxAmt) return false;
      if (f.start && r.date < f.start) return false;
      if (f.end && r.date > f.end) return false;
      return true;
    });
  });

  const filteredRegister = computed(() => {
    const f = filters.value;
    return registerRows.value.filter((r) => {
      if (f.search) {
        const s = f.search.toLowerCase();
        if (!r.payee.toLowerCase().includes(s) && !r.entityName.toLowerCase().includes(s))
          return false;
      }
      if (f.accountId && r.accountId !== f.accountId) return false;
      if (f.cleared && r.status !== 'C') return false;
      return true;
    });
  });

  watch(
    () => filters.value.accountId,
    async () => {
      importedOffset.value = 0;
      registerRows.value = [];
      hasMoreImported.value = true;
      importedLoaded.value = false;
      await loadImportedTransactions(true);
    },
  );

  const api = {
    // budget tab
    transactions: filtered,
    filters,
    fetchMore,
    loading,

    // register tab
    registerRows: filteredRegister,
    fetchMoreRegister,
    loadingRegister,
    canLoadMoreRegister: computed(() => hasMoreImported.value),
    loadingMoreRegister,

    // utilities
    scrollToDate,
    // also expose init for callers that want explicit control
    loadInitial,
    loadImportedTransactions,
  };

  // Load budgets initially so selection and options are available
  void (async () => {
    try {
      const user = auth.currentUser;
      if (!user) return;
      await familyStore.loadFamily(user.uid);
      await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);
    } catch {
      /* ignore */
    }
  })();

  // When the user's selection of budgets changes, hydrate those budgets explicitly
  watch(
    () => selectedBudgetIds.value?.slice().join(','),
    async () => {
      const ids = selectedBudgetIds.value.filter(Boolean);
      if (ids.length > 0) {
        await loadInitial(ids);
      } else {
        rows.value = [];
      }
    },
    { immediate: true },
  );

  return api;
}
