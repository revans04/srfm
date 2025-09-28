<!-- CategoryTransactions.vue -->
<template>
  <div class="category-transactions-panel column">
    <q-card flat bordered class="category-details-card column">
      <div :class="heroClass">
        <div class="hero-main row items-start no-wrap">
          <q-avatar size="56px" class="hero-avatar" text-color="white" :color="isIncome ? 'positive' : 'primary'">
            <q-icon :name="heroIcon" size="28px" />
          </q-avatar>
          <div class="hero-text column">
            <div class="hero-name text-weight-medium">{{ category.name }}</div>
            <div class="hero-progress-summary">{{ progressSummary }}</div>
          </div>
        </div>
        <q-btn flat dense round icon="close" class="hero-close" text-color="white" @click="$emit('close')" />
      </div>

      <q-card-section class="hero-progress-wrapper">
        <q-linear-progress :value="progressPercentage / 100" color="white" track-color="white" class="hero-progress" />
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
        <q-scroll-area class="category-transactions-scroll__area">
          <q-list separator>
            <q-item
              v-for="transaction in categoryTransactions"
              :key="transaction.id"
              clickable
              class="transaction-row"
              @click="editTransaction(transaction)"
            >
              <q-item-section avatar>
                <div class="date-pill">
                  <div class="date-month">{{ formatDateMonth(transaction.date) }}</div>
                  <div class="date-day">{{ formatDateDay(transaction.date) }}</div>
                </div>
              </q-item-section>
              <q-item-section>
                <div class="transaction-merchant">{{ transaction.merchant || 'Unnamed transaction' }}</div>
                <div class="transaction-meta">
                  <span>{{ formatTransactionCategories(transaction) }}</span>
                  <GoalFundingPill
                    v-if="transaction.fundedByGoalId"
                    :goal="goalMap[transaction.fundedByGoalId]"
                    class="q-ml-xs"
                  />
                </div>
              </q-item-section>
              <q-item-section side class="text-right">
                <div class="transaction-amount" :class="transaction.isIncome ? 'text-positive' : 'text-negative'">
                  {{ transaction.isIncome ? '+' : '-' }}{{ formatTransactionAmount(transaction) }}
                </div>
                <q-btn
                  flat
                  dense
                  round
                  icon="delete"
                  color="negative"
                  @click.stop="confirmDelete(transaction)"
                />
              </q-item-section>
            </q-item>
          </q-list>
          <div v-if="!categoryTransactions.length" class="q-pa-lg text-center text-grey-6">
            No transactions for this category.
          </div>
        </q-scroll-area>
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
import GoalFundingPill from './transactions/GoalFundingPill.vue';
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

const heroClass = computed(() => {
  if (isIncome.value) return 'category-hero income';
  if (props.category.isFund) return 'category-hero fund';
  return 'category-hero expense';
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

function formatDateMonth(dateStr?: string): string {
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
  return date.toLocaleDateString('en-US', { month: 'short' });
}

function formatDateDay(dateStr?: string): string {
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
  return date.toLocaleDateString('en-US', { day: '2-digit' });
}

function formatTransactionCategories(transaction: Transaction): string {
  if (!transaction.categories || transaction.categories.length === 0) {
    return 'Uncategorized';
  }
  return transaction.categories.map((c) => c.category).join(', ');
}

function getCategoryAmount(transaction: Transaction): number {
  const split = transaction.categories?.find((s) => s.category === props.category.name);
  return split ? split.amount : 0;
}

function formatTransactionAmount(transaction: Transaction): string {
  const amount = Math.abs(Number(getCategoryAmount(transaction)) || 0);
  return formatCurrency(toDollars(toCents(amount)));
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
  position: relative;
  min-height: 100%;
}

.category-details-card {
  height: 100%;
  background: var(--q-grey-1);
  display: flex;
  flex-direction: column;
}

.category-hero {
  position: relative;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: 24px;
  border-top-left-radius: 4px;
  border-top-right-radius: 4px;
  color: white;
  background: linear-gradient(135deg, rgba(63, 81, 181, 0.95), rgba(33, 150, 243, 0.85));
}

.category-hero.income {
  background: linear-gradient(135deg, rgba(56, 142, 60, 0.95), rgba(129, 199, 132, 0.85));
}

.category-hero.fund {
  background: linear-gradient(135deg, rgba(21, 101, 192, 0.95), rgba(100, 181, 246, 0.85));
}

.hero-main {
  gap: 16px;
}

.hero-avatar {
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
}

.hero-text {
  gap: 6px;
}

.hero-name {
  font-size: 1.15rem;
}

.hero-progress-summary {
  font-size: 0.85rem;
  opacity: 0.85;
}

.hero-amount {
  text-align: right;
}

.hero-amount-label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.hero-amount-value {
  font-size: 1.4rem;
  font-weight: 600;
}

.hero-close {
  position: absolute;
  top: 12px;
  right: 12px;
}

.hero-progress-wrapper {
  padding: 12px 24px 0;
}

.hero-progress {
  height: 6px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.25);
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  padding: 16px 24px;
}

.summary-item {
  background: white;
  border-radius: 12px;
  padding: 12px 16px;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.08);
}

.summary-label {
  font-size: 0.75rem;
  color: #546e7a;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 6px;
}

.summary-value {
  font-size: 1rem;
  font-weight: 600;
}

.transactions-header {
  padding: 8px 24px;
}

.category-transactions-scroll {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.category-transactions-scroll__area {
  flex: 1;
  min-height: 0;
  display: flex;
}

.category-transactions-scroll__area :deep(.q-scrollarea) {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.category-transactions-scroll__area :deep(.q-scrollarea__container) {
  flex: 1;
  min-height: 0;
}

.transactions-title {
  font-size: 1rem;
  font-weight: 600;
}

.transactions-subtitle {
  font-size: 0.85rem;
  color: #607d8b;
}

.transaction-row {
  border-radius: 12px;
  margin: 8px 8px 0;
  padding: 4px 8px;
  transition: background 0.2s ease;
}

.transaction-row:hover {
  background: rgba(33, 150, 243, 0.08);
}

.date-pill {
  width: 48px;
  height: 56px;
  border-radius: 18px;
  background: white;
  color: #1f2937;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.12);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  font-weight: 600;
}

.date-month {
  font-size: 0.75rem;
  text-transform: uppercase;
  color: #1e88e5;
}

.date-day {
  font-size: 1.1rem;
}

.transaction-merchant {
  font-size: 1rem;
  font-weight: 600;
}

.transaction-meta {
  font-size: 0.85rem;
  color: #607d8b;
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
