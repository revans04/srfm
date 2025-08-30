<template>
  <q-card class="q-pa-sm q-mb-md">
    <div class="row q-col-gutter-md items-center">
      <q-select
        class="col-3"
        v-model="account"
        :options="accounts"
        label="Select Account"
        dense
        outlined
        emit-value
        map-options
      />
      <q-input class="col" v-model="range.start" label="Start" type="date" dense outlined />
      <q-input class="col" v-model="range.end" label="End" type="date" dense outlined />
      <q-input class="col" v-model.number="beginBalance" label="Beginning" dense outlined readonly />
      <q-input class="col" v-model.number="endBalance" label="Ending" dense outlined />
    </div>
    <div class="row items-center q-mt-sm">
      <div class="col">
        Matched {{ matched.toLocaleString('en-US', { style: 'currency', currency: 'USD' }) }} /
        {{ total.toLocaleString('en-US', { style: 'currency', currency: 'USD' }) }}
        ({{ progress }}%)
        <q-linear-progress :value="progress / 100" color="positive" class="q-mt-xs" />
      </div>
      <q-btn color="primary" label="Finalize Statement" class="col-auto" @click="finalize" />
    </div>
  </q-card>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';

const account = ref('');
const accounts = ref([
  { label: 'Checking', value: 'checking' },
  { label: 'Savings', value: 'savings' },
]);
const range = ref({ start: '', end: '' });
const beginBalance = ref(1000);
const endBalance = ref(0);
const matched = ref(0);
const total = ref(0);

const progress = computed(() => {
  return total.value === 0 ? 0 : Math.round((matched.value / total.value) * 100);
});

function finalize() {
  // validation placeholder
}
</script>
