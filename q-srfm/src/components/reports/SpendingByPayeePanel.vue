<template>
  <div>
    <q-card class="q-mb-lg">
      <q-card-section>
        <div class="text-subtitle1 text-weight-medium">Filters</div>
      </q-card-section>
      <q-card-section class="q-pt-none">
        <div class="row q-col-gutter-md">
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <q-select
              v-model="fromMonth"
              :options="monthOptionsAsc"
              option-label="displayMonth"
              option-value="month"
              emit-value
              map-options
              label="Start month"
              outlined
            />
          </div>
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <q-select
              v-model="toMonth"
              :options="endMonthOptions"
              option-label="displayMonth"
              option-value="month"
              emit-value
              map-options
              label="End month"
              outlined
            />
          </div>
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <q-select
              v-model="excludedGroupIds"
              :options="groupSelectOptions"
              option-label="label"
              option-value="value"
              emit-value
              map-options
              label="Exclude groups"
              multiple
              outlined
              chips
              closable-chips
            />
          </div>
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <q-select
              v-model="excludedCategoryNames"
              :options="categorySelectOptions"
              label="Exclude categories"
              multiple
              outlined
              chips
              closable-chips
            />
          </div>
          <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <q-select
              v-model="excludedMerchants"
              :options="merchantFilteredOptions"
              label="Exclude merchants"
              multiple
              outlined
              chips
              closable-chips
              use-input
              input-debounce="100"
              @filter="filterMerchants"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>

    <q-card>
      <q-card-section>
        <div class="row items-center justify-between q-col-gutter-sm">
          <div class="col-auto">
            <div class="text-subtitle1 text-weight-medium">
              Spending by Payee
              <q-tooltip>
                Net spend per payee across the selected budget months. Refunds reduce the total. Income, transfers,
                and goal-only categories are excluded.
              </q-tooltip>
            </div>
            <div class="text-caption text-grey-7">{{ rangeSummary }}</div>
          </div>
          <div class="col-auto">
            <q-checkbox
              v-model="hideNetIncome"
              label="Hide net-income payees"
              dense
              color="primary"
            >
              <q-tooltip>
                When checked, hides payees whose refunds exceeded their charges (net income). Net-income payees
                are never shown in the chart.
              </q-tooltip>
            </q-checkbox>
          </div>
        </div>
        <div v-if="!loading && displayRows.length" class="text-caption text-grey-7 q-mt-xs">
          <span class="text-weight-medium text-grey-9 tabular-nums">{{ formatCurrency(displayTotal) }}</span>
          across <span class="text-weight-medium text-grey-9">{{ displayRows.length }}</span>
          {{ displayRows.length === 1 ? 'payee' : 'payees' }}
          <template v-if="hiddenNetIncomeCount > 0">
            · <span class="text-weight-medium text-grey-9">{{ hiddenNetIncomeCount }}</span>
            net-income {{ hiddenNetIncomeCount === 1 ? 'payee' : 'payees' }} hidden
          </template>
        </div>
      </q-card-section>

      <q-card-section v-if="loading" class="q-pt-none text-center">
        <q-circular-progress indeterminate color="primary" size="32px" />
      </q-card-section>

      <q-card-section v-else-if="errorMessage" class="q-pt-none">
        <q-banner class="bg-grey-2 text-grey-9">
          <template #avatar>
            <q-icon name="error_outline" color="warning" />
          </template>
          {{ errorMessage }}
          <template #action>
            <q-btn flat color="primary" label="Retry" @click="() => void loadReport()" />
          </template>
        </q-banner>
      </q-card-section>

      <q-card-section v-else-if="!displayRows.length" class="q-pt-none">
        <div class="text-grey-7">
          <template v-if="rows.length && hideNetIncome">
            All payees in this range are net-income (refunds exceeded charges) and are hidden. Uncheck
            "Hide net-income payees" to see them.
          </template>
          <template v-else>
            No spending matches these filters. Try widening the month range or removing exclusions.
          </template>
        </div>
      </q-card-section>

      <q-card-section v-else class="q-pt-none">
        <div class="row q-col-gutter-md">
          <div v-if="chartRows.length" class="col-12 col-md-5">
            <div class="chart-wrap">
              <DoughnutChart :data="chartData" :options="chartOptions" />
            </div>
          </div>
          <div :class="chartRows.length ? 'col-12 col-md-7' : 'col-12'">
            <q-markup-table flat dense separator="horizontal" class="payee-table">
              <thead>
                <tr>
                  <th class="col-payee text-left sortable" @click="setSort('payee')">
                    Payee
                    <q-icon
                      :name="sortIconFor('payee')"
                      size="14px"
                      :class="sortKey === 'payee' ? 'text-primary' : 'text-grey-5'"
                    />
                  </th>
                  <th class="col-share text-right sortable" @click="setSort('share')">
                    <q-icon
                      :name="sortIconFor('share')"
                      size="14px"
                      :class="sortKey === 'share' ? 'text-primary' : 'text-grey-5'"
                    />
                    Share
                  </th>
                  <th class="col-total text-right sortable" @click="setSort('total')">
                    <q-icon
                      :name="sortIconFor('total')"
                      size="14px"
                      :class="sortKey === 'total' ? 'text-primary' : 'text-grey-5'"
                    />
                    Total
                  </th>
                </tr>
              </thead>
              <tbody>
                <template v-for="row in sortedDisplayRows" :key="row.payee">
                  <tr class="payee-row" @click="toggle(row.payee)">
                    <td class="text-left">
                      <q-icon
                        :name="expanded.has(row.payee) ? 'expand_more' : 'chevron_right'"
                        size="18px"
                        class="q-mr-xs text-grey-7"
                      />
                      <span>{{ row.payee }}</span>
                      <span v-if="row.categories.length > 1" class="text-grey-7 q-ml-xs">
                        ({{ row.categories.length }} cats)
                      </span>
                    </td>
                    <td class="text-right tabular-nums text-grey-7">{{ formatShare(row.total) }}</td>
                    <td class="text-right tabular-nums" :class="row.total < 0 ? 'text-positive' : ''">
                      {{ formatCurrency(row.total) }}
                    </td>
                  </tr>
                  <tr v-if="expanded.has(row.payee)" class="payee-detail-row">
                    <td colspan="3" class="q-pa-none">
                      <div class="payee-detail">
                        <div
                          v-for="cat in row.categories"
                          :key="cat.name"
                          class="payee-detail-item row items-center justify-between"
                        >
                          <span class="text-grey-9">{{ cat.name }}</span>
                          <span
                            class="tabular-nums"
                            :class="cat.amount < 0 ? 'text-positive' : 'text-grey-9'"
                          >
                            {{ formatCurrency(cat.amount) }}
                          </span>
                        </div>
                      </div>
                    </td>
                  </tr>
                </template>
              </tbody>
            </q-markup-table>
          </div>
        </div>
      </q-card-section>
    </q-card>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { TooltipItem } from 'chart.js';
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip as ChartTooltip,
  Legend as ChartLegend,
} from 'chart.js';
import { Doughnut } from 'vue-chartjs';
import type { Budget, BudgetGroup, PayeeSpending } from '../../types';
import { dataAccess } from '../../dataAccess';
import { formatCurrency, currentMonthISO } from '../../utils/helpers';
import { isIncomeCategory } from '../../utils/groups';

