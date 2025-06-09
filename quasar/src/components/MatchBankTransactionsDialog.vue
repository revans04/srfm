<!-- src/components/BankTransactionMatchingDialog.vue -->
<template>
  <q-dialog
    v-if="isReady"
    :model-value="props.showDialog"
    :full-width="isMobile"
    :full-height="isMobile"
    persistent
    @update:model-value="closeDialog($event)"
  >
    <q-card>
      <q-card-section class="q-pa-md">
        <div class="row items-center">
          <div class="col"><q-space /></div>
          <div class="col-auto">
            <q-btn
              flat
              color="negative"
              icon="mdi-close"
              @click="closeDialog(false)"
              :disable="props.matching"
            />
          </div>
        </div>
        <q-tabs v-model="activeTab" dense align="justify" class="bg-grey-2">
          <q-tab name="smart-matches" label="Smart Matches" />
          <q-tab name="remaining" label="Remaining Transactions" />
        </q-tabs>

        <q-tab-panels v-model="activeTab" animated>
          <!-- Smart Matches Tab -->
          <q-tab-panel name="smart-matches">
            <div v-if="smartMatches.length > 0" class="q-mt-md">
              <h3>Smart Matches ({{ smartMatches.length }})</h3>
              <p class="text-caption q-pb-sm">
                These imported transactions have exactly one potential match. Review and confirm
                below (max 50 at a time).
              </p>

              <!-- Sort Controls -->
              <div class="row q-mb-md">
                <div class="col-12 col-md-4 q-pa-sm">
                  <q-select
                    v-model="smartMatchesSortField"
                    :options="smartMatchesSortFields"
                    label="Sort By"
                    outlined
                    dense
                    option-label="text"
                    option-value="value"
                    @update:model-value="sortSmartMatches"
                  />
                </div>
                <div class="col-12 col-md-4 q-pa-sm">
                  <q-btn
                    :color="smartMatchesSortDirection === 'asc' ? 'primary' : 'secondary'"
                    :label="smartMatchesSortDirection === 'asc' ? 'Ascending' : 'Descending'"
                    @click="toggleSmartMatchesSortDirection"
                  />
                </div>
                <div class="col-12 col-md-4 q-pa-sm">
                  <q-input
                    v-model.number="smartMatchDateRange"
                    label="Match Date Range (days)"
                    type="number"
                    outlined
                    dense
                    @update:model-value="computeSmartMatchesLocal()"
                  />
                </div>
                <div class="col-12 q-pa-sm">
                  <q-btn
                    color="primary"
                    label="Confirm Selected Matches"
                    :disable="selectedSmartMatchIds.length === 0 || props.matching"
                    :loading="props.matching"
                    @click="confirmSmartMatches"
                  >
                    <template v-slot:default>
                      Confirm Selected Matches ({{ selectedSmartMatchIds.length }})
                    </template>
                  </q-btn>
                </div>
              </div>

              <q-table
                :columns="smartMatchHeaders"
                :rows="sortedSmartMatches"
                v-model:selected="selectedSmartMatchIds"
                selection="multiple"
                row-key="importedTransaction.id"
                :pagination="{ rowsPerPage: 50 }"
                hide-pagination
                class="q-mt-md"
              >
                <template v-slot:body-cell-bankAmount="{ row }">
                  ${{ toDollars(toCents(row.bankAmount)) }}
                </template>
                <template v-slot:body-cell-bankType="{ row }">
                  {{ row.bankType }}
                </template>
                <template v-slot:body-cell-budgetAmount="{ row }">
                  ${{ toDollars(toCents(row.budgetTransaction.amount)) }}
                </template>
                <template v-slot:body-cell-budgetType="{ row }">
                  {{ row.budgetTransaction.isIncome ? 'Income' : 'Expense' }}
                </template>
                <template v-slot:body-cell-actions="{ row }">
                  <q-icon
                    v-if="isBudgetTxMatchedMultiple(row.budgetTransaction.id)"
                    name="mdi-alert"
                    color="warning"
                    title="This budget transaction matches multiple bank transactions"
                  />
                </template>
              </q-table>
              <q-btn
                color="primary"
                label="Confirm Selected Matches"
                :disable="selectedSmartMatchIds.length === 0 || props.matching"
                :loading="props.matching"
                @click="confirmSmartMatches"
                class="q-mt-md"
              >
                <template v-slot:default>
                  Confirm Selected Matches ({{ selectedSmartMatchIds.length }})
                </template>
              </q-btn>
            </div>
            <div v-else class="q-mt-md">
              <q-banner class="bg-info text-white">
                No smart matches found. Check Remaining Transactions for potential conflicts.
              </q-banner>
            </div>
          </q-tab-panel>

          <!-- Remaining Transactions Tab -->
          <q-tab-panel name="remaining">
            <div v-if="remainingImportedTransactions.length > 0" class="q-mt-md">
              <h3>
                Remaining Transactions ({{ currentBankTransactionIndex + 1 }} of
                {{ remainingImportedTransactions.length }})
              </h3>
              <q-table
                :columns="[
                  { name: 'postedDate', label: 'Posted Date', field: 'postedDate' },
                  { name: 'payee', label: 'Payee', field: 'payee' },
                  {
                    name: 'amount',
                    label: 'Amount',
                    field: (row) => toDollars(toCents(row.debitAmount || row.creditAmount || 0)),
                  },
                  {
                    name: 'type',
                    label: 'Type',
                    field: (row) =>
                      row.debitAmount ? 'Debit' : row.creditAmount ? 'Credit' : 'N/A',
                  },
                  { name: 'accountSource', label: 'Account Source', field: 'accountSource' },
                  { name: 'accountNumber', label: 'Account #', field: 'accountNumber' },
                  {
                    name: 'checkNumber',
                    label: 'Check Number',
                    field: (row) => row.checkNumber || 'N/A',
                  },
                ]"
                :rows="[selectedBankTransaction]"
                hide-pagination
              />

              <!-- Search Filters -->
              <div class="row q-mt-md">
                <div class="col-12 col-md-4 q-pa-sm">
                  <q-input
                    v-model="searchAmount"
                    label="Amount"
                    type="number"
                    outlined
                    dense
                    readonly
                  />
                </div>
                <div class="col-12 col-md-4 q-pa-sm">
                  <q-input
                    v-model="searchMerchant"
                    label="Merchant"
                    outlined
                    dense
                    @update:model-value="searchBudgetTransactions"
                  />
                </div>
                <div class="col-12 col-md-4 q-pa-sm">
                  <q-input
                    v-model.number="searchDateRange"
                    label="Date Range (days)"
                    type="number"
                    outlined
                    dense
                    @update:model-value="searchBudgetTransactions"
                  />
                </div>
              </div>

              <!-- Split Transaction Option -->
              <div class="row q-mt-md">
                <div class="col">
                  <q-btn
                    color="primary"
                    :label="showSplitForm ? 'Cancel Split' : 'Split Transaction'"
                    :disable="props.matching"
                    @click="toggleSplitTransaction"
                  />
                </div>
              </div>

              <!-- Split Transaction Form -->
              <q-form
                v-if="showSplitForm"
                ref="splitForm"
                @submit="saveSplitTransaction"
                class="q-mt-md"
              >
                <div
                  v-for="(split, index) in transactionSplits"
                  :key="index"
                  class="row items-center"
                >
                  <div class="col-12 col-md-3 q-pa-sm">
                    <q-select
                      v-model="split.entityId"
                      :options="entityOptions"
                      option-label="name"
                      option-value="id"
                      label="Entity"
                      outlined
                      dense
                      :rules="[(v) => !!v || 'Entity is required']"
                    />
                  </div>
                  <div class="col-12 col-md-3 q-pa-sm">
                    <q-select
                      v-model="split.category"
                      :options="props.categoryOptions"
                      label="Category"
                      outlined
                      dense
                      use-input
                      :rules="[(v) => !!v || 'Category is required']"
                    />
                  </div>
                  <div class="col-12 col-md-2 q-pa-sm">
                    <q-input
                      v-model.number="split.amount"
                      label="Amount"
                      type="number"
                      outlined
                      dense
                      :rules="[(v) => v > 0 || 'Amount must be greater than 0']"
                    />
                  </div>
                  <div class="col-12 col-md-1 q-pa-sm">
                    <q-btn flat color="negative" icon="mdi-close" @click="removeSplit(index)" />
                  </div>
                </div>
                <q-banner
                  v-if="remainingSplitAmount !== 0"
                  :class="remainingSplitAmount < 0 ? 'bg-negative' : 'bg-warning'"
                  class="q-mb-md"
                >
                  <div v-if="remainingSplitAmount > 0">
                    Remaining ${{ toDollars(toCents(remainingSplitAmount)) }}
                  </div>
                  <div v-else>
                    Over allocated ${{ toDollars(toCents(Math.abs(remainingSplitAmount))) }}
                  </div>
                </q-banner>
                <q-btn color="primary" label="Add Split" @click="addSplitTransaction" />
                <q-btn
                  color="positive"
                  type="submit"
                  label="Save Splits"
                  :disable="remainingSplitAmount !== 0 || props.matching"
                  :loading="props.matching"
                  class="q-ml-sm"
                />
              </q-form>

              <!-- Potential Matches -->
              <div v-if="!showSplitForm" class="q-mb-md">
                <div class="row">
                  <div class="col-12 col-md-4 q-pa-sm">
                    <q-select
                      v-model="potentialMatchesSortField"
                      :options="potentialMatchesSortFields"
                      label="Sort By"
                      outlined
                      dense
                      option-label="text"
                      option-value="value"
                      @update:model-value="sortPotentialMatches"
                    />
                  </div>
                  <div class="col-12 col-md-4 q-pa-sm">
                    <q-btn
                      :color="potentialMatchesSortDirection === 'asc' ? 'primary' : 'secondary'"
                      :label="potentialMatchesSortDirection === 'asc' ? 'Ascending' : 'Descending'"
                      @click="togglePotentialMatchesSortDirection"
                    />
                  </div>
                </div>
              </div>

              <q-table
                v-if="potentialMatches.length > 0 && !showSplitForm"
                :columns="budgetTransactionHeaders"
                :rows="sortedPotentialMatches"
                v-model:selected="selectedBudgetTransactionForMatch"
                selection="single"
                :pagination="{ rowsPerPage: 5 }"
              >
                <template v-slot:body-cell-amount="{ row }">
                  ${{ toDollars(toCents(row.amount)) }}
                </template>
                <template v-slot:body-cell-type="{ row }">
                  {{ row.isIncome ? 'Income' : 'Expense' }}
                </template>
                <template v-slot:body-cell-actions="{ row }">
                  <q-btn
                    color="primary"
                    label="Match"
                    :disable="!selectedBudgetTransactionForMatch.length || props.matching"
                    :loading="props.matching"
                    @click="matchBankTransaction(row)"
                  />
                </template>
              </q-table>
              <div v-else-if="!showSplitForm" class="q-mt-md">
                <q-banner class="bg-info text-white q-mb-md">
                  No potential matches found. Adjust the search criteria or add a new transaction.
                </q-banner>
                <q-btn
                  color="primary"
                  label="Add New Transaction"
                  :disable="props.matching"
                  @click="addNewTransaction"
                />
              </div>
            </div>
            <div v-else class="q-mt-md">
              <q-banner class="bg-positive text-white">
                All bank transactions have been matched or ignored.
              </q-banner>
            </div>
          </q-tab-panel>
        </q-tab-panels>
      </q-card-section>
      <q-card-actions align="right">
        <q-btn
          v-if="remainingImportedTransactions.length > 0"
          color="warning"
          label="Ignore"
          :disable="props.matching"
          @click="ignoreBankTransaction"
        />
        <q-btn
          v-if="remainingImportedTransactions.length > 0"
          color="secondary"
          label="Skip"
          :disable="props.matching"
          @click="skipBankTransaction"
        />
        <q-space />
        <q-btn
          color="negative"
          label="Close"
          :disable="props.matching"
          @click="closeDialog(false)"
        />
      </q-card-actions>
    </q-card>

    <!-- Add TransactionDialog -->
    <TransactionDialog
      v-if="showTransactionDialog"
      :show-dialog="showTransactionDialog"
      :initial-transaction="newTransaction"
      :edit-mode="false"
      :loading="props.matching"
      :category-options="props.categoryOptions"
      :budget-id="newTransactionBudgetId"
      :user-id="props.userId"
      @update:showDialog="showTransactionDialog = $event"
      @save="handleTransactionAdded"
      @cancel="showTransactionDialog = false"
    />

    <!-- Add Snackbar -->
    <q-notification v-model="snackbar" :color="snackbarColor" position="top" :timeout="timeout">
      {{ snackbarText }}
      <template v-slot:actions>
        <q-btn flat label="Close" @click="snackbar = false" />
      </template>
    </q-notification>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted, nextTick } from 'vue';
