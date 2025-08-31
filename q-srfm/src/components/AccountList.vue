<!-- src/components/AccountList.vue -->
<template>
  <q-card>
    <q-card-section>
      <div class="row items-center pa-2">
        <div class="col">{{ type }} Accounts</div>
        <div class="col-auto text-right q-mr-md">{{ formatCurrency(accountValue) }}</div>
        <div class="col-auto">
          <q-btn color="primary" flat dense @click="$emit('add')" icon="add">Add Account</q-btn>
        </div>
      </div>
    </q-card-section>
    <q-card-section>
      <q-table
        v-if="accounts.length > 0"
        :columns="columns"
        :rows="accounts"
        class="elevation-1"
        :pagination="{ rowsPerPage: 100 }"
        hide-bottom
      >
        <template v-slot:body-cell-owner="{ row }">
          {{ row.userId ? 'Personal' : 'Shared' }}
        </template>
        <template v-slot:body-cell-balance="{ row }">
          {{ formatCurrency(row.balance || 0) }}
        </template>
        <template v-slot:body-cell-actions="{ row }">
          <q-btn
            flat
            dense
            color="primary"
            icon="edit"
            @click="$emit('edit', row)"
            :disabled="row.userId && row.userId !== userId"
          />
          <q-btn
            flat
            dense
            color="error"
            icon="delete"
            @click="$emit('delete', row.id)"
            :disabled="row.userId && row.userId !== userId"
          />
        </template>
      </q-table>

    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, defineProps } from "vue";
import type { Account, ImportedTransaction } from "../types";
import { formatCurrency } from "../utils/helpers";
import { auth } from "../firebase/init";

const props = defineProps<{
  accounts: Account[];
  importedTransactions?: ImportedTransaction[];
  type: "Bank" | "CreditCard" | "Investment" | "Property" | "Loan";
}>();

const userId = computed(() => auth.currentUser?.uid || "");

const accountValue = computed(() => {
  let val = 0;
  props.accounts.forEach((a) => {
    val += a.balance ?? 0;
  })
  if (props.type == "CreditCard" || props.type == "Loan") {
    val = val * -1;
  }
  return val;
});

const columns = computed(() => [
  { name: 'name', label: 'Name', field: 'name' },
  ...(props.type !== 'Property' ? [{ name: 'institution', label: 'Institution', field: 'institution' }] : []),
  { name: 'balance', label: 'Balance', field: 'balance' },
  ...(props.type === 'Bank' || props.type === 'CreditCard'
    ? [{ name: 'accountNumber', label: 'Account Number', field: 'accountNumber' }]
    : []),
  ...(props.type === 'Property' ? [{ name: 'address', label: 'Address', field: (row: Account) => row.details?.address }] : []),
  ...(props.type === 'Loan' || props.type === 'CreditCard'
    ? [{ name: 'interestRate', label: 'Interest Rate', field: (row: Account) => row.details?.interestRate }]
    : []),
  { name: 'owner', label: 'Owner', field: 'userId' },
  { name: 'actions', label: 'Actions', field: 'actions' },
]);

</script>

<style scoped>

</style>
