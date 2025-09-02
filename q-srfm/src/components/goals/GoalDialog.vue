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
import type { Timestamp } from 'firebase/firestore';

const props = defineProps<{ modelValue: boolean; goal?: Goal }>();
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void; (e: 'save', data: Partial<Goal>): void }>();

const model = ref(props.modelValue);
watch(
  () => props.modelValue,
  (v) => (model.value = v),
);
watch(model, (v) => emit('update:modelValue', v));

type GoalForm = Omit<Partial<Goal>, 'targetDate'> & { targetDate?: string };
// Initialize numeric fields to avoid undefined being passed to CurrencyInput
const form = ref<GoalForm>({ totalTarget: 0, monthlyTarget: 0 });
function normalizeDate(val: unknown): string | undefined {
  if (!val) return undefined;
  let date: Date;
  if (val instanceof Date) {
    date = val;
  } else if (typeof val === 'string') {
    date = new Date(val);
  } else if (
    typeof val === 'object' &&
    val !== null &&
    'toDate' in val &&
    typeof (val as { toDate: () => Date }).toDate === 'function'
  ) {
    date = (val as { toDate: () => Date }).toDate();
  } else {
    return undefined;
  }
  return isNaN(date.getTime()) ? undefined : date.toISOString().slice(0, 10);
}

watch(
  () => props.goal,
  (g) => {
    form.value = g
      ? {
          ...g,
          totalTarget: g.totalTarget ?? 0,
          monthlyTarget: g.monthlyTarget ?? 0,
          targetDate: normalizeDate((g as { targetDate?: unknown }).targetDate),
        }
      : { totalTarget: 0, monthlyTarget: 0 };
  },
  { immediate: true },
);

function onSave() {
  const { targetDate, ...rest } = form.value;
  const payload: Partial<Goal> = { ...rest };
  if (targetDate) {
    payload.targetDate = new Date(targetDate) as unknown as Timestamp;
  }
  emit('save', payload);
  model.value = false;
}
</script>
