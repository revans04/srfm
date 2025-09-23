<!-- src/components/TransactionForm.vue -->
<template>
  <div v-if="isInitialized && locTrnsx?.categories">
    <div class="row q-pa-none">
      <div class="col col-6">
        <q-btn flat type="submit" color="primary" :loading="isLoading" @click="save" label="Save" />
      </div>
      <div class="col text-right col-6">
        <q-btn v-if="showCancel" @click="emit('cancel')" flat type="button" color="primary" :loading="isLoading" label="Cancel" />
      </div>
    </div>
    <q-form ref="form" @submit.prevent="save">
      <div class="row form-row">
        <div class="col form-col-label">Date</div>
        <div class="col form-col col-auto">
          <q-input v-model="locTrnsx.date" type="date" :rules="requiredField" @input="updateBudgetMonth" dense borderless class="text-right" />
        </div>
      </div>
      <div class="row form-row">
        <div class="col form-col-label q-pr-xl">Merchant</div>
        <div class="col form-col col-auto" style="min-width: 220px">
          <q-select
            v-model="locTrnsx.merchant"
            v-model:input-value="merchantInput"
            :options="merchantNames"
            :rules="requiredField"
            dense
            borderless
            use-input
            input-debounce="0"
            new-value-mode="add"
            clearable
            menu-icon=""
            class="text-right"
          />
        </div>
      </div>
      <div class="row form-row">
        <div class="col form-col-label col-auto">Total</div>
        <div class="col form-col">
          <Currency-Input v-model="locTrnsx.amount" class="text-right" borderless></Currency-Input>
        </div>
      </div>

      <div class="form-row q-my-xl ms-n3 q-py-md">
        <div v-for="(split, index) in locTrnsx.categories" :key="index">
          <div class="row no-gutters">
            <div class="col form-col-label q-pr-sm col-auto" :class="locTrnsx.categories.length > 1 ? '' : 'q-pb-md'">
              <q-icon color="negative" @click="removeSplit(index)" name="close" />
            </div>
            <div class="col q-pr-sm">
              <q-select
                v-model="split.category"
                :options="remainingCategories"
                label="Category"
                :rules="requiredField"
                dense
                borderless
                menu-icon=""
                class="full-width"
                required
              />
            </div>
            <div class="col" v-if="locTrnsx.categories.length > 1">
              <Currency-Input v-model="split.amount" class="text-right full-width" borderless></Currency-Input>
            </div>
          </div>
        </div>
        <div v-if="locTrnsx.categories.length > 1 && remainingSplit !== 0">
          <q-banner v-if="remainingSplit !== 0" :type="remainingSplit < 0 ? 'error' : 'warning'" class="q-mb-lg">
            <div v-if="remainingSplit > 0">Remaining ${{ toDollars(toCents(Math.abs(remainingSplit))) }}</div>
            <div v-else>Over allocated ${{ toDollars(toCents(Math.abs(remainingSplit))) }} ({{ toDollars(toCents(locTrnsx.amount + remainingSplit)) }})</div>
          </q-banner>
        </div>
        <div>
          <q-btn flat color="primary" @click="addSplit()">Add Split</q-btn>
        </div>
      </div>

      <!-- Moved Type toggle above Notes -->
      <div class="row form-row">
        <div class="col form-col-label col-auto">Type</div>
        <div class="col text-right">
          <ToggleButton v-model="locTrnsx.isIncome" active-text="Income" inactive-text="Expense" />
        </div>
      </div>

      <div class="v-row form-row">
        <q-input
          v-model="locTrnsx.notes"
          type="textarea"
          autogrow
          label="Notes"
          borderless
          @focus="scrollToNoteField"
        />
      </div>

      <div class="row rounded-5 bg-light q-mb-sm justify-center">
        <div class="col font-weight-bold col-auto" justify="center">Budget</div>
        <div class="col q-pa-none q-ma-none text-right">
          <q-select
            v-model="locTrnsx.budgetMonth"
            :options="availableMonths"
            :rules="requiredField"
            dense
            borderless
            :disabled="availableMonths.length === 0"
          />
        </div>
      </div>

      <div class="row q-mt-sm" v-if="availableMonths.length === 0">
        <div class="col">
          <q-banner type="warning">No budgets available. Please create a budget in the Dashboard.</q-banner>
        </div>
      </div>

      <div class="row form-row">
        <div class="col form-col-label col-auto">Fund from Goal</div>
        <div class="col text-right">
          <q-select v-model="locTrnsx.fundedByGoalId" :options="goalOptions" dense borderless emit-value map-options clearable />
        </div>
      </div>

      <q-checkbox v-model="locTrnsx.recurring" label="Recurring"></q-checkbox>
      <q-select v-if="locTrnsx.recurring" v-model="locTrnsx.recurringInterval" :options="intervals" label="Recurring Interval" dense borderless />

      <!-- Imported Transaction Fields (Shown only if matched) -->
      <div v-if="locTrnsx.status && (locTrnsx.status === 'C' || locTrnsx.status === 'R')" class="q-mt-lg">
        <div class="row form-row">
          <div class="col form-col-label">Posted Date</div>
          <div class="col form-col">
            <q-input v-model="locTrnsx.postedDate" type="date" dense borderless readonly></q-input>
          </div>
        </div>
        <div class="row form-row">
          <div class="col form-col-label q-pr-xl">Imported Merchant</div>
          <div class="col form-col" style="min-width: 150px">
            <q-input v-model="locTrnsx.importedMerchant" dense borderless readonly></q-input>
          </div>
        </div>
        <div class="row form-row">
          <div class="col form-col-label">Account Source</div>
          <div class="col form-col">
            <q-input v-model="locTrnsx.accountSource" dense borderless readonly></q-input>
          </div>
        </div>
        <div class="row form-row">
          <div class="col form-col-label">Account Number</div>
          <div class="col form-col">
            <q-input v-model="locTrnsx.accountNumber" dense borderless readonly></q-input>
          </div>
        </div>
        <div class="row form-row">
          <div class="col form-col-label">Check Number</div>
          <div class="col form-col">
            <q-input v-model="locTrnsx.checkNumber" dense borderless readonly></q-input>
          </div>
        </div>
        <div class="row form-row">
          <div class="col form-col-label">Status</div>
          <div class="col form-col">
            <q-input v-model="locTrnsx.status" dense borderless readonly></q-input>
          </div>
        </div>
        <div class="row form-row">
          <div class="col">
            <q-btn variant="text" color="warning" :loading="isLoading" @click="resetMatch">Reset Match</q-btn>
          </div>
        </div>
      </div>

      <div class="text-center">
        <q-btn v-if="locTrnsx.id" variant="text" color="negative" :loading="isLoading" @click="deleteTransaction" label="Delete Transaction" />
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
  showCancel?: boolean;
  categoryOptions: string[];
  budgetId: string;
  userId: string;
  loading?: boolean;
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
const merchantInput = ref('');

