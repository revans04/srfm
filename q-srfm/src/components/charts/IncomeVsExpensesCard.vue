<template>
  <q-card flat bordered class="section-card">
    <q-card-section class="row items-center justify-between q-px-md q-py-sm">
      <div class="text-subtitle2">Income vs. Expenses</div>
      <q-btn dense flat icon="refresh" :loading="loading" @click="loadSeries" color="primary" />
    </q-card-section>
    <q-card-section class="q-pt-xs q-px-md q-pb-md">
      <div v-if="loading" class="row items-center justify-center q-pa-md">
        <q-spinner size="24px" color="primary" />
      </div>
      <div v-else>
        <div v-if="series.labels.length" style="height: 260px">
          <LineChart :data="chartData" :options="options" />
        </div>
        <div v-else class="text-body2 text-grey-7">No monthly data available yet.</div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useBudgetStore } from '../../store/budget';
import type { Budget } from '../../types';
import { Line } from 'vue-chartjs';
import { Chart as ChartJS, LineElement, PointElement, LinearScale, TimeScale, TimeSeriesScale, Tooltip, Legend, Filler } from 'chart.js';
import 'chartjs-adapter-date-fns';

ChartJS.register(LineElement, PointElement, LinearScale, TimeScale, TimeSeriesScale, Tooltip, Legend, Filler);
const LineChart = Line;

function resolveTokenColor(token: string, fallback: string) {
  if (typeof window === 'undefined') return fallback;
  const value = getComputedStyle(document.documentElement).getPropertyValue(`--q-${token}`);
  return value ? value.trim() : fallback;
}

function hexToRgba(value: string, alpha: number) {
  const normalized = value.trim().replace('#', '');
  const hex = normalized.length === 3 ? normalized.split('').map((char) => char + char).join('') : normalized;
  if (!/^[0-9a-f]{6}$/i.test(hex)) {
    return value;
  }
  const numeric = parseInt(hex, 16);
  const r = (numeric >> 16) & 255;
  const g = (numeric >> 8) & 255;
  const b = numeric & 255;
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function withAlpha(color: string, alpha: number) {
  if (color.startsWith('#')) {
    return hexToRgba(color, alpha);
  }
  return color;
}

const incomeColor = resolveTokenColor('primary', '#2563EB');
const expenseColor = resolveTokenColor('negative', '#DC2626');

const props = defineProps<{ entityId?: string }>();
const budgetStore = useBudgetStore();
const loading = ref(false);

const series = ref<{ labels: Date[]; income: number[]; expenses: number[] }>({ labels: [], income: [], expenses: [] });

function monthToDate(m: string) {
  const [y, mm] = m.split('-').map(Number);
  return new Date(y, (mm || 1) - 1, 1);
}

function loadSeries() {
  loading.value = true;
  try {
    // Use whatever budgets are in the store (Dashboard already loads them)
    const budgets: Budget[] = Array.from(budgetStore.budgets.values())
      .filter((b) => !props.entityId || b.entityId === props.entityId)
      .sort((a, b) => a.month.localeCompare(b.month))
      .slice(-12);

    const labels: Date[] = [];
    const income: number[] = [];
    const expenses: number[] = [];

    for (const b of budgets) {
      labels.push(monthToDate(b.month));
      // income
      const incomeCats = new Set((b.categories || []).filter((c) => (c.group || '').toLowerCase() === 'income' || c.name.toLowerCase() === 'income').map((c) => c.name));
      let incomeSum = 0;
      let expenseSum = 0;
      (b.transactions || []).forEach((t) => {
        if (t.deleted) return;
        (t.categories || []).forEach((split) => {
          if (incomeCats.has(split.category)) incomeSum += split.amount || 0;
          else expenseSum += (t.isIncome ? -1 : 1) * (split.amount || 0);
        });
      });
      income.push(incomeSum);
      expenses.push(Math.max(0, expenseSum));
    }

    series.value = { labels, income, expenses };
  } finally {
    loading.value = false;
  }
}

const chartData = computed(() => ({
  labels: series.value.labels,
  datasets: [
    {
      label: 'Income',
      data: series.value.income,
      borderColor: incomeColor,
      backgroundColor: withAlpha(incomeColor, 0.15),
      fill: true,
      pointRadius: 2,
      tension: 0.3,
    },
    {
      label: 'Expenses',
      data: series.value.expenses,
      borderColor: expenseColor,
      backgroundColor: withAlpha(expenseColor, 0.12),
      fill: true,
      pointRadius: 2,
      tension: 0.3,
    },
  ],
}));

const options = {
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    x: {
      type: 'timeseries' as const,
      time: { unit: 'month' as const },
      ticks: { maxRotation: 0 },
    },
    y: {
      ticks: {
        callback: (v: number) => `$${Number(v).toLocaleString('en-US')}`,
      },
    },
  },
  plugins: {
    legend: { display: true },
  },
} as const;

watch(
  () => budgetStore.budgets.size,
  () => void loadSeries(),
  { immediate: true },
);
</script>

<style scoped>
.section-card {
  border-radius: 12px;
  background-color: #ffffff;
  min-height: 360px;
}
</style>
