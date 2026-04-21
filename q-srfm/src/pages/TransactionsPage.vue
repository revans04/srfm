<template>
  <q-page class="transactions-page">
    <section class="transactions-hero">
      <h1 class="page-title q-mb-sm">Transactions</h1>
      <div class="transactions-hero__tabs-row">
        <button
          v-for="t in tabOptions"
          :key="t.value"
          class="tx-pill"
          :class="{ 'tx-pill--active': tab === t.value }"
          @click="tab = t.value"
        >
          {{ $q.screen.lt.md ? t.shortLabel : t.label }}
        </button>
      </div>
      <div v-show="tab !== 'register' && !$q.screen.lt.md" class="transactions-hero__metrics">
        <div class="tx-metric">
          <div class="tx-metric__label">Total Records</div>
          <div class="tx-metric__value">{{ overviewCounts.total }}</div>
        </div>
        <div class="tx-metric">
          <div class="tx-metric__label">
            Cleared
            <q-tooltip>Transaction has posted to your account</q-tooltip>
          </div>
          <div class="tx-metric__value">{{ overviewCounts.cleared }}</div>
        </div>
        <div class="tx-metric">
          <div class="tx-metric__label">
            Reconciled
            <q-tooltip>Verified against your bank statement</q-tooltip>
          </div>
          <div class="tx-metric__value">{{ overviewCounts.reconciled }}</div>
        </div>
        <div class="tx-metric">
          <div class="tx-metric__label">
            Unmatched
            <q-tooltip>Not yet linked to a budget transaction</q-tooltip>
          </div>
          <div class="tx-metric__value">{{ overviewCounts.unmatched }}</div>
        </div>
      </div>
    </section>

    <q-tab-panels v-model="tab" animated>
      <!-- Budget Register Tab -->
      <q-tab-panel name="budget" class="transactions-panel">
        <GuidedTip tip-id="budget-register">
          Budget transactions are ones you've entered or matched to your budget categories.
        </GuidedTip>
        <div class="transactions-layout">
          <div class="transactions-layout__main">
            <div class="transactions-filters panel-card register-filters">
              <!-- Row 1: entity, search, refresh/clear -->
              <div class="row q-col-gutter-sm items-center">
                <div class="col-12 col-sm-5 col-md-4">
                  <EntitySelector class="full-width" @change="loadBudgets" />
                </div>
                <div class="col col-sm col-md">
                  <q-input v-model="filters.search" dense outlined label="Search">
                    <template #append>
                      <q-btn flat dense round size="sm" icon="refresh" @click="refreshBudget">
                        <q-tooltip>Refresh</q-tooltip>
                      </q-btn>
                      <q-btn flat dense round size="sm" icon="clear_all" @click="clearBudgetFilters">
                        <q-tooltip>Clear All</q-tooltip>
                      </q-btn>
                    </template>
                  </q-input>
                </div>
              </div>

              <!-- Row 2: budget selector -->
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
              />

              <!-- Row 3: secondary filters + chips flowing together -->
              <div class="register-filters__secondary">
                <q-input v-model="filters.importedMerchant" dense outlined label="Merchant" class="register-filters__input register-filters__input--merchant" />
                <q-input v-model="minAmtInput" type="number" dense outlined label="Min $" class="register-filters__input" />
                <q-input v-model="maxAmtInput" type="number" dense outlined label="Max $" class="register-filters__input" />
                <q-input v-model="startInput" dense outlined label="Start" mask="##/##/####" placeholder="mm/dd/yyyy" class="register-filters__input register-filters__input--date">
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy transition-show="scale" transition-hide="scale">
                        <q-date v-model="filters.start" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
                <q-input v-model="endInput" dense outlined label="End" mask="##/##/####" placeholder="mm/dd/yyyy" class="register-filters__input register-filters__input--date">
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy transition-show="scale" transition-hide="scale">
                        <q-date v-model="filters.end" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
                <q-chip clickable class="filter-chip" :color="filters.unmatchedOnly ? 'primary' : 'white'" :text-color="filters.unmatchedOnly ? 'white' : 'primary'" @click="filters.unmatchedOnly = !filters.unmatchedOnly">Unmatched<q-tooltip>Not yet linked to a budget transaction</q-tooltip></q-chip>
                <q-chip clickable class="filter-chip" :color="filters.cleared ? 'primary' : 'white'" :text-color="filters.cleared ? 'white' : 'primary'" @click="filters.cleared = !filters.cleared">Cleared<q-tooltip>Transaction has posted to your account</q-tooltip></q-chip>
                <q-chip clickable class="filter-chip" :color="filters.reconciled ? 'primary' : 'white'" :text-color="filters.reconciled ? 'white' : 'primary'" @click="filters.reconciled = !filters.reconciled">Reconciled<q-tooltip>Verified against your bank statement</q-tooltip></q-chip>
                <q-chip clickable class="filter-chip" :color="filters.duplicatesOnly ? 'primary' : 'white'" :text-color="filters.duplicatesOnly ? 'white' : 'primary'" @click="filters.duplicatesOnly = !filters.duplicatesOnly">Duplicates</q-chip>
              </div>

              <!-- Active filter chips -->
              <div class="transactions-filters__chips" v-if="Object.keys(activeChips).length">
                <q-chip v-for="(val, key) in activeChips" :key="key" dense removable @remove="() => removeChip(key)">
                  {{ key }}<template v-if="val">: {{ val }}</template>
                </q-chip>
              </div>
            </div>

            <!-- Compact selection bar -->
            <div v-if="selectedBudgetRowIds.length" class="register-selection-bar">
              <span class="text-body2 text-weight-medium">
                {{ selectedBudgetRowIds.length }} selected
              </span>
              <div class="row q-gutter-xs no-wrap">
                <q-btn dense flat size="sm" color="negative" label="Mark Deleted" @click="openBudgetDeleteDialog" />
                <q-btn dense flat size="sm" color="primary" label="Clear" @click="clearBudgetSelection" />
              </div>
            </div>
            <div class="transactions-table">
              <ledger-table
                :rows="transactions"
                :loading="loading"
                selection="multiple"
                v-model:selected="selectedBudgetRowIds"
                @row-click="onRowClick"
              />
            </div>
          </div>
        </div>
        <TransactionDialog
          v-if="editTx"
          :show-dialog="showTxDialog"
          :initial-transaction="editTx"
          edit-mode
          :category-options="editCategoryOptions"
          :budget-id="editBudgetId"
          :user-id="auth.user?.uid || ''"
          @update:showDialog="showTxDialog = $event"
          @save="onTransactionSaved"
          @cancel="onTxCancel"
        />
        <q-dialog v-model="showBudgetDeleteDialog">
          <q-card>
            <q-card-section>
              Are you sure you want to mark {{ selectedBudgetRowIds.length }} transaction{{ selectedBudgetRowIds.length > 1 ? 's' : '' }} as deleted?
            </q-card-section>
            <q-card-actions>
              <q-btn flat label="Cancel" color="primary" v-close-popup />
              <q-btn
                flat
                label="Mark as Deleted"
                color="negative"
                :loading="deletingBudgetTransactions"
                @click="deleteSelectedBudgetTransactions"
              />
            </q-card-actions>
          </q-card>
        </q-dialog>
      </q-tab-panel>
      <!-- Account Register Tab -->
      <q-tab-panel name="register" class="transactions-panel">
        <GuidedTip tip-id="account-register">
          These are transactions imported from your bank. Match them to budget entries to reconcile.
        </GuidedTip>
        <div class="transactions-layout">
          <div class="transactions-layout__main">
            <div class="transactions-filters panel-card register-filters">
              <!-- Row 1: account, search, refresh/clear -->
              <div class="row q-col-gutter-sm items-center">
                <div class="col-12 col-sm-5 col-md-4">
                  <q-select v-model="filters.accountId" :options="accountOptions" dense outlined label="Account" clearable emit-value map-options />
                </div>
                <div class="col col-sm col-md">
                  <q-input v-model="filters.search" dense outlined label="Search" debounce="300">
                    <template #append>
                      <q-btn flat dense round size="sm" icon="refresh" @click="refreshRegister">
                        <q-tooltip>Refresh</q-tooltip>
                      </q-btn>
                      <q-btn flat dense round size="sm" icon="clear_all" @click="clearRegisterFilters">
                        <q-tooltip>Clear All</q-tooltip>
                      </q-btn>
                    </template>
                  </q-input>
                </div>
              </div>

              <!-- Row 2: amounts, dates, and status chips in a single flowing row -->
              <div class="register-filters__secondary">
                <q-input v-model="minAmtInput" type="number" dense outlined label="Min $" class="register-filters__input" />
                <q-input v-model="maxAmtInput" type="number" dense outlined label="Max $" class="register-filters__input" />
                <q-input v-model="startInput" dense outlined mask="##/##/####" placeholder="mm/dd/yyyy" label="Start" debounce="300" class="register-filters__input register-filters__input--date">
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy transition-show="scale" transition-hide="scale">
                        <q-date v-model="filters.start" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
                <q-input v-model="endInput" dense outlined mask="##/##/####" placeholder="mm/dd/yyyy" label="End" debounce="300" class="register-filters__input register-filters__input--date">
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy transition-show="scale" transition-hide="scale">
                        <q-date v-model="filters.end" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
                <q-chip clickable class="filter-chip" :color="filters.unmatchedOnly ? 'primary' : 'white'" :text-color="filters.unmatchedOnly ? 'white' : 'primary'" @click="filters.unmatchedOnly = !filters.unmatchedOnly">Unmatched<q-tooltip>Not yet linked to a budget transaction</q-tooltip></q-chip>
                <q-chip clickable class="filter-chip" :color="filters.cleared ? 'primary' : 'white'" :text-color="filters.cleared ? 'white' : 'primary'" @click="filters.cleared = !filters.cleared">Cleared<q-tooltip>Transaction has posted to your account</q-tooltip></q-chip>
                <q-chip clickable class="filter-chip" :color="filters.reconciled ? 'primary' : 'white'" :text-color="filters.reconciled ? 'white' : 'primary'" @click="filters.reconciled = !filters.reconciled">Reconciled<q-tooltip>Verified against your bank statement</q-tooltip></q-chip>
                <q-chip clickable class="filter-chip" :color="filters.duplicatesOnly ? 'primary' : 'white'" :text-color="filters.duplicatesOnly ? 'white' : 'primary'" @click="filters.duplicatesOnly = !filters.duplicatesOnly">Duplicates</q-chip>
              </div>

              <!-- Collapsible: Reconciliation & Statement History -->
              <q-expansion-item
                dense
                icon="account_balance"
                label="Reconciliation & Statements"
                header-class="text-body2 text-weight-medium q-px-none"
                class="reconcile-expansion"
              >
                <q-card flat class="q-pa-sm reconcile-card">
                  <div class="register-reconcile-row">
                    <q-input v-model="registerBeginningBalanceInput" type="number" dense outlined label="Statement Begin" class="register-reconcile-row__input" />
                    <q-input v-model="registerTargetEndingInput" type="number" dense outlined label="Statement End" class="register-reconcile-row__input" />
                    <div class="register-reconcile-row__stat">
                      <div class="text-caption text-muted">Selected</div>
                      <div class="text-body2 text-weight-bold text-primary">{{ formatCurrency(registerMatchedTotal) }}</div>
                    </div>
                    <div class="register-reconcile-row__stat">
                      <div class="text-caption text-muted">Difference</div>
                      <div class="text-body2 text-weight-bold" :class="`text-${registerDeltaBadgeColor}`">{{ formatCurrency(registerDelta) }}</div>
                    </div>
                    <q-checkbox v-model="registerAllClearedSelected" dense :disable="!registerClearedVisibleRows.length" label="Select All Cleared" />
                    <q-space />
                    <q-btn
                      color="primary"
                      :disable="registerFinalizeDisabled"
                      :loading="registerFinalizing"
                      label="Finalize"
                      icon="check_circle"
                      @click="finalizeRegisterStatement"
                    />
                  </div>
                </q-card>

                <!-- Statement history sub-section -->
                <div v-if="accountStatements.length" class="q-mt-md">
                  <q-card flat class="reconcile-card">
                    <q-card-section class="q-py-sm q-px-md">
                      <div class="row items-center q-mb-xs">
                        <q-icon name="history" size="18px" color="primary" class="q-mr-xs" />
                        <span class="text-body2 text-weight-medium">Statement History</span>
                      </div>
                    </q-card-section>
                    <q-list dense separator class="q-px-sm">
                      <q-item v-for="stmt in accountStatements" :key="stmt.id" class="q-py-sm">
                        <q-item-section avatar style="min-width: 36px;">
                          <q-icon
                            :name="stmt.reconciled ? 'check_circle' : 'pending'"
                            :color="stmt.reconciled ? 'positive' : 'grey-5'"
                            size="20px"
                          />
                        </q-item-section>
                        <q-item-section>
                          <q-item-label class="text-body2">{{ formatDate(stmt.startDate) }} &mdash; {{ formatDate(stmt.endDate) }}</q-item-label>
                          <q-item-label caption>
                            {{ formatCurrency(stmt.startingBalance) }} &rarr; {{ formatCurrency(stmt.endingBalance) }}
                          </q-item-label>
                        </q-item-section>
                        <q-item-section side>
                          <q-btn
                            v-if="stmt.reconciled"
                            flat
                            dense
                            size="sm"
                            color="negative"
                            icon="undo"
                            label="Unreconcile"
                            @click="promptUnreconcile(stmt)"
                          />
                          <q-badge v-else color="grey-4" text-color="grey-7" label="Draft">
                            <q-tooltip>Statement not yet reconciled</q-tooltip>
                          </q-badge>
                        </q-item-section>
                      </q-item>
                    </q-list>
                  </q-card>
                </div>
              </q-expansion-item>
            </div>

            <!-- Compact selection bar -->
            <div v-if="selectedRegisterIds.length" class="register-selection-bar">
              <span class="text-body2 text-weight-medium">
                {{ selectedRegisterIds.length }} selected
              </span>
              <div class="row q-gutter-xs no-wrap">
                <q-btn dense flat size="sm" color="primary" label="Match" @click="openRegisterBatchDialog" />
                <q-btn dense flat size="sm" color="warning" label="Ignore" @click="confirmRegisterBatchAction('Ignore')" />
                <q-btn dense flat size="sm" color="negative" label="Delete" @click="confirmRegisterBatchAction('Delete')" />
                <q-btn
                  dense
                  flat
                  size="sm"
                  color="positive"
                  label="Finalize"
                  :disable="registerFinalizeDisabled"
                  :loading="registerFinalizing"
                  @click="finalizeRegisterStatement"
                />
              </div>
            </div>
            <div class="transactions-table">
              <ledger-table
                :rows="registerRows"
                :loading="loadingRegister"
                entity-label="Account"
                selection="multiple"
                v-model:selected="selectedRegisterIds"
                @row-click="onRegisterRowClick"
                @unmatch="promptUnmatch"
              />
            </div>
          </div>
        </div>
        <q-dialog v-model="showRegisterBatchDialog" max-width="500" @keyup.enter="executeRegisterBatchMatch">
          <q-card>
            <q-card-section class="bg-primary q-py-md">
              <span class="text-white">Batch Match Transactions</span>
            </q-card-section>
            <q-card-section class="q-pt-lg">
              <q-form ref="registerBatchForm">
                <p>
                  Assign an entity, merchant, and category for {{ selectedRegisterIds.length }} unmatched transaction{{
                    selectedRegisterIds.length > 1 ? 's' : ''
                  }}.
                </p>
                <div class="row">
                  <div class="col">
                    <q-select
                      v-model="selectedEntityId"
                      :options="entityOptions"
                      option-label="name"
                      option-value="id"
                      emit-value
                      map-options
                      label="Select Entity"
                      outlined
                      dense
                      :rules="requiredField"
                    />
                  </div>
                </div>
                <q-list bordered class="q-mt-md">
                  <q-item v-for="entry in batchEntries" :key="entry.id">
                    <q-item-section>
                      <div class="text-caption">{{ formatDate(entry.date) }} — {{ formatCurrency(entry.amount) }}</div>
                    </q-item-section>
                    <q-item-section>
                      <q-input v-model="entry.merchant" label="Merchant" dense :rules="requiredField" />
                    </q-item-section>
                    <q-item-section>
                      <q-select v-model="entry.category" :options="categoryOptionsFor(entry)" label="Category" dense :rules="requiredField" />
                    </q-item-section>
                  </q-item>
                </q-list>
              </q-form>
            </q-card-section>
            <q-card-actions>
              <q-space />
              <q-btn color="grey" flat @click="showRegisterBatchDialog = false">Cancel</q-btn>
              <q-btn color="primary" flat @click="executeRegisterBatchMatch" :loading="saving">Match</q-btn>
            </q-card-actions>
          </q-card>
        </q-dialog>
        <q-dialog v-model="showRegisterActionDialog">
          <q-card>
            <q-card-section>
              Are you sure you want to {{ registerBatchAction.toLowerCase() }} {{ selectedRegisterIds.length }} transaction{{
                selectedRegisterIds.length > 1 ? 's' : ''
              }}?
            </q-card-section>
            <q-card-actions>
              <q-btn flat label="Cancel" color="primary" v-close-popup />
              <q-btn flat label="Confirm" color="primary" :loading="saving" @click="performRegisterBatchAction" />
            </q-card-actions>
          </q-card>
        </q-dialog>
        <q-dialog v-model="showUnreconcileDialog">
          <q-card>
            <q-card-section class="bg-warning text-white q-py-md">
              <span>Unreconcile Statement</span>
            </q-card-section>
            <q-card-section v-if="statementToUnreconcile">
              <p>
                This will revert statement <strong>{{ formatDate(statementToUnreconcile.startDate) }} &mdash; {{ formatDate(statementToUnreconcile.endDate) }}</strong>
                and mark its transactions as cleared (not reconciled).
              </p>
              <p>Are you sure?</p>
            </q-card-section>
            <q-card-actions align="right">
              <q-btn flat label="Cancel" v-close-popup />
              <q-btn flat label="Unreconcile" color="negative" :loading="unreconciling" @click="confirmUnreconcile" />
            </q-card-actions>
          </q-card>
        </q-dialog>
        <q-dialog v-model="showUnmatchDialog">
          <q-card>
            <q-card-section class="bg-warning text-white q-py-md">
              <span>Unmatch Transaction</span>
            </q-card-section>
            <q-card-section v-if="rowToUnmatch">
              <p>
                Disconnect <strong>{{ rowToUnmatch.payee }}</strong> ({{ formatCurrency(rowToUnmatch.amount) }})
                from its linked budget transaction?
              </p>
              <p class="text-caption text-grey-7">The budget transaction will remain but will no longer be linked to this imported transaction.</p>
            </q-card-section>
            <q-card-actions align="right">
              <q-btn flat label="Cancel" v-close-popup />
              <q-btn flat label="Unmatch" color="negative" :loading="unmatching" @click="confirmUnmatch" />
            </q-card-actions>
          </q-card>
        </q-dialog>
      </q-tab-panel>
      <!-- Match Bank Transactions Tab -->
      <q-tab-panel name="match" class="transactions-panel">
        <GuidedTip tip-id="match-transactions">
          This tool helps you link imported bank transactions to your budget entries automatically.
        </GuidedTip>
        <match-bank-panel />
      </q-tab-panel>
    </q-tab-panels>
  </q-page>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue';
