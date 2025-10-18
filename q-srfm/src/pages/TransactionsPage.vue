<!--
README
======
TransactionsPage.vue demonstrates the new Pro Mode ledger UI. It uses
LedgerTable with Quasar virtual scrolling and a sticky header to keep
large datasets responsive while loading 100 rows initially and then 50
more at a time as the user scrolls.
Filters are reactive but for demo purposes only filter the locally seeded
mock data.

Key props/usage:
- uses `useTransactions` composable which seeds ~1000 rows on first load
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
              <div class="row q-col-gutter-sm q-col-gutter-y-sm items-center">
                <div class="col-12 col-md-4">
                  <EntitySelector class="full-width" @change="loadBudgets" />
                </div>
                <div class="col-12 col-md-4">
                  <q-input v-model="filters.search" dense outlined placeholder="Search" />
                </div>
                <div class="col-12 col-md-4">
                  <div class="row q-gutter-sm justify-end">
                    <div class="col-auto">
                      <q-btn dense flat label="Refresh" @click="refreshBudget" />
                    </div>
                    <div class="col-auto">
                      <q-btn dense flat label="Clear All" @click="clearBudgetFilters" />
                    </div>
                  </div>
                </div>
              </div>
              <div class="row q-col-gutter-sm q-col-gutter-y-sm">
                <div class="col-12">
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
                  />
                </div>
              </div>
              <div class="row q-col-gutter-sm q-col-gutter-y-sm">
                <div class="col-12 col-sm-6 col-md-3">
                  <q-input v-model="filters.importedMerchant" dense outlined placeholder="Imported Merchant" />
                </div>
                <div class="col-12 col-sm-6 col-md-2">
                  <q-input v-model="minAmtInput" type="number" dense outlined placeholder="Amount Min" />
                </div>
                <div class="col-12 col-sm-6 col-md-2">
                  <q-input v-model="maxAmtInput" type="number" dense outlined placeholder="Amount Max" />
                </div>
                <div class="col-12 col-sm-6 col-md-2">
                  <q-input v-model="filters.start" dense outlined placeholder="Start Date" mask="####-##-##">
                    <template #append>
                      <q-icon name="event" class="cursor-pointer">
                        <q-popup-proxy transition-show="scale" transition-hide="scale">
                          <q-date v-model="filters.start" mask="YYYY-MM-DD" />
                        </q-popup-proxy>
                      </q-icon>
                    </template>
                  </q-input>
                </div>
                <div class="col-12 col-sm-6 col-md-2">
                  <q-input v-model="filters.end" dense outlined placeholder="End Date" mask="####-##-##">
                    <template #append>
                      <q-icon name="event" class="cursor-pointer">
                        <q-popup-proxy transition-show="scale" transition-hide="scale">
                          <q-date v-model="filters.end" mask="YYYY-MM-DD" />
                        </q-popup-proxy>
                      </q-icon>
                    </template>
                  </q-input>
                </div>
              </div>
              <div class="transactions-filters__toggles">
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.cleared ? 'primary' : 'white'"
                  :text-color="filters.cleared ? 'white' : 'primary'"
                  @click="filters.cleared = !filters.cleared"
                  >Cleared</q-chip
                >
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.uncleared ? 'primary' : 'white'"
                  :text-color="filters.uncleared ? 'white' : 'primary'"
                  @click="filters.uncleared = !filters.uncleared"
                  >Uncleared</q-chip
                >
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.reconciled ? 'primary' : 'white'"
                  :text-color="filters.reconciled ? 'white' : 'primary'"
                  @click="filters.reconciled = !filters.reconciled"
                  >Reconciled</q-chip
                >
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.duplicatesOnly ? 'primary' : 'white'"
                  :text-color="filters.duplicatesOnly ? 'white' : 'primary'"
                  @click="filters.duplicatesOnly = !filters.duplicatesOnly"
                  >Duplicates</q-chip
                >
              </div>
              <div class="transactions-filters__chips" v-if="Object.keys(activeChips).length">
                <q-chip v-for="(val, key) in activeChips" :key="key" dense removable @remove="() => removeChip(key)">
                  {{ key }}<template v-if="val">: {{ val }}</template>
                </q-chip>
              </div>
            </div>
            <div v-if="selectedBudgetRowIds.length" class="transactions-selection panel-card">
              <div class="transactions-selection__title">
                {{ selectedBudgetRowIds.length }} transaction{{ selectedBudgetRowIds.length > 1 ? 's' : '' }} selected
              </div>
              <div class="transactions-selection__actions">
                <q-btn color="negative" label="Mark as Deleted" @click="openBudgetDeleteDialog" />
                <q-btn flat color="primary" label="Clear Selection" @click="clearBudgetSelection" />
              </div>
            </div>
            <div class="transactions-table">
              <ledger-table
                :rows="transactions"
                :loading="loading"
                selection="multiple"
                v-model:selected="selectedBudgetRowIds"
                @row-click="onRowClick"
              />
            </div>
          </div>
        </div>
        <q-dialog v-model="showTxDialog" :width="!isMobile ? '550px' : undefined" :fullscreen="isMobile" @hide="onTxCancel">
          <q-card>
            <q-card-section class="bg-primary row items-center">
              <div class="text-h6 text-white">Edit {{ editTx?.merchant }} Transaction</div>
              <q-btn flat dense icon="close" color="white" class="q-ml-auto" @click="onTxCancel" />
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
        <q-dialog v-model="showBudgetDeleteDialog">
          <q-card>
            <q-card-section>
              Are you sure you want to mark {{ selectedBudgetRowIds.length }} transaction{{ selectedBudgetRowIds.length > 1 ? 's' : '' }} as deleted?
            </q-card-section>
            <q-card-actions>
              <q-btn flat label="Cancel" color="primary" v-close-popup />
              <q-btn
                flat
                label="Mark as Deleted"
                color="negative"
                :loading="deletingBudgetTransactions"
                @click="deleteSelectedBudgetTransactions"
              />
            </q-card-actions>
          </q-card>
        </q-dialog>
      </q-tab-panel>
      <!-- Account Register Tab -->
      <q-tab-panel name="register" class="transactions-panel">
        <div class="transactions-layout">
          <div class="transactions-layout__main">
            <div class="transactions-filters panel-card">
              <div class="row q-col-gutter-sm q-col-gutter-y-sm items-center">
                <div class="col-12 col-md-4">
                  <q-select v-model="filters.accountId" :options="accountOptions" dense outlined label="Account" clearable emit-value map-options />
                </div>
                <div class="col-12 col-md-4">
                  <q-input v-model="filters.search" dense outlined placeholder="Search" debounce="300" />
                </div>
                <div class="col-12 col-md-4">
                  <div class="row q-gutter-sm justify-end">
                    <div class="col-auto">
                      <q-btn dense flat label="Refresh" @click="refreshRegister" />
                    </div>
                    <div class="col-auto">
                      <q-btn dense flat label="Clear All" @click="clearRegisterFilters" />
                    </div>
                  </div>
                </div>
              </div>
              <div class="row q-col-gutter-sm q-col-gutter-y-sm">
                <div class="col-12 col-sm-6 col-md-3 col-lg-2">
                  <q-input v-model="minAmtInput" type="number" dense outlined placeholder="Amount Min" />
                </div>
                <div class="col-12 col-sm-6 col-md-3 col-lg-2">
                  <q-input v-model="maxAmtInput" type="number" dense outlined placeholder="Amount Max" />
                </div>
                <div class="col-12 col-sm-6 col-md-3 col-lg-2">
                  <q-input v-model="filters.start" dense outlined mask="####-##-##" placeholder="Start" debounce="300">
                    <template #append>
                      <q-icon name="event" class="cursor-pointer">
                        <q-popup-proxy transition-show="scale" transition-hide="scale">
                          <q-date v-model="filters.start" mask="YYYY-MM-DD" />
                        </q-popup-proxy>
                      </q-icon>
                    </template>
                  </q-input>
                </div>
                <div class="col-12 col-sm-6 col-md-3 col-lg-2">
                  <q-input v-model="filters.end" dense outlined mask="####-##-##" placeholder="End" debounce="300">
                    <template #append>
                      <q-icon name="event" class="cursor-pointer">
                        <q-popup-proxy transition-show="scale" transition-hide="scale">
                          <q-date v-model="filters.end" mask="YYYY-MM-DD" />
                        </q-popup-proxy>
                      </q-icon>
                    </template>
                  </q-input>
                </div>
              </div>
              <div class="transactions-filters__toggles">
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.cleared ? 'primary' : 'white'"
                  :text-color="filters.cleared ? 'white' : 'primary'"
                  @click="filters.cleared = !filters.cleared"
                  >Cleared</q-chip
                >
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.uncleared ? 'primary' : 'white'"
                  :text-color="filters.uncleared ? 'white' : 'primary'"
                  @click="filters.uncleared = !filters.uncleared"
                  >Uncleared</q-chip
                >
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.reconciled ? 'primary' : 'white'"
                  :text-color="filters.reconciled ? 'white' : 'primary'"
                  @click="filters.reconciled = !filters.reconciled"
                  >Reconciled</q-chip
                >
                <q-chip
                  clickable
                  class="filter-chip"
                  :color="filters.unmatchedOnly ? 'primary' : 'white'"
                  :text-color="filters.unmatchedOnly ? 'white' : 'primary'"
                  @click="filters.unmatchedOnly = !filters.unmatchedOnly"
                  >Unmatched Only</q-chip
                >
              </div>
            </div>
            <div class="register-reconcile panel-card q-mt-md">
              <div class="row q-col-gutter-sm q-col-gutter-y-sm items-end">
                <div class="col-12 col-sm-6 col-md-3">
                  <q-input v-model="registerBeginningBalanceInput" type="number" dense outlined label="Beginning Balance" />
                </div>
                <div class="col-12 col-sm-6 col-md-3">
                  <q-input v-model="registerTargetEndingInput" type="number" dense outlined label="Target Ending Balance" />
                </div>
                <div class="col-12 col-sm-6 col-md-2">
                  <div class="text-caption text-grey-7">Matched Total</div>
                  <q-badge color="primary" outline class="text-body2 q-mt-xs">{{ formatCurrency(registerMatchedTotal) }}</q-badge>
                </div>
                <div class="col-12 col-sm-6 col-md-2">
                  <div class="text-caption text-grey-7">Computed Ending</div>
                  <q-badge color="primary" outline class="text-body2 q-mt-xs">{{ formatCurrency(registerComputedEnding) }}</q-badge>
                </div>
                <div class="col-12 col-sm-6 col-md-2">
                  <div class="text-caption text-grey-7">Delta</div>
                  <q-badge :color="registerDeltaBadgeColor" outline class="text-body2 q-mt-xs">{{ formatCurrency(registerDelta) }}</q-badge>
                </div>
              </div>
              <div class="row q-col-gutter-sm q-col-gutter-y-sm items-center q-mt-md">
                <div class="col-12 col-md-4">
                  <q-linear-progress :value="registerProgress" color="secondary" track-color="grey-3" rounded />
                </div>
                <div class="col-12 col-md-4">
                  <q-checkbox
                    v-model="registerAllClearedSelected"
                    label="Select Cleared In View"
                    dense
                    :disable="!registerClearedVisibleRows.length"
                  />
                </div>
                <div class="col-12 col-md-4 flex justify-end">
                  <q-btn
                    color="primary"
                    :disable="registerFinalizeDisabled"
                    :loading="registerFinalizing"
                    label="Finalize Reconciliation"
                    @click="finalizeRegisterStatement"
                  />
                </div>
              </div>
            </div>
            <div v-if="selectedRegisterIds.length" class="transactions-selection panel-card">
              <div class="transactions-selection__title">
                {{ selectedRegisterIds.length }} transaction{{ selectedRegisterIds.length > 1 ? 's' : '' }} selected
              </div>
              <div class="transactions-selection__actions">
                <q-btn color="primary" label="Create Budget Transactions" @click="openRegisterBatchDialog" />
                <q-btn color="warning" label="Ignore" @click="confirmRegisterBatchAction('Ignore')" />
                <q-btn color="negative" label="Delete" @click="confirmRegisterBatchAction('Delete')" />
                <q-btn
                  color="positive"
                  label="Finalize"
                  :disable="registerFinalizeDisabled"
                  :loading="registerFinalizing"
                  @click="finalizeRegisterStatement"
                />
              </div>
            </div>
            <div class="transactions-table">
              <ledger-table
                :rows="registerRows"
                :loading="loadingRegister"
                entity-label="Account"
                selection="multiple"
                v-model:selected="selectedRegisterIds"
                @row-click="onRegisterRowClick"
              />
            </div>
          </div>
        </div>
        <q-dialog v-model="showRegisterBatchDialog" max-width="500" @keyup.enter="executeRegisterBatchMatch">
          <q-card>
            <q-card-section class="bg-primary q-py-md">
              <span class="text-white">Batch Match Transactions</span>
            </q-card-section>
            <q-card-section class="q-pt-lg">
              <q-form ref="registerBatchForm">
                <p>
                  Assign an entity, merchant, and category for {{ selectedRegisterIds.length }} unmatched transaction{{
                    selectedRegisterIds.length > 1 ? 's' : ''
                  }}.
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
                      <q-select v-model="entry.category" :options="categoryOptions" label="Category" dense :rules="requiredField" />
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
              Are you sure you want to {{ registerBatchAction.toLowerCase() }} {{ selectedRegisterIds.length }} transaction{{
                selectedRegisterIds.length > 1 ? 's' : ''
              }}?
            </q-card-section>
            <q-card-actions>
              <q-btn flat label="Cancel" color="primary" v-close-popup />
              <q-btn flat label="Confirm" color="primary" :loading="saving" @click="performRegisterBatchAction" />
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
import MatchBankPanel from 'src/components/MatchBankPanel.vue';
import EntitySelector from 'src/components/EntitySelector.vue';
import TransactionForm from 'src/components/TransactionForm.vue';
import { useTransactions } from 'src/composables/useTransactions';
import type { LedgerFilters, LedgerRow, Status } from 'src/composables/useTransactions';
import { useBudgetStore } from 'src/store/budget';
import { useFamilyStore } from 'src/store/family';
import { useUIStore } from 'src/store/ui';
import { useAuthStore } from 'src/store/auth';
import { sortBudgetsByMonthDesc, createBudgetForMonth } from 'src/utils/budget';
import { dataAccess } from 'src/dataAccess';
import type { Budget, Transaction, StatementFinalizePayload } from 'src/types';
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

