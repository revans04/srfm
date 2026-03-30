<!-- components/TransactionDialog.vue -->
<template>
  <q-dialog v-model="localShowDialog" :width="!isMobile ? '550px' : undefined" :fullscreen="isMobile" @update:modelValue="handleDialogClose">
    <q-card class="transaction-dialog">
      <q-card-section class="transaction-dialog__header row items-start justify-between no-wrap">
        <div class="transaction-dialog__title-group">
          <div class="text-h6 transaction-dialog__title">{{ dialogTitle }}</div>
          <div v-if="helperText" class="text-caption text-grey-6">{{ helperText }}</div>
        </div>
        <q-btn flat round dense icon="close" size="sm" color="grey-6" @click="handleCancel" />
      </q-card-section>

      <q-card-section class="transaction-dialog__body">
        <TransactionForm
          ref="transactionForm"
          :initial-transaction="transaction"
          :category-options="categoryOptions"
          :budget-id="budgetId"
          :user-id="userId"
          @save="handleSave"
          @cancel="handleCancel"
          @update-transactions="handleTransactionsUpdated"
        />
      </q-card-section>

      <q-card-section class="transaction-dialog__actions">
        <div class="row items-center justify-between">
          <div class="row items-center q-gutter-sm">
            <q-btn flat color="primary" label="Cancel" :disable="isBusy" @click="handleCancel" />
            <q-btn
              v-if="canDelete"
              outline
              color="negative"
              label="Delete"
              icon="delete_outline"
              :loading="isBusy"
              @click="showDeleteConfirm = true"
            />
          </div>
          <q-btn unelevated color="primary" label="Save Transaction" :loading="isBusy" @click="handleSaveClick" />
        </div>
      </q-card-section>
    </q-card>
  </q-dialog>

  <q-dialog v-model="showDeleteConfirm">
    <q-card>
      <q-card-section class="text-h6">Delete Transaction</q-card-section>
      <q-card-section>
        Are you sure you want to delete the transaction for
        <strong>{{ transaction.merchant || 'this merchant' }}</strong>?
        This cannot be undone.
      </q-card-section>
      <q-card-actions align="right">
        <q-btn flat label="Cancel" @click="showDeleteConfirm = false" />
        <q-btn color="negative" label="Delete Transaction" @click="confirmDelete" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { useQuasar } from 'quasar';
import TransactionForm from './TransactionForm.vue';
import type { Transaction } from '../types';
const $q = useQuasar();

type TransactionFormHandle = {
  save: () => Promise<void>;
  deleteTransaction: () => Promise<void>;
};

const props = defineProps<{
  showDialog: boolean;
  initialTransaction: Transaction;
  editMode?: boolean;
  loading?: boolean;
  categoryOptions: string[];
  budgetId: string;
  userId: string;
  title?: string;
  helperText?: string;
}>();

const emit = defineEmits<{
  (e: 'update:showDialog', value: boolean): void;
  (e: 'save', transaction: Transaction): void;
  (e: 'cancel'): void;
  (e: 'update-transactions', transactions: Transaction[]): void;
}>();

// Local state to sync with props
const localShowDialog = ref(props.showDialog);
const transaction = ref<Transaction>({ ...props.initialTransaction });

const transactionForm = ref<TransactionFormHandle | null>(null);
const showDeleteConfirm = ref(false);
const isMobile = computed(() => $q.screen.lt.md);

watch(
  () => props.showDialog,
  (value) => {
    localShowDialog.value = value;
  },
);

watch(
  () => props.initialTransaction,
  (newTransaction) => {
    transaction.value = { ...newTransaction };
  },
  { deep: true },
);

const dialogTitle = computed(() => {
  if (props.title) {
    return props.title;
  }
  if (props.editMode) {
    if (transaction.value.transactionType === 'transfer') return 'Edit Transfer';
    return `Edit ${transaction.value.merchant || 'Transaction'}`;
  }
  return 'Add Transaction';
});

const canDelete = computed(() => Boolean(transaction.value?.id));
const isBusy = computed(() => Boolean(props.loading));

function handleDialogClose(value: boolean) {
  emit('update:showDialog', value);
  if (!value) {
    handleCancel();
  }
}

function handleSave(updatedTransaction: Transaction) {
  emit('save', updatedTransaction);
}

function handleTransactionsUpdated(updatedTransactions: Transaction[]) {
  emit('update-transactions', updatedTransactions);
}

function handleSaveClick() {
  void transactionForm.value?.save();
}

function handleDelete() {
  void transactionForm.value?.deleteTransaction();
}

function confirmDelete() {
  showDeleteConfirm.value = false;
  handleDelete();
}

function handleCancel() {
  emit('cancel');
}
</script>

<style scoped>
.transaction-dialog {
  width: 100%;
}

.transaction-dialog__header {
  border-bottom: 1px solid var(--q-color-grey-3);
  padding-bottom: 16px;
}

.transaction-dialog__title-group {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.transaction-dialog__body {
  max-height: 70vh;
  overflow-y: auto;
  padding-bottom: 0;
}

.transaction-dialog__actions {
  position: sticky;
  bottom: 0;
  background-color: var(--q-color-white);
  border-top: 1px solid var(--q-color-grey-3);
  padding: 12px 16px;
  z-index: 1;
}
</style>
