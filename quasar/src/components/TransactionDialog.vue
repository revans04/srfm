<!-- components/TransactionDialog.vue -->
<template>
  <q-dialog
    v-model="localShowDialog"
    :maximized="isMobile"
    style="max-width: 600px"
    @update:modelValue="handleDialogClose"
  >
    <q-card>
      <q-card-section class="text-h6">
        {{ editMode ? 'Edit Transaction' : 'Add Transaction' }}
      </q-card-section>
      <q-card-section>
        <TransactionForm
          :initial-transaction="transaction"
          :loading="loading"
          :show-cancel="true"
          :category-options="categoryOptions"
          :budget-id="budgetId"
          :user-id="userId"
          @save="handleSave"
          @cancel="handleCancel"
        />
      </q-card-section>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import TransactionForm from './TransactionForm.vue';
import type { Transaction } from '../types';

const props = defineProps<{
  showDialog: boolean;
  initialTransaction: Transaction;
  editMode: boolean;
  loading: boolean;
  categoryOptions: string[];
  budgetId: string;
  userId: string;
}>();

const emit = defineEmits<{
  (e: 'update:showDialog', value: boolean): void;
  (e: 'save', transaction: Transaction): void;
  (e: 'cancel'): void;
}>();

// Local state to sync with prop
const localShowDialog = ref(props.showDialog);
const transaction = ref<Transaction>({ ...props.initialTransaction });

// Sync local dialog state with prop
watch(
  () => props.showDialog,
  (newVal) => {
    localShowDialog.value = newVal;
  },
);

watch(
  () => props.initialTransaction,
  (newVal) => {
    transaction.value = { ...newVal };
  },
  { deep: true },
);

// Computed prop for mobile check
const isMobile = computed(() => window.innerWidth < 960);

function handleDialogClose(value: boolean) {
  emit('update:showDialog', value);
  if (!value) handleCancel();
}

function handleSave(updatedTransaction: Transaction) {
  emit('save', updatedTransaction);
}

function handleCancel() {
  emit('cancel');
}
</script>

<style scoped></style>
