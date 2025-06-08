<!-- src/components/TransactionRegistry.vue -->
<template>
  <q-page-container>
    <q-row>
      <q-col :cols="12" :md="4">
        <q-select
          v-model="selectedAccount"
          :options="accountOptions"
          label="Select Account"
          outlined
          dense
          option-label="title"
          option-value="value"
          @update:model-value="loadTransactions"
        />
      </q-col>
      <q-col class="flex items-center">
        <q-btn color="primary" flat :loading="loading" @click="refreshData">
          <q-icon name="refresh" left />
          Refresh
        </q-btn>
      </q-col>
    </q-row>
    <q-row v-if="selectedAccount">
      <q-col :cols="12" :md="4">
        <q-select
          v-model="selectedStatementId"
          :options="statementOptions"
          label="Select Statement"
          outlined
          dense
          option-label="title"
          option-value="id"
        />
      </q-col>
      <q-col>
        <q-btn color="primary" @click="openStatementDialog">Add Statement</q-btn>
      </q-col>
      <q-col v-if="selectedStatement">
        <q-btn color="primary" @click="startReconcile">Reconcile Statement</q-btn>
      </q-col>
      <q-col v-if="selectedStatement && selectedStatement.reconciled">
        <q-btn color="primary" @click="unreconcileStatement">Unreconcile</q-btn>
      </q-col>
      <q-col v-if="selectedStatement">
        <q-btn color="negative" @click="deleteStatement">Delete Statement</q-btn>
      </q-col>
    </q-row>

    <!-- Balance Display -->
    <q-card class="q-mb-md" v-if="selectedAccount">
      <q-card-section>
        <div class="text-h6">Account Balance</div>
      </q-card-section>
      <q-card-section>
        <q-row>
          <q-col>
            <span class="text-h6">Balance from Accounts: </span>
            <span
              :class="{
                'text-positive': currentBalance === latestTransactionBalance,
                'text-negative': currentBalance !== latestTransactionBalance,
              }"
            >
              {{ formatCurrency(currentBalance) }}
            </span>
            <q-tooltip v-if="currentBalance !== latestTransactionBalance">
              The current balance differs from the transaction registry balance ({{
                formatCurrency(latestTransactionBalance)
              }}) by {{ formatCurrency(Math.abs(currentBalance - latestTransactionBalance)) }}.
              Adjust Balance to reconcile.
            </q-tooltip>
          </q-col>
          <q-col v-if="currentBalance !== latestTransactionBalance">
            <q-btn color="primary" @click="openBalanceAdjustmentDialog">Adjust Balance</q-btn>
          </q-col>
        </q-row>
      </q-card-section>
    </q-card>

    <q-card class="q-mb-md" v-if="selectedStatement">
      <q-card-section>
        <div class="text-h6">Statement Totals</div>
      </q-card-section>
      <q-card-section>
        <p>Start Balance: {{ formatCurrency(selectedStatement.startingBalance) }}</p>
        <p>End Balance: {{ formatCurrency(selectedStatement.endingBalance) }}</p>
      </q-card-section>
    </q-card>

    <!-- Loading Overlay -->
    <q-inner-loading :showing="loading">
      <q-spinner color="primary" size="50px" />
    </q-inner-loading>

    <q-card class="q-mb-md">
      <q-card-section>
        <div class="text-h6">Filters</div>
      </q-card-section>
      <q-card-section>
        <q-row>
          <q-col :cols="6" :md="6">
            <q-input v-model="search" label="Search" outlined dense @input="applyFilters">
              <template v-slot:append>
                <q-icon name="search" />
              </template>
            </q-input>
          </q-col>
          <q-col>
            <q-checkbox
              v-model="filterMatched"
              label="Show Only Unmatched"
              dense
              @input="applyFilters"
            />
          </q-col>
        </q-row>
        <q-row>
          <q-col :cols="12" :md="2">
            <q-input
              v-model="filterMerchant"
              label="Merchant"
              outlined
              dense
              @input="applyFilters"
            />
          </q-col>
          <q-col :cols="12" :md="2">
            <q-input
              v-model.number="filterAmount"
              type="number"
              label="Amount"
              outlined
              dense
              @input="applyFilters"
            />
          </q-col>
          <q-col :cols="12" :md="3">
            <q-input
              v-model="filterImportedMerchant"
              label="Imported Merchant"
              outlined
              dense
              @input="applyFilters"
            />
          </q-col>
          <q-col :cols="12" :md="5">
            <q-row>
              <q-col :cols="12" :md="6">
                <q-input
                  v-model="filterStartDate"
                  type="date"
                  label="Start Date"
                  outlined
                  dense
                  clearable
                  @input="applyFilters"
                />
              </q-col>
              <q-col :cols="12" :md="6">
                <q-input
                  v-model="filterEndDate"
                  type="date"
                  label="End Date"
                  outlined
                  dense
                  clearable
                  @input="applyFilters"
                />
              </q-col>
            </q-row>
          </q-col>
        </q-row>
      </q-card-section>
    </q-card>

    <q-card v-if="!loading">
      <q-card-section>
        <q-row>
          <q-col>Transaction Registry</q-col>
          <q-col>
            <q-btn
              icon="download"
              flat
              @click="downloadCsv"
              :disable="displayTransactions.length === 0"
            >
              <q-tooltip>Download CSV</q-tooltip>
            </q-btn>
          </q-col>
        </q-row>
      </q-card-section>
      <q-card-section>
        <q-btn
          v-if="selectedRows.length > 0"
          color="primary"
          @click="openBatchMatchDialog"
          class="q-mb-md"
          :disable="
            loading ||
            !selectedRows.every((id) => {
              const tx = displayTransactions.find((t: DisplayTransaction) => t.id === id);
              return tx && tx.status === 'U';
            })
          "
        >
          Batch Match {{ selectedRows.length }} Transaction{{ selectedRows.length > 1 ? 's' : '' }}
        </q-btn>
        <q-btn
          v-if="selectedRows.length > 0"
          color="warning"
          class="q-mb-md q-ml-sm"
          @click="confirmBatchAction('Ignore')"
          :disable="
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
          class="q-mb-md q-ml-sm"
          @click="confirmBatchAction('Delete')"
          :disable="
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
        v-model:selected="selectedRows"
        :columns="headers"
        :rows="displayTransactions"
        row-key="id"
        :pagination="{ rowsPerPage: 0 }"
        selection="multiple"
        :selected-rows-label="(item) => item.status === 'U'"
        :style="{ height: '600px' }"
        :class="getRowClass"
      >
        <template v-slot:body-cell-amount="props">
          <q-td :props="props">
            <span :class="props.row.isIncome ? 'text-positive' : 'text-negative'">
              {{ formatCurrency(props.row.amount) }}
            </span>
          </q-td>
        </template>
        <template v-slot:body-cell-balance="props">
          <q-td :props="props">
            {{ formatCurrency(props.row.balance) }}
          </q-td>
        </template>
        <template v-slot:body-cell-entity="props">
          <q-td :props="props">
            {{ getEntityName(props.row.entityId || props.row.budgetId) }}
          </q-td>
        </template>
        <template v-slot:body-cell-actions="props">
          <q-td :props="props">
            <q-btn
              v-if="props.row.status === 'C'"
              icon="link_off"
              flat
              dense
              color="warning"
              @click.stop="confirmAction(props.row, 'Disconnect')"
            >
              <q-tooltip>Disconnect Transaction</q-tooltip>
            </q-btn>
            <q-btn
              v-if="props.row.status !== 'C' && props.row.id"
              icon="visibility_off"
              flat
              dense
              color="negative"
              @click.stop="confirmAction(props.row, 'Ignore')"
            >
              <q-tooltip>Ignore Imported Transaction</q-tooltip>
            </q-btn>
            <q-btn
              v-if="props.row.status !== 'C' && props.row.id"
              icon="delete"
              flat
              dense
              color="negative"
              @click.stop="confirmAction(props.row, 'Delete')"
            >
              <q-tooltip>Delete Imported Transaction</q-tooltip>
            </q-btn>
          </q-td>
        </template>
      </q-table>
    </q-card>

    <!-- Action Confirmation Dialog -->
    <q-dialog v-model="showActionDialog" @keyup.enter="executeAction">
      <q-card>
        <q-card-section class="bg-warning text-white">
          {{ transactionAction }} Transaction
        </q-card-section>
        <q-card-section v-if="transactionAction === 'Disconnect'">
          Are you sure you want to disconnect the transaction for "{{
            transactionToAction?.merchant
          }}" on {{ transactionToAction?.date }} from its imported transaction?
        </q-card-section>
        <q-card-section v-else>
          Are you sure you want to {{ transactionAction }} the transaction for "{{
            transactionToAction?.merchant
          }}" on {{ transactionToAction?.date }}?
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" v-close-popup />
          <q-btn flat :label="transactionAction" color="warning" @click="executeAction" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Batch Match Dialog -->
    <q-dialog v-model="showBatchMatchDialog" @keyup.enter="executeBatchMatch">
      <q-card>
        <q-card-section class="bg-primary text-white"> Batch Match Transactions </q-card-section>
        <q-card-section>
          <q-form ref="batchMatchForm">
            <p>
              Assign an entity, merchant, and category for {{ selectedRows.length }} unmatched
              transaction{{ selectedRows.length > 1 ? 's' : '' }}.
            </p>
            <q-row>
              <q-col>
                <q-select
                  v-model="selectedEntityId"
                  :options="entityOptions"
                  label="Select Entity"
                  outlined
                  dense
                  :rules="requiredField"
                  option-label="name"
                  option-value="id"
                />
              </q-col>
            </q-row>
            <q-row>
              <q-col>
                <q-input
                  v-model="batchMerchant"
                  label="Merchant"
                  outlined
                  dense
                  :rules="requiredField"
                />
              </q-col>
            </q-row>
            <q-row>
              <q-col>
                <q-select
                  v-model="batchCategory"
                  :options="categoryOptions"
                  label="Category"
                  outlined
                  dense
                  :rules="requiredField"
                  use-input
                  input-debounce="0"
                />
              </q-col>
            </q-row>
          </q-form>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" v-close-popup />
          <q-btn flat label="Match" color="primary" @click="executeBatchMatch" :loading="saving" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Add Statement Dialog -->
    <q-dialog v-model="showStatementDialog">
      <q-card>
        <q-card-section class="bg-primary text-white"> Add Statement </q-card-section>
        <q-card-section>
          <q-form ref="statementForm">
            <q-row>
              <q-col>
                <q-input
                  v-model="newStatement.startDate"
                  type="date"
                  label="Start Date"
                  outlined
                  dense
                  :rules="requiredField"
                />
              </q-col>
              <q-col>
                <q-input
                  v-model.number="newStatement.startingBalance"
                  type="number"
                  label="Starting Balance"
                  outlined
                  dense
                  :rules="requiredField"
                />
              </q-col>
            </q-row>
            <q-row>
              <q-col>
                <q-input
                  v-model="newStatement.endDate"
                  type="date"
                  label="End Date"
                  outlined
                  dense
                  :rules="requiredField"
                />
              </q-col>
              <q-col>
                <q-input
                  v-model.number="newStatement.endingBalance"
                  type="number"
                  label="Ending Balance"
                  outlined
                  dense
                  :rules="requiredField"
                />
              </q-col>
            </q-row>
          </q-form>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" v-close-popup />
          <q-btn flat label="Save" color="primary" @click="saveStatement" :loading="saving" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Batch Action Dialog -->
    <q-dialog v-model="showBatchActionDialog" @keyup.enter="executeBatchAction">
      <q-card>
        <q-card-section class="bg-warning text-white">
          {{ batchAction }} Selected Transactions
        </q-card-section>
        <q-card-section>
          Are you sure you want to {{ batchAction.toLowerCase() }}
          {{ selectedRows.length }} transaction{{ selectedRows.length > 1 ? 's' : '' }}?
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" v-close-popup />
          <q-btn
            flat
            :label="batchAction"
            color="warning"
            @click="executeBatchAction"
            :loading="saving"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Balance Adjustment Dialog -->
    <q-dialog v-model="showBalanceAdjustmentDialog">
      <q-card>
        <q-card-section class="bg-primary text-white"> Adjust Initial Balance </q-card-section>
        <q-card-section>
          <q-form ref="adjustmentForm" @submit="saveBalanceAdjustment">
            <q-row>
              <q-col>
                <p>
                  The current account balance ({{ formatCurrency(currentBalance) }}) differs from
                  the transaction registry balance ({{ formatCurrency(latestTransactionBalance) }}).
                </p>
                <p>Enter the adjustment amount to reconcile the balance:</p>
              </q-col>
            </q-row>
            <q-row>
              <q-col>
                <q-input
                  v-model.number="adjustmentAmount"
                  type="number"
                  label="Adjustment Amount"
                  outlined
                  dense
                  :rules="adjustmentRules"
                  hint="Positive to increase balance, negative to decrease"
                />
              </q-col>
            </q-row>
            <q-row>
              <q-col>
                <q-input
                  v-model="adjustmentDate"
                  type="date"
                  label="Adjustment Date"
                  outlined
                  dense
                  :rules="requiredField"
                />
              </q-col>
            </q-row>
            <q-btn type="submit" color="primary" :loading="saving">Save Adjustment</q-btn>
            <q-btn flat label="Cancel" color="grey" v-close-popup class="q-ml-sm" />
          </q-form>
        </q-card-section>
      </q-card>
    </q-dialog>

    <!-- Reconcile Summary -->
    <q-card class="q-mt-md" v-if="reconciling && selectedStatement">
      <q-card-section class="bg-primary text-white"> Reconcile Statement </q-card-section>
      <q-card-section>
        <p>
          Select transactions to reconcile for
          {{ selectedStatement.startDate }} - {{ selectedStatement.endDate }}
        </p>
        <div class="q-mt-md">
          <p>Starting Balance: {{ formatCurrency(selectedStatement.startingBalance) }}</p>
          <p>Selected Total: {{ formatCurrency(selectedTransactionsTotal) }}</p>
          <p>Calculated Ending Balance: {{ formatCurrency(calculatedEndingBalance) }}</p>
          <p :class="{ 'text-negative': !reconcileMatches }">
            Statement Ending Balance: {{ formatCurrency(selectedStatement.endingBalance) }}
          </p>
          <q-banner v-if="!reconcileMatches" class="bg-warning text-white q-mt-sm">
            Calculated ending balance does not match statement ending balance.
          </q-banner>
        </div>
      </q-card-section>
      <q-card-actions align="right">
        <q-btn flat label="Cancel" color="grey" @click="cancelReconcile" />
        <q-btn
          flat
          label="Reconcile"
          color="primary"
          @click="markStatementReconciled"
          :loading="saving"
        />
      </q-card-actions>
    </q-card>

    <q-banner v-model="snackbar" :class="snackbarColor" auto-close timeout="3000">
      {{ snackbarText }}
      <template v-slot:action>
        <q-btn flat label="Close" @click="snackbar = false" />
      </template>
    </q-banner>
  </q-page-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { Notify } from "quasar";
