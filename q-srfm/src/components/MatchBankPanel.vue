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
  for (let i = 0; i < entries.length; i++) {
    const [id, budget] = entries[i];
    progressMsg.value = `Loading budget ${i + 1} of ${entries.length}`;
    let full = budget;
    if (!full.transactions || full.transactions.length === 0) {
      full = await dataAccess.getBudget(id);
      if (full) {
        budgetStore.updateBudget(id, full);
      } else {
        continue;
      }
    }
    (full.transactions || []).filter((t) => !t.deleted && (!t.status || t.status === 'U')).forEach((t) => txs.push(t));
    (full.categories || []).forEach((c) => cats.add(c.name));
  }

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
