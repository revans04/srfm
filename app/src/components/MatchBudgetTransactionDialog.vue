<!-- src/components/MatchBudgetTransactionDialog.vue -->
<template>
  <v-dialog v-model="localShowDialog" :fullscreen="isMobile" @update:modelValue="handleDialogClose">
    <v-card>
      <v-card-title>Match Budget Transaction</v-card-title>
      <v-card-text>
        <v-row>
          <v-col>
            <h3>Selected Budget Transaction</h3>
            <v-table>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Merchant</th>
                  <th>Amount</th>
                  <th>Categories</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>{{ selectedBudgetTransaction?.date }}</td>
                  <td>{{ selectedBudgetTransaction?.merchant }}</td>
                  <td>${{ toDollars(toCents(selectedBudgetTransaction?.amount || 0)) }}</td>
                  <td>
                    {{ formatCategories(selectedBudgetTransaction?.categories) }}
                  </td>
                </tr>
              </tbody>
            </v-table>
          </v-col>
        </v-row>
        <v-row class="mt-4">
          <v-col>
            <h3>Select Bank Transaction to Match</h3>
            <v-row class="mb-2">
              <v-col cols="12" md="4">
                <v-text-field v-model="searchAmount" label="Amount" type="number" variant="outlined"></v-text-field>
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field v-model="searchMerchant" label="Merchant" variant="outlined"></v-text-field>
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field v-model="searchDateRange" label="Date Range (days)" type="number" variant="outlined"></v-text-field>
              </v-col>
            </v-row>
            <v-data-table
              :headers="importedTransactionHeaders"
              :items="filteredImportedTransactions"
              :items-per-page="10"
              v-model="selectedImportedTransaction"
              show-select
              single-select
              @click:row="selectImportedTransaction"
            >
              <template v-slot:item.creditAmount="{ item }">
                {{ item.creditAmount ? `$${toDollars(toCents(item.creditAmount))}` : "" }}
              </template>
              <template v-slot:item.debitAmount="{ item }">
                {{ item.debitAmount ? `$${toDollars(toCents(item.debitAmount))}` : "" }}
              </template>
              <template v-slot:item.accountId="{ item }">
                {{ getAccountName(item.accountId) }}
              </template>
            </v-data-table>
          </v-col>
        </v-row>
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn color="error" @click="cancel">Cancel</v-btn>
        <v-btn color="primary" @click="matchTransaction" :disabled="!selectedImportedTransaction.length"> Match </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from "vue";
import { Transaction, ImportedTransaction, Account } from "../types";
import { toDollars, toCents } from "../utils/helpers";

const props = defineProps<{
  showDialog: boolean;
  selectedBudgetTransaction: Transaction | null;
  unmatchedImportedTransactions: ImportedTransaction[];
  availableAccounts: Account[];
}>();

const emit = defineEmits<{
  (e: "update:showDialog", value: boolean): void;
  (e: "match-transaction", importedTx: ImportedTransaction): void;
}>();

// Local state
const localShowDialog = ref(props.showDialog);
const selectedImportedTransaction = ref<string[]>([]);
const searchAmount = ref<string>("");
const searchMerchant = ref<string>("");
const searchDateRange = ref<number>(4);

const isMobile = computed(() => window.innerWidth < 960);

const filteredImportedTransactions = computed(() => {
  let results = [...props.unmatchedImportedTransactions];

  if (props.selectedBudgetTransaction) {
    const range = Number(searchDateRange.value) || 7;
    const budgetDate = new Date(props.selectedBudgetTransaction.date);
    const startDate = new Date(budgetDate);
    startDate.setDate(budgetDate.getDate() - range);
    const endDate = new Date(budgetDate);
    endDate.setDate(budgetDate.getDate() + range);

    results = results.filter((tx) => {
      const txDate = new Date(tx.postedDate);
      return txDate >= startDate && txDate <= endDate;
    });
  }

  const merchant = searchMerchant.value.toLowerCase();
  if (merchant) {
    results = results.filter((tx) => tx.payee.toLowerCase().includes(merchant));
  }

  const amount = parseFloat(searchAmount.value);
  if (!isNaN(amount)) {
    results = results.filter((tx) => {
      const txAmount = tx.debitAmount ?? tx.creditAmount ?? 0;
      return Math.abs(txAmount - amount) < 0.01;
    });
  }

  return results;
});

const importedTransactionHeaders = [
  { title: "Posted Date", value: "postedDate" },
  { title: "Payee", value: "payee" },
  { title: "Credit Amount", value: "creditAmount" },
  { title: "Debit Amount", value: "debitAmount" },
  { title: "Account", value: "accountId" }, // Display the account name
  { title: "Account Source", value: "accountSource" },
  { title: "Account #", value: "accountNumber" },
  { title: "Check Number", value: "checkNumber" },
];

// Sync local dialog state with prop
watch(
  () => props.showDialog,
  (newVal) => {
    localShowDialog.value = newVal;
    if (newVal) {
      searchDateRange.value = 7;
      searchAmount.value = props.selectedBudgetTransaction ? props.selectedBudgetTransaction.amount.toString() : "";
      searchMerchant.value = "";
    }
    selectedImportedTransaction.value = [];
  }
);

watch(
  () => props.selectedBudgetTransaction,
  () => {
    searchDateRange.value = 7;
    searchAmount.value = props.selectedBudgetTransaction ? props.selectedBudgetTransaction.amount.toString() : "";
    searchMerchant.value = "";
  }
);

function handleDialogClose(value: boolean) {
  emit("update:showDialog", value);
}

function cancel() {
  localShowDialog.value = false;
  handleDialogClose(false);
}

function selectImportedTransaction(event: any, item: any) {
  selectedImportedTransaction.value = [item.item.id];
}

function matchTransaction() {
  if (selectedImportedTransaction.value.length > 0) {
    const firstSelectedTx = props.unmatchedImportedTransactions.find((t) => t.id === selectedImportedTransaction.value[0]);
    if (firstSelectedTx) emit("match-transaction", firstSelectedTx);
  }
}

function getAccountName(accountId: string): string {
  const account = props.availableAccounts.find((a) => a.id === accountId);
  return account ? account.name : "Unknown Account";
}

function formatCategories(categories: { category: string; amount: number }[] | undefined | null) {
  if (!categories || !Array.isArray(categories)) {
    return "No categories";
  }
  if (categories.length === 1) {
    return categories[0].category;
  }
  return categories.map((c) => `${c.category} ($${c.amount.toFixed(2)})`).join(",\n");
}
</script>

<style scoped></style>
