<template>
  <q-drawer v-model="model" side="right" overlay>
    <q-tabs v-model="tab" dense class="bg-grey-2">
      <q-tab name="overview" label="Overview" />
      <q-tab name="contribs" label="Contributions" />
      <q-tab name="spend" label="Goal Spend" />
    </q-tabs>
    <q-tab-panels v-model="tab" animated>
      <q-tab-panel name="overview">
        <div class="q-pa-md">
          <div class="text-h6">{{ goal?.name }}</div>
          <div>Target: {{ formatCurrency(goal?.totalTarget || 0) }}</div>
          <div>Saved: {{ formatCurrency(goal?.savedToDate || 0) }}</div>
        </div>
      </q-tab-panel>
      <q-tab-panel name="contribs">
        <q-table :rows="contribRows" :columns="contribCols" dense flat />
      </q-tab-panel>
      <q-tab-panel name="spend">
        <q-table :rows="spendRows" :columns="spendCols" dense flat />
      </q-tab-panel>
    </q-tab-panels>
  </q-drawer>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { formatCurrency } from '../../utils/helpers';
import type { Goal } from '../../types';

const props = defineProps<{ modelValue: boolean; goal?: Goal }>();
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void }>();

const model = ref(props.modelValue);
const tab = ref('overview');

const contribRows = ref([]);
const spendRows = ref([]);

watch(
  () => props.modelValue,
  (v) => (model.value = v),
);
watch(model, (v) => emit('update:modelValue', v));

const contribCols = [
  { name: 'month', label: 'Month', field: 'month' },
  { name: 'amount', label: 'Amount', field: 'amount', align: 'right' },
];
const spendCols = [
  { name: 'date', label: 'Date', field: 'txDate' },
  { name: 'amount', label: 'Amount', field: 'amount', align: 'right' },
];
</script>