import type { Transaction, ImportedTransaction, Budget } from '../types';
import {
  toDollars,
  toCents,
  toBudgetMonth,
  adjustTransactionDate,
  todayISO,
} from '../utils/helpers';
import { dataAccess } from '../dataAccess';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { auth } from '../firebase/index';
import TransactionDialog from './TransactionDialog.vue';
import { QForm } from 'quasar';
import { v4 as uuidv4 } from 'uuid';

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();

const props = defineProps<{
  showDialog: boolean;
  remainingImportedTransactions: ImportedTransaction[];
  selectedBankTransaction: ImportedTransaction | null;
  transactions: Transaction[];
  budgetId: string;
  matching: boolean;
  categoryOptions: string[];
  userId: string;
}>();

const emit = defineEmits<{
  (e: 'update:showDialog', value: boolean): void;
  (e: 'add-new-transaction', importedTx: ImportedTransaction): void;
  (e: 'transactions-updated'): void;
  (e: 'update:matching', value: boolean): void;
}>();

// Local reactive state
const isReady = ref(false);
const smartMatches = ref<
  Array<{
    importedTransaction: ImportedTransaction;
    budgetTransaction: Transaction;
    budgetId: string;
    bankAmount: number;
    bankType: string;
  }>
>([]);
const remainingImportedTransactions = ref<ImportedTransaction[]>(
  props.remainingImportedTransactions || [],
);
const selectedBankTransaction = ref<ImportedTransaction | null>(
  props.selectedBankTransaction || null,
);