import { auth } from '../firebase/index';
import { dataAccess } from '../dataAccess';
import Papa from 'papaparse';
import { saveAs } from 'file-saver';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { useStatementStore } from '../store/statements';
import { useUIStore } from '../store/ui';
import {
  Transaction,
  ImportedTransaction,
  Budget,
  Account,
  ImportedTransactionDoc,
  Statement,
} from '../types';
import { formatCurrency, toBudgetMonth, adjustTransactionDate, todayISO } from '../utils/helpers';
import { v4 as uuidv4 } from 'uuid';
import { DEFAULT_BUDGET_TEMPLATES } from '../constants/budgetTemplates';

// Interface for displayTransactions items
interface DisplayTransaction {
  id: string;
  date: string;
  merchant: string;
  category: string;
  entityId: string;
  amount: number;
  isIncome: boolean;
  status: string;
  notes: string;
  balance?: number;
  order?: number;
  budgetId?: string;
}

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();
const statementStore = useStatementStore();
const familyId = computed(() => familyStore.family?.id || '');

const loading = ref(false);
const saving = ref(false);
const uiStore = useUIStore();
const {
  selectedAccount,
  search,
  filterMerchant,
  filterMatched,
  filterAmount,
  filterImportedMerchant,
  filterStartDate,
  filterEndDate,
} = storeToRefs(uiStore);
const budgets = ref<Budget[]>([]);
const importedTransactions = ref<ImportedTransaction[]>([]);
const accounts = ref<Account[]>([]);
const accountOptions = ref<{ title: string; value: string }[]>([]);
const statements = ref<Statement[]>([]);
const statementOptions = computed(() =>
  statements.value.map((s) => ({
    title: `${s.startDate} - ${s.endDate}`,
    id: s.id,
  })),
);
const selectedStatementId = ref<string | null>(null);
const snackbar = ref(false);
const snackbarText = ref('');
const snackbarColor = ref('success');
const showActionDialog = ref(false);
const transactionToAction = ref<any | null>(null);
const transactionAction = ref('');
const showBalanceAdjustmentDialog = ref(false);
const adjustmentAmount = ref<number>(0);
const adjustmentDate = ref<string>(todayISO());
const adjustmentForm = ref<any | null>(null); // Quasar doesn't have a specific QForm type
const selectedRows = ref<string[]>([]);
const showBatchMatchDialog = ref(false);
const batchMatchForm = ref<any | null>(null);
const batchMerchant = ref('');
const batchCategory = ref('');
const selectedEntityId = ref<string>('');
const showBatchActionDialog = ref(false);
const batchAction = ref<string>('');
const showStatementDialog = ref(false);
const statementForm = ref<any | null>(null);
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
const selectedStatement = computed(
  () => statements.value.find((s) => s.id === selectedStatementId.value) || null,
);
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
const requiredField = [(value: any) => !!value || 'This field is required'];
const adjustmentRules = [
  (value: number) => value !== undefined || 'Amount is required',
  (value: number) => !isNaN(value) || 'Amount must be a number',
];

