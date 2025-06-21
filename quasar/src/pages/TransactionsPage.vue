<!-- src/views/TransactionsView.vue -->
<template>
  <q-page :class="isMobile ? 'ps-0' : ''">
    <h1>Transaction and Registry</h1>


    <!-- Tabs -->
    <q-tabs v-model="tab" color="primary">
      <q-tab value="entries">Budget Entries</q-tab>
      <q-tab value="register">Transaction Register</q-tab>
    </q-tabs>

    <q-window v-model="tab">
      <!-- Budget Entries -->
      <q-window-item value="entries">
        <!-- Add Transaction Button -->
        <q-btn color="primary" variant="plain" class="mb-4 mr-2" @click="showTransactionDialog = true"> Add Transaction </q-btn>
        <q-btn color="primary" variant="plain" class="mb-4 mr-2" @click="openMatchBankTransactionsDialog"> Match Bank Transactions </q-btn>

        <q-card class="mb-4">
          <q-card-section>
            <div class="row mt-2">
              <div class="col col-auto">Filters</div>
              <div class="col">
                <q-checkbox v-model="entriesFilterDuplicates" label="Look for Duplicates" density="compact" hide-details @update:modelValue="applyFilters" />
                <q-checkbox v-model="entriesIncludeDeleted" label="Include Deleted" density="compact" hide-details @update:modelValue="applyFilters" />
              </div>
            </div>
          </q-card-section>
          <q-card-section>
            <div class="row">
              <div class="col col-12 col-md-4">
                <EntitySelector @change="loadBudgets" />
              </div>
              <div class="col col-12 col-md-4">
                <q-text-field append-inner-icon="search" density="compact" label="Search" variant="outlined" single-line v-model="entriesSearch"></q-text-field>
              </div>
              <div class="col col-12 col-md-4">
                <q-select
                  v-model="selectedBudgetIds"
                  :items="budgetOptions"
                  density="compact"
                  label="Select Budgets"
                  item-title="month"
                  item-value="budgetId"
                  variant="outlined"
                  multiple
                  clearable
                  :disabled="budgetOptions.length === 0"
                  @update:modelValue="loadTransactions"
                ></q-select>
              </div>
            </div>
            <div class="row" v-if="budgetOptions.length === 0">
              <div class="col col-12">
                <q-banner type="info" class="mt-4"> No budgets available. Create a budget in the Dashboard to start tracking transactions. </q-banner>
              </div>
            </div>

            <template v-if="!isMobile">
              <div class="row">
                <div class="col col-12 col-md-2">
                  <q-text-field v-model="entriesFilterMerchant" label="Merchant" variant="outlined" density="compact" @input="applyFilters"></q-text-field>
                </div>
                <div class="col col-12 col-md-2">
                  <q-text-field
                    v-model="entriesFilterAmount"
                    label="Amount"
                    type="number"
                    variant="outlined"
                    density="compact"
                    @input="applyFilters"
                  ></q-text-field>
                </div>
                <div class="col col-12 col-md-2">
                  <q-text-field v-model="entriesFilterNote" label="Note/Memo" variant="outlined" density="compact" @input="applyFilters"></q-text-field>
                </div>
                <div class="col col-12 col-md-2">
                  <q-text-field
                    v-model="entriesFilterDate"
                    label="Date"
                    type="date"
                    variant="outlined"
                    density="compact"
                    :clearable="true"
                    @input="applyFilters"
                  ></q-text-field>
                </div>
                <div class="col col-12 col-md-2">
                  <q-text-field v-model="entriesFilterStatus" label="Status" variant="outlined" density="compact" @input="applyFilters"></q-text-field>
                </div>
                <div class="col col-12 col-md-2">
                  <q-select
                    v-model="entriesFilterAccount"
                    :items="availableAccounts"
                    item-title="name"
                    item-value="id"
                    label="Account"
                    variant="outlined"
                    density="compact"
                    clearable
                    @update:modelValue="applyFilters"
                  ></q-select>
                </div>
              </div>
            </template>
            <template v-else>
              <q-expansion-panels>
                <q-expansion-panel title="More Filters">
                  <q-expansion-panel-text>
                    <div class="row">
                      <div class="col col-12 col-md-2">
                        <q-text-field
                          v-model="entriesFilterMerchant"
                          label="Merchant"
                          variant="outlined"
                          density="compact"
                          @input="applyFilters"
                        ></q-text-field>
                      </div>
                      <div class="col col-12 col-md-2">
                        <q-text-field
                          v-model="entriesFilterAmount"
                          label="Amount"
                          type="number"
                          variant="outlined"
                          density="compact"
                          @input="applyFilters"
                        ></q-text-field>
                      </div>
                      <div class="col col-12 col-md-2">
                        <q-text-field v-model="entriesFilterNote" label="Note/Memo" variant="outlined" density="compact" @input="applyFilters"></q-text-field>
                      </div>
                      <div class="col col-12 col-md-2">
                        <q-text-field
                          v-model="entriesFilterDate"
                          label="Date"
                          type="date"
                          variant="outlined"
                          density="compact"
                          :clearable="true"
                          @input="applyFilters"
                        ></q-text-field>
                      </div>
                      <div class="col col-12 col-md-2">
                        <q-text-field v-model="entriesFilterStatus" label="Status" variant="outlined" density="compact" @input="applyFilters"></q-text-field>
                      </div>
                      <div class="col col-12 col-md-2">
                        <q-select
                          v-model="entriesFilterAccount"
                          :items="availableAccounts"
                          item-title="name"
                          item-value="id"
                          label="Account"
                          variant="outlined"
                          density="compact"
                          clearable
                          @update:modelValue="applyFilters"
                        ></q-select>
                      </div>
                    </div>
                  </q-expansion-panel-text>
                </q-expansion-panel>
              </q-expansion-panels>
            </template>
          </q-card-section>
        </q-card>

        <q-card density="compact">
          <q-card-section>Budget Entries</q-card-section>
          <q-list dense>
            <q-item v-for="transaction in expenseTransactions" :key="transaction.id" class="transaction-item" :class="{ 'deleted-transaction': transaction.deleted }" @click="editTransaction(transaction)">
              <!-- Desktop Layout -->
              <template v-if="!isMobile">
                <div class="row pa-2 align-center">
                  <div class="col text-center col-2">
                    {{ formatDateLong(transaction.date) }}
                  </div>
                  <div class="col col-2">
                    <div class="merchant">{{ transaction.merchant }}</div>
                  </div>
                  <div class="col col-2">
                    <div>{{ getEntityName(transaction.entityId || transaction.budgetId) }}</div>
                  </div>
                  <div class="col text-right col-2">
                    <div class="total" :class="transaction.isIncome ? 'text-success' : ''">${{ toDollars(toCents(transaction.amount)) }}</div>
                  </div>
                  <div class="col text-center col-1">
                    <span v-if="transaction.status === 'C'" class="text-success font-weight-bold" title="Cleared"> C </span>
                  </div>
                  <div class="col col-2">
                    <div v-if="transaction.notes" class="text-caption text-grey">Notes: {{ transaction.notes }}</div>
                    <div v-if="transaction.categories.length > 1" class="text-caption text-grey">Split: {{ formatCategories(transaction.categories) }}</div>
                    <div v-if="transaction.status === 'C'" class="text-caption text-grey">
                      Imported: {{ transaction.accountSource || 'N/A' }}
                      {{ getAccountName(transaction.accountNumber) }}
                      {{ transaction.postedDate ? `@ ${transaction.postedDate}` : '' }}
                      {{ transaction.importedMerchant ? ` ${transaction.importedMerchant}` : '' }}
                    </div>
                    <div v-if="transaction.recurring" class="text-caption text-primary">Repeats: {{ transaction.recurringInterval }}</div>
                  </div>
                  <div class="col text-right col-1">
                    <q-icon
                      v-if="transaction.status !== 'C' && !transaction.deleted"
                      color="primary"
                      small
                      @click.stop="selectBudgetTransactionToMatch(transaction)"
                      title="Match Transaction"
                      >link</q-icon
                    >
                    <q-icon
                      v-if="!transaction.deleted"
                      small
                      @click.stop="deleteTransaction(transaction.id)"
                      title="Delete Entry"
                      color="error"
                      class="ml-2"
                    >
                      delete_outline
                    </q-icon>
                    <q-icon
                      v-else
                      small
                      @click.stop="restoreTransaction(transaction.id)"
                      title="Restore Entry"
                      color="primary"
                      class="ml-2"
                    >
                      restore
                    </q-icon>
                  </div>
                </div>
              </template>

              <!-- Mobile Layout -->
              <template v-else>
                <q-card>
                  <q-card-item>
                    <div class="row no-gutters">
                      <div class="col font-weight-bold">
                        {{ transaction.merchant }}
                      </div>
                      <div class="col text-right">
                        <div :class="transaction.isIncome ? 'text-success' : ''">
                          {{ formatCurrency(toDollars(toCents(transaction.amount))) }}
                          <span v-if="transaction.status === 'C'" class="text-success font-weight-bold" title="Cleared">
                            {{ transaction.status }}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div class="row text-caption no-gutters">
                      <div class="col">
                        {{ formatDateLong(transaction.date) }}
                      </div>
                      <div class="col text-right">
                        <q-btn v-if="transaction.status !== 'C' && !transaction.deleted" icon small @click.stop="selectBudgetTransactionToMatch(transaction)" title="Match Transaction">
                          <q-icon color="primary">link</q-icon>
                        </q-btn>
                        <q-icon
                          v-if="!transaction.deleted"
                          small
                          @click.stop="deleteTransaction(transaction.id)"
                          title="Delete Entry"
                          color="error"
                        >
                          delete_outline
                        </q-icon>
                        <q-icon
                          v-else
                          small
                          @click.stop="restoreTransaction(transaction.id)"
                          title="Restore Entry"
                          color="primary"
                        >
                          restore
                        </q-icon>
                      </div>
                    </div>
                    <div class="row text-caption text-grey no-gutters" v-if="transaction.notes">
                      <div class="col col-12">Notes: {{ transaction.notes }}</div>
                    </div>
                    <div class="row text-caption text-grey no-gutters" v-if="transaction.categories.length > 1">
                      <div class="col col-12">Split: {{ formatCategories(transaction.categories) }}</div>
                    </div>
                    <div class="row text-caption text-grey no-gutters" v-if="transaction.status === 'C'">
                      <div class="col col-12">
                        Imported: {{ transaction.accountSource || 'N/A' }}
                        {{ getAccountName(transaction.accountNumber) }}
                        {{ transaction.postedDate ? `@ ${transaction.postedDate}` : '' }}
                      </div>
                    </div>
                    <div class="row text-primary text-caption no-gutters" v-if="transaction.recurring">
                      <div class="col col-12">Repeats: {{ transaction.recurringInterval }}</div>
                    </div>
                  </q-card-item>
                </q-card>
              </template>
            </q-item>
            <q-item v-if="expenseTransactions.length === 0">
              <q-item-label>No transactions found.</q-item-label>
            </q-item>
          </q-list>
        </q-card>
      </q-window-item>

      <!-- Transaction Register -->
      <q-window-item value="register">
        <TransactionRegistry></TransactionRegistry>
      </q-window-item>
    </q-window>

    <!-- Transaction Dialog Component -->
    <TransactionDialog
      v-if="showTransactionDialog"
      :show-dialog="showTransactionDialog"
      :initial-transaction="newTransaction"
      :edit-mode="editMode"
      :loading="loading"
      :category-options="categoryOptions"
      :budget-id="targetBudgetId"
      :user-id="userId"
      @update:showDialog="showTransactionDialog = $event"
      @save="saveTransaction"
      @cancel="cancelDialog"
      @update-transactions="updateTransactions"
    />

    <!-- Match Bank Transactions Dialog Component -->
    <MatchBankTransactionsDialog
      v-if="showMatchBankTransactionsDialog"
      :show-dialog="showMatchBankTransactionsDialog"
      :remaining-imported-transactions="remainingImportedTransactions"
      :selected-bank-transaction="selectedBankTransaction"
      :transactions="transactions"
      :budget-id="selectedBudgetIds.length > 0 ? selectedBudgetIds[0] : ''"
      :matching="matching"
      :category-options="categoryOptions"
      :user-id="userId"
      @update:matching="matching = $event"
      @update:showDialog="showMatchBankTransactionsDialog = $event"
      @transactions-updated="loadTransactions"
    />

    <!-- Match Budget Transaction Dialog Component -->
    <MatchBudgetTransactionDialog
      v-if="showMatchBudgetTransactionDialog"
      :show-dialog="showMatchBudgetTransactionDialog"
      :selected-budget-transaction="selectedBudgetTransaction"
      :unmatched-imported-transactions="unmatchedImportedTransactions"
      :available-accounts="availableAccounts"
      @update:showDialog="showMatchBudgetTransactionDialog = $event"
      @match-transaction="matchTransaction"
    />

  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch } from 'vue';
