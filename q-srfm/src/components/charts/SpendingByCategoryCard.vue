<template>
  <q-card flat bordered class="section-card">
    <q-card-section class="row items-center justify-between q-px-md q-py-sm">
      <div class="text-subtitle2">Spending by Category</div>
      <q-btn dense flat icon="refresh" :loading="loading" @click="loadBudget" color="primary" />
    </q-card-section>
    <q-card-section class="q-pt-xs q-px-md q-pb-md">
      <div v-if="loading" class="row items-center justify-center q-pa-md">
        <q-spinner size="24px" color="primary" />
      </div>
      <div v-else>
        <div v-if="groups.length" style="height: 260px">
          <DoughnutChart :data="chartData" :options="chartOptions" />
        </div>
        <div v-else class="text-body2 text-grey-7">No expense data available for this month.</div>
        <div class="q-mt-md compact-list">
          <div
            v-for="(g, idx) in groups"
            :key="g.name"
            class="row items-center q-py-xs row-clickable"
            @click="openCategoryDialog(g.name)"
          >
            <div class="col-auto">
              <span class="legend-dot" :style="{ backgroundColor: colors[idx % colors.length] }" />
            </div>
            <div class="col text-body1 text-grey-9">{{ formatGroupName(g.name) }}</div>
            <div class="col-auto text-weight-medium text-grey-8">{{ money(g.actual) }}</div>
          </div>
        </div>
      </div>
    </q-card-section>
    <q-dialog v-model="showMerchantDialog" max-width="520px">
      <q-card>
        <q-card-section>
          <div class="text-h6 q-mb-xs">Merchants for {{ selectedCategory }}</div>
          <div class="text-caption text-grey-6">Aggregated from this category's transactions.</div>
        </q-card-section>
        <q-separator />
        <q-card-section class="q-pa-none">
          <q-list dense bordered separator>
            <q-item v-for="merchant in merchantAggregates" :key="merchant.name">
              <q-item-section>
                <div class="text-body1">{{ merchant.name }}</div>
                <div class="text-caption text-grey-6">{{ merchant.count }} txn{{ merchant.count === 1 ? '' : 's' }}</div>
              </q-item-section>
              <q-item-section side class="text-right text-body2">{{ money(merchant.total) }}</q-item-section>
            </q-item>
            <q-item v-if="merchantAggregates.length === 0">
              <q-item-section>
                <div class="text-body2 text-grey-6">No transactions found for this category.</div>
              </q-item-section>
            </q-item>
          </q-list>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right">
          <q-btn flat label="Close" color="primary" @click="showMerchantDialog = false" />
        </q-card-actions>
      </q-card>
    </q-dialog>
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
const showMerchantDialog = ref(false);
const selectedCategory = ref('');
const merchantAggregates = ref<{ name: string; total: number; count: number }[]>([]);

type ColorToken = { token: string; fallback: string };

function resolveTokenColor(token: string, fallback: string) {
  if (typeof window === 'undefined') return fallback;
  const value = getComputedStyle(document.documentElement).getPropertyValue(`--q-${token}`);
  return value ? value.trim() : fallback;
}

const colorTokens: ColorToken[] = [
  { token: 'primary', fallback: '#2563EB' },
  { token: 'secondary', fallback: '#0F766E' },
  { token: 'positive', fallback: '#16A34A' },
  { token: 'warning', fallback: '#F59E0B' },
  { token: 'negative', fallback: '#DC2626' },
  { token: 'info', fallback: '#0284C7' },
];

const colors = [
  ...colorTokens.map((entry) => resolveTokenColor(entry.token, entry.fallback)),
  '#475569',
  '#64748B',
  '#0F172A',
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

function openCategoryDialog(categoryName: string) {
  if (!budget.value) return;
  const categoryToGroup = categoryGrouping.value;
  const aggregates = new Map<string, { total: number; count: number }>();
  (budget.value.transactions || []).forEach((tx) => {
    if (tx.deleted) return;
    (tx.categories || []).forEach((cat) => {
      const groupName = categoryToGroup.get(cat.category);
      if (groupName !== categoryName) return;
      const merchant = tx.merchant?.trim() || 'Unknown merchant';
      const total = aggregates.get(merchant) ?? { total: 0, count: 0 };
      const amount = tx.isIncome ? -1 * (cat.amount || 0) : cat.amount || 0;
      total.total += Math.abs(amount);
      total.count += 1;
      aggregates.set(merchant, total);
    });
  });

  merchantAggregates.value = Array.from(aggregates.entries())
    .map(([name, data]) => ({ name, total: data.total, count: data.count }))
    .sort((a, b) => b.total - a.total);
  selectedCategory.value = categoryName;
  showMerchantDialog.value = true;
}

const categoryGrouping = computed(() => {
  const map = new Map<string, string>();
  const b = budget.value;
  if (!b) return map;
  (b.categories || []).forEach((c) => {
    if (c.name.toLowerCase() !== 'income' && (c.group || '').toLowerCase() !== 'income') {
      map.set(c.name, c.group || 'Other');
    }
  });
  return map;
});

const groups = computed(() => {
  const b = budget.value;
  if (!b) return [] as { name: string; actual: number }[];
  const categoryToGroup = categoryGrouping.value;
  const groupTotals = new Map<string, number>();

  (b.categories || []).forEach((c) => {
    const name = c.name || 'Other';
    const group = categoryToGroup.get(name) || 'Other';
    if (!groupTotals.has(group)) {
      groupTotals.set(group, 0);
    }
  });

  (b.transactions || []).forEach((t) => {
    if (t.deleted) return;
    (t.categories || []).forEach((split) => {
      const group = categoryToGroup.get(split.category) || 'Other';
      const sign = t.isIncome ? -1 : 1;
      const amount = sign * (split.amount || 0);
      groupTotals.set(group, (groupTotals.get(group) || 0) + amount);
    });
  });

  return Array.from(groupTotals.entries())
    .map(([name, actual]) => ({ name, actual }))
    .sort((a, b) => b.actual - a.actual);
});

function formatGroupName(input: string) {
  if (!input) return '';
  const normalized = input.trim().toLowerCase();
  return normalized.charAt(0).toUpperCase() + normalized.slice(1);
}

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
.section-card {
  border-radius: 12px;
  background-color: #ffffff;
  min-height: 360px;
}
.legend-dot {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  margin-right: 10px;
}
.compact-list .row {
  margin-bottom: 2px;
}
.row-clickable {
  cursor: pointer;
  transition: background 0.2s ease;
}
.row-clickable:hover {
  background: rgba(37, 99, 235, 0.05);
}
</style>
