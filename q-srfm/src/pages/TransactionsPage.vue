<!--
README
======
TransactionsPage.vue demonstrates the new Pro Mode ledger UI. It uses
LedgerTable for virtualized infinite scrolling. Filters are reactive but
for demo purposes only filter the locally seeded mock data.

Key props/usage:
- uses `useTransactions` composable which seeds ~1000 rows on first load
- `fetchMore` loads the next page (50 rows) when the table scrolls to end
- `scrollToDate` (via Jump to Date control) scrolls to first row >= date
-->
<template>
  <q-page class="bg-grey-2">
    <!-- Sticky header: title, tabs, global search -->
    <div class="top-bar bg-grey-2 q-px-md q-pt-md">
      <div class="row items-center q-gutter-md">
        <div class="col-auto text-h5">Transactions</div>
        <q-tabs v-model="tab" dense class="col">
          <q-tab name="budget" label="Budget Transactions" />
          <q-tab name="register" label="Register" />
          <q-tab name="match" label="Match Bank Transactions" />
        </q-tabs>
        <q-input
          v-model="globalSearch"
          placeholder="Search"
          dense
          outlined
          debounce="300"
          class="col-3"
        >
          <template #append>
            <q-icon name="search" />
          </template>
        </q-input>
      </div>
    </div>

    <q-tab-panels v-model="tab" animated>
      <!-- Budget Transactions Tab -->
      <q-tab-panel name="budget">
        <div class="filter-bar shadow-2 bg-white q-pa-sm">
          <div class="column q-gutter-sm">
            <EntitySelector @change="loadBudgets" class="col-auto" />
            <div class="row q-col-gutter-sm items-center">
              <q-select
                v-model="selectedBudgetIds"
                :options="budgetOptions"
                dense
                outlined
                multiple
                use-chips
                emit-value
                map-options
                placeholder="Select Budgets"
                class="col-6"
              />
              <q-btn
                dense
                flat
                label="Clear All"
                class="col-auto"
                @click="clearSelectedBudgets"
              />
            </div>
            <div class="row q-col-gutter-sm">
              <q-input v-model="filters.search" dense outlined placeholder="Search" class="col" />
              <q-input v-model="filters.importedMerchant" dense outlined placeholder="Imported Merchant" class="col-2" />
              <q-input v-model.number="filters.minAmt" type="number" dense outlined placeholder="Amount Min" class="col-2" />
              <q-input v-model.number="filters.maxAmt" type="number" dense outlined placeholder="Amount Max" class="col-2" />
              <q-input v-model="filters.start" dense outlined placeholder="Date" mask="####-##-##" class="col-2">
                <template #append>
                  <q-icon name="event" class="cursor-pointer">
                    <q-popup-proxy transition-show="scale" transition-hide="scale">
                      <q-date v-model="filters.start" mask="YYYY-MM-DD" />
                    </q-popup-proxy>
                  </q-icon>
                </template>
              </q-input>
            </div>
            <div class="row items-center q-gutter-sm">
              <q-btn
                dense
                :color="filters.cleared ? 'primary' : 'grey-5'"
                text-color="white"
                label="Cleared"
                @click="filters.cleared = !filters.cleared"
              />
              <q-btn
                dense
                :color="filters.uncleared ? 'primary' : 'grey-5'"
                text-color="white"
                label="Uncleared"
                @click="filters.uncleared = !filters.uncleared"
              />
              <q-btn
                dense
                :color="filters.reconciled ? 'primary' : 'grey-5'"
                text-color="white"
                label="Reconciled"
                @click="filters.reconciled = !filters.reconciled"
              />
              <q-btn
                dense
                :color="filters.duplicatesOnly ? 'primary' : 'grey-5'"
                text-color="white"
                label="Duplicates"
                @click="filters.duplicatesOnly = !filters.duplicatesOnly"
              />
              <q-space />
              <q-btn dense flat label="Jump to Date" @click="jumpMenu = true" />
              <q-menu v-model="jumpMenu" anchor="bottom right" self="top right">
                <q-date v-model="jumpDate" mask="YYYY-MM-DD" @update:model-value="onJump" />
              </q-menu>
            </div>
            <!-- Active filter chips -->
            <div class="q-mt-sm">
              <q-chip
                v-for="(val, key) in activeChips"
                :key="key"
                dense
                removable
                @remove="() => removeChip(key)"
              >{{ key }}<template v-if="val">: {{ val }}</template></q-chip>
            </div>
          </div>
        </div>
        <ledger-table
          :rows="transactions"
          :columns="budgetColumns"
          :fetch-more="fetchMore"
          :loading="loading"
        />
      </q-tab-panel>

      <!-- Register Tab -->
      <q-tab-panel name="register">
        <statement-header class="q-mb-sm" />
        <div class="filter-bar shadow-2 bg-white q-pa-sm">
          <!-- reuse same filters for demo -->
          <div class="row q-col-gutter-sm items-center">
            <q-input v-model="filters.search" dense outlined placeholder="Search" class="col" />
              <q-checkbox v-model="filters.cleared" label="Cleared Only" class="col-auto" />
          </div>
        </div>
        <ledger-table
          :rows="registerRows"
          :columns="registerColumns"
          :fetch-more="fetchMoreRegister"
          :loading="loadingRegister"
        />
      </q-tab-panel>

      <!-- Match Bank Transactions Tab -->
      <q-tab-panel name="match">
        <match-bank-panel />
      </q-tab-panel>
    </q-tab-panels>
  </q-page>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import LedgerTable from 'src/components/LedgerTable.vue';