import { useQuasar } from 'quasar';
import { storeToRefs } from 'pinia';
import LedgerTable from 'src/components/LedgerTable.vue';
import GuidedTip from 'src/components/GuidedTip.vue';
import MatchBankPanel from 'src/components/MatchBankPanel.vue';
import EntitySelector from 'src/components/EntitySelector.vue';
import TransactionDialog from 'src/components/TransactionDialog.vue';
import { useTransactions } from 'src/composables/useTransactions';
import type { LedgerFilters, LedgerRow, Status } from 'src/composables/useTransactions';
import { useBudgetStore } from 'src/store/budget';
import { useFamilyStore } from 'src/store/family';
import { useUIStore } from 'src/store/ui';
import { useAuthStore } from 'src/store/auth';
import { sortBudgetsByMonthDesc, createBudgetForMonth } from 'src/utils/budget';
import { formatDate } from 'src/utils/helpers';
import { dataAccess } from 'src/dataAccess';
import type { Budget, Transaction, StatementFinalizePayload, Statement } from 'src/types';
import { useStatementStore } from 'src/store/statements';
import { splitImportedId } from 'src/utils/imported';

const tab = ref<'budget' | 'register' | 'match'>('budget');
const tabOptions = [
  { value: 'budget' as const, label: 'BUDGET REGISTER', shortLabel: 'Budget' },
  { value: 'register' as const, label: 'ACCOUNT REGISTER', shortLabel: 'Account' },
  { value: 'match' as const, label: 'MATCH BANK TRANSACTIONS', shortLabel: 'Match' },
];

