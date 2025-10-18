<!-- src/components/BankTransactionMatchingDialog.vue -->
<template>
  <div v-if="isReady">
    <q-card class="panel-card match-dialog">
      <q-card-section>
        <!-- Smart Matches Header -->
        <div class="row items-center q-gutter-md q-mb-md">
          <div class="col">
            <div class="row items-center q-gutter-sm">
              <h3 class="q-mb-none">Smart Matches ({{ smartMatchCountLabel }})</h3>
              <q-input v-model="filterStartDate" label="Start Date" type="date" dense class="q-ml-md" style="width: 150px" :max="filterEndDate || undefined" />
              <q-input v-model="filterEndDate" label="End Date" type="date" dense class="q-ml-sm" style="width: 150px" :min="filterStartDate || undefined" />
              <q-input v-model="smartMatchDateRange" label="Days" type="number" dense class="q-ml-md" style="width: 90px" :min="0" />
            </div>
            <p class="text-caption q-mt-xs q-mb-none">
              These imported transactions have exactly one potential match. Showing up to {{ MAX_SMART_MATCHES }} results.
            </p>
          </div>
          <div class="col-auto">
            <q-btn color="secondary" @click="findSmartMatches" :loading="findingSmartMatches">
              <q-icon start name="playlist_add_check" />
              Find Smart Matches
            </q-btn>
          </div>
        </div>

        <!-- Sort Controls -->
        <div class="row q-gutter-md items-center q-mb-md">
          <div class="col col-12 col-md-4">
            <q-select
              v-model="smartMatchesSortField"
              :options="smartMatchesSortFields"
              option-label="text"
              option-value="value"
              emit-value
              map-options
              label="Sort By"
              variant="outlined"
              density="compact"
              @update:model-value="sortSmartMatches"
            />
          </div>
          <div class="col col-12 col-md-4">
            <q-btn :color="smartMatchesSortDirection === 'asc' ? 'primary' : 'secondary'" @click="toggleSmartMatchesSortDirection">
              {{ smartMatchesSortDirection === 'asc' ? 'Ascending' : 'Descending' }}
            </q-btn>
          </div>
          <div class="col">
            <q-btn color="primary" @click="confirmSmartMatches" :disabled="selectedSmartMatchIds.length === 0 || props.matching" :loading="props.matching">
              Confirm Selected Matches ({{ selectedSmartMatchIds.length }})
            </q-btn>
          </div>
        </div>

        <q-banner v-if="!lookedForSmartMatches" type="info" class="q-mt-lg"> Click Find Smart Matches to look for transaction matches. </q-banner>

        <div v-else>
          <q-table
            v-if="smartMatches.length > 0"
            :columns="smartMatchColumns"
            :rows="visibleSmartMatches"
            :row-key="rowKey"
            :table-row-class-fn="smartMatchRowClass"
            selection="multiple"
            v-model:selected="selectedSmartMatchesInternal"
            class="q-mt-lg"
            hide-bottom
            :pagination="{ rowsPerPage: 0 }"
          >
            <template #body-cell-bankAmount="props">
              <q-td :props="props" class="text-right" style="vertical-align: middle"> ${{ toDollars(toCents(props.row.bankAmount)) }} </q-td>
            </template>
            <template #body-cell-budgetAmount="props">
              <q-td :props="props" class="text-right" style="vertical-align: middle"> ${{ toDollars(toCents(props.row.budgetAmount)) }} </q-td>
            </template>
            <template #body-cell-merchant="props">
              <q-td :props="props" style="max-width: 200px; vertical-align: middle">
                <div class="ellipsis">{{ props.row.merchant }}</div>
              </q-td>
            </template>
            <template #body-cell-actions="{ row }">
              <q-icon
                v-if="isBudgetTxMatchedMultiple(row.budgetTransaction.id)"
                color="warning"
                title="This budget transaction matches multiple bank transactions"
                name="warning"
              ></q-icon>
            </template>
          </q-table>

          <q-banner v-else type="info" class="q-mt-lg"> No smart matches found. Check Remaining Transactions for potential conflicts. </q-banner>

          <!-- Remaining Transactions -->
          <div class="row q-mt-lg" v-if="filteredImportedTransactions.length > 0">
            <div class="col">
              <h3>Remaining Transactions ({{ currentBankTransactionIndex + 1 }} of {{ filteredImportedTransactions.length }})</h3>
              <q-markup-table>
                <thead>
                  <tr>
                    <th>Posted Date</th>
                    <th>Payee</th>
                    <th>Amount</th>
                    <th>Type</th>
                    <th>Account Source</th>
                    <th>Account #</th>
                    <th>Check Number</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td>{{ getImportedTransactionDate(selectedBankTransaction) }}</td>
                    <td>{{ selectedBankTransaction?.payee }}</td>
                    <td>${{ toDollars(toCents(selectedBankTransaction?.debitAmount || selectedBankTransaction?.creditAmount || 0)) }}</td>
                    <td>
                      {{ selectedBankTransaction?.debitAmount ? 'Debit' : selectedBankTransaction?.creditAmount ? 'Credit' : 'N/A' }}
                    </td>
                    <td>{{ selectedBankTransaction?.accountSource }}</td>
                    <td>{{ selectedBankTransaction?.accountNumber }}</td>
                    <td>{{ selectedBankTransaction?.checkNumber || 'N/A' }}</td>
                  </tr>
                </tbody>
              </q-markup-table>

              <!-- Search Filters -->
              <div class="row q-mt-lg">
                <div class="col col-12 col-md-4">
                  <q-input v-model="searchAmount" label="Amount" type="number" variant="outlined" readonly></q-input>
                </div>
                <div class="col col-12 col-md-4">
                  <q-input v-model="searchMerchant" label="Merchant" variant="outlined" @input="searchBudgetTransactions"></q-input>
                </div>
                <div class="col col-12 col-md-4">
                  <q-input v-model="searchDateRange" label="Date Range (days)" type="number" variant="outlined" @input="searchBudgetTransactions"></q-input>
                </div>
              </div>

              <!-- Split Transaction Option -->
              <div class="row q-mt-lg">
                <div class="col">
                  <q-btn color="primary" @click="toggleSplitTransaction" :disabled="props.matching">
                    {{ showSplitForm ? 'Cancel Split' : 'Split Transaction' }}
                  </q-btn>
                </div>
              </div>

              <!-- Split Transaction Form -->
              <q-form v-if="showSplitForm" ref="splitForm" @submit.prevent="saveSplitTransaction" class="q-mt-lg">
                <div class="row align-center" v-for="(split, index) in transactionSplits" :key="index">
                  <div class="col col-12 col-md-3">
                    <q-select
                      v-model="split.entityId"
                      :options="entityOptions"
                      option-label="name"
                      option-value="id"
                      emit-value
                      map-options
                      label="Entity"
                      variant="outlined"
                      density="compact"
                      :rules="[(v: string) => !!v || 'Entity is required']"
                    ></q-select>
                  </div>
                  <div class="col col-12 col-md-3">
                    <q-select
                      v-model="split.category"
                      :options="props.categoryOptions"
                      label="Category"
                      variant="outlined"
                      density="compact"
                      :rules="[(v: string) => !!v || 'Category is required']"
                    ></q-select>
                  </div>

                  <q-table
                    v-if="potentialMatches.length > 0 && !showSplitForm"
                    :columns="budgetTransactionColumns"
                    :rows="sortedPotentialMatches"
                    :pagination="{ rowsPerPage: 5 }"
                    row-key="id"
                    :table-row-class-fn="potentialRowClass"
                    selection="single"
                    v-model:selected="selectedBudgetTransactionForMatch"
                  >
                    <template #body-cell-amount="slotProps">
                      <q-td :props="slotProps" class="text-right"> ${{ toDollars(toCents(slotProps.row.amount)) }} </q-td>
                    </template>
                    <template #body-cell-type="slotProps">
                      <q-td :props="slotProps">
                        {{ slotProps.row.isIncome ? 'Income' : 'Expense' }}
                      </q-td>
                    </template>
                    <template #body-cell-actions="slotProps">
                      <q-td :props="slotProps">
                        <q-btn
                          color="primary"
                          small
                          @click="matchBankTransaction(slotProps.row)"
                          :disabled="!selectedBudgetTransactionForMatch.length || props.matching"
                          :loading="props.matching"
                        >
                          Match
                        </q-btn>
                      </q-td>
                    </template>
                  </q-table>
                  <div v-else-if="!showSplitForm" class="q-mt-lg">
                    <q-banner type="info" class="q-mb-lg"> No potential matches found. Adjust the search criteria or add a new transaction. </q-banner>
                    <q-btn color="primary" @click="addNewTransaction" :disabled="props.matching"> Add New Transaction </q-btn>
                  </div>
                </div>
                <q-banner v-if="remainingSplitAmount !== 0" :type="remainingSplitAmount < 0 ? 'error' : 'warning'" class="q-mb-lg">
                  <div v-if="remainingSplitAmount > 0">Remaining ${{ toDollars(toCents(remainingSplitAmount)) }}</div>
                  <div v-else>Over allocated ${{ toDollars(toCents(Math.abs(remainingSplitAmount))) }}</div>
                </q-banner>
                <q-btn color="primary" @click="addSplitTransaction">Add Split</q-btn>
                <q-btn color="positive" type="submit" :disabled="remainingSplitAmount !== 0 || props.matching" :loading="props.matching" class="q-ml-sm">
                  Save Splits
                </q-btn>
              </q-form>

              <!-- Potential Matches -->
              <div class="row q-mb-lg" v-if="!showSplitForm">
                <div class="col col-12 col-md-4">
                  <q-select
                    v-model="potentialMatchesSortField"
                    :options="potentialMatchesSortFields"
                    option-label="text"
                    option-value="value"
                    emit-value
                    map-options
                    label="Sort By"
                    variant="outlined"
                    density="compact"
                    @update:model-value="sortPotentialMatches"
                  ></q-select>
                </div>
                <div class="col col-12 col-md-4">
                  <q-btn :color="potentialMatchesSortDirection === 'asc' ? 'primary' : 'secondary'" @click="togglePotentialMatchesSortDirection">
                    {{ potentialMatchesSortDirection === 'asc' ? 'Ascending' : 'Descending' }}
                  </q-btn>
                </div>
              </div>

              <q-table
                v-if="potentialMatches.length > 0 && !showSplitForm"
                :columns="budgetTransactionColumns"
                :rows="sortedPotentialMatches"
                :pagination="{ rowsPerPage: 5 }"
                row-key="id"
                :table-row-class-fn="potentialRowClass"
                selection="single"
                v-model:selected="selectedBudgetTransactionForMatch"
              >
                <template #body-cell-amount="{ row }"> ${{ toDollars(toCents(row.amount)) }} </template>
                <template #body-cell-type="{ row }">
                  {{ row.isIncome ? 'Income' : 'Expense' }}
                </template>
                <template #body-cell-actions="{ row }">
                  <q-btn
                    color="primary"
                    small
                    @click="matchBankTransaction(row)"
                    :disabled="!selectedBudgetTransactionForMatch.length || props.matching"
                    :loading="props.matching"
                  >
                    Match
                  </q-btn>
                </template>
              </q-table>
              <div v-else-if="!showSplitForm" class="q-mt-lg">
                <q-banner type="info" class="q-mb-lg"> No potential matches found. Adjust the search criteria or add a new transaction. </q-banner>
                <q-btn color="primary" @click="addNewTransaction" :disabled="props.matching"> Add New Transaction </q-btn>
              </div>
            </div>
          </div>
          <div class="row q-mt-sm" v-else>
            <div class="col">
              <q-banner type="positive"> All bank transactions have been matched or ignored. </q-banner>
            </div>
          </div>

          <div class="q-py-md row">
            <div class="col-auto q-pr-sm"><q-btn v-if="filteredImportedTransactions.length > 0" color="warning" @click="ignoreBankTransaction" :disabled="props.matching">Ignore</q-btn></div>
            <div class="col-auto q-px-sm"><q-btn v-if="filteredImportedTransactions.length > 0" color="secondary" @click="skipBankTransaction" :disabled="props.matching">Skip</q-btn></div>
          </div>
        </div>
      </q-card-section>
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

    <!-- Snackbar handled via $q.notify -->
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted, nextTick } from 'vue';
import type { Transaction, ImportedTransaction, Budget } from '../types';
import { useQuasar } from 'quasar';
import { toDollars, toCents, toBudgetMonth, todayISO, getImportedTransactionDate } from '../utils/helpers';
import { dataAccess } from '../dataAccess';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { auth } from '../firebase/init';
import TransactionDialog from './TransactionDialog.vue';
import { QForm } from 'quasar';
import { createBudgetForMonth } from '../utils/budget';
import { v4 as uuidv4 } from 'uuid';
import { splitImportedId } from '../utils/imported';

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();
const $q = useQuasar();

