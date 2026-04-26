<template>
  <div class="goal-panel column">
    <q-card class="goal-card column">
      <!-- Hero -->
      <div class="goal-hero">
        <div class="hero-main row items-start no-wrap">
          <q-avatar size="44px" class="hero-avatar" text-color="white" color="primary">
            <q-icon name="savings" size="24px" />
          </q-avatar>
          <div class="hero-text column">
            <div class="hero-label text-caption text-muted">Savings goal</div>
            <div class="hero-name text-h6">{{ goal.name }}</div>
            <div class="hero-summary text-caption text-muted">{{ progressSummary }}</div>
          </div>
        </div>
        <q-btn flat dense round icon="close" class="hero-close" @click="$emit('close')">
          <q-tooltip>Close panel</q-tooltip>
        </q-btn>
      </div>

      <!-- Progress -->
      <q-card-section class="progress-section">
        <div class="row items-center justify-between">
          <div class="text-caption text-muted">Progress</div>
          <div class="text-caption text-muted">{{ Math.round(progress * 100) }}%</div>
        </div>
        <q-linear-progress
          :value="progress"
          color="primary"
          track-color="grey-3"
          class="goal-progress"
        />
      </q-card-section>

      <!-- Summary cards -->
      <q-card-section class="summary-grid">
        <div class="summary-item">
          <div class="summary-label">Available</div>
          <div class="summary-value">{{ formatCurrency(available) }}</div>
        </div>
        <div class="summary-item">
          <div class="summary-label">Contributed</div>
          <div class="summary-value">{{ formatCurrency(saved) }}</div>
        </div>
        <div v-if="withdrawn > 0" class="summary-item">
          <div class="summary-label">Withdrawn</div>
          <div class="summary-value">{{ formatCurrency(withdrawn) }}</div>
        </div>
      </q-card-section>

      <q-separator class="q-mx-md" />

      <!-- Tabs -->
      <q-tabs
        v-model="tab"
        dense
        no-caps
        align="left"
        active-color="primary"
        indicator-color="primary"
        class="goal-tabs"
      >
        <q-tab name="contribs" :label="`Contributions (${contribRows.length})`" />
        <q-tab name="spend" :label="`Goal spend (${spendRows.length})`" />
      </q-tabs>

      <q-separator />

      <!-- Activity list -->
      <q-card-section class="goal-activity-scroll q-pt-none q-px-md q-pb-md">
        <q-list separator class="q-pa-none">
          <q-item
            v-for="row in activeRows"
            :key="row.txId || rowMonth(row) || `${row.txDate}-${row.amount}`"
            clickable
            class="tx-item"
          >
            <q-item-section avatar class="tx-item__date-col">
              <div class="tx-date">
                <div class="tx-date__month">{{ formatRowMonth(row.txDate || rowMonth(row)) }}</div>
                <div class="tx-date__day">{{ formatRowDay(row.txDate, rowMonth(row)) }}</div>
              </div>
            </q-item-section>

            <q-item-section>
              <q-item-label class="tx-item__merchant">
                {{ row.merchant || (tab === 'contribs' ? 'Contribution' : 'Transaction') }}
              </q-item-label>
            </q-item-section>

            <q-item-section side class="tx-item__right">
              <span
                class="tx-item__amount"
                :class="tab === 'contribs' ? 'text-positive' : 'text-negative'"
              >
                {{ tab === 'contribs' ? '+' : '-' }}{{ formatCurrency(Math.abs(row.amount)) }}
              </span>
            </q-item-section>

            <q-item-section v-if="row.txId && row.budgetId" side class="tx-item__action">
              <q-btn
                flat
                round
                dense
                icon="delete_outline"
                color="grey-5"
                size="xs"
                :loading="deletingTxId === row.txId"
                @click.stop="confirmDelete(row.txId, row.budgetId, row.merchant, row.amount, tab === 'contribs' ? 'contribution' : 'spend')"
                style="min-width: 36px; min-height: 36px;"
              >
                <q-tooltip>Delete this transaction</q-tooltip>
              </q-btn>
            </q-item-section>
          </q-item>
        </q-list>

        <div v-if="activeRows.length === 0" class="q-pa-lg text-center text-grey-6">
          {{ tab === 'contribs' ? 'No contributions yet.' : 'No goal spend yet.' }}
        </div>
      </q-card-section>
    </q-card>

    <q-dialog v-model="showDeleteDialog">
      <q-card style="min-width: 320px">
        <q-card-section class="bg-negative q-py-sm">
          <span class="text-white">Delete transaction</span>
        </q-card-section>
        <q-card-section class="q-pt-md">
          Delete the
          <span class="text-weight-bold">{{ pendingDelete?.kind === 'spend' ? 'goal spend' : 'contribution' }}</span>
          for
          <span class="text-weight-bold">{{ pendingDelete?.merchant || 'this transaction' }}</span>
          ({{ pendingDelete ? formatCurrency(Math.abs(pendingDelete.amount)) : '' }})?
          <div class="text-caption q-mt-sm text-grey-7">
            This permanently removes the underlying transaction from its budget.
          </div>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" @click="showDeleteDialog = false" />
          <q-btn flat label="Delete" color="negative" :loading="!!deletingTxId" @click="executeDelete" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useQuasar } from 'quasar';
import { formatCurrency } from '../../utils/helpers';
import { useGoals } from '../../composables/useGoals';
import { useFamilyStore } from '../../store/family';
import { dataAccess } from '../../dataAccess';
import type { Goal, GoalContribution, GoalSpend } from '../../types';

function rowMonth(row: GoalContribution | GoalSpend): string | undefined {
  return (row as GoalContribution).month;
}

const props = defineProps<{ goal: Goal }>();
defineEmits<{ (e: 'close'): void }>();

