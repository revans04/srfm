<!-- src/components/AccountList.vue -->
<template>
  <q-card>
    <q-card-section>
      <div class="row pa-2" >
        <div class="col">{{ type }} Accounts</div>
        <div class="col">
          {{ formatCurrency(accountValue) }}
        </div>
        <div class="col col-auto">
          <q-btn color="primary" variant="plain" @click="$emit('add')">Add Account</q-btn>
        </div>
      </div>
    </q-card-section>
    <q-card-section>
      <q-table
        v-if="accounts.length > 0"
        :headers="headers"
        :rows="accounts"
        class="elevation-1"
        :items-per-page="100"
        :hide-default-footer="true"
      >
        <template v-slot:body-cell-owner="{ row }">
          {{ row.userId ? 'Personal' : 'Shared' }}
        </template>
        <template v-slot:body-cell-balance="{ row }">
          {{ formatCurrency(row.balance || 0) }}
        </template>
        <template v-slot:body-cell-actions="{ row }">
          <q-btn
            density="compact"
            variant="plain"
            color="primary"
            @click="$emit('edit', row)"
            :disabled="row.userId && row.userId !== userId"
          >
              <q-icon name="edit"></q-icon>
          </q-btn>
          <q-btn
            density="compact"
            variant="plain"
            color="error"
            @click="$emit('delete', row.id)"
            :disabled="row.userId && row.userId !== userId"
          >
              <q-icon name="delete"></q-icon>
          </q-btn>
        </template>
      </q-table>

    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, defineProps, defineEmits } from "vue";
import { Account, ImportedTransaction } from "../types";
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

const headers = computed(() => [
  { title: "Name", value: "name" },
  ...(props.type !== "Property" ? [{ title: "Institution", value: "institution" }] : []),
  { title: "Balance", value: "balance" },
  ...(props.type === "Bank" || props.type === "CreditCard"
    ? [{ title: "Account Number", value: "accountNumber" }]
    : []),
  ...(props.type === "Property" ? [{ title: "Address", value: "details.address" }] : []),
  ...(props.type === "Loan" || props.type === "CreditCard"
    ? [{ title: "Interest Rate", value: "details.interestRate" }]
    : []),
  { title: "Actions", value: "actions" },
]);

</script>

<style scoped>

</style>
