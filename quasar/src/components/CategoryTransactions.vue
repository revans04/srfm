<!-- CategoryTransactions.vue -->
<template>
  <q-page fluid class="category-transactions text-black">
    <!-- Loading Overlay -->
    <q-overlay :model-value="loading" class="align-center justify-center" scrim="#00000080">
      <q-progress-circular indeterminate color="primary" size="50" />
    </q-overlay>

    <!-- Header -->
    <q-row class="header">
      <q-col>
        <h2 class="category-title">{{ category.name }}</h2>
      </q-col>
      <q-col cols="auto">
        <q-fab
          :class="isMobile ? 'mr-2' : 'mr-2 mt-2'"
          icon="mdi-close"
          variant="plain"
          :absolute="true"
          location="top"
          @click="$emit('close')"
        />
      </q-col>
    </q-row>

    <!-- Progress Bar and Remaining -->
    <q-row class="mt-2">
      <q-col>
        <div class="progress-section">
          <div class="progress-label">
            <span :class="!isIncome && spent > category.target ? 'text-error' : ''">{{ formatCurrency(toDollars(toCents(spent))) }}</span>
            {{ isIncome ? "received" : "spent" }} of
            {{ formatCurrency(toDollars(toCents(category.target))) }}
          </div>
          <q-progress-linear
            v-model="progressPercentage"
            height="10"
            :color="isIncome ? 'green' : 'primary'"
            background-color="#e0e0e0"
            rounded
          ></q-progress-linear>
        </div>
        <div class="remaining-section" :class="{ 'over-budget': (remaining || 0) < 0 && !isIncome }">
          <span class="remaining-label"> {{ category.group == "Income" ? "Left to Receive " : "Available " }}</span>
          <span class="remaining-amount">
            {{ formatCurrency(toDollars(toCents(available))) }}
          </span>
        </div>
      </q-col>
    </q-row>

    <!-- Transactions List -->
    <q-row class="flex-grow-1 mt-4 pl-0 pr-0">
      <q-col class="transaction-list pl-0 pr-0">
        <h3 class="section-title pb-2">Transactions ({{ categoryTransactions.length }})</h3>
        <div class="my-2 bg-white rounded-10 pt-2 pr-3 pl-3 mb-4">
          <q-text-field append-inner-icon="mdi-magnify" density="compact" label="Search" variant="plain" single-line v-model="search"></q-text-field>
        </div>
        <q-list dense class="rounded-10">
          <q-item
            v-for="transaction in categoryTransactions"
            :key="transaction.id"
            class="transaction-item"
            density="compact"
            @click="editTransaction(transaction)"
            style="border-bottom: 1px solid rgb(var(--v-theme-light))"
          >
            <q-item-section class="d-flex align-center">
              <q-row class="pa-2 align-center" no-gutters>
                <q-col cols="2" class="pt-2 font-weight-bold text-primary" style="min-width: 60px; font-size: 10px">
                  {{ formatDate(transaction.date) }}
                </q-col>
                <q-col class="text-truncate" style="flex: 1; min-width: 0">
                  {{ transaction.merchant }}
                </q-col>
                <q-col cols="auto" class="text-right no-wrap" :class="transaction.isIncome ? 'green--text' : ''" style="min-width: 60px">
                  ${{ Math.abs(getCategoryAmount(transaction)).toFixed(2) }}
                </q-col>
                <q-col cols="auto" class="text-right" style="min-width: 40px">
                  <q-icon small @click.stop="confirmDelete(transaction)" title="Move to Trash" color="error">mdi-trash-can-outline</q-icon>
                </q-col>
              </q-row>
            </q-item-section>
          </q-item>
          <q-item v-if="categoryTransactions.length === 0">
            <q-item-label>No transactions for this category.</q-item-label>
          </q-item>
        </q-list>
      </q-col>
    </q-row>

    <!-- Floating Action Button -->
    <q-fab
      icon="mdi-plus"
      :app="true"
      color="primary"
      @click="$emit('add-transaction')"
      location="bottom right"
      class="mr-2"
      :class="isMobile ? 'mb-14' : 'mb-2'"
    />

    <!-- Edit Transaction Dialog -->
    <q-dialog v-model="showEditDialog" :max-width="!isMobile ? '600px' : ''" :fullscreen="isMobile">
      <q-card density="compact">
        <q-card-section class="bg-primary py-5">
          <q-row>
            <q-col>Edit {{ transactionToEdit?.merchant }} Transaction</q-col>
          </q-row>
        </q-card-section>
        <q-card-section>
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
        </q-card-section>
      </q-card>
    </q-dialog>

    <!-- Delete Confirmation Dialog -->
    <q-dialog v-model="showDeleteDialog" max-width="400">
      <q-card>
        <q-card-section class="bg-error py-3">
          <span class="text-white">Confirm Deletion</span>
        </q-card-section>
        <q-card-section class="pt-4">
          Are you sure you want to delete the transaction for "{{ transactionToDelete?.merchant }}" on
          {{ transactionToDelete ? formatDate(transactionToDelete.date) : "" }}?
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteDialog = false">Cancel</q-btn>
          <q-btn color="error" variant="flat" @click="executeDelete">Move to Trash</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
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
  let spentTotal = 0;
  if (props.transactions) {
    for (const transaction of props.transactions) {
      if (transaction.deleted) continue; // Skip deleted transactions
      const hasCategory = transaction.categories.some(
        (c) => c.category === props.category.name
      );
      if (hasCategory) {
        if (isIncome.value) {
          transaction.categories.forEach((c) => {
            spentTotal += c.amount;
          });
        } else {
          if (transaction.isIncome) {
            transaction.categories.forEach((c) => {
              if (c.category === props.category.name) spentTotal -= c.amount;
            });
          } else {
            transaction.categories.forEach((c) => {
              if (c.category === props.category.name) spentTotal += c.amount;
            });
          }
        }
      }
    }
  }
  return spentTotal;
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
  const [yearStr, monthStr, dayStr] = dateStr.split("-");
  const year = Number(yearStr);
  const month = Number(monthStr);
  const day = Number(dayStr);
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
      await dataAccess.deleteTransaction(targetBudget, transactionToDelete.value.id, !isLastMonth(transactionToDelete.value));
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
