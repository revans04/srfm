<!-- src/components/TransactionRegistry.vue -->
<template>
  <v-container fluid>
    <v-row>
      <v-col cols="12" md="4">
        <v-select
          v-model="selectedAccount"
          :items="accountOptions"
          label="Select Account"
          variant="outlined"
          item-title="title"
          item-value="value"
          @update:modelValue="loadTransactions"
        ></v-select>
      </v-col>
      <v-col cols="auto" class="d-flex align-center">
        <v-btn color="primary" variant="plain" @click="refreshData" :loading="loading">
          <v-icon start>mdi-refresh</v-icon>
          Refresh
        </v-btn>
      </v-col>
    </v-row>
    <v-row v-if="selectedAccount">
      <v-col cols="12" md="4">
        <v-select
          v-model="selectedStatementId"
          :items="statementOptions"
          label="Select Statement"
          variant="outlined"
          item-title="title"
          item-value="id"
        ></v-select>
      </v-col>
      <v-col cols="auto">
        <v-btn color="primary" @click="openStatementDialog">Add Statement</v-btn>
      </v-col>
      <v-col cols="auto" v-if="selectedStatement">
        <v-btn color="primary" @click="startReconcile">Reconcile Statement</v-btn>
      </v-col>
      <v-col cols="auto" v-if="selectedStatement && selectedStatement.reconciled">
        <v-btn color="primary" @click="unreconcileStatement">Unreconcile</v-btn>
      </v-col>
      <v-col cols="auto" v-if="selectedStatement">
        <v-btn color="error" @click="deleteStatement">Delete Statement</v-btn>
      </v-col>
    </v-row>

    <!-- Balance Display -->
    <v-card class="mb-4" v-if="selectedAccount">
      <v-card-title>Account Balance</v-card-title>
      <v-card-text>
        <v-row>
          <v-col>
            <span class="text-h6">Balance from Accounts: </span>
            <span
              :class="{
                'text-success': currentBalance === latestTransactionBalance,
                'text-error': currentBalance !== latestTransactionBalance,
              }"
            >
              {{ formatCurrency(currentBalance) }}
            </span>
            <v-tooltip v-if="currentBalance !== latestTransactionBalance" activator="parent" location="top">
              The current balance differs from the transaction registry balance ({{ formatCurrency(latestTransactionBalance) }}) by
              {{ formatCurrency(Math.abs(currentBalance - latestTransactionBalance)) }}. Adjust Balance to reconcile.
            </v-tooltip>
          </v-col>
          <v-col cols="auto" v-if="currentBalance !== latestTransactionBalance">
            <v-btn color="primary" @click="openBalanceAdjustmentDialog">Adjust Balance</v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card class="mb-4" v-if="selectedStatement">
      <v-card-title>Statement Totals</v-card-title>
      <v-card-text>
        <p>Start Balance: {{ formatCurrency(selectedStatement.startingBalance) }}</p>
        <p>End Balance: {{ formatCurrency(selectedStatement.endingBalance) }}</p>
      </v-card-text>
    </v-card>

    <!-- Loading Overlay -->
    <v-overlay :model-value="loading" class="align-center justify-center" scrim="#00000080">
      <v-progress-circular indeterminate color="primary" size="50" />
    </v-overlay>

    <v-card class="mb-4">
      <v-card-title>Filters</v-card-title>
      <v-card-text>
        <v-row>
          <v-col cols="6" md="6">
            <v-text-field
              append-inner-icon="mdi-magnify"
              density="compact"
              label="Search"
              variant="outlined"
              single-line
              v-model="search"
              @input="applyFilters"
            ></v-text-field>
          </v-col>
          <v-col cols="auto">
            <v-checkbox v-model="filterMatched" label="Show Only Unmatched" density="compact" @input="applyFilters"></v-checkbox>
          </v-col>
        </v-row>
        <v-row>
          <v-col cols="12" md="2">
            <v-text-field v-model="filterMerchant" label="Merchant" variant="outlined" density="compact" @input="applyFilters"></v-text-field>
          </v-col>
          <v-col cols="12" md="2">
            <v-text-field v-model="filterAmount" label="Amount" type="number" variant="outlined" density="compact" @input="applyFilters"></v-text-field>
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field v-model="filterImportedMerchant" label="Imported Merchant" variant="outlined" density="compact" @input="applyFilters"></v-text-field>
          </v-col>
          <v-col cols="12" md="5">
            <v-row>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="filterStartDate"
                  label="Start Date"
                  type="date"
                  variant="outlined"
                  density="compact"
                  :clearable="true"
                  @input="applyFilters"
                ></v-text-field>
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="filterEndDate"
                  label="End Date"
                  type="date"
                  variant="outlined"
                  density="compact"
                  :clearable="true"
                  @input="applyFilters"
                ></v-text-field>
              </v-col>
            </v-row>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card v-if="!loading">
      <v-card-title>
        <v-row :dense="true">
          <v-col>Transaction Registry</v-col>
          <v-col cols="auto">
            <v-btn icon variant="plain" @click="downloadCsv" :disabled="displayTransactions.length === 0">
              <v-icon>mdi-download</v-icon>
              <v-tooltip activator="parent" location="top">Download CSV</v-tooltip>
            </v-btn>
          </v-col>
        </v-row>
      </v-card-title>
      <v-card-text>
        <v-btn
          v-if="selectedRows.length > 0"
          color="primary"
          @click="openBatchMatchDialog"
          class="mb-4"
          :disabled="loading || !selectedRows.every(id => {
            const tx = displayTransactions.find((t: DisplayTransaction) => t.id === id);
            return tx && tx.status === 'U';
          })"
        >
          Batch Match {{ selectedRows.length }} Transaction{{ selectedRows.length > 1 ? "s" : "" }}
        </v-btn>
        <v-btn
          v-if="selectedRows.length > 0"
          color="warning"
          class="mb-4 ml-2"
          @click="confirmBatchAction('Ignore')"
          :disabled="loading || !selectedRows.every(id => {
            const tx = displayTransactions.find((t: DisplayTransaction) => t.id === id);
            return tx && tx.status === 'U' && !tx.budgetId;
          })"
        >
          Ignore {{ selectedRows.length }}
        </v-btn>
        <v-btn
          v-if="selectedRows.length > 0"
          color="error"
          class="mb-4 ml-2"
          @click="confirmBatchAction('Delete')"
          :disabled="loading || !selectedRows.every(id => {
            const tx = displayTransactions.find((t: DisplayTransaction) => t.id === id);
            return tx && tx.status === 'U' && !tx.budgetId;
          })"
        >
          Delete {{ selectedRows.length }}
        </v-btn>
      </v-card-text>
      <v-data-table
        v-model="selectedRows"
        :headers="headers"
        :items="displayTransactions"
        class="elevation-1"
        items-per-page="0"
        :hide-default-footer="true"
        show-select
        :item-selectable="(item) => item.status === 'U'"
        item-value="id"
        fixed-header
        fixed-footer
        height="600"
        :item-class="getRowClass"
      >
        <template v-slot:item.amount="{ item }">
          <span :class="item.isIncome ? 'text-success' : 'text-error'">
            {{ formatCurrency(item.amount) }}
          </span>
        </template>
        <template v-slot:item.balance="{ item }">
          {{ formatCurrency(item.balance) }}
        </template>
        <template v-slot:item.entity="{ item }">
          {{ getEntityName(item.entityId || item.budgetId) }}
        </template>
        <template v-slot:item.actions="{ item }">
          <v-btn
            v-if="item.status === 'C'"
            icon
            density="compact"
            variant="plain"
            color="warning"
            @click.stop="confirmAction(item, 'Disconnect')"
            title="Disconnect Transaction"
          >
            <v-icon>mdi-link-off</v-icon>
          </v-btn>
          <v-btn
            v-if="item.status != 'C' && item.id"
            icon
            density="compact"
            variant="plain"
            color="error"
            @click.stop="confirmAction(item, 'Ignore')"
            title="Ignore Imported Transaction"
          >
            <v-icon>mdi-eye-off-outline</v-icon>
          </v-btn>
          <v-btn
            v-if="item.status != 'C' && item.id"
            icon
            density="compact"
            variant="plain"
            color="error"
            @click.stop="confirmAction(item, 'Delete')"
            title="Delete Imported Transaction"
          >
            <v-icon>mdi-trash-can-outline</v-icon>
          </v-btn>
        </template>
      </v-data-table>
    </v-card>

    <!-- Action Confirmation Dialog -->
    <v-dialog v-model="showActionDialog" max-width="400" @keyup.enter="executeAction">
      <v-card>
        <v-card-title class="bg-warning py-3">
          <span class="text-white">{{ transactionAction }} Transaction</span>
        </v-card-title>
        <v-card-text v-if="transactionAction == 'Disconnect'" class="pt-4">
          Are you sure you want to disconnect the transaction for "{{ transactionToAction?.merchant }}" on {{ transactionToAction?.date }} from its imported
          transaction?
        </v-card-text>
        <v-card-text v-else class="pt-4">
          Are you sure you want to {{ transactionAction }} the transaction for "{{ transactionToAction?.merchant }}" on {{ transactionToAction?.date }}?
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showActionDialog = false">Cancel</v-btn>
          <v-btn color="warning" variant="flat" @click="executeAction">{{ transactionAction }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Batch Match Dialog -->
    <v-dialog v-model="showBatchMatchDialog" max-width="500" @keyup.enter="executeBatchMatch">
      <v-card>
        <v-card-title class="bg-primary py-3">
          <span class="text-white">Batch Match Transactions</span>
        </v-card-title>
        <v-card-text class="pt-4">
          <v-form ref="batchMatchForm">
            <p>Assign an entity, merchant, and category for {{ selectedRows.length }} unmatched transaction{{ selectedRows.length > 1 ? "s" : "" }}.</p>
            <v-row>
              <v-col>
                <v-select
                  v-model="selectedEntityId"
                  :items="entityOptions"
                  label="Select Entity"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                  item-title="name"
                  item-value="id"
                ></v-select>
              </v-col>
            </v-row>
            <v-row>
              <v-col>
                <v-text-field v-model="batchMerchant" label="Merchant" variant="outlined" density="compact" :rules="requiredField"></v-text-field>
              </v-col>
            </v-row>
            <v-row>
              <v-col>
                <v-combobox
                  v-model="batchCategory"
                  :items="categoryOptions"
                  label="Category"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                ></v-combobox>
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showBatchMatchDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="flat" @click="executeBatchMatch" :loading="saving">Match</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Add Statement Dialog -->
    <v-dialog v-model="showStatementDialog" max-width="500">
      <v-card>
        <v-card-title class="bg-primary py-3">
          <span class="text-white">Add Statement</span>
        </v-card-title>
        <v-card-text>
          <v-form ref="statementForm">
            <v-row>
              <v-col>
                <v-text-field
                  v-model="newStatement.startDate"
                  label="Start Date"
                  type="date"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                ></v-text-field>
              </v-col>
              <v-col>
                <v-text-field
                  v-model.number="newStatement.startingBalance"
                  label="Starting Balance"
                  type="number"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                ></v-text-field>
              </v-col>
            </v-row>
            <v-row>
              <v-col>
                <v-text-field
                  v-model="newStatement.endDate"
                  label="End Date"
                  type="date"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                ></v-text-field>
              </v-col>
              <v-col>
                <v-text-field
                  v-model.number="newStatement.endingBalance"
                  label="Ending Balance"
                  type="number"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                ></v-text-field>
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="closeStatementDialog">Cancel</v-btn>
          <v-btn color="primary" variant="flat" @click="saveStatement" :loading="saving">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Batch Action Dialog -->
    <v-dialog v-model="showBatchActionDialog" max-width="400" @keyup.enter="executeBatchAction">
      <v-card>
        <v-card-title class="bg-warning py-3">
          <span class="text-white">{{ batchAction }} Selected Transactions</span>
        </v-card-title>
        <v-card-text class="pt-4">
          Are you sure you want to {{ batchAction.toLowerCase() }} {{ selectedRows.length }} transaction{{ selectedRows.length > 1 ? "s" : "" }}?
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showBatchActionDialog = false">Cancel</v-btn>
          <v-btn color="warning" variant="flat" @click="executeBatchAction" :loading="saving">{{ batchAction }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Balance Adjustment Dialog -->
    <v-dialog v-model="showBalanceAdjustmentDialog" max-width="500">
      <v-card>
        <v-card-title class="bg-primary py-3">
          <span class="text-white">Adjust Initial Balance</span>
        </v-card-title>
        <v-card-text>
          <v-form ref="adjustmentForm" @submit.prevent="saveBalanceAdjustment">
            <v-row>
              <v-col>
                <p>
                  The current account balance ({{ formatCurrency(currentBalance) }}) differs from the transaction registry balance ({{
                    formatCurrency(latestTransactionBalance)
                  }}).
                </p>
                <p>Enter the adjustment amount to reconcile the balance:</p>
              </v-col>
            </v-row>
            <v-row>
              <v-col>
                <v-text-field
                  v-model.number="adjustmentAmount"
                  label="Adjustment Amount"
                  type="number"
                  variant="outlined"
                  density="compact"
                  :rules="adjustmentRules"
                  hint="Positive to increase balance, negative to decrease"
                  persistent-hint
                ></v-text-field>
              </v-col>
            </v-row>
            <v-row>
              <v-col>
                <v-text-field
                  v-model="adjustmentDate"
                  label="Adjustment Date"
                  type="date"
                  variant="outlined"
                  density="compact"
                  :rules="requiredField"
                ></v-text-field>
              </v-col>
            </v-row>
            <v-btn type="submit" color="primary" :loading="saving">Save Adjustment</v-btn>
            <v-btn color="grey" variant="text" @click="closeBalanceAdjustmentDialog" class="ml-2">Cancel</v-btn>
          </v-form>
        </v-card-text>
      </v-card>
    </v-dialog>

    <!-- Reconcile Summary -->
    <v-card class="mt-4" v-if="reconciling && selectedStatement">
      <v-card-title class="bg-primary py-3">
        <span class="text-white">Reconcile Statement</span>
      </v-card-title>
      <v-card-text>
        <p>
          Select transactions to reconcile for
          {{ selectedStatement.startDate }} - {{ selectedStatement.endDate }}
        </p>
        <div class="mt-4">
          <p>Starting Balance: {{ formatCurrency(selectedStatement.startingBalance) }}</p>
          <p>Selected Total: {{ formatCurrency(selectedTransactionsTotal) }}</p>
          <p>Calculated Ending Balance: {{ formatCurrency(calculatedEndingBalance) }}</p>
          <p :class="{ 'text-error': !reconcileMatches }">Statement Ending Balance: {{ formatCurrency(selectedStatement.endingBalance) }}</p>
          <v-alert type="warning" v-if="!reconcileMatches" class="mt-2" dense>
            Calculated ending balance does not match statement ending balance.
          </v-alert>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn color="grey" variant="text" @click="cancelReconcile">Cancel</v-btn>
        <v-btn color="primary" variant="flat" @click="markStatementReconciled" :loading="saving">Reconcile</v-btn>
      </v-card-actions>
    </v-card>

    <v-snackbar v-model="snackbar" :color="snackbarColor" timeout="3000">
      {{ snackbarText }}
      <template v-slot:actions>
        <v-btn variant="text" @click="snackbar = false">Close</v-btn>
      </template>
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { storeToRefs } from "pinia";
import { auth } from "../firebase/index";
import { dataAccess } from "../dataAccess";
import Papa from "papaparse";
import { saveAs } from "file-saver";
import { useBudgetStore } from "../store/budget";
import { useFamilyStore } from "../store/family";
import { useStatementStore } from "../store/statements";
import { useUIStore } from "../store/ui";
import { Transaction, ImportedTransaction, Budget, Account, ImportedTransactionDoc, Statement } from "../types";
import { formatCurrency, toBudgetMonth, adjustTransactionDate, todayISO } from "../utils/helpers";
import { VForm } from "vuetify/components";
import { v4 as uuidv4 } from "uuid";
import { DEFAULT_BUDGET_TEMPLATES } from "../constants/budgetTemplates";

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
const familyId = computed(() => familyStore.family?.id || "");

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
const statementOptions = computed(() => [
  { title: "All", id: "ALL" },
  ...statements.value.map((s) => ({
    title: `${s.startDate} - ${s.endDate}`,
    id: s.id,
  })),
]);
const selectedStatementId = ref<string | null>(null);
const snackbar = ref(false);
const snackbarText = ref("");
const snackbarColor = ref("success");
const showActionDialog = ref(false);
const transactionToAction = ref<any | null>(null);
const transactionAction = ref("");
const showBalanceAdjustmentDialog = ref(false);
const adjustmentAmount = ref<number>(0);
const adjustmentDate = ref<string>(todayISO());
const adjustmentForm = ref<InstanceType<typeof VForm> | null>(null);
const selectedRows = ref<string[]>([]);
const showBatchMatchDialog = ref(false);
const batchMatchForm = ref<InstanceType<typeof VForm> | null>(null);
const batchMerchant = ref("");
const batchCategory = ref("");
const selectedEntityId = ref<string>("");
const showBatchActionDialog = ref(false);
const batchAction = ref<string>("");
const showStatementDialog = ref(false);
const statementForm = ref<InstanceType<typeof VForm> | null>(null);
const newStatement = ref<Statement>({
  id: "",
  accountNumber: "",
  startDate: "",
  startingBalance: 0,
  endDate: "",
  endingBalance: 0,
  reconciled: false,
});
const reconciling = ref(false);
const selectedStatement = computed(() => {
  if (!selectedStatementId.value || selectedStatementId.value === "ALL") {
    return null;
  }
  return statements.value.find((s) => s.id === selectedStatementId.value) || null;
});
const categoryOptions = computed(() => {
  const categories = new Set<string>(["Income"]);
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
const requiredField = [(value: string) => !!value || "This field is required"];
const adjustmentRules = [(value: number) => value !== undefined || "Amount is required", (value: number) => !isNaN(value) || "Amount must be a number"];

// Computed properties
const currentBalance = computed(() => {
  if (!selectedAccount.value) return 0;
  const account = accounts.value.find((acc) => acc.accountNumber === selectedAccount.value);
  return account ? account.balance || 0 : 0;
});

const latestTransactionBalance = computed(() => {
  if (!displayTransactions.value?.length) return 0;
  if (selectedAccount.value && accounts.value) {
    const aInfo = accounts.value.filter((a) => (a.accountNumber ?? "") == selectedAccount.value);
    if (aInfo && aInfo.length > 0 && (aInfo[0].type == "CreditCard" || aInfo[0].type == "Loan")) {
      return Math.abs(displayTransactions.value[0].balance) || 0;
    }
  }
  return displayTransactions.value[0].balance || 0;
});

const headers = [
  { title: "Date", value: "date" },
  { title: "Merchant", value: "merchant" },
  { title: "Category", value: "category" },
  { title: "Entity", value: "entity" },
  { title: "Amount", value: "amount" },
  { title: "Status", value: "status" },
  { title: "Notes", value: "notes" },
  { title: "Balance", value: "balance" },
  { title: "Actions", value: "actions" },
];

const reconcileHeaders = [
  { title: "Date", key: "date" },
  { title: "Merchant", key: "merchant" },
  { title: "Amount", key: "amount" },
  { title: "Status", key: "status" },
];

// Combined transactions for display
const displayTransactions = computed((): DisplayTransaction[] => {
  if (!selectedAccount.value) return [];

  let matchedTxs: DisplayTransaction[] = budgets.value
    .flatMap((budget) =>
      (budget.transactions || []).map((tx) => ({
        tx,
        budget,
      }))
    )
    .filter(
      ({ tx }) =>
        tx.accountNumber === selectedAccount.value &&
        tx.status === "C" &&
        !tx.deleted &&
        (!selectedStatement.value || (tx.date >= selectedStatement.value.startDate && tx.date <= selectedStatement.value.endDate))
    )
    .map(({ tx, budget }) => ({
      id: tx.id,
      date: tx.date,
      merchant: tx.importedMerchant || tx.merchant || "N/A",
      category: tx.categories && tx.categories.length > 0 ? tx.categories[0].category : "N/A",
      entityId: tx.entityId || budget.entityId,
      amount: tx.isIncome || false ? tx.amount : -1 * tx.amount,
      isIncome: tx.isIncome || false,
      status: tx.status || "C",
      notes: tx.notes || "",
      budgetId: budget.budgetId,
    }));

  let baseUnmatchedTxs: (DisplayTransaction & { ignored?: boolean })[] = importedTransactions.value
    .filter(
      (tx) =>
        tx.accountNumber === selectedAccount.value &&
        !tx.matched &&
        !tx.deleted &&
        (!selectedStatement.value ||
          (tx.postedDate >= selectedStatement.value.startDate &&
            tx.postedDate <= selectedStatement.value.endDate))
    )
    .map((tx) => ({
      id: tx.id,
      date: tx.postedDate,
      merchant: tx.payee || "N/A",
      category: "",
      entityId: "",
      amount: tx.debitAmount > 0 ? -tx.debitAmount : tx.creditAmount,
      isIncome: tx.creditAmount > 0,
      status: tx.status || "U",
      notes: "",
      ignored: tx.ignored,
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
      (t) => t.merchant.toLowerCase().includes(searchLower) || Math.abs(t.amount).toString().toLowerCase().includes(searchLower)
    );
  }

  const allTxs = [...matchedTxs, ...baseUnmatchedTxs].sort((a, b) => {
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
  }).filter((t) => !(t as any).ignored);

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
      merchant: tx.importedMerchant || tx.merchant || "N/A",
      amount: tx.isIncome ? tx.amount : -1 * tx.amount,
      status: tx.status || "U",
      budgetId: budget.budgetId,
    }));

  const importedTxs: DisplayTransaction[] = importedTransactions.value
    .filter((itx) => itx.accountNumber === selectedAccount.value && itx.postedDate >= start && itx.postedDate <= end && !itx.deleted)
    .map((itx) => ({
      id: itx.id,
      date: itx.postedDate,
      merchant: itx.payee || "N/A",
      amount: itx.debitAmount > 0 ? -itx.debitAmount : itx.creditAmount,
      status: itx.status || "U",
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
      showSnackbar("Please log in to view transactions", "error");
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
      showSnackbar("No family found", "error");
      return;
    }
    accounts.value = await dataAccess.getAccounts(family.id);

    // Extract unique account numbers and names
    const budgetAccounts = budgets.value
      .flatMap((budget) => budget.transactions || [])
      .filter((tx) => !tx.deleted && tx.accountNumber)
      .map((tx) => tx.accountNumber!);

    const importedAccounts = importedTransactions.value.filter((tx) => tx.accountNumber).map((tx) => tx.accountNumber);

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
    console.error("Error loading data:", error);
    showSnackbar(`Error loading data: ${error.message}`, "error");
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
      selectedStatementId.value = "ALL";
    }
  } catch (error: any) {
    console.error("Error loading transactions:", error);
    showSnackbar(`Error loading transactions: ${error.message}`, "error");
  } finally {
    loading.value = false;
  }
}

function getEntityName(entityId: string): string {
  if (!entityId) return "N/A";
  const entity = familyStore.family?.entities?.find((e) => e.id === entityId);
  if (entity) return entity.name;
  // Fallback: Check if entityId is a budgetId and get entityId from budget
  const budget = budgetStore.getBudget(entityId);
  if (budget?.entityId) {
    const budgetEntity = familyStore.family?.entities?.find((e) => e.id === budget.entityId);
    return budgetEntity ? budgetEntity.name : "N/A";
  }
  return "N/A";
}

function getRowClass(item: DisplayTransaction) {
  if (item.status === "U") return "status-u";
  if (item.status === "C") return "status-c";
  return "";
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
      showSnackbar("Invalid transaction data", "error");
      return;
    }

    let updatedBudgetTx: Transaction;

    const parts = id.split("-");
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join("-");

    if (transactionAction.value == "Disconnect" || budgetId) {
      if (!budgetId) {
        showSnackbar("Invalid transaction data (no budget info)", "error");
        return;
      }

      const budget = budgetStore.getBudget(budgetId);
      if (!budget) {
        showSnackbar("Budget not found", "error");
        return;
      }

      const budgetTx = budget.transactions.find((tx) => tx.id === id);
      if (!budgetTx) {
        showSnackbar("Budget transaction not found", "error");
        return;
      }

      const importedTxIndex = importedTransactions.value.findIndex(
        (tx) =>
          tx.accountNumber === selectedAccount.value &&
          tx.matched &&
          tx.payee == budgetTx.importedMerchant &&
          tx.postedDate == budgetTx.postedDate &&
          (tx.debitAmount == budgetTx.amount || tx.creditAmount == budgetTx.amount)
      );

      updatedBudgetTx = {
        ...budgetTx,
        accountNumber: undefined,
        accountSource: undefined,
        postedDate: undefined,
        importedMerchant: undefined,
        checkNumber: undefined,
        status: "U",
      };

      await dataAccess.saveTransaction(budget, updatedBudgetTx, false);

      const budgetIndex = budgets.value.findIndex((b) => b.budgetId === budgetId);
      if (budgetIndex !== -1) {
        budgets.value[budgetIndex].transactions = budgets.value[budgetIndex].transactions.map((tx) => (tx.id === id ? updatedBudgetTx : tx));
        budgetStore.updateBudget(budgetId, budgets.value[budgetIndex]);
      }

      if (importedTxIndex !== -1) {
        importedTransactions.value[importedTxIndex].matched = false;
        await dataAccess.updateImportedTransaction(docId, importedTransactions.value[importedTxIndex].id, false, false);
      }
    }

    if (!budgetId && transactionAction.value == "Delete") {
      await dataAccess.deleteImportedTransaction(docId, id);
      const importedTxIndex = importedTransactions.value.findIndex((tx) => tx.id === id);
      if (importedTxIndex !== -1) {
        importedTransactions.value[importedTxIndex].deleted = true;
      }
    } else if (!budgetId && transactionAction.value == "Ignore") {
      await dataAccess.updateImportedTransaction(docId, id, false, true);
      const importedTxIndex = importedTransactions.value.findIndex((tx) => tx.id === id);
      if (importedTxIndex !== -1) {
        importedTransactions.value[importedTxIndex].matched = false;
        importedTransactions.value[importedTxIndex].ignored = true;
      }
    }

    showSnackbar(`${transactionAction.value} action completed successfully`, "success");
  } catch (error: any) {
    console.error(`Error performing ${transactionAction.value} action:`, error);
    showSnackbar(`Error performing ${transactionAction.value} action: ${error.message}`, "error");
  } finally {
    loading.value = false;
    showActionDialog.value = false;
    transactionToAction.value = null;
    transactionAction.value = "";
  }
}

function openBatchMatchDialog() {
  if (
    selectedRows.value.length === 0 ||
    !selectedRows.value.every((id) => {
      const tx = displayTransactions.value?.find((t) => t.id === id);
      return tx && tx.status === "U";
    })
  ) {
    showSnackbar("Please select unmatched transactions to match", "error");
    return;
  }
  if (!entityOptions.value.length) {
    showSnackbar("No entities available. Please set up entities first.", "error");
    return;
  }
  const firstSelectedTx = displayTransactions.value?.find((t) => t.id === selectedRows.value[0]);
  batchMerchant.value = filterMerchant.value || (firstSelectedTx ? firstSelectedTx.merchant : "");
  batchCategory.value = "";
  selectedEntityId.value = familyStore.selectedEntityId || "";
  showBatchMatchDialog.value = true;
}

async function executeBatchMatch() {
  if (!batchMatchForm.value) return;

  const { valid } = await batchMatchForm.value.validate();
  if (!valid) {
    showSnackbar("Please fill in all required fields", "error");
    return;
  }

  saving.value = true;
  try {
    const user = auth.currentUser;
    if (!user) {
      showSnackbar("User not authenticated", "error");
      return;
    }

    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar("No family found", "error");
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
      const parts = id.split("-");
      const importedTxId = id;
      const docId = parts.slice(0, -1).join("-");
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
        targetBudget = await createBudgetForMonth(budgetMonth, family.id, ownerUid, selectedEntityId.value);
      }

      // Check if category exists in budget; if not, add it
      if (!targetBudget.categories.some((cat) => cat.name === batchCategory.value)) {
        targetBudget.categories.push({
          name: batchCategory.value,
          target: 0,
          isFund: false,
          group: "Ungrouped",
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
        notes: "",
        recurring: false,
        recurringInterval: "Monthly",
        userId: user.uid,
        isIncome: isIncome,
        accountSource: importedTx.accountSource || "",
        accountNumber: importedTx.accountNumber || "",
        postedDate: importedTx.postedDate || "",
        checkNumber: importedTx.checkNumber || "",
        importedMerchant: importedTx.payee || "",
        status: "C",
        entityId: selectedEntityId.value,
      };

      // Save budget transaction
      await dataAccess.saveTransaction(targetBudget, newTransaction, false);

      // Update local budgets
      const budgetIndex = budgets.value.findIndex((b) => b.budgetId === budgetId);
      if (budgetIndex !== -1) {
        budgets.value[budgetIndex].transactions = [...(budgets.value[budgetIndex].transactions || []), newTransaction];
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

    showSnackbar(`Successfully matched ${selectedRows.value.length} transaction${selectedRows.value.length > 1 ? "s" : ""}`, "success");
    selectedRows.value = [];
    showBatchMatchDialog.value = false;
    batchMerchant.value = "";
    batchCategory.value = "";
    selectedEntityId.value = "";
    const acct = selectedAccount.value;
    await loadData(); // Refresh data to reflect changes
    selectedAccount.value = acct;
  } catch (error: any) {
    console.error("Error performing batch match:", error);
    showSnackbar(`Error performing batch match: ${error.message}`, "error");
  } finally {
    saving.value = false;
  }
}

function confirmBatchAction(action: string) {
  if (
    selectedRows.value.length === 0 ||
    !selectedRows.value.every((id) => {
      const tx = displayTransactions.value?.find((t) => t.id === id);
      return tx && tx.status === "U" && !tx.budgetId;
    })
  ) {
    showSnackbar("Please select unmatched imported transactions", "error");
    return;
  }
  batchAction.value = action;
  showBatchActionDialog.value = true;
}

async function executeBatchAction() {
  if (!["Ignore", "Delete"].includes(batchAction.value)) {
    showBatchActionDialog.value = false;
    return;
  }

  saving.value = true;
  try {
    for (const txId of selectedRows.value) {
      const tx = displayTransactions.value?.find((t) => t.id === txId);
      if (!tx || tx.status !== "U" || tx.budgetId) continue;
      const parts = tx.id.split("-");
      const docId = parts.slice(0, -1).join("-");
      if (batchAction.value === "Delete") {
        await dataAccess.deleteImportedTransaction(docId, tx.id);
        const index = importedTransactions.value.findIndex((itx) => itx.id === tx.id);
        if (index !== -1) importedTransactions.value.splice(index, 1);
      } else if (batchAction.value === "Ignore") {
        await dataAccess.updateImportedTransaction(docId, tx.id, false, true);
        const index = importedTransactions.value.findIndex((itx) => itx.id === tx.id);
        if (index !== -1) importedTransactions.value[index].ignored = true;
      }
    }
    showSnackbar(
      `Successfully ${batchAction.value.toLowerCase()}d ${selectedRows.value.length} transaction${selectedRows.value.length > 1 ? "s" : ""}`,
      "success"
    );
    selectedRows.value = [];
  } catch (error: any) {
    console.error("Error performing batch action:", error);
    showSnackbar(`Error performing batch action: ${error.message}`, "error");
  } finally {
    saving.value = false;
    showBatchActionDialog.value = false;
    batchAction.value = "";
  }
}

async function createBudgetForMonth(month: string, familyId: string, ownerUid: string, entityId: string): Promise<Budget> {
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
  const availableBudgets = Array.from(budgetStore.budgets.values()).sort((a, b) => a.month.localeCompare(b.month));
  let sourceBudget: Budget | undefined;

  // Look for previous budget (preferred)
  const previousBudgets = availableBudgets.filter((b) => b.month < month && b.entityId === entityId);
  if (previousBudgets.length > 0) {
    sourceBudget = previousBudgets[previousBudgets.length - 1]; // Most recent previous
  } else {
    // Fall back to earliest future budget
    const futureBudgets = availableBudgets.filter((b) => b.month > month && b.entityId === entityId);
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
  const [newYear, newMonthNum] = month.split("-").map(Number);
  const [sourceYear, sourceMonthNum] = sourceBudget.month.split("-").map(Number);
  const isFutureMonth = newYear > sourceYear || (newYear === sourceYear && newMonthNum > sourceMonthNum);

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
    const recurringGroups = sourceBudget.transactions.reduce((groups, trx) => {
      if (!trx.deleted && trx.recurring) {
        const key = `${trx.merchant}-${trx.amount}-${trx.recurringInterval}-${trx.userId}-${trx.isIncome}`;
        if (!groups[key]) {
          groups[key] = [];
        }
        groups[key].push(trx);
      }
      return groups;
    }, {} as Record<string, Transaction[]>);

    Object.values(recurringGroups).forEach((group) => {
      const firstInstance = group.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())[0];
      if (firstInstance.recurringInterval === "Monthly") {
        const newDate = adjustTransactionDate(firstInstance.date, month, "Monthly");
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

  const { valid } = await adjustmentForm.value.validate();
  if (!valid) {
    showSnackbar("Please fill in all required fields", "error");
    return;
  }

  saving.value = true;
  try {
    const user = auth.currentUser;
    if (!user) {
      showSnackbar("User not authenticated", "error");
      return;
    }

    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar("No family found", "error");
      return;
    }

    const selectedAcc = accounts.value.find((acc) => acc.accountNumber === selectedAccount.value);
    if (!selectedAcc) {
      showSnackbar("Selected account not found", "error");
      return;
    }

    const newDocId = uuidv4();
    const adjustmentTransaction: ImportedTransaction = {
      id: `${newDocId}-0`,
      accountId: selectedAcc.id,
      accountNumber: selectedAccount.value,
      accountSource: selectedAcc.institution || "",
      payee: "Initial Balance Adjustment",
      postedDate: adjustmentDate.value,
      status: "U",
      creditAmount: adjustmentAmount.value >= 0 ? Math.abs(adjustmentAmount.value) : 0,
      debitAmount: adjustmentAmount.value < 0 ? Math.abs(adjustmentAmount.value) : 0,
      checkNumber: "",
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

    showSnackbar("Balance adjustment saved successfully", "success");
    closeBalanceAdjustmentDialog();
  } catch (error: any) {
    console.error("Error saving balance adjustment:", error);
    showSnackbar(`Error saving balance adjustment: ${error.message}`, "error");
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
    if (
      prevStatement === "ALL" ||
      (prevStatement && statements.value.some((s) => s.id === prevStatement))
    ) {
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

  const { valid } = await statementForm.value.validate();
  if (!valid) {
    showSnackbar("Please fill in all required fields", "error");
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
    showSnackbar("Statement saved successfully", "success");
    closeStatementDialog();
  } catch (error: any) {
    console.error("Error saving statement:", error);
    showSnackbar(`Error saving statement: ${error.message}`, "error");
  } finally {
    saving.value = false;
  }
}

function closeStatementDialog() {
  showStatementDialog.value = false;
  newStatement.value = {
    id: "",
    accountNumber: "",
    startDate: "",
    startingBalance: 0,
    endDate: "",
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
          tx.status = "R";
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
        const parts = id.split("-");
        const docId = parts.slice(0, -1).join("-");
        const updatedTx = { ...importedTransactions.value[idx], status: "R" };
        importedTransactions.value[idx].status = "R";
        await dataAccess.updateImportedTransaction(docId, updatedTx);
      }
    }

    const updated: Statement = { ...selectedStatement.value, reconciled: true };
    await statementStore.saveStatement(familyId.value, selectedAccount.value, updated, txRefs);
    statements.value = statementStore.getStatements(familyId.value, selectedAccount.value);
    showSnackbar("Statement reconciled", "success");
    reconciling.value = false;
    selectedRows.value = [];
  } catch (error: any) {
    console.error("Error reconciling statement:", error);
    showSnackbar(`Error reconciling statement: ${error.message}`, "error");
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
          tx.status === "R"
        ) {
          tx.status = "C";
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
        itx.status === "R"
      ) {
        const parts = itx.id.split("-");
        const docId = parts.slice(0, -1).join("-");
        const updatedTx = { ...itx, status: "C" };
        itx.status = "C";
        await dataAccess.updateImportedTransaction(docId, updatedTx);
      }
    }

    await statementStore.unreconcileStatement(
      familyId.value,
      selectedAccount.value,
      selectedStatement.value.id,
      txRefs
    );
    statements.value = statementStore.getStatements(
      familyId.value,
      selectedAccount.value
    );
    showSnackbar("Statement unreconciled", "success");
  } catch (error: any) {
    console.error("Error unreconciling statement:", error);
    showSnackbar(`Error unreconciling statement: ${error.message}`, "error");
  } finally {
    saving.value = false;
  }
}

async function deleteStatement() {
  if (!selectedStatement.value || !selectedAccount.value) return;
  if (!confirm("Delete this statement?")) return;
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
          tx.status === "R"
        ) {
          tx.status = "C";
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
        itx.status === "R"
      ) {
        const parts = itx.id.split("-");
        const docId = parts.slice(0, -1).join("-");
        const updatedTx = { ...itx, status: "C" };
        itx.status = "C";
        await dataAccess.updateImportedTransaction(docId, updatedTx);
      }
    }

    await statementStore.deleteStatement(
      familyId.value,
      selectedAccount.value,
      selectedStatement.value.id,
      txRefs
    );
    await statementStore.loadStatements(familyId.value, selectedAccount.value);
    statements.value = statementStore.getStatements(
      familyId.value,
      selectedAccount.value
    );
    if (statements.value.length > 0) {
      selectedStatementId.value = statements.value[statements.value.length - 1].id;
    } else {
      selectedStatementId.value = "ALL";
    }
    showSnackbar("Statement deleted", "success");
  } catch (error: any) {
    console.error("Error deleting statement:", error);
    showSnackbar(`Error deleting statement: ${error.message}`, "error");
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
    isIncome: tx.isIncome ? "true" : "false",
    status: tx.status,
    notes: tx.notes,
    balance: tx.balance ?? "",
  }));
  const csv = Papa.unparse(rows);
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const account = accounts.value.find((a) => a.accountNumber === selectedAccount.value);
  const name = account ? account.name.replace(/[^a-zA-Z0-9_-]/g, "_").toLowerCase() : "transactions";
  const today = todayISO();
  saveAs(blob, `${name}_${today}.csv`);
}

function showSnackbar(text: string, color = "success") {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
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
