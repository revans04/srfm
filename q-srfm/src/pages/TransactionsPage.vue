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
  <q-page class="bg-grey-2">
    <!-- Sticky header: title and tabs -->
    <div class="top-bar bg-grey-2 q-px-md q-pt-md">
      <div class="row items-center q-gutter-md">
        <div class="col-auto text-h5">Transactions</div>
        <q-tabs v-model="tab" dense class="col">
          <q-tab name="budget" label="Budget Register" />
          <q-tab name="register" label="Account Register" />
          <q-tab name="match" label="Match Bank Transactions" />
        </q-tabs>
      </div>
    </div>

    <q-tab-panels v-model="tab" animated>
      <!-- Budget Register Tab -->
      <q-tab-panel name="budget">
        <div class="filter-bar shadow-2 bg-white q-pa-sm">
          <div class="column q-gutter-sm">
            <EntitySelector @change="loadBudgets" class="col-auto" />
            <div class="row q-col-gutter-sm items-center">
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
                class="col-6"
              />
              <q-btn
                dense
                flat
                label="Refresh"
                class="col-auto"
                @click="refreshBudget"
              />
              <q-btn
                dense
                flat
                label="Clear All"
                class="col-auto"
                @click="clearBudgetFilters"
              />
            </div>
            <div class="row q-col-gutter-sm">
              <q-input v-model="filters.search" dense outlined placeholder="Search" class="col" />
              <q-input v-model="filters.importedMerchant" dense outlined placeholder="Imported Merchant" class="col-2" />
              <q-input v-model="minAmtInput" type="number" dense outlined placeholder="Amount Min" class="col-2" />
              <q-input v-model="maxAmtInput" type="number" dense outlined placeholder="Amount Max" class="col-2" />
              <q-input v-model="filters.start" dense outlined placeholder="Date" mask="####-##-##" class="col-2">
                <template #append>
                  <q-icon name="event" class="cursor-pointer">
                    <q-popup-proxy transition-show="scale" transition-hide="scale">
                      <q-date v-model="filters.start" mask="YYYY-MM-DD" />
                    </q-popup-proxy>
                  </q-icon>
                </template>
              </q-input>
            </div>
            <div class="row items-center q-gutter-sm">
              <q-btn
                dense
                :color="filters.cleared ? 'primary' : 'grey-5'"
                text-color="white"
                label="Cleared"
                @click="filters.cleared = !filters.cleared"
              />
              <q-btn
                dense
                :color="filters.uncleared ? 'primary' : 'grey-5'"
                text-color="white"
                label="Uncleared"
                @click="filters.uncleared = !filters.uncleared"
              />
              <q-btn
                dense
                :color="filters.reconciled ? 'primary' : 'grey-5'"
                text-color="white"
                label="Reconciled"
                @click="filters.reconciled = !filters.reconciled"
              />
              <q-btn
                dense
                :color="filters.duplicatesOnly ? 'primary' : 'grey-5'"
                text-color="white"
                label="Duplicates"
                @click="filters.duplicatesOnly = !filters.duplicatesOnly"
              />
              <q-space />
              <q-btn dense flat label="Jump to Date">
                <q-menu v-model="jumpMenu" anchor="bottom right" self="top right">
                  <q-date v-model="jumpDate" mask="YYYY-MM-DD" @update:model-value="onJump" />
                </q-menu>
              </q-btn>
            </div>
            <!-- Active filter chips -->
            <div class="q-mt-sm">
              <q-chip
                v-for="(val, key) in activeChips"
                :key="key"
                dense
                removable
                @remove="() => removeChip(key)"
              >{{ key }}<template v-if="val">: {{ val }}</template></q-chip>
            </div>
          </div>
        </div>
        <ledger-table
          :rows="transactions"
          :loading="loading"
          @load-more="fetchMore"
          @row-click="onRowClick"
        />
        <TransactionDialog
          v-if="editTx"
          :show-dialog="showTxDialog"
          :initial-transaction="editTx"
          :edit-mode="true"
          :loading="false"
          :category-options="editCategoryOptions"
          :budget-id="editBudgetId"
          :user-id="auth.user?.uid || ''"
          @update:showDialog="(val) => { showTxDialog = val; if (!val) editTx = null; }"
          @save="onTransactionSaved"
          @cancel="() => { showTxDialog = false; editTx = null; }"
        />
      </q-tab-panel>

      <!-- Account Register Tab -->
      <q-tab-panel name="register">
        <statement-header class="q-mb-sm" />
        <div class="filter-bar shadow-2 bg-white q-pa-sm">
          <!-- reuse same filters for demo -->
          <div class="row q-col-gutter-sm items-center">
            <q-select
              v-model="filters.accountId"
              :options="accountOptions"
              dense
              outlined
              label="Account"
              clearable
              emit-value
              map-options
              class="col-3"
            />
            <q-input v-model="filters.search" dense outlined placeholder="Search" class="col" />
            <q-checkbox v-model="filters.unmatchedOnly" label="Unmatched Only" class="col-auto" />
            <q-btn
              dense
              flat
              label="Refresh"
              class="col-auto"
              @click="refreshRegister"
            />
            <q-btn
              dense
              flat
              label="Clear All"
              class="col-auto"
              @click="clearRegisterFilters"
            />
          </div>
        </div>
        <ledger-table
          :rows="registerRows"
          :loading="loadingRegister"
          entity-label="Account"
          :can-load-more="canLoadMoreRegister"
          :loading-more="loadingMoreRegister"
          @load-more="fetchMoreRegister"
        />
      </q-tab-panel>

      <!-- Match Bank Transactions Tab -->
      <q-tab-panel name="match">
        <match-bank-panel />
      </q-tab-panel>
    </q-tab-panels>
  </q-page>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue';
import { storeToRefs } from 'pinia';
import LedgerTable from 'src/components/LedgerTable.vue';
import StatementHeader from 'src/components/StatementHeader.vue';
import MatchBankPanel from 'src/components/MatchBankPanel.vue';
import EntitySelector from 'src/components/EntitySelector.vue';
import TransactionDialog from 'src/components/TransactionDialog.vue';
import { useTransactions } from 'src/composables/useTransactions';
import type { LedgerFilters, LedgerRow } from 'src/composables/useTransactions';
import { useBudgetStore } from 'src/store/budget';
import { useFamilyStore } from 'src/store/family';
import { useUIStore } from 'src/store/ui';
import { useAuthStore } from 'src/store/auth';
import { sortBudgetsByMonthDesc } from 'src/utils/budget';
import { dataAccess } from 'src/dataAccess';
import type { Transaction } from 'src/types';

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
  scrollToDate,
  loadImportedTransactions,
  loadInitial,
} = useTransactions();

onMounted(loadBudgets);

const minAmtInput = ref('');
const maxAmtInput = ref('');

const showTxDialog = ref(false);
const editTx = ref<Transaction | null>(null);
const editBudgetId = ref('');
const editCategoryOptions = computed(() =>
  editBudgetId.value
    ? budgetStore.getBudget(editBudgetId.value)?.categories.map((c) => c.name) || []
    : [],
);

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

function onRowClick(row: LedgerRow) {
  const budget = budgetStore.getBudget(row.budgetId);
  const tx = budget?.transactions?.find((t) => t.id === row.id);
  if (tx) {
    editTx.value = { ...tx };
    editBudgetId.value = budget?.budgetId || '';
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

// Jump to date
const jumpDate = ref('');
const jumpMenu = ref(false);
function onJump(val: string) {
  jumpMenu.value = false;
  if (val) scrollToDate(val);
}

</script>