ChartJS.register(ArcElement, ChartTooltip, ChartLegend);
const DoughnutChart = Doughnut;

const props = defineProps<{
  entityId: string | null;
  budgets: Budget[];
  groupList: BudgetGroup[];
  budgetOptions: Array<{ month: string; displayMonth: string; budgetId?: string }>;
}>();

const fromMonth = ref<string>('');
const toMonth = ref<string>('');
const excludedGroupIds = ref<string[]>([]);
const excludedCategoryNames = ref<string[]>([]);
const excludedMerchants = ref<string[]>([]);
const hideNetIncome = ref(true);
const rows = ref<PayeeSpending[]>([]);
const loading = ref(false);
const errorMessage = ref<string | null>(null);
const expanded = ref(new Set<string>());

type SortKey = 'payee' | 'total' | 'share';
const sortKey = ref<SortKey>('total');
const sortDir = ref<'asc' | 'desc'>('desc');

function setSort(key: SortKey) {
  if (sortKey.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc';
    return;
  }
  sortKey.value = key;
  // Sensible defaults: alphabetical asc for payee, value-desc for amount columns.
  sortDir.value = key === 'payee' ? 'asc' : 'desc';
}

function sortIconFor(key: SortKey): string {
  if (sortKey.value !== key) return 'unfold_more';
  return sortDir.value === 'asc' ? 'arrow_upward' : 'arrow_downward';
}

