<template>
  <q-card>
    <q-card-section>
      <div class="row items-center justify-between">
        <div>
          <div class="text-subtitle1 text-weight-medium">
            Transfers
            <q-tooltip>Money moved between categories or funds. Doesn't change overall income or spending.</q-tooltip>
          </div>
          <div class="text-caption text-grey-7">Inter-category and goal-funded movements across selected budgets</div>
        </div>
        <q-toggle
          v-if="transfers.length"
          v-model="goalFundedOnly"
          label="Goal-funded only"
          dense
          color="primary"
          class="q-ml-sm"
        />
      </div>
    </q-card-section>

    <q-card-section v-if="filteredTransfers.length" class="q-pt-none">
      <div class="row q-col-gutter-md text-caption text-grey-7 q-mb-sm">
        <div class="col-auto">
          <span class="text-weight-medium text-grey-9">{{ formatCurrency(totalMoved) }}</span> moved
        </div>
        <div class="col-auto">
          <span class="text-weight-medium text-grey-9">{{ filteredTransfers.length }}</span>
          {{ filteredTransfers.length === 1 ? 'transfer' : 'transfers' }}
        </div>
        <div class="col-auto">
          <span class="text-weight-medium text-grey-9">{{ goalFundedCount }}</span> goal-funded
        </div>
      </div>

      <q-markup-table flat dense separator="horizontal" class="transfers-table">
        <thead>
          <tr>
            <th class="text-left">Date</th>
            <th class="text-left">From</th>
            <th class="text-left">To</th>
            <th class="text-right">Amount</th>
            <th class="text-left">
              Goal
              <q-tooltip>Marked when the source category is linked to a savings goal.</q-tooltip>
            </th>
            <th v-if="!isCompact" class="text-left">Notes</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in filteredTransfers" :key="row.id">
            <td class="text-left">{{ formatDate(row.date) }}</td>
            <td class="text-left">
              <span>{{ row.from }}</span>
              <span v-if="isCompact" class="block text-grey-7">↓ {{ row.to }}</span>
            </td>
            <td v-if="!isCompact" class="text-left">{{ row.to }}</td>
            <td v-else class="text-right" />
            <td class="text-right tabular-nums">{{ formatCurrency(row.amount) }}</td>
            <td class="text-left">
              <q-chip v-if="row.goalName" dense color="primary" text-color="white" :label="row.goalName" class="q-ma-none" />
            </td>
            <td v-if="!isCompact" class="text-left text-grey-7">{{ row.notes || '' }}</td>
          </tr>
        </tbody>
      </q-markup-table>
    </q-card-section>

    <q-card-section v-else class="q-pt-none">
      <div class="text-grey-7">
        <template v-if="goalFundedOnly && transfers.length">
          No goal-funded transfers in the selected period.
        </template>
        <template v-else>
          No transfers in the selected period. Transfers are recorded when you move money between categories or fund an expense from a savings goal.
        </template>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useQuasar } from 'quasar';
import type { Budget, Goal } from '../../types';
import { formatDate, formatCurrency } from '../../utils/helpers';

interface TransferRow {
  id: string;
  date: string;
  from: string;
  to: string;
  amount: number;
  goalName: string | null;
  notes: string;
}

const props = defineProps<{
  budgets: Budget[];
  goals: Goal[];
}>();

const $q = useQuasar();
const goalFundedOnly = ref(false);

const isCompact = computed(() => $q.screen.lt.sm);

const goalCategoryNames = computed(() => new Set(props.goals.map((g) => g.name)));

const transfers = computed<TransferRow[]>(() => {
  const rows: TransferRow[] = [];
  for (const budget of props.budgets) {
    for (const tx of budget.transactions) {
      if (tx.deleted) continue;
      if (tx.transactionType !== 'transfer') continue;

      // A transfer is two signed splits: source (negative) and destination (positive).
      const splits = tx.categories || [];
      const source = splits.find((s) => (s.amount || 0) < 0);
      const dest = splits.find((s) => (s.amount || 0) > 0);
      if (!source || !dest) continue;

      const goalName = goalCategoryNames.value.has(source.category) ? source.category : null;
      rows.push({
        id: tx.id,
        date: tx.date,
        from: source.category,
        to: dest.category,
        amount: Math.abs(source.amount || 0),
        goalName,
        notes: tx.notes || '',
      });
    }
  }
  rows.sort((a, b) => b.date.localeCompare(a.date));
  return rows;
});

const filteredTransfers = computed(() => {
  if (!goalFundedOnly.value) return transfers.value;
  return transfers.value.filter((r) => r.goalName !== null);
});

const totalMoved = computed(() => filteredTransfers.value.reduce((sum, r) => sum + r.amount, 0));
const goalFundedCount = computed(() => filteredTransfers.value.filter((r) => r.goalName !== null).length);
</script>

<style scoped>
.transfers-table th,
.transfers-table td {
  vertical-align: top;
}

.tabular-nums {
  font-variant-numeric: tabular-nums;
}

.block {
  display: block;
}
</style>
