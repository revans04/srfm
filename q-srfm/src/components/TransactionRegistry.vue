<!-- src/components/TransactionRegistry.vue -->
<template>
  <q-page fluid>
    <div class="row" v-if="selectedAccount">
      <div class="col col-12 col-md-4">
        <q-select
          v-model="selectedStatementId"
          :options="statementOptions"
          option-label="title"
          option-value="id"
          emit-value
          map-options
          label="Select Statement"
          variant="outlined"
        ></q-select>
      </div>
      <div class="col col-auto">
        <q-btn color="primary" @click="openStatementDialog">Add Statement</q-btn>
      </div>
      <div class="col col-auto" v-if="selectedStatement">
        <q-btn color="primary" @click="startReconcile">Reconcile Statement</q-btn>
      </div>
      <div class="col col-auto" v-if="selectedStatement && selectedStatement.reconciled">
        <q-btn color="primary" @click="unreconcileStatement">Unreconcile</q-btn>
      </div>
      <div class="col col-auto" v-if="selectedStatement">
        <q-btn color="negative" @click="deleteStatement">Delete Statement</q-btn>
      </div>
    </div>

    <!-- Balance Display -->
    <q-card class="q-mb-lg" v-if="selectedAccount">
      <q-card-section>Account Balance</q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col">
            <span class="text-h6">Balance from Accounts: </span>
            <span
              :class="{
                'text-positive': currentBalance === latestTransactionBalance,
                'text-negative': currentBalance !== latestTransactionBalance,
              }"
            >
              {{ formatCurrency(currentBalance) }}
            </span>
            <q-tooltip v-if="currentBalance !== latestTransactionBalance" activator="parent" location="top">
              The current balance differs from the transaction registry balance ({{ formatCurrency(latestTransactionBalance) }}) by
              {{ formatCurrency(Math.abs(currentBalance - latestTransactionBalance)) }}. Adjust Balance to reconcile.
            </q-tooltip>
          </div>
          <div class="col col-auto" v-if="currentBalance !== latestTransactionBalance">
            <q-btn color="primary" @click="openBalanceAdjustmentDialog">Adjust Balance</q-btn>
          </div>
        </div>
      </q-card-section>
    </q-card>

    <q-card class="q-mb-lg" v-if="selectedStatement">
      <q-card-section>Statement Totals</q-card-section>
      <q-card-section>
        <p>Start Balance: {{ formatCurrency(selectedStatement.startingBalance) }}</p>
        <p>End Balance: {{ formatCurrency(selectedStatement.endingBalance) }}</p>
      </q-card-section>
    </q-card>

    <!-- Loading handled via $q.loading -->

    <q-card class="q-mb-lg">
      <q-card-section>Filters</q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col col-12 col-md-4">
            <q-select
              v-model="selectedAccount"
              :options="accountOptions"
              option-label="title"
              option-value="value"
              emit-value
              map-options
              placeholder="Account"
              variant="outlined"
              clearable
              @update:modelValue="loadTransactions"
            ></q-select>
          </div>
          <div class="col col-12 col-md-4">
            <q-input
              append-inner-icon="search"
              density="compact"
              label="Search"
              variant="outlined"
              single-line
              v-model="search"
              @input="applyFilters"
            ></q-input>
          </div>
          <div class="col col-auto">
            <q-checkbox v-model="filterMatched" label="Show Only Unmatched" density="compact" @input="applyFilters"></q-checkbox>
          </div>
          <div class="col col-auto d-flex align-center">
            <q-btn color="primary" variant="plain" @click="refreshData" :loading="loading">
              <q-icon start name="refresh"></q-icon>
              Refresh
            </q-btn>
          </div>
        </div>
        <div class="row">
          <div class="col col-12 col-md-2">
            <q-input v-model="filterMerchant" label="Merchant" variant="outlined" density="compact" @input="applyFilters"></q-input>
          </div>
          <div class="col col-12 col-md-2">
            <q-input v-model="filterAmount" label="Amount" type="number" variant="outlined" density="compact" @input="applyFilters"></q-input>
          </div>
          <div class="col col-12 col-md-3">
            <q-input v-model="filterImportedMerchant" label="Imported Merchant" variant="outlined" density="compact" @input="applyFilters"></q-input>
          </div>
          <div class="col col-12 col-md-5">
            <div class="row">
              <div class="col col-12 col-md-6">
                <q-input
                  v-model="filterStartDate"
                  label="Start Date"
                  type="date"
                  variant="outlined"
                  density="compact"
                  :clearable="true"
                  @input="applyFilters"
                ></q-input>
              </div>
              <div class="col col-12 col-md-6">
                <q-input
                  v-model="filterEndDate"
                  label="End Date"
                  type="date"
                  variant="outlined"
                  density="compact"
                  :clearable="true"
                  @input="applyFilters"
                ></q-input>
              </div>
            </div>
          </div>
        </div>
      </q-card-section>
    </q-card>

    <q-card v-if="!loading">
      <q-card-section>
        <div class="row dense">
          <div class="col">Transaction Registry</div>
          <div class="col col-auto">
            <q-btn variant="plain" @click="downloadCsv" :disabled="displayTransactions.length === 0">
              <q-icon name="download"></q-icon>
              <q-tooltip activator="parent" location="top">Download CSV</q-tooltip>
            </q-btn>
          </div>
        </div>
      </q-card-section>
      <q-card-section>
        <q-btn
          v-if="selectedRows.length > 0"
          color="primary"
          @click="openBatchMatchDialog"
          class="q-mb-lg"
          :disabled="
            loading ||
            !selectedRows.every((id) => {
              const tx = displayTransactions.find((t: DisplayTransaction) => t.id === id);
              return tx && tx.status === 'U';
            })
          "
        >
          Create {{ selectedRows.length }} Budget Transaction{{ selectedRows.length > 1 ? 's' : '' }}
        </q-btn>
        <q-btn
          v-if="selectedRows.length > 0"
          color="warning"
          class="q-mb-lg q-ml-sm"
          @click="confirmBatchAction('Ignore')"
          :disabled="
            loading ||
            !selectedRows.every((id) => {
              const tx = displayTransactions.find((t: DisplayTransaction) => t.id === id);
              return tx && tx.status === 'U' && !tx.budgetId;
            })
          "
        >
          Ignore {{ selectedRows.length }}
        </q-btn>
        <q-btn
          v-if="selectedRows.length > 0"
          color="negative"
          class="q-mb-lg q-ml-sm"
          @click="confirmBatchAction('Delete')"
          :disabled="
            loading ||
            !selectedRows.every((id) => {
              const tx = displayTransactions.find((t: DisplayTransaction) => t.id === id);
              return tx && tx.status === 'U' && !tx.budgetId;
            })
          "
        >
          Delete {{ selectedRows.length }}
        </q-btn>
      </q-card-section>
      <q-table
        :columns="columns"
        :rows="displayTransactions"
        class="elevation-1"
        :pagination="{ rowsPerPage: 0 }"
        hide-bottom
        selection="multiple"
        row-key="id"
        v-model:selected="selectedRowsInternal"
        fixed-header
        fixed-footer
        height="600"
        :table-row-class-fn="getRowClass"
        virtual-scroll
        @virtual-scroll="onTableVirtualScroll"
        @row-click="onSingleRowClick"
      >
        <template #body-cell-amount="{ row }">
          <span :class="row.isIncome ? 'text-positive' : 'text-negative'">
            {{ formatCurrency(row.amount) }}
          </span>
        </template>
        <template #body-cell-balance="{ row }">
          {{ formatCurrency(row.balance) }}
        </template>
        <template #body-cell-entity="{ row }">
          {{ getEntityName(row.entityId || row.budgetId) }}
        </template>
        <template #body-cell-actions="{ row }">
          <q-btn
            v-if="row.status === 'C'"
            density="compact"
            variant="plain"
            color="warning"
            @click.stop="confirmAction(row, 'Disconnect')"
            title="Disconnect Transaction"
          >
            <q-icon name="link_off"></q-icon>
          </q-btn>
          <q-btn
            v-if="row.status != 'C' && row.id"
            density="compact"
            variant="plain"
            color="negative"
            @click.stop="confirmAction(row, 'Ignore')"
            title="Ignore Imported Transaction"
          >
            <q-icon name="visibility_off"></q-icon>
          </q-btn>
          <q-btn
            v-if="row.status != 'C' && row.id"
            density="compact"
            variant="plain"
            color="negative"
            @click.stop="confirmAction(row, 'Delete')"
            title="Delete Imported Transaction"
          >
            <q-icon name="delete_outline"></q-icon>
          </q-btn>
        </template>
      </q-table>
    </q-card>

    <!-- Action Confirmation Dialog -->
    <q-dialog v-model="showActionDialog" max-width="400" @keyup.enter="executeAction">
      <q-card>
        <q-card-section class="bg-warning q-py-md">
          <span class="text-white">{{ transactionAction }} Transaction</span>
        </q-card-section>
        <q-card-section v-if="transactionAction == 'Disconnect'" class="q-pt-lg">
          Are you sure you want to disconnect the transaction for "{{ transactionToAction?.merchant }}" on {{ transactionToAction?.date }} from its imported
          transaction?
        </q-card-section>
        <q-card-section v-else class="q-pt-lg">
          Are you sure you want to {{ transactionAction }} the transaction for "{{ transactionToAction?.merchant }}" on {{ transactionToAction?.date }}?
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showActionDialog = false">Cancel</q-btn>
          <q-btn color="warning" variant="flat" @click="executeAction">{{ transactionAction }}</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Batch Match Dialog -->
    <q-dialog v-model="showBatchMatchDialog" max-width="500" @keyup.enter="executeBatchMatch">
      <q-card>
        <q-card-section class="bg-primary q-py-md">
          <span class="text-white">Batch Match Transactions</span>
        </q-card-section>
        <q-card-section class="q-pt-lg">
          <q-form ref="batchMatchForm">
            <p>Assign an entity, merchant, and category for {{ selectedRows.length }} unmatched transaction{{ selectedRows.length > 1 ? 's' : '' }}.</p>
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
                ></q-select>
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
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showBatchMatchDialog = false">Cancel</q-btn>
          <q-btn color="primary" variant="flat" @click="executeBatchMatch" :loading="saving">Match</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Add Statement Dialog -->
    <q-dialog v-model="showStatementDialog" max-width="500">
      <q-card>
        <q-card-section class="bg-primary q-py-md">
          <span class="text-white">Add Statement</span>
        </q-card-section>
        <q-card-section>
          <q-form ref="statementForm">
            <div class="row">
              <div class="col">
                <q-input v-model="newStatement.startDate" label="Start Date" type="date" variant="outlined" density="compact" :rules="requiredField"></q-input>
              </div>
              <div class="col">
                <q-input
                  v-model.number="newStatement.startingBalance"
                  label="Starting Balance"
                  type="number"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                ></q-input>
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input v-model="newStatement.endDate" label="End Date" type="date" variant="outlined" density="compact" :rules="requiredField"></q-input>
              </div>
              <div class="col">
                <q-input
                  v-model.number="newStatement.endingBalance"
                  label="Ending Balance"
                  type="number"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                ></q-input>
              </div>
            </div>
          </q-form>
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="closeStatementDialog">Cancel</q-btn>
          <q-btn color="primary" variant="flat" @click="saveStatement" :loading="saving">Save</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Batch Action Dialog -->
    <q-dialog v-model="showBatchActionDialog" max-width="400" @keyup.enter="executeBatchAction">
      <q-card>
        <q-card-section class="bg-warning q-py-md">
          <span class="text-white">{{ batchAction }} Selected Transactions</span>
        </q-card-section>
        <q-card-section class="q-pt-lg">
          Are you sure you want to {{ batchAction.toLowerCase() }} {{ selectedRows.length }} transaction{{ selectedRows.length > 1 ? 's' : '' }}?
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showBatchActionDialog = false">Cancel</q-btn>
          <q-btn color="warning" variant="flat" @click="executeBatchAction" :loading="saving">{{ batchAction }}</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Balance Adjustment Dialog -->
    <q-dialog v-model="showBalanceAdjustmentDialog" max-width="500">
      <q-card>
        <q-card-section class="bg-primary q-py-md">
          <span class="text-white">Adjust Initial Balance</span>
        </q-card-section>
        <q-card-section>
          <q-form ref="adjustmentForm" @submit.prevent="saveBalanceAdjustment">
            <div class="row">
              <div class="col">
                <p>
                  The current account balance ({{ formatCurrency(currentBalance) }}) differs from the transaction registry balance ({{
                    formatCurrency(latestTransactionBalance)
                  }}).
                </p>
                <p>Enter the adjustment amount to reconcile the balance:</p>
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input
                  v-model.number="adjustmentAmount"
                  label="Adjustment Amount"
                  type="number"
                  variant="outlined"
                  density="compact"
                  :rules="adjustmentRules"
                  hint="Positive to increase balance, negative to decrease"
                  persistent-hint
                ></q-input>
              </div>
            </div>
            <div class="row">
              <div class="col">
                <q-input v-model="adjustmentDate" label="Adjustment Date" type="date" variant="outlined" density="compact" :rules="requiredField"></q-input>
              </div>
            </div>
            <q-btn type="submit" color="primary" :loading="saving">Save Adjustment</q-btn>
            <q-btn color="grey" variant="text" @click="closeBalanceAdjustmentDialog" class="q-ml-sm">Cancel</q-btn>
          </q-form>
        </q-card-section>
      </q-card>
    </q-dialog>

    <!-- Reconcile Summary -->
    <q-card class="q-mt-lg" v-if="reconciling && selectedStatement">
      <q-card-section class="bg-primary q-py-md">
        <span class="text-white">Reconcile Statement</span>
      </q-card-section>
      <q-card-section>
        <p>
          Select transactions to reconcile for
          {{ selectedStatement.startDate }} - {{ selectedStatement.endDate }}
        </p>
        <div class="q-mt-lg">
          <p>Starting Balance: {{ formatCurrency(selectedStatement.startingBalance) }}</p>
          <p>Selected Total: {{ formatCurrency(selectedTransactionsTotal) }}</p>
          <p>Calculated Ending Balance: {{ formatCurrency(calculatedEndingBalance) }}</p>
          <p :class="{ 'text-negative': !reconcileMatches }">Statement Ending Balance: {{ formatCurrency(selectedStatement.endingBalance) }}</p>
          <q-banner type="warning" v-if="!reconcileMatches" class="q-mt-sm" dense>
            Calculated ending balance does not match statement ending balance.
          </q-banner>
        </div>
      </q-card-section>
      <q-card-actions>
        <q-space></q-space>
        <q-btn color="grey" variant="text" @click="cancelReconcile">Cancel</q-btn>
        <q-btn color="primary" variant="flat" @click="markStatementReconciled" :loading="saving">Reconcile</q-btn>
      </q-card-actions>
    </q-card>

    <!-- Snackbar handled via $q.notify -->
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useQuasar } from 'quasar';
import { storeToRefs } from 'pinia';
import { auth } from '../firebase/init';
import { dataAccess } from '../dataAccess';
import Papa from 'papaparse';
import { saveAs } from 'file-saver';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { useStatementStore } from '../store/statements';
import { createBudgetForMonth } from '../utils/budget';
import { useUIStore } from '../store/ui';
import type { Transaction, ImportedTransaction, Budget, Account, ImportedTransactionDoc, Statement } from '../types';
import { formatCurrency, todayISO, getImportedTransactionDate } from '../utils/helpers';
import { splitImportedId } from '../utils/imported';
import { QForm } from 'quasar';
import { v4 as uuidv4 } from 'uuid';