import { useQuasar } from 'quasar';
import { storeToRefs } from 'pinia';
import { auth } from '../firebase/init';
import { dataAccess } from '../dataAccess';
import TransactionDialog from '../components/TransactionDialog.vue';
import MatchBankTransactionsDialog from '../components/MatchBankTransactionsDialog.vue';
import MatchBudgetTransactionDialog from '../components/MatchBudgetTransactionDialog.vue';
import TransactionRegistry from '../components/TransactionRegistry.vue';
import EntitySelector from '../components/EntitySelector.vue';
import { Transaction, BudgetInfo, ImportedTransaction, Account, Entity } from '../types';
import { formatDateLong, toDollars, toCents, formatCurrency, toBudgetMonth, todayISO } from '../utils/helpers';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { useUIStore } from '../store/ui';
import { v4 as uuidv4 } from 'uuid';
const $q = useQuasar();

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();
const uiStore = useUIStore();
const {
  entriesSearch,
  entriesFilterMerchant,
  entriesFilterAmount,
  entriesFilterNote,
  entriesFilterStatus,
  entriesFilterDate,
  entriesFilterAccount,
  entriesFilterDuplicates,
  entriesIncludeDeleted,
  selectedBudgetIds,
} = storeToRefs(uiStore);

const tab = ref('entries');

