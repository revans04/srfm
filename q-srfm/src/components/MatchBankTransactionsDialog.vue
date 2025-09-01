<!-- src/components/BankTransactionMatchingDialog.vue -->
<template>
  <div v-if="isReady">
    <q-card>
      <q-card-section>
        <!-- Smart Matches Header -->
        <div class="row items-center q-gutter-md q-mb-md">
          <div class="col">
            <div class="row items-center q-gutter-sm">
              <h3 class="q-mb-none">Smart Matches ({{ smartMatches.length }})</h3>
              <q-input
                v-model="smartMatchDateRange"
                label="Days"
                type="number"
                dense
                class="q-ml-md"
                style="width: 90px"
                @input="computeSmartMatchesLocal()"
              />
            </div>
            <p class="text-caption q-mt-xs q-mb-none">These imported transactions have exactly one potential match. Review and confirm below (max 50 at a time).</p>
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
              {{ smartMatchesSortDirection === "asc" ? "Ascending" : "Descending" }}
            </q-btn>
          </div>
          <div class="col">
            <q-btn
              color="primary"
              @click="confirmSmartMatches"
              :disabled="selectedSmartMatchIds.length === 0 || props.matching"
              :loading="props.matching"
            >
              Confirm Selected Matches ({{ selectedSmartMatchIds.length }})
            </q-btn>
          </div>
        </div>

                  <q-table
                  v-if="smartMatches.length > 0"
                  :columns="smartMatchColumns"
                  :rows="sortedSmartMatches"
                  :row-key="rowKey"
                  selection="multiple"
                  v-model:selected="selectedSmartMatchesInternal"
                  class="q-mt-lg"
                  hide-bottom
                  :pagination="{ rowsPerPage: 50 }"
                >
                  <template #body-cell-bankAmount="slotProps">
                    <q-td :props="slotProps" class="text-right">
                      ${{ toDollars(toCents(slotProps.row.bankAmount)) }}
                    </q-td>
                  </template>
                  <template #body-cell-budgetAmount="slotProps">
                    <q-td :props="slotProps" class="text-right">
                      ${{ toDollars(toCents(slotProps.row.budgetAmount)) }}
                    </q-td>
                  </template>
                  <template #body-cell-actions="slotProps">
                    <q-td :props="slotProps">
                      <q-icon
                        v-if="isBudgetTxMatchedMultiple(slotProps.row.budgetTransaction.id)"
                        color="warning"
                        title="This budget transaction matches multiple bank transactions"
                        name="warning"
                      ></q-icon>
                    </q-td>
                  </template>
                </q-table>

                <q-banner v-else type="info" class="q-mt-lg">
                  No smart matches found. Check Remaining Transactions for potential conflicts.
                </q-banner>

        <!-- Remaining Transactions -->
        <div class="row q-mt-lg" v-if="remainingImportedTransactions.length > 0">
              <div class="col">
                <h3>Remaining Transactions ({{ currentBankTransactionIndex + 1 }} of {{ remainingImportedTransactions.length }})</h3>
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
                      <td>{{ selectedBankTransaction?.postedDate }}</td>
                      <td>{{ selectedBankTransaction?.payee }}</td>
                      <td>${{ toDollars(toCents(selectedBankTransaction?.debitAmount || selectedBankTransaction?.creditAmount || 0)) }}</td>
                      <td>
                        {{ selectedBankTransaction?.debitAmount ? "Debit" : selectedBankTransaction?.creditAmount ? "Credit" : "N/A" }}
                      </td>
                      <td>{{ selectedBankTransaction?.accountSource }}</td>
                      <td>{{ selectedBankTransaction?.accountNumber }}</td>
                      <td>{{ selectedBankTransaction?.checkNumber || "N/A" }}</td>
                    </tr>
                  </tbody>
                </q-markup-table>

                <!-- Search Filters -->
                <div class="row q-mt-lg" >
                  <div class="col col-12 col-md-4">
                    <q-input v-model="searchAmount" label="Amount" type="number" variant="outlined" readonly></q-input>
                  </div>
                  <div class="col col-12 col-md-4">
                    <q-input v-model="searchMerchant" label="Merchant" variant="outlined" @input="searchBudgetTransactions"></q-input>
                  </div>
                  <div class="col col-12 col-md-4">
                    <q-input
                      v-model="searchDateRange"
                      label="Date Range (days)"
                      type="number"
                      variant="outlined"
                      @input="searchBudgetTransactions"
                    ></q-input>
                  </div>
                </div>

                <!-- Split Transaction Option -->
                <div class="row q-mt-lg" >
                  <div class="col">
                    <q-btn color="primary" @click="toggleSplitTransaction" :disabled="props.matching">
                      {{ showSplitForm ? "Cancel Split" : "Split Transaction" }}
                    </q-btn>
                  </div>
                </div>

                <!-- Split Transaction Form -->
                <q-form v-if="showSplitForm" ref="splitForm" @submit.prevent="saveSplitTransaction" class="q-mt-lg">
                  <div class="row align-center" v-for="(split, index) in transactionSplits" :key="index" >
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
                    <div class="col col-12 col-md-2">
                      <q-input
                        v-model.number="split.amount"
                        label="Amount"
                        type="number"
                        variant="outlined"
                        density="compact"
                        :rules="[(v: number) => v > 0 || 'Amount must be greater than 0']"
                      ></q-input>
                    </div>
                    <div class="col col-12 col-md-1">
                      <q-btn color="negative" icon="close" @click="removeSplit(index)" variant="plain"></q-btn>
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
                <div class="row q-mb-lg" v-if="!showSplitForm" >
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
                      {{ potentialMatchesSortDirection === "asc" ? "Ascending" : "Descending" }}
                    </q-btn>
                  </div>
                </div>

                <q-table
                  v-if="potentialMatches.length > 0 && !showSplitForm"
                  :columns="budgetTransactionColumns"
                  :rows="sortedPotentialMatches"
                  :pagination="{ rowsPerPage: 5 }"
                  row-key="id"
                  selection="single"
                  v-model:selected="selectedBudgetTransactionForMatch"
                >
                  <template #body-cell-amount="slotProps">
                    <q-td :props="slotProps" class="text-right">
                      ${{ toDollars(toCents(slotProps.row.amount)) }}
                    </q-td>
                  </template>
                  <template #body-cell-type="slotProps">
                    <q-td :props="slotProps">
                      {{ slotProps.row.isIncome ? "Income" : "Expense" }}
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
            </div>
            <div class="row q-mt-sm" v-else >
              <div class="col">
                <q-banner type="positive"> All bank transactions have been matched or ignored. </q-banner>
              </div>
            </div>
      </q-card-section>
      <q-card-actions>
        <q-btn v-if="remainingImportedTransactions.length > 0" color="warning" @click="ignoreBankTransaction" :disabled="props.matching"> Ignore </q-btn>
        <q-btn v-if="remainingImportedTransactions.length > 0" color="secondary" @click="skipBankTransaction" :disabled="props.matching"> Skip </q-btn>
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

    <!-- Snackbar handled via $q.notify -->
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted, nextTick } from "vue";
import type { Transaction, ImportedTransaction } from "../types";
import { useQuasar } from 'quasar';
import { toDollars, toCents, toBudgetMonth, todayISO } from "../utils/helpers";
import { dataAccess } from "../dataAccess";
import { useBudgetStore } from "../store/budget";
import { useFamilyStore } from "../store/family";
import { auth } from "../firebase/init";
import TransactionDialog from "./TransactionDialog.vue";
import { QForm } from "quasar";
import { v4 as uuidv4 } from "uuid";
import { createBudgetForMonth } from "../utils/budget";

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
  (e: "add-new-transaction", importedTx: ImportedTransaction): void;
  (e: "transactions-updated"): void;
  (e: "update:matching", value: boolean): void;
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
};