watch(
  () => locTrnsx.merchant,
  (val) => {
    merchantInput.value = val || '';
    if (val && val.trim()) {
      merchantStore.ensureMerchant(val);
    }
  },
  { immediate: true },
);

watch(merchantInput, (val) => {
  if ((locTrnsx.merchant || '') !== val) {
    locTrnsx.merchant = val;
  }
});

const availableMonths = computed(() => {
  return budgetStore.availableBudgetMonths;
});

const isLastMonth = computed(() => {
  return [...availableMonths.value].sort((a, b) => b.localeCompare(a))[0] == locTrnsx.budgetMonth;
});

const remainingCategories = computed(() => {
  const categoryNames = new Set(locTrnsx.categories.map((entry) => entry.category));
  return props.categoryOptions.filter((str) => {
    if (locTrnsx.isIncome) {
      return str === 'Income' && !categoryNames.has(str);
    } else {
      return str !== 'Income' && !categoryNames.has(str);
    }
  });
});

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
      const savedTransaction = await dataAccess.saveTransaction(targetBudget, locTrnsx, !isLastMonth.value);
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
          await dataAccess.deleteTransaction(budget.value, locTrnsx.id, !isLastMonth.value);
          moved = true;
        }

        const savedTransaction = await dataAccess.saveTransaction(targetBudget, locTrnsx, !isLastMonth.value);
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
    await dataAccess.deleteTransaction(budget.value, locTrnsx.id, !isLastMonth.value);

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

<style>
.form-row {
  background-color: #f5f5f5;
  padding: 4px 8px;
  align-items: center;
  margin-bottom: 4px;
}

.form-col-label {
  font-weight: 600;
  display: flex;
  align-items: center;
}

.text-right.v-text-field input,
.text-right .v-input__control .v-field__input,
.text-right .v-autocomplete .v-field__input .v-autocomplete__selection-text,
.text-right .v-select .v-field__input .v-select__selection-text,
.text-right .v-textarea .v-field__input {
  text-align: right !important;
}
</style>
