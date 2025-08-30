<template>
  <q-table
    :rows="rows"
    :columns="columns"
    row-key="id"
    flat
    dense
    virtual-scroll
    :virtual-scroll-item-size="rowHeight"
    :loading="loading"
    class="ledger-table"
    @virtual-scroll="onVirtualScroll"
  >
    <!-- Amount cell formatting -->
    <template #body-cell-amount="{ value, props }">
      <q-td :props="props" class="text-right">
        <span :class="value < 0 ? 'text-negative' : ''">{{ formatCurrency(value) }}</span>
      </q-td>
    </template>

    <!-- Status badge -->
    <template #body-cell-status="{ row, props }">
      <q-td :props="props">
        <q-badge
          v-if="row.status === 'C'"
          color="positive"
          text-color="white"
          dense
          aria-label="Cleared"
        >C</q-badge>
        <q-badge
          v-else-if="row.status === 'U'"
          color="warning"
          text-color="white"
          dense
          aria-label="Unmatched"
        >U</q-badge>
        <q-icon
          v-if="row.isDuplicate"
          name="warning"
          color="warning"
          size="16px"
          class="q-ml-xs"
          aria-label="Duplicate"
        />
      </q-td>
    </template>

    <!-- Default slot passthrough for additional customization -->
    <slot />
  </q-table>
</template>

<script setup lang="ts">
import type { QTableProps } from 'quasar';
import { formatCurrency } from 'src/utils/helpers';
import { onBeforeUnmount } from 'vue';

interface LedgerTableProps {
  rows: Array<Record<string, unknown>>;
  columns: QTableProps['columns'];
  loading?: boolean;
  fetchMore?: () => Promise<void> | void;
  rowHeight?: number;
}

const props = withDefaults(defineProps<LedgerTableProps>(), {
  loading: false,
  rowHeight: 44,
});

function onVirtualScroll(info: { index: number; from: number; to: number }) {
  if (props.fetchMore && !props.loading && info.to >= props.rows.length - 1) {
    void props.fetchMore();
  }
}

onBeforeUnmount(() => {
  // allow parent to clean up if necessary
});
</script>

<style scoped>
.ledger-table thead th {
  position: sticky;
  top: 0;
  background: white;
  z-index: 1;
}
.ledger-table tbody tr:nth-child(even) {
  background-color: #fafafa;
}
.ledger-table tbody tr.duplicate {
  background-color: #FFF4E5;
}
</style>
