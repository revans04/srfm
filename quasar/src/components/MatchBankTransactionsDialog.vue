<!-- src/components/BankTransactionMatchingDialog.vue -->
<template>
  <q-dialog v-if="isReady" :model-value="props.showDialog" :fullscreen="isMobile" persistent @update:model-value="closeDialog($event)">
    <q-card>
      <q-card-section>Match Bank Transactions</q-card-section>
      <q-card-section>
        <div class="row">
          <div class="col"><q-space></q-space></div>
          <div class="col-auto text-right">
            <q-btn color="error" @click="closeDialog(false)" :disabled="props.matching" variant="plain">
              <q-icon>mdi-close</q-icon>
            </q-btn>
          </div>
        </div>
        <q-tabs v-model="activeTab" grow>
          <q-tab value="smart-matches">Smart Matches</q-tab>
          <q-tab value="remaining">Remaining Transactions</q-tab>
        </q-tabs>

        <q-window v-model="activeTab">
          <!-- Smart Matches Tab -->
          <q-window-item value="smart-matches">
            <q-row v-if="smartMatches.length > 0" class="mt-4">
              <q-col>
                <h3>Smart Matches ({{ smartMatches.length }})</h3>
                <p class="text-caption pb-2">These imported transactions have exactly one potential match. Review and confirm below (max 50 at a time).</p>

                <!-- Sort Controls -->
                <q-row class="mb-4">
                  <q-col cols="12" md="4">
                    <q-select
                      v-model="smartMatchesSortField"
                      :items="smartMatchesSortFields"
                      label="Sort By"
                      variant="outlined"
                      density="compact"
                      item-title="text"
                      item-value="value"
                      @update:model-value="sortSmartMatches"
                    ></q-select>
                  </q-col>
                  <q-col cols="12" md="4">
                    <q-btn :color="smartMatchesSortDirection === 'asc' ? 'primary' : 'secondary'" @click="toggleSmartMatchesSortDirection">
                      {{ smartMatchesSortDirection === "asc" ? "Ascending" : "Descending" }}
                    </q-btn>
                  </q-col>
                  <q-col cols="12" md="4">
                    <q-text-field
                      v-model="smartMatchDateRange"
                      label="Match Date Range (days)"
                      type="number"
                      variant="outlined"
                      @input="computeSmartMatchesLocal()"
                    ></q-text-field>
                  </q-col>
                  <q-col>
                    <q-btn
                      color="primary"
                      @click="confirmSmartMatches"
                      :disabled="selectedSmartMatchIds.length === 0 || props.matching"
                      :loading="props.matching"
                    >
                      Confirm Selected Matches ({{ selectedSmartMatchIds.length }})
                    </q-btn>
                  </q-col>
                </q-row>

                <q-data-table
                  :headers="smartMatchHeaders"
                  :items="sortedSmartMatches"
                  v-model="selectedSmartMatchIds"
                  show-select
                  item-value="importedTransaction.id"
                  class="mt-4"
                  hide-default-footer
                  items-per-page="50"
                >
                  <template v-slot:item.bankAmount="{ item }"> ${{ toDollars(toCents(item.bankAmount)) }} </template>
                  <template v-slot:item.bankType="{ item }"> {{ item.bankType }} </template>
                  <template v-slot:item.budgetAmount="{ item }"> ${{ toDollars(toCents(item.budgetTransaction.amount)) }} </template>
                  <template v-slot:item.budgetType="{ item }"> {{ item.budgetTransaction.isIncome ? "Income" : "Expense" }} </template>
                  <template v-slot:item.actions="{ item }">
                    <q-icon
                      v-if="isBudgetTxMatchedMultiple(item.budgetTransaction.id)"
                      color="warning"
                      title="This budget transaction matches multiple bank transactions"
                    >
                      mdi-alert
                    </q-icon>
                  </template>
                </q-data-table>
                <q-btn color="primary" @click="confirmSmartMatches" :disabled="selectedSmartMatchIds.length === 0 || props.matching" :loading="props.matching">
                  Confirm Selected Matches ({{ selectedSmartMatchIds.length }})
                </q-btn>
              </q-col>
            </q-row>
            <q-row v-else class="mt-4">
              <q-col>
                <q-banner type="info"> No smart matches found. Check Remaining Transactions for potential conflicts. </q-banner>
              </q-col>
            </q-row>
          </q-window-item>

          <!-- Remaining Transactions Tab -->
          <q-window-item value="remaining">
            <q-row v-if="remainingImportedTransactions.length > 0" class="mt-4">
              <q-col>
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
                <q-row class="mt-4">
                  <q-col cols="12" md="4">
                    <q-text-field v-model="searchAmount" label="Amount" type="number" variant="outlined" readonly></q-text-field>
                  </q-col>
                  <q-col cols="12" md="4">
                    <q-text-field v-model="searchMerchant" label="Merchant" variant="outlined" @input="searchBudgetTransactions"></q-text-field>
                  </q-col>
                  <q-col cols="12" md="4">
                    <q-text-field
                      v-model="searchDateRange"
                      label="Date Range (days)"
                      type="number"
                      variant="outlined"
                      @input="searchBudgetTransactions"
                    ></q-text-field>
                  </q-col>
                </q-row>

                <!-- Split Transaction Option -->
                <q-row class="mt-4">
                  <q-col>
                    <q-btn color="primary" @click="toggleSplitTransaction" :disabled="props.matching">
                      {{ showSplitForm ? "Cancel Split" : "Split Transaction" }}
                    </q-btn>
                  </q-col>
                </q-row>

                <!-- Split Transaction Form -->
                <q-form v-if="showSplitForm" ref="splitForm" @submit.prevent="saveSplitTransaction" class="mt-4">
                  <q-row v-for="(split, index) in transactionSplits" :key="index" class="align-center">
                    <q-col cols="12" md="3">
                      <q-select
                        v-model="split.entityId"
                        :items="entityOptions"
                        item-title="name"
                        item-value="id"
                        label="Entity"
                        variant="outlined"
                        density="compact"
                        :rules="[(v: string) => !!v || 'Entity is required']"
                      ></q-select>
                    </q-col>
                    <q-col cols="12" md="3">
                      <q-combobox
                        v-model="split.category"
                        :items="props.categoryOptions"
                        label="Category"
                        variant="outlined"
                        density="compact"
                        :rules="[(v: string) => !!v || 'Category is required']"
                      ></q-combobox>
                    </q-col>
                    <q-col cols="12" md="2">
                      <q-text-field
                        v-model.number="split.amount"
                        label="Amount"
                        type="number"
                        variant="outlined"
                        density="compact"
                        :rules="[(v: number) => v > 0 || 'Amount must be greater than 0']"
                      ></q-text-field>
                    </q-col>
                    <q-col cols="12" md="1">
                      <q-btn color="error" icon="close" @click="removeSplit(index)" variant="plain"></q-btn>
                    </q-col>
                  </q-row>
                  <q-banner v-if="remainingSplitAmount !== 0" :type="remainingSplitAmount < 0 ? 'error' : 'warning'" class="mb-4">
                    <div v-if="remainingSplitAmount > 0">Remaining ${{ toDollars(toCents(remainingSplitAmount)) }}</div>
                    <div v-else>Over allocated ${{ toDollars(toCents(Math.abs(remainingSplitAmount))) }}</div>
                  </q-banner>
                  <q-btn color="primary" @click="addSplitTransaction">Add Split</q-btn>
                  <q-btn color="success" type="submit" :disabled="remainingSplitAmount !== 0 || props.matching" :loading="props.matching" class="ml-2">
                    Save Splits
                  </q-btn>
                </q-form>

                <!-- Potential Matches -->
                <q-row v-if="!showSplitForm" class="mb-4">
                  <q-col cols="12" md="4">
                    <q-select
                      v-model="potentialMatchesSortField"
                      :items="potentialMatchesSortFields"
                      label="Sort By"
                      variant="outlined"
                      density="compact"
                      item-title="text"
                      item-value="value"
                      @update:model-value="sortPotentialMatches"
                    ></q-select>
                  </q-col>
                  <q-col cols="12" md="4">
                    <q-btn :color="potentialMatchesSortDirection === 'asc' ? 'primary' : 'secondary'" @click="togglePotentialMatchesSortDirection">
                      {{ potentialMatchesSortDirection === "asc" ? "Ascending" : "Descending" }}
                    </q-btn>
                  </q-col>
                </q-row>

                <q-data-table
                  v-if="potentialMatches.length > 0 && !showSplitForm"
                  :headers="budgetTransactionHeaders"
                  :items="sortedPotentialMatches"
                  :items-per-page="5"
                  v-model="selectedBudgetTransactionForMatch"
                  show-select
                  single-select
                >
                  <template v-slot:item.amount="{ item }"> ${{ toDollars(toCents(item.amount)) }} </template>
                  <template v-slot:item.type="{ item }">
                    {{ item.isIncome ? "Income" : "Expense" }}
                  </template>
                  <template v-slot:item.actions="{ item }">
                    <q-btn
                      color="primary"
                      small
                      @click="matchBankTransaction(item)"
                      :disabled="!selectedBudgetTransactionForMatch.length || props.matching"
                      :loading="props.matching"
                    >
                      Match
                    </q-btn>
                  </template>
                </q-data-table>
                <div v-else-if="!showSplitForm" class="mt-4">
                  <q-banner type="info" class="mb-4"> No potential matches found. Adjust the search criteria or add a new transaction. </q-banner>
                  <q-btn color="primary" @click="addNewTransaction" :disabled="props.matching"> Add New Transaction </q-btn>
                </div>
              </q-col>
            </q-row>
            <q-row v-else class="mt-4">
              <q-col>
                <q-banner type="success"> All bank transactions have been matched or ignored. </q-banner>
              </q-col>
            </q-row>
          </q-window-item>
        </q-window>
      </q-card-section>
      <q-card-actions>
        <q-btn v-if="remainingImportedTransactions.length > 0" color="warning" @click="ignoreBankTransaction" :disabled="props.matching"> Ignore </q-btn>
        <q-btn v-if="remainingImportedTransactions.length > 0" color="secondary" @click="skipBankTransaction" :disabled="props.matching"> Skip </q-btn>
        <q-space></q-space>
        <q-btn color="error" @click="closeDialog(false)" :disabled="props.matching"> Close </q-btn>
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
    <q-snackbar v-model="snackbar" :color="snackbarColor" :timeout="timeout">
      {{ snackbarText }}
      <template v-slot:actions>
        <q-btn variant="text" @click="snackbar = false">Close</q-btn>
      </template>
    </q-snackbar>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted, nextTick } from "vue";
