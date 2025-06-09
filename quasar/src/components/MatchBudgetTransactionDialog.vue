<!-- src/components/MatchBudgetTransactionDialog.vue -->
<template>
  <q-dialog v-model="localShowDialog" :maximized="isMobile" @update:modelValue="handleDialogClose">
    <q-card>
      <q-card-section class="text-h6">Match Budget Transaction</q-card-section>
      <q-card-section>
        <q-row>
          <q-col>
            <h3>Selected Budget Transaction</h3>
            <q-table
              :rows="[selectedBudgetTransaction]"
              :columns="budgetTransactionColumns"
              row-key="id"
              hide-bottom
              flat
              bordered
            >
              <template v-slot:body-cell-categories="props">
                <q-td :props="props">
                  {{ formatCategories(props.row?.categories) }}
                </q-td>
              </template>
              <template v-slot:body-cell-amount="props">
                <q-td :props="props"> ${{ toDollars(toCents(props.row?.amount || 0)) }} </q-td>
              </template>
            </q-table>
          </q-col>
        </q-row>
        <q-row class="q-mt-md">
          <q-col>
            <h3>Select Bank Transaction to Match</h3>
            <q-row class="q-mb-sm">
              <q-col cols="12" md="4">
                <q-input v-model="searchAmount" label="Amount" type="number" outlined dense />
              </q-col>
              <q-col cols="12" md="4">
                <q-input v-model="searchMerchant" label="Merchant" outlined dense />
              </q-col>
              <q-col cols="12" md="4">
                <q-input
                  v-model="searchDateRange"
                  label="Date Range (days)"
                  type="number"
                  outlined
                  dense
                />
              </q-col>
            </q-row>
            <q-table
              :rows="filteredImportedTransactions"
              :columns="importedTransactionHeaders"
              row-key="id"
              :rows-per-page="10"
              selection="single"
              v-model:selected="selectedImportedTransaction"
              @row-click="selectImportedTransaction"
              flat
              bordered
            >
              <template v-slot:body-cell-creditAmount="props">
                <q-td :props="props">
                  {{
                    props.row.creditAmount ? `$${toDollars(toCents(props.row.creditAmount))}` : ''
                  }}
                </q-td>
              </template>
              <template v-slot:body-cell-debitAmount="props">
                <q-td :props="props">
                  {{ props.row.debitAmount ? `$${toDollars(toCents(props.row.debitAmount))}` : '' }}
                </q-td>
              </template>
              <template v-slot:body-cell-accountId="props">
                <q-td :props="props">
                  {{ getAccountName(props.row.accountId) }}
                </q-td>
              </template>
            </q-table>
          </q-col>
        </q-row>
      </q-card-section>
      <q-card-actions align="right">
        <q-space />
        <q-btn color="negative" @click="localShowDialog = false" label="Cancel" />
        <q-btn
          color="primary"
          @click="matchTransaction"
          :disable="!selectedImportedTransaction.length"
          label="Match"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import type { Transaction, ImportedTransaction, Account } from '../types';
import { toDollars, toCents } from '../utils/helpers';

const props = defineProps<{
  showDialog: boolean;
  selectedBudgetTransaction: Transaction | null;
  unmatchedImportedTransactions: ImportedTransaction[];
  availableAccounts: Account[];
}>();

const emit = defineEmits<{
  (e: 'update:showDialog', value: boolean): void;
  (e: 'match-transaction', importedTx: ImportedTransaction): void;
}>();

// Local state
const localShowDialog = ref(props.showDialog);
const selectedImportedTransaction = ref<ImportedTransaction[]>([]);
const searchAmount = ref<string>('');
const searchMerchant = ref<string>('');
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

const budgetTransactionColumns = [
  { name: 'date', label: 'Date', field: 'date' },
  { name: 'merchant', label: 'Merchant', field: 'merchant' },
  { name: 'amount', label: 'Amount', field: 'amount' },
  { name: 'categories', label: 'Categories', field: 'categories' },
];

const importedTransactionHeaders = [
  { name: 'postedDate', label: 'Posted Date', field: 'postedDate' },
  { name: 'payee', label: 'Payee', field: 'payee' },
  { name: 'creditAmount', label: 'Credit Amount', field: 'creditAmount' },
  { name: 'debitAmount', label: 'Debit Amount', field: 'debitAmount' },
  { name: 'accountId', label: 'Account', field: 'accountId' },
  { name: 'accountSource', label: 'Account Source', field: 'accountSource' },
  { name: 'accountNumber', label: 'Account #', field: 'accountNumber' },
  { name: 'checkNumber', label: 'Check Number', field: 'checkNumber' },
];

// Sync local dialog state with prop
watch(
  () => props.showDialog,
  (newVal) => {
    localShowDialog.value = newVal;
    if (newVal) {
      searchDateRange.value = 7;
      searchAmount.value = props.selectedBudgetTransaction
        ? props.selectedBudgetTransaction.amount.toString()
        : '';
      searchMerchant.value = '';
    }
    selectedImportedTransaction.value = [];
  },
);

watch(
  () => props.selectedBudgetTransaction,
  () => {
    searchDateRange.value = 7;
    searchAmount.value = props.selectedBudgetTransaction
      ? props.selectedBudgetTransaction.amount.toString()
      : '';
    searchMerchant.value = '';
  },
);

function handleDialogClose(value: boolean) {
  emit('update:showDialog', value);
}

function selectImportedTransaction(event: any, row: any) {
  selectedImportedTransaction.value = [row.row];
}

function matchTransaction() {
  if (selectedImportedTransaction.value.length > 0) {
    const firstSelectedTx = selectedImportedTransaction.value[0];
    if (firstSelectedTx) emit('match-transaction', firstSelectedTx);
  }
}

function getAccountName(accountId: string): string {
  const account = props.availableAccounts.find((a) => a.id === accountId);
  return account ? account.name : 'Unknown Account';
}

function formatCategories(categories: { category: string; amount: number }[] | undefined | null) {
  if (!categories || !Array.isArray(categories)) {
    return 'No categories';
  }
  if (categories.length === 1) {
    return categories[0].category;
  }
  return categories.map((c) => `${c.category} ($${c.amount.toFixed(2)})`).join(',\n');
}
</script>

<style scoped></style>