// Interface for displayTransactions items
interface DisplayTransaction {
  id: string;
  date: string;
  merchant: string;
  category?: string;
  entityId?: string;
  amount: number;
  isIncome?: boolean;
  status: 'C' | 'U' | 'R';
  notes?: string;
  balance?: number;
  order?: number;
  budgetId?: string;
}

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();
const statementStore = useStatementStore();
const $q = useQuasar();
const familyId = computed(() => familyStore.family?.id || '');

const loading = ref(false);
const saving = ref(false);
const uiStore = useUIStore();
const { selectedAccount, search, filterMerchant, filterMatched, filterAmount, filterImportedMerchant, filterStartDate, filterEndDate } = storeToRefs(uiStore);
const budgets = ref<Budget[]>([]);
const importedTransactions = ref<ImportedTransaction[]>([]);
const importedOffset = ref(0);
const pageSize = 100;
const hasMoreImported = ref(true);
const loadingMore = ref(false);
const accounts = ref<Account[]>([]);
const accountOptions = ref<{ title: string; value: string }[]>([]);
const statements = ref<Statement[]>([]);
const statementOptions = computed(() => [
  { title: 'All', id: 'ALL' },
  ...statements.value.map((s) => ({
    title: `${s.startDate} - ${s.endDate}`,
    id: s.id,
  })),
]);
const selectedStatementId = ref<string | null>(null);
const showActionDialog = ref(false);
const transactionToAction = ref<DisplayTransaction | null>(null);
const transactionAction = ref('');
const showBalanceAdjustmentDialog = ref(false);
const adjustmentAmount = ref<number>(0);
const adjustmentDate = ref<string>(todayISO());
const adjustmentForm = ref<InstanceType<typeof QForm> | null>(null);
const selectedRows = ref<string[]>([]);
const showBatchMatchDialog = ref(false);
const batchMatchForm = ref<InstanceType<typeof QForm> | null>(null);
const batchEntries = ref<Array<{ id: string; date: string; amount: number; merchant: string; category: string; isIncome: boolean }>>([]);
const selectedEntityId = ref<string>('');
const showBatchActionDialog = ref(false);
const batchAction = ref<string>('');
const showStatementDialog = ref(false);
const statementForm = ref<InstanceType<typeof QForm> | null>(null);
const newStatement = ref<Statement>({
  id: '',
  accountNumber: '',
  startDate: '',
  startingBalance: 0,
  endDate: '',
  endingBalance: 0,
  reconciled: false,
});
const reconciling = ref(false);
const selectedStatement = computed(() => {
  if (!selectedStatementId.value || selectedStatementId.value === 'ALL') {
    return null;
  }
  return statements.value.find((s) => s.id === selectedStatementId.value) || null;
});
const categoryOptions = computed(() => {
  const categories = new Set<string>(['Income']);
  budgets.value.forEach((budget) => {
    budget.categories.forEach((cat) => categories.add(cat.name));
  });
  return Array.from(categories).sort();
});