const monthOptionsAsc = computed(() =>
  [...props.budgetOptions].sort((a, b) => a.month.localeCompare(b.month)),
);

const endMonthOptions = computed(() =>
  monthOptionsAsc.value.filter((b) => !fromMonth.value || b.month >= fromMonth.value),
);

// Group list resolution: each loaded Budget carries a snapshot of the
// entity's group taxonomy (BudgetService.LoadBudgetDetails hydrates it). The
// family store's currentGroups is empty unless someone called loadGroups()
// for the entity, which the Reports page doesn't do — so we union both
// sources, deduped by id, falling back gracefully to whichever is populated.
const resolvedGroups = computed<BudgetGroup[]>(() => {
  const map = new Map<string, BudgetGroup>();
  for (const b of props.budgets) {
    for (const g of b.groups ?? []) map.set(g.id, g);
  }
  for (const g of props.groupList) {
    if (!map.has(g.id)) map.set(g.id, g);
  }
  return Array.from(map.values());
});

const expenseGroups = computed(() =>
  resolvedGroups.value.filter((g) => g.kind !== 'income' && !g.archived),
);

const groupSelectOptions = computed(() =>
  expenseGroups.value
    .map((g) => ({ label: g.name, value: g.id }))
    .sort((a, b) => a.label.localeCompare(b.label)),
);

const categorySelectOptions = computed(() => {
  const excludedSet = new Set(excludedGroupIds.value);
  const seen = new Set<string>();
  const names: string[] = [];
  for (const budget of props.budgets) {
    for (const cat of budget.categories) {
      if (isIncomeCategory(cat, resolvedGroups.value)) continue;
      const groupId = cat.groupId ?? null;
      if (groupId && excludedSet.has(groupId)) continue;
      if (seen.has(cat.name)) continue;
      seen.add(cat.name);
      names.push(cat.name);
    }
  }
  return names.sort((a, b) => a.localeCompare(b));
});

// Merchants come from the union of (a) budgets' merchant cache, (b) payees
// in the current report response, and (c) anything already excluded — so a
// chip the user added doesn't disappear from the dropdown after the next
// fetch removes it from the row set.
const merchantSelectOptions = computed(() => {
  const set = new Set<string>();
  for (const m of excludedMerchants.value) set.add(m);
  for (const b of props.budgets) {
    for (const m of b.merchants ?? []) {
      const trimmed = (m.name ?? '').trim();
      if (trimmed) set.add(trimmed);
    }
  }
  for (const r of rows.value) if (r.payee) set.add(r.payee);
  return Array.from(set).sort((a, b) => a.localeCompare(b));
});