const smartMatches = ref<SmartMatchRow[]>([]);
const remainingImportedTransactions = ref<ImportedTransaction[]>(props.remainingImportedTransactions || []);
const selectedBankTransaction = ref<ImportedTransaction | null>(props.selectedBankTransaction || null);

// Snackbar timeout state for notifications
const timeout = ref(3000);
const findingSmartMatches = ref(false);

// Local state for Smart Matches sorting
const selectedSmartMatchIds = ref<string[]>([]);
const smartMatchesSortField = ref<string>("bankDate");
const smartMatchesSortDirection = ref<"asc" | "desc">("asc");
const smartMatchesSortFields = [
  { text: "Bank Date", value: "bankDate" },
  { text: "Merchant", value: "merchant" },
  { text: "Amount", value: "bankAmount" },
];
const smartMatchDateRange = ref<string>("3");

// Local state for Remaining Transactions
const currentBankTransactionIndex = ref<number>(0);
const searchAmount = ref<string>("");
const searchMerchant = ref<string>("");
const searchDateRange = ref<string>("3");
const potentialMatches = ref<Transaction[]>([]);
const selectedBudgetTransactionForMatch = ref<Transaction[]>([]);
const potentialMatchesSortField = ref<string>("date");
const potentialMatchesSortDirection = ref<"asc" | "desc">("asc");
const potentialMatchesSortFields = [
  { text: "Date", value: "date" },
  { text: "Merchant", value: "merchant" },
  { text: "Amount", value: "amount" },
];

