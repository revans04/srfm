<!-- src/components/AccountList.vue -->
<template>
  <q-card flat bordered class="account-card q-mt-lg">
    <q-card-section class="row items-center justify-between q-gutter-sm">
      <div class="column">
        <div class="text-subtitle1">{{ headerTitle }}</div>
        <div class="text-caption text-grey-7">Total: {{ formatCurrency(accountValue) }}</div>
      </div>
      <q-btn color="primary" icon="add" label="Add Account" dense unelevated @click="$emit('add')" />
    </q-card-section>
    <q-separator spaced />
    <q-table
      flat
      bordered
      dense
      :columns="columns"
      :rows="accounts"
      row-key="id"
      :pagination="pagination"
      :rows-per-page-options="[10, 25, 50, 0]"
      class="account-table"
    >
      <template #body-cell-owner="props">
        <q-td :props="props">
          {{ props.row.userId ? 'Personal' : 'Shared' }}
        </q-td>
      </template>
      <template #body-cell-balance="props">
        <q-td :props="props">
          {{ formatCurrency(props.row.balance || 0) }}
        </q-td>
      </template>
      <template #body-cell-actions="props">
        <q-td :props="props" class="text-right">
          <q-btn
            flat
            dense
            color="primary"
            icon="edit"
            @click="$emit('edit', props.row)"
            :disabled="props.row.userId && props.row.userId !== userId"
          />
          <q-btn
            flat
            dense
            color="negative"
            icon="delete"
            @click="$emit('delete', props.row.id)"
            :disabled="props.row.userId && props.row.userId !== userId"
          />
        </q-td>
      </template>
      <template #no-data>
        <div class="full-width text-center text-grey-6 q-py-xl">No {{ emptyStateLabel }} yet.</div>
      </template>
    </q-table>
  </q-card>
</template>

<script setup lang="ts">
import { computed, defineProps } from 'vue';
import type { Account, ImportedTransaction } from '../types';
import { formatCurrency } from '../utils/helpers';
import { auth } from '../firebase/init';

const props = defineProps<{
  accounts: Account[];
  importedTransactions?: ImportedTransaction[];
  type: 'Bank' | 'CreditCard' | 'Investment' | 'Property' | 'Loan';
}>();

const userId = computed(() => auth.currentUser?.uid || '');

const pagination = { rowsPerPage: 10 };

const accountValue = computed(() => {
  let val = 0;
  props.accounts.forEach((a) => {
    val += a.balance ?? 0;
  });
  if (props.type == 'CreditCard' || props.type == 'Loan') {
    val = val * -1;
  }
  return val;
});

const headerTitle = computed(() => {
  switch (props.type) {
    case 'CreditCard':
      return 'Credit Card Accounts';
    case 'Investment':
      return 'Investment Accounts';
    case 'Property':
      return 'Property Accounts';
    case 'Loan':
      return 'Loan Accounts';
    default:
      return 'Bank Accounts';
  }
});

const emptyStateLabel = computed(() => headerTitle.value.toLowerCase());

const columns = computed(() => [
  { name: 'name', label: 'Name', field: 'name' },
  ...(props.type !== 'Property' ? [{ name: 'institution', label: 'Institution', field: 'institution' }] : []),
  { name: 'balance', label: 'Balance', field: 'balance' },
  ...(props.type === 'Bank' || props.type === 'CreditCard' ? [{ name: 'accountNumber', label: 'Account Number', field: 'accountNumber' }] : []),
  ...(props.type === 'Property' ? [{ name: 'address', label: 'Address', field: (row: Account) => row.details?.address }] : []),
  ...(props.type === 'Loan' || props.type === 'CreditCard'
    ? [{ name: 'interestRate', label: 'Interest Rate', field: (row: Account) => row.details?.interestRate }]
    : []),
  { name: 'owner', label: 'Owner', field: 'userId' },
  { name: 'actions', label: 'Actions', field: 'actions' },
]);
</script>

<style scoped>
.account-card {
  background: #ffffff;
}

.account-table :deep(thead tr) {
  background-color: #f5f5f5;
}

.account-table :deep(.q-table__bottom) {
  border-top: 1px solid #d9d9d9;
}
</style>
