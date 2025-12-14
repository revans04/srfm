<!-- CategoryTransactions.vue -->
<template>
  <div class="category-transactions-panel column">
    <q-card flat bordered class="category-details-card column">
      <div class="category-hero">
        <div class="hero-main row items-start no-wrap">
          <q-avatar size="44px" class="hero-avatar" text-color="white" :color="isIncome ? 'positive' : 'primary'">
            <q-icon :name="heroIcon" size="24px" />
          </q-avatar>
          <div class="hero-text column">
            <div class="category-hero__label text-caption text-muted">{{ heroLabel }}</div>
            <div class="hero-name text-h6">{{ category.name }}</div>
            <div class="hero-progress-summary text-caption text-muted">{{ progressSummary }}</div>
          </div>
        </div>
        <q-btn flat dense round icon="close" class="hero-close" @click="$emit('close')">
          <q-tooltip>Close panel</q-tooltip>
        </q-btn>
      </div>

      <q-card-section class="category-progress-section">
        <div class="row items-center justify-between">
          <div class="text-caption text-muted">Progress</div>
          <div class="text-caption text-muted">{{ Math.round(progressPercentage) }}%</div>
        </div>
        <q-linear-progress
          :value="progressPercentage / 100"
          color="primary"
          track-color="grey-3"
          class="category-progress"
        />
      </q-card-section>

      <q-card-section class="summary-grid q-gutter-md">
        <div class="summary-item">
          <div class="summary-label">{{ availableLabel }}</div>
          <div class="summary-value" :class="{ 'text-negative': available < 0 && !isIncome }">
            {{ availableDisplay }}
          </div>
        </div>
        <div v-if="showCarryover" class="summary-item">
          <div class="summary-label">Carryover</div>
          <div class="summary-value">{{ carryoverDisplay }}</div>
        </div>
      </q-card-section>

      <q-separator class="q-mx-md" />

      <q-card-section class="transactions-header row items-center no-wrap q-mt-sm">
        <div class="col">
          <div class="transactions-title">Activity this month</div>
          <div class="transactions-subtitle">
            {{ categoryTransactions.length }}
            {{ categoryTransactions.length === 1 ? 'transaction' : 'transactions' }}
          </div>
        </div>
      </q-card-section>

      <q-card-section class="q-pt-none q-px-md">
        <q-input
          v-model="search"
          dense
          rounded
          outlined
          clearable
          placeholder="Search transactions"
          prepend-icon="search"
        />
      </q-card-section>

      <q-separator class="q-mx-md" />

      <q-card-section class="category-transactions-scroll q-pt-none q-px-md q-pb-md">
          <q-list separator class="q-pa-none">
            <BudgetTransactionItem
              v-for="transaction in categoryTransactions"
              :key="transaction.id"
              class="transaction-row"
              :transaction="transaction"
              :category-name="category.name"
              :goal="goalMap[transaction.fundedByGoalId]"
              removable
              @select="editTransaction"
              @delete="confirmDelete"
            />
          </q-list>
          <div v-if="!categoryTransactions.length" class="q-pa-lg text-center text-grey-6">
            No transactions for this category.
          </div>
      </q-card-section>
    </q-card>

    <q-dialog v-model="showEditDialog" :width="!isMobile ? '600px' : undefined" :fullscreen="isMobile">
      <q-card dense :width="!isMobile ? '600px' : undefined">
        <q-card-section class="bg-primary row items-center q-py-md">
          <div class="text-white">Edit {{ transactionToEdit?.merchant }} Transaction</div>
          <q-btn flat dense color="white" label="X" class="q-ml-auto" @click="showEditDialog = false" />
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useQuasar, QSpinner } from 'quasar';
import { dataAccess } from '../dataAccess';
import TransactionForm from './TransactionForm.vue';
import BudgetTransactionItem from './BudgetTransactionItem.vue';
import type { BudgetCategory, Transaction, Goal } from '../types';
import { toDollars, toCents, formatCurrency } from '../utils/helpers';
import { useBudgetStore } from '../store/budget';
import { useGoals } from '../composables/useGoals';
import { useFamilyStore } from '../store/family';

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
const familyStore = useFamilyStore();
const { listGoals } = useGoals();
const goalMap = ref<Record<string, Goal>>({});

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

const heroIcon = computed(() => {
  if (props.category.isFund) return 'savings';
  if (isIncome.value) return 'trending_up';
  return 'payments';
});

const heroLabel = computed(() => {
  if (isIncome.value) return 'Income';
  if (props.category.isFund) return 'Savings goal';
  return 'Expense';
});

