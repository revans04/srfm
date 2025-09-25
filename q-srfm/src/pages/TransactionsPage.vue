<!--
README
======
TransactionsPage.vue demonstrates the new Pro Mode ledger UI. It uses
LedgerTable for virtualized infinite scrolling. Filters are reactive but
for demo purposes only filter the locally seeded mock data.

Key props/usage:
- uses `useTransactions` composable which seeds ~1000 rows on first load
- `fetchMore` loads the next page (50 rows) when the table scrolls to end
- `scrollToDate` (via Jump to Date control) scrolls to first row >= date
-->
<template>
  <q-page class="transactions-page">
    <section class="transactions-hero panel-card no-round-bottom">
      <div class="transactions-hero__header">
        <div class="transactions-hero__title-group">
          <div class="transactions-hero__title">Transactions</div>
          <div class="transactions-hero__subtitle">Monitor budgets and accounts side by side</div>
        </div>
        <q-tabs v-model="tab" dense class="transactions-hero__tabs" align="left">
          <q-tab name="budget" label="Budget Register" />
          <q-tab name="register" label="Account Register" />
          <q-tab name="match" label="Match Bank Transactions" />
        </q-tabs>
      </div>
      <div class="transactions-hero__metrics">
        <div class="tx-metric tx-metric--primary">
          <div class="tx-metric__label">Total Records</div>
          <div class="tx-metric__value">{{ overviewCounts.total }}</div>
        </div>
        <div class="tx-metric">
          <div class="tx-metric__label">Cleared</div>
          <div class="tx-metric__value">{{ overviewCounts.cleared }}</div>
        </div>
        <div class="tx-metric">
          <div class="tx-metric__label">Reconciled</div>
          <div class="tx-metric__value">{{ overviewCounts.reconciled }}</div>
        </div>
        <div class="tx-metric">
          <div class="tx-metric__label">Uncleared</div>
          <div class="tx-metric__value">{{ overviewCounts.uncleared }}</div>
        </div>
      </div>
    </section>

    <q-tab-panels v-model="tab" animated>
      <!-- Budget Register Tab -->
      <q-tab-panel name="budget" class="transactions-panel">
        <div class="transactions-layout">
          <div class="transactions-layout__main">
            <div class="transactions-filters panel-card">
              <div class="transactions-filters__row transactions-filters__row--entity">
                <EntitySelector class="transactions-filters__entity" @change="loadBudgets" />
                <q-input
                  v-model="filters.search"
                  dense
                  outlined
                  placeholder="Search"
                  class="transactions-filters__control"
                />
                <div class="transactions-filters__actions">
                  <q-btn dense flat label="Refresh" @click="refreshBudget" />
                  <q-btn dense flat label="Clear All" @click="clearBudgetFilters" />
                </div>
              </div>
              <div class="transactions-filters__row transactions-filters__row--select">
                <q-select
                  v-model="selectedBudgetIds"
                  :options="budgetOptions"
                  dense
                  outlined
                  multiple
                  use-chips
                  emit-value
                  map-options
                  placeholder="Select Budgets"
                  class="transactions-filters__control transactions-filters__control--full"
                />
              </div>
              <div class="transactions-filters__row transactions-filters__row--inputs">
                <q-input
                  v-model="filters.importedMerchant"
                  dense
                  outlined
                  placeholder="Imported Merchant"
                  class="transactions-filters__control"
                />
                <q-input
                  v-model="minAmtInput"
                  type="number"
                  dense
                  outlined
                  placeholder="Amount Min"
                  class="transactions-filters__control"
                />
                <q-input
                  v-model="maxAmtInput"
                  type="number"
                  dense
                  outlined
                  placeholder="Amount Max"
                  class="transactions-filters__control"
                />
                <q-input
                  v-model="filters.start"
                  dense
                  outlined
                  placeholder="Start Date"
                  mask="####-##-##"
                  class="transactions-filters__control"
                >
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy transition-show="scale" transition-hide="scale">
                        <q-date v-model="filters.start" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
                <q-input
                  v-model="filters.end"
                  dense
                  outlined
                  placeholder="End Date"
                  mask="####-##-##"
                  class="transactions-filters__control"
                >
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy transition-show="scale" transition-hide="scale">
                        <q-date v-model="filters.end" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
              </div>
              <div class="transactions-filters__toggles">
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.cleared ? 'primary' : 'white'"
                  :text-color="filters.cleared ? 'white' : 'primary'"
                  @click="filters.cleared = !filters.cleared"
                >Cleared</q-chip>
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.uncleared ? 'primary' : 'white'"
                  :text-color="filters.uncleared ? 'white' : 'primary'"
                  @click="filters.uncleared = !filters.uncleared"
                >Uncleared</q-chip>
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.reconciled ? 'primary' : 'white'"
                  :text-color="filters.reconciled ? 'white' : 'primary'"
                  @click="filters.reconciled = !filters.reconciled"
                >Reconciled</q-chip>
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.duplicatesOnly ? 'primary' : 'white'"
                  :text-color="filters.duplicatesOnly ? 'white' : 'primary'"
                  @click="filters.duplicatesOnly = !filters.duplicatesOnly"
                >Duplicates</q-chip>
              </div>
              <div class="transactions-filters__chips" v-if="Object.keys(activeChips).length">
                <q-chip
                  v-for="(val, key) in activeChips"
                  :key="key"
                  dense
                  removable
                  @remove="() => removeChip(key)"
                >
                  {{ key }}<template v-if="val">: {{ val }}</template>
                </q-chip>
              </div>
            </div>
            <div class="transactions-table">
              <ledger-table
                :rows="transactions"
                :loading="loading"
                :row-height="52"
                :header-offset="0"
                @load-more="fetchMore"
                @row-click="onRowClick"
              />
            </div>
          </div>
        </div>
        <q-dialog
          v-model="showTxDialog"
          :width="!isMobile ? '550px' : undefined"
          :fullscreen="isMobile"
          @hide="onTxCancel"
        >
          <q-card>
            <q-card-section class="bg-primary row items-center">
              <div class="text-h6 text-white">
                Edit {{ editTx?.merchant }} Transaction
              </div>
              <q-btn
                flat
                dense
                icon="close"
                color="white"
                class="q-ml-auto"
                @click="onTxCancel"
              />
            </q-card-section>
            <q-card-section>
              <TransactionForm
                v-if="showTxDialog && editTx"
                :initial-transaction="editTx"
                :category-options="editCategoryOptions"
                :budget-id="editBudgetId"
                :user-id="auth.user?.uid || ''"
                :show-cancel="true"
                :loading="false"
                @save="onTransactionSaved"
                @cancel="onTxCancel"
              />
            </q-card-section>
          </q-card>
        </q-dialog>
      </q-tab-panel>
      <!-- Account Register Tab -->
      <q-tab-panel name="register" class="transactions-panel">
        <div class="transactions-layout">
          <div class="transactions-layout__main">
            <statement-header class="panel-card transactions-statement q-mb-md" />
            <div class="transactions-filters panel-card">
              <div class="transactions-filters__row transactions-filters__row--entity">
                <q-select
                  v-model="filters.accountId"
                  :options="accountOptions"
                  dense
                  outlined
                  label="Account"
                  clearable
                  emit-value
                  map-options
                  class="transactions-filters__control transactions-filters__control--full"
                />
                <q-input
                  v-model="filters.search"
                  dense
                  outlined
                  placeholder="Search"
                  debounce="300"
                  class="transactions-filters__control"
                />
                <div class="transactions-filters__actions">
                  <q-btn dense flat label="Refresh" @click="refreshRegister" />
                  <q-btn dense flat label="Clear All" @click="clearRegisterFilters" />
                </div>
              </div>
              <div class="transactions-filters__row transactions-filters__row--inputs">
                <q-input
                  v-model="filters.search"
                  dense
                  outlined
                  placeholder="Search"
                  debounce="300"
                  class="transactions-filters__control"
                />
                <q-input
                  v-model="minAmtInput"
                  type="number"
                  dense
                  outlined
                  placeholder="Amount Min"
                  class="transactions-filters__control"
                />
                <q-input
                  v-model="maxAmtInput"
                  type="number"
                  dense
                  outlined
                  placeholder="Amount Max"
                  class="transactions-filters__control"
                />
                <q-input
                  v-model="filters.start"
                  dense
                  outlined
                  mask="####-##-##"
                  placeholder="Start"
                  debounce="300"
                  class="transactions-filters__control"
                >
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy transition-show="scale" transition-hide="scale">
                        <q-date v-model="filters.start" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
                <q-input
                  v-model="filters.end"
                  dense
                  outlined
                  mask="####-##-##"
                  placeholder="End"
                  debounce="300"
                  class="transactions-filters__control"
                >
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy transition-show="scale" transition-hide="scale">
                        <q-date v-model="filters.end" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
              </div>
              <div class="transactions-filters__toggles">
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.unmatchedOnly ? 'primary' : 'white'"
                  :text-color="filters.unmatchedOnly ? 'white' : 'primary'"
                  @click="filters.unmatchedOnly = !filters.unmatchedOnly"
                >Unmatched Only</q-chip>
              </div>
            </div>
            <div v-if="selectedRegisterIds.length" class="transactions-selection panel-card">
              <div class="transactions-selection__title">{{ selectedRegisterIds.length }} transaction{{ selectedRegisterIds.length > 1 ? 's' : '' }} selected</div>
              <div class="transactions-selection__actions">
                <q-btn color="primary" label="Create Budget Transactions" @click="openRegisterBatchDialog" />
                <q-btn color="warning" label="Ignore" @click="confirmRegisterBatchAction('Ignore')" />
                <q-btn color="negative" label="Delete" @click="confirmRegisterBatchAction('Delete')" />
              </div>
            </div>
            <div class="transactions-table">
              <ledger-table
                :rows="registerRows"
                :loading="loadingRegister"
                entity-label="Account"
                :row-height="52"
                :header-offset="0"
                :can-load-more="canLoadMoreRegister"
                :loading-more="loadingMoreRegister"
                selection="multiple"
                v-model:selected="selectedRegisterIds"
                @row-click="onRegisterRowClick"
                @load-more="fetchMoreRegister"
              />
            </div>
          </div>
          <aside class="transactions-layout__aside">
            <q-card class="panel-card transactions-summary-card">
              <div class="summary-title">Selection</div>
              <div class="summary-row">
                <span class="summary-label">Selected</span>
                <span class="summary-value">{{ selectedRegisterIds.length }}</span>
              </div>
              <div class="summary-row">
                <span class="summary-label">Account</span>
                <span class="summary-value">{{ registerAccountLabel }}</span>
              </div>
            </q-card>
            <q-card class="panel-card transactions-summary-card">
              <div class="summary-title">Imported Status</div>
              <div class="summary-row">
                <span class="summary-label">Records</span>
                <span class="summary-value">{{ registerRows.length }}</span>
              </div>
              <div class="summary-row">
                <span class="summary-label">More Available</span>
                <span class="summary-value">{{ canLoadMoreRegister ? 'Yes' : 'No' }}</span>
              </div>
            </q-card>
          </aside>
        </div>
        <q-dialog v-model="showRegisterBatchDialog" max-width="500" @keyup.enter="executeRegisterBatchMatch">
          <q-card>
            <q-card-section class="bg-primary q-py-md">
              <span class="text-white">Batch Match Transactions</span>
            </q-card-section>
            <q-card-section class="q-pt-lg">
              <q-form ref="registerBatchForm">
                <p>
                  Assign an entity, merchant, and category for {{ selectedRegisterIds.length }} unmatched
                  transaction{{ selectedRegisterIds.length > 1 ? 's' : '' }}.
                </p>
                <div class="row">
                  <div class="col">
                    <q-select
                      v-model="selectedEntityId"
                      :options="entityOptions"
                      option-label="name"
                      option-value="id"
                      emit-value
                      map-options
                      label="Select Entity"
                      variant="outlined"
                      density="compact"
                      :rules="requiredField"
                    />
                  </div>
                </div>
                <q-list bordered class="q-mt-md">
                  <q-item v-for="entry in batchEntries" :key="entry.id">
                    <q-item-section>
                      <div class="text-caption">{{ entry.date }} - {{ formatCurrency(entry.amount) }}</div>
                    </q-item-section>
                    <q-item-section>
                      <q-input v-model="entry.merchant" label="Merchant" dense :rules="requiredField" />
                    </q-item-section>
                    <q-item-section>
                      <q-select
                        v-model="entry.category"
                        :options="categoryOptions"
                        label="Category"
                        dense
                        :rules="requiredField"
                      />
                    </q-item-section>
                  </q-item>
                </q-list>
              </q-form>
            </q-card-section>
            <q-card-actions>
              <q-space />
              <q-btn color="grey" variant="text" @click="showRegisterBatchDialog = false">Cancel</q-btn>
              <q-btn color="primary" variant="flat" @click="executeRegisterBatchMatch" :loading="saving">Match</q-btn>
            </q-card-actions>
          </q-card>
        </q-dialog>
        <q-dialog v-model="showRegisterActionDialog">
          <q-card>
            <q-card-section>
              Are you sure you want to {{ registerBatchAction.toLowerCase() }}
              {{ selectedRegisterIds.length }} transaction{{ selectedRegisterIds.length > 1 ? 's' : '' }}?
            </q-card-section>
            <q-card-actions>
              <q-btn flat label="Cancel" color="primary" v-close-popup />
              <q-btn
                flat
                label="Confirm"
                color="primary"
                :loading="saving"
                @click="performRegisterBatchAction"
              />
            </q-card-actions>
          </q-card>
        </q-dialog>
      </q-tab-panel>
      <!-- Match Bank Transactions Tab -->
      <q-tab-panel name="match" class="transactions-panel">
        <match-bank-panel />
      </q-tab-panel>
    </q-tab-panels>
  </q-page>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue';
