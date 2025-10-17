<template>
  <q-table
    :rows="rows"
    :columns="visibleColumns"
    row-key="id"
    flat
    bordered
    dense
    class="ledger-table"
    :style="{ '--header-offset': `${headerOffset}px` }"
    :virtual-scroll="true"
    :virtual-scroll-item-size="rowHeight"
    :rows-per-page-options="[0]"
    :loading="loading"
    :selection="selection"
    v-model:selected="selectedInternal"
    @virtual-scroll="onVirtualScroll"
  >
    <template #top>
      <slot name="header"></slot>
    </template>

    <template #header="h">
      <q-tr :props="h">
        <q-th v-if="props.selection" auto-width>
          <q-checkbox
            v-model="h.selected"
            :indeterminate="selectedInternal.length > 0 && selectedInternal.length < rows.length"
            @click.stop
          />
        </q-th>
        <q-th v-for="col in h.cols" :key="col.name" :props="h" :class="[`col-${col.name}`]">
          {{ col.label }}
        </q-th>
      </q-tr>
    </template>

    <template #body="b">
      <q-tr :props="b" :class="[{ 'dup-row': b.row.isDuplicate }, 'row-striped', 'cursor-pointer']" @click="onRowClick(b.row)">
        <q-td v-if="props.selection" auto-width>
          <q-checkbox v-model="b.selected" @click.stop />
        </q-td>
        <q-td key="date" :props="b" class="col-date ellipsis">{{ formatDate(b.row.date) }}</q-td>
        <q-td key="payee" :props="b" class="col-payee ellipsis">{{ b.row.payee }}</q-td>
        <q-td key="category" :props="b" class="col-category ellipsis">{{ b.row.category }}</q-td>
        <q-td key="entity" :props="b" class="col-entity ellipsis">{{ b.row.entityName }}</q-td>
        <q-td key="amount" :props="b" class="text-right col-amount" :class="{ 'text-negative': b.row.amount < 0 }">
          {{ money(b.row.amount) }}
        </q-td>
        <q-td key="status" :props="b" class="col-status">
          <q-badge
            v-if="statusMetaMap[b.row.status]"
            :color="statusMetaMap[b.row.status].color"
            outline
          >
            {{ statusMetaMap[b.row.status].label }}
          </q-badge>
          <span v-else>{{ b.row.status }}</span>
          <q-icon v-if="b.row.linkId" name="link" color="primary" size="16px" class="q-ml-xs" />
          <q-icon v-if="b.row.isDuplicate" name="warning" color="warning" size="16px" class="q-ml-xs" />
        </q-td>
        <q-td key="notes" :props="b" class="col-notes ellipsis">
          <q-tooltip v-if="b.row.notes">{{ b.row.notes }}</q-tooltip>
          <span class="truncate">{{ b.row.notes }}</span>
        </q-td>
        <q-td key="actions" :props="b" class="col-actions text-right">
          <slot name="actions" :row="b.row"></slot>
        </q-td>
      </q-tr>
    </template>

    <template #bottom>
      <div class="row justify-center q-pa-sm">
        <q-btn v-if="canLoadMore" flat :loading="loadingMore" label="Load more" @click="onLoadMore" />
      </div>
    </template>
  </q-table>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, watch } from 'vue';
import type { Transaction } from '../types';
type Align = 'left' | 'right' | 'center';
// Generic column typing to avoid any
export type Column<Row = Record<string, unknown>> = {
  name: string;
  label: string;
  field: keyof Row | ((row: Row) => unknown);
  align: Align;
  sortable?: boolean;
};

export interface LedgerRow {
  id: string;
  date: string; // ISO
  payee: string;
  category: string;
  entityName: string;
  budgetId: string;
  amount: number; // dollars
  status: 'C' | 'U' | 'R' | 'M' | 'I';
  importedMerchant?: string;
  isDuplicate?: boolean;
  linkId?: string;
  notes?: string;
  accountId?: string;
  matched?: boolean;
  transaction?: Transaction;
}

const props = defineProps<{
  rows: LedgerRow[];
  loading?: boolean;
  canLoadMore?: boolean;
  loadingMore?: boolean;
  rowHeight?: number;
  headerOffset?: number;
  entityLabel?: string;
  selection?: 'single' | 'multiple';
  selected?: string[];
}>();

const emit = defineEmits<{
  (e: 'load-more'): void;
  (e: 'row-click', row: LedgerRow): void;
  (e: 'update:selected', ids: string[]): void;
}>();

