<template>
  <q-page class="category-transactions text-black bg-grey-2 q-pa-md">
    <div class="row header items-center">
      <div class="col">
        <h2 class="category-title">{{ goal.name }}</h2>
      </div>
      <div class="col-auto">
        <q-btn flat dense icon="close" class="q-mt-sm" @click="$emit('close')" />
      </div>
    </div>
    <div class="row q-mt-sm">
      <div class="col">
        <div class="progress-section">
          <div class="progress-label">
            <span class="text-weight-bold">{{ formatCurrency(available) }}</span>
            available
            <template v-if="target > 0">of {{ formatCurrency(target) }}</template>
          </div>
          <q-linear-progress
            :value="progress"
            height="10"
            color="primary"
            track-color="grey-3"
            rounded
          />
          <div class="progress-breakdown text-caption text-grey-7 q-mt-xs">
            {{ formatCurrency(saved) }} contributed
            <span v-if="withdrawn > 0">
              · {{ formatCurrency(withdrawn) }} withdrawn
            </span>
          </div>
        </div>
      </div>
    </div>
    <q-tabs v-model="tab" dense class="bg-grey-2 q-mt-md">
      <q-tab name="contribs" label="Contributions" />
      <q-tab name="spend" label="Goal Spend" />
    </q-tabs>
    <q-tab-panels v-model="tab" animated class="q-mt-md">
      <q-tab-panel name="contribs">
        <q-card flat class="bg-white" rounded>
          <q-list dense>
            <q-item
              v-for="row in contribRows"
              :key="row.txId || row.month || `${row.txDate}-${row.amount}`"
              class="transaction-item"
            >
              <q-item-section>
                <div class="row q-pa-sm align-center no-gutters">
                  <div class="col q-pt-sm font-weight-bold text-primary col-2" style="min-width: 60px; font-size: 10px">
                    {{ row.txDate ? formatDate(row.txDate) : formatDateMonthYYYY(row.month || '') }}
                  </div>
                  <div class="col text-truncate" style="flex: 1; min-width: 0">
                    {{ row.merchant || 'Contribution' }}
                  </div>
                  <div class="col text-right no-wrap col-auto" style="min-width: 60px">
                    {{ formatCurrency(row.amount) }}
                  </div>
                  <div class="col-auto q-ml-sm">
                    <q-btn
                      v-if="row.txId && row.budgetId"
                      flat
                      dense
                      round
                      size="sm"
                      icon="delete"
                      color="grey-6"
                      :loading="deletingTxId === row.txId"
                      @click="confirmDelete(row.txId, row.budgetId, row.merchant, row.amount, 'contribution')"
                    >
                      <q-tooltip>Delete this transaction</q-tooltip>
                    </q-btn>
                  </div>
                </div>
              </q-item-section>
            </q-item>
            <q-item v-if="contribRows.length === 0">
              <q-item-label>No contributions.</q-item-label>
            </q-item>
          </q-list>
        </q-card>
      </q-tab-panel>
      <q-tab-panel name="spend">
        <q-card flat class="bg-white" rounded>
          <q-list dense>
            <q-item v-for="row in spendRows" :key="row.txId" class="transaction-item">
              <q-item-section>
                <div class="row q-pa-sm align-center no-gutters">
                  <div class="col q-pt-sm font-weight-bold text-primary col-2" style="min-width: 60px; font-size: 10px">
                    {{ formatDate(row.txDate) }}
                  </div>
                  <div class="col text-truncate" style="flex: 1; min-width: 0">
                    {{ row.merchant || 'Transaction' }}
                  </div>
                  <div class="col text-right no-wrap col-auto" style="min-width: 60px">
                    {{ formatCurrency(row.amount) }}
                  </div>
                  <div class="col-auto q-ml-sm">
                    <q-btn
                      v-if="row.txId && row.budgetId"
                      flat
                      dense
                      round
                      size="sm"
                      icon="delete"
                      color="grey-6"
                      :loading="deletingTxId === row.txId"
                      @click="confirmDelete(row.txId, row.budgetId, row.merchant, row.amount, 'spend')"
                    >
                      <q-tooltip>Delete this transaction</q-tooltip>
                    </q-btn>
                  </div>
                </div>
              </q-item-section>
            </q-item>
            <q-item v-if="spendRows.length === 0">
              <q-item-label>No goal spend.</q-item-label>
            </q-item>
          </q-list>
        </q-card>
      </q-tab-panel>
    </q-tab-panels>

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
          ({{ pendingDelete ? formatCurrency(pendingDelete.amount) : '' }})?
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
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useQuasar } from 'quasar';
import { formatCurrency, formatDate, formatDateMonthYYYY } from '../../utils/helpers';
import { useGoals } from '../../composables/useGoals';
import { useFamilyStore } from '../../store/family';
import { dataAccess } from '../../dataAccess';
import type { Goal } from '../../types';

const props = defineProps<{ goal: Goal }>();
defineEmits<{ (e: 'close'): void }>();

const tab = ref('contribs');
const $q = useQuasar();
const familyStore = useFamilyStore();

const { listContributions, listGoalSpends, loadGoalDetails, loadGoals, getGoal } = useGoals();

const contribRows = computed(() => listContributions(props.goal.id));
const spendRows = computed(() => listGoalSpends(props.goal.id));

// Pull the live goal from the cache so rollups update after a delete.
const liveGoal = computed(() => getGoal(props.goal.id) || props.goal);
// `saved` and `withdrawn` are the gross deposit/withdrawal sums maintained
// by GoalService. `available` is the current net balance (deposits minus
// withdrawals) — this is what the user is asking "how much is in this goal".
// Progress is measured against the net balance, so spending from a goal
// rolls the progress bar back, which matches user mental model.
const saved = computed(() => liveGoal.value.savedToDate || 0);
const withdrawn = computed(() => liveGoal.value.spentToDate || 0);
const available = computed(() => Math.max(saved.value - withdrawn.value, 0));
const target = computed(() => liveGoal.value.totalTarget || 0);
const progress = computed(() => {
  if (!target.value) return available.value > 0 ? 1 : 0;
  return Math.min(available.value / target.value, 1);
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
    // Refresh goal details list and goal roll-ups (savedToDate / spentToDate)
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

onMounted(() => {
  console.log('GoalDetailsPanel opened for goal', props.goal.id);
});
</script>

<style scoped>
.category-title {
  margin: 0;
}
.progress-section {
  margin-bottom: 8px;
}
.progress-label {
  font-size: 14px;
  margin-bottom: 4px;
}
</style>