const props = defineProps<{
  remainingImportedTransactions: ImportedTransaction[];
  selectedBankTransaction: ImportedTransaction | null;
  transactions: Transaction[];
  budgetId: string;
  matching: boolean;
  categoryOptions: string[];
  userId: string;
}>();

const emit = defineEmits<{
  (e: 'add-new-transaction', importedTx: ImportedTransaction): void;
  (e: 'transactions-updated'): void;
  (e: 'update:matching', value: boolean): void;
}>();

// Local reactive state
const isReady = ref(false);
type SmartMatchRow = {
  importedTransaction: ImportedTransaction;
  budgetTransaction: Transaction;
  budgetId: string;
  bankAmount: number;
  bankType: string;
  bankDate: string;
  payee: string;
  merchant: string;
  budgetDate: string;
  budgetAmount: number;
  budgetType: string;
  approxMatch: boolean;
};

const smartMatches = ref<SmartMatchRow[]>([]);
const allRemainingImportedTransactions = ref<ImportedTransaction[]>(
  Array.isArray(props.remainingImportedTransactions) ? [...props.remainingImportedTransactions] : [],
);
const filteredImportedTransactions = ref<ImportedTransaction[]>([]);
const selectedBankTransaction = ref<ImportedTransaction | null>(props.selectedBankTransaction || null);