const budgetStore = useBudgetStore();
const familyStore = useFamilyStore();
const uiStore = useUIStore();
const auth = useAuthStore();
const statementStore = useStatementStore();

const { selectedBudgetIds, budgetFilters, registerFilters } = storeToRefs(uiStore);
const { selectedEntityId } = storeToRefs(familyStore);

const accountOptions = computed(() => {
  const accounts = familyStore.family?.accounts || [];
  const opts = accounts
    .filter((a) => ['Bank', 'CreditCard', 'Investment'].includes(a.type))
    .map((a) => ({
      label: a.accountNumber ? `${a.name} (${a.accountNumber})` : a.name,
      value: String(a.id),
    }))
    .sort((a, b) => a.label.localeCompare(b.label));
  return [{ label: 'All', value: null }, ...opts];
});

const {
  transactions,
  filters,
  registerRows,
  loading,
  loadingRegister,
  loadImportedTransactions,
  loadInitial,
  getImportedTx,
} = useTransactions();

const activeRows = computed(() => {
  if (tab.value === 'budget') {
    return transactions.value;
  }
  if (tab.value === 'register') {
    return registerRows.value;
  }
  return [];
});

const overviewCounts = computed(() => {
  const counts = { total: activeRows.value.length, cleared: 0, reconciled: 0, unmatched: 0, duplicates: 0 };
  activeRows.value.forEach((row) => {
    if (row.status === 'C') {
      counts.cleared += 1;
    } else if (row.status === 'R') {
      counts.reconciled += 1;
    }
    if (!row.matched) {
      counts.unmatched += 1;
    }
    if (row.isDuplicate) {
      counts.duplicates += 1;
    }
  });
  return counts;
});

