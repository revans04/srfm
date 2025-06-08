<!-- CategoryTransactions.vue -->
<template>
  <q-page class="category-transactions text-black q-pa-md">
    <!-- Loading Overlay -->
    <q-inner-loading :showing="loading">
      <q-spinner color="primary" size="50px" />
    </q-inner-loading>

    <!-- Header -->
    <q-row class="header">
      <q-col>
        <h2 class="category-title">{{ category.name }}</h2>
      </q-col>
      <q-col cols="auto">
        <q-btn
          :class="isMobile ? 'q-mr-sm' : 'q-mr-sm q-mt-sm'"
          flat
          round
          icon="mdi-close"
          @click="$emit('close')"
        />
      </q-col>
    </q-row>

    <!-- Progress Bar and Remaining -->
    <q-row class="q-mt-sm">
      <q-col>
        <div class="progress-section">
          <div class="progress-label">
            <span :class="!isIncome && spent > category.target ? 'text-negative' : ''">{{
              formatCurrency(toDollars(toCents(spent)))
            }}</span>
            {{ isIncome ? 'received' : 'spent' }} of
            {{ formatCurrency(toDollars(toCents(category.target))) }}
          </div>
          <q-linear-progress
            :value="progressPercentage / 100"
            size="10px"
            :color="isIncome ? 'green' : 'primary'"
            track-color="grey-4"
            round
          />
        </div>
        <div
          class="remaining-section"
          :class="{ 'over-budget': (remaining || 0) < 0 && !isIncome }"
        >
          <span class="remaining-label">
            {{ category.group === 'Income' ? 'Left to Receive ' : 'Available ' }}</span
          >
          <span class="remaining-amount">
            {{ formatCurrency(toDollars(toCents(available))) }}
          </span>
        </div>
      </q-col>
    </q-row>

    <!-- Transactions List -->
    <q-row class="flex-grow-1 q-mt-md q-pl-none q-pr-none">
      <q-col class="transaction-list q-pl-none q-pr-none">
        <h3 class="section-title q-pb-sm">Transactions ({{ categoryTransactions.length }})</h3>
        <div class="q-my-sm bg-white rounded-borders q-pa-sm q-mb-md">
          <q-input v-model="search" label="Search" dense flat hide-bottom-space>
            <template v-slot:append>
              <q-icon name="mdi-magnify" />
            </template>
          </q-input>
        </div>
        <q-list bordered separator class="rounded-borders">
          <q-item
            v-for="transaction in categoryTransactions"
            :key="transaction.id"
            class="transaction-item"
            clickable
            dense
            @click="editTransaction(transaction)"
          >
            <q-item-section>
              <q-row class="q-pa-sm align-center" no-wrap>
                <q-col
                  cols="2"
                  class="q-pt-sm font-weight-bold text-primary"
                  style="min-width: 60px; font-size: 10px"
                >
                  {{ formatDate(transaction.date) }}
                </q-col>
                <q-col class="text-truncate" style="flex: 1; min-width: 0">
                  {{ transaction.merchant }}
                </q-col>
                <q-col
                  cols="auto"
                  class="text-right no-wrap"
                  :class="transaction.isIncome ? 'text-green' : ''"
                  style="min-width: 60px"
                >
                  ${{ Math.abs(getCategoryAmount(transaction)).toFixed(2) }}
                </q-col>
                <q-col cols="auto" class="text-right" style="min-width: 40px">
                  <q-icon
                    name="mdi-trash-can-outline"
                    size="sm"
                    color="negative"
                    @click.stop="confirmDelete(transaction)"
                    title="Move to Trash"
                  />
                </q-col>
              </q-row>
            </q-item-section>
          </q-item>
          <q-item v-if="categoryTransactions.length === 0">
            <q-item-section>
              <q-item-label>No transactions for this category.</q-item-label>
            </q-item-section>
          </q-item>
        </q-list>
      </q-col>
    </q-row>

    <!-- Floating Action Button -->
    <q-btn
      fab
      color="primary"
      icon="mdi-plus"
      class="q-mr-sm"
      :class="isMobile ? 'q-mb-xl' : 'q-mb-sm'"
      style="position: fixed; bottom: 16px; right: 16px"
      @click="$emit('add-transaction')"
    />

    <!-- Edit Transaction Dialog -->
    <q-dialog v-model="showEditDialog" :maximized="isMobile" style="max-width: 600px">
      <q-card dense>
        <q-card-section class="bg-primary q-py-md text-white">
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
    <q-dialog v-model="showDeleteDialog" persistent>
      <q-card>
        <q-card-section class="bg-negative text-white q-py-sm">
          <div class="text-h6">Confirm Deletion</div>
        </q-card-section>
        <q-card-section class="q-pt-md">
          Are you sure you want to delete the transaction for "{{ transactionToDelete?.merchant }}"
          on {{ transactionToDelete ? formatDate(transactionToDelete.date) : '' }}?
        </q-card-section>
        <q-card-actions align="right">
          <q-space />
          <q-btn flat label="Cancel" color="grey" v-close-popup />
          <q-btn flat label="Move to Trash" color="negative" @click="executeDelete" v-close-popup />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { dataAccess } from '../dataAccess';