// Snackbar timeout state for notifications
const timeout = ref(3000);
const findingSmartMatches = ref(false);

// Local state for Smart Matches sorting
const selectedSmartMatchIds = ref<string[]>([]);
const smartMatchesSortField = ref<string>('merchant');
const smartMatchesSortDirection = ref<'asc' | 'desc'>('asc');
const smartMatchesSortFields = [
  { text: 'Bank Date', value: 'bankDate' },
  { text: 'Merchant', value: 'merchant' },
  { text: 'Amount', value: 'bankAmount' },
];
const smartMatchDateRange = ref<string>('3');
const filterEndDate = ref<string>(todayISO());
const filterStartDate = ref<string>(computeDefaultStartDate());
const parsedFilterStartDate = computed(() => (filterStartDate.value ? new Date(`${filterStartDate.value}T00:00:00`) : null));
const parsedFilterEndDate = computed(() => (filterEndDate.value ? new Date(`${filterEndDate.value}T23:59:59.999`) : null));
const MAX_SMART_MATCHES = 250;
const lookedForSmartMatches = ref(false);
const totalSmartMatches = ref(0);
const sortedSmartMatches = computed(() => {
  const items = [...smartMatches.value];
  const field = smartMatchesSortField.value;
  const direction = smartMatchesSortDirection.value;

  return items.sort((a, b) => {
    let valueA: number | string = 0;
    let valueB: number | string = 0;

    if (field === 'bankDate') {
      valueA = new Date(a.bankDate).getTime();
      valueB = new Date(b.bankDate).getTime();
    } else if (field === 'merchant') {
      valueA = a.merchant.toLowerCase();
      valueB = b.merchant.toLowerCase();
    } else if (field === 'bankAmount') {
      valueA = a.bankAmount;
      valueB = b.bankAmount;
    }

    if (valueA < valueB) return direction === 'asc' ? -1 : 1;
    if (valueA > valueB) return direction === 'asc' ? 1 : -1;
    return 0;
  });
});
const visibleSmartMatches = computed(() => sortedSmartMatches.value.slice(0, MAX_SMART_MATCHES));
const smartMatchCountLabel = computed(() =>
  totalSmartMatches.value > MAX_SMART_MATCHES ? `${visibleSmartMatches.value.length} of ${totalSmartMatches.value}` : `${totalSmartMatches.value}`,
);

// Local state for Remaining Transactions
const currentBankTransactionIndex = ref<number>(0);
const searchAmount = ref<string>('');
const searchMerchant = ref<string>('');
const searchDateRange = ref<string>('3');
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
const transactionSplits = ref<Array<{ entityId: string; category: string; amount: number }>>([{ entityId: '', category: '', amount: 0 }]);
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
  taxMetadata: [],
} as Transaction);
const newTransactionBudgetId = ref<string>(''); // Track budgetId for TransactionDialog

const entityOptions = computed(() => {
  return (familyStore.family?.entities || []).map((entity) => ({
    id: entity.id,
    name: entity.name,
  }));
});

const remainingSplitAmount = computed(() => {
  if (!selectedBankTransaction.value) return 0;
  const totalAmount = selectedBankTransaction.value.debitAmount || selectedBankTransaction.value.creditAmount || 0;
  const allocated = transactionSplits.value.reduce((sum, split) => sum + split.amount, 0);
  return Math.round((totalAmount - allocated) * 100) / 100;
});

const smartMatchColumns = [
  { name: 'bankDate', label: 'Bank Date', field: 'bankDate', sortable: true },
  { name: 'bankAmount', label: 'Bank Amount', field: 'bankAmount', sortable: true },
  { name: 'bankType', label: 'Bank Type', field: 'bankType', sortable: true },
  { name: 'payee', label: 'Payee', field: 'payee', sortable: true },
  {
    name: 'merchant',
    label: 'Merchant',
    field: 'merchant',
    sortable: true,
    style: 'max-width: 200px; width: 200px;',
    headerStyle: 'max-width: 200px; width: 200px;',
  },
  { name: 'budgetDate', label: 'Budget Date', field: 'budgetDate', sortable: true },
  { name: 'budgetAmount', label: 'Budget Amount', field: 'budgetAmount', sortable: true },
  { name: 'budgetType', label: 'Budget Type', field: 'budgetType' },
  { name: 'actions', label: 'Actions', field: 'actions' },
];

const rowKey = (row: SmartMatchRow) => row.importedTransaction.id;