const tab = ref<'contribs' | 'spend'>('contribs');
const $q = useQuasar();
const familyStore = useFamilyStore();

const { listContributions, listGoalSpends, loadGoalDetails, loadGoals, getGoal } = useGoals();

const contribRows = computed(() => listContributions(props.goal.id));
const spendRows = computed(() => listGoalSpends(props.goal.id));
const activeRows = computed(() => (tab.value === 'contribs' ? contribRows.value : spendRows.value));

// Pull the live goal from the cache so rollups update after a delete.
const liveGoal = computed(() => getGoal(props.goal.id) || props.goal);
const saved = computed(() => liveGoal.value.savedToDate || 0);
const withdrawn = computed(() => liveGoal.value.spentToDate || 0);
const available = computed(() => Math.max(saved.value - withdrawn.value, 0));
const target = computed(() => liveGoal.value.totalTarget || 0);
const progress = computed(() => {
  if (!target.value) return available.value > 0 ? 1 : 0;
  return Math.min(available.value / target.value, 1);
});

const progressSummary = computed(() => {
  const availStr = formatCurrency(available.value);
  if (target.value > 0) {
    return `${availStr} of ${formatCurrency(target.value)} saved`;
  }
  return `${availStr} available`;
});

const showDeleteDialog = ref(false);
const deletingTxId = ref<string | null>(null);
const pendingDelete = ref<{
  txId: string;
  budgetId: string;
  merchant?: string;
  amount: number;
  kind: 'contribution' | 'spend';
} | null>(null);

function confirmDelete(
  txId: string,
  budgetId: string,
  merchant: string | undefined,
  amount: number,
  kind: 'contribution' | 'spend',
) {
  pendingDelete.value = { txId, budgetId, merchant, amount, kind };
  showDeleteDialog.value = true;
}

async function executeDelete() {
  if (!pendingDelete.value) return;
  const { txId, budgetId } = pendingDelete.value;
  deletingTxId.value = txId;
  try {
    await dataAccess.deleteTransactionById(budgetId, txId);
    await loadGoalDetails(props.goal.id);
    if (familyStore.selectedEntityId) {
      await loadGoals(familyStore.selectedEntityId, true);
    }
    $q.notify({ type: 'positive', message: 'Transaction deleted.' });
  } catch (err) {
    console.error('Failed to delete goal transaction', err);
    $q.notify({ type: 'negative', message: 'Failed to delete transaction.' });
  } finally {
    deletingTxId.value = null;
    showDeleteDialog.value = false;
    pendingDelete.value = null;
  }
}

function formatRowMonth(dateStr?: string): string {
  if (!dateStr) return '--';
  const [yearStr, monthStr] = dateStr.split('-');
  const year = Number(yearStr);
  const month = Number(monthStr);
  if (!year || !month) return '--';
  const date = new Date(year, month - 1);
  if (Number.isNaN(date.getTime())) return '--';
  return date.toLocaleDateString('en-US', { month: 'short' });
}

function formatRowDay(txDate?: string, month?: string): string {
  // If only a month is provided (recurring contribution rollup), show the year's last 2 digits
  if (!txDate && month) {
    const [yearStr] = month.split('-');
    return yearStr ? yearStr.slice(-2) : '--';
  }
  if (!txDate) return '--';
  const [, , dayStr] = txDate.split('-');
  const day = Number(dayStr);
  if (!day) return '--';
  return String(day).padStart(2, '0');
}

onMounted(() => {
  console.log('GoalDetailsPanel opened for goal', props.goal.id);
});
</script>

<style scoped>
.goal-panel {
  min-height: 100%;
}

.goal-card {
  height: 100%;
  background: var(--color-surface-card);
  display: flex;
  flex-direction: column;
  border-radius: var(--radius-md);
  border: 1px solid rgba(15, 23, 42, 0.08);
  padding: 0;
}

/* Hero */
.goal-hero {
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

.hero-label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.hero-name {
  font-size: 1.15rem;
  font-weight: 600;
}

.hero-summary {
  font-size: 0.85rem;
  color: var(--color-text-muted);
}

.hero-close {
  flex-shrink: 0;
}

/* Progress */
.progress-section {
  padding: 8px 20px 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.goal-progress {
  height: 6px;
  border-radius: 999px;
}

/* Summary cards */
.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 12px;
  padding: 12px;
}

.summary-item {
  border: 1px solid rgba(15, 23, 42, 0.08);
  border-radius: var(--radius-sm);
  padding: 12px;
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

/* Tabs */
.goal-tabs {
  padding: 0 12px;
}

/* Activity scroll */
.goal-activity-scroll {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
}

/* Transaction row — mirrors BudgetTransactionItem */
.tx-item {
  min-height: 48px;
  padding: 4px 8px;
}

.tx-item__date-col {
  min-width: 40px;
  padding-right: 8px;
}

.tx-date {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  border: 1.5px solid var(--color-outline-soft);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1px;
}

.tx-date__month {
  font-size: 0.6rem;
  font-weight: 500;
  text-transform: uppercase;
  line-height: 1;
  color: var(--color-text-muted);
}

.tx-date__day {
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1;
  color: var(--color-text-primary);
}

.tx-item__merchant {
  font-size: 0.85rem;
  font-weight: 500;
  line-height: 1.3;
}

.tx-item__right {
  padding-left: 4px;
}

.tx-item__amount {
  font-size: 0.85rem;
  font-weight: 600;
  white-space: nowrap;
}

.tx-item__action {
  padding-left: 0;
  min-width: 36px;
}

@media (max-width: 1023px) {
  /* Same fix as CategoryTransactions: keep card filling its parent so the
     background extends to the bottom of the maximized mobile dialog. */
  .goal-card {
    height: 100%;
  }
}
</style>
