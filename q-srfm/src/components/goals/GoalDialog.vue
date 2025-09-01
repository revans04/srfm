<template>
  <q-dialog v-model="model">
    <q-card style="min-width:300px">
      <q-card-section class="text-h6">{{ goal ? 'Edit Goal' : 'New Goal' }}</q-card-section>
      <q-card-section>
        <q-input v-model="form.name" label="Name" dense />
        <CurrencyInput v-model.number="form.totalTarget" label="Total Target" dense class="q-mt-sm" />
        <CurrencyInput v-model.number="form.monthlyTarget" label="Monthly Target" dense class="q-mt-sm" />
        <q-input v-model="form.targetDate" label="Target Date" type="date" dense class="q-mt-sm" />
        <q-input v-model="form.notes" label="Notes" type="textarea" dense class="q-mt-sm" />
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
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void; (e: 'save', data: Partial<Goal>): void }>();

const model = ref(props.modelValue);
watch(
  () => props.modelValue,
  (v) => (model.value = v),
);
watch(model, (v) => emit('update:modelValue', v));

const form = ref<Partial<Goal>>({});
watch(
  () => props.goal,
  (g) => {
    form.value = g ? { ...g } : {};
  },
  { immediate: true },
);

function onSave() {
  emit('save', form.value);
  model.value = false;
}
</script>
