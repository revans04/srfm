<template>
  <!-- Mobile card list view -->
  <div v-if="isMobileView" class="ledger-cards">
    <div v-if="loading" class="text-center q-pa-lg">
      <q-spinner color="primary" size="40px" />
    </div>
    <q-list v-else-if="props.rows.length > 0" separator>
      <q-item
        v-for="row in props.rows"
        :key="row.id"
        clickable
        @click="emit('row-click', row)"
        class="ledger-card-item"
      >
        <q-item-section>
          <div class="row items-center justify-between q-mb-xs">
            <span class="text-caption text-muted">{{ formatDate(row.date) }}</span>
            <span class="text-weight-bold" :class="{ 'text-negative': row.amount < 0 }">
              {{ money(row.amount) }}
            </span>
          </div>
          <q-item-label class="text-weight-medium">{{ row.payee || 'No payee' }}</q-item-label>
          <div class="row items-center q-gutter-x-sm q-mt-xs">
            <q-badge v-if="row.category" outline color="primary" class="text-caption">{{ row.category }}</q-badge>
            <q-badge v-if="statusMetaMap[row.status]" :color="statusMetaMap[row.status].color" outline class="text-caption">
              {{ statusMetaMap[row.status].label }}
            </q-badge>
            <q-icon v-if="row.isDuplicate" name="warning" color="warning" size="16px" />
          </div>
          <q-item-label v-if="row.notes" caption class="q-mt-xs ellipsis">{{ row.notes }}</q-item-label>
        </q-item-section>
        <q-item-section side v-if="selection">
          <q-checkbox
            :model-value="props.selected?.includes(row.id) ?? false"
            @update:model-value="toggleRowSelection(row)"
            @click.stop
          />
        </q-item-section>
      </q-item>
    </q-list>
    <div v-else class="text-center q-pa-lg text-muted">
      No transactions to display.
    </div>
  </div>

  <!-- Desktop table view -->
  <q-table
    v-else
    :rows="props.rows"
    :columns="visibleColumns"
    row-key="id"
    flat
    bordered
    dense
    v-model:pagination="pagination"
    :rows-per-page-options="[0]"
    :loading="loading"
    :selection="selection"
    v-model:selected="selectedInternal"
    :row-class="rowClassFn"
    @row-click="handleRowClick"
  >
    <template #body-cell-date="slotProps">
      <q-td :props="slotProps" class="ellipsis">
        {{ formatDate(slotProps.row.date) }}
      </q-td>
    </template>

    <template #body-cell-payee="slotProps">
      <q-td :props="slotProps" class="ellipsis">
        {{ slotProps.row.payee }}
      </q-td>
    </template>

    <template #body-cell-category="slotProps">
      <q-td :props="slotProps" class="ellipsis ledger-cell--category">
        {{ slotProps.row.category }}
        <q-tooltip v-if="slotProps.row.category">{{ slotProps.row.category }}</q-tooltip>
      </q-td>
    </template>

    <template #body-cell-entity="slotProps">
      <q-td :props="slotProps" class="ellipsis">
        {{ slotProps.row.entityName }}
      </q-td>
    </template>

    <template #body-cell-amount="slotProps">
      <q-td :props="slotProps" class="text-right" :class="{ 'text-negative': slotProps.row.amount < 0 }">
        {{ money(slotProps.row.amount) }}
      </q-td>
    </template>

    <template #body-cell-status="slotProps">
      <q-td :props="slotProps">
        <q-badge v-if="statusMetaMap[slotProps.row.status]" :color="statusMetaMap[slotProps.row.status].color" outline>
          {{ statusMetaMap[slotProps.row.status].label }}
        </q-badge>
        <span v-else>{{ slotProps.row.status }}</span>
        <q-btn
          v-if="slotProps.row.matched && slotProps.row.status !== 'R'"
          flat
          dense
          round
          size="xs"
          icon="link_off"
          color="negative"
          class="q-ml-xs"
          @click.stop="emit('unmatch', slotProps.row)"
        >
          <q-tooltip>Unmatch from budget transaction</q-tooltip>
        </q-btn>
        <q-icon v-else-if="slotProps.row.linkId" name="link" color="primary" size="16px" class="q-ml-xs" />
        <q-icon v-if="slotProps.row.isDuplicate" name="warning" color="warning" size="16px" class="q-ml-xs" />
      </q-td>
    </template>

    <template #body-cell-notes="slotProps">
      <q-td :props="slotProps" class="col-notes ellipsis">
        <q-tooltip v-if="slotProps.row.notes">{{ slotProps.row.notes }}</q-tooltip>
        <span class="truncate">{{ slotProps.row.notes }}</span>
      </q-td>
    </template>

    <template #body-cell-actions="slotProps">
      <q-td :props="slotProps" class="text-right">
        <slot name="actions" :row="slotProps.row"></slot>
      </q-td>
    </template>

  </q-table>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useQuasar } from 'quasar';
