<!-- CategoryTransactions.vue -->
<template>
  <v-container fluid class="category-transactions text-black">
    <!-- Loading Overlay -->
    <v-overlay :model-value="loading" class="align-center justify-center" scrim="#00000080">
      <v-progress-circular indeterminate color="primary" size="50" />
    </v-overlay>

    <!-- Header -->
    <v-row class="header">
      <v-col>
        <h2 class="category-title">{{ category.name }}</h2>
      </v-col>
      <v-col cols="auto">
        <v-fab :class="isMobile ? 'mr-2' : 'mr-2 mt-2'" icon variant="plain" :absolute="true" location="top" @click="$emit('close')">
          <v-icon>mdi-close</v-icon>
        </v-fab>
      </v-col>
    </v-row>

    <!-- Progress Bar and Remaining -->
    <v-row class="mt-2">
      <v-col>
        <div class="progress-section">
          <div class="progress-label">
            <span :class="!isIncome && spent > category.target ? 'text-error' : ''">{{ formatCurrency(toDollars(toCents(spent))) }}</span>
            {{ isIncome ? "received" : "spent" }} of
            {{ formatCurrency(toDollars(toCents(category.target))) }}
          </div>
          <v-progress-linear
            v-model="progressPercentage"
            height="10"
            :color="isIncome ? 'green' : 'primary'"
            background-color="#e0e0e0"
            rounded
          ></v-progress-linear>
        </div>
        <div class="remaining-section" :class="{ 'over-budget': (remaining || 0) < 0 && !isIncome }">
          <span class="remaining-label"> {{ category.group == "Income" ? "Left to Receive " : "Available " }}</span>
          <span class="remaining-amount">
            {{ formatCurrency(toDollars(toCents(available))) }}
          </span>
        </div>
      </v-col>
    </v-row>

    <!-- Transactions List -->
    <v-row class="flex-grow-1 mt-4 pl-0 pr-0">
      <v-col class="transaction-list pl-0 pr-0">
        <h3 class="section-title pb-2">Transactions ({{ categoryTransactions.length }})</h3>
        <div class="my-2 bg-white rounded-10 pt-2 pr-3 pl-3 mb-4">
          <v-text-field append-inner-icon="mdi-magnify" density="compact" label="Search" variant="plain" single-line v-model="search"></v-text-field>
        </div>
        <v-list dense class="rounded-10">
          <v-list-item
            v-for="transaction in categoryTransactions"
            :key="transaction.id"
            class="transaction-item"
            density="compact"
            @click="editTransaction(transaction)"
            style="border-bottom: 1px solid rgb(var(--v-theme-light))"
          >
            <v-list-item-action class="d-flex align-center">
              <v-row class="pa-2 align-center" no-gutters>
                <v-col cols="2" class="pt-2 font-weight-bold text-primary" style="min-width: 60px; font-size: 10px">
                  {{ formatDate(transaction.date) }}
                </v-col>
                <v-col class="text-truncate" style="flex: 1; min-width: 0">
                  {{ transaction.merchant }}
                </v-col>
                <v-col cols="auto" class="text-right no-wrap" :class="transaction.isIncome ? 'green--text' : ''" style="min-width: 60px">
                  ${{ Math.abs(getCategoryAmount(transaction)).toFixed(2) }}
                </v-col>
                <v-col cols="auto" class="text-right" style="min-width: 40px">
                  <v-icon small @click.stop="confirmDelete(transaction)" title="Move to Trash" color="error">mdi-trash-can-outline</v-icon>
                </v-col>
              </v-row>
            </v-list-item-action>
          </v-list-item>
          <v-list-item v-if="categoryTransactions.length === 0">
            <v-list-item-title>No transactions for this category.</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-col>
    </v-row>

    <!-- Floating Action Button -->
    <v-fab icon :app="true" color="primary" @click="$emit('add-transaction')" location="bottom right" class="mr-2" :class="isMobile ? 'mb-14' : 'mb-2'">
      <v-icon>mdi-plus</v-icon>
    </v-fab>

    <!-- Edit Transaction Dialog -->
    <v-dialog v-model="showEditDialog" :max-width="!isMobile ? '600px' : ''" :fullscreen="isMobile">
      <v-card density="compact">
        <v-card-title class="bg-primary py-5">
          <v-row>
            <v-col>Edit {{ transactionToEdit?.merchant }} Transaction</v-col>
          </v-row>
        </v-card-title>
        <v-card-text>
          <transaction-form
            v-if="showEditDialog && transactionToEdit"
            :initial-transaction="transactionToEdit"
            :category-options="categoryOptions"
            :budget-id="budgetId"
            :user-id="userId"
            :show-cancel="true"
            @save="onTransactionSaved"
            @cancel="showEditDialog = false"
            @update-transactions="updateTransactions"
          />
        </v-card-text>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation Dialog -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card>
        <v-card-title class="bg-error py-3">
          <span class="text-white">Confirm Deletion</span>
        </v-card-title>
        <v-card-text class="pt-4">
          Are you sure you want to delete the transaction for "{{ transactionToDelete?.merchant }}" on
          {{ transactionToDelete ? formatDate(transactionToDelete.date) : "" }}?
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="flat" @click="executeDelete">Move to Trash</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { dataAccess } from "../dataAccess";
import TransactionForm from "./TransactionForm.vue";
import { BudgetCategory, Transaction, Budget } from "../types";
import { toDollars, toCents, formatCurrency } from "../utils/helpers";
import { useBudgetStore } from "../store/budget";