// Snackbar state
const snackbar = ref(false);
const snackbarText = ref('');
const snackbarColor = ref('success');
const timeout = ref(3000);

// Local state for Smart Matches sorting
const activeTab = ref('smart-matches');
const selectedSmartMatchIds = ref<string[]>([]);
const smartMatchesSortField = ref<string>('postedDate');
const smartMatchesSortDirection = ref<'asc' | 'desc'>('asc');
const smartMatchesSortFields = [
  { text: 'Bank Date', value: 'postedDate' },
  { text: 'Merchant', value: 'merchant' },
  { text: 'Amount', value: 'bankAmount' },
];
const smartMatchDateRange = ref<string>('7');

// Local state for Remaining Transactions
const currentBankTransactionIndex = ref<number>(0);
const searchAmount = ref<string>('');
const searchMerchant = ref<string>('');
const searchDateRange = ref<string>('7');
const potentialMatches = ref<Transaction[]>([]);
const selectedBudgetTransactionForMatch = ref<Transaction[]>([]);
const potentialMatchesSortField = ref<string>('date');
const potentialMatchesSortDirection = ref<'asc' | 'desc'>('asc');
const potentialMatchesSortFields = [
  { text: 'Date', value: 'date' },
  { text: 'Merchant', value: 'merchant' },
  { text: 'Amount', value: 'amount' },
];

// Split transaction state
const showSplitForm = ref(false);
const transactionSplits = ref<Array<{ entityId: string; category: string; amount: number }>>([
  { entityId: '', category: '', amount: 0 },
]);
const splitForm = ref<InstanceType<typeof QForm> | null>(null);

const showTransactionDialog = ref(false);
const newTransaction = ref<Transaction>({
  id: '',
  budgetMonth: '',
  date: '',
  merchant: '',
  categories: [{ category: '', amount: 0 }],
  amount: 0,
  notes: '',
  recurring: false,
  recurringInterval: 'Monthly',
  userId: '',
  isIncome: false,
} as Transaction);
const newTransactionBudgetId = ref<string>(''); // Track budgetId for TransactionDialog

const isMobile = computed(() => window.innerWidth < 960);

const entityOptions = computed(() => {
  return (familyStore.family?.entities || []).map((entity) => ({
    id: entity.id,
    name: entity.name,
  }));
});

const remainingSplitAmount = computed(() => {
  if (!selectedBankTransaction.value) return 0;
  const totalAmount =
    selectedBankTransaction.value.debitAmount || selectedBankTransaction.value.creditAmount || 0;
  const allocated = transactionSplits.value.reduce((sum, split) => sum + split.amount, 0);
  return Math.round((totalAmount - allocated) * 100) / 100;
});

const smartMatchHeaders = ref([
  {
    name: 'importedTransaction.postedDate',
    label: 'Bank Date',
    field: 'importedTransaction.postedDate',
  },
  { name: 'bankAmount', label: 'Bank Amount', field: 'bankAmount' },
  { name: 'bankType', label: 'Bank Type', field: 'bankType' },
  { name: 'importedTransaction.payee', label: 'Payee', field: 'importedTransaction.payee' },
  { name: 'budgetTransaction.merchant', label: 'Merchant', field: 'budgetTransaction.merchant' },
  { name: 'budgetTransaction.date', label: 'Budget Date', field: 'budgetTransaction.date' },
  { name: 'budgetAmount', label: 'Budget Amount', field: 'budgetAmount' },
  { name: 'budgetType', label: 'Budget Type', field: 'budgetType' },
  { name: 'actions', label: 'Actions', field: 'actions' },
]);