import { useQuasar } from 'quasar';
import { storeToRefs } from 'pinia';
import LedgerTable from 'src/components/LedgerTable.vue';
import StatementHeader from 'src/components/StatementHeader.vue';
import MatchBankPanel from 'src/components/MatchBankPanel.vue';
import EntitySelector from 'src/components/EntitySelector.vue';
import TransactionForm from 'src/components/TransactionForm.vue';
import { useTransactions } from 'src/composables/useTransactions';
import type { LedgerFilters, LedgerRow } from 'src/composables/useTransactions';
import { useBudgetStore } from 'src/store/budget';
import { useFamilyStore } from 'src/store/family';
import { useUIStore } from 'src/store/ui';
import { useAuthStore } from 'src/store/auth';
import { sortBudgetsByMonthDesc, createBudgetForMonth } from 'src/utils/budget';
import { dataAccess } from 'src/dataAccess';
import type { Transaction } from 'src/types';
import { splitImportedId } from 'src/utils/imported';


const tab = ref<'budget' | 'register' | 'match'>('budget');

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();
const uiStore = useUIStore();
const auth = useAuthStore();

const { selectedBudgetIds, budgetFilters, registerFilters } = storeToRefs(uiStore);
const { selectedEntityId } = storeToRefs(familyStore);

