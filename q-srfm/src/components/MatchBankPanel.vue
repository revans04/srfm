<template>
  <div>
    <q-tabs v-model="inner" dense>
      <q-tab name="smart" label="Smart Matches" />
      <q-tab name="remaining" label="Remaining" />
    </q-tabs>
    <q-tab-panels v-model="inner" animated>
      <q-tab-panel name="smart">
        <q-table
          :rows="smartMatches"
          :columns="columns"
          row-key="id"
          dense
          flat
          virtual-scroll
          :virtual-scroll-item-size="44"
          selection="multiple"
          v-model:selected="selected"
        />
      </q-tab-panel>
      <q-tab-panel name="remaining">
        <q-table
          :rows="remaining"
          :columns="columns"
          row-key="id"
          dense
          flat
          virtual-scroll
          :virtual-scroll-item-size="44"
          selection="multiple"
          v-model:selected="selected"
        />
      </q-tab-panel>
    </q-tab-panels>
    <q-footer v-if="selected.length" class="bg-white q-pa-sm shadow-2">
      <div class="row items-center">
        <div class="col">{{ selected.length }} selected</div>
        <q-btn color="primary" label="Confirm" @click="confirm" />
      </div>
    </q-footer>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useImported } from 'src/composables/useImported';
import type { ImportedTransaction } from 'src/composables/useImported';
import type { QTableColumn } from 'quasar';

const inner = ref<'smart' | 'remaining'>('smart');
const selected = ref<ImportedTransaction[]>([]);

const { smartMatches, remaining, confirmMatches } = useImported();

const columns: QTableColumn[] = [
  { name: 'bankDate', label: 'Bank Date', field: 'bankDate', align: 'left' },
  { name: 'bankAmount', label: 'Bank Amount', field: 'bankAmount', align: 'right' },
  { name: 'bankPayee', label: 'Payee', field: 'bankPayee', align: 'left' },
];

function confirm() {
  confirmMatches(selected.value.map((r) => r.id));
  selected.value = [];
}
</script>