const budgetTransactionHeaders = ref([
  { name: 'date', label: 'Date', field: 'date' },
  { name: 'merchant', label: 'Merchant', field: 'merchant' },
  { name: 'amount', label: 'Amount', field: 'amount' },
  { name: 'type', label: 'Type', field: 'type' },
  { name: 'categories', label: 'Categories', field: 'categories' },
  { name: 'actions', label: 'Actions', field: 'actions' },
]);

// Computed properties
const sortedSmartMatches = computed(() => {
  const items = [...smartMatches.value];
  const field = smartMatchesSortField.value;
  const direction = smartMatchesSortDirection.value;

  return items.sort((a, b) => {
    let valueA: number | string = 0;
    let valueB: number | string = 0;

    if (field === 'postedDate') {
      valueA = new Date(a.importedTransaction.postedDate).getTime();
      valueB = new Date(b.importedTransaction.postedDate).getTime();
    } else if (field === 'merchant') {
      valueA = a.budgetTransaction.merchant.toLowerCase();
      valueB = b.budgetTransaction.merchant.toLowerCase();
    } else if (field === 'bankAmount') {
      valueA = a.bankAmount;
      valueB = b.bankAmount;
    }

    if (valueA < valueB) return direction === 'asc' ? -1 : 1;
    if (valueA > valueB) return direction === 'asc' ? 1 : -1;
    return 0;
  });
});

const sortedPotentialMatches = computed(() => {
  const items = [...potentialMatches.value];
  const field = potentialMatchesSortField.value;
  const direction = potentialMatchesSortDirection.value;

  return items.sort((a, b) => {
    let valueA: number | string = 0;
    let valueB: number | string = 0;

    if (field === 'date') {
      valueA = new Date(a.date).getTime();
      valueB = new Date(b.date).getTime();
    } else if (field === 'merchant') {
      valueA = a.merchant.toLowerCase();
      valueB = b.merchant.toLowerCase();
    } else if (field === 'amount') {
      valueA = a.amount;
      valueB = b.amount;
    }

    if (valueA < valueB) return direction === 'asc' ? -1 : 1;
    if (valueA > valueB) return direction === 'asc' ? 1 : -1;
    return 0;
  });
});

// Watchers and Initialization
onMounted(async () => {
  await initializeState();
});

watch(
  () => props.showDialog,
  async (newVal) => {
    if (newVal) {
      await initializeState();
    }
  },
);

watch(
  () => smartMatchDateRange.value,
  () => {
    computeSmartMatchesLocal();
  },
);

async function initializeState() {
  remainingImportedTransactions.value = Array.isArray(props.remainingImportedTransactions)
    ? [...props.remainingImportedTransactions]
    : [];
  selectedBankTransaction.value =
    props.selectedBankTransaction || remainingImportedTransactions.value[0] || null;

  smartMatches.value = [];
  smartMatchDateRange.value = '7';
  computeSmartMatchesLocal();

  resetState(false);
  if (selectedBankTransaction.value) searchBudgetTransactions();
  await nextTick();
  isReady.value = true;
}

watch(
  () => props.selectedBankTransaction,
  (newVal) => {
    if (newVal) {
      selectedBankTransaction.value = newVal;
      searchAmount.value = newVal.debitAmount
        ? newVal.debitAmount.toString()
        : newVal.creditAmount?.toString() || '0';
      searchMerchant.value = '';
      searchDateRange.value = '7';
      transactionSplits.value = [
        { entityId: familyStore.selectedEntityId || '', category: '', amount: 0 },
      ]; // Reset splits
      showSplitForm.value = false;
      searchBudgetTransactions();
    }
  },
);

watch(
  () => remainingImportedTransactions.value,
  (newVal) => {
    if (newVal && newVal.length > 0) {
      currentBankTransactionIndex.value = Math.min(
        currentBankTransactionIndex.value,
        newVal.length - 1,
      );
      if (
        !selectedBankTransaction.value ||
        !newVal.some((tx) => tx.id === selectedBankTransaction.value?.id)
      ) {
        currentBankTransactionIndex.value = 0;
        selectedBankTransaction.value = newVal[0];
        searchBudgetTransactions();
      }
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }
  },
  { deep: true },
);

// Methods
function closeDialog(value: boolean) {
  emit('update:showDialog', value);
  if (!value) {
    emit('transactions-updated');
    isReady.value = false;
  }
}

async function confirmSmartMatches() {
  if (selectedSmartMatchIds.value.length === 0) {
    showSnackbar('No smart matches selected to confirm', 'error');
    return;
  }

  emit('update:matching', true);
  try {
    const user = auth.currentUser;
    if (!user) throw new Error('User not authenticated');

    const matchesToConfirm = smartMatches.value.filter((match) =>
      selectedSmartMatchIds.value.includes(match.importedTransaction.id),
    );

    const matchesByBudget: {
      [budgetId: string]: Array<{
        budgetTransactionId: string;
        importedTransactionId: string;
        match: boolean;
        ignore: boolean;
      }>;
    } = {};
    matchesToConfirm.forEach((match) => {
      const budgetId = match.budgetId;
      const importedTxId = match.importedTransaction.id;
      const budgetTxId = match.budgetTransaction.id;

      if (!budgetTxId)
        throw new Error(`Budget transaction ID is missing for match: ${JSON.stringify(match)}`);

      if (!matchesByBudget[budgetId]) matchesByBudget[budgetId] = [];
      matchesByBudget[budgetId].push({
        budgetTransactionId: budgetTxId,
        importedTransactionId: importedTxId,
        match: true,
        ignore: false,
      });
    });

    for (const budgetId in matchesByBudget) {
      const budget = budgetStore.getBudget(budgetId);
      if (!budget) throw new Error(`Budget ${budgetId} not found`);

      const reconcileData = {
        budgetId,
        reconciliations: matchesByBudget[budgetId],
      };

      await dataAccess.batchReconcileTransactions(budgetId, budget, reconcileData);
      const updatedBudget = await dataAccess.getBudget(budgetId);
      if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
    }

    showSnackbar(`${matchesToConfirm.length} smart matches confirmed successfully`);
    emit('transactions-updated');
    computeSmartMatchesLocal(matchesToConfirm);
    updateRemainingTransactions();
  } catch (error: any) {
    console.error('Error confirming smart matches:', error);
    showSnackbar(`Error confirming smart matches: ${error.message}`, 'error');
  } finally {
    emit('update:matching', false);
  }
}