// Computed properties
const currentBalance = computed(() => {
  if (!selectedAccount.value) return 0;
  const account = accounts.value.find((acc) => acc.accountNumber === selectedAccount.value);
  return account ? account.balance || 0 : 0;
});

const latestTransactionBalance = computed(() => {
  if (!displayTransactions.value?.length) return 0;
  if (selectedAccount.value && accounts.value) {
    const aInfo = accounts.value.filter((a) => (a.accountNumber ?? '') == selectedAccount.value);
    if (aInfo && aInfo.length > 0 && (aInfo[0].type == 'CreditCard' || aInfo[0].type == 'Loan')) {
      return Math.abs(displayTransactions.value[0].balance) || 0;
    }
  }
  return displayTransactions.value[0].balance || 0;
});

const headers = [
  { name: 'date', label: 'Date', field: 'date' },
  { name: 'merchant', label: 'Merchant', field: 'merchant' },
  { name: 'category', label: 'Category', field: 'category' },
  { name: 'entity', label: 'Entity', field: 'entity' },
  { name: 'amount', label: 'Amount', field: 'amount' },
  { name: 'status', label: 'Status', field: 'status' },
  { name: 'notes', label: 'Notes', field: 'notes' },
  { name: 'balance', label: 'Balance', field: 'balance' },
  { name: 'actions', label: 'Actions', field: 'actions' },
];