// Entity options for the dropdown
const entityOptions = computed(() => {
  return (familyStore.family?.entities || []).map((entity) => ({
    id: entity.id,
    name: entity.name,
  }));
});

// Validation rules
const requiredField = [(value: string) => !!value || 'This field is required'];
const adjustmentRules = [(value: number) => value !== undefined || 'Amount is required', (value: number) => !isNaN(value) || 'Amount must be a number'];

// Computed properties
const currentBalance = computed(() => {
  if (!selectedAccount.value) return 0;
  const account = accounts.value.find((acc) => acc.accountNumber === selectedAccount.value);
  return account ? account.balance || 0 : 0;
});

const latestTransactionBalance = computed(() => {
  const first = displayTransactions.value?.[0];
  if (!first) return 0;
  if (selectedAccount.value && accounts.value) {
    const [acc] = accounts.value.filter((a) => (a.accountNumber ?? '') === selectedAccount.value);
    if (acc && (acc.type === 'CreditCard' || acc.type === 'Loan')) {
      return Math.abs(first.balance ?? 0);
    }
  }
  return first.balance ?? 0;
});

const columns = [
  { name: 'date', label: 'Date', field: 'date', sortable: true },
  { name: 'merchant', label: 'Merchant', field: 'merchant', sortable: true },
  { name: 'category', label: 'Category', field: 'category', sortable: true },
  { name: 'entity', label: 'Entity', field: (row: DisplayTransaction) => row.entityId || row.budgetId },
  { name: 'amount', label: 'Amount', field: 'amount', sortable: true },
  { name: 'status', label: 'Status', field: 'status', sortable: true },
  { name: 'notes', label: 'Notes', field: 'notes' },
  { name: 'balance', label: 'Balance', field: 'balance', sortable: true },
  { name: 'actions', label: 'Actions', field: 'actions' },
];