async function matchBankTransaction(budgetTransaction: Transaction) {
  if (!selectedBankTransaction.value || !budgetTransaction) {
    showSnackbar('Please select a budget transaction to match', 'error');
    return;
  }

  emit('update:matching', true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error('User not authenticated');

    const updatedTransaction: Transaction = {
      ...budgetTransaction,
      accountSource: importedTx.accountSource || '',
      accountNumber: importedTx.accountNumber || '',
      postedDate: importedTx.postedDate || '',
      importedMerchant: importedTx.payee || '',
      status: 'C',
      id: budgetTransaction.id,
      userId: budgetTransaction.userId || user.uid,
      budgetMonth:
        budgetTransaction.budgetMonth || toBudgetMonth(importedTx.postedDate || todayISO()),
      date: budgetTransaction.date || importedTx.postedDate || todayISO(),
      merchant: budgetTransaction.merchant || importedTx.payee || '',
      categories: budgetTransaction.categories || [{ category: '', amount: 0 }],
      amount: budgetTransaction.amount || importedTx.debitAmount || importedTx.creditAmount || 0,
      notes: budgetTransaction.notes || '',
      recurring: budgetTransaction.recurring || false,
      recurringInterval: budgetTransaction.recurringInterval || 'Monthly',
      isIncome: budgetTransaction.isIncome || !!importedTx.creditAmount,
      entityId: budgetTransaction.entityId || familyStore.selectedEntityId || '',
    };

    const targetBudgetIdToUse =
      budgetTransaction.budgetId ||
      `${user.uid}_${updatedTransaction.entityId}_${updatedTransaction.budgetMonth}`;
    let budget = budgetStore.getBudget(targetBudgetIdToUse);
    if (!budget) {
      budget = await createBudgetForMonth(
        updatedTransaction.budgetMonth!,
        familyStore.family?.id || '',
        user.uid,
        updatedTransaction.entityId,
      );
    }

    await dataAccess.saveTransaction(budget, updatedTransaction, false);

    const parts = importedTx.id.split('-');
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join('-');
    await dataAccess.updateImportedTransaction(docId, importedTx.id, true);

    showSnackbar('Transaction matched successfully');
    emit('transactions-updated');
    updateRemainingTransactions();
    skipBankTransaction();
  } catch (error: any) {
    console.log(error);
    showSnackbar(`Error matching transaction: ${error.message}`, 'error');
  } finally {
    emit('update:matching', false);
  }
}

async function ignoreBankTransaction() {
  if (!selectedBankTransaction.value) {
    showSnackbar('No bank transaction selected to ignore', 'error');
    return;
  }

  emit('update:matching', true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error('User not authenticated');

    const parts = importedTx.id.split('-');
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join('-');
    await dataAccess.updateImportedTransaction(docId, importedTx.id, undefined, true);

    showSnackbar('Bank transaction ignored');
    updateRemainingTransactions();
    skipBankTransaction();
  } catch (error: any) {
    showSnackbar(`Error ignoring transaction: ${error.message}`, 'error');
  } finally {
    emit('update:matching', false);
  }
}

function skipBankTransaction() {
  if (currentBankTransactionIndex.value + 1 < remainingImportedTransactions.value.length) {
    currentBankTransactionIndex.value++;
    selectedBankTransaction.value =
      remainingImportedTransactions.value[currentBankTransactionIndex.value];
    searchBudgetTransactions();
  } else {
    if (smartMatches.value.length === 0) {
      closeDialog(false);
      showSnackbar('All bank transactions have been processed', 'success');
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }
  }
}

async function saveSplitTransaction() {
  if (!selectedBankTransaction.value || remainingSplitAmount.value !== 0) {
    showSnackbar('Invalid split amounts or no bank transaction selected', 'error');
    return;
  }

  if (!splitForm.value) return;

  const valid = await splitForm.value.validate();
  if (!valid) {
    showSnackbar('Please fill in all required fields', 'error');
    return;
  }

  emit('update:matching', true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error('User not authenticated');

    const family = await familyStore.getFamily();
    if (!family) throw new Error('No family found');

    // Group splits by budget (based on entityId and budgetMonth)
    const transactionsByBudget: { [budgetId: string]: Transaction[] } = {};
    for (const split of transactionSplits.value) {
      const budgetMonth = toBudgetMonth(importedTx.postedDate || todayISO());
      const budgetId = `${user.uid}_${split.entityId}_${budgetMonth}`;
      const transaction: Transaction = {
        id: uuidv4(),
        budgetMonth,
        date: importedTx.postedDate || todayISO(),
        merchant: importedTx.payee || '',
        categories: [{ category: split.category, amount: split.amount }],
        amount: split.amount,
        notes: '',
        recurring: false,
        recurringInterval: 'Monthly',
        userId: props.userId,
        isIncome: !!importedTx.creditAmount,
        postedDate: importedTx.postedDate,
        importedMerchant: importedTx.payee,
        accountSource: importedTx.accountSource,
        accountNumber: importedTx.accountNumber,
        checkNumber: importedTx.checkNumber,
        status: 'C',
        entityId: split.entityId,
        taxMetadata: [],
      };

      if (!transactionsByBudget[budgetId]) transactionsByBudget[budgetId] = [];
      transactionsByBudget[budgetId].push(transaction);
    }

    // Save transactions to their respective budgets
    for (const budgetId in transactionsByBudget) {
      let budget = budgetStore.getBudget(budgetId);
      if (!budget) {
        const [, entityId, budgetMonth] = budgetId.split('_');
        budget = await createBudgetForMonth(budgetMonth, family.id, user.uid, entityId);
      }

      await dataAccess.batchSaveTransactions(budgetId, budget, transactionsByBudget[budgetId]);
      const updatedBudget = await dataAccess.getBudget(budgetId);
      if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
    }

    const parts = importedTx.id.split('-');
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join('-');
    await dataAccess.updateImportedTransaction(docId, importedTx.id, true);

    remainingImportedTransactions.value = remainingImportedTransactions.value.filter(
      (tx) => tx.id !== importedTx.id,
    );
    if (remainingImportedTransactions.value.length > 0) {
      currentBankTransactionIndex.value = Math.min(
        currentBankTransactionIndex.value,
        remainingImportedTransactions.value.length - 1,
      );
      selectedBankTransaction.value =
        remainingImportedTransactions.value[currentBankTransactionIndex.value];
      searchBudgetTransactions();
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }

    showSnackbar('Split transaction saved successfully');
    emit('transactions-updated');
    resetSplitForm();
  } catch (error: any) {
    showSnackbar(`Error saving split transaction: ${error.message}`, 'error');
  } finally {
    emit('update:matching', false);
  }
}

