<template>
  <div class="account-reconcile">
    <div class="panel-card sticky top-0 z-20 bg-white shadow-sm q-pa-md">
      <q-form class="column gap-3 md:gap-0" @submit.prevent>
        <div class="row q-col-gutter-md items-end">
          <div class="col-12 col-md-3">
            <q-select
              v-model="selectedAccountId"
              :options="accountOptions"
              label="Account"
              dense
              outlined
              emit-value
              map-options
              use-input
              :hide-dropdown-icon="!accountOptions.length"
              :loading="loadingAccounts"
              @filter="onAccountFilter"
            />
          </div>
          <div class="col-6 col-md-2">
            <q-input v-model="startDate" dense outlined label="Start Date" mask="####-##-##">
              <template #append>
                <q-icon name="event" class="cursor-pointer">
                  <q-popup-proxy transition-show="scale" transition-hide="scale">
                    <q-date v-model="startDate" mask="YYYY-MM-DD" :options="limitEnd" />
                  </q-popup-proxy>
                </q-icon>
              </template>
            </q-input>
          </div>
          <div class="col-6 col-md-2">
            <q-input v-model="endDate" dense outlined label="End Date" mask="####-##-##">
              <template #append>
                <q-icon name="event" class="cursor-pointer">
                  <q-popup-proxy transition-show="scale" transition-hide="scale">
                    <q-date v-model="endDate" mask="YYYY-MM-DD" :options="limitStart" />
                  </q-popup-proxy>
                </q-icon>
              </template>
            </q-input>
          </div>
          <div class="col-6 col-md-2">
            <q-input v-model="beginningBalanceInput" type="number" dense outlined label="Beginning Balance" />
          </div>
          <div class="col-6 col-md-2">
            <q-input v-model="targetEndingInput" type="number" dense outlined label="Target Ending Balance" />
          </div>
          <div class="col-12 col-md-1 q-mt-sm q-mt-md-none flex justify-end">
            <q-btn color="primary" :disable="finalizeDisabled" :loading="finalizing" label="Finalize" @click="finalize" />
          </div>
        </div>
        <div class="row q-col-gutter-md q-mt-md items-center">
          <div class="col-12 col-md-2">
            <div class="text-caption text-grey-7">Matched Total</div>
            <q-badge color="primary" outline class="text-body2 q-mt-xs">{{ formatCurrency(matchedTotal) }}</q-badge>
          </div>
          <div class="col-12 col-md-2">
            <div class="text-caption text-grey-7">Computed Ending</div>
            <q-badge color="primary" outline class="text-body2 q-mt-xs">{{ formatCurrency(computedEnding) }}</q-badge>
          </div>
          <div class="col-12 col-md-2">
            <div class="text-caption text-grey-7">Delta</div>
            <q-badge :color="deltaBadgeColor" outline class="text-body2 q-mt-xs">{{ formatCurrency(delta) }}</q-badge>
          </div>
          <div class="col-12 col-md-4">
            <q-linear-progress :value="progress" color="secondary" track-color="grey-3" rounded class="q-mt-sm" />
          </div>
          <div class="col-12 col-md-2">
            <q-checkbox
              v-model="allVisibleSelected"
              label="Select All Visible"
              dense
              :disable="!selectableVisibleRows.length"
            />
          </div>
        </div>
      </q-form>
    </div>

    <q-table
      class="panel-card q-mt-md"
      flat
      bordered
      dense
      :rows="tableRows"
      :columns="columns"
      row-key="id"
      :loading="loadingRows"
      :rows-per-page-options="[0]"
      :hide-bottom="true"
    >
      <template #body-cell-select="props">
        <q-td auto-width>
          <q-checkbox
            :model-value="selectedIdSet.has(props.row.id)"
            :disable="props.row.status === 'R'"
            dense
            @update:model-value="(val) => toggleRowSelection(props.row.id, val)"
          />
        </q-td>
      </template>
      <template #body-cell-date="props">
        <q-td>{{ formatDisplayDate(props.row.date) }}</q-td>
      </template>
      <template #body-cell-amount="props">
        <q-td class="text-right" :class="{ 'text-negative': props.row.amount < 0 }">{{ formatCurrency(props.row.amount) }}</q-td>
      </template>
      <template #body-cell-status="props">
        <q-td>
          <q-badge v-if="props.row.status === 'C'" color="positive" outline>C</q-badge>
          <q-badge v-else color="primary" outline>R</q-badge>
        </q-td>
      </template>
      <template #no-data>
        <div class="column items-center q-pa-md text-grey-6">
          <q-icon name="inventory_2" size="32px" class="q-mb-sm" />
          <div>No cleared transactions in this window.</div>
        </div>
      </template>
    </q-table>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useQuasar } from 'quasar';