// Split transaction state
const showSplitForm = ref(false);
const transactionSplits = ref<Array<{ entityId: string; category: string; amount: number }>>([{ entityId: "", category: "", amount: 0 }]);
const splitForm = ref<InstanceType<typeof QForm> | null>(null);

const showTransactionDialog = ref(false);
const newTransaction = ref<Transaction>({
  id: "",
  budgetMonth: "",
  date: "",
  merchant: "",
  categories: [{ category: "", amount: 0 }],
  amount: 0,
  notes: "",
  recurring: false,
  recurringInterval: "Monthly",
  userId: "",
  isIncome: false,
  taxMetadata: [],
} as Transaction);
const newTransactionBudgetId = ref<string>(""); // Track budgetId for TransactionDialog


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
  { name: 'merchant', label: 'Merchant', field: 'merchant', sortable: true },
  { name: 'budgetDate', label: 'Budget Date', field: 'budgetDate', sortable: true },
  { name: 'budgetAmount', label: 'Budget Amount', field: 'budgetAmount', sortable: true },
  { name: 'budgetType', label: 'Budget Type', field: 'budgetType' },
  { name: 'actions', label: 'Actions', field: 'actions' },
];

const rowKey = (row: SmartMatchRow) => row.importedTransaction.id;

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
    // Return selected rows as-is; template only needs length
    return smartMatches.value.filter((m) => selectedSmartMatchIds.value.includes(m.importedTransaction.id));
  },
  set(rows: SmartMatchRow[]) {
    selectedSmartMatchIds.value = Array.isArray(rows)
      ? rows
          .map((r) => r.importedTransaction?.id)
          .filter((id): id is string => Boolean(id))
      : [];
  },
});

// Computed properties
const sortedSmartMatches = computed(() => {
  const items = [...smartMatches.value];
  const field = smartMatchesSortField.value;
  const direction = smartMatchesSortDirection.value;

  return items.sort((a, b) => {
    let valueA: number | string = 0;
    let valueB: number | string = 0;

    if (field === "bankDate") {
      valueA = new Date(a.bankDate).getTime();
      valueB = new Date(b.bankDate).getTime();
    } else if (field === "merchant") {
      valueA = a.merchant.toLowerCase();
      valueB = b.merchant.toLowerCase();
    } else if (field === "bankAmount") {
      valueA = a.bankAmount;
      valueB = b.bankAmount;
    }

    if (valueA < valueB) return direction === "asc" ? -1 : 1;
    if (valueA > valueB) return direction === "asc" ? 1 : -1;
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

    if (field === "date") {
      valueA = new Date(a.date).getTime();
      valueB = new Date(b.date).getTime();
    } else if (field === "merchant") {
      valueA = a.merchant.toLowerCase();
      valueB = b.merchant.toLowerCase();
    } else if (field === "amount") {
      valueA = a.amount;
      valueB = b.amount;
    }

    if (valueA < valueB) return direction === "asc" ? -1 : 1;
    if (valueA > valueB) return direction === "asc" ? 1 : -1;
    return 0;
  });
});

// Watchers and Initialization
onMounted(async () => {
  await initializeState();
});

watch(
  () => smartMatchDateRange.value,
  () => {
    computeSmartMatchesLocal();
  }
);