// Bridge selection (rows vs ids)
const selectedRowsInternal = computed({
  get() {
    return displayTransactions.value.filter((t) => selectedRows.value.includes(t.id));
  },
  set(rows: DisplayTransaction[]) {
    if (Array.isArray(rows)) {
      selectedRows.value = rows.map((r) => r.id);
    } else {
      selectedRows.value = [];
    }
  },
});

// Combined transactions for display
const displayTransactions = computed((): DisplayTransaction[] => {
  if (!selectedAccount.value) return [];

  let matchedTxs: DisplayTransaction[] = budgets.value
    .flatMap((budget) =>
      (budget.transactions || []).map((tx) => ({
        tx,
        budget,
      })),
    )
    .filter(
      ({ tx }) =>
        tx.accountNumber === selectedAccount.value &&
        tx.status === 'C' &&
        !tx.deleted &&
        (!selectedStatement.value || (tx.date >= selectedStatement.value.startDate && tx.date <= selectedStatement.value.endDate)),
    )
    .map(({ tx, budget }) => ({
      id: tx.id,
      date: tx.date,
      merchant: tx.importedMerchant || tx.merchant || 'N/A',
      category: tx.categories?.[0]?.category ?? 'N/A',
      amount: tx.isIncome || false ? tx.amount : -1 * tx.amount,
      isIncome: tx.isIncome || false,
      status: tx.status || 'C',
      notes: tx.notes || '',
      ...(tx.entityId || budget.entityId ? { entityId: tx.entityId ?? budget.entityId } : {}),
      ...(budget.budgetId ? { budgetId: budget.budgetId } : {}),
    }));

  const baseUnmatchedTxs: (DisplayTransaction & { ignored?: boolean })[] = importedTransactions.value
    .filter((tx) => {
      if (tx.accountNumber !== selectedAccount.value) return false;
      if (tx.matched || tx.deleted) return false;
      if (!selectedStatement.value) return true;
      const dateStr = getImportedTransactionDate(tx);
      return dateStr >= selectedStatement.value.startDate && dateStr <= selectedStatement.value.endDate;
    })
    .map((tx) => ({
      id: tx.id,
      date: getImportedTransactionDate(tx),
      merchant: tx.payee || 'N/A',
      category: '',
      entityId: '',
      amount: tx.debitAmount && tx.debitAmount > 0 ? -tx.debitAmount : (tx.creditAmount ?? 0),
      isIncome: (tx.creditAmount ?? 0) > 0,
      status: tx.status || 'U',
      notes: '',
      ignored: !!tx.ignored,
    }));

  let unmatchedTxs: DisplayTransaction[] = baseUnmatchedTxs.filter((tx) => !tx.ignored);

  // Apply filters
  if (filterMatched.value) {
    matchedTxs = [];
  }

  if (filterMerchant.value) {
    const merchantFilter = filterMerchant.value.toLowerCase();
    matchedTxs = matchedTxs.filter((t) => t.merchant.toLowerCase().includes(merchantFilter));
    unmatchedTxs = unmatchedTxs.filter((t) => t.merchant.toLowerCase().includes(merchantFilter));
  }

  if (filterAmount.value) {
    const amountFilter = parseFloat(filterAmount.value);
    if (!isNaN(amountFilter)) {
      matchedTxs = matchedTxs.filter((t) => Math.abs(t.amount).toString().includes(amountFilter.toString()));
      unmatchedTxs = unmatchedTxs.filter((t) => Math.abs(t.amount).toString().includes(amountFilter.toString()));
    }
  }

  if (filterImportedMerchant.value) {
    const importedMerchantFilter = filterImportedMerchant.value.toLowerCase();
    matchedTxs = matchedTxs.filter((t) => t.merchant.toLowerCase().includes(importedMerchantFilter));
    unmatchedTxs = unmatchedTxs.filter((t) => t.merchant.toLowerCase().includes(importedMerchantFilter));
  }

  if (filterStartDate.value) {
    matchedTxs = matchedTxs.filter((t) => t.date >= filterStartDate.value);
    unmatchedTxs = unmatchedTxs.filter((t) => t.date >= filterStartDate.value);
  }

  if (filterEndDate.value) {
    matchedTxs = matchedTxs.filter((t) => t.date <= filterEndDate.value);
    unmatchedTxs = unmatchedTxs.filter((t) => t.date <= filterEndDate.value);
  }

  if (search.value) {
    const searchLower = search.value.toLowerCase();
    matchedTxs = matchedTxs.filter((t) => t.merchant.toLowerCase().includes(searchLower) || Math.abs(t.amount).toString().toLowerCase().includes(searchLower));
    unmatchedTxs = unmatchedTxs.filter(
      (t) => t.merchant.toLowerCase().includes(searchLower) || Math.abs(t.amount).toString().toLowerCase().includes(searchLower),
    );
  }

  const allTxs = [...matchedTxs, ...unmatchedTxs].sort((a, b) => {
    const dateA = new Date(a.date);
    const dateB = new Date(b.date);
    if (dateA.getTime() == dateB.getTime()) return b.merchant.localeCompare(a.merchant);
    return dateA.getTime() - dateB.getTime(); // Newest first
  });

  // Calculate running balance
  let balance = 0;
  let order = -1;
  let retTrxs = allTxs.map((tx) => {
    balance += tx.amount;
    order += 1;
    return { ...tx, balance, order };
  });

  if (filterMerchant.value) {
    const merchantFilter = filterMerchant.value.toLowerCase();
    retTrxs = retTrxs.filter((t) => t.merchant.toLowerCase().includes(merchantFilter));
  }

  if (filterAmount.value) {
    const amountFilter = parseFloat(filterAmount.value);
    if (!isNaN(amountFilter)) {
      retTrxs = retTrxs.filter((t) => Math.abs(t.amount).toString().includes(amountFilter.toString()));
    }
  }

  if (filterImportedMerchant.value) {
    const importedMerchantFilter = filterImportedMerchant.value.toLowerCase();
    retTrxs = retTrxs.filter((t) => t.merchant.toLowerCase().includes(importedMerchantFilter));
  }

  if (filterStartDate.value) {
    retTrxs = retTrxs.filter((t) => t.date >= filterStartDate.value);
  }

  if (filterEndDate.value) {
    retTrxs = retTrxs.filter((t) => t.date <= filterEndDate.value);
  }

  if (search.value) {
    const searchLower = search.value.toLowerCase();
    retTrxs = retTrxs.filter((t) => t.merchant.toLowerCase().includes(searchLower) || Math.abs(t.amount).toString().toLowerCase().includes(searchLower));
  }

  return retTrxs.sort((a, b) => {
    return b.order - a.order; // Newest first
  });
});