const transactions = ref<Transaction[]>([]);
const newTransaction = ref<Transaction>({
  id: uuidv4(),
  date: todayISO(),
  merchant: '',
  categories: [{ category: '', amount: 0 }],
  amount: 0,
  notes: '',
  recurring: false,
  recurringInterval: 'Monthly',
  userId: '',
  isIncome: false,
  entityId: familyStore.selectedEntityId, // Initialize with selected entity
  taxMetadata: [],
});
const availableAccounts = ref<Account[]>([]);
const categoryOptions = ref<string[]>(['Income']);
const loading = ref(false);
const editMode = ref(false);
const showTransactionDialog = ref(false);
const showMatchBudgetTransactionDialog = ref(false);
const showMatchBankTransactionsDialog = ref(false);
const selectedBudgetTransaction = ref<Transaction | null>(null);
const importedTransactions = ref<ImportedTransaction[]>([]);
const currentBankTransactionIndex = ref<number>(0);
const smartMatches = ref<
  Array<{
    importedTransaction: ImportedTransaction;
    budgetTransaction: Transaction;
    budgetId: string;
    bankAmount: number;
    bankType: string;
  }>
>([]);
const selectedBankTransaction = ref<ImportedTransaction | null>(null);
const matching = ref(false);
const remainingImportedTransactions = ref<ImportedTransaction[]>([]);
const pendingImportedTx = ref<ImportedTransaction | null>(null);
const targetBudgetId = ref<string>('');
const isMobile = computed(() => $q.screen.lt.md);