import { dataAccess } from 'src/dataAccess';
import { useFamilyStore } from 'src/store/family';
import type { ImportedTransaction, StatementFinalizePayload } from 'src/types';
import { formatCurrency, getImportedTransactionDate } from 'src/utils/helpers';

interface RegisterRow {
  id: string;
  date: string;
  payee: string;
  amount: number;
  status: 'C' | 'R' | 'U';
}

const $q = useQuasar();
const familyStore = useFamilyStore();

const loadingAccounts = ref(false);
const loadingRows = ref(false);
const finalizing = ref(false);

const rawRows = ref<RegisterRow[]>([]);
const selectedAccountId = ref<string>('');
const accountSearch = ref('');
const startDate = ref('');
const endDate = ref('');
const beginningBalanceInput = ref('0');
const targetEndingInput = ref('0');
const selectedIds = ref<string[]>([]);

const accountOptions = computed(() => {
  const accounts = familyStore.family?.accounts || [];
  const filtered = accountSearch.value
    ? accounts.filter((acct) => acct.name.toLowerCase().includes(accountSearch.value.toLowerCase()))
    : accounts;
  return filtered
    .filter((acct) => ['Bank', 'CreditCard', 'Investment', 'Loan'].includes(acct.type || ''))
    .map((acct) => ({
      label: acct.accountNumber ? `${acct.name} (${acct.accountNumber})` : acct.name,
      value: String(acct.id),
    }))
    .sort((a, b) => a.label.localeCompare(b.label));
});

const limitEnd = (date: string) => {
  if (!endDate.value) return true;
  return date <= endDate.value;
};
const limitStart = (date: string) => {
  if (!startDate.value) return true;
  return date >= startDate.value;
};

const columns = [
  { name: 'select', label: '', field: 'id', align: 'left' as const },
  { name: 'date', label: 'Date', field: 'date', align: 'left' as const },
  { name: 'payee', label: 'Payee', field: 'payee', align: 'left' as const },
  { name: 'amount', label: 'Amount', field: 'amount', align: 'right' as const },
  { name: 'status', label: 'Status', field: 'status', align: 'center' as const },
];

const selectedIdSet = computed(() => new Set(selectedIds.value));

const filteredRows = computed(() => {
  return rawRows.value
    .filter((row) => (row.status === 'C' || row.status === 'R'))
    .filter((row) => {
      if (startDate.value && row.date < startDate.value) return false;
      if (endDate.value && row.date > endDate.value) return false;
      return true;
    })
    .sort((a, b) => b.date.localeCompare(a.date));
});

const selectableVisibleRows = computed(() => filteredRows.value.filter((row) => row.status === 'C'));

const tableRows = computed(() => filteredRows.value);

const selectedRowsDetailed = computed(() => {
  const byId = new Map(rawRows.value.map((row) => [row.id, row] as const));
  return selectedIds.value
    .map((id) => byId.get(id))
    .filter((row): row is RegisterRow => Boolean(row) && row.status === 'C');
});

const matchedTotal = computed(() => selectedRowsDetailed.value.reduce((sum, row) => sum + row.amount, 0));

const beginningBalance = computed(() => parseNumber(beginningBalanceInput.value));
const targetEndingBalance = computed(() => parseNumber(targetEndingInput.value));

const computedEnding = computed(() => beginningBalance.value + matchedTotal.value);
const delta = computed(() => targetEndingBalance.value - computedEnding.value);

const progress = computed(() => {
  const change = targetEndingBalance.value - beginningBalance.value;
  if (!isFinite(change) || change === 0) {
    return Math.abs(delta.value) < 0.005 ? 1 : 0;
  }
  const ratio = 1 - Math.min(1, Math.abs(delta.value) / Math.abs(change));
  return Math.max(0, Math.min(1, Number.isNaN(ratio) ? 0 : ratio));
});

const deltaBadgeColor = computed(() => {
  if (Math.abs(delta.value) < 0.005) return 'positive';
  return delta.value > 0 ? 'warning' : 'negative';
});

const finalizeDisabled = computed(() => {
  if (!selectedAccountId.value || !startDate.value || !endDate.value) return true;
  if (!Number.isFinite(beginningBalance.value) || !Number.isFinite(targetEndingBalance.value)) return true;
  if (!selectedRowsDetailed.value.length) return true;
  return Math.abs(delta.value) >= 0.01;
});

const allVisibleSelected = computed({
  get() {
    if (!selectableVisibleRows.value.length) return false;
    return selectableVisibleRows.value.every((row) => selectedIdSet.value.has(row.id));
  },
  set(value) {
    toggleSelectAllVisible(value);
  },
});