const {
  transactions,
  filters,
  registerRows,
  loading,
  loadingRegister,
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
const editCategoryOptions = computed(() => (editBudgetId.value ? budgetStore.getBudget(editBudgetId.value)?.categories.map((c) => c.name) || [] : []));

const selectedBudgetRowIds = ref<string[]>([]);
const showBudgetDeleteDialog = ref(false);
const deletingBudgetTransactions = ref(false);
const selectedBudgetRows = computed(() =>
  selectedBudgetRowIds.value
    .map((id) => transactions.value.find((row) => row.id === id))
    .filter((row): row is LedgerRow => Boolean(row)),
);

const selectedRegisterIds = ref<string[]>([]);
const showRegisterBatchDialog = ref(false);
const registerBatchForm = ref();
const batchEntries = ref<{ id: string; date: string; amount: number; merchant: string; category: string }[]>([]);
const showRegisterActionDialog = ref(false);
const registerBatchAction = ref<'Ignore' | 'Delete' | ''>('');
const saving = ref(false);
const registerBeginningBalanceInput = ref('0');
const registerTargetEndingInput = ref('0');
const registerFinalizing = ref(false);

const normalizedRegisterStatus = (status: LedgerRow['status']): Status => {
  if (status === 'M') return 'C';
  if (status === 'I') return 'U';
  return status;
};

const registerClearedVisibleRows = computed(() =>
  registerRows.value.filter((row) => normalizedRegisterStatus(row.status) === 'C'),
);

const registerSelectedRows = computed(() =>
  selectedRegisterIds.value
    .map((id) => registerRows.value.find((row) => row.id === id))
    .filter((row): row is LedgerRow => Boolean(row)),
);

const registerSelectedClearedRows = computed(() =>
  registerSelectedRows.value.filter((row) => normalizedRegisterStatus(row.status) === 'C'),
);

const parseNumericInput = (value: string): number => {
  const num = Number(value);
  return Number.isFinite(num) ? num : 0;
};

const registerMatchedTotal = computed(() =>
  registerSelectedClearedRows.value.reduce((sum, row) => sum + row.amount, 0),
);
const registerBeginningBalance = computed(() => parseNumericInput(registerBeginningBalanceInput.value));
const registerTargetEndingBalance = computed(() => parseNumericInput(registerTargetEndingInput.value));
const registerComputedEnding = computed(() => registerBeginningBalance.value + registerMatchedTotal.value);
const registerDelta = computed(() => registerTargetEndingBalance.value - registerComputedEnding.value);

const registerProgress = computed(() => {
  const change = registerTargetEndingBalance.value - registerBeginningBalance.value;
  if (!Number.isFinite(change) || change === 0) {
    return Math.abs(registerDelta.value) < 0.005 ? 1 : 0;
  }
  const ratio = 1 - Math.min(1, Math.abs(registerDelta.value) / Math.abs(change));
  const bounded = Number.isNaN(ratio) ? 0 : ratio;
  return Math.max(0, Math.min(1, bounded));
});

const registerDeltaBadgeColor = computed(() => {
  if (Math.abs(registerDelta.value) < 0.005) return 'positive';
  return registerDelta.value > 0 ? 'warning' : 'negative';
});

const registerFinalizeDisabled = computed(() => {
  if (!filters.value.accountId) return true;
  if (!filters.value.start || !filters.value.end) return true;
  if (!registerSelectedClearedRows.value.length) return true;
  if (!Number.isFinite(registerBeginningBalance.value) || !Number.isFinite(registerTargetEndingBalance.value)) return true;
  return Math.abs(registerDelta.value) >= 0.01;
});

const registerAllClearedSelected = computed({
  get() {
    if (!registerClearedVisibleRows.value.length) return false;
    return registerClearedVisibleRows.value.every((row) => selectedRegisterIds.value.includes(row.id));
  },
  set(value: boolean) {
    const clearedIds = registerClearedVisibleRows.value.map((row) => row.id);
    if (value) {
      const merged = new Set([...selectedRegisterIds.value, ...clearedIds]);
      selectedRegisterIds.value = Array.from(merged);
    } else {
      const clearedSet = new Set(clearedIds);
      selectedRegisterIds.value = selectedRegisterIds.value.filter((id) => !clearedSet.has(id));
    }
  },
});

const entityOptions = computed(() => (familyStore.family?.entities || []).map((e) => ({ id: e.id, name: e.name })));

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

function clearBudgetSelection() {
  selectedBudgetRowIds.value = [];
}

function openBudgetDeleteDialog() {
  if (!selectedBudgetRows.value.length) return;
  showBudgetDeleteDialog.value = true;
}

async function finalizeRegisterStatement() {
  if (registerFinalizeDisabled.value) return;
  const familyId = familyStore.family?.id;
  if (!familyId) {
    $q.notify({ type: 'negative', message: 'Family context missing.' });
    return;
  }
  const accountId = filters.value.accountId;
  if (!accountId) {
    $q.notify({ type: 'negative', message: 'Select an account before finalizing.' });
    return;
  }
  registerFinalizing.value = true;
  const payload: StatementFinalizePayload = {
    familyId,
    accountId,
    startDate: filters.value.start || '',
    endDate: filters.value.end || '',
    beginningBalance: registerBeginningBalance.value,
    endingBalance: registerTargetEndingBalance.value,
    matchedTransactionIds: registerSelectedClearedRows.value.map((row) => row.id),
  };
  try {
    await dataAccess.finalizeStatement(payload);
    $q.notify({ type: 'positive', message: 'Statement finalized.' });
    selectedRegisterIds.value = selectedRegisterIds.value.filter(
      (id) => !payload.matchedTransactionIds.includes(id),
    );
    await loadImportedTransactions(true);
  } catch (err) {
    console.error('Failed to finalize statement', err);
    const message = err instanceof Error ? err.message : 'Unable to finalize statement.';
    $q.notify({ type: 'negative', message });
  } finally {
    registerFinalizing.value = false;
  }
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

watch(
  () => filters.value.accountId,
  () => {
    registerBeginningBalanceInput.value = '0';
    registerTargetEndingInput.value = '0';
    selectedRegisterIds.value = [];
    filters.value.start = null;
    filters.value.end = null;
  },
);

const formatLongMonth = (month: string) => {
  const [year, monthNum] = month.split('-');
  const date = new Date(parseInt(year), parseInt(monthNum) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
};

const budgetOptions = computed(() =>
  sortBudgetsByMonthDesc(Array.from(budgetStore.budgets.values()).filter((b) => !selectedEntityId.value || b.entityId === selectedEntityId.value)).map((b) => ({
    label: formatLongMonth(b.month),
    value: b.budgetId || '',
  })),
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

watch(
  () => transactions.value.map((row) => row.id),
  (ids) => {
    const available = new Set(ids);
    const filtered = selectedBudgetRowIds.value.filter((id) => available.has(id));
    if (filtered.length !== selectedBudgetRowIds.value.length) {
      selectedBudgetRowIds.value = filtered;
    }
  },
);

watch(tab, (value) => {
  if (value !== 'budget' && selectedBudgetRowIds.value.length) {
    selectedBudgetRowIds.value = [];
  }
});

watch(
  () => registerRows.value.map((row) => row.id),
  (ids) => {
    const available = new Set(ids);
    const filtered = selectedRegisterIds.value.filter((id) => available.has(id));
    if (filtered.length !== selectedRegisterIds.value.length) {
      selectedRegisterIds.value = filtered;
    }
  },
);

watch(registerRows, (rows) => {
  if (!rows.length) return;
  if (!filters.value.start) {
    const last = rows[rows.length - 1]?.date?.slice(0, 10);
    if (last) filters.value.start = last;
  }
  if (!filters.value.end) {
    const first = rows[0]?.date?.slice(0, 10);
    if (first) filters.value.end = first;
  }
});

function clearBudgetFilters() {
  selectedBudgetIds.value = [];
  filters.value = createDefaultFilters();
  syncInputsFromFilters();
}

function clearRegisterFilters() {
  filters.value = createDefaultFilters();
  syncInputsFromFilters();
  selectedRegisterIds.value = [];
  registerBeginningBalanceInput.value = '0';
  registerTargetEndingInput.value = '0';
  void loadImportedTransactions(true);
}

async function refreshBudget() {
  await loadBudgets();
  if (selectedBudgetIds.value.length > 0) {
    await loadInitial(selectedBudgetIds.value);
  }
}

async function ensureBudgetLoadedWithTransactions(budgetId: string): Promise<Budget | null> {
  let budget = budgetStore.getBudget(budgetId);
  if (!budget || !budget.transactions || budget.transactions.length === 0) {
    try {
      budget = await dataAccess.getBudget(budgetId);
      if (budget) {
        budgetStore.updateBudget(budgetId, budget);
      }
    } catch (err) {
      console.error('Failed to load budget for deletion', budgetId, err);
      return null;
    }
  }
  return budget ?? null;
}

async function deleteSelectedBudgetTransactions() {
  if (!selectedBudgetRows.value.length) {
    showBudgetDeleteDialog.value = false;
    return;
  }

  deletingBudgetTransactions.value = true;
  try {
    const budgetsById = new Map<string, Budget>();
    for (const row of selectedBudgetRows.value) {
      if (!row.budgetId) continue;
      let budget = budgetsById.get(row.budgetId);
      if (!budget) {
        budget = await ensureBudgetLoadedWithTransactions(row.budgetId);
        if (!budget) {
          throw new Error('Unable to load budget data for the selected transactions.');
        }
        budgetsById.set(row.budgetId, budget);
      }
      await dataAccess.deleteTransaction(budget, row.id);
    }

    showBudgetDeleteDialog.value = false;
    selectedBudgetRowIds.value = [];
    if (selectedBudgetIds.value.length > 0) {
      await loadInitial(selectedBudgetIds.value);
    }
    $q.notify({ type: 'positive', message: 'Transactions marked as deleted' });
  } catch (err) {
    console.error('Failed to mark transactions as deleted', err);
    const message = err instanceof Error ? err.message : 'Failed to delete transactions';
    $q.notify({ type: 'negative', message });
  } finally {
    deletingBudgetTransactions.value = false;
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
  (filters.value as Record<string, unknown>)[key as string] = typeof current === 'boolean' ? false : null;
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
      let targetBudget = Array.from(budgetStore.budgets.values()).find((b) => b.entityId === selectedEntityId.value && b.month === budgetMonth);
      if (!targetBudget) {
        targetBudget = await createBudgetForMonth(budgetMonth, family.id, ownerUid, selectedEntityId.value);
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
      const savedTx = await dataAccess.saveTransaction(targetBudget, tx);
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
  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease;
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
  min-height: 0;
  overflow: auto;
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
