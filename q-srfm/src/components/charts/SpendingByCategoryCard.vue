<template>
  <q-card flat bordered class="section-card">
    <q-card-section class="row items-center justify-between">
      <div class="col text-subtitle2">Spending by Category</div>
      <q-btn dense flat icon="refresh" :loading="loading" @click="loadBudget" />
    </q-card-section>
    <q-separator />
    <q-card-section>
      <div v-if="loading" class="row items-center justify-center q-pa-md">
        <q-spinner size="24px" color="primary" />
      </div>
      <div v-else>
        <div v-if="groups.length" style="height: 260px">
          <DoughnutChart :data="chartData" :options="chartOptions" />
        </div>
        <div v-else class="text-body2">No expense data available for this month.</div>
        <div class="q-mt-md compact-list">
          <div v-for="(g, idx) in groups" :key="g.name" class="row items-center q-py-xs">
            <div class="col-auto">
              <span class="legend-dot" :style="{ backgroundColor: colors[idx % colors.length] }" />
            </div>
            <div class="col">{{ g.name }}</div>
            <div class="col-auto text-weight-medium">{{ money(g.actual) }}</div>
          </div>
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useBudgetStore } from '../../store/budget';
import { dataAccess } from '../../dataAccess';
import type { Budget } from '../../types';
import { Doughnut } from 'vue-chartjs';
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import type { TooltipItem } from 'chart.js';

ChartJS.register(ArcElement, Tooltip, Legend);
const DoughnutChart = Doughnut;

const props = defineProps<{ budgetId: string }>();
const budgetStore = useBudgetStore();
const budget = ref<Budget | null>(null);
const loading = ref(false);

const colors = [
  '#1E88E5', // primary
  '#43A047', // secondary
  '#FDD835', // accent
  '#29B6F6', // info
  '#4CAF50', // positive
  '#E53935', // negative
  '#8E24AA', // purple (extra)
  '#FB8C00', // orange (extra)
  '#3949AB', // indigo (extra)
];

function money(n: number) {
  const s = (Math.round(n * 100) / 100).toFixed(2);
  return `$${Number(s).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

async function ensureBudget() {
  const b = budgetStore.getBudget(props.budgetId);
  if (b && b.categories && b.categories.length > 0) {
    budget.value = b;
    return;
  }
  const full = await dataAccess.getBudget(props.budgetId);
  if (full) {
    budget.value = full;
    budgetStore.updateBudget(props.budgetId, full);
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

const groups = computed(() => {
  const b = budget.value;
  if (!b) return [] as { name: string; actual: number }[];
  const categoryToGroup = new Map<string, string>();
  (b.categories || []).forEach((c) => {
    if (c.name.toLowerCase() !== 'income' && (c.group || '').toLowerCase() !== 'income') {
      categoryToGroup.set(c.name, c.group || 'Other');
    }
  });
  const map = new Map<string, number>();
  (b.transactions || []).forEach((t) => {
    if (t.deleted) return;
    (t.categories || []).forEach((split) => {
      const g = categoryToGroup.get(split.category);
      if (!g) return; // skip income
      const sign = t.isIncome ? -1 : 1; // income reduces expenses
      map.set(g, (map.get(g) || 0) + sign * (split.amount || 0));
    });
  });
  return Array.from(map.entries())
    .map(([name, actual]) => ({ name, actual }))
    .sort((a, b) => b.actual - a.actual)
    .slice(0, 6);
});

const chartData = computed(() => ({
  labels: groups.value.map((g) => g.name),
  datasets: [
    {
      data: groups.value.map((g) => Math.max(0, g.actual)),
      backgroundColor: colors.slice(0, groups.value.length),
      borderWidth: 1,
    },
  ],
}));

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '70%',
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (ctx: TooltipItem<'doughnut'>) => {
          const label = ctx.label || '';
          const val = Number(ctx.raw) || 0;
          return `${label}: ${money(val)}`;
        },
      },
    },
  },
} as const;

watch(
  () => props.budgetId,
  (val) => {
    if (val) void loadBudget();
  },
  { immediate: true },
);
</script>

<style scoped>
.section-card { border-radius: 12px; }
.legend-dot {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
}
.compact-list .row { margin-bottom: 2px; }
</style>