import { Transaction, ImportedTransaction, Budget } from "../types";
import { toDollars, toCents, toBudgetMonth, adjustTransactionDate, todayISO } from "../utils/helpers";
import { dataAccess } from "../dataAccess";
import { useBudgetStore } from "../store/budget";
import { useFamilyStore } from "../store/family";
import { auth } from "../firebase/init";
import TransactionDialog from "./TransactionDialog.vue";
import { QForm } from "quasar";
import { v4 as uuidv4 } from "uuid";

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
  (e: "update:showDialog", value: boolean): void;
  (e: "add-new-transaction", importedTx: ImportedTransaction): void;
  (e: "transactions-updated"): void;
  (e: "update:matching", value: boolean): void;
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
const remainingImportedTransactions = ref<ImportedTransaction[]>(props.remainingImportedTransactions || []);
const selectedBankTransaction = ref<ImportedTransaction | null>(props.selectedBankTransaction || null);

// Snackbar state
const snackbar = ref(false);
const snackbarText = ref("");
const snackbarColor = ref("success");
const timeout = ref(3000);

// Local state for Smart Matches sorting
const activeTab = ref("smart-matches");
const selectedSmartMatchIds = ref<string[]>([]);
const smartMatchesSortField = ref<string>("postedDate");
const smartMatchesSortDirection = ref<"asc" | "desc">("asc");
const smartMatchesSortFields = [
  { text: "Bank Date", value: "postedDate" },
  { text: "Merchant", value: "merchant" },
  { text: "Amount", value: "bankAmount" },
];
const smartMatchDateRange = ref<string>("7");