async function handleTransactionAdded(savedTransaction: Transaction) {
  if (!selectedBankTransaction.value) return;

  emit('update:matching', true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error('User not authenticated');

    const family = await familyStore.getFamily();
    if (!family) throw new Error('No family found');

    const targetBudgetId = `${user.uid}_${savedTransaction.entityId}_${savedTransaction.budgetMonth}`;
    let budget = budgetStore.getBudget(targetBudgetId);
    if (!budget) {
      budget = await createBudgetForMonth(
        savedTransaction.budgetMonth || "",
        family.id,
        user.uid,
        savedTransaction.entityId,
      );
    }

    budgetStore.updateBudget(targetBudgetId, {
      ...budget,
      transactions: [...budget.transactions, savedTransaction],
    });

    const parts = importedTx.id.split('-');
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join('-');
    await dataAccess.updateImportedTransaction(docId, importedTx.id, true);

    remainingImportedTransactions.value = remainingImportedTransactions.value.filter(
      (tx) => tx.id !== importedTx.id,
    );
    if (remainingImportedTransactions.value.length > 0) {
      currentBankTransactionIndex.value = Math.min(
        currentBankTransactionIndex.value,
        remainingImportedTransactions.value.length - 1,
      );
      selectedBankTransaction.value =
        remainingImportedTransactions.value[currentBankTransactionIndex.value];
      searchBudgetTransactions();
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }

    showSnackbar('Transaction added and matched successfully');
    emit('transactions-updated');
  } catch (error: any) {
    showSnackbar(`Error adding transaction: ${error.message}`, 'error');
  } finally {
    emit('update:matching', false);
    showTransactionDialog.value = false;
  }
}

async function addNewTransaction() {
  if (!selectedBankTransaction.value) {
    showSnackbar('No bank transaction selected to add', 'error');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before adding a transaction', 'error');
    return;
  }

  const postedDate = selectedBankTransaction.value.postedDate || todayISO();
  let budgetMonth = toBudgetMonth(postedDate);

  if (!budgetStore.availableBudgetMonths.includes(budgetMonth)) {
    budgetMonth =
      budgetStore.availableBudgetMonths[budgetStore.availableBudgetMonths.length - 1] ||
      budgetMonth;
    console.log(
      `Budget month ${toBudgetMonth(postedDate)} not found, falling back to ${budgetMonth}`,
    );
  }

  newTransaction.value = {
    id: '',
    budgetMonth,
    date: selectedBankTransaction.value.postedDate || todayISO(),
    merchant: selectedBankTransaction.value.payee || '',
    categories: [
      {
        category: '',
        amount:
          selectedBankTransaction.value.debitAmount ||
          selectedBankTransaction.value.creditAmount ||
          0,
      },
    ],
    amount:
      selectedBankTransaction.value.debitAmount || selectedBankTransaction.value.creditAmount || 0,
    notes: '',
    recurring: false,
    recurringInterval: 'Monthly',
    userId: props.userId,
    isIncome: !!selectedBankTransaction.value.creditAmount,
    postedDate: selectedBankTransaction.value.postedDate,
    importedMerchant: selectedBankTransaction.value.payee,
    accountSource: selectedBankTransaction.value.accountSource,
    accountNumber: selectedBankTransaction.value.accountNumber,
    checkNumber: selectedBankTransaction.value.checkNumber,
    status: 'C',
    entityId: familyStore.selectedEntityId,
  };
  newTransactionBudgetId.value = `${props.userId}_${familyStore.selectedEntityId}_${budgetMonth}`;
  showTransactionDialog.value = true;
}

function toggleSplitTransaction() {
  showSplitForm.value = !showSplitForm.value;
  if (showSplitForm.value) {
    transactionSplits.value = [
      { entityId: familyStore.selectedEntityId || '', category: '', amount: 0 },
    ];
  } else {
    resetSplitForm();
  }
}

function addSplitTransaction() {
  transactionSplits.value.push({
    entityId: familyStore.selectedEntityId || '',
    category: '',
    amount: 0,
  });
}

function removeSplit(index: number) {
  if (transactionSplits.value.length > 1) {
    transactionSplits.value.splice(index, 1);
  }
}

function resetSplitForm() {
  transactionSplits.value = [
    { entityId: familyStore.selectedEntityId || '', category: '', amount: 0 },
  ];
  showSplitForm.value = false;
}

const isBudgetTxMatchedMultiple = (budgetTxId: string) => {
  const matchesForBudgetTx = smartMatches.value.filter(
    (match) => match.budgetTransaction.id === budgetTxId,
  );
  return matchesForBudgetTx.length > 1;
};

function sortSmartMatches() {
  // Sorting handled by computed property
}

