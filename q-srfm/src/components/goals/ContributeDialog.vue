<template>
  <q-dialog v-model="model">
    <q-card style="min-width:250px">
      <q-card-section class="text-h6">Contribute to {{ goal?.name }}</q-card-section>
      <q-card-section>
        <CurrencyInput v-model.number="amount" label="Amount" dense />
        <q-input v-model="note" label="Note" dense class="q-mt-sm" />
      </q-card-section>
      <q-card-actions align="right">
        <q-btn flat label="Cancel" v-close-popup />
        <q-btn color="primary" label="Save" @click="onSave" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import CurrencyInput from '../CurrencyInput.vue';
import type { Goal } from '../../types';

const props = defineProps<{ modelValue: boolean; goal?: Goal }>();
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void; (e: 'save', amount: number, note?: string): void }>();

const model = ref(props.modelValue);
const amount = ref<number>(0);
const note = ref('');

watch(
  () => props.modelValue,
  (v) => (model.value = v),
);
watch(model, (v) => emit('update:modelValue', v));

watch(
  () => props.goal,
  (g) => {
    amount.value = g?.monthlyTarget || 0;
  },
  { immediate: true },
);

function onSave() {
  emit('save', amount.value, note.value);
  model.value = false;
}
</script>