const smartMatchRowClass = (row: SmartMatchRow) => {
  const classes = [] as string[];
  if (toCents(row.bankAmount) !== toCents(row.budgetAmount)) {
    classes.push('amount-mismatch');
  }
  if (row.approxMatch) {
    classes.push('approx-match');
  }
  return classes.join(' ');
};

const potentialRowClass = (row: Transaction) => {
  const bankAmount = selectedBankTransaction.value?.debitAmount || selectedBankTransaction.value?.creditAmount || 0;
  return toCents(row.amount) !== toCents(bankAmount) ? 'amount-mismatch' : '';
};

type TxRow = Transaction;
const budgetTransactionColumns = [
  { name: 'date', label: 'Date', field: 'date', sortable: true },
  { name: 'merchant', label: 'Merchant', field: 'merchant', sortable: true },
  { name: 'amount', label: 'Amount', field: 'amount', sortable: true },
  { name: 'type', label: 'Type', field: (row: TxRow) => (row.isIncome ? 'Income' : 'Expense') },
  { name: 'categories', label: 'Categories', field: (row: TxRow) => row.categories?.map((c) => c.category).join(', ') },
  { name: 'actions', label: 'Actions', field: 'actions' },
];

// Bridge selection to ids
const selectedSmartMatchesInternal = computed({
  get() {
    return visibleSmartMatches.value.filter((m) => selectedSmartMatchIds.value.includes(m.importedTransaction.id));
  },
  set(rows: Array<SmartMatchRow | string>) {
    const allowedIds = new Set(visibleSmartMatches.value.map((m) => m.importedTransaction.id));
    selectedSmartMatchIds.value = Array.isArray(rows)
      ? rows.map((r) => (typeof r === 'string' ? r : r.importedTransaction?.id)).filter((id): id is string => Boolean(id) && allowedIds.has(id))
      : [];
  },
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

function computeDefaultStartDate(): string {
  const start = new Date();
  start.setDate(start.getDate() - 30);
  return formatDateOnly(start);
}

function formatDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function areSelectedDatesValid(showNotification = false): boolean {
  if (!filterStartDate.value || !filterEndDate.value) return true;
  const start = parsedFilterStartDate.value;
  const end = parsedFilterEndDate.value;
  if (start && end && start > end) {
    if (showNotification) {
      showSnackbar('Start date must be on or before the end date', 'negative');
    }
    return false;
  }
  return true;
}

function isWithinSelectedRange(dateValue?: string | null): boolean {
  if (!dateValue) return false;
  const candidate = new Date(dateValue);
  if (Number.isNaN(candidate.getTime())) return false;
  const start = parsedFilterStartDate.value;
  if (start && candidate < start) return false;
  const end = parsedFilterEndDate.value;
  if (end && candidate > end) return false;
  return true;
}

function isSmartMatchWithinRange(match: SmartMatchRow): boolean {
  return isWithinSelectedRange(match.bankDate) && isWithinSelectedRange(match.budgetDate);
}

function applyDateFilters(options: { preserveSelection?: boolean; resetIndex?: boolean } = {}) {
  if (!areSelectedDatesValid()) return;

  const { preserveSelection = true, resetIndex = false } = options;
  const previousSelectedId = selectedBankTransaction.value?.id || null;

  const filtered = allRemainingImportedTransactions.value.filter((tx) =>
    isWithinSelectedRange(getImportedTransactionDate(tx)),
  );
  filteredImportedTransactions.value = filtered;

  if (filtered.length === 0) {
    currentBankTransactionIndex.value = -1;
    selectedBankTransaction.value = null;
    potentialMatches.value = [];
    selectedBudgetTransactionForMatch.value = [];
    return;
  }

  if (resetIndex) {
    currentBankTransactionIndex.value = 0;
    selectedBankTransaction.value = filtered[0];
  } else if (preserveSelection && previousSelectedId) {
    const existingIndex = filtered.findIndex((tx) => tx.id === previousSelectedId);
    if (existingIndex !== -1) {
      currentBankTransactionIndex.value = existingIndex;
      selectedBankTransaction.value = filtered[existingIndex];
    } else {
      currentBankTransactionIndex.value = Math.min(Math.max(currentBankTransactionIndex.value, 0), filtered.length - 1);
      selectedBankTransaction.value = filtered[currentBankTransactionIndex.value];
    }
  } else {
    currentBankTransactionIndex.value = Math.min(Math.max(currentBankTransactionIndex.value, 0), filtered.length - 1);
    selectedBankTransaction.value = filtered[currentBankTransactionIndex.value];
  }

  selectedBudgetTransactionForMatch.value = [];
}

function removeImportedTransaction(importedTransactionId: string, options: { preserveSelection?: boolean } = {}) {
  allRemainingImportedTransactions.value = allRemainingImportedTransactions.value.filter((tx) => tx.id !== importedTransactionId);
  applyDateFilters({ preserveSelection: options.preserveSelection ?? false });
  if (selectedBankTransaction.value) {
    searchBudgetTransactions();
  } else {
    potentialMatches.value = [];
  }
}

// Watchers and Initialization
onMounted(async () => {
  await initializeState();
});

watch(
  () => props.remainingImportedTransactions,
  (newVal) => {
    allRemainingImportedTransactions.value = Array.isArray(newVal) ? [...newVal] : [];
    if (!areSelectedDatesValid()) return;
    applyDateFilters({ preserveSelection: true });
    if (selectedBankTransaction.value) searchBudgetTransactions();
  },
  { deep: true },
);

watch([filterStartDate, filterEndDate], () => {
  if (!areSelectedDatesValid()) return;
  applyDateFilters({ preserveSelection: true });
  smartMatches.value = smartMatches.value.filter(isSmartMatchWithinRange);
  totalSmartMatches.value = smartMatches.value.length;
  selectedSmartMatchIds.value = selectedSmartMatchIds.value.filter((id) => smartMatches.value.some((match) => match.importedTransaction.id === id));
  if (selectedBankTransaction.value) searchBudgetTransactions();
});

async function initializeState() {
  allRemainingImportedTransactions.value = Array.isArray(props.remainingImportedTransactions) ? [...props.remainingImportedTransactions] : [];
  smartMatches.value = [];
  totalSmartMatches.value = 0;

  resetState(false);
  const hasInitialSelection = !!props.selectedBankTransaction;
  selectedBankTransaction.value = props.selectedBankTransaction || null;
  applyDateFilters({ resetIndex: !hasInitialSelection, preserveSelection: hasInitialSelection });
  if (selectedBankTransaction.value) {
    const initial = selectedBankTransaction.value;
    searchAmount.value = initial.debitAmount ? initial.debitAmount.toString() : initial.creditAmount?.toString() || '0';
    searchMerchant.value = '';
    searchDateRange.value = '3';
    searchBudgetTransactions();
  }
  await nextTick();
  isReady.value = true;
}

watch(
  () => props.selectedBankTransaction,
  (newVal) => {
    if (newVal) {
      selectedBankTransaction.value = newVal;
      if (!allRemainingImportedTransactions.value.some((tx) => tx.id === newVal.id)) {
        allRemainingImportedTransactions.value = [newVal, ...allRemainingImportedTransactions.value];
      }
      applyDateFilters({ preserveSelection: true });
      searchAmount.value = newVal.debitAmount ? newVal.debitAmount.toString() : newVal.creditAmount?.toString() || '0';
      searchMerchant.value = '';
      searchDateRange.value = '3';
      transactionSplits.value = [{ entityId: familyStore.selectedEntityId || '', category: '', amount: 0 }]; // Reset splits
      showSplitForm.value = false;
      searchBudgetTransactions();
    }
  },
);

// Methods
function findSmartMatches() {
  if (!areSelectedDatesValid(true)) return;

  lookedForSmartMatches.value = true;
  findingSmartMatches.value = true;
  try {
    smartMatches.value = [];
    totalSmartMatches.value = 0;
    allRemainingImportedTransactions.value = Array.isArray(props.remainingImportedTransactions) ? [...props.remainingImportedTransactions] : [];
    applyDateFilters({ resetIndex: true, preserveSelection: false });
    computeSmartMatchesLocal();
    showSnackbar(`Found ${totalSmartMatches.value} smart match${totalSmartMatches.value !== 1 ? 'es' : ''}`, 'info');
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error finding smart matches:', err);
    showSnackbar(`Error finding smart matches: ${err.message}`, 'negative');
  } finally {
    findingSmartMatches.value = false;
  }
}

async function confirmSmartMatches() {
  if (selectedSmartMatchIds.value.length === 0) {
    showSnackbar('No smart matches selected to confirm', 'negative');
    return;
  }

  emit('update:matching', true);
  try {
    const user = auth.currentUser;
    if (!user) throw new Error('User not authenticated');

    const matchesToConfirm = smartMatches.value.filter((match) => selectedSmartMatchIds.value.includes(match.importedTransaction.id));

    const matchesByBudget: { [budgetId: string]: Array<{ budgetTransactionId: string; importedTransactionId: string; match: boolean; ignore: boolean }> } = {};
    matchesToConfirm.forEach((match) => {
      const budgetId = match.budgetId;
      const importedTxId = match.importedTransaction.id;
      const budgetTxId = match.budgetTransaction.id;

      if (!budgetTxId) throw new Error(`Budget transaction ID is missing for match: ${JSON.stringify(match)}`);

      if (!matchesByBudget[budgetId]) matchesByBudget[budgetId] = [];
      matchesByBudget[budgetId].push({
        budgetTransactionId: budgetTxId,
        importedTransactionId: importedTxId,
        match: true,
        ignore: false,
      });
    });

    await Promise.all(
      Object.entries(matchesByBudget).map(async ([budgetId, recs]) => {
        const budget = budgetStore.getBudget(budgetId);
        if (!budget) throw new Error(`Budget ${budgetId} not found`);

        const reconcileData = {
          budgetId,
          reconciliations: recs,
        };

        await dataAccess.batchReconcileTransactions(budgetId, budget, reconcileData);
        const updatedBudget = await dataAccess.getBudget(budgetId);
        if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
      }),
    );

    showSnackbar(`${matchesToConfirm.length} smart matches confirmed successfully`);
    emit('transactions-updated');
    const confirmedIds = new Set(matchesToConfirm.map((m) => m.importedTransaction.id));
    smartMatches.value = smartMatches.value.filter((m) => !confirmedIds.has(m.importedTransaction.id));
    totalSmartMatches.value = smartMatches.value.length;
    updateRemainingTransactions(confirmedIds);
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error confirming smart matches:', err);
    showSnackbar(`Error confirming smart matches: ${err.message}`, 'negative');
  } finally {
    emit('update:matching', false);
  }
}

async function matchBankTransaction(budgetTransaction: Transaction) {
  if (!selectedBankTransaction.value || !budgetTransaction) {
    showSnackbar('Please select a budget transaction to match', 'negative');
    return;
  }

  emit('update:matching', true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error('User not authenticated');

    const importedDate = getImportedTransactionDate(importedTx) || todayISO();
    const transactionDate = importedTx.transactionDate || (importedTx.postedDate || '');

    const updatedTransaction: Transaction = {
      ...budgetTransaction,
      accountSource: importedTx.accountSource || '',
      accountNumber: importedTx.accountNumber || '',
      transactionDate: transactionDate || importedDate,
      postedDate: importedTx.postedDate || '',
      importedMerchant: importedTx.payee || '',
      status: 'C',
      id: budgetTransaction.id,
      userId: budgetTransaction.userId || user.uid,
      budgetMonth: budgetTransaction.budgetMonth || toBudgetMonth(importedDate),
      date: budgetTransaction.date || importedDate,
      merchant: budgetTransaction.merchant || importedTx.payee || '',
      categories: budgetTransaction.categories || [{ category: '', amount: 0 }],
      amount: budgetTransaction.amount || importedTx.debitAmount || importedTx.creditAmount || 0,
      notes: budgetTransaction.notes || '',
      recurring: budgetTransaction.recurring || false,
      recurringInterval: budgetTransaction.recurringInterval || 'Monthly',
      isIncome: budgetTransaction.isIncome || !!importedTx.creditAmount,
      entityId: budgetTransaction.entityId || familyStore.selectedEntityId || '',
      taxMetadata: budgetTransaction.taxMetadata || [],
    };

    const existingBudgetId =
      'budgetId' in budgetTransaction && typeof (budgetTransaction as { budgetId?: unknown }).budgetId === 'string'
        ? (budgetTransaction as { budgetId: string }).budgetId
        : undefined;
    let budget: Budget | null = null;
    if (existingBudgetId) {
      budget = budgetStore.getBudget(existingBudgetId) || (await dataAccess.getBudget(existingBudgetId));
    }
    if (!budget) {
      const fam = await familyStore.getFamily();
      budget = await createBudgetForMonth(updatedTransaction.budgetMonth, fam?.id ?? '', user.uid, updatedTransaction.entityId || '');
    }

    await dataAccess.saveTransaction(budget, updatedTransaction);

    const { docId, txId } = splitImportedId(importedTx.id);
    await dataAccess.updateImportedTransaction(docId, txId, true);

    showSnackbar('Transaction matched successfully');
    emit('transactions-updated');
    removeImportedTransaction(importedTx.id, { preserveSelection: false });
    skipBankTransaction();
  } catch (error: unknown) {
    const err = error as Error;
    console.log(err);
    showSnackbar(`Error matching transaction: ${err.message}`, 'negative');
  } finally {
    emit('update:matching', false);
  }
}

async function ignoreBankTransaction() {
  if (!selectedBankTransaction.value) {
    showSnackbar('No bank transaction selected to ignore', 'negative');
    return;
  }

  emit('update:matching', true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error('User not authenticated');

    const { docId, txId } = splitImportedId(importedTx.id);
    await dataAccess.updateImportedTransaction(docId, txId, undefined, true);

    showSnackbar('Bank transaction ignored');
    removeImportedTransaction(importedTx.id, { preserveSelection: false });
    skipBankTransaction();
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error ignoring transaction: ${err.message}`, 'negative');
  } finally {
    emit('update:matching', false);
  }
}

function skipBankTransaction() {
  if (currentBankTransactionIndex.value + 1 < filteredImportedTransactions.value.length) {
    currentBankTransactionIndex.value++;
    selectedBankTransaction.value = filteredImportedTransactions.value[currentBankTransactionIndex.value] ?? null;
    searchBudgetTransactions();
  } else {
    if (smartMatches.value.length === 0) {
      emit('transactions-updated');
      showSnackbar('All bank transactions have been processed', 'success');
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }
  }
}

async function saveSplitTransaction() {
  if (!selectedBankTransaction.value || remainingSplitAmount.value !== 0) {
    showSnackbar('Invalid split amounts or no bank transaction selected', 'negative');
    return;
  }

  if (!splitForm.value) return;

  const valid = await splitForm.value.validate();
  if (!valid) {
    showSnackbar('Please fill in all required fields', 'negative');
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
    const budgetCache: Record<string, Budget> = {};
    const importedDate = getImportedTransactionDate(importedTx) || todayISO();
    const transactionDate = importedTx.transactionDate || (importedTx.postedDate || '');

    for (const split of transactionSplits.value) {
      const budgetMonth = toBudgetMonth(importedDate);
      const key = `${split.entityId}_${budgetMonth}`;
      if (!budgetCache[key]) {
        budgetCache[key] = await createBudgetForMonth(budgetMonth, family.id, user.uid, split.entityId);
      }
      const budget = budgetCache[key];
      const baseTx = {
        id: uuidv4(),
        budgetMonth,
        date: importedDate,
        merchant: importedTx.payee || '',
        categories: [{ category: split.category, amount: split.amount }],
        amount: split.amount,
        notes: '',
        recurring: false,
        recurringInterval: 'Monthly' as const,
        userId: props.userId,
        isIncome: !!importedTx.creditAmount,
        status: 'C' as const,
        entityId: split.entityId,
        taxMetadata: [] as Transaction['taxMetadata'],
      };
      const transaction: Transaction = {
        ...baseTx,
        ...(transactionDate ? { transactionDate } : {}),
        ...(importedTx.postedDate ? { postedDate: importedTx.postedDate } : {}),
        ...(importedTx.payee ? { importedMerchant: importedTx.payee } : {}),
        ...(importedTx.accountSource ? { accountSource: importedTx.accountSource } : {}),
        ...(importedTx.accountNumber ? { accountNumber: importedTx.accountNumber } : {}),
        ...(importedTx.checkNumber ? { checkNumber: importedTx.checkNumber } : {}),
      };

      if (!transactionsByBudget[budget.budgetId]) transactionsByBudget[budget.budgetId] = [];
      transactionsByBudget[budget.budgetId].push(transaction);
    }

    // Save transactions to their respective budgets
    for (const budgetId in transactionsByBudget) {
      const budget = budgetStore.getBudget(budgetId) || (await dataAccess.getBudget(budgetId));
      if (!budget) continue;

      await dataAccess.batchSaveTransactions(budgetId, budget, transactionsByBudget[budgetId] || []);
      const updatedBudget = await dataAccess.getBudget(budgetId);
      if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
    }

    const { docId, txId } = splitImportedId(importedTx.id);
    await dataAccess.updateImportedTransaction(docId, txId, true);

    removeImportedTransaction(importedTx.id, { preserveSelection: false });

    showSnackbar('Split transaction saved successfully');
    emit('transactions-updated');
    resetSplitForm();
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error saving split transaction: ${err.message}`, 'negative');
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

    let budget =
      (savedTransaction.budgetId && (budgetStore.getBudget(savedTransaction.budgetId) || (await dataAccess.getBudget(savedTransaction.budgetId)))) || null;
    if (!budget) {
      budget = await createBudgetForMonth(savedTransaction.budgetMonth, family.id, user.uid, savedTransaction.entityId || '');
    }

    budgetStore.updateBudget(budget.budgetId, {
      ...budget,
      transactions: [...budget.transactions, savedTransaction],
    });

    const { docId, txId } = splitImportedId(importedTx.id);
    await dataAccess.updateImportedTransaction(docId, txId, true);

    removeImportedTransaction(importedTx.id, { preserveSelection: false });

    showSnackbar('Transaction added and matched successfully');
    emit('transactions-updated');
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error adding transaction: ${err.message}`, 'negative');
  } finally {
    emit('update:matching', false);
    showTransactionDialog.value = false;
  }
}

async function addNewTransaction() {
  if (!selectedBankTransaction.value) {
    showSnackbar('No bank transaction selected to add', 'negative');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before adding a transaction', 'negative');
    return;
  }

  const importedDate = getImportedTransactionDate(selectedBankTransaction.value) || todayISO();
  const transactionDate = selectedBankTransaction.value?.transactionDate || (selectedBankTransaction.value?.postedDate || '');
  const postedDate = selectedBankTransaction.value?.postedDate || '';
  let budgetMonth = toBudgetMonth(importedDate);

  if (!budgetStore.availableBudgetMonths.includes(budgetMonth)) {
    budgetMonth = budgetStore.availableBudgetMonths[budgetStore.availableBudgetMonths.length - 1] || budgetMonth;
    console.log(`Budget month ${toBudgetMonth(importedDate)} not found, falling back to ${budgetMonth}`);
  }

  const baseTx = {
    id: '',
    budgetMonth,
    date: importedDate,
    merchant: selectedBankTransaction.value.payee || '',
    categories: [
      {
        category: '',
        amount: selectedBankTransaction.value.debitAmount || selectedBankTransaction.value.creditAmount || 0,
      },
    ],
    amount: selectedBankTransaction.value.debitAmount || selectedBankTransaction.value.creditAmount || 0,
    notes: '',
    recurring: false,
    recurringInterval: 'Monthly' as const,
    userId: props.userId,
    isIncome: !!selectedBankTransaction.value.creditAmount,
    status: 'C' as const,
    entityId: familyStore.selectedEntityId || '',
    taxMetadata: [] as Transaction['taxMetadata'],
  };
  newTransaction.value = {
    ...baseTx,
    ...(transactionDate ? { transactionDate } : {}),
    ...(postedDate ? { postedDate } : {}),
    ...(selectedBankTransaction.value.payee ? { importedMerchant: selectedBankTransaction.value.payee } : {}),
    ...(selectedBankTransaction.value.accountSource ? { accountSource: selectedBankTransaction.value.accountSource } : {}),
    ...(selectedBankTransaction.value.accountNumber ? { accountNumber: selectedBankTransaction.value.accountNumber } : {}),
    ...(selectedBankTransaction.value.checkNumber ? { checkNumber: selectedBankTransaction.value.checkNumber } : {}),
  } as Transaction;
  if (familyStore.family) {
    const budget = await createBudgetForMonth(budgetMonth, familyStore.family.id, props.userId, familyStore.selectedEntityId || '');
    newTransactionBudgetId.value = budget.budgetId;
  }
  showTransactionDialog.value = true;
}

function toggleSplitTransaction() {
  showSplitForm.value = !showSplitForm.value;
  if (showSplitForm.value) {
    transactionSplits.value = [{ entityId: familyStore.selectedEntityId || '', category: '', amount: 0 }];
  } else {
    resetSplitForm();
  }
}

function addSplitTransaction() {
  transactionSplits.value.push({ entityId: familyStore.selectedEntityId || '', category: '', amount: 0 });
}

function resetSplitForm() {
  transactionSplits.value = [{ entityId: familyStore.selectedEntityId || '', category: '', amount: 0 }];
  showSplitForm.value = false;
}

const isBudgetTxMatchedMultiple = (budgetTxId: string) => {
  const matchesForBudgetTx = smartMatches.value.filter((match) => match.budgetTransaction.id === budgetTxId);
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
  potentialMatchesSortDirection.value = potentialMatchesSortDirection.value === 'asc' ? 'desc' : 'asc';
}

function merchantSimilarity(a: string, b: string): number {
  const normalize = (s: string) =>
    s
      .toLowerCase()
      .replace(/[^a-z0-9\s]/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  const aTokens = normalize(a).split(' ').filter(Boolean);
  const bTokens = normalize(b).split(' ').filter(Boolean);
  if (aTokens.length === 0 || bTokens.length === 0) return 0;
  const bSet = new Set(bTokens);
  let matches = 0;
  aTokens.forEach((t) => {
    if (bSet.has(t)) matches++;
  });
  return matches / Math.max(aTokens.length, bTokens.length);
}

function searchBudgetTransactions() {
  if (!selectedBankTransaction.value) return;

  const bankTx = selectedBankTransaction.value;
  const bankDateStr = getImportedTransactionDate(bankTx);
  if (!isWithinSelectedRange(bankDateStr)) {
    potentialMatches.value = [];
    return;
  }

  const amount = parseFloat(searchAmount.value) || bankTx.debitAmount || bankTx.creditAmount || 0;
  const merchant = searchMerchant.value.toLowerCase();
  const dateRangeDays = parseInt(searchDateRange.value) || 3;

  const bankDate = new Date(bankDateStr);
  if (Number.isNaN(bankDate.getTime())) {
    potentialMatches.value = [];
    return;
  }
  const startDate = new Date(bankDate);
  startDate.setDate(bankDate.getDate() - dateRangeDays);
  const endDate = new Date(bankDate);
  endDate.setDate(bankDate.getDate() + dateRangeDays);

  potentialMatches.value = props.transactions.filter((tx) => {
    const txDate = new Date(tx.date);
    const txAmount = tx.amount;
    const txMerchant = tx.merchant.toLowerCase();
    if (!isWithinSelectedRange(tx.date)) return false;

    const dateMatch = txDate >= startDate && txDate <= endDate;
    const amountMatch = Math.abs(txAmount - amount) <= 0.05;
    const merchantMatch = merchant ? txMerchant.includes(merchant) : true;

    return !tx.deleted && dateMatch && amountMatch && merchantMatch && (!tx.status || tx.status === 'U');
  });
}

function computeSmartMatchesLocal(confirmedMatches: typeof smartMatches.value = []) {
  const confirmedIds = new Set(confirmedMatches.map((m) => m.importedTransaction.id));
  const dateRangeDays = Math.max(parseInt(smartMatchDateRange.value, 10) || 0, 0);
  const retainedSmartMatches = smartMatches.value.filter((match) => !confirmedIds.has(match.importedTransaction.id)).filter(isSmartMatchWithinRange);

  const unmatchedImported = allRemainingImportedTransactions.value.filter(
    (tx) =>
      !confirmedIds.has(tx.id) &&
      !retainedSmartMatches.some((m) => m.importedTransaction.id === tx.id) &&
      isWithinSelectedRange(getImportedTransactionDate(tx)),
  );

  const budgetCandidates = props.transactions.filter((tx) => !tx.deleted && (!tx.status || tx.status === 'U') && isWithinSelectedRange(tx.date));

  const potentialMatches: Array<{
    importedTx: ImportedTransaction;
    budgetTx: Transaction;
    budgetId: string;
    bankAmount: number;
    bankType: string;
    merchantScore: number;
    dateExact: boolean;
    approxAmount: boolean;
  }> = [];

  unmatchedImported.forEach((importedTx) => {
    const bankAmount = importedTx.debitAmount || importedTx.creditAmount || 0;
    const bankDateStr = getImportedTransactionDate(importedTx);
    const bankDate = new Date(bankDateStr || '');
    if (Number.isNaN(bankDate.getTime())) return;
    const normalizedBankDate = new Date(bankDate.toISOString().split('T')[0]);

    const startDate = new Date(normalizedBankDate);
    startDate.setDate(normalizedBankDate.getDate() - dateRangeDays);
    const endDate = new Date(normalizedBankDate);
    endDate.setDate(normalizedBankDate.getDate() + dateRangeDays);

    budgetCandidates.forEach((tx) => {
      const txDate = new Date(tx.date || '');
      if (Number.isNaN(txDate.getTime())) return;
      const txDateStr = txDate.toISOString().split('T')[0];
      const normalizedTxDate = new Date(txDateStr);
      const txAmount = tx.amount;
      const typeMatch = tx.isIncome === !!importedTx.creditAmount;
      if (normalizedTxDate >= startDate && normalizedTxDate <= endDate && Math.abs(txAmount - bankAmount) <= 0.05 && typeMatch) {
        const diffCents = Math.abs(toCents(txAmount) - toCents(bankAmount));
        const score = merchantSimilarity(importedTx.payee || '', tx.merchant);
        potentialMatches.push({
          importedTx,
          budgetTx: tx,
          budgetId: 'budgetId' in tx && typeof (tx as { budgetId?: unknown }).budgetId === 'string' ? (tx as { budgetId: string }).budgetId : '',
          bankAmount,
          bankType: importedTx.debitAmount ? 'Debit' : 'Credit',
          merchantScore: score,
          dateExact: normalizedTxDate.getTime() === normalizedBankDate.getTime(),
          approxAmount: diffCents !== 0,
        });
      }
    });
  });

  potentialMatches.sort((a, b) => {
    if (a.approxAmount !== b.approxAmount) return a.approxAmount ? 1 : -1;
    if (a.merchantScore !== b.merchantScore) return b.merchantScore - a.merchantScore;
    if (a.dateExact !== b.dateExact) return a.dateExact ? -1 : 1;
    return 0;
  });

  const usedBudgetTxIds = new Set<string>();
  const usedBankTxIds = new Set<string>();
  const smartMatchesToAdd: typeof potentialMatches = [];

  potentialMatches.forEach((m) => {
    if (!usedBudgetTxIds.has(m.budgetTx.id) && !usedBankTxIds.has(m.importedTx.id)) {
      smartMatchesToAdd.push(m);
      usedBudgetTxIds.add(m.budgetTx.id);
      usedBankTxIds.add(m.importedTx.id);
    }
  });

  const combinedSmartMatches: SmartMatchRow[] = [...retainedSmartMatches];

  smartMatchesToAdd.forEach((match) => {
    combinedSmartMatches.push({
      importedTransaction: match.importedTx,
      budgetTransaction: { ...match.budgetTx, id: match.budgetTx.id },
      budgetId: match.budgetId,
      bankAmount: match.bankAmount,
      bankType: match.bankType,
      bankDate: getImportedTransactionDate(match.importedTx),
      payee: match.importedTx.payee,
      merchant: match.budgetTx.merchant,
      budgetDate: match.budgetTx.date,
      budgetAmount: match.budgetTx.amount,
      budgetType: match.budgetTx.isIncome ? 'Income' : 'Expense',
      approxMatch: match.approxAmount,
    });
  });

  totalSmartMatches.value = combinedSmartMatches.length;
  smartMatches.value = combinedSmartMatches;
  selectedSmartMatchIds.value = [];

  const smartMatchImportedIds = new Set(combinedSmartMatches.map((m) => m.importedTransaction.id));
  allRemainingImportedTransactions.value = allRemainingImportedTransactions.value.filter((tx) => !smartMatchImportedIds.has(tx.id));
  applyDateFilters({ resetIndex: true, preserveSelection: false });
  if (selectedBankTransaction.value) {
    searchBudgetTransactions();
  }
}

function updateRemainingTransactions(matchedIds?: Set<string>) {
  const ids =
    matchedIds ||
    new Set(
      smartMatches.value.filter((match) => selectedSmartMatchIds.value.includes(match.importedTransaction.id)).map((match) => match.importedTransaction.id),
    );
  if (ids.size === 0) return;

  selectedSmartMatchIds.value = [];
  allRemainingImportedTransactions.value = allRemainingImportedTransactions.value.filter((importedTx) => !ids.has(importedTx.id));
  applyDateFilters({ preserveSelection: false });
  if (selectedBankTransaction.value) {
    searchBudgetTransactions();
  } else {
    potentialMatches.value = [];
  }
}

function resetState(computeMatches = true) {
  selectedSmartMatchIds.value = [];
  currentBankTransactionIndex.value = filteredImportedTransactions.value.length > 0 ? 0 : -1;
  potentialMatches.value = [];
  selectedBudgetTransactionForMatch.value = [];
  smartMatchesSortField.value = 'merchant';
  smartMatchesSortDirection.value = 'asc';
  potentialMatchesSortField.value = 'date';
  potentialMatchesSortDirection.value = 'asc';
  if (computeMatches) computeSmartMatchesLocal();
  else console.log('Skipping computeSmartMatchesLocal on initial reset');
}

function showSnackbar(text: string, color = 'success') {
  $q.notify({
    message: text,
    color,
    position: 'bottom',
    timeout: timeout.value,
    actions: [{ label: 'Close', color: 'white', handler: () => {} }],
  });
}

// Helper function to create a budget if it doesn't exist
</script>

<style>
.q-table tbody tr.amount-mismatch td {
  background-color: #fff7e6;
}
.q-table tbody tr.amount-mismatch.q-tr--selected td,
.q-table tbody tr.amount-mismatch.selected td {
  background-color: #ffe5cc;
}
.q-table tbody tr.approx-match td {
  font-weight: 600;
}
</style>