function toggleSmartMatchesSortDirection() {
  smartMatchesSortDirection.value = smartMatchesSortDirection.value === 'asc' ? 'desc' : 'asc';
}

function sortPotentialMatches() {
  // Sorting handled by computed property
}

function togglePotentialMatchesSortDirection() {
  potentialMatchesSortDirection.value =
    potentialMatchesSortDirection.value === 'asc' ? 'desc' : 'asc';
}

function searchBudgetTransactions() {
  if (!selectedBankTransaction.value) return;

  const bankTx = selectedBankTransaction.value;
  const amount = parseFloat(searchAmount.value) || bankTx.debitAmount || bankTx.creditAmount || 0;
  const merchant = searchMerchant.value.toLowerCase();
  const dateRangeDays = parseInt(searchDateRange.value) || 7;

  const bankDate = new Date(bankTx.postedDate);
  const startDate = new Date(bankDate);
  startDate.setDate(bankDate.getDate() - dateRangeDays);
  const endDate = new Date(bankDate);
  endDate.setDate(bankDate.getDate() + dateRangeDays);

  potentialMatches.value = props.transactions.filter((tx) => {
    const txDate = new Date(tx.date);
    const txAmount = tx.amount;
    const txMerchant = tx.merchant.toLowerCase();

    const dateMatch = txDate >= startDate && txDate <= endDate;
    const amountMatch = Math.abs(txAmount - amount) < 0.01;
    const merchantMatch = merchant ? txMerchant.includes(merchant) : true;

    return (
      !tx.deleted && dateMatch && amountMatch && merchantMatch && (!tx.status || tx.status === 'U')
    );
  });
}

function computeSmartMatchesLocal(confirmedMatches: typeof smartMatches.value = []) {
  console.log('Computing smart matches...');
  const confirmedIds = new Set(confirmedMatches.map((m) => m.importedTransaction.id));
  const newSmartMatches = smartMatches.value.filter(
    (match) => !confirmedIds.has(match.importedTransaction.id),
  );

  const dateRangeDays = parseInt(smartMatchDateRange.value) || 7;
  const unmatchedImported = remainingImportedTransactions.value.filter(
    (tx) =>
      !confirmedIds.has(tx.id) && !newSmartMatches.some((m) => m.importedTransaction.id === tx.id),
  );

  const potentialMatches: Array<{
    importedTx: ImportedTransaction;
    budgetTx: Transaction;
    budgetId: string;
    bankAmount: number;
    bankType: string;
    merchantMatch: boolean;
    dateExact: boolean;
  }> = [];

  unmatchedImported.forEach((importedTx) => {
    const bankAmount = importedTx.debitAmount || importedTx.creditAmount || 0;
    const bankDate = new Date(importedTx.postedDate);
    const bankDateStr = bankDate.toISOString().split('T')[0];
    const normalizedBankDate = new Date(bankDateStr);

    const startDate = new Date(normalizedBankDate);
    startDate.setDate(normalizedBankDate.getDate() - dateRangeDays);
    const endDate = new Date(normalizedBankDate);
    endDate.setDate(normalizedBankDate.getDate() + dateRangeDays);

    props.transactions.forEach((tx) => {
      const txDate = new Date(tx.date);
      const txDateStr = txDate.toISOString().split('T')[0];
      const normalizedTxDate = new Date(txDateStr);
      const txAmount = tx.amount;
      const typeMatch = tx.isIncome === !!importedTx.creditAmount;
      if (
        normalizedTxDate >= startDate &&
        normalizedTxDate <= endDate &&
        Math.abs(txAmount - bankAmount) < 0.01 &&
        (!tx.status || tx.status === 'U') &&
        !tx.deleted &&
        typeMatch
      ) {
        potentialMatches.push({
          importedTx,
          budgetTx: tx,
          budgetId:
            tx.budgetId || `${props.userId}_${tx.entityId}_${toBudgetMonth(importedTx.postedDate)}`,
          bankAmount,
          bankType: importedTx.debitAmount ? 'Debit' : 'Credit',
          merchantMatch:
            !!importedTx.payee &&
            importedTx.payee.toLowerCase().includes(tx.merchant.toLowerCase()),
          dateExact: normalizedTxDate.getTime() === normalizedBankDate.getTime(),
        });
      }
    });
  });

  const usedBudgetTxIds = new Set<string>();
  const usedBankTxIds = new Set<string>();
  const smartMatchesToAdd: typeof potentialMatches = [];

  const matchesByBank: Record<string, typeof potentialMatches> = {};
  potentialMatches.forEach((m) => {
    if (!matchesByBank[m.importedTx.id]) matchesByBank[m.importedTx.id] = [];
    matchesByBank[m.importedTx.id].push(m);
  });

  Object.values(matchesByBank).forEach((cands) => {
    const available = cands.filter((c) => !usedBudgetTxIds.has(c.budgetTx.id));
    if (available.length === 0) return;
    let chosen: (typeof cands)[0] | null = null;

    if (available.length === 1) {
      chosen = available[0];
    } else {
      const merchantMatches = available.filter((c) => c.merchantMatch);
      if (merchantMatches.length === 1) {
        chosen = merchantMatches[0];
      } else {
        const dateMatches = available.filter((c) => c.dateExact);
        if (dateMatches.length === 1) {
          chosen = dateMatches[0];
        }
      }
    }

    if (
      chosen &&
      !usedBudgetTxIds.has(chosen.budgetTx.id) &&
      !usedBankTxIds.has(chosen.importedTx.id)
    ) {
      smartMatchesToAdd.push(chosen);
      usedBudgetTxIds.add(chosen.budgetTx.id);
      usedBankTxIds.add(chosen.importedTx.id);
    }
  });

  smartMatchesToAdd.forEach((match) => {
    newSmartMatches.push({
      importedTransaction: match.importedTx,
      budgetTransaction: { ...match.budgetTx, id: match.budgetTx.id },
      budgetId: match.budgetId,
      bankAmount: match.bankAmount,
      bankType: match.bankType,
    });
  });

  smartMatches.value = newSmartMatches;
  selectedSmartMatchIds.value = [];

  const smartMatchImportedIds = new Set(smartMatches.value.map((m) => m.importedTransaction.id));
  remainingImportedTransactions.value = unmatchedImported.filter(
    (tx) => !smartMatchImportedIds.has(tx.id),
  );
}