const userId = computed(() => auth.currentUser?.uid || '');

const entityOptions = computed(() => {
  const options = (familyStore.family?.entities || []).map((entity) => ({
    id: entity.id,
    name: entity.name,
  }));
  return [{ id: '', name: 'All Entities' }, ...options];
});

const potentialDuplicateIds = computed(() => {
  const ids = new Set<string>();
  const txs = transactions.value;
  for (let i = 0; i < txs.length; i++) {
    const a = txs[i];
    const merchantA = (a.importedMerchant || a.merchant).toLowerCase();
    const dateA = new Date(a.date);
    for (let j = i + 1; j < txs.length; j++) {
      const b = txs[j];
      if (a.amount !== b.amount) continue;
      if (a.isIncome !== b.isIncome) continue;
      const merchantB = (b.importedMerchant || b.merchant).toLowerCase();
      if (merchantA.includes(merchantB) || merchantB.includes(merchantA)) {
        const dateB = new Date(b.date);
        const diffDays = Math.abs(dateA.getTime() - dateB.getTime()) / (1000 * 60 * 60 * 24);
        if (diffDays <= 3) {
          ids.add(a.id);
          ids.add(b.id);
        }
      }
    }
  }
  return ids;
});

const expenseTransactions = computed(() => {
  let temp = transactions.value;
  if (!entriesIncludeDeleted.value) {
    temp = temp.filter((t) => !t.deleted);
  }

  temp.sort((a, b) => {
    const dateA = new Date(a.date);
    const dateB = new Date(b.date);
    return dateB.getTime() - dateA.getTime();
  });

  if (entriesFilterMerchant.value) {
    temp = temp.filter(
      (t) =>
        t.merchant.toLowerCase().includes(entriesFilterMerchant.value.toLowerCase()) ||
        (t.importedMerchant && t.importedMerchant.toLowerCase().includes(entriesFilterMerchant.value.toLowerCase())),
    );
  }
  if (entriesFilterAmount.value) {
    const amount = parseFloat(entriesFilterAmount.value);
    temp = temp.filter((t) => t.amount.toString().includes(entriesFilterAmount.value.toString()));
  }
  if (entriesFilterNote.value) {
    temp = temp.filter((t) => t.notes && t.notes.toLowerCase().includes(entriesFilterNote.value.toLowerCase()));
  }
  if (entriesFilterStatus.value) {
    temp = temp.filter((t) => t.status && t.status.toLowerCase().includes(entriesFilterStatus.value.toLowerCase()));
  }
  if (entriesFilterDate.value) {
    temp = temp.filter((t) => t.date === entriesFilterDate.value);
  }
  if (entriesFilterAccount.value) {
    temp = temp.filter((t) => t.accountNumber && getAccountId(t.accountNumber) === entriesFilterAccount.value);
  }

  if (entriesSearch.value && entriesSearch.value !== '') {
    const searchLower = entriesSearch.value.toLowerCase();
    temp = temp.filter((t) => {
      if (t.merchant.toLowerCase().includes(searchLower)) return true;
      if (t.amount.toString().toLowerCase().includes(searchLower)) return true;
      if (t.categories && t.categories.some((c) => c.category.toLowerCase().includes(searchLower))) {
        return true;
      }
      const budget = budgetStore.getBudget(t.budgetId || '');
      if (budget) {
        for (const cat of t.categories || []) {
          const matchCat = budget.categories.find((bc) => bc.name === cat.category);
          if (matchCat && matchCat.group && matchCat.group.toLowerCase().includes(searchLower)) {
            return true;
          }
        }
      }
      return false;
    });
  }

  if (entriesFilterDuplicates.value) {
    const dupes = potentialDuplicateIds.value;
    temp = temp.filter((t) => dupes.has(t.id));
  }

  return temp;
});