import StatementHeader from 'src/components/StatementHeader.vue';
import MatchBankPanel from 'src/components/MatchBankPanel.vue';
import EntitySelector from 'src/components/EntitySelector.vue';
import { useTransactions } from 'src/composables/useTransactions';
import { useBudgetStore } from 'src/store/budget';
import { useFamilyStore } from 'src/store/family';
import { useUIStore } from 'src/store/ui';
import { useAuthStore } from 'src/store/auth';
import { sortBudgetsByMonthDesc } from 'src/utils/budget';

const tab = ref<'budget' | 'register' | 'match'>('budget');
const globalSearch = ref('');

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();
const uiStore = useUIStore();
const auth = useAuthStore();

const { selectedBudgetIds } = storeToRefs(uiStore);
const { selectedEntityId } = storeToRefs(familyStore);

const {
  transactions,
  filters,
  registerRows,
  fetchMore,
  fetchMoreRegister,
  loading,
  loadingRegister,
  scrollToDate,
  budgetColumns,
  registerColumns,
} = useTransactions();

const formatLongMonth = (month: string) => {
  const [year, monthNum] = month.split('-');
  const date = new Date(parseInt(year), parseInt(monthNum) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
};

const budgetOptions = computed(() =>
  sortBudgetsByMonthDesc(
    Array.from(budgetStore.budgets.values()).filter(
      (b) => !selectedEntityId.value || b.entityId === selectedEntityId.value,
    ),
  ).map((b) => ({ label: formatLongMonth(b.month), value: b.budgetId || '' })),
);

function setCurrentBudgetSelection() {
  const budgetsArr = Array.from(budgetStore.budgets.values());
  if (budgetsArr.length === 0) {
    selectedBudgetIds.value = [];
    return;
  }
  const currentMonth = new Date().toISOString().slice(0, 7);
  const sorted = sortBudgetsByMonthDesc(budgetsArr);
  const current = sorted.find((b) => b.month === currentMonth) || sorted[0];
  selectedBudgetIds.value = current?.budgetId ? [current.budgetId] : [];
}

async function loadBudgets() {
  const user = auth.user;
  if (!user) return;
  await budgetStore.loadBudgets(user.uid, selectedEntityId.value);
  if (selectedBudgetIds.value.length === 0) {
    setCurrentBudgetSelection();
  }
}

watch(
  () => budgetStore.budgets.size,
  (size) => {
    if (size > 0 && selectedBudgetIds.value.length === 0) {
      setCurrentBudgetSelection();
    }
  },
  { immediate: true },
);

function clearSelectedBudgets() {
  selectedBudgetIds.value = [];
}

// Filters come from useTransactions

function removeFilter(key: keyof typeof filters.value) {
  const current = filters.value[key];
  (filters.value as Record<string, unknown>)[key as string] =
    typeof current === 'boolean' ? false : null;
}

const activeChips = computed<Record<string, unknown>>(() => {
  const res: Record<string, unknown> = {};
  Object.entries(filters.value).forEach(([k, v]) => {
    if (v !== null && v !== '' && v !== false) {
      res[k] = typeof v === 'boolean' ? '' : v;
    }
  });
  return res;
});

function removeChip(key: string) {
  removeFilter(key as keyof typeof filters.value);
}

// Jump to date
const jumpDate = ref('');
const jumpMenu = ref(false);
function onJump(val: string) {
  jumpMenu.value = false;
  if (val) scrollToDate(val);
}

</script>

<style scoped>
.top-bar {
  position: sticky;
  top: 0;
  z-index: 10;
}
.filter-bar {
  position: sticky;
  top: 56px;
  z-index: 5;
}
</style>