watch(
  () => props.transactions,
  () => {
    computeSmartMatchesLocal();
  },
  { deep: true }
);

watch(
  () => props.remainingImportedTransactions,
  (newVal) => {
    remainingImportedTransactions.value = Array.isArray(newVal) ? [...newVal] : [];
    computeSmartMatchesLocal();
  },
  { deep: true }
);

async function initializeState() {
  remainingImportedTransactions.value = Array.isArray(props.remainingImportedTransactions) ? [...props.remainingImportedTransactions] : [];
  selectedBankTransaction.value = props.selectedBankTransaction || remainingImportedTransactions.value[0] || null;

  smartMatches.value = [];
  smartMatchDateRange.value = "3";
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
      searchAmount.value = newVal.debitAmount ? newVal.debitAmount.toString() : newVal.creditAmount?.toString() || "0";
      searchMerchant.value = "";
      searchDateRange.value = "3";
      transactionSplits.value = [{ entityId: familyStore.selectedEntityId || "", category: "", amount: 0 }]; // Reset splits
      showSplitForm.value = false;
      searchBudgetTransactions();
    }
  }
);

watch(
  () => remainingImportedTransactions.value,
  (newVal) => {
    if (newVal && newVal.length > 0) {
      currentBankTransactionIndex.value = Math.min(currentBankTransactionIndex.value, newVal.length - 1);
      if (!selectedBankTransaction.value || !newVal.some((tx) => tx.id === selectedBankTransaction.value?.id)) {
        currentBankTransactionIndex.value = 0;
        selectedBankTransaction.value = newVal[0] ?? null;
        searchBudgetTransactions();
      }
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }
  },
  { deep: true }
);

// Methods
function findSmartMatches() {
  findingSmartMatches.value = true;
  try {
    smartMatches.value = [];
    computeSmartMatchesLocal();
    showSnackbar(
      `Found ${smartMatches.value.length} smart match${smartMatches.value.length !== 1 ? "es" : ""}`,
      "info"
    );
  } catch (error: unknown) {
    const err = error as Error;
    console.error("Error finding smart matches:", err);
    showSnackbar(`Error finding smart matches: ${err.message}`, "negative");
  } finally {
    findingSmartMatches.value = false;
  }
}

async function confirmSmartMatches() {
  if (selectedSmartMatchIds.value.length === 0) {
    showSnackbar("No smart matches selected to confirm", "negative");
    return;
  }

  emit("update:matching", true);
  try {
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

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

    for (const budgetId in matchesByBudget) {
      const budget = budgetStore.getBudget(budgetId);
      if (!budget) throw new Error(`Budget ${budgetId} not found`);

      const reconcileData: {
        budgetId: string;
        reconciliations: {
          budgetTransactionId: string;
          importedTransactionId: string;
          match: boolean;
          ignore: boolean;
        }[];
      } = {
        budgetId,
        reconciliations: matchesByBudget[budgetId] || [],
      };

      await dataAccess.batchReconcileTransactions(budgetId, budget, reconcileData);
      const updatedBudget = await dataAccess.getBudget(budgetId);
      if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
    }

    showSnackbar(`${matchesToConfirm.length} smart matches confirmed successfully`);
    emit("transactions-updated");
    computeSmartMatchesLocal(matchesToConfirm);
    updateRemainingTransactions();
  } catch (error: unknown) {
    const err = error as Error;
    console.error("Error confirming smart matches:", err);
    showSnackbar(`Error confirming smart matches: ${err.message}`, "negative");
  } finally {
    emit("update:matching", false);
  }
}