const unmatchedImportedTransactions = computed(() => {
  return importedTransactions.value.filter((tx) => !tx.matched && !tx.ignored);
});

const budgetOptions = computed(() => {
  return Array.from(budgetStore.budgets.entries())
    .map(([budgetId, budget]) => ({
      budgetId,
      month: budget.month,
      familyId: budget.familyId,
    }))
    .sort((a, b) => b.month.localeCompare(a.month));
});

onMounted(async () => {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar('Please log in to view transactions', 'error');
    return;
  }

  $q.loading.show({
    message: 'Loading data...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    await familyStore.loadFamily(user.uid);
    await loadBudgets();
    if (budgetOptions.value.length > 0) {
      if (!selectedBudgetIds.value || selectedBudgetIds.value.length == 0) selectedBudgetIds.value = [budgetOptions.value[0].budgetId];
      targetBudgetId.value = selectedBudgetIds.value[0];
      await loadTransactions();
    } else {
      showSnackbar('No budgets available. Please create one in the Dashboard.', 'warning');
    }

    importedTransactions.value = await dataAccess.getImportedTransactions();
    const family = await familyStore.getFamily();
    if (family) {
      availableAccounts.value = await dataAccess.getAccounts(family.id);
      availableAccounts.value = availableAccounts.value.filter((account) => account.type === 'Bank' || account.type === 'CreditCard');
    }
  } catch (error: any) {
    showSnackbar(`Error loading data: ${error.message}`, 'error');
  } finally {
    $q.loading.hide();
  }
});