const reconcileHeaders = [
  { name: 'date', label: 'Date', field: 'date' },
  { name: 'merchant', label: 'Merchant', field: 'merchant' },
  { name: 'amount', label: 'Amount', field: 'amount' },
  { name: 'status', label: 'Status', field: 'status' },
];

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
        (!selectedStatement.value ||
          (tx.date >= selectedStatement.value.startDate &&
            tx.date <= selectedStatement.value.endDate)),
    )
    .map(({ tx, budget }) => ({
      id: tx.id,
      date: tx.date,
      merchant: tx.importedMerchant || tx.merchant || 'N/A',
      category: tx.categories && tx.categories.length > 0 ? tx.categories[0].category : 'N/A',
      entityId: tx.entityId || budget.entityId,
      amount: tx.isIncome || false ? tx.amount : -1 * tx.amount,
      isIncome: tx.isIncome || false,
      status: tx.status || 'C',
      notes: tx.notes || '',
      budgetId: budget.budgetId,
    }));

  let unmatchedTxs: DisplayTransaction[] = importedTransactions.value
    .filter(
      (tx) =>
        tx.accountNumber === selectedAccount.value &&
        !tx.matched &&
        !tx.ignored &&
        !tx.deleted &&
        (!selectedStatement.value ||
          (tx.postedDate >= selectedStatement.value.startDate &&
            tx.postedDate <= selectedStatement.value.endDate)),
    )
    .map((tx) => ({
      id: tx.id,
      date: tx.postedDate,
      merchant: tx.payee || 'N/A',
      category: '',
      entityId: '',
      amount: tx.debitAmount > 0 ? -tx.debitAmount : tx.creditAmount,
      isIncome: tx.creditAmount > 0,
      status: tx.status || 'U',
      notes: '',
    }));

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
      matchedTxs = matchedTxs.filter((t) =>
        Math.abs(t.amount).toString().includes(amountFilter.toString()),
      );
      unmatchedTxs = unmatchedTxs.filter((t) =>
        Math.abs(t.amount).toString().includes(amountFilter.toString()),
      );
    }
  }

  if (filterImportedMerchant.value) {
    const importedMerchantFilter = filterImportedMerchant.value.toLowerCase();
    matchedTxs = matchedTxs.filter((t) =>
      t.merchant.toLowerCase().includes(importedMerchantFilter),
    );
    unmatchedTxs = unmatchedTxs.filter((t) =>
      t.merchant.toLowerCase().includes(importedMerchantFilter),
    );
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
    matchedTxs = matchedTxs.filter(
      (t) =>
        t.merchant.toLowerCase().includes(searchLower) ||
        Math.abs(t.amount).toString().toLowerCase().includes(searchLower),
    );
    unmatchedTxs = unmatchedTxs.filter(
      (t) =>
        t.merchant.toLowerCase().includes(searchLower) ||
        Math.abs(t.amount).toString().toLowerCase().includes(searchLower),
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
      retTrxs = retTrxs.filter((t) =>
        Math.abs(t.amount).toString().includes(amountFilter.toString()),
      );
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
    retTrxs = retTrxs.filter(
      (t) =>
        t.merchant.toLowerCase().includes(searchLower) ||
        Math.abs(t.amount).toString().toLowerCase().includes(searchLower),
    );
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
    .filter(
      ({ tx }) =>
        tx.accountNumber === selectedAccount.value &&
        tx.date >= start &&
        tx.date <= end &&
        !tx.deleted,
    )
    .map(({ tx, budget }) => ({
      id: tx.id,
      date: tx.date,
      merchant: tx.importedMerchant || tx.merchant || 'N/A',
      amount: tx.isIncome ? tx.amount : -1 * tx.amount,
      status: tx.status || 'U',
      budgetId: budget.budgetId,
    }));

  const importedTxs: DisplayTransaction[] = importedTransactions.value
    .filter(
      (itx) =>
        itx.accountNumber === selectedAccount.value &&
        itx.postedDate >= start &&
        itx.postedDate <= end &&
        !itx.deleted,
    )
    .map((itx) => ({
      id: itx.id,
      date: itx.postedDate,
      merchant: itx.payee || 'N/A',
      amount: itx.debitAmount > 0 ? -itx.debitAmount : itx.creditAmount,
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

    // Load imported transactions
    importedTransactions.value = await dataAccess.getImportedTransactions();

    // Load accounts
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found', 'negative');
      return;
    }
    accounts.value = await dataAccess.getAccounts(family.id);

    // Extract unique account numbers and names
    const budgetAccounts = budgets.value
      .flatMap((budget) => budget.transactions || [])
      .filter((tx) => !tx.deleted && tx.accountNumber)
      .map((tx) => tx.accountNumber!);

    const importedAccounts = importedTransactions.value
      .filter((tx) => tx.accountNumber)
      .map((tx) => tx.accountNumber);

    const uniqueAccountNumbers = [...new Set([...budgetAccounts, ...importedAccounts])];

    // Create account options with name and number
    accountOptions.value = uniqueAccountNumbers
      .map((accountNumber) => {
        const account = accounts.value.find((acc) => acc.accountNumber === accountNumber);
        return {
          title: account ? `${account.name} (${accountNumber})` : `Unknown (${accountNumber})`,
          value: accountNumber,
        };
      })
      .sort((a, b) => a.title.localeCompare(b.title));

    // Default to first account if available
    if (accountOptions.value.length > 0) {
      selectedAccount.value = accountOptions.value[0].value;
      await loadTransactions();
    }
  } catch (error: any) {
    console.error('Error loading data:', error);
    showSnackbar(`Error loading data: ${error.message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

async function loadTransactions() {
  if (!selectedAccount.value) return;

  loading.value = true;
  try {
    // Transactions are already loaded in budgets.value and importedTransactions.value
    // Filtering happens in displayTransactions computed property
    await statementStore.loadStatements(familyId.value, selectedAccount.value);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    if (statements.value.length > 0) {
      selectedStatementId.value = statements.value[statements.value.length - 1].id;
    } else {
      selectedStatementId.value = null;
    }
  } catch (error: any) {
    console.error('Error loading transactions:', error);
    showSnackbar(`Error loading transactions: ${error.message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

function getEntityName(entityId: string): string {
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

function getRowClass(row: DisplayTransaction) {
  if (row.status === 'U') return 'status-u';
  if (row.status === 'C') return 'status-c';
  return '';
}

function confirmAction(transaction: any, action: string) {
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

    const parts = id.split('-');
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join('-');

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

      const importedTxIndex = importedTransactions.value.findIndex(
        (tx) =>
          tx.accountNumber === selectedAccount.value &&
          tx.matched &&
          tx.payee == budgetTx.importedMerchant &&
          tx.postedDate == budgetTx.postedDate &&
          (tx.debitAmount == budgetTx.amount || tx.creditAmount == budgetTx.amount),
      );

      updatedBudgetTx = {
        ...budgetTx,
        accountNumber: undefined,
        accountSource: undefined,
        postedDate: undefined,
        importedMerchant: undefined,
        checkNumber: undefined,
        status: 'U',
      };

      await dataAccess.saveTransaction(budget, updatedBudgetTx, false);

      const budgetIndex = budgets.value.findIndex((b) => b.budgetId === budgetId);
      if (budgetIndex !== -1) {
        budgets.value[budgetIndex].transactions = budgets.value[budgetIndex].transactions.map(
          (tx) => (tx.id === id ? updatedBudgetTx : tx),
        );
        budgetStore.updateBudget(budgetId, budgets.value[budgetIndex]);
      }

      if (importedTxIndex !== -1) {
        importedTransactions.value[importedTxIndex].matched = false;
        await dataAccess.updateImportedTransaction(
          docId,
          importedTransactions.value[importedTxIndex].id,
          false,
          false,
        );
      }
    }

    if (!budgetId && transactionAction.value == 'Delete') {
      await dataAccess.deleteImportedTransaction(docId, id);
      const importedTxIndex = importedTransactions.value.findIndex((tx) => tx.id === id);
      if (importedTxIndex !== -1) {
        importedTransactions.value[importedTxIndex].deleted = true;
      }
    } else if (!budgetId && transactionAction.value == 'Ignore') {
      await dataAccess.updateImportedTransaction(docId, id, false, true);
      const importedTxIndex = importedTransactions.value.findIndex((tx) => tx.id === id);
      if (importedTxIndex !== -1) {
        importedTransactions.value[importedTxIndex].matched = false;
        importedTransactions.value[importedTxIndex].ignored = true;
      }
    }

    showSnackbar(`${transactionAction.value} action completed successfully`, 'success');
  } catch (error: any) {
    console.error(`Error performing ${transactionAction.value} action:`, error);
    showSnackbar(
      `Error performing ${transactionAction.value} action: ${error.message}`,
      'negative',
    );
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
  const firstSelectedTx = displayTransactions.value?.find((t) => t.id === selectedRows.value[0]);
  batchMerchant.value = filterMerchant.value || (firstSelectedTx ? firstSelectedTx.merchant : '');
  batchCategory.value = '';
  selectedEntityId.value = familyStore.selectedEntityId || '';
  showBatchMatchDialog.value = true;
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

    for (const txId of selectedRows.value) {
      const transaction = displayTransactions.value?.find((t) => t.id === txId);
      if (!transaction) {
        console.error(`Transaction with ID ${txId} not found in displayTransactions`);
        continue;
      }

      const { id, date, amount, isIncome } = transaction;
      const parts = id.split('-');
      const importedTxId = id;
      const docId = parts.slice(0, -1).join('-');
      const importedTx = importedTransactions.value.find((tx) => tx.id === id);
      if (!importedTx) {
        console.error(`Imported transaction ${id} not found`);
        continue;
      }

      // Determine budget month from postedDate
      const budgetMonth = date.slice(0, 7); // YYYY-MM
      const budgetId = `${ownerUid}_${selectedEntityId.value}_${budgetMonth}`; // New ID format: uid_entityId_budgetMonth

      // Check if budget exists
      let targetBudget = budgetStore.getBudget(budgetId);
      if (!targetBudget) {
        // Create new budget by copying the most recent previous budget
        targetBudget = await createBudgetForMonth(
          budgetMonth,
          family.id,
          ownerUid,
          selectedEntityId.value,
        );
      }

      // Check if category exists in budget; if not, add it
      if (!targetBudget.categories.some((cat) => cat.name === batchCategory.value)) {
        targetBudget.categories.push({
          name: batchCategory.value,
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
        merchant: batchMerchant.value,
        categories: [{ category: batchCategory.value, amount: Math.abs(amount) }],
        amount: Math.abs(amount),
        notes: '',
        recurring: false,
        recurringInterval: 'Monthly',
        userId: user.uid,
        isIncome: isIncome,
        accountSource: importedTx.accountSource || '',
        accountNumber: importedTx.accountNumber || '',
        postedDate: importedTx.postedDate || '',
        checkNumber: importedTx.checkNumber || '',
        importedMerchant: importedTx.payee || '',
        status: 'C',
        entityId: selectedEntityId.value,
      };

      // Save budget transaction
      await dataAccess.saveTransaction(targetBudget, newTransaction, false);

      // Update local budgets
      const budgetIndex = budgets.value.findIndex((b) => b.budgetId === budgetId);
      if (budgetIndex !== -1) {
        budgets.value[budgetIndex].transactions = [
          ...(budgets.value[budgetIndex].transactions || []),
          newTransaction,
        ];
        budgetStore.updateBudget(budgetId, budgets.value[budgetIndex]);
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
        importedTransactions.value[importedTxIndex].matched = true;
      }
    }

    showSnackbar(
      `Successfully matched ${selectedRows.value.length} transaction${selectedRows.value.length > 1 ? 's' : ''}`,
      'success',
    );
    selectedRows.value = [];
    showBatchMatchDialog.value = false;
    batchMerchant.value = '';
    batchCategory.value = '';
    selectedEntityId.value = '';
    const acct = selectedAccount.value;
    await loadData(); // Refresh data to reflect changes
    selectedAccount.value = acct;
  } catch (error: any) {
    console.error('Error performing batch match:', error);
    showSnackbar(`Error performing batch match: ${error.message}`, 'negative');
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
      const parts = tx.id.split('-');
      const docId = parts.slice(0, -1).join('-');
      if (batchAction.value === 'Delete') {
        await dataAccess.deleteImportedTransaction(docId, tx.id);
        const index = importedTransactions.value.findIndex((itx) => itx.id === tx.id);
        if (index !== -1) importedTransactions.value.splice(index, 1);
      } else if (batchAction.value === 'Ignore') {
        await dataAccess.updateImportedTransaction(docId, tx.id, false, true);
        const index = importedTransactions.value.findIndex((itx) => itx.id === tx.id);
        if (index !== -1) importedTransactions.value[index].ignored = true;
      }
    }
    showSnackbar(
      `Successfully ${batchAction.value.toLowerCase()}d ${selectedRows.value.length} transaction${selectedRows.value.length > 1 ? 's' : ''}`,
      'success',
    );
    selectedRows.value = [];
  } catch (error: any) {
    console.error('Error performing batch action:', error);
    showSnackbar(`Error performing batch action: ${error.message}`, 'negative');
  } finally {
    saving.value = false;
    showBatchActionDialog.value = false;
    batchAction.value = '';
  }
}

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

  // Get the family store
  const familyStore = useFamilyStore();

  // Find the entity from familyStore.family?.entities
  const entity = familyStore.family?.entities?.find((e) => e.id === entityId);
  const templateBudget = entity?.templateBudget;

  if (templateBudget && templateBudget.categories.length > 0) {
    // Use template budget if it exists and has at least one category
    const newBudget: Budget = {
      familyId: familyId,
      entityId: entityId,
      month: month,
      incomeTarget: 0, // Use template's incomeTarget or default to 0
      categories: templateBudget.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? 0 : 0, // Initialize carryover as 0 for new budgets
      })),
      transactions: [], // No transactions initially
      label: `Budget for ${month}`,
      merchants: [],
      budgetId: budgetId,
    };

    await dataAccess.saveBudget(budgetId, newBudget);
    budgetStore.updateBudget(budgetId, newBudget);
    budgets.value.push(newBudget);
    return newBudget;
  }

  // Use predefined template based on entity.type if no valid templateBudget
  if (entity && DEFAULT_BUDGET_TEMPLATES[entity.type]) {
    const predefinedTemplate = DEFAULT_BUDGET_TEMPLATES[entity.type];
    const newBudget: Budget = {
      familyId: familyId,
      entityId: entityId,
      month: month,
      incomeTarget: 0,
      categories: predefinedTemplate.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? 0 : 0, // Initialize carryover as 0
      })),
      transactions: [],
      label: `Default ${entity.type} Budget for ${month}`,
      merchants: [],
      budgetId: budgetId,
    };

    await dataAccess.saveBudget(budgetId, newBudget);
    budgetStore.updateBudget(budgetId, newBudget);
    budgets.value.push(newBudget);
    return newBudget;
  }

  // Fallback to existing logic: find the most recent previous budget or earliest future budget
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
    budgets.value.push(defaultBudget);
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
    label: sourceBudget.label || `Budget for ${month}`,
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
  } catch (error: any) {
    console.error('Error saving balance adjustment:', error);
    showSnackbar(`Error saving balance adjustment: ${error.message}`, 'negative');
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
    if (prevStatement && statements.value.some((s) => s.id === prevStatement)) {
      selectedStatementId.value = prevStatement;
    }
  }
}