async function matchBankTransaction(budgetTransaction: Transaction) {
  if (!selectedBankTransaction.value || !budgetTransaction) {
    showSnackbar("Please select a budget transaction to match", "negative");
    return;
  }

  emit("update:matching", true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

    const updatedTransaction: Transaction = {
      ...budgetTransaction,
      accountSource: importedTx.accountSource || "",
      accountNumber: importedTx.accountNumber || "",
      postedDate: importedTx.postedDate || "",
      importedMerchant: importedTx.payee || "",
      status: "C",
      id: budgetTransaction.id,
      userId: budgetTransaction.userId || user.uid,
      budgetMonth: budgetTransaction.budgetMonth || toBudgetMonth(importedTx.postedDate || todayISO()),
      date: budgetTransaction.date || importedTx.postedDate || todayISO(),
      merchant: budgetTransaction.merchant || importedTx.payee || "",
      categories: budgetTransaction.categories || [{ category: "", amount: 0 }],
      amount: budgetTransaction.amount || importedTx.debitAmount || importedTx.creditAmount || 0,
      notes: budgetTransaction.notes || "",
      recurring: budgetTransaction.recurring || false,
      recurringInterval: budgetTransaction.recurringInterval || "Monthly",
      isIncome: budgetTransaction.isIncome || !!importedTx.creditAmount,
      entityId: budgetTransaction.entityId || familyStore.selectedEntityId || "",
      taxMetadata: budgetTransaction.taxMetadata || [],
    };

    const existingBudgetId =
      'budgetId' in budgetTransaction && typeof (budgetTransaction as { budgetId?: unknown }).budgetId === 'string'
        ? (budgetTransaction as { budgetId: string }).budgetId
        : undefined;
    const targetBudgetIdToUse =
      existingBudgetId || `${user.uid}_${updatedTransaction.entityId}_${updatedTransaction.budgetMonth}`;
    let budget = budgetStore.getBudget(targetBudgetIdToUse);
    if (!budget) {
      const fam = await familyStore.getFamily();
      budget = await createBudgetForMonth(
        updatedTransaction.budgetMonth,
        fam?.id ?? "",
        user.uid,
        updatedTransaction.entityId || ""
      );
    }

    await dataAccess.saveTransaction(budget, updatedTransaction, false);

    const parts = importedTx.id.split("-");
    const docId = parts.slice(0, -1).join("-");
    await dataAccess.updateImportedTransaction(docId, importedTx.id, true);

    showSnackbar("Transaction matched successfully");
    emit("transactions-updated");
    updateRemainingTransactions();
    skipBankTransaction();
  } catch (error: unknown) {
    const err = error as Error;
    console.log(err);
    showSnackbar(`Error matching transaction: ${err.message}`, "negative");
  } finally {
    emit("update:matching", false);
  }
}

