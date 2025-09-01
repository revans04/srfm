<template>
  <MatchBankTransactionsDialog
    v-if="ready"
    :remaining-imported-transactions="remainingImportedTransactions"
    :selected-bank-transaction="selectedBankTransaction"
    :transactions="transactions"
    :budget-id="budgetId"
    :matching="matching"
    :category-options="categoryOptions"
    :user-id="userId"
    @update:matching="matching = $event"
    @transactions-updated="loadData"
  />
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import MatchBankTransactionsDialog from './MatchBankTransactionsDialog.vue';
import { dataAccess } from '../dataAccess';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { useAuthStore } from '../store/auth';
import { useUIStore } from '../store/ui';
import type { ImportedTransaction, Transaction } from '../types';

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();
const authStore = useAuthStore();
const uiStore = useUIStore();

const remainingImportedTransactions = ref<ImportedTransaction[]>([]);
const selectedBankTransaction = ref<ImportedTransaction | null>(null);
const transactions = ref<Transaction[]>([]);
const matching = ref(false);
const categoryOptions = ref<string[]>(['Income']);
const ready = ref(false);

const userId = computed(() => authStore.user?.uid || '');
const budgetId = computed(() => uiStore.selectedBudgetIds[0] || '');

async function loadData() {
  const user = authStore.user;
  if (!user) return;

  await familyStore.loadFamily(user.uid);
  await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);

  const txs: Transaction[] = [];
  const cats = new Set<string>();

  for (const [id, budget] of budgetStore.budgets.entries()) {
    let full = budget;
    if (!full.transactions || full.transactions.length === 0) {
      full = await dataAccess.getBudget(id);
      if (full) {
        budgetStore.updateBudget(id, full);
      } else {
        continue;
      }
    }
    (full.transactions || [])
      .filter((t) => !t.deleted && (!t.status || t.status === 'U'))
      .forEach((t) => txs.push(t));
    (full.categories || []).forEach((c) => cats.add(c.name));
  }

  transactions.value = txs;
  categoryOptions.value = ['Income', ...Array.from(cats).sort((a, b) => b.localeCompare(a))];

  const imported = await dataAccess.getImportedTransactions();
  remainingImportedTransactions.value = imported.filter((t) => !t.matched && !t.ignored);
  selectedBankTransaction.value = remainingImportedTransactions.value[0] || null;

  ready.value = true;
}

onMounted(loadData);
</script>