const accountOptions = computed(() => {
  const accounts = familyStore.family?.accounts || [];
  const opts = accounts
    .filter((a) => ['Bank', 'CreditCard', 'Investment'].includes(a.type))
    .map((a) => ({
      label: a.accountNumber ? `${a.name} (${a.accountNumber})` : a.name,
      value: String(a.id),
    }))
    .sort((a, b) => a.label.localeCompare(b.label));
  return [{ label: 'All', value: null }, ...opts];
});

const registerAccountLabel = computed(() => {
  if (!filters.value.accountId) {
    return 'All accounts';
  }
  const option = accountOptions.value.find((opt) => opt.value === filters.value.accountId);
  return option?.label || 'Account';
});

const {
  transactions,
  filters,
  registerRows,
  fetchMore,
  fetchMoreRegister,
  loading,
  loadingRegister,
  canLoadMoreRegister,
  loadingMoreRegister,
  loadImportedTransactions,
  loadInitial,
  getImportedTx,
} = useTransactions();


const activeRows = computed(() => {
  if (tab.value === 'budget') {
    return transactions.value;
  }
  if (tab.value === 'register') {
    return registerRows.value;
  }
  return [];
});

const overviewCounts = computed(() => {
  const counts = { total: activeRows.value.length, cleared: 0, reconciled: 0, uncleared: 0 };
  activeRows.value.forEach((row) => {
    if (row.status === 'C') {
      counts.cleared += 1;
    } else if (row.status === 'R') {
      counts.reconciled += 1;
    } else {
      counts.uncleared += 1;
    }
  });
  return counts;
});