const merchantFilteredOptions = ref<string[]>([]);
function filterMerchants(needle: string, update: (cb: () => void) => void) {
  update(() => {
    const n = (needle || '').toLowerCase();
    merchantFilteredOptions.value = !n
      ? merchantSelectOptions.value
      : merchantSelectOptions.value.filter((m) => m.toLowerCase().includes(n));
  });
}
// Keep the filtered list in sync as the underlying source changes (e.g. new
// rows arrive). Without this, the dropdown stays stuck on the last typed
// filter result until the user retypes.
watch(merchantSelectOptions, (next) => {
  merchantFilteredOptions.value = next;
}, { immediate: true });

// Chart slices represent positive net spend only — pie slices can't be
// negative. Always exclude net-income payees here regardless of the table
// toggle so percentages stay meaningful.
const chartRows = computed(() => rows.value.filter((r) => r.total > 0));

// Table rows respect the user's checkbox: hide negative totals when on,
// otherwise show every payee returned by the backend.
const displayRows = computed(() =>
  hideNetIncome.value ? rows.value.filter((r) => r.total > 0) : rows.value,
);

const sortedDisplayRows = computed(() => {
  const arr = [...displayRows.value];
  const dir = sortDir.value === 'asc' ? 1 : -1;
  arr.sort((a, b) => {
    if (sortKey.value === 'payee') return a.payee.localeCompare(b.payee) * dir;
    // Share is a monotonic function of total — sort numerically by total.
    return (a.total - b.total) * dir;
  });
  return arr;
});

const displayTotal = computed(() => displayRows.value.reduce((sum, r) => sum + r.total, 0));
const chartTotal = computed(() => chartRows.value.reduce((sum, r) => sum + r.total, 0));
const hiddenNetIncomeCount = computed(() =>
  hideNetIncome.value ? rows.value.filter((r) => r.total < 0).length : 0,
);

function formatShare(value: number): string {
  if (chartTotal.value <= 0 || value <= 0) return '—';
  return `${((value / chartTotal.value) * 100).toFixed(1)}%`;
}

const rangeSummary = computed(() => {
  if (!fromMonth.value || !toMonth.value) return 'Select a budget range to view spending';
  if (fromMonth.value === toMonth.value) return findDisplay(fromMonth.value);
  return `${findDisplay(fromMonth.value)} – ${findDisplay(toMonth.value)}`;
});

function findDisplay(month: string): string {
  return props.budgetOptions.find((b) => b.month === month)?.displayMonth ?? month;
}

function toggle(payee: string) {
  if (expanded.value.has(payee)) expanded.value.delete(payee);
  else expanded.value.add(payee);
  expanded.value = new Set(expanded.value);
}

// Top N + "Other" so the donut stays readable when a user has many payees.
const TOP_N = 10;

function resolveTokenColor(token: string, fallback: string): string {
  if (typeof window === 'undefined') return fallback;
  const value = getComputedStyle(document.documentElement).getPropertyValue(`--q-${token}`);
  return value ? value.trim() : fallback;
}

const palette = computed<string[]>(() => [
  resolveTokenColor('primary', '#2563EB'),
  resolveTokenColor('secondary', '#0F766E'),
  resolveTokenColor('positive', '#16A34A'),
  resolveTokenColor('warning', '#E65100'),
  resolveTokenColor('info', '#0284C7'),
  resolveTokenColor('accent', '#6366F1'),
  '#475569', // slate-600
  '#0F172A', // slate-900
  '#64748B', // slate-500
  '#334155', // slate-700
  '#94A3B8', // slate-400 — used for "Other"
]);

const chartData = computed(() => {
  const top = chartRows.value.slice(0, TOP_N);
  const remainder = chartRows.value.slice(TOP_N);
  const labels = top.map((r) => r.payee);
  const data = top.map((r) => r.total);
  if (remainder.length) {
    labels.push(`Other (${remainder.length})`);
    data.push(remainder.reduce((s, r) => s + r.total, 0));
  }
  return {
    labels,
    datasets: [
      {
        data,
        backgroundColor: palette.value.slice(0, labels.length),
        borderWidth: 1,
      },
    ],
  };
});

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '70%',
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (ctx: TooltipItem<'doughnut'>) => {
          const label = ctx.label || '';
          const value = Number(ctx.raw) || 0;
          const total = chartTotal.value;
          const pct = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0';
          return `${label}: ${formatCurrency(value)} (${pct}%)`;
        },
      },
    },
  },
}));

