import { ref, computed, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useUIStore } from '../store/ui';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { dataAccess } from '../dataAccess';
import { auth } from '../firebase/init';
import type { Budget, Transaction, ImportedTransaction } from '../types';

export type Status = 'C' | 'U' | 'R';
export type LedgerStatus = Status | 'M' | 'I';

export interface LedgerRow {
  id: string;
  date: string; // ISO
  payee: string;
  category: string;
  entityName: string;
  budgetId: string;
  amount: number;
  status: LedgerStatus;
  importedMerchant?: string;
  isDuplicate?: boolean;
  linkId?: string;
  notes?: string;
  accountId?: string;
  matched?: boolean;
  transaction?: Transaction;
}

export type BudgetTransaction = Transaction & { linkId?: string };

export function withinDateWindow(date1: string, date2: string, windowDays: number): boolean {
  const d1 = new Date(date1).getTime();
  const d2 = new Date(date2).getTime();
  const diff = Math.abs(d1 - d2);
  return diff <= windowDays * 86400000;
}

function amountsMatch(a: number | undefined, b: number | undefined): boolean {
  if (a == null || b == null) return false;
  return Math.abs(Number(a) - Number(b)) == 0;
}

// function normalizeMerchant(value?: string | null): string {
//   return (value || '')
//     .toLowerCase()
//     .normalize('NFD')
//     .replace(/[\u0300-\u036f]/g, '')
//     .replace(/[^a-z0-9]/g, '');
// }

// function merchantsMatchStrict(a?: string | null, b?: string | null): boolean {
//   const na = normalizeMerchant(a);
//   const nb = normalizeMerchant(b);
//   return Boolean(na && nb && na === nb);
// }

// function merchantSimilar(a?: string | null, b?: string | null): boolean {
//   const na = normalizeMerchant(a);
//   const nb = normalizeMerchant(b);
//   if (!na || !nb) return true;
//   return na.includes(nb) || nb.includes(na);
// }

// function accountMatches(a: BudgetTransaction, b: BudgetTransaction): boolean {
//   if (!a.accountNumber || !b.accountNumber) return false;
//   if (a.accountNumber !== b.accountNumber) return false;
//   if (a.accountSource && b.accountSource && a.accountSource !== b.accountSource) return false;
//   return true;
// }

function datesAlign(a: BudgetTransaction, b: BudgetTransaction): boolean {
  if (withinDateWindow(a.date, b.date, 3)) return true;
  if (a.postedDate && b.postedDate && withinDateWindow(a.postedDate, b.postedDate, 3)) return true;
  return false;
}

function checkNumbersMatch(a: BudgetTransaction, b: BudgetTransaction): boolean {
  if (!a.checkNumber || !b.checkNumber) return false;
  return a.checkNumber === b.checkNumber;
}

export function isDuplicate(tx: BudgetTransaction, list: BudgetTransaction[]): boolean {
  if (tx.amount == 649.52)  {
      console.log('Dup check', tx);
      console.log('Dup check list', list.filter(t => t.amount == 649.52));
    }
  return list.some((other) => {
    if (other.id === tx.id) return false;
    if (!amountsMatch(other.amount, tx.amount)) return false;

    const sameEntity = Boolean(other.entityId && tx.entityId && other.entityId === tx.entityId);
    // const accountAligned = accountMatches(other, tx);
    // const similarPayee = merchantSimilar(
    //   other.merchant || other.importedMerchant,
    //   tx.merchant || tx.importedMerchant,
    // );
    // const identicalPayee = merchantsMatchStrict(
    //   other.merchant || other.importedMerchant,
    //   tx.merchant || tx.importedMerchant,
    // );
    const sameCheck = checkNumbersMatch(other, tx);
    const closeInTime = datesAlign(other, tx);

    return sameEntity && sameCheck && closeInTime; //&& (identicalPayee || similarPayee)

    // if (identicalPayee && (closeInTime || sameEntity || accountAligned || sameCheck)) {
    //   return true;
    // }

    // if (closeInTime && sameCheck && (similarPayee || sameEntity || accountAligned || sameCheck)) {
    //   return true;
    // }

    // if ((sameEntity || accountAligned) && identicalPayee) {
    //   return true;
    // }

    // if (sameCheck && (similarPayee || sameEntity || accountAligned)) {
    //   return true;
    // }

    // return false;
  });
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
  unmatchedOnly: boolean;
}