// Local state for Remaining Transactions
const currentBankTransactionIndex = ref<number>(0);
const searchAmount = ref<string>("");
const searchMerchant = ref<string>("");
const searchDateRange = ref<string>("7");
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

const isMobile = computed(() => window.innerWidth < 960);

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

const smartMatchHeaders = ref([
  { title: "Bank Date", value: "importedTransaction.postedDate" },
  { title: "Bank Amount", value: "bankAmount" },
  { title: "Bank Type", value: "bankType" },
  { title: "Payee", value: "importedTransaction.payee" },
  { title: "Merchant", value: "budgetTransaction.merchant" },
  { title: "Budget Date", value: "budgetTransaction.date" },
  { title: "Budget Amount", value: "budgetAmount" },
  { title: "Budget Type", value: "budgetType" },
  { title: "Actions", value: "actions" },
]);

const budgetTransactionHeaders = ref([
  { title: "Date", value: "date" },
  { title: "Merchant", value: "merchant" },
  { title: "Amount", value: "amount" },
  { title: "Type", value: "type" },
  { title: "Categories", value: "categories" },
  { title: "Actions", value: "actions" },
]);

// Computed properties
const sortedSmartMatches = computed(() => {
  const items = [...smartMatches.value];
  const field = smartMatchesSortField.value;
  const direction = smartMatchesSortDirection.value;

  return items.sort((a, b) => {
    let valueA: number | string = 0;
    let valueB: number | string = 0;

    if (field === "postedDate") {
      valueA = new Date(a.importedTransaction.postedDate).getTime();
      valueB = new Date(b.importedTransaction.postedDate).getTime();
    } else if (field === "merchant") {
      valueA = a.budgetTransaction.merchant.toLowerCase();
      valueB = b.budgetTransaction.merchant.toLowerCase();
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
  () => props.showDialog,
  async (newVal) => {
    if (newVal) {
      await initializeState();
    }
  }
);

watch(
  () => smartMatchDateRange.value,
  () => {
    computeSmartMatchesLocal();
  }
);

async function initializeState() {
  remainingImportedTransactions.value = Array.isArray(props.remainingImportedTransactions) ? [...props.remainingImportedTransactions] : [];
  selectedBankTransaction.value = props.selectedBankTransaction || remainingImportedTransactions.value[0] || null;

  smartMatches.value = [];
  smartMatchDateRange.value = "7";
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
      searchDateRange.value = "7";
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
function closeDialog(value: boolean) {
  emit("update:showDialog", value);
  if (!value) {
    emit("transactions-updated");
    isReady.value = false;
  }
}

async function confirmSmartMatches() {
  if (selectedSmartMatchIds.value.length === 0) {
    showSnackbar("No smart matches selected to confirm", "error");
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

      const reconcileData = {
        budgetId,
        reconciliations: matchesByBudget[budgetId]!,
      } as {
        budgetId: string;
        reconciliations: {
          budgetTransactionId: string;
          importedTransactionId: string;
          match: boolean;
          ignore: boolean;
        }[];
      };

      await dataAccess.batchReconcileTransactions(budgetId, budget, reconcileData);
      const updatedBudget = await dataAccess.getBudget(budgetId);
      if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
    }

    showSnackbar(`${matchesToConfirm.length} smart matches confirmed successfully`);
    emit("transactions-updated");
    computeSmartMatchesLocal(matchesToConfirm);
    updateRemainingTransactions();
  } catch (error: any) {
    console.error("Error confirming smart matches:", error);
    showSnackbar(`Error confirming smart matches: ${error.message}`, "error");
  } finally {
    emit("update:matching", false);
  }
}

async function matchBankTransaction(budgetTransaction: Transaction) {
  if (!selectedBankTransaction.value || !budgetTransaction) {
    showSnackbar("Please select a budget transaction to match", "error");
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

    const targetBudgetIdToUse = (budgetTransaction as any).budgetId || `${user.uid}_${updatedTransaction.entityId}_${updatedTransaction.budgetMonth}`;
    let budget = budgetStore.getBudget(targetBudgetIdToUse);
    if (!budget) {
      const fam = await familyStore.getFamily();
      budget = await createBudgetForMonth(
        updatedTransaction.budgetMonth!,
        fam?.id ?? "",
        user.uid,
        updatedTransaction.entityId
      );
    }

    await dataAccess.saveTransaction(budget, updatedTransaction, false);

    const parts = importedTx.id.split("-");
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join("-");
    await dataAccess.updateImportedTransaction(docId, importedTx.id, true);

    showSnackbar("Transaction matched successfully");
    emit("transactions-updated");
    updateRemainingTransactions();
    skipBankTransaction();
  } catch (error: any) {
    console.log(error);
    showSnackbar(`Error matching transaction: ${error.message}`, "error");
  } finally {
    emit("update:matching", false);
  }
}

async function ignoreBankTransaction() {
  if (!selectedBankTransaction.value) {
    showSnackbar("No bank transaction selected to ignore", "error");
    return;
  }

  emit("update:matching", true);
  try {
    const importedTx = selectedBankTransaction.value;
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

    const parts = importedTx.id.split("-");
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join("-");
    await dataAccess.updateImportedTransaction(docId, importedTx.id, undefined, true);

    showSnackbar("Bank transaction ignored");
    updateRemainingTransactions();
    skipBankTransaction();
  } catch (error: any) {
    showSnackbar(`Error ignoring transaction: ${error.message}`, "error");
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
      closeDialog(false);
      showSnackbar("All bank transactions have been processed", "success");
    } else {
      currentBankTransactionIndex.value = -1;
      selectedBankTransaction.value = null;
    }
  }
}

async function saveSplitTransaction() {
  if (!selectedBankTransaction.value || remainingSplitAmount.value !== 0) {
    showSnackbar("Invalid split amounts or no bank transaction selected", "error");
    return;
  }

  if (!splitForm.value) return;

  const { valid } = await splitForm.value.validate();
  if (!valid) {
    showSnackbar("Please fill in all required fields", "error");
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
      const transaction: Transaction = {
        id: uuidv4(),
        budgetMonth,
        date: importedTx.postedDate || todayISO(),
        merchant: importedTx.payee || "",
        categories: [{ category: split.category, amount: split.amount }],
        amount: split.amount,
        notes: "",
        recurring: false,
        recurringInterval: "Monthly",
        userId: props.userId,
        isIncome: !!importedTx.creditAmount,
        postedDate: importedTx.postedDate,
        importedMerchant: importedTx.payee,
        accountSource: importedTx.accountSource,
        accountNumber: importedTx.accountNumber,
        checkNumber: importedTx.checkNumber,
      status: "C",
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
        const [, entityId, budgetMonth] = budgetId.split("_");
        budget = await createBudgetForMonth(budgetMonth!, family.id, user.uid, entityId);
      }

      await dataAccess.batchSaveTransactions(budgetId, budget, transactionsByBudget[budgetId]!);
      const updatedBudget = await dataAccess.getBudget(budgetId);
      if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
    }

    const parts = importedTx.id.split("-");
    const txId = parts[parts.length - 1];
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
  } catch (error: any) {
    showSnackbar(`Error saving split transaction: ${error.message}`, "error");
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
      budget = await createBudgetForMonth(savedTransaction.budgetMonth!, family.id, user.uid, savedTransaction.entityId);
    }

    budgetStore.updateBudget(targetBudgetId, {
      ...budget,
      transactions: [...budget.transactions, savedTransaction],
    });

    const parts = importedTx.id.split("-");
    const txId = parts[parts.length - 1];
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
  } catch (error: any) {
    showSnackbar(`Error adding transaction: ${error.message}`, "error");
  } finally {
    emit("update:matching", false);
    showTransactionDialog.value = false;
  }
}

async function addNewTransaction() {
  if (!selectedBankTransaction.value) {
    showSnackbar("No bank transaction selected to add", "error");
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar("Please select an entity before adding a transaction", "error");
    return;
  }

  const postedDate = selectedBankTransaction.value.postedDate || todayISO();
  let budgetMonth = toBudgetMonth(postedDate);

  if (!budgetStore.availableBudgetMonths.includes(budgetMonth)) {
    budgetMonth = budgetStore.availableBudgetMonths[budgetStore.availableBudgetMonths.length - 1] || budgetMonth;
    console.log(`Budget month ${toBudgetMonth(postedDate)} not found, falling back to ${budgetMonth}`);
  }

  newTransaction.value = {
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
    recurringInterval: "Monthly",
    userId: props.userId,
    isIncome: !!selectedBankTransaction.value.creditAmount,
    postedDate: selectedBankTransaction.value.postedDate,
    importedMerchant: selectedBankTransaction.value.payee,
    accountSource: selectedBankTransaction.value.accountSource,
    accountNumber: selectedBankTransaction.value.accountNumber,
    checkNumber: selectedBankTransaction.value.checkNumber,
    status: "C",
    entityId: familyStore.selectedEntityId,
    taxMetadata: [],
  };
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

    return !tx.deleted && dateMatch && amountMatch && merchantMatch && (!tx.status || tx.status === "U");
  });
}