const props = defineProps<{
  category: BudgetCategory;
  transactions?: Transaction[] | null;
  budgetId: string;
  userId: string;
  categoryOptions: string[];
}>();

const emit = defineEmits<{
  (e: "close"): void;
  (e: "add-transaction"): void;
  (e: "update-transactions", transactions: Transaction[]): void;
}>();

const search = ref("");
const budgetStore = useBudgetStore();
const loading = ref(false);
const showEditDialog = ref(false);
const transactionToEdit = ref<Transaction | null>(null);
const showDeleteDialog = ref(false);
const transactionToDelete = ref<Transaction | null>(null);

const spent = computed(() => {
  let spent = 0;
  if (props.transactions) {
    for (let i = 0; i < props.transactions.length; i++) {
      const tx = props.transactions[i];
      if (tx.deleted) continue; // Skip deleted transactions

      const hasCategory = tx.categories?.some((c) => c.category === props.category.name);
      if (hasCategory) {
        if (isIncome.value) {
          tx.categories?.forEach((c) => {
            spent += c.amount;
          });
        } else {
          if (tx.isIncome) {
            tx.categories?.forEach((c) => {
              if (c.category === props.category.name) spent -= c.amount;
            });
          } else {
            tx.categories?.forEach((c) => {
              if (c.category === props.category.name) spent += c.amount;
            });
          }
        }
      }
    }
  }
  return spent;
});

const carryOverAndTarget = computed(() => {
  return (props.category.isFund ? Number(props.category.carryover) || 0 : 0) + Number(props.category.target) || 0;
});

const remaining = computed(() => {
  if (props.category.group === "Income") return (Number(spent.value) || 0) - (Number(props.category.target) || 0);
  return (Number(props.category.target) || 0) - (Number(spent.value) || 0);
});

const available = computed(() => {
  if (props.category.group === "Income") {
    const avail = (Number(spent.value) || 0) - carryOverAndTarget.value;
    if (avail > 0) return 0;
    return avail;
  }
  return carryOverAndTarget.value - Number(spent.value) || 0;
});

const progressPercentage = computed(() => {
  const targetVal = carryOverAndTarget.value;
  const spentVal = Number(spent.value) || 0;
  const rawPercentage = targetVal > 0 ? (spentVal / targetVal) * 100 : 0;
  return Math.min(Math.max(rawPercentage, 0), 100);
});

const isMobile = computed(() => window.innerWidth < 960);

const isIncome = computed(() => {
  return props.category.group === "Income";
});

const categoryTransactions = computed(() => {
  let temp = props.transactions || [];
  temp = temp
    .filter((t) => !t.deleted) // Exclude deleted transactions
    .filter((t) => (t.categories && Array.isArray(t.categories) ? t.categories.some((split) => split.category === props.category.name) : false))
    .sort((a, b) => {
      const dateA = new Date(a.date);
      const dateB = new Date(b.date);
      if (dateA.getTime() !== dateB.getTime()) {
        return dateB.getTime() - dateA.getTime();
      }
      return a.merchant.localeCompare(b.merchant);
    });
  if (search.value && search.value !== "") {
    temp = temp.filter(
      (t) => t.merchant.toLowerCase().includes(search.value.toLowerCase()) || t.amount.toString().toLowerCase().includes(search.value.toLowerCase())
    );
  }
  return temp;
});

function isLastMonth(transaction: Transaction) {
  return transaction.budgetMonth == budgetStore.availableBudgetMonths[budgetStore.availableBudgetMonths.length - 1];
}

const getCategoryAmount = (transaction: Transaction) => {
  const split = transaction.categories?.find((s) => s.category === props.category.name);
  return split ? split.amount : 0;
};

const formatDate = (dateStr: string): string => {
  const [year, month, day] = dateStr.split("-").map(Number);
  const date = new Date(year, month - 1, day);
  return `
    ${date.toLocaleDateString("en-US", { month: "short" })}
    ${date.toLocaleDateString("en-US", { day: "numeric" })}
  `;
};

function editTransaction(transaction: Transaction) {
  transactionToEdit.value = { ...transaction };
  setTimeout(() => {
    showEditDialog.value = true;
  }, 100);
}

function confirmDelete(transaction: Transaction) {
  transactionToDelete.value = transaction;
  showDeleteDialog.value = true;
}

async function executeDelete() {
  if (!transactionToDelete.value?.id) {
    showDeleteDialog.value = false;
    return;
  }
  loading.value = true;
  try {
    const targetBudget = budgetStore.getBudget(props.budgetId);
    if (targetBudget) {
      await dataAccess.deleteTransaction(targetBudget, transactionToDelete.value.id);
    }
    const updatedTransactions = budgetStore.getBudget(props.budgetId)?.transactions;
    if (updatedTransactions) {
      emit("update-transactions", updatedTransactions);
    }
  } catch (error: any) {
    console.error("Error moving transaction to trash:", error);
  } finally {
    loading.value = false;
    showDeleteDialog.value = false;
    transactionToDelete.value = null;
  }
}

async function onTransactionSaved(savedTransaction: Transaction) {
  showEditDialog.value = false;
  // No need to emit update-transactions here; TransactionForm already emits it
}

function updateTransactions(updatedTransactions: Transaction[]) {
  // Propagate the update-transactions event to DashboardView
  emit("update-transactions", updatedTransactions);
}

onMounted(async () => {
  // Rely on budgetStore instead of fetching directly
  const budget = budgetStore.getBudget(props.budgetId);
  if (!budget) {
    console.error(`Budget ${props.budgetId} not found in store`);
  }
});
</script>

<style scoped>
</style>