export function useTransactions() {
  const budgetStore = useBudgetStore();
  const familyStore = useFamilyStore();
  const { selectedBudgetIds } = storeToRefs(useUIStore());

  const rows = ref<LedgerRow[]>([]);
  const registerRows = ref<LedgerRow[]>([]);
  const loading = ref(false);
  const loadingRegister = ref(false);
  const importedMap = ref<Record<string, ImportedTransaction>>({});

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
    unmatchedOnly: false,
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
      transaction: tx,
    };
  }

  const normalizeImportedStatus = (status: string | undefined | null): Status => {
    if (!status) return 'U';
    const value = status.trim().toUpperCase();
    switch (value) {
      case 'M':
      case 'MATCHED':
        return 'C';
      case 'C':
      case 'CLEARED':
        return 'C';
      case 'R':
      case 'RECONCILED':
        return 'R';
      case 'I':
      case 'IGNORED':
        return 'U';
      case 'U':
      case 'UNCLEARED':
      case 'UNMATCHED':
      case 'P':
      case 'PENDING':
      case 'N':
      case 'NEW':
        return 'U';
      default:
        return 'U';
    }
  };

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
    const derivedStatus: LedgerStatus = tx.ignored
      ? 'I'
      : tx.matched
        ? 'M'
        : normalizeImportedStatus(tx.status);

    return {
      id: tx.id,
      date: tx.postedDate,
      payee: tx.payee,
      category: '',
      entityName,
      budgetId: '',
      amount: (tx.creditAmount ?? 0) - (tx.debitAmount ?? 0),
      status: derivedStatus,
      linkId: tx.accountNumber ? `${tx.accountSource || ''}:${tx.accountNumber}` : undefined,
      notes: '',
      accountId: account?.id ? String(account.id) : tx.accountId ? String(tx.accountId) : undefined,
      matched: tx.matched,
    };
  }

  async function hydrateBudgets(budgetIds: string[]) {
    const out: LedgerRow[] = [];

    const budgets = await Promise.all(
      budgetIds.map(async (id) => {
        try {
          let full = budgetStore.getBudget(id);
          if (!full || !full.transactions || full.transactions.length === 0) {
            const fetched = await dataAccess.getBudget(id);
            if (fetched) {
              budgetStore.updateBudget(id, fetched);
              full = fetched;
            }
          }
          return full ?? null;
        } catch (err) {
          console.error('Failed to hydrate budget', id, err);
          return null;
        }
      }),
    );

    budgets.forEach((full) => {
      if (!full) return;
      const transactions = (full.transactions || []).filter((t) => !t.deleted);
      const budgetTransactions = transactions as BudgetTransaction[];
      const duplicateIds = new Set<string>();
      for (const tx of budgetTransactions) {
        if (isDuplicate(tx, budgetTransactions)) {
          duplicateIds.add(tx.id);
        }
      }
      const mapped = budgetTransactions.map((t) => {
        const row = mapTxToRow(t, full);
        row.isDuplicate = duplicateIds.has(t.id);
        return row;
      });
      out.push(...mapped);
    });

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

  async function fetchImportedByAccount(accountId: string) {
    const pageSize = 200;
    const collected: ImportedTransaction[] = [];
    let offset = 0;
    while (true) {
      const batch = await dataAccess.getImportedTransactionsByAccountId(accountId, offset, pageSize);
      collected.push(...batch);
      if (batch.length < pageSize) {
        break;
      }
      offset += batch.length;
    }
    return collected;
  }

  async function loadImportedTransactions(reset = false) {
    if (loadingRegister.value && !reset) return;
    if (reset) {
      registerRows.value = [];
      importedMap.value = {};
    }
    loadingRegister.value = true;
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
        imported = await fetchImportedByAccount(filters.value.accountId);
      } else {
        imported = await dataAccess.getImportedTransactions();
      }
      imported.forEach((t) => {
        if (!t.deleted) importedMap.value[t.id] = t;
      });
      const mapped = imported
        .filter((t) => !t.deleted)
        .map((t) => mapImportedToRow(t));
      registerRows.value = mapped.sort((a, b) =>
        b.date.localeCompare(a.date),
      );
    } finally {
      loadingRegister.value = false;
    }
  }

  function scrollToDate(iso: string) {
    // Placeholder hook should the UI add programmatic scrolling in the future.
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
      if (statusFilters.length && !statusFilters.includes(normalizeImportedStatus(r.status))) return false;
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
      if (f.start && r.date < f.start) return false;
      if (f.end && r.date > f.end) return false;
      const statusFilters: Status[] = [];
      if (f.cleared) statusFilters.push('C');
      if (f.uncleared) statusFilters.push('U');
      if (f.reconciled) statusFilters.push('R');
      if (statusFilters.length && !statusFilters.includes(normalizeImportedStatus(r.status))) return false;
      if (f.unmatchedOnly && r.matched) return false;
      return true;
    });
  });

  watch(
    () => filters.value.accountId,
    async () => {
      registerRows.value = [];
      await loadImportedTransactions(true);
    },
  );

  const api = {
    // budget tab
    transactions: filtered,
    filters,
    loading,

    // register tab
    registerRows: filteredRegister,
    loadingRegister,
    // utilities
    scrollToDate,
    // also expose init for callers that want explicit control
    loadInitial,
    loadImportedTransactions,
    getImportedTx: (id: string) => importedMap.value[id],
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
