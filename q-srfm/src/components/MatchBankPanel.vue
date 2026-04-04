<template>
  <div class="relative-position match-panel" :class="{ 'cursor-wait': !ready }">
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
    <q-inner-loading :showing="!ready">
      <div class="column items-center">
        <q-spinner color="primary" />
        <div class="text-subtitle2 q-mt-md">{{ progressMsg }}</div>
      </div>
    </q-inner-loading>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import MatchBankTransactionsDialog from './MatchBankTransactionsDialog.vue';
import { dataAccess } from '../dataAccess';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { useAuthStore } from '../store/auth';
import { useUIStore } from '../store/ui';
import type { Budget, ImportedTransaction, Transaction } from '../types';

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
const progressMsg = ref('Loading...');

const userId = computed(() => authStore.user?.uid || '');
const budgetId = computed(() => uiStore.selectedBudgetIds[0] || '');

async function loadData() {
  ready.value = false;
  const user = authStore.user;
  if (!user) return;

  progressMsg.value = 'Loading budgets...';
  await familyStore.loadFamily(user.uid);
  await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);

  const txs: Transaction[] = [];
  const cats = new Set<string>();

  const entries = Array.from(budgetStore.budgets.entries());
  const totalBudgets = entries.length;
  progressMsg.value = `Loading budgets (0 of ${totalBudgets})`;

  // Separate budgets that need fetching from those already loaded
  const loaded: Budget[] = [];
  const idsToFetch: string[] = [];
  for (const [id, budget] of entries) {
    if (budget.transactions && budget.transactions.length > 0) {
      loaded.push(budget);
    } else {
      idsToFetch.push(id);
    }
  }

  // Batch-fetch all unloaded budgets in one request
  let fetched: Budget[] = [];
  if (idsToFetch.length > 0) {
    try {
      fetched = await dataAccess.getBudgetsBatch(idsToFetch);
      for (const b of fetched) {
        if (b.budgetId) budgetStore.updateBudget(b.budgetId, b);
      }
    } catch (error) {
      console.error('Error batch-loading budgets', error);
    }
  }
  progressMsg.value = `Loading budgets (${totalBudgets} of ${totalBudgets})`;

  const budgets = [...loaded, ...fetched];

  for (const budget of budgets) {
    if (!budget) continue;
    (budget.transactions || [])
      .filter((t) => !t.deleted && (!t.status || t.status === 'U'))
      .forEach((t) => txs.push(t));
    (budget.categories || []).forEach((c) => {
      if (c.name) cats.add(c.name);
    });
  }

  // Also collect categories from store in case full budget data was loaded separately
  for (const [, b] of budgetStore.budgets.entries()) {
    if (b?.categories) {
      b.categories.forEach((c) => {
        if (c.name) cats.add(c.name);
      });
    }
  }

  transactions.value = txs;
  cats.delete('Income');
  categoryOptions.value = ['Income', ...Array.from(cats).sort((a, b) => a.localeCompare(b))];

  progressMsg.value = 'Downloading imported transactions...';
  const imported = await dataAccess.getImportedTransactions();
  progressMsg.value = `Downloading ${imported.length} imported transactions`;
  remainingImportedTransactions.value = imported.filter((t) => !t.matched && !t.ignored);
  selectedBankTransaction.value = remainingImportedTransactions.value[0] || null;

  ready.value = true;
}

onMounted(loadData);
</script>

<style scoped>
.match-panel {
  min-height: 220px;
}
</style>
