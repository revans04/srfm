import { ref } from 'vue';
import type { QTableColumn } from 'quasar';

export interface TransactionBase {
  id: string;
  date: string; // ISO
  payee: string;
  categoryId: string;
  entityId: string;
  budgetId: string;
  amount: number;
  notes?: string;
}

export interface BudgetTransaction extends TransactionBase {
  status: 'C' | 'U';
  isDuplicate?: boolean;
  linkId?: string;
}

export interface RegistryRow extends BudgetTransaction {
  runningBalance?: number;
}

// mock generator
function generateMock(count = 1000): BudgetTransaction[] {
  const arr: BudgetTransaction[] = [];
  for (let i = 0; i < count; i++) {
    const amt = parseFloat((Math.random() * 200 - 100).toFixed(2));
    arr.push({
      id: `tx-${i}`,
      date: new Date(Date.now() - i * 86400000).toISOString().slice(0, 10),
      payee: `Merchant ${i % 20}`,
      categoryId: `cat-${i % 5}`,
      entityId: `ent-${i % 3}`,
      budgetId: `bud-${i % 2}`,
      amount: amt,
      status: i % 3 === 0 ? 'C' : 'U',
      notes: 'Sample note',
    });
  }
  return arr;
}

const all = generateMock();

export function useTransactions() {
  const transactions = ref<BudgetTransaction[]>([]);
  const registerRows = ref<RegistryRow[]>([]);
  const page = ref(0);
  const registerPage = ref(0);
  const pageSize = 50;
  const loading = ref(false);
  const loadingRegister = ref(false);

  function loadPage(target: typeof transactions, pageRef: typeof page) {
    const start = pageRef.value * pageSize;
    const next = all.slice(start, start + pageSize);
    target.value.push(...next);
    pageRef.value++;
  }

  function fetchMore() {
    if (loading.value) return;
    loading.value = true;
    loadPage(transactions, page);
    loading.value = false;
  }

  function fetchMoreRegister() {
    if (loadingRegister.value) return;
    loadingRegister.value = true;
    const start = registerPage.value * pageSize;
    const next = all.slice(start, start + pageSize).map((t) => ({
      ...t,
      runningBalance: (registerRows.value.at(-1)?.runningBalance || 0) + t.amount,
    }));
    registerRows.value.push(...next);
    registerPage.value++;
    loadingRegister.value = false;
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  function scrollToDate(_date: string) {
    // no-op placeholder; would map date to index and scroll
  }

  // Columns for tables
  const budgetColumns: QTableColumn[] = [
    { name: 'date', label: 'Date', field: 'date', align: 'left' },
    { name: 'payee', label: 'Payee', field: 'payee', align: 'left' },
    { name: 'category', label: 'Category', field: 'categoryId', align: 'left' },
    { name: 'entity', label: 'Entity/Budget', field: 'entityId', align: 'left' },
    { name: 'amount', label: 'Amount', field: 'amount', align: 'right' },
    { name: 'status', label: 'Status', field: 'status', align: 'center' },
    { name: 'notes', label: 'Notes', field: 'notes', align: 'left' },
  ];

  const registerColumns: QTableColumn[] = [
    ...budgetColumns,
    { name: 'balance', label: 'Balance', field: 'runningBalance', align: 'right' },
  ];

  // seed initial
  void fetchMore();
  void fetchMoreRegister();

  return {
    transactions,
    registerRows,
    fetchMore,
    fetchMoreRegister,
    loading,
    loadingRegister,
    scrollToDate,
    budgetColumns,
    registerColumns,
    withinDateWindow,
    isDuplicate,
    link,
    unlink,
  } as const;
}

// duplicate helper exported at module scope
export function withinDateWindow(a: string, b: string, days: number) {
  const diff = Math.abs(new Date(a).getTime() - new Date(b).getTime());
  return diff <= days * 86400000;
}

export function isDuplicate(tx: BudgetTransaction, others: BudgetTransaction[], days = 3) {
  return others.some(
    (o) =>
      o.id !== tx.id &&
      o.payee === tx.payee &&
      Math.abs(o.amount - tx.amount) <= 0.01 &&
      withinDateWindow(tx.date, o.date, days)
  );
}

export function link(tx: BudgetTransaction, importedId: string) {
  tx.linkId = importedId;
}

export function unlink(tx: BudgetTransaction) {
  delete tx.linkId;
}