async function ignoreBankTransaction() {
  if (!selectedBankTransaction.value) {
    showSnackbar("No bank transaction selected to ignore", "negative");
    return;
  }

  emit("update:matching", true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

    const parts = importedTx.id.split("-");
    const docId = parts.slice(0, -1).join("-");
    await dataAccess.updateImportedTransaction(docId, importedTx.id, undefined, true);

    showSnackbar("Bank transaction ignored");
    updateRemainingTransactions();
    skipBankTransaction();
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error ignoring transaction: ${err.message}`, "negative");
  } finally {
    emit("update:matching", false);
  }
}

function skipBankTransaction() {
  if (currentBankTransactionIndex.value + 1 < remainingImportedTransactions.value.length) {
    currentBankTransactionIndex.value++;
    selectedBankTransaction.value =
      remainingImportedTransactions.value[currentBankTransactionIndex.value] ?? null;
    searchBudgetTransactions();
  } else {
    if (smartMatches.value.length === 0) {
      emit("transactions-updated");
      showSnackbar("All bank transactions have been processed", "success");
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }
  }
}

async function saveSplitTransaction() {
  if (!selectedBankTransaction.value || remainingSplitAmount.value !== 0) {
    showSnackbar("Invalid split amounts or no bank transaction selected", "negative");
    return;
  }

  if (!splitForm.value) return;

  const valid = await splitForm.value.validate();
  if (!valid) {
    showSnackbar("Please fill in all required fields", "negative");
    return;
  }

  emit("update:matching", true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

    const family = await familyStore.getFamily();
    if (!family) throw new Error("No family found");

    // Group splits by budget (based on entityId and budgetMonth)
    const transactionsByBudget: { [budgetId: string]: Transaction[] } = {};
    for (const split of transactionSplits.value) {
      const budgetMonth = toBudgetMonth(importedTx.postedDate || todayISO());
      const budgetId = `${user.uid}_${split.entityId}_${budgetMonth}`;
      const baseTx = {
        id: uuidv4(),
        budgetMonth,
        date: importedTx.postedDate || todayISO(),
        merchant: importedTx.payee || "",
        categories: [{ category: split.category, amount: split.amount }],
        amount: split.amount,
        notes: "",
        recurring: false,
        recurringInterval: "Monthly" as const,
        userId: props.userId,
        isIncome: !!importedTx.creditAmount,
        status: "C" as const,
        entityId: split.entityId,
        taxMetadata: [] as Transaction['taxMetadata'],
      };
      const transaction: Transaction = {
        ...baseTx,
        ...(importedTx.postedDate ? { postedDate: importedTx.postedDate } : {}),
        ...(importedTx.payee ? { importedMerchant: importedTx.payee } : {}),
        ...(importedTx.accountSource ? { accountSource: importedTx.accountSource } : {}),
        ...(importedTx.accountNumber ? { accountNumber: importedTx.accountNumber } : {}),
        ...(importedTx.checkNumber ? { checkNumber: importedTx.checkNumber } : {}),
      };

      if (!transactionsByBudget[budgetId]) transactionsByBudget[budgetId] = [];
      transactionsByBudget[budgetId].push(transaction);
    }

    // Save transactions to their respective budgets
    for (const budgetId in transactionsByBudget) {
      let budget = budgetStore.getBudget(budgetId);
      if (!budget) {
        const [, entityId, month] = budgetId.split("_");
        budget = await createBudgetForMonth(month, family.id, user.uid, entityId);
      }

      await dataAccess.batchSaveTransactions(budgetId, budget, transactionsByBudget[budgetId] || []);
      const updatedBudget = await dataAccess.getBudget(budgetId);
      if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
    }

    const parts = importedTx.id.split("-");
    const docId = parts.slice(0, -1).join("-");
    await dataAccess.updateImportedTransaction(docId, importedTx.id, true);

    remainingImportedTransactions.value = remainingImportedTransactions.value.filter((tx) => tx.id !== importedTx.id);
    if (remainingImportedTransactions.value.length > 0) {
      currentBankTransactionIndex.value = Math.min(currentBankTransactionIndex.value, remainingImportedTransactions.value.length - 1);
      selectedBankTransaction.value =
        remainingImportedTransactions.value[currentBankTransactionIndex.value] ?? null;
      searchBudgetTransactions();
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }

    showSnackbar("Split transaction saved successfully");
    emit("transactions-updated");
    resetSplitForm();
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error saving split transaction: ${err.message}`, "negative");
  } finally {
    emit("update:matching", false);
  }
}