function updateRemainingTransactions() {
  const matchedIds = new Set(
    smartMatches.value
      .filter((match) => selectedSmartMatchIds.value.includes(match.importedTransaction.id))
      .map((match) => match.importedTransaction.id),
  );
  remainingImportedTransactions.value = remainingImportedTransactions.value.filter(
    (importedTx) => !matchedIds.has(importedTx.id),
  );

  if (remainingImportedTransactions.value.length > 0) {
    currentBankTransactionIndex.value = Math.min(
      currentBankTransactionIndex.value,
      remainingImportedTransactions.value.length - 1,
    );
    selectedBankTransaction.value =
      remainingImportedTransactions.value[currentBankTransactionIndex.value];
    selectedBudgetTransactionForMatch.value = [];
    searchBudgetTransactions();
  } else {
    currentBankTransactionIndex.value = -1;
    selectedBankTransaction.value = null;
  }
}

function resetState(computeMatches = true) {
  selectedSmartMatchIds.value = [];
  currentBankTransactionIndex.value = remainingImportedTransactions.value.length > 0 ? 0 : -1;
  potentialMatches.value = [];
  selectedBudgetTransactionForMatch.value = [];
  smartMatchesSortField.value = 'postedDate';
  smartMatchesSortDirection.value = 'asc';
  potentialMatchesSortField.value = 'date';
  potentialMatchesSortDirection.value = 'asc';
  if (computeMatches) computeSmartMatchesLocal();
  else console.log('Skipping computeSmartMatchesLocal on initial reset');
}

function showSnackbar(text: string, color = 'success') {
  snackbarText.value = text;
  snackbarColor.value = color;
  timeout.value = 3000;
  snackbar.value = true;
}

// Helper function to create a budget if it doesn't exist
async function createBudgetForMonth(
  month: string,
  familyId: string,
  ownerUid: string,
  entityId: string,
): Promise<Budget> {
  const budgetId = `${ownerUid}_${entityId}_${month}`; // New ID format: uid_entityId_budgetMonth
  const existingBudget = await dataAccess.getBudget(budgetId);
  if (existingBudget) {
    return existingBudget;
  }

  // Find the most recent previous budget, or the earliest future budget if none exists, for the same entity
  const availableBudgets = Array.from(budgetStore.budgets.values()).sort((a, b) =>
    a.month.localeCompare(b.month),
  );
  let sourceBudget: Budget | undefined;

  // Look for previous budget (preferred)
  const previousBudgets = availableBudgets.filter(
    (b) => b.month < month && b.entityId === entityId,
  );
  if (previousBudgets.length > 0) {
    sourceBudget = previousBudgets[previousBudgets.length - 1]; // Most recent previous
  } else {
    // Fall back to earliest future budget
    const futureBudgets = availableBudgets.filter(
      (b) => b.month > month && b.entityId === entityId,
    );
    if (futureBudgets.length > 0) {
      sourceBudget = futureBudgets[0]; // Earliest future
    }
  }

  if (!sourceBudget) {
    // Create a default budget if no source budget exists
    const defaultBudget: Budget = {
      familyId: familyId,
      entityId: entityId,
      month: month,
      incomeTarget: 0,
      categories: [],
      transactions: [],
      label: `Default Budget for ${month}`,
      merchants: [],
      budgetId: budgetId,
    };
    await dataAccess.saveBudget(budgetId, defaultBudget);
    budgetStore.updateBudget(budgetId, defaultBudget);
    return defaultBudget;
  }

  // Copy source budget
  const [newYear, newMonthNum] = month.split('-').map(Number);
  const [sourceYear, sourceMonthNum] = sourceBudget.month.split('-').map(Number);
  const isFutureMonth =
    newYear > sourceYear || (newYear === sourceYear && newMonthNum > sourceMonthNum);

  let newCarryover: Record<string, number> = {};
  if (isFutureMonth) {
    newCarryover = await dataAccess.calculateCarryOver(sourceBudget);
  }

  const newBudget: Budget = {
    familyId: familyId,
    entityId: entityId,
    month: month,
    incomeTarget: sourceBudget.incomeTarget,
    categories: sourceBudget.categories.map((cat) => ({
      ...cat,
      carryover: cat.isFund ? newCarryover[cat.name] || 0 : 0,
    })),
    label: '',
    merchants: sourceBudget.merchants || [],
    transactions: [],
    budgetId: budgetId,
  };

  // Copy recurring transactions
  const recurringTransactions: Transaction[] = [];
  if (sourceBudget.transactions) {
    const recurringGroups = sourceBudget.transactions.reduce(
      (groups, trx) => {
        if (!trx.deleted && trx.recurring) {
          const key = `${trx.merchant}-${trx.amount}-${trx.recurringInterval}-${trx.userId}-${trx.isIncome}`;
          if (!groups[key]) {
            groups[key] = [];
          }
          groups[key].push(trx);
        }
        return groups;
      },
      {} as Record<string, Transaction[]>,
    );

    Object.values(recurringGroups).forEach((group) => {
      const firstInstance = group.sort(
        (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
      )[0];
      if (firstInstance.recurringInterval === 'Monthly') {
        const newDate = adjustTransactionDate(firstInstance.date, month, 'Monthly');
        recurringTransactions.push({
          ...firstInstance,
          id: uuidv4(),
          date: newDate,
          budgetMonth: month,
          entityId: entityId,
        });
      }
      // Add support for other intervals (Daily, Weekly, etc.) if needed
    });
  }

  newBudget.transactions = recurringTransactions;
  await dataAccess.saveBudget(budgetId, newBudget);
  budgetStore.updateBudget(budgetId, newBudget);
  return newBudget;
}
</script>