function parseNumber(value: string): number {
  const num = Number(value);
  return Number.isFinite(num) ? num : 0;
}

async function ensureFamilyLoaded() {
  if (familyStore.family) return;
  loadingAccounts.value = true;
  try {
    await familyStore.loadFamily();
  } catch (error) {
    console.error('Failed to load family', error);
    $q.notify({ type: 'negative', message: 'Unable to load family information.' });
  } finally {
    loadingAccounts.value = false;
  }
}

function onAccountFilter(val: string, update: (fn: () => void) => void) {
  update(() => {
    accountSearch.value = val;
  });
}

async function loadTransactions() {
  if (!selectedAccountId.value) {
    rawRows.value = [];
    return;
  }
  loadingRows.value = true;
  try {
    const imported = await dataAccess.getImportedTransactionsByAccountId(selectedAccountId.value, 0, 500);
    rawRows.value = imported.map(mapImportedToRow);
    resetSelectionIfNeeded();
    seedDates();
  } catch (error) {
    console.error('Failed to load account transactions', error);
    $q.notify({ type: 'negative', message: 'Unable to load account transactions.' });
  } finally {
    loadingRows.value = false;
  }
}

function seedDates() {
  if (filteredRows.value.length === 0) return;
  if (!startDate.value) {
    startDate.value = filteredRows.value[filteredRows.value.length - 1].date.slice(0, 10);
  }
  if (!endDate.value) {
    endDate.value = filteredRows.value[0].date.slice(0, 10);
  }
}

function mapImportedToRow(tx: ImportedTransaction): RegisterRow {
  const amount = (tx.creditAmount ?? 0) - (tx.debitAmount ?? 0);
  const isoDate = (getImportedTransactionDate(tx) || '').slice(0, 10);
  return {
    id: tx.id,
    date: isoDate,
    payee: tx.payee,
    amount: Number(amount),
    status: tx.status,
  };
}

function toggleRowSelection(id: string, selected: boolean) {
  if (selected) {
    if (!selectedIdSet.value.has(id)) {
      selectedIds.value = [...selectedIds.value, id];
    }
  } else {
    selectedIds.value = selectedIds.value.filter((storedId) => storedId !== id);
  }
}

function toggleSelectAllVisible(state: boolean) {
  if (state) {
    const ids = selectableVisibleRows.value.map((row) => row.id);
    const merged = new Set([...selectedIds.value, ...ids]);
    selectedIds.value = Array.from(merged);
  } else {
    const visibleSet = new Set(selectableVisibleRows.value.map((row) => row.id));
    selectedIds.value = selectedIds.value.filter((id) => !visibleSet.has(id));
  }
}

function resetSelectionIfNeeded() {
  const validIds = new Set(rawRows.value.filter((row) => row.status === 'C').map((row) => row.id));
  selectedIds.value = selectedIds.value.filter((id) => validIds.has(id));
}

function formatDisplayDate(iso: string): string {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

async function finalize() {
  if (finalizeDisabled.value) return;
  const familyId = familyStore.family?.id;
  if (!familyId) {
    $q.notify({ type: 'negative', message: 'Family context missing.' });
    return;
  }
  finalizing.value = true;
  const payload: StatementFinalizePayload = {
    familyId,
    accountId: selectedAccountId.value,
    startDate: startDate.value,
    endDate: endDate.value,
    beginningBalance: beginningBalance.value,
    endingBalance: targetEndingBalance.value,
    matchedTransactionIds: selectedRowsDetailed.value.map((row) => row.id),
  };
  try {
    await dataAccess.finalizeStatement(payload);
    $q.notify({ type: 'positive', message: 'Statement finalized.' });
    selectedIds.value = [];
  } catch (error) {
    console.error('Failed to finalize statement', error);
    const message = error instanceof Error ? error.message : 'Unable to finalize statement.';
    $q.notify({ type: 'negative', message });
  } finally {
    finalizing.value = false;
    await loadTransactions();
  }
}

watch(selectedAccountId, async (value, oldValue) => {
  if (value && value !== oldValue) {
    startDate.value = '';
    endDate.value = '';
    selectedIds.value = [];
    await loadTransactions();
  }
});

watch([startDate, endDate], () => {
  resetSelectionIfNeeded();
});

watch(accountOptions, (opts) => {
  if (!selectedAccountId.value && opts.length) {
    selectedAccountId.value = opts[0].value;
  }
});

onMounted(async () => {
  await ensureFamilyLoaded();
  if (!selectedAccountId.value && accountOptions.value.length) {
    selectedAccountId.value = accountOptions.value[0].value;
  }
  if (selectedAccountId.value) {
    await loadTransactions();
  }
});
</script>

<style scoped>
.account-reconcile {
  display: flex;
  flex-direction: column;
}
</style>