function openStatementDialog() {
  showStatementDialog.value = true;
  if (statements.value.length > 0) {
    newStatement.value.startDate = statements.value[statements.value.length - 1].endDate;
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
  } catch (error: any) {
    console.error('Error saving statement:', error);
    showSnackbar(`Error saving statement: ${error.message}`, 'negative');
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
          txRefs.push({ budgetId: budget.budgetId!, transactionId: tx.id });
          updated = true;
        }
      }
      if (updated) {
        budgetStore.updateBudget(budget.budgetId!, budget);
      }
    }

    for (const id of selectedRows.value) {
      const idx = importedTransactions.value.findIndex((itx) => itx.id === id);
      if (idx !== -1) {
        const parts = id.split('-');
        const docId = parts.slice(0, -1).join('-');
        const updatedTx = { ...importedTransactions.value[idx], status: 'R' };
        importedTransactions.value[idx].status = 'R';
        await dataAccess.updateImportedTransaction(docId, updatedTx);
      }
    }

    const updated: Statement = { ...selectedStatement.value, reconciled: true };
    await statementStore.saveStatement(familyId.value, selectedAccount.value, updated, txRefs);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    showSnackbar('Statement reconciled', 'success');
    reconciling.value = false;
    selectedRows.value = [];
  } catch (error: any) {
    console.error('Error reconciling statement:', error);
    showSnackbar(`Error reconciling statement: ${error.message}`, 'negative');
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
          txRefs.push({ budgetId: budget.budgetId!, transactionId: tx.id });
          updated = true;
        }
      }
      if (updated) budgetStore.updateBudget(budget.budgetId!, budget);
    }

    for (const itx of importedTransactions.value) {
      if (
        itx.accountNumber === selectedAccount.value &&
        itx.postedDate >= selectedStatement.value.startDate &&
        itx.postedDate <= selectedStatement.value.endDate &&
        itx.status === 'R'
      ) {
        const parts = itx.id.split('-');
        const docId = parts.slice(0, -1).join('-');
        const updatedTx = { ...itx, status: 'C' };
        itx.status = 'C';
        await dataAccess.updateImportedTransaction(docId, updatedTx);
      }
    }

    await statementStore.unreconcileStatement(
      familyId.value,
      selectedAccount.value,
      selectedStatement.value.id,
      txRefs,
    );
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    showSnackbar('Statement unreconciled', 'success');
  } catch (error: any) {
    console.error('Error unreconciling statement:', error);
    showSnackbar(`Error unreconciling statement: ${error.message}`, 'negative');
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
          txRefs.push({ budgetId: budget.budgetId!, transactionId: tx.id });
          updated = true;
        }
      }
      if (updated) budgetStore.updateBudget(budget.budgetId!, budget);
    }

    for (const itx of importedTransactions.value) {
      if (
        itx.accountNumber === selectedAccount.value &&
        itx.postedDate >= selectedStatement.value.startDate &&
        itx.postedDate <= selectedStatement.value.endDate &&
        itx.status === 'R'
      ) {
        const parts = itx.id.split('-');
        const docId = parts.slice(0, -1).join('-');
        const updatedTx = { ...itx, status: 'C' };
        itx.status = 'C';
        await dataAccess.updateImportedTransaction(docId, updatedTx);
      }
    }

    await statementStore.deleteStatement(
      familyId.value,
      selectedAccount.value,
      selectedStatement.value.id,
      txRefs,
    );
    await statementStore.loadStatements(familyId.value, selectedAccount.value);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    if (statements.value.length > 0) {
      selectedStatementId.value = statements.value[statements.value.length - 1].id;
    } else {
      selectedStatementId.value = null;
    }
    showSnackbar('Statement deleted', 'success');
  } catch (error: any) {
    console.error('Error deleting statement:', error);
    showSnackbar(`Error deleting statement: ${error.message}`, 'negative');
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
    entity: getEntityName(tx.entityId || tx.budgetId),
    amount: tx.amount,
    isIncome: tx.isIncome ? 'true' : 'false',
    status: tx.status,
    notes: tx.notes,
    balance: tx.balance ?? '',
  }));
  const csv = Papa.unparse(rows);
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const account = accounts.value.find((a) => a.accountNumber === selectedAccount.value);
  const name = account
    ? account.name.replace(/[^a-zA-Z0-9_-]/g, '_').toLowerCase()
    : 'transactions';
  const today = todayISO();
  saveAs(blob, `${name}_${today}.csv`);
}

function showSnackbar(text: string, color = "success") {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;

  Notify.create({
    message: text,
    type: color === "success" ? "positive" : "negative", // Map success to positive, negative to negative
    timeout: 3000,
    actions: [
      {
        label: "Close",
        color: "white",
        handler: () => {
          snackbar.value = false;
        },
      },
    ],
    position: "top-right", // Adjust position as needed (e.g., 'bottom', 'center')
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
