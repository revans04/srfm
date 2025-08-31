<!-- CategoryTransactions.vue -->
<template>
  <q-page fluid class="category-transactions text-black">
    <!-- Header -->
    <div class="row header">
      <div class="col">
        <h2 class="category-title">{{ category.name }}</h2>
      </div>
      <div class="col col-auto">
        <q-fab :class="isMobile ? 'mr-2' : 'mr-2 mt-2'" icon="close" variant="plain" :absolute="true" location="top" @click="$emit('close')" />
      </div>
    </div>

    <!-- Progress Bar and Remaining -->
    <div class="row mt-2">
      <div class="col">
        <div class="progress-section">
          <div class="progress-label">
            <span :class="!isIncome && spent > category.target ? 'text-negative' : ''">{{ formatCurrency(toDollars(toCents(spent))) }}</span>
            {{ isIncome ? 'received' : 'spent' }} of
            {{ formatCurrency(toDollars(toCents(category.target))) }}
          </div>
          <q-linear-progress
            v-model="progressPercentage"
            height="10"
            :color="isIncome ? 'green' : 'primary'"
            background-color="#e0e0e0"
            rounded
          ></q-linear-progress>
        </div>
        <div class="remaining-section" :class="{ 'over-budget': (remaining || 0) < 0 && !isIncome }">
          <span class="remaining-label"> {{ category.group == 'Income' ? 'Left to Receive ' : 'Available ' }}</span>
          <span class="remaining-amount">
            {{ formatCurrency(toDollars(toCents(available))) }}
          </span>
        </div>
      </div>
    </div>

    <!-- Transactions List -->
    <div class="row flex-grow-1 mt-4 q-pl-none q-pr-none">
      <div class="col transaction-list q-pl-none q-pr-none">
        <h3 class="section-title q-pb-sm">Transactions ({{ categoryTransactions.length }})</h3>
        <div class="my-2 bg-white rounded-10 q-pt-sm q-pr-md q-pl-md mb-4">
          <q-input v-model="search" label="Search" dense clearable prepend-icon="search"></q-input>
        </div>
        <q-list dense class="rounded-10">
          <q-item
            v-for="transaction in categoryTransactions"
            :key="transaction.id"
            class="transaction-item"
            dense
            clickable
            @click="editTransaction(transaction)"
            style="border-bottom: 1px solid rgb(var(--v-theme-light))"
          >
            <q-item-section class="d-flex align-center">
              <div class="row q-pa-sm align-center no-gutters">
                <div class="col q-pt-sm font-weight-bold text-primary col-2" style="min-width: 60px; font-size: 10px">
                  {{ formatDate(transaction.date) }}
                </div>
                <div class="col text-truncate" style="flex: 1; min-width: 0">
                  {{ transaction.merchant }}
                </div>
                <div class="col text-right no-wrap col-auto" :class="transaction.isIncome ? 'green--text' : ''" style="min-width: 60px">
                  ${{ Math.abs(getCategoryAmount(transaction)).toFixed(2) }}
                </div>
                <div class="col text-right col-auto" style="min-width: 40px">
                  <q-icon
                    small
                    name="delete"
                    color="negative"
                    title="Move to Trash"
                    @click.stop="confirmDelete(transaction)"
                  />
                </div>
              </div>
            </q-item-section>
          </q-item>
          <q-item v-if="categoryTransactions.length === 0">
            <q-item-label>No transactions for this category.</q-item-label>
          </q-item>
        </q-list>
      </div>
    </div>

    <!-- Floating Action Button -->
    <q-fab icon="add" :app="true" color="primary" @click="$emit('add-transaction')" location="bottom right" class="mr-2" :class="isMobile ? 'mb-14' : 'mb-2'" />

    <!-- Edit Transaction Dialog -->
    <q-dialog v-model="showEditDialog" :width="!isMobile ? '550px' : undefined" :fullscreen="isMobile">
      <q-card dense>
        <q-card-section class="bg-primary row items-center q-py-md">
          <div class="text-white">Edit {{ transactionToEdit?.merchant }} Transaction</div>
          <q-btn
            flat
            dense
            color="negative"
            label="X"
            class="q-ml-auto"
            @click="showEditDialog = false"
          />
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
        <q-card-section class="bg-negative q-py-sm">
          <span class="text-white">Confirm Deletion</span>
        </q-card-section>
        <q-card-section class="q-pt-md">
          Are you sure you want to delete the transaction for "{{ transactionToDelete?.merchant }}" on
          {{ transactionToDelete ? formatDate(transactionToDelete.date) : '' }}?
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteDialog = false">Cancel</q-btn>
          <q-btn color="negative" variant="flat" @click="executeDelete">Move to Trash</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useQuasar, QSpinner } from 'quasar';
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
const $q = useQuasar();
const showEditDialog = ref(false);
const transactionToEdit = ref<Transaction | null>(null);
const showDeleteDialog = ref(false);
const transactionToDelete = ref<Transaction | null>(null);

const spent = computed(() => {
  let spentTotal = 0;
  if (props.transactions) {
    for (const transaction of props.transactions) {
      if (transaction.deleted) continue;
      const hasCategory = transaction.categories.some((c) => c.category === props.category.name);
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
  if (props.category.group === 'Income') return (Number(spent.value) || 0) - (Number(props.category.target) || 0);
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

const isMobile = computed(() => $q.screen.lt.md);

const isIncome = computed(() => {
  return props.category.group === 'Income';
});

const categoryTransactions = computed(() => {
  let temp = props.transactions || [];
  temp = temp
    .filter((t) => !t.deleted)
    .filter((t) => (t.categories && Array.isArray(t.categories) ? t.categories.some((split) => split.category === props.category.name) : false))
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
      (t) => t.merchant.toLowerCase().includes(search.value.toLowerCase()) || t.amount.toString().toLowerCase().includes(search.value.toLowerCase()),
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
  const [yearStr, monthStr, dayStr] = dateStr.split('-');
  const year = Number(yearStr);
  const month = Number(monthStr);
  const day = Number(dayStr);
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
  $q.loading.show({
    message: 'Deleting transaction...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });
  try {
    const targetBudget = budgetStore.getBudget(props.budgetId);
    if (targetBudget) {
      await dataAccess.deleteTransaction(targetBudget, transactionToDelete.value.id, !isLastMonth(transactionToDelete.value));
    }
    const updatedTransactions = budgetStore.getBudget(props.budgetId)?.transactions;
    if (updatedTransactions) {
      emit('update-transactions', updatedTransactions);
    }
  } catch (error: unknown) {
    console.error('Error moving transaction to trash:', error);
  } finally {
    $q.loading.hide();
    showDeleteDialog.value = false;
    transactionToDelete.value = null;
  }
}

function onTransactionSaved() {
  showEditDialog.value = false;
}

function updateTransactions(updatedTransactions: Transaction[]) {
  emit('update-transactions', updatedTransactions);
}

onMounted(() => {
  const budget = budgetStore.getBudget(props.budgetId);
  if (!budget) {
    console.error(`Budget ${props.budgetId} not found in store`);
  }
});
</script>

<style scoped></style>
