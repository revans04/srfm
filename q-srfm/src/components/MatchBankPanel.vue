<template>
  <div class="relative-position panel-card match-panel" :class="{ 'cursor-wait': !ready }">
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
  let completedBudgets = 0;
  if (totalBudgets > 0) {
    progressMsg.value = `Loading budgets (0 of ${totalBudgets})`;
  }
  const updateProgress = () => {
    completedBudgets += 1;
    if (totalBudgets > 0) {
      const clamped = Math.min(completedBudgets, totalBudgets);
      progressMsg.value = `Loading budgets (${clamped} of ${totalBudgets})`;
    }
  };

  const budgets = await Promise.all(
    entries.map(async ([id, budget]) => {
      if (budget.transactions && budget.transactions.length > 0) {
        updateProgress();
        return budget;
      }

      try {
        const full = await dataAccess.getBudget(id);
        if (full) {
          budgetStore.updateBudget(id, full);
          updateProgress();
          return full;
        }
      } catch (error) {
        console.error(`Error loading budget ${id}`, error);
      }

      updateProgress();
      return budget;
    }),
  );

  budgets.forEach((budget) => {
    if (!budget) return;
    (budget.transactions || [])
      .filter((t) => !t.deleted && (!t.status || t.status === 'U'))
      .forEach((t) => txs.push(t));
    (budget.categories || []).forEach((c) => cats.add(c.name));
  });

  transactions.value = txs;
  categoryOptions.value = ['Income', ...Array.from(cats).sort((a, b) => b.localeCompare(a))];

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
