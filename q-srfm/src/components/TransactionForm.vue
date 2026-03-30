<!-- src/components/TransactionForm.vue -->
<template>
  <div v-if="isInitialized && locTrnsx?.categories">
    <q-form ref="form" class="transaction-form q-gutter-lg" @submit.prevent="save">
      <div class="transaction-form__field">
        <q-input
          v-model="locTrnsx.date"
          type="date"
          :rules="requiredField"
          label="Date *"
          aria-required="true"
          stack-label
          dense
          borderless
          @input="updateBudgetMonth"
        />
      </div>

      <div class="transaction-form__field">
        <q-select
          v-model="locTrnsx.merchant"
          :options="filteredMerchants"
          :rules="requiredField"
          label="Merchant *"
          aria-required="true"
          stack-label
          dense
          borderless
          clearable
          use-input
          input-debounce="0"
          new-value-mode="add-unique"
          @filter="onMerchantFilter"
          @input-value="onMerchantInput"
        />
      </div>

      <div class="transaction-form__field">
        <Currency-Input
          v-model="locTrnsx.amount"
          label="Amount"
          stack-label
          dense
          borderless
          class="transaction-form__amount"
        />
      </div>

      <div class="transaction-form__section">
        <div class="transaction-form__section-title row items-center justify-between">
          <div class="text-caption text-uppercase text-grey-6">Categories</div>
          <q-btn flat color="primary" dense icon="add" label="Add Split" @click="addSplit" />
        </div>
        <div class="transaction-form__splits q-gutter-md">
          <div v-for="(split, index) in locTrnsx.categories" :key="index" class="row items-end q-gutter-sm">
            <div class="col">
              <q-select
                v-model="split.category"
                :options="filteredCategories"
                label="Category *"
                :rules="requiredField"
                aria-required="true"
                stack-label
                dense
                borderless
                menu-icon=""
                class="full-width"
                required
                use-input
                input-debounce="0"
                @filter="onCategoryFilter"
              />
            </div>
            <div class="col-auto">
              <Currency-Input v-model="split.amount" label="Split Amount" stack-label dense borderless />
            </div>
            <div class="col-auto">
              <q-btn dense flat icon="close" color="negative" @click="removeSplit(index)" />
            </div>
          </div>
          <q-banner v-if="locTrnsx.categories.length > 1 && remainingSplit !== 0" :type="remainingSplit < 0 ? 'negative' : 'warning'">
            <div v-if="remainingSplit > 0">Remaining ${{ toDollars(toCents(Math.abs(remainingSplit))) }}</div>
            <div v-else>Over allocated ${{ toDollars(toCents(Math.abs(remainingSplit))) }} ({{ toDollars(toCents(locTrnsx.amount + remainingSplit)) }})</div>
          </q-banner>
        </div>
      </div>

      <div class="transaction-form__field">
        <div class="row items-center justify-between">
          <div class="text-caption text-muted">Type</div>
          <ToggleButton v-model="locTrnsx.isIncome" active-text="Income" inactive-text="Expense" />
        </div>
      </div>

      <div class="transaction-form__field">
        <q-input
          type="textarea"
          v-model="locTrnsx.notes"
          label="Notes"
          stack-label
          borderless
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
          borderless
          :disable="availableMonths.length === 0"
        />
      </div>

      <div class="transaction-form__field" v-if="availableMonths.length === 0">
        <q-banner type="warning" class="q-ma-none">No budgets available. Please create a budget in the Dashboard.</q-banner>
      </div>

      <div class="transaction-form__field">
        <q-select
          v-model="locTrnsx.fundedByGoalId"
          :options="goalOptions"
          label="Fund from Goal"
          stack-label
          dense
          borderless
          emit-value
          map-options
          clearable
        />
      </div>

      <div class="transaction-form__field">
        <q-checkbox v-model="locTrnsx.recurring" label="Recurring" />
      </div>
      <div class="transaction-form__field" v-if="locTrnsx.recurring">
        <q-select
          v-model="locTrnsx.recurringInterval"
          :options="intervals"
          label="Recurring Interval"
          stack-label
          dense
          borderless
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
          <q-btn variant="text" color="warning" :loading="isLoading" @click="resetMatch">Reset Match</q-btn>
        </div>
      </div>
    </q-form>
  </div>
  <div v-else>Loading...</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue';
import { useQuasar } from 'quasar';
import { dataAccess } from '../dataAccess';
import type { Budget, Transaction, Goal } from '../types';
import { toCents, toDollars, todayISO, currentMonthISO } from '../utils/helpers';
import CurrencyInput from './CurrencyInput.vue';
import ToggleButton from './ToggleButton.vue';
import { QForm } from 'quasar';
import { useMerchantStore } from '../store/merchants';
import { useBudgetStore } from '../store/budget';
import { useGoals } from '../composables/useGoals';
import { useFamilyStore } from '../store/family';
import { createBudgetForMonth } from '../utils/budget';

const merchantStore = useMerchantStore();
const budgetStore = useBudgetStore();
const $q = useQuasar();
const familyStore = useFamilyStore();
const { listGoals, addGoalSpend } = useGoals();

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

const merchantNames = computed(() => merchantStore.merchantNames);
const merchantFilterText = ref('');
const filteredMerchants = computed(() => {
  if (!merchantFilterText.value) return merchantNames.value;
  const needle = merchantFilterText.value.toLowerCase();
  return merchantNames.value.filter((m) => m.toLowerCase().includes(needle));
});

function onMerchantFilter(val: string, update: (fn: () => void) => void) {
  update(() => {
    merchantFilterText.value = val;
  });
}

function onMerchantInput(val: string) {
  locTrnsx.merchant = val;
}

const availableMonths = computed(() => {
  return budgetStore.availableBudgetMonths;
});

const remainingCategories = computed(() => {
  const categoryNames = new Set(locTrnsx.categories.map((entry) => entry.category));
  return props.categoryOptions
    .filter((str) => {
      if (locTrnsx.isIncome) {
        return str === 'Income' && !categoryNames.has(str);
      } else {
        return str !== 'Income' && !categoryNames.has(str);
      }
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
      showSnackbar('Could not reset match, Budget does not exist!', 'negative');
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
  if (remainingSplit.value !== 0) return;
  if (!form.value) return;

  const valid = await form.value.validate();
  if (valid) {
    isLoading.value = true;
    try {
      if (locTrnsx.categories.length === 1 && locTrnsx.categories[0]?.amount === 0) {
        const firstCategory = locTrnsx.categories[0];
        if (firstCategory) firstCategory.amount = locTrnsx.amount;
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

        if (locTrnsx.fundedByGoalId) {
          addGoalSpend(locTrnsx.fundedByGoalId, savedTransaction.id, Math.abs(savedTransaction.amount), savedTransaction.date);
        }
        emit('save', savedTransaction);
        showSnackbar('Transaction saved successfully', 'success');
      } else {
        showSnackbar('Could not save Transaction, Budget does not exist!', 'negative');
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

.transaction-form__splits .row {
  align-items: flex-end;
}

.transaction-form__amount .q-field__native,
.transaction-form__splits .q-field__native {
  text-align: right;
}
</style>