import TransactionForm from './TransactionForm.vue';
import type { BudgetCategory, Transaction } from '../types';
import { toDollars, toCents, formatCurrency } from '../utils/helpers';
import { useBudgetStore } from '../store/budget';

const props = defineProps<{
  category: BudgetCategory;
  transactions?: Transaction[] | null;
  budgetId: string;
  userId: string;
  categoryOptions: string[];
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'add-transaction'): void;
  (e: 'update-transactions', transactions: Transaction[]): void;
}>();

const search = ref('');
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
      if (props.transactions[i].deleted) continue; // Skip deleted transactions
      if (
        props.transactions[i].categories.filter((c) => c.category === props.category.name).length >
        0
      ) {
        if (isIncome.value) {
          props.transactions[i].categories.forEach((c) => {
            spent += c.amount;
          });
        } else {
          if (props.transactions[i].isIncome) {
            props.transactions[i].categories.forEach((c) => {
              if (c.category === props.category.name) spent -= c.amount;
            });
          } else {
            props.transactions[i].categories.forEach((c) => {
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
  return (
    (props.category.isFund ? Number(props.category.carryover) || 0 : 0) +
      Number(props.category.target) || 0
  );
});

const remaining = computed(() => {
  if (props.category.group === 'Income')
    return (Number(spent.value) || 0) - (Number(props.category.target) || 0);
  return (Number(props.category.target) || 0) - (Number(spent.value) || 0);
});

const available = computed(() => {
  if (props.category.group === 'Income') {
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
  return props.category.group === 'Income';
});

const categoryTransactions = computed(() => {
  let temp = props.transactions || [];
  temp = temp
    .filter((t) => !t.deleted) // Exclude deleted transactions
    .filter((t) =>
      t.categories && Array.isArray(t.categories)
        ? t.categories.some((split) => split.category === props.category.name)
        : false,
    )
    .sort((a, b) => {
      const dateA = new Date(a.date);
      const dateB = new Date(b.date);
      if (dateA.getTime() !== dateB.getTime()) {
        return dateB.getTime() - dateA.getTime();
      }
      return a.merchant.localeCompare(b.merchant);
    });
  if (search.value && search.value !== '') {
    temp = temp.filter(
      (t) =>
        t.merchant.toLowerCase().includes(search.value.toLowerCase()) ||
        t.amount.toString().toLowerCase().includes(search.value.toLowerCase()),
    );
  }
  return temp;
});

function isLastMonth(transaction: Transaction) {
  return (
    transaction.budgetMonth ==
    budgetStore.availableBudgetMonths[budgetStore.availableBudgetMonths.length - 1]
  );
}

const getCategoryAmount = (transaction: Transaction) => {
  const split = transaction.categories?.find((s) => s.category === props.category.name);
  return split ? split.amount : 0;
};

const formatDate = (dateStr: string): string => {
  const [year, month, day] = dateStr.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  return `
    ${date.toLocaleDateString('en-US', { month: 'short' })}
    ${date.toLocaleDateString('en-US', { day: 'numeric' })}
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
      await dataAccess.deleteTransaction(
        targetBudget,
        transactionToDelete.value.id,
        !isLastMonth(transactionToDelete.value),
      );
    }
    const updatedTransactions = budgetStore.getBudget(props.budgetId)?.transactions;
    if (updatedTransactions) {
      emit('update-transactions', updatedTransactions);
    }
  } catch (error: unknown) {
    console.error('Error moving transaction to trash:', error);
  } finally {
    loading.value = false;
    showDeleteDialog.value = false;
    transactionToDelete.value = null;
  }
}

function onTransactionSaved() {
  showEditDialog.value = false;
  // No need to emit update-transactions here; TransactionForm already emits it
}

function updateTransactions(updatedTransactions: Transaction[]) {
  // Propagate the update-transactions event to DashboardView
  emit('update-transactions', updatedTransactions);
}

onMounted(() => {
  // Rely on budgetStore instead of fetching directly
  const budget = budgetStore.getBudget(props.budgetId);
  if (!budget) {
    console.error(`Budget ${props.budgetId} not found in store`);
  }
});
</script>

<style scoped>
.rounded-borders {
  border-radius: 10px;
}
</style>