onMounted(loadBudgets);

const minAmtInput = ref('');
const maxAmtInput = ref('');

const showTxDialog = ref(false);
const editTx = ref<Transaction | null>(null);
const editBudgetId = ref('');
const $q = useQuasar();
const isMobile = computed(() => $q.screen.lt.md);
const editCategoryOptions = computed(() =>
  editBudgetId.value
    ? budgetStore.getBudget(editBudgetId.value)?.categories.map((c) => c.name) || []
    : [],
);

const selectedRegisterIds = ref<string[]>([]);
const showRegisterBatchDialog = ref(false);
const registerBatchForm = ref();
const batchEntries = ref<{ id: string; date: string; amount: number; merchant: string; category: string }[]>([]);
const showRegisterActionDialog = ref(false);
const registerBatchAction = ref<'Ignore' | 'Delete' | ''>('');
const saving = ref(false);

const entityOptions = computed(() =>
  (familyStore.family?.entities || []).map((e) => ({ id: e.id, name: e.name }))
);

const categoryOptions = computed(() => {
  const cats = new Set<string>(['Income']);
  Array.from(budgetStore.budgets.values()).forEach((b) => {
    b.categories.forEach((cat) => cats.add(cat.name));
  });
  return Array.from(cats).sort();
});

const requiredField = [(v: string) => !!v || 'This field is required'];