const availableDisplay = computed(() => formatCurrency(toDollars(toCents(available.value))));
const spentDisplay = computed(() => formatCurrency(toDollars(toCents(Number(spent.value) || 0))));
const carryoverDisplay = computed(() => formatCurrency(toDollars(toCents(Number(props.category.carryover) || 0))));
const availableLabel = computed(() => (isIncome.value ? 'To receive' : 'Available'));
const showCarryover = computed(() => props.category.isFund && !!Number(props.category.carryover));
const progressSummary = computed(() => {
  const totalPlanned = formatCurrency(toDollars(toCents(carryOverAndTarget.value)));
  return `${spentDisplay.value} ${isIncome.value ? 'received' : 'spent'} of ${totalPlanned}`;
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
      const merchantA = a.merchant || '';
      const merchantB = b.merchant || '';
      return merchantA.localeCompare(merchantB);
    });
  if (search.value && search.value !== '') {
    const searchTerm = search.value.toLowerCase();
    temp = temp.filter((t) => {
      const merchantMatch = t.merchant?.toLowerCase().includes(searchTerm);
      const amountMatch = t.amount?.toString().toLowerCase().includes(searchTerm);
      const categoryMatch = t.categories?.some((split) => split.category.toLowerCase().includes(searchTerm));
      const notesMatch = t.notes?.toLowerCase().includes(searchTerm);
      return Boolean(merchantMatch || amountMatch || categoryMatch || notesMatch);
    });
  }
  return temp;
});

function formatDate(dateStr?: string): string {
  if (!dateStr) {
    return '--';
  }

  const [yearStr, monthStr, dayStr] = dateStr.split('-');
  const year = Number(yearStr);
  const month = Number(monthStr);
  const day = Number(dayStr);
  const date = new Date(year, month - 1, day);
  if (Number.isNaN(date.getTime())) {
    return '--';
  }

  return `
    ${date.toLocaleDateString('en-US', { month: 'short' })}
    ${date.toLocaleDateString('en-US', { day: 'numeric' })}
  `;
}

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
      await dataAccess.deleteTransaction(targetBudget, transactionToDelete.value.id);
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
  const foundBudget = budgetStore.getBudget(props.budgetId);
  if (!foundBudget) {
    console.error(`Budget ${props.budgetId} not found in store`);
  }
  if (familyStore.selectedEntityId) {
    const gs = listGoals(familyStore.selectedEntityId);
    goalMap.value = Object.fromEntries(gs.map((g) => [g.id, g]));
  }
});
</script>

<style scoped>
.category-transactions-panel {
  min-height: 100%;
}

.category-details-card {
  height: 100%;
  background: var(--color-surface-card);
  display: flex;
  flex-direction: column;
  border-radius: var(--radius-md);
  border: 1px solid rgba(15, 23, 42, 0.08);
  padding: 0;
}

.category-hero {
  padding: 16px 20px;
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
  border-radius: var(--radius-md) var(--radius-md) 0 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.hero-main {
  gap: 12px;
}

.hero-avatar {
  box-shadow: 0 6px 16px rgba(15, 23, 42, 0.2);
}

.hero-text {
  gap: 6px;
}

.hero-name {
  font-size: 1.15rem;
  font-weight: 600;
}

.hero-progress-summary {
  font-size: 0.85rem;
  color: var(--color-text-muted);
}

.hero-close {
  flex-shrink: 0;
}

.category-progress-section {
  padding: 8px 20px 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.category-progress {
  height: 6px;
  border-radius: 999px;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 12px;
  padding: 12px 20px;
}

.summary-item {
  border: 1px solid rgba(15, 23, 42, 0.08);
  border-radius: var(--radius-sm);
  padding: 12px 16px;
  background: var(--color-surface-card);
}

.summary-label {
  font-size: 0.75rem;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.06em;
  margin-bottom: 6px;
}

.summary-value {
  font-size: 1rem;
  font-weight: 600;
}

.transactions-header {
  padding: 10px 20px 0;
}

.transactions-title {
  font-size: 1rem;
  font-weight: 600;
}

.transactions-subtitle {
  font-size: 0.85rem;
  color: var(--color-text-muted);
}

.category-transactions-scroll {
  flex: 1;
  min-height: 0;
}

.category-transactions-scroll__area,
.category-transactions-scroll__area :deep(.q-scrollarea),
.category-transactions-scroll__area :deep(.q-scrollarea__container) {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.transaction-row {
  border-radius: var(--radius-sm);
  padding: 6px 8px;
  transition: background 0.2s ease;
}

.transaction-row:hover {
  background: rgba(15, 23, 42, 0.04);
}

.transaction-merchant {
  font-size: 1rem;
  font-weight: 600;
}

.transaction-meta {
  font-size: 0.85rem;
  color: var(--color-text-muted);
  display: flex;
  align-items: center;
  gap: 4px;
}

.transaction-amount {
  font-size: 1rem;
  font-weight: 600;
}

@media (max-width: 1023px) {
  .category-details-card {
    height: auto;
  }
}
</style>