import type { QTableProps } from 'quasar';
import type { Transaction } from '../types';

const $q = useQuasar();
const isMobileView = computed(() => $q.screen.lt.md);

type Align = 'left' | 'right' | 'center';

export type Column<Row = Record<string, unknown>> = {
  name: string;
  label: string;
  field: keyof Row | ((row: Row) => unknown);
  align: Align;
  sortable?: boolean;
  classes?: string;
  headerClasses?: string;
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
  entityLabel?: string;
  selection?: 'single' | 'multiple';
  selected?: string[];
}>();

const emit = defineEmits<{
  (e: 'row-click', row: LedgerRow): void;
  (e: 'update:selected', ids: string[]): void;
  (e: 'unmatch', row: LedgerRow): void;
}>();

function toggleRowSelection(row: LedgerRow) {
  const current = props.selected ?? [];
  const idx = current.indexOf(row.id);
  if (idx >= 0) {
    emit('update:selected', current.filter((id) => id !== row.id));
  } else {
    emit('update:selected', [...current, row.id]);
  }
}

const statusMetaMap: Record<LedgerRow['status'], { label: string; color: string }> = {
  C: { label: 'C', color: 'positive' },
  U: { label: 'U', color: 'warning' },
  R: { label: 'R', color: 'primary' },
  M: { label: 'C', color: 'positive' },
  I: { label: 'I', color: 'secondary' },
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

const baseColumns = computed<Column<LedgerRow>[]>(() => [
  { name: 'date', label: 'Date', field: 'date', align: 'left', sortable: true, classes: 'ellipsis' },
  { name: 'payee', label: 'Payee', field: 'payee', align: 'left', sortable: true, classes: 'ellipsis' },
  { name: 'category', label: 'Category', field: 'category', align: 'left', classes: 'ellipsis' },
  {
    name: 'entity',
    label: props.entityLabel ?? 'Entity/Budget',
    field: 'entityName',
    align: 'left',
    classes: 'ellipsis',
  },
  {
    name: 'amount',
    label: 'Amount',
    field: 'amount',
    align: 'right',
    sortable: true,
    classes: 'text-right',
  },
  { name: 'status', label: 'Status', field: 'status', align: 'center', classes: 'text-center', headerClasses: 'text-center' },
  { name: 'notes', label: 'Notes', field: 'notes', align: 'left', classes: 'ellipsis', headerClasses: 'col-notes' },
  { name: 'actions', label: '', field: 'id', align: 'right', classes: 'text-right', headerClasses: 'text-right' },
]);

const visibleColumns = ref<Column<LedgerRow>[]>([]);
const mqQuery = '(max-width: 768px)';
let mq: MediaQueryList | undefined;

const pagination = ref<QTableProps['pagination']>({
  rowsPerPage: 0,
});

function applyColumnVisibility(matches: boolean) {
  const cols = baseColumns.value;
  visibleColumns.value = matches ? cols.filter((c) => !['category', 'notes'].includes(c.name)) : cols;
}

const onMediaChange = (event: MediaQueryListEvent) => {
  applyColumnVisibility(event.matches);
};

onMounted(() => {
  mq = window.matchMedia(mqQuery);
  applyColumnVisibility(mq.matches);
  mq.addEventListener?.('change', onMediaChange);
});

onBeforeUnmount(() => {
  mq?.removeEventListener?.('change', onMediaChange);
});

watch(baseColumns, () => {
  applyColumnVisibility(mq?.matches ?? false);
});

function money(n: number) {
  return (n < 0 ? '-$' : '$') + Math.abs(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatDate(iso: string) {
  if (!iso) return '';

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

function handleRowClick(_: Event, row: LedgerRow) {
  emit('row-click', row);
}

function rowClassFn(row: LedgerRow, index: number) {
  const classes = ['row-striped', 'cursor-pointer'];
  if (index % 2 === 1) {
    classes.push('row-even');
  }
  if (row.isDuplicate) {
    classes.push('dup-row');
  }
  return classes.join(' ');
}
</script>

<style scoped>
.ledger-card-item {
  min-height: 56px;
  padding: 12px 16px;
}

.row-striped.row-even {
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
:deep(.q-table__middle thead tr th) {
  position: sticky;
  top: 0;
  z-index: 1;
  background: var(--q-color-white, #ffffff);
}
.ledger-cell--category {
  max-width: 150px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