function formatCurrency(n: number) {
  return (n < 0 ? '-$' : '$') + Math.abs(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

const createDefaultFilters = (): LedgerFilters => ({
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

function syncInputsFromFilters() {
  minAmtInput.value = filters.value.minAmt == null ? '' : String(filters.value.minAmt);
  maxAmtInput.value = filters.value.maxAmt == null ? '' : String(filters.value.maxAmt);
}

function applyStoredFilters(t: 'budget' | 'register') {
  const source = t === 'budget' ? budgetFilters.value : registerFilters.value;
  filters.value = { ...source };
  syncInputsFromFilters();
}

watch(
  tab,
  (t, old) => {
    if (old === 'budget') budgetFilters.value = { ...filters.value };
    if (old === 'register') registerFilters.value = { ...filters.value };
    if (t === 'budget' || t === 'register') {
      applyStoredFilters(t);
    }
  },
  { immediate: true },
);

watch(
  filters,
  (f) => {
    if (tab.value === 'budget') budgetFilters.value = { ...f };
    else if (tab.value === 'register') registerFilters.value = { ...f };
  },
  { deep: true },
);

async function ensureAccountsLoaded() {
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
}

// Ensure an account is selected when viewing the register so data loads
watch(
  [tab, accountOptions],
  async ([t]) => {
    if (t === 'register') {
      await ensureAccountsLoaded();
    }
  },
  { immediate: true },
);

watch(tab, async (t) => {
  if (t === 'register' && registerRows.value.length === 0) {
    await loadImportedTransactions(true);
  }
});

watch(minAmtInput, (v) => {
  filters.value.minAmt = v === '' ? null : Number(v);
});
watch(maxAmtInput, (v) => {
  filters.value.maxAmt = v === '' ? null : Number(v);
});
watch(
  () => filters.value.minAmt,
  (v) => {
    if (v == null) minAmtInput.value = '';
  },
);
watch(
  () => filters.value.maxAmt,
  (v) => {
    if (v == null) maxAmtInput.value = '';
  },
);

const formatLongMonth = (month: string) => {
  const [year, monthNum] = month.split('-');
  const date = new Date(parseInt(year), parseInt(monthNum) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
};

const budgetOptions = computed(() =>
  sortBudgetsByMonthDesc(
    Array.from(budgetStore.budgets.values()).filter(
      (b) => !selectedEntityId.value || b.entityId === selectedEntityId.value,
    ),
  ).map((b) => ({ label: formatLongMonth(b.month), value: b.budgetId || '' })),
);

function setCurrentBudgetSelection() {
  const budgetsArr = Array.from(budgetStore.budgets.values());
  if (budgetsArr.length === 0) {
    selectedBudgetIds.value = [];
    return;
  }
  const currentMonth = new Date().toISOString().slice(0, 7);
  const sorted = sortBudgetsByMonthDesc(budgetsArr);
  const current = sorted.find((b) => b.month === currentMonth) || sorted[0];
  selectedBudgetIds.value = current?.budgetId ? [current.budgetId] : [];
}

async function loadBudgets() {
  const user = auth.user;
  if (!user) return;
  await budgetStore.loadBudgets(user.uid, selectedEntityId.value);
  if (selectedBudgetIds.value.length === 0) {
    setCurrentBudgetSelection();
  }
}

watch(
  () => budgetStore.budgets.size,
  (size) => {
    if (size > 0 && selectedBudgetIds.value.length === 0) {
      setCurrentBudgetSelection();
    }
  },
  { immediate: true },
);

function clearBudgetFilters() {
  selectedBudgetIds.value = [];
  filters.value = createDefaultFilters();
  syncInputsFromFilters();
}

function clearRegisterFilters() {
  filters.value = createDefaultFilters();
  syncInputsFromFilters();
}

async function refreshBudget() {
  await loadBudgets();
  if (selectedBudgetIds.value.length > 0) {
    await loadInitial(selectedBudgetIds.value);
  }
}

async function refreshRegister() {
  await loadImportedTransactions(true);
}

async function onRowClick(row: LedgerRow) {
  const tx = row.transaction;
  if (tx) {
    editTx.value = { ...tx };
    editBudgetId.value = row.budgetId;
    showTxDialog.value = true;
    return;
  }

  let budget = budgetStore.getBudget(row.budgetId);

  // Some budgets in the store may only contain summary info without
  // transactions. If we don't have the full budget or its transaction
  // list hasn't been populated yet, fetch it from the API so we can
  // locate the transaction to edit.
  if (!budget || !budget.transactions || budget.transactions.length === 0) {
    try {
      budget = await dataAccess.getBudget(row.budgetId);
      if (budget) budgetStore.updateBudget(row.budgetId, budget);
    } catch (err) {
      console.error('Failed to load budget for transaction', row.budgetId, err);
    }
  }

  const fetchedTx = budget?.transactions?.find((t) => t.id === row.id);
  if (fetchedTx) {
    editTx.value = { ...fetchedTx };
    editBudgetId.value = budget?.budgetId || row.budgetId;
    showTxDialog.value = true;
  }
}

async function onTransactionSaved(updated: Transaction) {
  showTxDialog.value = false;
  const budget = budgetStore.getBudget(editBudgetId.value);
  if (budget) {
    const idx = budget.transactions.findIndex((t) => t.id === updated.id);
    if (idx >= 0) budget.transactions[idx] = updated;
    else budget.transactions.push(updated);
  }
  await loadInitial(selectedBudgetIds.value);
  editTx.value = null;
}

function onTxCancel() {
  showTxDialog.value = false;
  editTx.value = null;
}

// Filters come from useTransactions

function removeFilter(key: keyof typeof filters.value) {
  const current = filters.value[key];
  (filters.value as Record<string, unknown>)[key as string] =
    typeof current === 'boolean' ? false : null;
}

const activeChips = computed<Record<string, unknown>>(() => {
  const res: Record<string, unknown> = {};
  Object.entries(filters.value).forEach(([k, v]) => {
    if (k === 'accountId') return;
    if (v !== null && v !== '' && v !== false) {
      res[k] = typeof v === 'boolean' ? '' : v;
    }
  });
  return res;
});

function removeChip(key: string) {
  removeFilter(key as keyof typeof filters.value);
}

function onRegisterRowClick(row: LedgerRow) {
  if (row.status === 'U') {
    selectedRegisterIds.value = [row.id];
    openRegisterBatchDialog();
  }
}

function openRegisterBatchDialog() {
  if (
    selectedRegisterIds.value.length === 0 ||
    !selectedRegisterIds.value.every((id) => {
      const tx = registerRows.value.find((r) => r.id === id);
      return tx && tx.status === 'U';
    })
  ) {
    return;
  }
  batchEntries.value = selectedRegisterIds.value.map((id) => {
    const tx = registerRows.value.find((r) => r.id === id);
    return {
      id,
      date: tx?.date || '',
      amount: tx?.amount || 0,
      merchant: tx?.importedMerchant || tx?.payee || '',
      category: '',
    };
  });
  showRegisterBatchDialog.value = true;
}

async function executeRegisterBatchMatch() {
  if (!registerBatchForm.value) return;
  const valid = await registerBatchForm.value.validate();
  if (!valid) return;
  saving.value = true;
  try {
    const family = familyStore.family;
    const ownerUid = family?.ownerUid || auth.user?.uid || '';
    const matchesByBudget: Record<
      string,
      Array<{
        budgetTransactionId: string;
        importedTransactionId: string;
        match: boolean;
        ignore: boolean;
      }>
    > = {};
    for (const entry of batchEntries.value) {
      const imported = getImportedTx(entry.id);
      if (!imported || !family) continue;
      const budgetMonth = entry.date.slice(0, 7);
      let targetBudget = Array.from(budgetStore.budgets.values()).find(
        (b) => b.entityId === selectedEntityId.value && b.month === budgetMonth,
      );
      if (!targetBudget) {
        targetBudget = await createBudgetForMonth(
          budgetMonth,
          family.id,
          ownerUid,
          selectedEntityId.value,
        );
      }
      if (!targetBudget?.budgetId) continue;
      if (!targetBudget.categories.some((cat) => cat.name === entry.category)) {
        targetBudget.categories.push({
          name: entry.category,
          target: 0,
          isFund: false,
          group: 'Ungrouped',
        });
        await dataAccess.saveBudget(targetBudget.budgetId, targetBudget);
        budgetStore.updateBudget(targetBudget.budgetId, targetBudget);
      }
      const tx: Transaction = {
        id: '',
        date: entry.date,
        budgetMonth,
        merchant: entry.merchant,
        categories: [{ category: entry.category, amount: Math.abs(entry.amount) }],
        amount: Math.abs(entry.amount),
        notes: '',
        recurring: false,
        recurringInterval: 'Monthly',
        userId: auth.user?.uid || '',
        isIncome: entry.amount > 0,
        accountSource: imported.accountSource || '',
        accountNumber: imported.accountNumber || '',
        postedDate: imported.postedDate,
        checkNumber: imported.checkNumber,
        importedMerchant: imported.payee,
        status: 'C',
        entityId: selectedEntityId.value,
        taxMetadata: imported.taxMetadata || [],
      };
      const savedTx = await dataAccess.saveTransaction(targetBudget, tx, false);
      if (!matchesByBudget[targetBudget.budgetId]) matchesByBudget[targetBudget.budgetId] = [];
      matchesByBudget[targetBudget.budgetId].push({
        budgetTransactionId: savedTx.id,
        importedTransactionId: imported.id,
        match: true,
        ignore: false,
      });
    }
    await Promise.all(
      Object.entries(matchesByBudget).map(async ([budgetId, recs]) => {
        const budget = budgetStore.getBudget(budgetId);
        if (!budget) return;
        const reconcileData = {
          budgetId,
          reconciliations: recs,
        };
        await dataAccess.batchReconcileTransactions(budgetId, budget, reconcileData);
        const updatedBudget = await dataAccess.getBudget(budgetId);
        if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
      }),
    );
    showRegisterBatchDialog.value = false;
    selectedRegisterIds.value = [];
    await loadImportedTransactions(true);
  } finally {
    saving.value = false;
  }
}

function confirmRegisterBatchAction(action: 'Ignore' | 'Delete') {
  registerBatchAction.value = action;
  showRegisterActionDialog.value = true;
}

async function performRegisterBatchAction() {
  saving.value = true;
  try {
    for (const id of selectedRegisterIds.value) {
      const { docId, txId } = splitImportedId(id);
      if (registerBatchAction.value === 'Delete') {
        await dataAccess.deleteImportedTransaction(docId, txId);
      } else if (registerBatchAction.value === 'Ignore') {
        await dataAccess.updateImportedTransaction(docId, txId, false, true);
      }
    }
    selectedRegisterIds.value = [];
    showRegisterActionDialog.value = false;
    await loadImportedTransactions(true);
  } finally {
    saving.value = false;
  }
}

</script>

<style scoped>
.transactions-page {
  padding: 24px 24px 56px;
}

@media (min-width: 1280px) {
  .transactions-page {
    padding: 32px 48px 72px;
  }
}

.transactions-panel {
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.transactions-hero {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.transactions-hero__header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.transactions-hero__tabs {
  flex: 1 1 auto;
}

.transactions-hero__title {
  font-size: 1.6rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.transactions-hero__subtitle {
  font-size: 0.95rem;
  color: var(--color-text-muted);
  margin-top: 4px;
}

.transactions-hero__metrics {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
}

.tx-metric {
  background: var(--color-surface-subtle);
  border-radius: var(--radius-md);
  padding: 16px;
  box-shadow: var(--shadow-subtle);
}

.tx-metric--primary {
  background: linear-gradient(140deg, rgba(37, 99, 235, 0.16), rgba(14, 165, 233, 0.08));
}

.tx-metric__label {
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--color-text-muted);
  font-weight: 600;
}

.tx-metric__value {
  margin-top: 8px;
  font-size: 1.35rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.transactions-layout {
  display: flex;
  flex-direction: column;
  gap: 20px;
  flex: 1;
  min-height: 0;
}

@media (min-width: 1200px) {
  .transactions-layout {
    flex-direction: row;
    align-items: stretch;
  }
}

.transactions-layout__main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-width: 0;
  min-height: 0;
}

.transactions-layout__aside {
  width: 320px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

@media (max-width: 1199px) {
  .transactions-layout__aside {
    width: 100%;
    min-height: 0;
  }
}

.transactions-filters {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.transactions-filters__row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
}

.transactions-filters__row--entity {
  justify-content: space-between;
}

.transactions-filters__row--select {
  margin-top: -4px;
}

.transactions-filters__row--inputs {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
}

.transactions-filters__control {
  flex: 1 1 200px;
  min-width: 160px;
}

.transactions-filters__control--full {
  flex: 1 1 100%;
  min-width: 100%;
}

.transactions-filters__entity {
  flex: 1 1 220px;
  min-width: 200px;
}

.transactions-filters__actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

.transactions-filters__toggles {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.filter-chip {
  border-radius: 999px;
  padding: 4px 14px;
  font-weight: 600;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  box-shadow: 0 6px 14px rgba(37, 99, 235, 0.12);
}

.filter-chip:not(.bg-primary) {
  box-shadow: none;
  border: 1px solid rgba(37, 99, 235, 0.22);
}

.filter-chip:hover {
  transform: translateY(-1px);
}

.transactions-filters__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.transactions-filters__chips :deep(.q-chip) {
  background: rgba(37, 99, 235, 0.1);
}

.transactions-summary-card {
  padding: 18px 20px;
}

.summary-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 12px;
  font-size: 0.95rem;
}

.summary-label {
  color: var(--color-text-muted);
}

.summary-value {
  font-weight: 600;
  color: var(--color-text-primary);
  text-align: right;
}

.transactions-selection {
  padding: 18px 20px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.transactions-selection__title {
  font-size: 1rem;
  font-weight: 600;
}

.transactions-selection__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.transactions-statement {
  padding: 18px 20px;
}


.transactions-table {
  flex: 1;
  min-height: 360px;
  display: flex;
  min-width: 0;
}

.transactions-table :deep(.q-table) {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.transactions-table :deep(.q-table__middle) {
  flex: 1;
  display: flex;
  min-height: 0;
}

.transactions-table :deep(.q-virtual-scroll) {
  flex: 1;
}

.transactions-hero__tabs :deep(.q-tab) {
  min-width: 0;
}

@media (max-width: 959px) {
  .transactions-page {
    padding: 16px 12px 48px;
  }

  .transactions-hero__metrics {
    grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  }

  .transactions-layout__aside {
    width: 100%;
  }
}
</style>