const statusMetaMap: Record<LedgerRow['status'], { label: string; color: string }> = {
  C: { label: 'Cleared', color: 'positive' },
  U: { label: 'Uncleared', color: 'warning' },
  R: { label: 'Reconciled', color: 'primary' },
  M: { label: 'Matched', color: 'accent' },
  I: { label: 'Ignored', color: 'secondary' },
};

const selectedInternal = computed<LedgerRow[] | string[]>({
  get() {
    if (!props.selected) return [];
    return props.rows.filter((r) => props.selected.includes(r.id));
  },
  set(val) {
    if (Array.isArray(val)) {
      emit(
        'update:selected',
        (val as LedgerRow[]).map((r) => (typeof r === 'string' ? r : r.id)),
      );
    } else {
      emit('update:selected', []);
    }
  },
});

const rowHeight = computed(() => props.rowHeight ?? 44);
const headerOffset = computed(() => props.headerOffset ?? 0);

const baseColumns = computed<Column<LedgerRow>[]>(() => [
  { name: 'date', label: 'Date', field: 'date', align: 'left', sortable: true },
  { name: 'payee', label: 'Payee', field: 'payee', align: 'left', sortable: true },
  { name: 'category', label: 'Category', field: 'category', align: 'left' },
  { name: 'entity', label: props.entityLabel ?? 'Entity/Budget', field: 'entityName', align: 'left' },
  { name: 'amount', label: 'Amount', field: 'amount', align: 'right', sortable: true },
  { name: 'status', label: 'Status', field: 'status', align: 'center' },
  { name: 'notes', label: 'Notes', field: 'notes', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
]);

const visibleColumns = ref<Column<LedgerRow>[]>([]);

function updateCols(mq: MediaQueryList) {
  const cols = baseColumns.value;
  visibleColumns.value = mq.matches ? cols.filter((c) => !['category', 'notes'].includes(c.name)) : cols;
}

onMounted(() => {
  const mq = window.matchMedia('(max-width: 768px)');
  const handler = () => updateCols(mq);
  handler();
  mq.addEventListener?.('change', handler);
});

watch(baseColumns, () => {
  const mq = window.matchMedia('(max-width: 768px)');
  updateCols(mq);
});

function money(n: number) {
  return (n < 0 ? '-$' : '$') + Math.abs(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function formatDate(iso: string) {
  if (!iso) return '';

  // Preserve calendar date for YYYY-MM-DD strings by constructing the date in local time
  if (/^\d{4}-\d{2}-\d{2}$/.test(iso)) {
    const [year, month, day] = iso.split('-').map((part) => Number(part));
    const localDate = new Date(year, month - 1, day);
    return localDate.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'numeric',
      day: 'numeric',
    });
  }

  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;

  return d.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'numeric',
    day: 'numeric',
  });
}

function onVirtualScroll({ to }: { to: number }) {
  if (props.canLoadMore && !props.loadingMore && to >= props.rows.length - 1) {
    onLoadMore();
  }
}

function onLoadMore() {
  emit('load-more');
}

function onRowClick(row: LedgerRow) {
  emit('row-click', row);
}
</script>

<style scoped>
.ledger-table thead tr {
  position: sticky;
  top: var(--header-offset);
  z-index: 2;
  background: var(--color-surface-card);
  box-shadow: 0 1px 0 rgba(15, 23, 42, 0.08);
}
.ledger-table :deep(table) {
  table-layout: fixed;
  width: 100%;
}
.ledger-table :deep(th),
.ledger-table :deep(td) {
  vertical-align: middle;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.ledger-table :deep(.col-date) {
  width: 96px;
  max-width: 80px;
}
.ledger-table :deep(.col-payee) {
  width: 18%;
  max-width: 160px;
}
.ledger-table :deep(.col-category) {
  width: 14%;
  max-width: 140px;
}
.ledger-table :deep(.col-entity) {
  width: 14%;
  max-width: 140px;
}
.ledger-table :deep(.col-amount) {
  width: 120px;
  max-width: 120px;
}
.ledger-table :deep(.col-status) {
  width: 96px;
  max-width: 96px;
  text-align: center;
}
.ledger-table :deep(.col-notes) {
  width: 18%;
  max-width: 180px;
}
.ledger-table :deep(.col-actions) {
  width: 64px;
  max-width: 64px;
}
.row-striped:nth-child(even) {
  background: rgba(37, 99, 235, 0.04);
}
.dup-row {
  background: rgba(255, 196, 140, 0.18);
}
.truncate {
  display: inline-block;
  max-width: 320px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