onUnmounted(() => {
  budgetStore.unsubscribeAll();
});

watch(showMatchBankTransactionsDialog, (newVal) => {
  if (!newVal) {
    smartMatches.value = [];
    remainingImportedTransactions.value = [];
    currentBankTransactionIndex.value = 0;
    selectedBankTransaction.value = null;
    matching.value = false;
  }
});

async function loadBudgets() {
  const user = auth.currentUser;
  if (!user) return;

  $q.loading.show({
    message: 'Loading budgets...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);
  } catch (error: any) {
    showSnackbar(`Error loading budgets: ${error.message}`, 'error');
  } finally {
    $q.loading.hide();
  }
}

async function loadTransactions() {
  if (selectedBudgetIds.value.length === 0) {
    transactions.value = [];
    categoryOptions.value = ['Income'];
    return;
  }

  $q.loading.show({
    message: 'Loading transactions...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  const allTransactions: Transaction[] = [];
  const allCategories = new Set<string>(['Income']);

  try {
    for (const budgetId of selectedBudgetIds.value) {
      const budget = budgetStore.getBudget(budgetId) || (await dataAccess.getBudget(budgetId));
      if (budget) {
        budgetStore.updateBudget(budgetId, budget);
        const budgetTransactions = (budget.transactions || [])
          .map((tx) => ({
            ...tx,
            budgetId,
            entityId: tx.entityId || budget.entityId, // Use transaction entityId or fall back to budget
          }));
        allTransactions.push(...budgetTransactions);
        budget.categories.forEach((cat) => allCategories.add(cat.name));
      }
    }

    transactions.value = allTransactions;
    categoryOptions.value = Array.from(allCategories).sort((a, b) => b.localeCompare(a));
  } catch (error: any) {
    showSnackbar(`Error loading transactions: ${error.message}`, 'error');
  } finally {
    $q.loading.hide();
  }
}

async function isLastMonth(transaction: Transaction) {
  return transaction.budgetMonth == budgetOptions.value[0].month;
}

async function saveTransaction(transaction: Transaction) {
  $q.loading.show({
    message: 'Saving transaction...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    let targetBudgetIdToUse = targetBudgetId.value;

    if (editMode.value && transaction.id) {
      targetBudgetIdToUse = transaction.budgetId || selectedBudgetIds.value[0];
    }

    transaction.entityId = familyStore.selectedEntityId || transaction.entityId; // Ensure entityId is set
    showSnackbar(editMode.value ? 'Transaction updated successfully' : 'Transaction added successfully');
    resetForm();
    showTransactionDialog.value = false;
    await loadTransactions();
  } catch (error: any) {
    showSnackbar(`Error: ${error.message}`, 'error');
  } finally {
    $q.loading.hide();
  }
}

function editTransaction(item: Transaction) {
  newTransaction.value = { ...item, categories: [...item.categories] };
  editMode.value = true;
  targetBudgetId.value = item.budgetId || selectedBudgetIds.value[0];
  familyStore.selectEntity(item.entityId || ''); // Set entity for editing
  showTransactionDialog.value = true;
}

async function deleteTransaction(id: string) {
  if (selectedBudgetIds.value.length === 0) {
    showSnackbar('Please select at least one budget to delete transactions', 'error');
    return;
  }

  $q.loading.show({
    message: 'Deleting transaction...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    const targetTransaction = transactions.value.find((tx) => tx.id === id);

    if (!targetTransaction) {
      showSnackbar('Transaction not found in selected budgets', 'error');
      return;
    }

    // when loading transactions, we add budgetId
    const targetBudgetIdToUse = targetTransaction.budgetId;
    const originalId = targetTransaction.originalId ?? targetTransaction.id;

    if (!targetBudgetIdToUse || !originalId) {
      showSnackbar('Transaction not found in selected budgets', 'error');
      return;
    }

    const budget = budgetStore.getBudget(targetBudgetIdToUse);
    if (!budget) throw new Error('Selected budget not found');

    await dataAccess.deleteTransaction(budget, originalId, await !isLastMonth(targetTransaction));
    showSnackbar('Transaction deleted successfully');
    await loadTransactions();
  } catch (error: any) {
    showSnackbar(`Error: ${error.message}`, 'error');
  } finally {
    $q.loading.hide();
  }
}

async function restoreTransaction(id: string) {
  if (selectedBudgetIds.value.length === 0) {
    showSnackbar('Please select at least one budget to restore transactions', 'error');
    return;
  }

  $q.loading.show({
    message: 'Restoring transaction...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    const targetTransaction = transactions.value.find((tx) => tx.id === id);

    if (!targetTransaction) {
      showSnackbar('Transaction not found in selected budgets', 'error');
      return;
    }

    const targetBudgetIdToUse = targetTransaction.budgetId;
    const originalId = targetTransaction.originalId ?? targetTransaction.id;

    if (!targetBudgetIdToUse || !originalId) {
      showSnackbar('Transaction not found in selected budgets', 'error');
      return;
    }

    const budget = budgetStore.getBudget(targetBudgetIdToUse);
    if (!budget) throw new Error('Selected budget not found');

    await dataAccess.restoreTransaction(
      budget,
      originalId,
      await !isLastMonth(targetTransaction)
    );
    showSnackbar('Transaction restored successfully');
    await loadTransactions();
  } catch (error: any) {
    showSnackbar(`Error: ${error.message}`, 'error');
  } finally {
    $q.loading.hide();
  }
}

function selectBudgetTransactionToMatch(transaction: Transaction) {
  selectedBudgetTransaction.value = transaction;
  showMatchBudgetTransactionDialog.value = true;
}

async function matchTransaction(importedTx: ImportedTransaction) {
  if (!selectedBudgetTransaction.value) {
    showSnackbar('No budget transaction selected to match', 'error');
    return;
  }

  $q.loading.show({
    message: 'Matching transaction...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    const budgetTx = selectedBudgetTransaction.value;

    const updatedTransaction: Transaction = {
      ...budgetTx,
      accountSource: importedTx.accountSource || '',
      accountNumber: importedTx.accountNumber || '',
      postedDate: importedTx.postedDate || '',
      checkNumber: importedTx.checkNumber || '',
      importedMerchant: importedTx.payee || '',
      status: 'C',
      id: budgetTx.originalId || budgetTx.id,
      userId: budgetTx.userId || userId.value,
      budgetMonth: budgetTx.budgetMonth || '',
      date: budgetTx.date || '',
      merchant: budgetTx.merchant || '',
      categories: budgetTx.categories || [],
      amount: budgetTx.amount || 0,
      notes: budgetTx.notes || '',
      recurring: budgetTx.recurring || false,
      recurringInterval: budgetTx.recurringInterval || 'Monthly',
      isIncome: budgetTx.isIncome || false,
      entityId: budgetTx.entityId, // Preserve entityId
    };

    let targetBudgetIdToUse = budgetTx.budgetId;
    if (!targetBudgetIdToUse) {
      for (const budgetId of selectedBudgetIds.value) {
        const budget = budgetStore.getBudget(budgetId);
        if (budget && budget.transactions?.some((tx) => tx.id === budgetTx.originalId)) {
          targetBudgetIdToUse = budgetId;
          break;
        }
      }
    }

    if (!targetBudgetIdToUse) {
      showSnackbar('Transaction not found in selected budgets', 'error');
      return;
    }

    const budget = budgetStore.getBudget(targetBudgetIdToUse);
    if (!budget) throw new Error('Selected budget not found');

    await dataAccess.saveTransaction(budget, updatedTransaction, await !isLastMonth(updatedTransaction));

    const parts = importedTx.id.split('-');
    const txId = parts[parts.length - 1];
    const docId = parts.slice(0, -1).join('-');
    await dataAccess.updateImportedTransaction(docId, { ...importedTx, matched: true });

    const txIndex = importedTransactions.value.findIndex((tx) => tx.id === importedTx.id);
    if (txIndex !== -1) {
      importedTransactions.value[txIndex].matched = true;
    }

    showSnackbar('Transaction matched successfully');
    showMatchBudgetTransactionDialog.value = false;
    selectedBudgetTransaction.value = null;
    await loadTransactions();
  } catch (error: any) {
    console.log(error);
    showSnackbar(`Error matching transaction: ${error.message}`, 'error');
  } finally {
    $q.loading.hide();
  }
}

function openMatchBankTransactionsDialog() {
  if (unmatchedImportedTransactions.value.length === 0) {
    showSnackbar('No unmatched bank transactions to process', 'info');
    return;
  }

  remainingImportedTransactions.value = unmatchedImportedTransactions.value.filter(
    (importedTx) => !smartMatches.value.some((match) => match.importedTransaction.id === importedTx.id),
  );

  if (remainingImportedTransactions.value.length > 0) {
    currentBankTransactionIndex.value = 0;
    selectedBankTransaction.value = remainingImportedTransactions.value[0];
  } else {
    currentBankTransactionIndex.value = -1;
    selectedBankTransaction.value = null;
  }

  showMatchBankTransactionsDialog.value = true;
}

function cancelDialog() {
  showTransactionDialog.value = false;
  pendingImportedTx.value = null;
  resetForm();
}

function resetForm() {
  newTransaction.value = {
    id: uuidv4(),
    date: todayISO(),
    merchant: '',
    categories: [{ category: '', amount: 0 }],
    amount: 0,
    notes: '',
    recurring: false,
    recurringInterval: 'Monthly',
    userId: '',
    isIncome: false,
    entityId: familyStore.selectedEntityId, // Set default entityId
  };
  editMode.value = false;
  targetBudgetId.value = selectedBudgetIds.value.length > 0 ? selectedBudgetIds.value[0] : '';
}

function getAccountId(accountNumber: string): string {
  const account = availableAccounts.value.find((a) => a.accountNumber === accountNumber);
  return account ? account.id : '';
}

function getAccountName(accountNumber: string): string {
  const account = availableAccounts.value.find((a) => a.accountNumber === accountNumber);
  return account ? account.name : 'Unknown Account';
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

function formatCategories(categories: { category: string; amount: number }[] | undefined | null) {
  if (!categories || !Array.isArray(categories)) {
    return 'No categories';
  }
  if (categories.length === 1) {
    return categories[0].category;
  }
  return categories.map((c) => `${c.category} (${formatCurrency(toDollars(toCents(c.amount)))})`).join(', ');
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

function updateTransactions(newTransactions: Transaction[]) {
  loadTransactions();
}

function applyFilters() {
  // Trigger recomputation of expenseTransactions
}
</script>

<style scoped>
.transaction-item {
  border-bottom: 1px solid #e0e0e0;
}
.deleted-transaction {
  background-color: #f5f5f5;
}
.entity-selector {
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  font-size: 1.5rem;
  font-weight: bold;
  color: rgb(var(--v-theme-primary));
}
</style>
