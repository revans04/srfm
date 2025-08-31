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
  >
    <template #top>
      <slot name="header"></slot>
    </template>

    <template #body="props">
      <q-tr :props="props" :class="[{ 'dup-row': props.row.isDuplicate }, 'row-striped']">
        <q-td key="date" :props="props" class="col-date">{{ formatDate(props.row.date) }}</q-td>
        <q-td key="payee" :props="props" class="col-payee">{{ props.row.payee }}</q-td>
        <q-td key="category" :props="props" class="col-category">{{ props.row.category }}</q-td>
        <q-td key="entity" :props="props" class="col-entity">{{ props.row.entityName }}</q-td>
        <q-td key="amount" :props="props" class="text-right col-amount" :class="{ 'text-negative': props.row.amount < 0 }">
          {{ money(props.row.amount) }}
        </q-td>
        <q-td key="status" :props="props" class="col-status">
          <q-badge v-if="props.row.status === 'C'" color="positive" outline> C </q-badge>
          <q-badge v-else-if="props.row.status === 'U'" color="warning" outline> U </q-badge>
          <q-icon v-if="props.row.linkId" name="link" color="primary" size="16px" class="q-ml-xs" />
          <q-icon v-if="props.row.isDuplicate" name="warning" color="warning" size="16px" class="q-ml-xs" />
        </q-td>
        <q-td key="notes" :props="props" class="col-notes">
          <q-tooltip v-if="props.row.notes">{{ props.row.notes }}</q-tooltip>
          <span class="truncate">{{ props.row.notes }}</span>
        </q-td>
        <q-td key="actions" :props="props" class="col-actions text-right">
          <slot name="actions" :row="props.row"></slot>
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
import { computed, ref, onMounted } from 'vue';
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
  status: 'C' | 'U';
  isDuplicate?: boolean;
  linkId?: string;
  notes?: string;
}

const props = defineProps<{
  rows: LedgerRow[];
  loading?: boolean;
  canLoadMore?: boolean;
  loadingMore?: boolean;
  rowHeight?: number;
  headerOffset?: number;
}>();

const emit = defineEmits<{ (e: 'load-more'): void }>();

const rowHeight = computed(() => props.rowHeight ?? 44);
const headerOffset = computed(() => props.headerOffset ?? 0);

const baseColumns: Column<LedgerRow>[] = [
  { name: 'date', label: 'Date', field: 'date', align: 'left', sortable: true },
  { name: 'payee', label: 'Payee', field: 'payee', align: 'left', sortable: true },
  { name: 'category', label: 'Category', field: 'category', align: 'left' },
  { name: 'entity', label: 'Entity/Budget', field: 'entityName', align: 'left' },
  { name: 'amount', label: 'Amount', field: 'amount', align: 'right', sortable: true },
  { name: 'status', label: 'Status', field: 'status', align: 'left' },
  { name: 'notes', label: 'Notes', field: 'notes', align: 'left' },
  { name: 'actions', label: '', field: 'id', align: 'right' },
];

const visibleColumns = ref<Column<LedgerRow>[]>(baseColumns);

function money(n: number) {
  return (n < 0 ? '-$' : '$') + Math.abs(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function formatDate(iso: string) {
  const d = new Date(iso);
  return d.toLocaleDateString('en-US');
}

function onLoadMore() {
  emit('load-more');
}

onMounted(() => {
  // Responsive columns: hide notes on xs
  const mq = window.matchMedia('(max-width: 768px)');
  const updateCols = () => {
    if (mq.matches) {
      visibleColumns.value = baseColumns.filter((c) => !['category', 'notes'].includes(c.name));
    } else {
      visibleColumns.value = baseColumns;
    }
  };
  updateCols();
  mq.addEventListener?.('change', updateCols);
});
</script>

<style scoped>
.ledger-table thead tr {
  position: sticky;
  top: var(--header-offset);
  z-index: 2;
  background: #fff;
}
.row-striped:nth-child(even) {
  background: rgba(0, 0, 0, 0.02);
}
.dup-row {
  background: #fff4e5;
}
.truncate {
  display: inline-block;
  max-width: 320px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.col-notes {
  max-width: 360px;
}
.col-payee {
  max-width: 280px;
}
.col-category {
  max-width: 160px;
}
.col-entity {
  max-width: 220px;
}
.col-amount {
  width: 140px;
}
</style>