const statementTransactions = computed((): DisplayTransaction[] => {
  if (!selectedAccount.value || !selectedStatement.value) return [];
  const start = selectedStatement.value.startDate;
  const end = selectedStatement.value.endDate;

  const budgetTxs: DisplayTransaction[] = budgets.value
    .flatMap((budget) => (budget.transactions || []).map((tx) => ({ tx, budget })))
    .filter(({ tx }) => tx.accountNumber === selectedAccount.value && tx.date >= start && tx.date <= end && !tx.deleted)
    .map(({ tx, budget }) => ({
      id: tx.id,
      date: tx.date,
      merchant: tx.importedMerchant || tx.merchant || 'N/A',
      amount: tx.isIncome ? tx.amount : -1 * tx.amount,
      status: tx.status || 'U',
      ...(budget.budgetId ? { budgetId: budget.budgetId } : {}),
    }));

  const importedTxs: DisplayTransaction[] = importedTransactions.value
    .filter((itx) => {
      if (itx.accountNumber !== selectedAccount.value || itx.deleted) return false;
      const dateStr = getImportedTransactionDate(itx);
      return dateStr >= start && dateStr <= end;
    })
    .map((itx) => ({
      id: itx.id,
      date: getImportedTransactionDate(itx),
      merchant: itx.payee || 'N/A',
      amount: itx.debitAmount && itx.debitAmount > 0 ? -itx.debitAmount : (itx.creditAmount ?? 0),
      status: itx.status || 'U',
    }));

  return [...budgetTxs, ...importedTxs].sort((a, b) => a.date.localeCompare(b.date));
});

const selectedTransactionsTotal = computed(() => {
  const map = new Map(statementTransactions.value.map((t) => [t.id, t]));
  return selectedRows.value.reduce((sum, id) => {
    const t = map.get(id);
    return sum + (t ? t.amount : 0);
  }, 0);
});

const calculatedEndingBalance = computed(() => {
  if (!selectedStatement.value) return 0;
  return selectedStatement.value.startingBalance + selectedTransactionsTotal.value;
});

const reconcileMatches = computed(() => {
  if (!selectedStatement.value) return false;
  return Math.abs(calculatedEndingBalance.value - selectedStatement.value.endingBalance) < 0.01;
});

onMounted(async () => {
  await loadData();
});