function pickDefaultRange() {
  if (!props.budgetOptions.length) return;
  const currentMonth = currentMonthISO();
  const yearPrefix = `${currentMonth.slice(0, 4)}-`;
  const ytd = props.budgetOptions
    .filter((b) => b.month.startsWith(yearPrefix) && b.month <= currentMonth)
    .sort((a, b) => a.month.localeCompare(b.month));
  if (ytd.length) {
    fromMonth.value = ytd[0].month;
    toMonth.value = ytd[ytd.length - 1].month;
    return;
  }
  const latest = monthOptionsAsc.value[monthOptionsAsc.value.length - 1];
  if (latest) {
    fromMonth.value = latest.month;
    toMonth.value = latest.month;
  }
}

watch(categorySelectOptions, (visible) => {
  const visibleSet = new Set(visible);
  excludedCategoryNames.value = excludedCategoryNames.value.filter((n) => visibleSet.has(n));
});

watch(fromMonth, (val) => {
  if (val && toMonth.value && val > toMonth.value) toMonth.value = val;
});

let pendingFetchToken = 0;

async function loadReport() {
  if (!props.entityId || !fromMonth.value || !toMonth.value) {
    rows.value = [];
    return;
  }
  const token = ++pendingFetchToken;
  loading.value = true;
  errorMessage.value = null;
  try {
    const [from, to] =
      fromMonth.value <= toMonth.value
        ? [fromMonth.value, toMonth.value]
        : [toMonth.value, fromMonth.value];
    const result = await dataAccess.getSpendingByPayee(props.entityId, from, to, {
      excludeGroupIds: excludedGroupIds.value,
      excludeCategoryNames: excludedCategoryNames.value,
      excludeMerchants: excludedMerchants.value,
    });
    if (token !== pendingFetchToken) return;
    rows.value = result;
  } catch (err) {
    if (token !== pendingFetchToken) return;
    console.error('Failed to load by-payee report:', err);
    errorMessage.value = 'Could not load spending by payee. Please try again.';
    rows.value = [];
  } finally {
    if (token === pendingFetchToken) loading.value = false;
  }
}

watch(
  () => props.entityId,
  () => {
    if (props.entityId) void loadReport();
  },
);

watch(
  () => props.budgetOptions.length,
  (len, prev) => {
    if (!fromMonth.value && len > 0 && len !== prev) {
      pickDefaultRange();
      void loadReport();
    }
  },
  { immediate: true },
);

watch(
  [fromMonth, toMonth, excludedGroupIds, excludedCategoryNames, excludedMerchants],
  () => {
    if (props.entityId && fromMonth.value && toMonth.value) void loadReport();
  },
  { deep: true },
);
</script>

<style scoped>
.tabular-nums {
  font-variant-numeric: tabular-nums;
}

.chart-wrap {
  max-width: 300px;
  height: 260px;
  margin: 0 auto;
}

.payee-table th,
.payee-table td {
  vertical-align: top;
}

.payee-table th.sortable {
  cursor: pointer;
  user-select: none;
}

.payee-table th.sortable:hover {
  color: var(--q-primary, #2563eb);
}

.col-payee {
  width: auto;
}

.col-share {
  width: 1%;
  white-space: nowrap;
}

.col-total {
  width: 1%;
  white-space: nowrap;
}

.payee-row {
  cursor: pointer;
}

.payee-row:hover {
  background: var(--q-grey-1, #f8fafc);
}

.payee-detail-row td {
  border-top: none;
}

.payee-detail {
  padding: 8px 16px 12px 40px;
  background: var(--q-grey-1, #f8fafc);
}

.payee-detail-item {
  padding: 4px 0;
}
</style>
