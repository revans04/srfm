<!-- src/components/TransactionForm.vue -->
<template>
  <div v-if="isInitialized && locTrnsx?.categories">
    <q-form ref="form" class="transaction-form q-gutter-sm" @submit.prevent="save">
      <div class="transaction-form__field">
        <div class="text-caption text-muted q-mb-xs">Type</div>
        <q-btn-toggle
          v-model="transactionMode"
          :options="[
            { label: 'Expense', value: 'expense' },
            { label: 'Income', value: 'income' },
            { label: 'Transfer', value: 'transfer' }
          ]"
          no-caps
          unelevated
          spread
          toggle-color="primary"
          color="grey-3"
          text-color="dark"
          class="transaction-form__type-toggle"
        />
      </div>

      <div class="transaction-form__field">
        <q-input
          v-model="locTrnsx.date"
          type="date"
          :rules="requiredField"
          label="Date *"
          aria-required="true"
          stack-label
          dense
          outlined
          hide-bottom-space
          @input="updateBudgetMonth"
        />
      </div>

      <div v-if="transactionMode !== 'transfer'" class="transaction-form__field">
        <q-input
          ref="merchantInputRef"
          v-model="locTrnsx.merchant"
          :rules="requiredField"
          label="Merchant *"
          aria-required="true"
          stack-label
          dense
          outlined
          hide-bottom-space
          clearable
          autocomplete="off"
          @update:model-value="onMerchantTyping"
          @focus="showMerchantSuggestions = true"
          @blur="hideMerchantSuggestions"
        >
          <q-menu
            v-model="showMerchantSuggestions"
            no-parent-event
            fit
            no-focus
            no-refocus
            class="merchant-suggestions-menu"
          >
            <q-list dense>
              <q-item
                v-for="name in filteredMerchants"
                :key="name"
                clickable
                v-close-popup
                @mousedown.prevent="selectMerchant(name)"
              >
                <q-item-section>{{ name }}</q-item-section>
              </q-item>
              <q-item v-if="filteredMerchants.length === 0" disable>
                <q-item-section class="text-grey-6 text-caption">No matches</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-input>
      </div>

      <div class="transaction-form__field">
        <Currency-Input
          v-model="locTrnsx.amount"
          label="Amount"
          stack-label
          dense
          outlined
          class="transaction-form__amount"
        />
      </div>

      <!-- Transfer mode: source and destination category selects -->
      <div v-if="transactionMode === 'transfer'" class="transaction-form__section">
        <div class="transaction-form__section-title">
          <div class="text-caption text-uppercase text-grey-6">Transfer Between Categories</div>
        </div>
        <div class="q-gutter-sm">
          <q-select
            v-model="transferSource"
            :options="transferSourceOptions"
            :rules="requiredField"
            label="From Category *"
            aria-required="true"
            stack-label
            dense
            outlined
            hide-bottom-space
            use-input
            input-debounce="0"
            @filter="onTransferSourceFilter"
          />
          <q-select
            v-model="transferDest"
            :options="transferDestOptions"
            :rules="requiredField"
            label="To Category *"
            aria-required="true"
            stack-label
            dense
            outlined
            hide-bottom-space
            use-input
            input-debounce="0"
            @filter="onTransferDestFilter"
          />
          <q-banner v-if="transferSource && transferDest && transferSource === transferDest" class="bg-negative text-white">
            Source and destination must be different categories.
          </q-banner>
        </div>
      </div>

      <!-- Standard mode: free-form category splits -->
      <div v-else class="transaction-form__section">
        <div class="transaction-form__section-title row items-center justify-between">
          <div class="text-caption text-uppercase text-grey-6">Categories</div>
          <q-btn flat color="primary" dense icon="add" label="Add Split" @click="addSplit" />
        </div>
        <div class="transaction-form__splits q-gutter-md">
          <div v-for="(split, index) in locTrnsx.categories" :key="index" class="split-row">
            <div class="split-row__category">
              <q-select
                v-model="split.category"
                :options="filteredCategories"
                label="Category *"
                :rules="requiredField"
                aria-required="true"
                stack-label
                dense
                outlined
                menu-icon=""
                class="full-width"
                required
                use-input
                input-debounce="0"
                hide-bottom-space
                @filter="onCategoryFilter"
              />
            </div>
            <!-- Split amount is only relevant when there's more than one
                 category. With a single category the amount is always the
                 transaction's full amount, so we hide the field and keep
                 it auto-synced via a watcher (see syncSingleSplitAmount).
                 This eliminates the previous "stale 0 / mismatched value
                 saved" bug. -->
            <div v-if="locTrnsx.categories.length > 1" class="split-row__amount">
              <Currency-Input v-model="split.amount" label="Amount" stack-label dense outlined />
            </div>
            <q-btn v-if="locTrnsx.categories.length > 1" dense flat round icon="close" color="negative" size="sm" class="split-row__remove" @click="removeSplit(index)" />
          </div>
          <q-banner v-if="locTrnsx.categories.length > 1 && remainingSplit !== 0" :type="remainingSplit < 0 ? 'negative' : 'warning'">
            <div v-if="remainingSplit > 0">Remaining ${{ toDollars(toCents(Math.abs(remainingSplit))) }} unallocated &mdash; assign it to a category before saving.</div>
            <div v-else>Over allocated ${{ toDollars(toCents(Math.abs(remainingSplit))) }} (splits total ${{ toDollars(toCents(locTrnsx.amount + remainingSplit)) }} but transaction amount is ${{ toDollars(toCents(locTrnsx.amount)) }}).</div>
          </q-banner>
        </div>
      </div>

      <div class="transaction-form__field">
        <q-input
          type="textarea"
          v-model="locTrnsx.notes"
          label="Notes"
          stack-label
          outlined
          @focus="scrollToNoteField"
        />
      </div>

      <div class="transaction-form__field">
        <q-select
          v-model="locTrnsx.budgetMonth"
          :options="availableMonths"
          :rules="requiredField"
          label="Budget Month *"
          aria-required="true"
          stack-label
          dense
          outlined
          hide-bottom-space
          :disable="availableMonths.length === 0"
        />
      </div>

      <div class="transaction-form__field" v-if="availableMonths.length === 0">
        <q-banner class="bg-warning text-white q-ma-none">
          No budgets available.
          <template #action>
            <q-btn flat color="white" label="Go to Dashboard" to="/dashboard" />
          </template>
        </q-banner>
      </div>

      <div
        v-if="transactionMode !== 'transfer' && !hasActiveCategoryDefault"
        class="transaction-form__field"
      >
        <q-select
          v-model="locTrnsx.fundedByGoalId"
          :options="goalOptions"
          label="Fund from Goal"
          stack-label
          dense
          outlined
          emit-value
          map-options
          clearable
        />
      </div>

      <!-- Category-level default funding source ("Rosemary Beach is funded
           from Vacation Goal by default"). Appears only when the selected
           category(ies) all share the same fundingSourceCategory and the
           user hasn't explicitly picked a goal. The default is ON so the
           expense routes through a transfer automatically; user can opt out. -->
      <div
        v-if="transactionMode === 'expense' && !locTrnsx.fundedByGoalId && inferredFundingSource"
        class="transaction-form__field"
      >
        <q-toggle
          v-model="sourceFromCategoryEnabled"
          :label="`Source from ${inferredFundingSource}`"
          color="primary"
        />
        <div class="text-caption text-grey-7 q-mt-xs">
          This expense will draw from <strong>{{ inferredFundingSource }}</strong> instead of your monthly income.
        </div>
      </div>

      <!-- Category-level default goal funding source ("Vacation Spending is
           funded from the Vacation goal by default"). Mutually exclusive with
           the category-level category source via DB CHECK; the dropdowns in
           BudgetPage disable each other in the UI. Transaction-level
           fundedByGoalId still wins if explicitly set. -->
      <div
        v-if="transactionMode === 'expense' && !locTrnsx.fundedByGoalId && inferredFundingGoal"
        class="transaction-form__field"
      >
        <q-toggle
          v-model="sourceFromGoalEnabled"
          :label="`Source from ${inferredFundingGoal.name}`"
          color="primary"
        />
        <div class="text-caption text-grey-7 q-mt-xs">
          This expense will draw from the <strong>{{ inferredFundingGoal.name }}</strong> goal instead of your monthly income.
        </div>
      </div>

      <div v-if="transactionMode !== 'transfer'" class="transaction-form__field">
        <q-checkbox v-model="locTrnsx.recurring" label="Recurring" />
      </div>
      <div class="transaction-form__field" v-if="locTrnsx.recurring && transactionMode !== 'transfer'">
        <q-select
          v-model="locTrnsx.recurringInterval"
          :options="intervals"
          label="Recurring Interval"
          stack-label
          dense
          outlined
        />
      </div>

      <div v-if="locTrnsx.status && (locTrnsx.status === 'C' || locTrnsx.status === 'R')" class="transaction-form__section">
        <div class="transaction-form__section-title text-caption text-uppercase text-grey-6 q-mb-sm">
          Imported data
        </div>
        <div class="transaction-form__field">
          <q-input v-model="locTrnsx.postedDate" type="date" label="Posted Date" stack-label dense borderless readonly />
        </div>
        <div class="transaction-form__field">
          <q-input v-model="locTrnsx.importedMerchant" label="Imported Merchant" stack-label dense borderless readonly />
        </div>
        <div class="transaction-form__field">
          <q-input v-model="locTrnsx.accountSource" label="Account Source" stack-label dense borderless readonly />
        </div>
        <div class="transaction-form__field">
          <q-input v-model="locTrnsx.accountNumber" label="Account Number" stack-label dense borderless readonly />
        </div>
        <div class="transaction-form__field">
          <q-input v-model="locTrnsx.checkNumber" label="Check Number" stack-label dense borderless readonly />
        </div>
        <div class="transaction-form__field">
          <q-input v-model="locTrnsx.status" label="Status" stack-label dense borderless readonly />
        </div>
        <div class="transaction-form__field q-pt-none">
          <q-btn flat color="warning" :loading="isLoading" @click="resetMatch">Reset Match</q-btn>
        </div>
      </div>
    </q-form>
  </div>
  <div v-else>Loading...</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive, watch } from 'vue';
import { useQuasar } from 'quasar';
import { dataAccess } from '../dataAccess';
import type { Budget, Transaction, Goal } from '../types';
import { toCents, toDollars, todayISO, currentMonthISO, adjustTransactionDate, generateDailyTransactions, generateWeeklyTransactions, generateBiWeeklyTransactions } from '../utils/helpers';
import CurrencyInput from './CurrencyInput.vue';
import { QForm } from 'quasar';
import { useMerchantStore } from '../store/merchants';
import { useBudgetStore } from '../store/budget';
import { useGoals } from '../composables/useGoals';
import { useFamilyStore } from '../store/family';
import { isIncomeCategory } from '../utils/groups';
import { createBudgetForMonth } from '../utils/budget';
import { inferGoalFundingFromCategory } from '../utils/transactionFunding';
import { v4 as uuidv4 } from 'uuid';

const merchantStore = useMerchantStore();
const budgetStore = useBudgetStore();
const $q = useQuasar();
const familyStore = useFamilyStore();
const { listGoals } = useGoals();

const props = defineProps<{
  initialTransaction: Transaction;
  categoryOptions: string[];
  budgetId: string;
  userId: string;
}>();

const emit = defineEmits<{
  (e: 'save', transaction: Transaction): void;
  (e: 'cancel'): void;
  (e: 'update-transactions', transactions: Transaction[]): void;
}>();

const requiredField = [(value: string | null) => !!value || 'This field is required', (value: string | null) => value !== '' || 'This field is required'];

const form = ref<InstanceType<typeof QForm> | null>(null);

const defaultTransaction: Transaction = {
  id: '',
  budgetMonth: currentMonthISO(),
  date: todayISO(),
  merchant: '',
  categories: [{ category: '', amount: 0 }],
  amount: 0,
  notes: '',
  recurring: false,
  recurringInterval: 'Monthly',
  userId: props.userId,
  isIncome: false,
  taxMetadata: [],
};

const locTrnsx = reactive<Transaction>(
  props.initialTransaction
    ? {
        ...defaultTransaction,
        ...props.initialTransaction,
        categories: props.initialTransaction.categories ? [...props.initialTransaction.categories] : [{ category: '', amount: 0 }],
      }
    : { ...defaultTransaction },
);

const isInitialized = ref(false);
const budget = ref<Budget | null>(null);
const transactions = ref<Transaction[]>([]);
const isLoading = ref(false);
const intervals = ref(['Daily', 'Weekly', 'Bi-Weekly', 'Monthly', 'Quarterly', 'Bi-Annually', 'Yearly']);
const isMobile = computed(() => $q.screen.lt.md);

const goalList = ref<Goal[]>([]);
const goalOptions = computed(() => goalList.value.map((g) => ({ label: g.name, value: g.id })));

const transactionMode = computed({
  get(): 'expense' | 'income' | 'transfer' {
    if (locTrnsx.transactionType === 'transfer') return 'transfer';
    return locTrnsx.isIncome ? 'income' : 'expense';
  },
  set(val: 'expense' | 'income' | 'transfer') {
    if (val === 'transfer') {
      locTrnsx.transactionType = 'transfer';
      locTrnsx.isIncome = false;
    } else {
      locTrnsx.transactionType = 'standard';
      locTrnsx.isIncome = val === 'income';
    }
  },
});

// Transfer-specific state
const transferSource = ref('');
const transferDest = ref('');
const transferSourceFilterText = ref('');
const transferDestFilterText = ref('');

// Category-default funding source state. When the destination category has
// a `fundingSourceCategory`, we offer to auto-source the expense from it
// (creating a transfer on save). Toggle defaults ON whenever the inferred
// source becomes available.
const sourceFromCategoryEnabled = ref(false);

const inferredFundingSource = computed<string | null>(() => {
  if (transactionMode.value !== 'expense') return null;
  const b = budget.value;
  if (!b) return null;
  const destNames = locTrnsx.categories.map((c) => c.category).filter((n): n is string => !!n);
  if (destNames.length === 0) return null;
  const sources = new Set<string>();
  for (const name of destNames) {
    const cat = b.categories.find((c) => c.name === name);
    if (cat?.fundingSourceCategory) sources.add(cat.fundingSourceCategory);
    else return null; // Any split without a funding source breaks the inference
  }
  return sources.size === 1 ? (Array.from(sources)[0] ?? null) : null;
});

// Category-default goal funding source. Parallels `inferredFundingSource` but
// for `fundingSourceGoalId`. When the destination category(ies) all share the
// same `fundingSourceGoalId`, surface the goal so the form can offer to auto-
// source the expense from it.
const sourceFromGoalEnabled = ref(false);

const inferredFundingGoal = computed<Goal | null>(() => {
  if (transactionMode.value !== 'expense') return null;
  const b = budget.value;
  if (!b) return null;
  const destNames = locTrnsx.categories.map((c) => c.category).filter((n): n is string => !!n);
  return inferGoalFundingFromCategory(b, destNames, goalList.value);
});

watch(
  inferredFundingSource,
  (src) => {
    sourceFromCategoryEnabled.value = !!src;
  },
  { immediate: true },
);

watch(
  inferredFundingGoal,
  (g) => {
    sourceFromGoalEnabled.value = !!g;
  },
  { immediate: true },
);

// True when a category-level default funding source (either another category
// or a goal) is both inferred and enabled. While true, the per-transaction
// "Fund from Goal" dropdown is hidden — it would be redundant noise on top of
// the toggle. Toggling off reveals the dropdown again so the user can pick a
// different goal as a one-off override.
const hasActiveCategoryDefault = computed(
  () =>
    (sourceFromCategoryEnabled.value && !!inferredFundingSource.value) ||
    (sourceFromGoalEnabled.value && !!inferredFundingGoal.value),
);

// Initialize transfer fields from existing categories when editing a transfer
if (locTrnsx.transactionType === 'transfer' && locTrnsx.categories?.length === 2) {
  const neg = locTrnsx.categories.find((c) => c.amount < 0);
  const pos = locTrnsx.categories.find((c) => c.amount >= 0);
  if (neg) transferSource.value = neg.category;
  if (pos) transferDest.value = pos.category;
}

// Names of every income-kind category in the loaded budget. Used to filter
// `categoryOptions` (which is just a flat string list) into "income" vs
// "non-income" buckets without relying on string equality with the literal
// "Income" — categories named "Bonus", "Side Hustle" etc. that live in an
// income-kind group are now correctly treated as income.
const incomeCategoryNames = computed<Set<string>>(() => {
  const names = new Set<string>();
  const groupList = familyStore.currentGroups;
  for (const cat of budget.value?.categories || []) {
    if (isIncomeCategory(cat, groupList)) names.add(cat.name);
  }
  return names;
});

const allNonIncomeCategories = computed(() =>
  props.categoryOptions
    .filter((c) => !incomeCategoryNames.value.has(c))
    .sort((a, b) => a.localeCompare(b)),
);

const transferSourceOptions = computed(() => {
  const cats = allNonIncomeCategories.value;
  if (!transferSourceFilterText.value) return cats;
  const needle = transferSourceFilterText.value.toLowerCase();
  return cats.filter((c) => c.toLowerCase().includes(needle));
});

const transferDestOptions = computed(() => {
  const cats = allNonIncomeCategories.value.filter((c) => c !== transferSource.value);
  if (!transferDestFilterText.value) return cats;
  const needle = transferDestFilterText.value.toLowerCase();
  return cats.filter((c) => c.toLowerCase().includes(needle));
});

function onTransferSourceFilter(val: string, update: (fn: () => void) => void) {
  update(() => { transferSourceFilterText.value = val; });
}

function onTransferDestFilter(val: string, update: (fn: () => void) => void) {
  update(() => { transferDestFilterText.value = val; });
}

const merchantNames = computed(() => merchantStore.merchantNames);
const showMerchantSuggestions = ref(false);
const merchantInputRef = ref<HTMLElement | null>(null);
const filteredMerchants = computed(() => {
  const val = locTrnsx.merchant?.trim();
  if (!val) return merchantNames.value.slice(0, 20);
  const needle = val.toLowerCase();
  return merchantNames.value.filter((m) => m.toLowerCase().includes(needle)).slice(0, 20);
});

function onMerchantTyping() {
  showMerchantSuggestions.value = !!(locTrnsx.merchant && locTrnsx.merchant.length > 0);
}

function selectMerchant(name: string) {
  locTrnsx.merchant = name;
  showMerchantSuggestions.value = false;
}

function hideMerchantSuggestions() {
  // Small delay so mousedown on menu item fires before we hide
  setTimeout(() => {
    showMerchantSuggestions.value = false;
  }, 150);
}

const availableMonths = computed(() => {
  return budgetStore.availableBudgetMonths;
});

const remainingCategories = computed(() => {
  const alreadyChosen = new Set(locTrnsx.categories.map((entry) => entry.category));
  const incomeNames = incomeCategoryNames.value;
  return props.categoryOptions
    .filter((str) => {
      if (alreadyChosen.has(str)) return false;
      // Income transactions only show income-kind categories; expense txs hide them.
      return locTrnsx.isIncome ? incomeNames.has(str) : !incomeNames.has(str);
    })
    .sort((a, b) => a.localeCompare(b));
});

const categoryFilterText = ref('');
const filteredCategories = computed(() => {
  if (!categoryFilterText.value) return remainingCategories.value;
  const needle = categoryFilterText.value.toLowerCase();
  return remainingCategories.value.filter((c) => c.toLowerCase().includes(needle));
});

function onCategoryFilter(val: string, update: (fn: () => void) => void) {
  update(() => {
    categoryFilterText.value = val;
  });
}

const remainingSplit = computed(() => {
  if (locTrnsx.categories.length <= 1) return 0;
  let remaining = locTrnsx.amount;
  locTrnsx.categories.forEach((c) => {
    remaining = remaining - c.amount;
  });
  return Math.round(remaining * 100) / 100.0;
});

// Keep the lone split's amount in sync with the transaction total whenever
// there's a single category. With the per-split amount field hidden in that
// case, this is the source of truth for the saved value — without it, an
// edit on an existing transaction would persist a stale split amount that
// no longer matches the new total. Skips transfer mode (which manages its
// own signed-pair categories via transferSource/transferDest).
watch(
  [
    () => locTrnsx.amount,
    () => locTrnsx.categories.length,
    () => transactionMode.value,
  ],
  () => {
    if (transactionMode.value === 'transfer') return;
    if (locTrnsx.categories.length !== 1) return;
    const only = locTrnsx.categories[0];
    if (!only) return;
    if (only.amount !== locTrnsx.amount) {
      only.amount = locTrnsx.amount;
    }
  },
  { immediate: true },
);

onMounted(async () => {
  budget.value = await dataAccess.getBudget(props.budgetId);
  if (!budget.value) {
    console.error(`Budget ${props.budgetId} not found`);
  }

  if (availableMonths.value.length === 0) {
    showSnackbar('No budgets available. Please create a budget in the Dashboard.', 'warning');
  } else {
    if (!locTrnsx.budgetMonth || !availableMonths.value.includes(locTrnsx.budgetMonth)) {
      locTrnsx.budgetMonth = availableMonths.value[0] || '';
    }
  }

  transactions.value = await dataAccess.getTransactions(props.budgetId);
  emit('update-transactions', transactions.value);

  if (budget.value?.merchants && budget.value.merchants.length > 0) {
    merchantStore.updateMerchantsFromCounts(Object.fromEntries(budget.value.merchants.map((m) => [m.name, m.usageCount])));
  } else {
    merchantStore.updateMerchants(transactions.value);
  }

  isInitialized.value = true;

  if (familyStore.selectedEntityId) {
    goalList.value = listGoals(familyStore.selectedEntityId);
  }
});

function scrollToNoteField(event: FocusEvent) {
  if (isMobile.value) {
    setTimeout(() => {
      (event.target as HTMLElement).scrollIntoView({ behavior: 'smooth', block: 'center' });
    }, 300); // Delay to account for keyboard appearance
  }
}

function updateBudgetMonth() {
  if (!locTrnsx.budgetMonth && locTrnsx.date) {
    const potentialMonth = locTrnsx.date.slice(0, 7);
    if (availableMonths.value.includes(potentialMonth)) {
      locTrnsx.budgetMonth = potentialMonth;
    } else {
      locTrnsx.budgetMonth = availableMonths.value[0] || '';
    }
  }
}

function addSplit() {
  locTrnsx.categories.push({ category: '', amount: 0 });
}

function removeSplit(index: number) {
  if (locTrnsx.categories.length > 1) {
    locTrnsx.categories.splice(index, 1);
  }
  if (locTrnsx.categories.length === 1) {
    const firstCategory = locTrnsx.categories[0];
    if (firstCategory) firstCategory.amount = locTrnsx.amount;
  }
}

async function resetMatch() {
  if (!locTrnsx.id) return;

  isLoading.value = true;
  try {
    // Update local transaction to reset imported fields
    locTrnsx.status = 'U';
    locTrnsx.accountSource = '';
    locTrnsx.accountNumber = '';
    locTrnsx.postedDate = '';
    locTrnsx.checkNumber = '';
    locTrnsx.importedMerchant = '';

    // Save the updated transaction
    if (!budget.value) {
      throw new Error(`Budget ${props.budgetId} not found`);
    }

    const entityId = budget.value?.entityId;
    const currentBudgetMonth = budget.value?.month;
    const targetBudgetMonth = locTrnsx.budgetMonth;
    let targetBudget = budget.value;

    if (currentBudgetMonth !== targetBudgetMonth && entityId && familyStore.family) {
      targetBudget = await createBudgetForMonth(targetBudgetMonth, familyStore.family.id, props.userId, entityId);
    }

    if (targetBudget) {
      const savedTransaction = await dataAccess.saveTransaction(targetBudget, locTrnsx);
      const index = transactions.value.findIndex((t) => t.id === savedTransaction.id);
      if (index >= 0) {
        transactions.value[index] = savedTransaction;
      } else {
        transactions.value.push(savedTransaction);
      }
      emit('update-transactions', transactions.value);
      emit('save', savedTransaction);
      showSnackbar('Transaction match reset successfully', 'success');
    } else {
      showSnackbar('Could not reset match — budget does not exist.', 'negative');
    }
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error resetting transaction match:', err.message);
    showSnackbar(`Error resetting transaction match: ${err.message}`, 'negative');
  } finally {
    isLoading.value = false;
  }
}

async function save() {
  if (transactionMode.value !== 'transfer' && remainingSplit.value !== 0) return;
  if (!form.value) return;

  const valid = await form.value.validate();
  if (valid) {
    // Transfer-specific validation
    if (transactionMode.value === 'transfer') {
      if (!transferSource.value || !transferDest.value) {
        showSnackbar('Please select both source and destination categories', 'negative');
        return;
      }
      if (transferSource.value === transferDest.value) {
        showSnackbar('Source and destination must be different categories', 'negative');
        return;
      }
      if (!locTrnsx.amount || locTrnsx.amount <= 0) {
        showSnackbar('Transfer amount must be greater than 0', 'negative');
        return;
      }
      // Set transfer fields
      locTrnsx.transactionType = 'transfer';
      locTrnsx.isIncome = false;
      locTrnsx.merchant = 'Transfer';
      locTrnsx.recurring = false;
      locTrnsx.categories = [
        { category: transferSource.value, amount: -locTrnsx.amount },
        { category: transferDest.value, amount: locTrnsx.amount },
      ];
    }

    // Category-default funding source: if the chosen destination category
    // has a `fundingSourceCategory` and the user left the toggle enabled,
    // rewrite the expense into a transfer from the source category. This
    // mirrors the goal-funded path below but is driven by category config
    // rather than an explicit goal selection.
    if (
      transactionMode.value === 'expense' &&
      !locTrnsx.fundedByGoalId &&
      sourceFromCategoryEnabled.value &&
      inferredFundingSource.value
    ) {
      const source = inferredFundingSource.value;
      const total = Math.abs(locTrnsx.amount);
      if (!total) {
        showSnackbar('Amount must be greater than 0', 'negative');
        return;
      }
      const destCategories = locTrnsx.categories
        .filter((c) => c.category)
        .map((c) => ({ category: c.category, amount: Math.abs(c.amount || total) }));
      if (destCategories.length === 0) {
        destCategories.push({ category: '', amount: total });
      }
      if (destCategories.some((c) => c.category === source)) {
        showSnackbar('Destination category cannot be its own funding source', 'negative');
        return;
      }
      const destSum = destCategories.reduce((s, c) => s + c.amount, 0);
      locTrnsx.categories = [
        { category: source, amount: -destSum },
        ...destCategories,
      ];
      locTrnsx.transactionType = 'transfer';
      locTrnsx.isIncome = false;
      locTrnsx.amount = destSum;
    }

    // Category-default goal funding source: when the destination category
    // has a `fundingSourceGoalId` and no transaction-level `fundedByGoalId`
    // was set, fold in the inferred goal id and let the next branch perform
    // the conversion. Skipped if the cat-level category branch above already
    // converted (transactionType === 'transfer'). Mutually exclusive with
    // `fundingSourceCategory` at the DB level.
    if (
      transactionMode.value === 'expense' &&
      locTrnsx.transactionType !== 'transfer' &&
      !locTrnsx.fundedByGoalId &&
      sourceFromGoalEnabled.value &&
      inferredFundingGoal.value
    ) {
      locTrnsx.fundedByGoalId = inferredFundingGoal.value.id;
    }

    // Goal-funded expense: leave the transaction as a standard expense
    // (counts toward the destination category's spent/available) and persist
    // `fundedByGoalId` so the goal's savedToDate / spentToDate is derived
    // server-side. We previously rewrote this into a transfer at save time
    // — that nets the category to zero and shows the row as a transfer
    // arrow instead of an expense, which doesn't match user intent. Just
    // validate inputs here.
    if (transactionMode.value !== 'transfer' && locTrnsx.fundedByGoalId) {
      const goal = goalList.value.find((g) => g.id === locTrnsx.fundedByGoalId);
      if (!goal) {
        showSnackbar('Selected funding goal not found', 'negative');
        return;
      }
      const total = Math.abs(locTrnsx.amount);
      if (!total) {
        showSnackbar('Amount must be greater than 0 when funding from a goal', 'negative');
        return;
      }
    }

    isLoading.value = true;
    try {
      if (transactionMode.value !== 'transfer' && locTrnsx.categories.length === 1 && locTrnsx.categories[0]?.amount === 0) {
        const firstCategory = locTrnsx.categories[0];
        if (firstCategory) firstCategory.amount = locTrnsx.amount;
      }
      // Also sync the other direction. If the user typed the amount only into
      // a category split row (or an upstream import populated splits but left
      // the top-level amount at 0), copy from splits → transaction.amount so
      // the whole-budget transactions list — which falls back to
      // transaction.amount when no categoryName scope is available — doesn't
      // render $0 against a $X split.
      if (transactionMode.value !== 'transfer' && (!locTrnsx.amount || locTrnsx.amount === 0)) {
        const splitTotal = (locTrnsx.categories || []).reduce(
          (s, c) => s + Math.abs(Number(c.amount) || 0),
          0,
        );
        if (splitTotal !== 0) locTrnsx.amount = splitTotal;
      }
      locTrnsx.userId = props.userId;
      if (!locTrnsx.status) {
        locTrnsx.status = 'U';
      }
      if (!budget.value) {
        throw new Error(`Budget ${props.budgetId} not found`);
      }

      const entityId = budget.value?.entityId;
      const currentBudgetMonth = budget.value?.month;
      const targetBudgetMonth = locTrnsx.budgetMonth;
      let moved = false;
      let targetBudget = budget.value;
      if (currentBudgetMonth !== targetBudgetMonth && entityId && familyStore.family) {
        targetBudget = await createBudgetForMonth(targetBudgetMonth, familyStore.family.id, props.userId, entityId);
      }

      if (targetBudget) {
        if (currentBudgetMonth !== targetBudgetMonth && locTrnsx.id) {
          await dataAccess.deleteTransaction(budget.value, locTrnsx.id);
          moved = true;
        }

        const savedTransaction = await dataAccess.saveTransaction(targetBudget, locTrnsx);
        const index = transactions.value.findIndex((t) => t.id === savedTransaction.id);
        if (moved) {
          if (index >= 0) {
            transactions.value.splice(index, 1);
          }
        } else {
          if (index >= 0) {
            transactions.value[index] = savedTransaction;
          } else {
            transactions.value.push(savedTransaction);
          }
        }
        emit('update-transactions', transactions.value);

        emit('save', savedTransaction);
        showSnackbar('Transaction saved successfully', 'success');

        // Prompt to propagate recurring transaction to future budgets
        if (savedTransaction.recurring && savedTransaction.recurringInterval) {
          promptRecurringPropagation(savedTransaction);
        }
      } else {
        showSnackbar('Could not save transaction — budget does not exist.', 'negative');
      }
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error saving transaction:', err.message);
      showSnackbar(`Error saving transaction: ${err.message}`, 'negative');
    } finally {
      isLoading.value = false;
    }
  } else {
    console.log('Form validation failed');
    showSnackbar('Form validation failed', 'negative');
  }
}

/**
 * Generate transaction(s) for a given month based on the recurring interval.
 * Daily/Weekly/Bi-Weekly produce multiple transactions per month.
 * Monthly/Quarterly/Bi-Annually/Yearly produce one (or zero if the interval
 * doesn't land in that month).
 */
function generateTransactionsForMonth(source: Transaction, targetMonth: string): Transaction[] {
  const interval = source.recurringInterval;
  if (!interval) return [];

  // For intervals that can produce multiple transactions per month, use the dedicated generators
  if (interval === 'Daily') return generateDailyTransactions(source, targetMonth);
  if (interval === 'Weekly') return generateWeeklyTransactions(source, targetMonth);
  if (interval === 'Bi-Weekly') return generateBiWeeklyTransactions(source, targetMonth);

  // For longer intervals, check whether the target month is a valid recurrence
  const sourceDate = new Date(source.date);
  const [targetYear, targetMonthNum] = targetMonth.split('-').map(Number);
  const monthsDiff = (targetYear - sourceDate.getFullYear()) * 12 + (targetMonthNum - (sourceDate.getMonth() + 1));

  if (monthsDiff <= 0) return []; // Don't propagate to same or earlier months

  let shouldInclude = false;
  if (interval === 'Monthly') {
    shouldInclude = true;
  } else if (interval === 'Quarterly') {
    shouldInclude = monthsDiff % 3 === 0;
  } else if (interval === 'Bi-Annually') {
    shouldInclude = monthsDiff % 6 === 0;
  } else if (interval === 'Yearly') {
    shouldInclude = monthsDiff % 12 === 0;
  }

  if (!shouldInclude) return [];

  const newDate = adjustTransactionDate(source.date, targetMonth, interval);
  return [{
    ...source,
    id: uuidv4(),
    date: newDate,
    budgetMonth: targetMonth,
    status: undefined,
    postedDate: undefined,
    importedMerchant: undefined,
    accountNumber: undefined,
    accountSource: undefined,
  }];
}

function promptRecurringPropagation(savedTransaction: Transaction) {
  const currentMonth = savedTransaction.budgetMonth || savedTransaction.date?.slice(0, 7);
  if (!currentMonth) return;

  // Find future months that already have budgets
  const futureMonths = budgetStore.availableBudgetMonths
    .filter((m: string) => m > currentMonth)
    .sort();

  if (futureMonths.length === 0) return;

  // Prompt the user
  $q.dialog({
    title: 'Add to Future Budgets?',
    message: `This recurring transaction (${savedTransaction.recurringInterval}) can be added to ${futureMonths.length} existing future budget${futureMonths.length > 1 ? 's' : ''} (${futureMonths[0]}${futureMonths.length > 1 ? ' – ' + futureMonths[futureMonths.length - 1] : ''}).`,
    cancel: { flat: true, label: 'No thanks' },
    ok: { unelevated: true, color: 'primary', label: 'Add to future budgets' },
    persistent: false,
  }).onOk(() => {
    void propagateRecurring(savedTransaction, futureMonths);
  });
}

async function propagateRecurring(savedTransaction: Transaction, futureMonths: string[]) {
  try {
    let addedCount = 0;
    for (const month of futureMonths) {
      const newTransactions = generateTransactionsForMonth(savedTransaction, month);
      if (newTransactions.length === 0) continue;

      // Find the budget for this month
      const targetBudget = Array.from(budgetStore.budgets.values()).find(
        (b) => b.month === month && b.entityId === budget.value?.entityId,
      );
      if (!targetBudget) continue;

      await dataAccess.batchSaveTransactions(targetBudget.budgetId, targetBudget, newTransactions);
      addedCount += newTransactions.length;
    }
    if (addedCount > 0) {
      showSnackbar(`Added ${addedCount} recurring transaction${addedCount > 1 ? 's' : ''} to future budgets`, 'success');
    }
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error propagating recurring transactions:', err.message);
    showSnackbar(`Error adding to future budgets: ${err.message}`, 'negative');
  }
}

async function deleteTransaction() {
  if (!locTrnsx.id) return;

  isLoading.value = true;
  try {
    if (!budget.value) {
      throw new Error(`Budget ${props.budgetId} not found`);
    }
    await dataAccess.deleteTransaction(budget.value, locTrnsx.id);

    transactions.value = transactions.value.filter((t) => t.id !== locTrnsx.id);
    emit('update-transactions', transactions.value);

    emit('cancel');
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error deleting transaction:', err.message);
  } finally {
    isLoading.value = false;
  }
}

defineExpose({
  save,
  deleteTransaction,
  isLoading,
});

function showSnackbar(text: string, color = 'success') {
  $q.notify({
    message: text,
    color,
    position: 'bottom',
    timeout: 3000,
    actions: [{ label: 'Close', color: 'white', handler: () => {} }],
  });
}
</script>

<style scoped>
.transaction-form {
  width: 100%;
  display: flex;
  flex-direction: column;
}

.transaction-form__field {
  width: 100%;
}

.transaction-form__section {
  border-top: 1px solid var(--q-color-grey-3);
  padding-top: 12px;
  margin-top: 12px;
}

.transaction-form__section-title {
  font-size: 0.75rem;
  letter-spacing: 0.1em;
}

.split-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.split-row__category {
  flex: 1;
  min-width: 0;
}

.split-row__amount {
  flex: 0 0 35%;
}

.split-row__remove {
  flex-shrink: 0;
}

.transaction-form__amount .q-field__native,
.split-row__amount .q-field__native {
  text-align: right;
}

.transaction-form__type-toggle {
  font-size: 0.85rem;
  border-radius: var(--radius-sm);
  overflow: hidden;
}

.transaction-form__type-toggle :deep(.q-btn) {
  border-radius: 0 !important;
  padding: 6px 16px;
  font-weight: 500;
}

.transaction-form__type-toggle :deep(.q-btn:first-child) {
  border-radius: var(--radius-sm) 0 0 var(--radius-sm) !important;
}

.transaction-form__type-toggle :deep(.q-btn:last-child) {
  border-radius: 0 var(--radius-sm) var(--radius-sm) 0 !important;
}
</style>

<style>
.merchant-suggestions-menu {
  max-height: 240px;
}
.merchant-suggestions-menu .q-item {
  min-height: 36px;
  padding: 4px 12px;
}
</style>