onMounted(loadBudgets);

const minAmtInput = ref('');
const maxAmtInput = ref('');

// Dates are stored ISO (YYYY-MM-DD) for lexicographic comparison against r.date,
// but displayed to US users as mm/dd/yyyy. See docs/design_system.md.
function makeDateDisplayRef(getIso: () => string | null, setIso: (v: string | null) => void) {
  return computed<string>({
    get() {
      const iso = getIso();
      if (!iso) return '';
      const [yyyy, mm, dd] = iso.split('-');
      return yyyy && mm && dd ? `${mm}/${dd}/${yyyy}` : '';
    },
    set(v: string) {
      if (!v || v.length < 10) { setIso(null); return; }
      const [mm, dd, yyyy] = v.split('/');
      if (mm && dd && yyyy && yyyy.length === 4) setIso(`${yyyy}-${mm}-${dd}`);
    },
  });
}
const startInput = makeDateDisplayRef(() => filters.value.start, (v) => { filters.value.start = v; });
const endInput = makeDateDisplayRef(() => filters.value.end, (v) => { filters.value.end = v; });

const showTxDialog = ref(false);
const editTx = ref<Transaction | null>(null);
const editBudgetId = ref('');
const $q = useQuasar();
const editCategoryOptions = computed(() => (editBudgetId.value ? budgetStore.getBudget(editBudgetId.value)?.categories.map((c) => c.name) || [] : []));

const selectedBudgetRowIds = ref<string[]>([]);
const showBudgetDeleteDialog = ref(false);
const deletingBudgetTransactions = ref(false);
const selectedBudgetRows = computed(() =>
  selectedBudgetRowIds.value
    .map((id) => transactions.value.find((row) => row.id === id))
    .filter((row): row is LedgerRow => Boolean(row)),
);

const selectedRegisterIds = ref<string[]>([]);
const showRegisterBatchDialog = ref(false);
const registerBatchForm = ref();
const batchEntries = ref<{ id: string; date: string; amount: number; merchant: string; category: string }[]>([]);
const showRegisterActionDialog = ref(false);
const registerBatchAction = ref<'Ignore' | 'Delete' | ''>('');
const saving = ref(false);
const registerBeginningBalanceInput = ref('0');
const registerTargetEndingInput = ref('0');
const registerFinalizing = ref(false);

const normalizedRegisterStatus = (status: LedgerRow['status']): Status => {
  if (status === 'M') return 'C';
  if (status === 'I') return 'C';
  return status;
};

const registerClearedVisibleRows = computed(() =>
  registerRows.value.filter((row) => normalizedRegisterStatus(row.status) === 'C'),
);

const registerSelectedRows = computed(() =>
  selectedRegisterIds.value
    .map((id) => registerRows.value.find((row) => row.id === id))
    .filter((row): row is LedgerRow => Boolean(row)),
);

const registerSelectedClearedRows = computed(() =>
  registerSelectedRows.value.filter((row) => normalizedRegisterStatus(row.status) === 'C'),
);

const parseNumericInput = (value: string): number => {
  const num = Number(value);
  return Number.isFinite(num) ? num : 0;
};

const registerMatchedTotal = computed(() =>
  registerSelectedClearedRows.value.reduce((sum, row) => sum + row.amount, 0),
);
const registerBeginningBalance = computed(() => parseNumericInput(registerBeginningBalanceInput.value));
const registerTargetEndingBalance = computed(() => parseNumericInput(registerTargetEndingInput.value));
const registerDelta = computed(() => registerTargetEndingBalance.value - (registerBeginningBalance.value + registerMatchedTotal.value));

const registerDeltaBadgeColor = computed(() => {
  if (Math.abs(registerDelta.value) < 0.005) return 'positive';
  return registerDelta.value > 0 ? 'warning' : 'negative';
});

const registerFinalizeDisabled = computed(() => {
  if (!filters.value.accountId) return true;
  if (!registerSelectedClearedRows.value.length) return true;
  if (!Number.isFinite(registerBeginningBalance.value) || !Number.isFinite(registerTargetEndingBalance.value)) return true;
  return Math.abs(registerDelta.value) >= 0.01;
});

