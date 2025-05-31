<!-- src/components/AccountList.vue -->
<template>
  <v-card>
    <v-card-title>
      <v-row class="pa-2">
        <v-col>{{ type }} Accounts</v-col>
        <v-col>
          {{ formatCurrency(accountValue) }}
        </v-col>
        <v-col cols="auto">
          <v-btn color="primary" variant="plain" @click="$emit('add')">Add Account</v-btn>
        </v-col>
      </v-row>
    </v-card-title>
    <v-card-text>
      <v-data-table
        v-if="accounts.length > 0"
        :headers="headers"
        :items="accounts"
        class="elevation-1"
        :items-per-page="100"
        :hide-default-footer="true"
      >
        <template v-slot:item.owner="{ item }">
          {{ item.userId ? 'Personal' : 'Shared' }}
        </template>
        <template v-slot:item.balance="{ item }">
          {{ formatCurrency(item.balance || 0) }}
        </template>
        <template v-slot:item.actions="{ item }">
          <v-btn
            icon
            density="compact"
            variant="plain"
            color="primary"
            @click="$emit('edit', item)"
            :disabled="item.userId && item.userId !== userId"
          >
            <v-icon>mdi-pencil</v-icon>
          </v-btn>
          <v-btn
            icon
            density="compact"
            variant="plain"
            color="error"
            @click="$emit('delete', item.id)"
            :disabled="item.userId && item.userId !== userId"
          >
            <v-icon>mdi-trash-can-outline</v-icon>
          </v-btn>
        </template>
      </v-data-table>

    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed, defineProps, defineEmits } from "vue";
import { Account, ImportedTransaction } from "../types";
import { formatCurrency } from "../utils/helpers";
import { auth } from "../firebase";

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