function computeSmartMatchesLocal(confirmedMatches: typeof smartMatches.value = []) {
  console.log("Computing smart matches...");
  const confirmedIds = new Set(confirmedMatches.map((m) => m.importedTransaction.id));
  const newSmartMatches = smartMatches.value.filter((match) => !confirmedIds.has(match.importedTransaction.id));

  const dateRangeDays = parseInt(smartMatchDateRange.value) || 7;
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
    const normalizedBankDate = new Date(bankDateStr as string);

    const startDate = new Date(normalizedBankDate);
    startDate.setDate(normalizedBankDate.getDate() - dateRangeDays);
    const endDate = new Date(normalizedBankDate);
    endDate.setDate(normalizedBankDate.getDate() + dateRangeDays);

    props.transactions.forEach((tx) => {
      const txDate = new Date(tx.date);
      const txDateStr = txDate.toISOString().split("T")[0];
      const normalizedTxDate = new Date(txDateStr as string);
      const txAmount = tx.amount;
      const typeMatch = tx.isIncome === !!importedTx.creditAmount;
      if (
        normalizedTxDate >= startDate &&
        normalizedTxDate <= endDate &&
        Math.abs(txAmount - bankAmount) < 0.01 &&
        (!tx.status || tx.status === "U") &&
        !tx.deleted &&
        typeMatch
      ) {
        potentialMatches.push({
          importedTx,
          budgetTx: tx,
          budgetId: (tx as any).budgetId || `${props.userId}_${tx.entityId}_${toBudgetMonth(importedTx.postedDate)}`,
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
    matchesByBank[m.importedTx.id]!.push(m);
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
  smartMatchesSortField.value = "postedDate";
  smartMatchesSortDirection.value = "asc";
  potentialMatchesSortField.value = "date";
  potentialMatchesSortDirection.value = "asc";
  if (computeMatches) computeSmartMatchesLocal();
  else console.log("Skipping computeSmartMatchesLocal on initial reset");
}

function showSnackbar(text: string, color = "success") {
  snackbarText.value = text;
  snackbarColor.value = color;
  timeout.value = 3000;
  snackbar.value = true;
}

// Helper function to create a budget if it doesn't exist
async function createBudgetForMonth(month: string, familyId: string, ownerUid: string, entityId: string): Promise<Budget> {
  const budgetId = `${ownerUid}_${entityId}_${month}`; // New ID format: uid_entityId_budgetMonth
  const existingBudget = await dataAccess.getBudget(budgetId);
  if (existingBudget) {
    return existingBudget;
  }

  // Find the most recent previous budget, or the earliest future budget if none exists, for the same entity
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
    return defaultBudget;
  }

  // Copy source budget
  const [newYear, newMonthNum] = month.split("-").map(Number) as [number, number];
  const [sourceYear, sourceMonthNum] =
    sourceBudget.month.split("-").map(Number) as [number, number];
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
    label: "",
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
      if (firstInstance && firstInstance.recurringInterval === "Monthly") {
        const newDate = adjustTransactionDate(firstInstance.date, month, "Monthly");
        recurringTransactions.push({
          ...firstInstance,
          id: uuidv4(),
          date: newDate,
          budgetMonth: month,
          entityId: entityId,
          taxMetadata: firstInstance.taxMetadata || [],
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