const registerAllClearedSelected = computed({
  get() {
    if (!registerClearedVisibleRows.value.length) return false;
    return registerClearedVisibleRows.value.every((row) => selectedRegisterIds.value.includes(row.id));
  },
  set(value: boolean) {
    const clearedIds = registerClearedVisibleRows.value.map((row) => row.id);
    if (value) {
      const merged = new Set([...selectedRegisterIds.value, ...clearedIds]);
      selectedRegisterIds.value = Array.from(merged);
    } else {
      const clearedSet = new Set(clearedIds);
      selectedRegisterIds.value = selectedRegisterIds.value.filter((id) => !clearedSet.has(id));
    }
  },
});

// --- Statement history ---
const showUnreconcileDialog = ref(false);
const statementToUnreconcile = ref<Statement | null>(null);
const unreconciling = ref(false);

const selectedAccountNumber = computed(() => {
  if (!filters.value.accountId) return null;
  const acct = (familyStore.family?.accounts || []).find((a) => String(a.id) === filters.value.accountId);
  return acct ? String(acct.id) : null;
});

const accountStatements = computed(() => {
  const fid = familyStore.family?.id;
  const an = selectedAccountNumber.value;
  if (!fid || !an) return [];
  return [...statementStore.getStatements(fid, an)].sort((a, b) => b.endDate.localeCompare(a.endDate));
});

const latestReconciledEndingBalance = computed(() => {
  const reconciled = accountStatements.value.find((s) => s.reconciled);
  return reconciled ? reconciled.endingBalance : null;
});

function promptUnreconcile(stmt: Statement) {
  statementToUnreconcile.value = stmt;
  showUnreconcileDialog.value = true;
}

async function confirmUnreconcile() {
  const stmt = statementToUnreconcile.value;
  const fid = familyStore.family?.id;
  const an = selectedAccountNumber.value;
  if (!stmt || !fid || !an) return;
  unreconciling.value = true;
  try {
    await statementStore.unreconcileStatement(fid, an, stmt.id, []);
    $q.notify({ type: 'positive', message: 'Statement unreconciled.' });
    showUnreconcileDialog.value = false;
    statementToUnreconcile.value = null;
    await loadImportedTransactions(true);
  } catch (err) {
    const message = err instanceof Error ? err.message : 'Failed to unreconcile.';
    $q.notify({ type: 'negative', message });
  } finally {
    unreconciling.value = false;
  }
}

// --- Unmatch ---
const showUnmatchDialog = ref(false);
const rowToUnmatch = ref<LedgerRow | null>(null);
const unmatching = ref(false);

function promptUnmatch(row: LedgerRow) {
  rowToUnmatch.value = row;
  showUnmatchDialog.value = true;
}

async function confirmUnmatch() {
  const row = rowToUnmatch.value;
  if (!row) return;
  const imp = getImportedTx(row.id);
  if (!imp) return;
  unmatching.value = true;
  try {
    // Set matched=false on the imported transaction via batch-reconcile
    // We need to find which budget has the linked transaction
    const accountId = filters.value.accountId;
    if (!accountId) return;
    const budgetTxs = await dataAccess.getBudgetTransactionsMatchedToImported(accountId);
    const linked = budgetTxs.find(
      (bt) => bt.transaction.importedMerchant === imp.payee
        && (bt.transaction.postedDate === (imp.transactionDate || imp.postedDate) || bt.transaction.transactionDate === (imp.transactionDate || imp.postedDate)),
    );
    if (linked) {
      const budget = budgetStore.getBudget(linked.budgetId) || await dataAccess.getBudget(linked.budgetId);
      if (budget) {
        await dataAccess.batchReconcileTransactions(linked.budgetId, budget, {
          budgetId: linked.budgetId,
          reconciliations: [{
            budgetTransactionId: linked.transaction.id || '',
            importedTransactionId: imp.id,
            match: false,
            ignore: false,
          }],
        });
      }
    }
    showUnmatchDialog.value = false;
    rowToUnmatch.value = null;
    $q.notify({ type: 'positive', message: 'Transaction unmatched.' });
    await loadImportedTransactions(true);
  } catch (err) {
    const message = err instanceof Error ? err.message : 'Failed to unmatch.';
    $q.notify({ type: 'negative', message });
  } finally {
    unmatching.value = false;
  }
}

const entityOptions = computed(() => (familyStore.family?.entities || []).map((e) => ({ id: e.id, name: e.name })));

function categoryOptionsFor(entry: { amount: number; date: string }): string[] {
  if (entry.amount > 0) {
    return ['Income'];
  }
  const budgetMonth = (entry.date || '').slice(0, 7);
  const targetBudget = Array.from(budgetStore.budgets.values()).find(
    (b) => b.entityId === selectedEntityId.value && b.month === budgetMonth,
  );
  if (targetBudget && targetBudget.categories && targetBudget.categories.length > 0) {
    return targetBudget.categories
      .map((c) => c.name)
      .filter((n) => n && n !== 'Income')
      .sort();
  }
  // Fallback: all categories across all loaded budgets for this entity
  const cats = new Set<string>();
  Array.from(budgetStore.budgets.values())
    .filter((b) => !selectedEntityId.value || b.entityId === selectedEntityId.value)
    .forEach((b) => {
      (b.categories || []).forEach((cat) => {
        if (cat.name && cat.name !== 'Income') cats.add(cat.name);
      });
    });
  return Array.from(cats).sort();
}

const requiredField = [(v: string) => !!v || 'This field is required'];

