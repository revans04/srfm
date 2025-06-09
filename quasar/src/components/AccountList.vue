<!-- src/components/AccountList.vue -->
<template>
  <q-card flat bordered>
    <q-card-section>
      <q-row class="q-pa-sm">
        <q-col>{{ type }} Accounts</q-col>
        <q-col>
          {{ formatCurrency(accountValue) }}
        </q-col>
        <q-col cols="auto">
          <q-btn color="primary" flat label="Add Account" @click="$emit('add')" />
        </q-col>
      </q-row>
    </q-card-section>
    <q-card-section>
      <q-table
        v-if="accounts.length > 0"
        :rows="accounts"
        :columns="headers"
        row-key="id"
        :rows-per-page="100"
        hide-bottom
        flat
        bordered
      >
        <template v-slot:body-cell-owner="props">
          <q-td :props="props">
            {{ props.row.userId ? 'Personal' : 'Shared' }}
          </q-td>
        </template>
        <template v-slot:body-cell-balance="props">
          <q-td :props="props">
            {{ formatCurrency(props.row.balance || 0) }}
          </q-td>
        </template>
        <template v-slot:body-cell-actions="props">
          <q-td :props="props">
            <q-btn
              icon="mdi-pencil"
              dense
              flat
              color="primary"
              @click="$emit('edit', props.row)"
              :disable="props.row.userId && props.row.userId !== userId"
            />
            <q-btn
              icon="mdi-trash-can-outline"
              dense
              flat
              color="negative"
              @click="$emit('delete', props.row.id)"
              :disable="props.row.userId && props.row.userId !== userId"
            />
          </q-td>
        </template>
      </q-table>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, defineProps } from 'vue';
import type { Account, ImportedTransaction } from '../types';
import { formatCurrency } from '../utils/helpers';
import { auth } from '../firebase';

const props = defineProps<{
  accounts: Account[];
  importedTransactions?: ImportedTransaction[];
  type: 'Bank' | 'CreditCard' | 'Investment' | 'Property' | 'Loan';
}>();

const userId = computed(() => auth.currentUser?.uid || '');

const accountValue = computed(() => {
  let val = 0;
  props.accounts.forEach((a) => {
    val += a.balance ?? 0;
  });
  if (props.type === 'CreditCard' || props.type === 'Loan') {
    val = val * -1;
  }
  return val;
});

const headers = computed(() => [
  { name: 'name', label: 'Name', field: 'name' },
  ...(props.type !== 'Property'
    ? [{ name: 'institution', label: 'Institution', field: 'institution' }]
    : []),
  { name: 'balance', label: 'Balance', field: 'balance' },
  ...(props.type === 'Bank' || props.type === 'CreditCard'
    ? [{ name: 'accountNumber', label: 'Account Number', field: 'accountNumber' }]
    : []),
  ...(props.type === 'Property'
    ? [
        {
          name: 'address',
          label: 'Address',
          field: (row: Account) => row.details?.address ?? '',
        },
      ]
    : []),
  ...(props.type === 'Loan' || props.type === 'CreditCard'
    ? [
        {
          name: 'interestRate',
          label: 'Interest Rate',
          field: (row: Account) => row.details?.interestRate ?? 0,
        },
      ]
    : []),
  { name: 'actions', label: 'Actions', field: 'actions' },
]);
</script>

<style scoped></style>