async function handleTransactionAdded(savedTransaction: Transaction) {
  if (!selectedBankTransaction.value) return;

  emit("update:matching", true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

    const family = await familyStore.getFamily();
    if (!family) throw new Error("No family found");

    const targetBudgetId = `${user.uid}_${savedTransaction.entityId}_${savedTransaction.budgetMonth}`;
    let budget = budgetStore.getBudget(targetBudgetId);
    if (!budget) {
      budget = await createBudgetForMonth(savedTransaction.budgetMonth, family.id, user.uid, savedTransaction.entityId || "");
    }

    budgetStore.updateBudget(targetBudgetId, {
      ...budget,
      transactions: [...budget.transactions, savedTransaction],
    });

    const parts = importedTx.id.split("-");
    const docId = parts.slice(0, -1).join("-");
    await dataAccess.updateImportedTransaction(docId, importedTx.id, true);

    remainingImportedTransactions.value = remainingImportedTransactions.value.filter((tx) => tx.id !== importedTx.id);
    if (remainingImportedTransactions.value.length > 0) {
      currentBankTransactionIndex.value = Math.min(currentBankTransactionIndex.value, remainingImportedTransactions.value.length - 1);
      selectedBankTransaction.value =
        remainingImportedTransactions.value[currentBankTransactionIndex.value] ?? null;
      searchBudgetTransactions();
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }

    showSnackbar("Transaction added and matched successfully");
    emit("transactions-updated");
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error adding transaction: ${err.message}`, "negative");
  } finally {
    emit("update:matching", false);
    showTransactionDialog.value = false;
  }
}

function addNewTransaction() {
  if (!selectedBankTransaction.value) {
    showSnackbar("No bank transaction selected to add", "negative");
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar("Please select an entity before adding a transaction", "negative");
    return;
  }

  const postedDate = selectedBankTransaction.value.postedDate || todayISO();
  let budgetMonth = toBudgetMonth(postedDate);

  if (!budgetStore.availableBudgetMonths.includes(budgetMonth)) {
    budgetMonth = budgetStore.availableBudgetMonths[budgetStore.availableBudgetMonths.length - 1] || budgetMonth;
    console.log(`Budget month ${toBudgetMonth(postedDate)} not found, falling back to ${budgetMonth}`);
  }

  const baseTx = {
    id: "",
    budgetMonth,
    date: selectedBankTransaction.value.postedDate || todayISO(),
    merchant: selectedBankTransaction.value.payee || "",
    categories: [
      {
        category: "",
        amount: selectedBankTransaction.value.debitAmount || selectedBankTransaction.value.creditAmount || 0,
      },
    ],
    amount: selectedBankTransaction.value.debitAmount || selectedBankTransaction.value.creditAmount || 0,
    notes: "",
    recurring: false,
    recurringInterval: "Monthly" as const,
    userId: props.userId,
    isIncome: !!selectedBankTransaction.value.creditAmount,
    status: "C" as const,
    entityId: familyStore.selectedEntityId || "",
    taxMetadata: [] as Transaction['taxMetadata'],
  };
  newTransaction.value = {
    ...baseTx,
    ...(selectedBankTransaction.value.postedDate ? { postedDate: selectedBankTransaction.value.postedDate } : {}),
    ...(selectedBankTransaction.value.payee ? { importedMerchant: selectedBankTransaction.value.payee } : {}),
    ...(selectedBankTransaction.value.accountSource ? { accountSource: selectedBankTransaction.value.accountSource } : {}),
    ...(selectedBankTransaction.value.accountNumber ? { accountNumber: selectedBankTransaction.value.accountNumber } : {}),
    ...(selectedBankTransaction.value.checkNumber ? { checkNumber: selectedBankTransaction.value.checkNumber } : {}),
  } as Transaction;
  newTransactionBudgetId.value = `${props.userId}_${familyStore.selectedEntityId}_${budgetMonth}`;
  showTransactionDialog.value = true;
}

function toggleSplitTransaction() {
  showSplitForm.value = !showSplitForm.value;
  if (showSplitForm.value) {
    transactionSplits.value = [{ entityId: familyStore.selectedEntityId || "", category: "", amount: 0 }];
  } else {
    resetSplitForm();
  }
}

function addSplitTransaction() {
  transactionSplits.value.push({ entityId: familyStore.selectedEntityId || "", category: "", amount: 0 });
}

function removeSplit(index: number) {
  if (transactionSplits.value.length > 1) {
    transactionSplits.value.splice(index, 1);
  }
}

function resetSplitForm() {
  transactionSplits.value = [{ entityId: familyStore.selectedEntityId || "", category: "", amount: 0 }];
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
  smartMatchesSortDirection.value = smartMatchesSortDirection.value === "asc" ? "desc" : "asc";
}

function sortPotentialMatches() {
  // Sorting handled by computed property
}

function togglePotentialMatchesSortDirection() {
  potentialMatchesSortDirection.value = potentialMatchesSortDirection.value === "asc" ? "desc" : "asc";
}

function searchBudgetTransactions() {
  if (!selectedBankTransaction.value) return;

  const bankTx = selectedBankTransaction.value;
  const amount = parseFloat(searchAmount.value) || bankTx.debitAmount || bankTx.creditAmount || 0;
  const merchant = searchMerchant.value.toLowerCase();
  const dateRangeDays = parseInt(searchDateRange.value) || 3;

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
    const amountMatch = Math.abs(txAmount - amount) <= 0.05;
    const merchantMatch = merchant ? txMerchant.includes(merchant) : true;

    return !tx.deleted && dateMatch && amountMatch && merchantMatch && (!tx.status || tx.status === "U");
  });
}

function computeSmartMatchesLocal(confirmedMatches: typeof smartMatches.value = []) {
  console.log("Computing smart matches...");
  const confirmedIds = new Set(confirmedMatches.map((m) => m.importedTransaction.id));
  const newSmartMatches = smartMatches.value.filter((match) => !confirmedIds.has(match.importedTransaction.id));

  const dateRangeDays = parseInt(smartMatchDateRange.value) || 3;
  const unmatchedImported = remainingImportedTransactions.value.filter(
    (tx) => !confirmedIds.has(tx.id) && !newSmartMatches.some((m) => m.importedTransaction.id === tx.id)
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
    const bankDateStr = bankDate.toISOString().split("T")[0];
    const normalizedBankDate = new Date(bankDateStr);

    const startDate = new Date(normalizedBankDate);
    startDate.setDate(normalizedBankDate.getDate() - dateRangeDays);
    const endDate = new Date(normalizedBankDate);
    endDate.setDate(normalizedBankDate.getDate() + dateRangeDays);

    props.transactions.forEach((tx) => {
      const txDate = new Date(tx.date);
      const txDateStr = txDate.toISOString().split("T")[0];
      const normalizedTxDate = new Date(txDateStr);
      const txAmount = tx.amount;
      const typeMatch = tx.isIncome === !!importedTx.creditAmount;
      if (
        normalizedTxDate >= startDate &&
        normalizedTxDate <= endDate &&
        Math.abs(txAmount - bankAmount) <= 0.05 &&
        (!tx.status || tx.status === "U") &&
        !tx.deleted &&
        typeMatch
      ) {
        potentialMatches.push({
          importedTx,
          budgetTx: tx,
          budgetId:
            ('budgetId' in tx && typeof (tx as { budgetId?: unknown }).budgetId === 'string'
              ? (tx as { budgetId: string }).budgetId
              : `${props.userId}_${tx.entityId}_${toBudgetMonth(importedTx.postedDate)}`),
          bankAmount,
          bankType: importedTx.debitAmount ? "Debit" : "Credit",
          merchantMatch: !!importedTx.payee && importedTx.payee.toLowerCase().includes(tx.merchant.toLowerCase()),
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
      chosen = available[0]!;
    } else {
      const merchantMatches = available.filter((c) => c.merchantMatch);
      if (merchantMatches.length === 1) {
        chosen = merchantMatches[0]!;
      } else {
        const dateMatches = available.filter((c) => c.dateExact);
        if (dateMatches.length === 1) {
          chosen = dateMatches[0]!;
        }
      }
    }

    if (chosen && !usedBudgetTxIds.has(chosen.budgetTx.id) && !usedBankTxIds.has(chosen.importedTx.id)) {
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
      bankDate: match.importedTx.postedDate,
      payee: match.importedTx.payee,
      merchant: match.budgetTx.merchant,
      budgetDate: match.budgetTx.date,
      budgetAmount: match.budgetTx.amount,
      budgetType: match.budgetTx.isIncome ? 'Income' : 'Expense',
    });
  });

  smartMatches.value = newSmartMatches;
  selectedSmartMatchIds.value = [];

  const smartMatchImportedIds = new Set(smartMatches.value.map((m) => m.importedTransaction.id));
  remainingImportedTransactions.value = unmatchedImported.filter((tx) => !smartMatchImportedIds.has(tx.id));
}

function updateRemainingTransactions() {
  const matchedIds = new Set(
    smartMatches.value.filter((match) => selectedSmartMatchIds.value.includes(match.importedTransaction.id)).map((match) => match.importedTransaction.id)
  );
  remainingImportedTransactions.value = remainingImportedTransactions.value.filter((importedTx) => !matchedIds.has(importedTx.id));

  if (remainingImportedTransactions.value.length > 0) {
    currentBankTransactionIndex.value = Math.min(currentBankTransactionIndex.value, remainingImportedTransactions.value.length - 1);
    selectedBankTransaction.value =
      remainingImportedTransactions.value[currentBankTransactionIndex.value] ?? null;
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
  smartMatchesSortField.value = "bankDate";
  smartMatchesSortDirection.value = "asc";
  potentialMatchesSortField.value = "date";
  potentialMatchesSortDirection.value = "asc";
  if (computeMatches) computeSmartMatchesLocal();
  else console.log("Skipping computeSmartMatchesLocal on initial reset");
}

function showSnackbar(text: string, color = "success") {
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