async function loadData() {
  loading.value = true;
  try {
    const user = auth.currentUser;
    if (!user) {
      showSnackbar('Please log in to view transactions', 'negative');
      return;
    }

    // Load family to get entities
    await familyStore.loadFamily(user.uid);

    // Load budgets
    await budgetStore.loadBudgets(user.uid);
    budgets.value = Array.from(budgetStore.budgets.values());

    // Load accounts
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found', 'negative');
      return;
    }
    accounts.value = await dataAccess.getAccounts(family.id);

    accountOptions.value = accounts.value
      .filter((acc) => ['Bank', 'CreditCard', 'Investment'].includes(acc.type) && acc.accountNumber)
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((acc) => ({ title: acc.name, value: acc.accountNumber }));

    // Default to first account if available
    if (accountOptions.value.length > 0) {
      selectedAccount.value = accountOptions.value[0].value;
      await loadTransactions();
    }
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error loading data:', err);
    showSnackbar(`Error loading data: ${err.message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

async function loadImportedTransactions(reset = false) {
  if (!selectedAccount.value || loadingMore.value || !hasMoreImported.value) return;
  if (reset) {
    importedOffset.value = 0;
    importedTransactions.value = [];
    hasMoreImported.value = true;
  }
  loadingMore.value = true;
  try {
    const txs = await dataAccess.getImportedTransactionsByAccountId(selectedAccount.value, importedOffset.value, pageSize);
    importedTransactions.value = [...importedTransactions.value, ...txs];
    importedOffset.value += txs.length;
    if (txs.length < pageSize) {
      hasMoreImported.value = false;
    }
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error loading imported transactions:', err);
    showSnackbar(`Error loading imported transactions: ${err.message}`, 'negative');
  } finally {
    loadingMore.value = false;
  }
}

function onTableVirtualScroll({ to }: { to: number }) {
  if (to >= displayTransactions.value.length - 20) {
    void loadImportedTransactions();
  }
}

async function loadTransactions() {
  if (!selectedAccount.value) return;

  loading.value = true;
  try {
    await loadImportedTransactions(true);
    await statementStore.loadStatements(familyId.value, selectedAccount.value);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    if (statements.value.length > 0) {
      selectedStatementId.value = statements.value[statements.value.length - 1].id;
    } else {
      selectedStatementId.value = 'ALL';
    }
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error loading transactions:', err);
    showSnackbar(`Error loading transactions: ${err.message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

function getEntityName(entityId?: string): string {
  if (!entityId) return 'N/A';
  const entity = familyStore.family?.entities?.find((e) => e.id === entityId);
  if (entity) return entity.name;
  // Fallback: Check if entityId is a budgetId and get entityId from budget
  const budget = budgetStore.getBudget(entityId);
  if (budget?.entityId) {
    const budgetEntity = familyStore.family?.entities?.find((e) => e.id === budget.entityId);
    return budgetEntity ? budgetEntity.name : 'N/A';
  }
  return 'N/A';
}

function getRowClass(item: DisplayTransaction) {
  if (item.status === 'U') return 'status-u';
  if (item.status === 'C') return 'status-c';
  return '';
}

function confirmAction(transaction: DisplayTransaction, action: string) {
  transactionToAction.value = transaction;
  transactionAction.value = action;
  showActionDialog.value = true;
}

async function executeAction() {
  if (!transactionToAction.value) {
    showActionDialog.value = false;
    return;
  }

  loading.value = true;
  try {
    const { budgetId, id } = transactionToAction.value;
    if (!id) {
      showSnackbar('Invalid transaction data', 'negative');
      return;
    }

    let updatedBudgetTx: Transaction;

    const { docId, txId } = splitImportedId(id);

    if (transactionAction.value == 'Disconnect' || budgetId) {
      if (!budgetId) {
        showSnackbar('Invalid transaction data (no budget info)', 'negative');
        return;
      }

      const budget = budgetStore.getBudget(budgetId);
      if (!budget) {
        showSnackbar('Budget not found', 'negative');
        return;
      }

      const budgetTx = budget.transactions.find((tx) => tx.id === id);
      if (!budgetTx) {
        showSnackbar('Budget transaction not found', 'negative');
        return;
      }

      const importedTxIndex = importedTransactions.value.findIndex((tx) => {
        if (tx.accountNumber !== selectedAccount.value || !tx.matched) return false;
        if (tx.payee !== budgetTx.importedMerchant) return false;
        const importedDate = getImportedTransactionDate(tx);
        const budgetEffectiveDate = budgetTx.transactionDate || budgetTx.postedDate || budgetTx.date;
        if (importedDate !== (budgetEffectiveDate || '')) return false;
        return tx.debitAmount == budgetTx.amount || tx.creditAmount == budgetTx.amount;
      });

      const rest: Partial<Transaction> = { ...budgetTx };
      delete rest.accountNumber;
      delete rest.accountSource;
      delete rest.transactionDate;
      delete rest.postedDate;
      delete rest.importedMerchant;
      delete rest.checkNumber;
      updatedBudgetTx = {
        ...rest,
        status: 'U',
        taxMetadata: budgetTx.taxMetadata || [],
      } as Transaction;

      await dataAccess.saveTransaction(budget, updatedBudgetTx);

      const budgetIndex = budgets.value.findIndex((b) => b.budgetId === budgetId);
      if (budgetIndex !== -1) {
        const budgetRef = budgets.value[budgetIndex];
        if (budgetRef) {
          budgetRef.transactions = budgetRef.transactions.map((tx) => (tx.id === id ? updatedBudgetTx : tx));
          budgetStore.updateBudget(budgetId, budgetRef);
        }
      }

      if (importedTxIndex !== -1) {
        const itx = importedTransactions.value[importedTxIndex];
        if (itx) {
          itx.matched = false;
          const { docId: itxDoc, txId: itxId } = splitImportedId(itx.id);
          await dataAccess.updateImportedTransaction(itxDoc, itxId, false, false);
        }
      }
    }

    if (!budgetId && transactionAction.value == 'Delete') {
      await dataAccess.deleteImportedTransaction(docId, txId);
      const importedTxIndex = importedTransactions.value.findIndex((tx) => tx.id === id);
      if (importedTxIndex !== -1) {
        const itx = importedTransactions.value[importedTxIndex];
        if (itx) {
          itx.deleted = true;
        }
      }
    } else if (!budgetId && transactionAction.value == 'Ignore') {
      await dataAccess.updateImportedTransaction(docId, txId, false, true);
      const importedTxIndex = importedTransactions.value.findIndex((tx) => tx.id === id);
      if (importedTxIndex !== -1) {
        const itx = importedTransactions.value[importedTxIndex];
        if (itx) {
          itx.matched = false;
          itx.ignored = true;
        }
      }
    }

    showSnackbar(`${transactionAction.value} action completed successfully`, 'success');
  } catch (error: unknown) {
    const err = error as Error;
    console.error(`Error performing ${transactionAction.value} action:`, err);
    showSnackbar(`Error performing ${transactionAction.value} action: ${err.message}`, 'negative');
  } finally {
    loading.value = false;
    showActionDialog.value = false;
    transactionToAction.value = null;
    transactionAction.value = '';
  }
}

function openBatchMatchDialog() {
  if (
    selectedRows.value.length === 0 ||
    !selectedRows.value.every((id) => {
      const tx = displayTransactions.value?.find((t) => t.id === id);
      return tx && tx.status === 'U';
    })
  ) {
    showSnackbar('Please select unmatched transactions to match', 'negative');
    return;
  }
  if (!entityOptions.value.length) {
    showSnackbar('No entities available. Please set up entities first.', 'negative');
    return;
  }
  batchEntries.value = selectedRows.value.map((id) => {
    const tx = displayTransactions.value?.find((t) => t.id === id);
    return {
      id,
      date: tx?.date || '',
      amount: tx?.amount || 0,
      isIncome: tx?.isIncome || false,
      merchant: filterMerchant.value || (tx ? tx.merchant : ''),
      category: '',
    };
  });
  selectedEntityId.value = familyStore.selectedEntityId || '';
  showBatchMatchDialog.value = true;
}

function onSingleRowClick(_: unknown, row: DisplayTransaction) {
  if (row.status === 'U' && !row.budgetId) {
    selectedRows.value = [row.id];
    openBatchMatchDialog();
  }
}

async function executeBatchMatch() {
  if (!batchMatchForm.value) return;

  const valid = await batchMatchForm.value.validate();
  if (!valid) {
    showSnackbar('Please fill in all required fields', 'negative');
    return;
  }

  saving.value = true;
  try {
    const user = auth.currentUser;
    if (!user) {
      showSnackbar('User not authenticated', 'negative');
      return;
    }

    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found', 'negative');
      return;
    }

    const ownerUid = family.ownerUid || user.uid;

    for (const entry of batchEntries.value) {
      const txId = entry.id;
      const transaction = displayTransactions.value?.find((t) => t.id === txId);
      if (!transaction) {
        console.error(`Transaction with ID ${txId} not found in displayTransactions`);
        continue;
      }

      const { id, date, amount, isIncome } = transaction;
      const { docId, txId: importedTxId } = splitImportedId(id);
      const importedTx = importedTransactions.value.find((tx) => tx.id === id);
      if (!importedTx) {
        console.error(`Imported transaction ${id} not found`);
        continue;
      }

      // Determine budget month from postedDate
      const budgetMonth = date.slice(0, 7); // YYYY-MM

      // Check if a budget exists for this month/entity
      let targetBudget = Array.from(budgetStore.budgets.values()).find(
        (b) => b.entityId === selectedEntityId.value && b.month === budgetMonth,
      );
      if (!targetBudget) {
        // Create new budget by copying the most recent previous budget
        targetBudget = await createBudgetForMonth(budgetMonth, family.id, ownerUid, selectedEntityId.value);
        budgets.value = Array.from(budgetStore.budgets.values());
      }
      const budgetId = targetBudget.budgetId;

      // Check if category exists in budget; if not, add it
      if (!targetBudget.categories.some((cat) => cat.name === entry.category)) {
        targetBudget.categories.push({
          name: entry.category,
          target: 0,
          isFund: false,
          group: 'Ungrouped',
        });
        await dataAccess.saveBudget(budgetId, targetBudget);
        budgetStore.updateBudget(budgetId, targetBudget);
        budgets.value = Array.from(budgetStore.budgets.values());
      }

      // Create new budget transaction
      const newTransaction: Transaction = {
        id: uuidv4(),
        date: date,
        budgetMonth: budgetMonth,
        merchant: entry.merchant,
        categories: [{ category: entry.category, amount: Math.abs(amount) }],
        amount: Math.abs(amount),
        notes: '',
        recurring: false,
        recurringInterval: 'Monthly',
        userId: user.uid,
        isIncome: !!isIncome,
        accountSource: importedTx.accountSource || '',
        accountNumber: importedTx.accountNumber || '',
        transactionDate: importedTx.transactionDate || (importedTx.postedDate || ''),
        postedDate: importedTx.postedDate || '',
        checkNumber: importedTx.checkNumber || '',
        importedMerchant: importedTx.payee || '',
        status: 'C',
        entityId: selectedEntityId.value,
        taxMetadata: [],
      };

      // Save budget transaction
      await dataAccess.saveTransaction(targetBudget, newTransaction);

      // Update local budgets
      const budgetIndex = budgets.value.findIndex((b) => b.budgetId === budgetId);
      if (budgetIndex !== -1) {
        const b = budgets.value[budgetIndex];
        if (b) {
          b.transactions = [...(b.transactions || []), newTransaction];
          budgetStore.updateBudget(budgetId, b);
        }
      } else {
        budgets.value.push({ ...targetBudget, transactions: [newTransaction] });
        budgetStore.updateBudget(budgetId, { ...targetBudget, transactions: [newTransaction] });
      }

      // Mark imported transaction as matched
      importedTx.matched = true;
      await dataAccess.updateImportedTransaction(docId, importedTxId, true, false);

      // Update local imported transactions
      const importedTxIndex = importedTransactions.value.findIndex((tx) => tx.id === id);
      if (importedTxIndex !== -1) {
        const itx = importedTransactions.value[importedTxIndex];
        if (itx) {
          itx.matched = true;
        }
      }
    }

    showSnackbar(`Successfully matched ${selectedRows.value.length} transaction${selectedRows.value.length > 1 ? 's' : ''}`, 'success');
    selectedRows.value = [];
    showBatchMatchDialog.value = false;
    selectedEntityId.value = '';
    batchEntries.value = [];
    const acct = selectedAccount.value;
    await loadData(); // Refresh data to reflect changes
    selectedAccount.value = acct;
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error performing batch match:', err);
    showSnackbar(`Error performing batch match: ${err.message}`, 'negative');
  } finally {
    saving.value = false;
  }
}

function confirmBatchAction(action: string) {
  if (
    selectedRows.value.length === 0 ||
    !selectedRows.value.every((id) => {
      const tx = displayTransactions.value?.find((t) => t.id === id);
      return tx && tx.status === 'U' && !tx.budgetId;
    })
  ) {
    showSnackbar('Please select unmatched imported transactions', 'negative');
    return;
  }
  batchAction.value = action;
  showBatchActionDialog.value = true;
}

async function executeBatchAction() {
  if (!['Ignore', 'Delete'].includes(batchAction.value)) {
    showBatchActionDialog.value = false;
    return;
  }

  saving.value = true;
  try {
    for (const txId of selectedRows.value) {
      const tx = displayTransactions.value?.find((t) => t.id === txId);
      if (!tx || tx.status !== 'U' || tx.budgetId) continue;
      const { docId, txId: importedId } = splitImportedId(tx.id);
      if (batchAction.value === 'Delete') {
        await dataAccess.deleteImportedTransaction(docId, importedId);
        const index = importedTransactions.value.findIndex((itx) => itx.id === tx.id);
        if (index !== -1) importedTransactions.value.splice(index, 1);
      } else if (batchAction.value === 'Ignore') {
        await dataAccess.updateImportedTransaction(docId, importedId, false, true);
        const index = importedTransactions.value.findIndex((itx) => itx.id === tx.id);
        if (index !== -1) {
          const itx = importedTransactions.value[index];
          if (itx) itx.ignored = true;
        }
      }
    }
    showSnackbar(
      `Successfully ${batchAction.value.toLowerCase()}d ${selectedRows.value.length} transaction${selectedRows.value.length > 1 ? 's' : ''}`,
      'success',
    );
    selectedRows.value = [];
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error performing batch action:', err);
    showSnackbar(`Error performing batch action: ${err.message}`, 'negative');
  } finally {
    saving.value = false;
    showBatchActionDialog.value = false;
    batchAction.value = '';
  }
}

function openBalanceAdjustmentDialog() {
  if (currentBalance.value !== latestTransactionBalance.value) {
    adjustmentAmount.value = currentBalance.value - latestTransactionBalance.value;
  } else {
    adjustmentAmount.value = 0;
  }
  showBalanceAdjustmentDialog.value = true;
}

function startReconcile() {
  selectedRows.value = statementTransactions.value.map((t) => t.id);
  reconciling.value = true;
}

function cancelReconcile() {
  reconciling.value = false;
}

async function saveBalanceAdjustment() {
  if (!adjustmentForm.value || !selectedAccount.value) return;

  const valid = await adjustmentForm.value.validate();
  if (!valid) {
    showSnackbar('Please fill in all required fields', 'negative');
    return;
  }

  saving.value = true;
  try {
    const user = auth.currentUser;
    if (!user) {
      showSnackbar('User not authenticated', 'negative');
      return;
    }

    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found', 'negative');
      return;
    }

    const selectedAcc = accounts.value.find((acc) => acc.accountNumber === selectedAccount.value);
    if (!selectedAcc) {
      showSnackbar('Selected account not found', 'negative');
      return;
    }

    const newDocId = uuidv4();
    const adjustmentTransaction: ImportedTransaction = {
      id: `${newDocId}-0`,
      accountId: selectedAcc.id,
      accountNumber: selectedAccount.value,
      accountSource: selectedAcc.institution || '',
      payee: 'Initial Balance Adjustment',
      postedDate: adjustmentDate.value,
      status: 'U',
      creditAmount: adjustmentAmount.value >= 0 ? Math.abs(adjustmentAmount.value) : 0,
      debitAmount: adjustmentAmount.value < 0 ? Math.abs(adjustmentAmount.value) : 0,
      checkNumber: '',
      matched: false,
      ignored: true,
    };

    const importedDoc: ImportedTransactionDoc = {
      id: newDocId,
      userId: user.uid,
      familyId: family.id,
      importedTransactions: [adjustmentTransaction],
    };

    await dataAccess.saveImportedTransactions(importedDoc);
    importedTransactions.value.push(adjustmentTransaction);

    showSnackbar('Balance adjustment saved successfully', 'success');
    closeBalanceAdjustmentDialog();
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error saving balance adjustment:', err);
    showSnackbar(`Error saving balance adjustment: ${err.message}`, 'negative');
  } finally {
    saving.value = false;
  }
}

function closeBalanceAdjustmentDialog() {
  showBalanceAdjustmentDialog.value = false;
  adjustmentAmount.value = 0;
  adjustmentDate.value = todayISO();
}

async function refreshData() {
  const prevAccount = selectedAccount.value;
  const prevStatement = selectedStatementId.value;
  await loadData();
  if (prevAccount) {
    selectedAccount.value = prevAccount;
    await loadTransactions();
    if (prevStatement === 'ALL' || (prevStatement && statements.value.some((s) => s.id === prevStatement))) {
      selectedStatementId.value = prevStatement;
    }
  }
}

function openStatementDialog() {
  showStatementDialog.value = true;
  if (statements.value.length > 0) {
    const last = statements.value[statements.value.length - 1];
    if (last) newStatement.value.startDate = last.endDate;
  }
}

async function saveStatement() {
  if (!statementForm.value || !selectedAccount.value) return;

  const valid = await statementForm.value.validate();
  if (!valid) {
    showSnackbar('Please fill in all required fields', 'negative');
    return;
  }

  saving.value = true;
  try {
    const st: Statement = {
      ...newStatement.value,
      id: uuidv4(),
      accountNumber: selectedAccount.value,
      reconciled: false,
    };
    await statementStore.saveStatement(familyId.value, selectedAccount.value, st, []);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    selectedStatementId.value = st.id;
    showSnackbar('Statement saved successfully', 'success');
    closeStatementDialog();
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error saving statement:', err);
    showSnackbar(`Error saving statement: ${err.message}`, 'negative');
  } finally {
    saving.value = false;
  }
}

function closeStatementDialog() {
  showStatementDialog.value = false;
  newStatement.value = {
    id: '',
    accountNumber: '',
    startDate: '',
    startingBalance: 0,
    endDate: '',
    endingBalance: 0,
    reconciled: false,
  };
}

async function markStatementReconciled() {
  if (!selectedStatement.value || !selectedAccount.value) return;
  saving.value = true;
  try {
    const txRefs: { budgetId: string; transactionId: string }[] = [];

    for (const budget of budgets.value) {
      let updated = false;
      for (const tx of budget.transactions) {
        if (selectedRows.value.includes(tx.id)) {
          tx.status = 'R';
          if (budget.budgetId) {
            txRefs.push({ budgetId: budget.budgetId, transactionId: tx.id });
            updated = true;
          }
        }
      }
      if (updated && budget.budgetId) {
        budgetStore.updateBudget(budget.budgetId, budget);
      }
    }

    for (const id of selectedRows.value) {
      const idx = importedTransactions.value.findIndex((itx) => itx.id === id);
      if (idx !== -1) {
        const { docId, txId } = splitImportedId(id);
        const baseItx = importedTransactions.value[idx];
        if (baseItx) {
          const updatedTx: ImportedTransaction = { ...baseItx, id: txId, status: 'R' };
          importedTransactions.value[idx].status = 'R';
          await dataAccess.updateImportedTransaction(docId, updatedTx);
        }
      }
    }

    const updated: Statement = { ...selectedStatement.value, reconciled: true };
    await statementStore.saveStatement(familyId.value, selectedAccount.value, updated, txRefs);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    showSnackbar('Statement reconciled', 'success');
    reconciling.value = false;
    selectedRows.value = [];
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error reconciling statement:', err);
    showSnackbar(`Error reconciling statement: ${err.message}`, 'negative');
  } finally {
    saving.value = false;
  }
}

async function unreconcileStatement() {
  if (!selectedStatement.value || !selectedAccount.value) return;
  saving.value = true;
  try {
    const txRefs: { budgetId: string; transactionId: string }[] = [];

    for (const budget of budgets.value) {
      let updated = false;
      for (const tx of budget.transactions) {
        if (
          tx.accountNumber === selectedAccount.value &&
          tx.date >= selectedStatement.value.startDate &&
          tx.date <= selectedStatement.value.endDate &&
          tx.status === 'R'
        ) {
          tx.status = 'C';
          if (budget.budgetId) {
            txRefs.push({ budgetId: budget.budgetId, transactionId: tx.id });
            updated = true;
          }
        }
      }
      if (updated && budget.budgetId) budgetStore.updateBudget(budget.budgetId, budget);
    }

    for (const itx of importedTransactions.value) {
      const dateStr = getImportedTransactionDate(itx);
      if (
        itx.accountNumber === selectedAccount.value &&
        dateStr >= selectedStatement.value.startDate &&
        dateStr <= selectedStatement.value.endDate &&
        itx.status === 'R'
      ) {
        const { docId, txId } = splitImportedId(itx.id);
        const updatedTx: ImportedTransaction = { ...itx, id: txId, status: 'C' };
        itx.status = 'C';
        await dataAccess.updateImportedTransaction(docId, updatedTx);
      }
    }

    await statementStore.unreconcileStatement(familyId.value, selectedAccount.value, selectedStatement.value.id, txRefs);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    showSnackbar('Statement unreconciled', 'success');
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error unreconciling statement:', err);
    showSnackbar(`Error unreconciling statement: ${err.message}`, 'negative');
  } finally {
    saving.value = false;
  }
}

async function deleteStatement() {
  if (!selectedStatement.value || !selectedAccount.value) return;
  if (!confirm('Delete this statement?')) return;
  saving.value = true;
  try {
    const txRefs: { budgetId: string; transactionId: string }[] = [];

    for (const budget of budgets.value) {
      let updated = false;
      for (const tx of budget.transactions) {
        if (
          tx.accountNumber === selectedAccount.value &&
          tx.date >= selectedStatement.value.startDate &&
          tx.date <= selectedStatement.value.endDate &&
          tx.status === 'R'
        ) {
          tx.status = 'C';
          if (budget.budgetId) {
            txRefs.push({ budgetId: budget.budgetId, transactionId: tx.id });
            updated = true;
          }
        }
      }
      if (updated && budget.budgetId) budgetStore.updateBudget(budget.budgetId, budget);
    }

    for (const itx of importedTransactions.value) {
      const dateStr = getImportedTransactionDate(itx);
      if (
        itx.accountNumber === selectedAccount.value &&
        dateStr >= selectedStatement.value.startDate &&
        dateStr <= selectedStatement.value.endDate &&
        itx.status === 'R'
      ) {
        const { docId, txId } = splitImportedId(itx.id);
        const updatedTx: ImportedTransaction = { ...itx, id: txId, status: 'C' };
        itx.status = 'C';
        await dataAccess.updateImportedTransaction(docId, updatedTx);
      }
    }

    await statementStore.deleteStatement(familyId.value, selectedAccount.value, selectedStatement.value.id, txRefs);
    await statementStore.loadStatements(familyId.value, selectedAccount.value);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    if (statements.value.length > 0) {
      const lastStatement = statements.value[statements.value.length - 1];
      selectedStatementId.value = lastStatement ? lastStatement.id : 'ALL';
    } else {
      selectedStatementId.value = 'ALL';
    }
    showSnackbar('Statement deleted', 'success');
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error deleting statement:', err);
    showSnackbar(`Error deleting statement: ${err.message}`, 'negative');
  } finally {
    saving.value = false;
  }
}

function downloadCsv() {
  const rows = displayTransactions.value.map((tx) => ({
    id: tx.id,
    date: tx.date,
    merchant: tx.merchant,
    category: tx.category,
    entity: getEntityName(tx.entityId || tx.budgetId || ''),
    amount: tx.amount,
    isIncome: tx.isIncome ? 'true' : 'false',
    status: tx.status,
    notes: tx.notes,
    balance: tx.balance ?? '',
  }));
  const csv = Papa.unparse(rows);
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const account = accounts.value.find((a) => a.accountNumber === selectedAccount.value);
  const name = account ? account.name.replace(/[^a-zA-Z0-9_-]/g, '_').toLowerCase() : 'transactions';
  const today = todayISO();
  saveAs(blob, `${name}_${today}.csv`);
}

function showSnackbar(text: string, color = 'success') {
  $q.notify({
    message: text,
    color,
    position: 'bottom',
    timeout: 3000,
    actions: [{ label: 'Close', color: 'white', handler: () => {} }],
  });
}

function applyFilters() {
  // Trigger recomputation of displayTransactions
}
</script>

<style scoped>
.status-u {
  background-color: #fff9e6;
}
.status-c {
  background-color: #e6f4ff;
}
</style>
