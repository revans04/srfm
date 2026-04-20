<template>
  <q-dialog v-model="model">
    <q-card style="min-width:280px">
      <q-card-section class="text-h6">Contribute to {{ goal?.name }}</q-card-section>
      <q-card-section>
        <CurrencyInput v-model.number="amount" label="Amount" dense />
        <q-select
          v-model="sourceCategory"
          :options="sourceOptions"
          label="From"
          dense
          outlined
          stack-label
          class="q-mt-sm"
          :hint="'Category this contribution is drawn from'"
        />
        <q-input v-model="note" label="Note" dense class="q-mt-sm" />
      </q-card-section>
      <q-card-actions align="right">
        <q-btn flat label="Cancel" v-close-popup />
        <q-btn color="primary" label="Save" :disable="!canSave" @click="onSave" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import CurrencyInput from '../CurrencyInput.vue';
import type { Goal } from '../../types';
import { useFamilyStore } from '../../store/family';
import { useBudgetStore } from '../../store/budget';
import { isIncomeCategory } from '../../utils/groups';

const props = defineProps<{
  modelValue: boolean;
  goal?: Goal;
  categoryOptions?: string[];
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'save', payload: { amount: number; note?: string; sourceCategory: string }): void;
}>();

const familyStore = useFamilyStore();
const budgetStore = useBudgetStore();

const model = ref(props.modelValue);
const amount = ref<number>(0);
const note = ref('');
const sourceCategory = ref<string>('');

// Default source = the first category (alpha) in any income-kind group on the
// entity. Falls back to whatever's in categoryOptions if no income-kind
// category can be located.
const defaultSourceName = computed<string>(() => {
  const groupList = familyStore.currentGroups;
  for (const b of budgetStore.budgets.values()) {
    if (props.goal?.entityId && b.entityId !== props.goal.entityId) continue;
    for (const cat of b.categories) {
      if (isIncomeCategory(cat, groupList) && cat.name !== props.goal?.name) {
        return cat.name;
      }
    }
  }
  return props.categoryOptions?.find((c) => c !== props.goal?.name) ?? '';
});

const sourceOptions = computed(() => {
  const opts = props.categoryOptions && props.categoryOptions.length > 0 ? [...props.categoryOptions] : [];
  const goalName = props.goal?.name;
  const filtered = goalName ? opts.filter((c) => c !== goalName) : opts;
  if (defaultSourceName.value && !filtered.includes(defaultSourceName.value)) {
    filtered.unshift(defaultSourceName.value);
  }
  return filtered;
});

const canSave = computed(() => amount.value > 0 && !!sourceCategory.value);

watch(
  () => props.modelValue,
  (v) => (model.value = v),
);
watch(model, (v) => emit('update:modelValue', v));

watch(
  () => props.goal,
  (g) => {
    amount.value = g?.monthlyTarget || 0;
    note.value = '';
  },
  { immediate: true },
);

// Default the source whenever the dialog opens.
watch(model, (v) => {
  if (v) {
    sourceCategory.value = defaultSourceName.value || sourceOptions.value[0] || '';
  }
});

function onSave() {
  if (!canSave.value) return;
  emit('save', { amount: amount.value, note: note.value || undefined, sourceCategory: sourceCategory.value });
  model.value = false;
}
</script>
