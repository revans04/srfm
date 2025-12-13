<template>
  <q-card flat bordered class="dashboard-tile">
    <q-card-section class="row items-center justify-between q-px-md q-py-sm">
      <div class="text-subtitle2 q-mb-none">Budget Progress</div>
      <q-btn dense flat icon="refresh" color="primary" :loading="loading" @click="loadBudget" />
    </q-card-section>
    <q-card-section class="q-pt-xs q-px-md q-pb-md">
      <div v-if="loading" class="row items-center justify-center q-pa-md">
        <q-spinner size="24px" color="primary" />
      </div>
      <div v-else>
        <div class="row items-center">
          <div class="col">
            <div class="text-caption text-grey-6">Planned Expenses</div>
            <div class="text-h5 q-mt-xs">{{ money(plannedExpenses) }}</div>
          </div>
          <div class="col">
            <div class="text-caption text-grey-6">Actual Income</div>
            <div class="text-h5 q-mt-xs">{{ money(actualIncome) }}</div>
          </div>
        </div>
        <div class="row items-center q-mt-sm">
          <div class="col">
            <q-linear-progress :value="progress" color="primary" track-color="grey-3" rounded />
          </div>
          <div class="col-auto text-caption text-grey-7 q-ml-sm">
            {{ (progress * 100).toFixed(0) }}%
          </div>
        </div>
        <div class="row q-mt-sm">
          <div class="col text-body2 text-weight-medium text-grey-8">
            {{ remainingLabel }}
          </div>
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useBudgetStore } from '../../store/budget';
import { dataAccess } from '../../dataAccess';
import type { Budget } from '../../types';

const props = defineProps<{ budgetId: string }>();
const budgetStore = useBudgetStore();
const budget = ref<Budget | undefined>(undefined);
const loading = ref(false);

function money(n: number) {
  const v = Math.round(n * 100) / 100;
  return `$${v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

async function ensureBudget() {
  budget.value = budgetStore.getBudget(props.budgetId);
  if (!budget.value) return;
  // If thin (no categories), hydrate details
  if (!budget.value.categories || budget.value.categories.length === 0) {
    const full = await dataAccess.getBudget(props.budgetId);
    if (full) {
      budget.value = full;
      budgetStore.updateBudget(props.budgetId, full);
    }
  }
}

async function loadBudget() {
  if (!props.budgetId) return;
  loading.value = true;
  try {
    await ensureBudget();
  } finally {
    loading.value = false;
  }
}

const plannedExpenses = computed(() => {
  const b = budget.value;
  if (!b) return 0;
  return (b.categories || [])
    .filter((c) => c.name !== 'Income' && c.group !== 'Income')
    .reduce((sum, c) => sum + (c.target || 0), 0);
});

const actualIncome = computed(() => {
  const b = budget.value;
  if (!b) return 0;
  // Sum category splits that belong to income categories
  const incomeCats = new Set((b.categories || []).filter((c) => c.group === 'Income' || c.name === 'Income').map((c) => c.name));
  let total = 0;
  (b.transactions || []).forEach((t) => {
    if (t.deleted) return;
    (t.categories || []).forEach((split) => {
      if (incomeCats.has(split.category)) total += split.amount;
    });
  });
  return total;
});

const remaining = computed(() => actualIncome.value - plannedExpenses.value);
const progress = computed(() => {
  const p = plannedExpenses.value;
  if (p <= 0) return 0;
  const v = Math.min(Math.max(actualIncome.value / p, 0), 2); // cap at 200%
  return Math.min(v, 1);
});
const remainingLabel = computed(() =>
  `${money(Math.abs(remaining.value))} ${remaining.value >= 0 ? 'left to budget' : 'over budget'}`,
);

onMounted(loadBudget);

watch(
  () => props.budgetId,
  (val, oldVal) => {
    if (val && val !== oldVal) void loadBudget();
  },
  { immediate: true },
);
</script>

<style scoped>
.dashboard-tile {
  min-height: 150px;
  border-radius: 12px;
  background-color: #ffffff;
}
.text-subtitle2 {
  font-weight: 600;
  letter-spacing: 0.3px;
}
</style>