function formatCurrency(n: number) {
  return (n < 0 ? '-$' : '$') + Math.abs(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function clearBudgetSelection() {
  selectedBudgetRowIds.value = [];
}

function openBudgetDeleteDialog() {
  if (!selectedBudgetRows.value.length) return;
  showBudgetDeleteDialog.value = true;
}

async function finalizeRegisterStatement() {
  if (registerFinalizeDisabled.value) return;
  const familyId = familyStore.family?.id;
  if (!familyId) {
    $q.notify({ type: 'negative', message: 'Family context missing.' });
    return;
  }
  const accountId = filters.value.accountId;
  if (!accountId) {
    $q.notify({ type: 'negative', message: 'Select an account before finalizing.' });
    return;
  }
  registerFinalizing.value = true;
  const selectedRows = registerSelectedClearedRows.value;
  const selectedDates = selectedRows.map((r) => r.date).filter(Boolean).sort();
  const payload: StatementFinalizePayload = {
    familyId,
    accountId,
    startDate: selectedDates[0] || filters.value.start || '',
    endDate: selectedDates[selectedDates.length - 1] || filters.value.end || '',
    beginningBalance: registerBeginningBalance.value,
    endingBalance: registerTargetEndingBalance.value,
    importedTransactionIds: selectedRows.map((row) => row.id),
  };
  try {
    await dataAccess.finalizeStatement(payload);
    $q.notify({ type: 'positive', message: 'Statement finalized.' });
    selectedRegisterIds.value = selectedRegisterIds.value.filter(
      (id) => !(payload.importedTransactionIds || []).includes(id),
    );
    await loadImportedTransactions(true);
    // Refresh statement history
    if (familyId && accountId) {
      await statementStore.loadStatements(familyId, accountId);
    }
  } catch (err) {
    console.error('Failed to finalize statement', err);
    const message = err instanceof Error ? err.message : 'Unable to finalize statement.';
    $q.notify({ type: 'negative', message });
  } finally {
    registerFinalizing.value = false;
  }
}

const createDefaultFilters = (): LedgerFilters => ({
  search: '',
  importedMerchant: '',
  cleared: false,
  reconciled: false,
  duplicatesOnly: false,
  minAmt: null,
  maxAmt: null,
  start: null,
  end: null,
  accountId: null,
  unmatchedOnly: false,
});

function syncInputsFromFilters() {
  minAmtInput.value = filters.value.minAmt == null ? '' : String(filters.value.minAmt);
  maxAmtInput.value = filters.value.maxAmt == null ? '' : String(filters.value.maxAmt);
}

function applyStoredFilters(t: 'budget' | 'register') {
  const source = t === 'budget' ? budgetFilters.value : registerFilters.value;
  filters.value = { ...source };
  syncInputsFromFilters();
}

watch(
  tab,
  (t, old) => {
    if (old === 'budget') budgetFilters.value = { ...filters.value };
    if (old === 'register') registerFilters.value = { ...filters.value };
    if (t === 'budget' || t === 'register') {
      applyStoredFilters(t);
    }
  },
  { immediate: true },
);

watch(
  filters,
  (f) => {
    if (tab.value === 'budget') budgetFilters.value = { ...f };
    else if (tab.value === 'register') registerFilters.value = { ...f };
  },
  { deep: true },
);

async function ensureAccountsLoaded() {
  if (!familyStore.family?.accounts || familyStore.family.accounts.length === 0) {
    const fid = familyStore.family?.id;
    if (fid) {
      try {
        const accounts = await dataAccess.getAccounts(fid);
        if (familyStore.family) familyStore.family.accounts = accounts;
      } catch {
        /* ignore */
      }
    }
  }
}

// Ensure an account is selected when viewing the register so data loads
watch(
  [tab, accountOptions],
  async ([t]) => {
    if (t === 'register') {
      await ensureAccountsLoaded();
    }
  },
  { immediate: true },
);

watch(tab, async (t) => {
  if (t === 'register' && registerRows.value.length === 0) {
    await loadImportedTransactions(true);
  }
});

watch(minAmtInput, (v) => {
  filters.value.minAmt = v === '' ? null : Number(v);
});
watch(maxAmtInput, (v) => {
  filters.value.maxAmt = v === '' ? null : Number(v);
});
watch(
  () => filters.value.minAmt,
  (v) => {
    if (v == null) minAmtInput.value = '';
  },
);
watch(
  () => filters.value.maxAmt,
  (v) => {
    if (v == null) maxAmtInput.value = '';
  },
);

watch(
  () => filters.value.accountId,
  async (accountId) => {
    registerBeginningBalanceInput.value = '0';
    registerTargetEndingInput.value = '0';
    selectedRegisterIds.value = [];
    // Load statement history for the selected account
    const fid = familyStore.family?.id;
    if (fid && accountId) {
      try {
        await statementStore.loadStatements(fid, accountId);
      } catch { /* ignore */ }
    }
  },
);

// Auto-populate beginning balance from last reconciled statement
watch(latestReconciledEndingBalance, (bal) => {
  if (bal != null) {
    registerBeginningBalanceInput.value = String(bal);
  }
});

const formatLongMonth = (month: string) => {
  const [year, monthNum] = month.split('-');
  const date = new Date(parseInt(year), parseInt(monthNum) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
};

const budgetOptions = computed(() =>
  sortBudgetsByMonthDesc(Array.from(budgetStore.budgets.values()).filter((b) => !selectedEntityId.value || b.entityId === selectedEntityId.value)).map((b) => ({
    label: formatLongMonth(b.month),
    value: b.budgetId || '',
  })),
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

watch(
  () => transactions.value.map((row) => row.id),
  (ids) => {
    const available = new Set(ids);
    const filtered = selectedBudgetRowIds.value.filter((id) => available.has(id));
    if (filtered.length !== selectedBudgetRowIds.value.length) {
      selectedBudgetRowIds.value = filtered;
    }
  },
);

watch(tab, (value) => {
  if (value !== 'budget' && selectedBudgetRowIds.value.length) {
    selectedBudgetRowIds.value = [];
  }
});

watch(
  () => registerRows.value.map((row) => row.id),
  (ids) => {
    const available = new Set(ids);
    const filtered = selectedRegisterIds.value.filter((id) => available.has(id));
    if (filtered.length !== selectedRegisterIds.value.length) {
      selectedRegisterIds.value = filtered;
    }
  },
);

watch(registerRows, (rows) => {
  if (!rows.length) return;
  if (!filters.value.start) {
    const last = rows[rows.length - 1]?.date?.slice(0, 10);
    if (last) filters.value.start = last;
  }
  if (!filters.value.end) {
    const first = rows[0]?.date?.slice(0, 10);
    if (first) filters.value.end = first;
  }
});

function clearBudgetFilters() {
  selectedBudgetIds.value = [];
  filters.value = createDefaultFilters();
  syncInputsFromFilters();
}

function clearRegisterFilters() {
  filters.value = createDefaultFilters();
  syncInputsFromFilters();
  selectedRegisterIds.value = [];
  registerBeginningBalanceInput.value = '0';
  registerTargetEndingInput.value = '0';
  void loadImportedTransactions(true);
}

async function refreshBudget() {
  await loadBudgets();
  if (selectedBudgetIds.value.length > 0) {
    await loadInitial(selectedBudgetIds.value);
  }
}

async function ensureBudgetLoadedWithTransactions(budgetId: string): Promise<Budget | null> {
  let budget = budgetStore.getBudget(budgetId);
  if (!budget || !budget.transactions || budget.transactions.length === 0) {
    try {
      budget = await dataAccess.getBudget(budgetId);
      if (budget) {
        budgetStore.updateBudget(budgetId, budget);
      }
    } catch (err) {
      console.error('Failed to load budget for deletion', budgetId, err);
      return null;
    }
  }
  return budget ?? null;
}

async function deleteSelectedBudgetTransactions() {
  if (!selectedBudgetRows.value.length) {
    showBudgetDeleteDialog.value = false;
    return;
  }

  deletingBudgetTransactions.value = true;
  try {
    const budgetsById = new Map<string, Budget>();
    for (const row of selectedBudgetRows.value) {
      if (!row.budgetId) continue;
      let budget = budgetsById.get(row.budgetId);
      if (!budget) {
        budget = await ensureBudgetLoadedWithTransactions(row.budgetId);
        if (!budget) {
          throw new Error('Unable to load budget data for the selected transactions.');
        }
        budgetsById.set(row.budgetId, budget);
      }
      await dataAccess.deleteTransaction(budget, row.id);
    }

    showBudgetDeleteDialog.value = false;
    selectedBudgetRowIds.value = [];
    if (selectedBudgetIds.value.length > 0) {
      await loadInitial(selectedBudgetIds.value);
    }
    $q.notify({ type: 'positive', message: 'Transactions marked as deleted' });
  } catch (err) {
    console.error('Failed to mark transactions as deleted', err);
    const message = err instanceof Error ? err.message : 'Failed to delete transactions';
    $q.notify({ type: 'negative', message });
  } finally {
    deletingBudgetTransactions.value = false;
  }
}

async function refreshRegister() {
  await loadImportedTransactions(true);
}

async function onRowClick(row: LedgerRow) {
  const tx = row.transaction;
  if (tx) {
    editTx.value = { ...tx };
    editBudgetId.value = row.budgetId;
    showTxDialog.value = true;
    return;
  }

  let budget = budgetStore.getBudget(row.budgetId);

  // Some budgets in the store may only contain summary info without
  // transactions. If we don't have the full budget or its transaction
  // list hasn't been populated yet, fetch it from the API so we can
  // locate the transaction to edit.
  if (!budget || !budget.transactions || budget.transactions.length === 0) {
    try {
      budget = await dataAccess.getBudget(row.budgetId);
      if (budget) budgetStore.updateBudget(row.budgetId, budget);
    } catch (err) {
      console.error('Failed to load budget for transaction', row.budgetId, err);
    }
  }

  const fetchedTx = budget?.transactions?.find((t) => t.id === row.id);
  if (fetchedTx) {
    editTx.value = { ...fetchedTx };
    editBudgetId.value = budget?.budgetId || row.budgetId;
    showTxDialog.value = true;
  }
}

async function onTransactionSaved(updated: Transaction) {
  showTxDialog.value = false;
  const budget = budgetStore.getBudget(editBudgetId.value);
  if (budget) {
    const idx = budget.transactions.findIndex((t) => t.id === updated.id);
    if (idx >= 0) budget.transactions[idx] = updated;
    else budget.transactions.push(updated);
  }
  await loadInitial(selectedBudgetIds.value);
  editTx.value = null;
}

function onTxCancel() {
  showTxDialog.value = false;
  editTx.value = null;
}

// Filters come from useTransactions

function removeFilter(key: keyof typeof filters.value) {
  const current = filters.value[key];
  (filters.value as Record<string, unknown>)[key as string] = typeof current === 'boolean' ? false : null;
}

const activeChips = computed<Record<string, unknown>>(() => {
  const res: Record<string, unknown> = {};
  Object.entries(filters.value).forEach(([k, v]) => {
    if (k === 'accountId') return;
    if (v !== null && v !== '' && v !== false) {
      res[k] = typeof v === 'boolean' ? '' : v;
    }
  });
  return res;
});

function removeChip(key: string) {
  removeFilter(key as keyof typeof filters.value);
}

function onRegisterRowClick(row: LedgerRow) {
  if (row.status === 'U') {
    selectedRegisterIds.value = [row.id];
    void openRegisterBatchDialog();
  }
}

async function openRegisterBatchDialog() {
  if (
    selectedRegisterIds.value.length === 0 ||
    !selectedRegisterIds.value.every((id) => {
      const tx = registerRows.value.find((r) => r.id === id);
      return tx && tx.status === 'U';
    })
  ) {
    return;
  }
  batchEntries.value = selectedRegisterIds.value.map((id) => {
    const tx = registerRows.value.find((r) => r.id === id);
    return {
      id,
      date: tx?.date || '',
      amount: tx?.amount || 0,
      merchant: tx?.importedMerchant || tx?.payee || '',
      category: '',
    };
  });

  // Ensure we have full category data for budgets covering the selected entries'
  // months only. loadAccessibleBudgets returns thin summaries without categories.
  const neededMonths = new Set(
    batchEntries.value.map((e) => (e.date || '').slice(0, 7)).filter(Boolean),
  );
  const budgetsToLoad = Array.from(budgetStore.budgets.values()).filter(
    (b) =>
      (!selectedEntityId.value || b.entityId === selectedEntityId.value) &&
      neededMonths.has(b.month) &&
      (!b.categories || b.categories.length === 0),
  );
  await Promise.all(
    budgetsToLoad.map(async (b) => {
      try {
        const full = await dataAccess.getBudget(b.budgetId);
        if (full) budgetStore.updateBudget(b.budgetId, full);
      } catch (err) {
        console.error('Failed to load full budget', b.budgetId, err);
      }
    }),
  );

  showRegisterBatchDialog.value = true;
}

async function executeRegisterBatchMatch() {
  if (!registerBatchForm.value) return;
  const valid = await registerBatchForm.value.validate();
  if (!valid) return;
  saving.value = true;
  try {
    const family = familyStore.family;
    const ownerUid = family?.ownerUid || auth.user?.uid || '';
    const matchesByBudget: Record<
      string,
      Array<{
        budgetTransactionId: string;
        importedTransactionId: string;
        match: boolean;
        ignore: boolean;
      }>
    > = {};
    for (const entry of batchEntries.value) {
      const imported = getImportedTx(entry.id);
      if (!imported || !family) continue;
      const budgetMonth = entry.date.slice(0, 7);
      let targetBudget = Array.from(budgetStore.budgets.values()).find((b) => b.entityId === selectedEntityId.value && b.month === budgetMonth);
      if (!targetBudget) {
        targetBudget = await createBudgetForMonth(budgetMonth, family.id, ownerUid, selectedEntityId.value);
      }
      if (!targetBudget?.budgetId) continue;
      if (!targetBudget.categories.some((cat) => cat.name === entry.category)) {
        targetBudget.categories.push({
          name: entry.category,
          target: 0,
          isFund: false,
          groupName: '(Ungrouped)',
        });
        await dataAccess.saveBudget(targetBudget.budgetId, targetBudget);
        budgetStore.updateBudget(targetBudget.budgetId, targetBudget);
      }
      const tx: Transaction = {
        id: '',
        date: entry.date,
        budgetMonth,
        merchant: entry.merchant,
        categories: [{ category: entry.category, amount: Math.abs(entry.amount) }],
        amount: Math.abs(entry.amount),
        notes: '',
        recurring: false,
        recurringInterval: 'Monthly',
        userId: auth.user?.uid || '',
        isIncome: entry.amount > 0,
        accountSource: imported.accountSource || '',
        accountNumber: imported.accountNumber || '',
        postedDate: imported.transactionDate || imported.postedDate || '',
        checkNumber: imported.checkNumber,
        importedMerchant: imported.payee,
        status: 'C',
        entityId: selectedEntityId.value,
        taxMetadata: imported.taxMetadata || [],
      };
      const savedTx = await dataAccess.saveTransaction(targetBudget, tx);
      if (!matchesByBudget[targetBudget.budgetId]) matchesByBudget[targetBudget.budgetId] = [];
      matchesByBudget[targetBudget.budgetId].push({
        budgetTransactionId: savedTx.id,
        importedTransactionId: imported.id,
        match: true,
        ignore: false,
      });
    }
    await Promise.all(
      Object.entries(matchesByBudget).map(async ([budgetId, recs]) => {
        const budget = budgetStore.getBudget(budgetId);
        if (!budget) return;
        const reconcileData = {
          budgetId,
          reconciliations: recs,
        };
        await dataAccess.batchReconcileTransactions(budgetId, budget, reconcileData);
        const updatedBudget = await dataAccess.getBudget(budgetId);
        if (updatedBudget) budgetStore.updateBudget(budgetId, updatedBudget);
      }),
    );
    showRegisterBatchDialog.value = false;
    selectedRegisterIds.value = [];
    await loadImportedTransactions(true);
  } finally {
    saving.value = false;
  }
}

function confirmRegisterBatchAction(action: 'Ignore' | 'Delete') {
  registerBatchAction.value = action;
  showRegisterActionDialog.value = true;
}

async function performRegisterBatchAction() {
  saving.value = true;
  try {
    for (const id of selectedRegisterIds.value) {
      const { docId, txId } = splitImportedId(id);
      if (registerBatchAction.value === 'Delete') {
        await dataAccess.deleteImportedTransaction(docId, txId);
      } else if (registerBatchAction.value === 'Ignore') {
        await dataAccess.updateImportedTransaction(docId, txId, false, true);
      }
    }
    selectedRegisterIds.value = [];
    showRegisterActionDialog.value = false;
    await loadImportedTransactions(true);
  } finally {
    saving.value = false;
  }
}
</script>

<style scoped>
.transactions-page {
  padding: 24px 24px 56px;
}

@media (min-width: 1280px) {
  .transactions-page {
    padding: 32px 48px 72px;
  }
}

.transactions-panel {
  min-height: 0;
}

.transactions-hero {
  padding: 0 0 16px;
}

.transactions-hero__tabs-row {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

.tx-pill {
  border: none;
  border-radius: 999px;
  padding: 8px 16px;
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.02em;
  cursor: pointer;
  transition: all 0.15s ease;
  background: transparent;
  color: #64748b;
  border: 1px solid #cbd5e1;
}

.tx-pill--active {
  background: #1d4ed8;
  color: #ffffff;
  border-color: #1d4ed8;
  font-weight: 600;
}

.tx-pill:hover:not(.tx-pill--active) {
  background: #f8fafc;
}

.transactions-hero__metrics {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(4, 1fr);
}

.tx-metric {
  background: #dbeafe;
  border-radius: 14px;
  padding: 12px 16px;
}

.tx-metric__label {
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #1e40af;
  font-weight: 600;
}

.tx-metric__value {
  margin-top: 6px;
  font-size: 24px;
  font-weight: 700;
  color: #1e3a5f;
}

.transactions-layout {
  gap: 20px;
  min-height: 0;
}

@media (min-width: 1200px) {
  .transactions-layout {
    align-items: stretch;
  }
}

.transactions-layout__main {
  gap: 20px;
  min-width: 0;
  min-height: 0;
}

.transactions-layout__aside {
  width: 320px;
  gap: 16px;
}

@media (max-width: 1199px) {
  .transactions-layout__aside {
    width: 100%;
    min-height: 0;
  }
}

.transactions-filters {
  gap: 16px;
}

.register-filters {
  gap: 8px;
}

.register-filters__secondary {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.register-filters__input {
  width: 110px;
  flex: 0 0 auto;
}

.register-filters__input--date {
  width: 150px;
}

.register-filters__input--merchant {
  width: 160px;
}

.transactions-filters__toggles {
  gap: 8px;
  align-items: center;
}

.filter-chip {
  border-radius: 999px;
  padding: 4px 14px;
  font-weight: 600;
  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease;
  box-shadow: 0 6px 14px rgba(37, 99, 235, 0.12);
}

.filter-chip:not(.bg-primary) {
  box-shadow: none;
  border: 1px solid rgba(37, 99, 235, 0.22);
}

.filter-chip:hover {
  transform: translateY(-1px);
}

.transactions-filters__chips {
  gap: 8px;
}

.transactions-filters__chips :deep(.q-chip) {
  background: rgba(37, 99, 235, 0.1);
}

.transactions-summary-card {
  padding: 18px 20px;
}

.summary-title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.summary-row {
  justify-content: space-between;
  align-items: center;
  margin-top: 12px;
  font-size: 0.95rem;
}

.summary-label {
  color: var(--color-text-muted);
}

.summary-value {
  font-weight: 600;
  color: var(--color-text-primary);
  text-align: right;
}

.transactions-selection {
  padding: 18px 20px;
  gap: 12px;
}

.transactions-selection__title {
  font-size: 1rem;
  font-weight: 600;
}

.transactions-selection__actions {
  gap: 12px;
}

.reconcile-expansion {
  border-top: 1px solid var(--color-outline-soft);
  margin-top: 8px;
}

.reconcile-card {
  background: var(--color-surface-subtle);
  border-radius: var(--radius-sm);
}

.register-reconcile-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
}

.register-reconcile-row__input {
  width: 150px;
  flex: 0 0 auto;
}

.register-reconcile-row__stat {
  text-align: center;
  min-width: 80px;
}

.register-selection-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
  padding: 6px 12px;
  border-radius: var(--radius-sm);
  background: var(--color-surface-subtle);
}

.transactions-table {
  min-height: 360px;
  min-width: 0;
}

.transactions-table :deep(.q-table__middle) {
  min-height: 0;
  overflow: auto;
}

@media (max-width: 959px) {
  .transactions-page {
    padding: 16px 12px 48px;
  }

  .transactions-hero__metrics {
    grid-template-columns: repeat(2, 1fr);
  }

  .transactions-hero__tabs-row {
    flex-wrap: wrap;
  }

  .transactions-layout__aside {
    width: 100%;
  }
}

@media (max-width: 599px) {
  .register-filters__input,
  .register-filters__input--date {
    width: 100%;
    flex: 1 1 100%;
  }

  .register-reconcile-row__input {
    width: 100%;
    flex: 1 1 100%;
  }
}
</style>
