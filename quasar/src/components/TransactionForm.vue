<!-- src/components/TransactionForm.vue -->
<template>
  <div v-if="isInitialized && locTrnsx?.categories">
    <div>
      <q-row class="q-pa-none">
        <q-col cols="6">
          <q-btn flat color="primary" :loading="saving" @click="save" label="Save" />
        </q-col>
        <q-col cols="6" class="text-right">
          <q-btn
            v-if="showCancel"
            flat
            color="primary"
            :loading="saving"
            @click="emit('cancel')"
            label="Cancel"
          />
        </q-col>
      </q-row>
    </div>
    <q-form ref="form" @submit="save">
      <q-row class="form-row">
        <q-col class="form-col-label">Date</q-col>
        <q-col cols="auto" class="form-col">
          <q-input
            v-model="locTrnsx.date"
            type="date"
            :rules="requiredField"
            @update:model-value="updateBudgetMonth"
            flat
            dense
            class="text-right"
          />
        </q-col>
      </q-row>
      <q-row class="form-row">
        <q-col class="form-col-label q-pr-sm">Merchant</q-col>
        <q-col cols="auto" class="form-col" style="min-width: 150px">
          <q-select
            v-model="locTrnsx.merchant"
            :options="merchantNames"
            :rules="requiredField"
            dense
            flat
            use-input
            hide-dropdown-icon
            class="text-right"
          />
        </q-col>
      </q-row>
      <q-row class="form-row">
        <q-col cols="auto" class="form-col-label">Total</q-col>
        <q-col class="form-col">
          <Currency-Input v-model="locTrnsx.amount" class="text-right" flat />
        </q-col>
      </q-row>

      <div class="form-row q-my-md q-ml-xs q-py-md">
        <div v-for="(split, index) in locTrnsx.categories" :key="index">
          <q-row no-wrap>
            <q-col
              cols="auto"
              class="form-col-label q-pr-sm"
              :class="locTrnsx.categories.length > 1 ? '' : 'q-pb-md'"
            >
              <q-icon color="negative" @click="removeSplit(index)" name="mdi-close" />
            </q-col>
            <q-col>
              <q-select
                v-model="split.category"
                :options="remainingCategories"
                label="Category"
                :rules="requiredField"
                dense
                flat
                use-input
                hide-dropdown-icon
                required
              />
            </q-col>
            <q-col v-if="locTrnsx.categories.length > 1" cols="4" class="form-col">
              <Currency-Input v-model="split.amount" class="text-right" flat />
            </q-col>
            <q-col v-if="locTrnsx.categories.length > 1" cols="auto" class="form-col"></q-col>
          </q-row>
        </div>
        <div v-if="locTrnsx.categories.length > 1 && remainingSplit !== 0">
          <q-banner
            v-if="remainingSplit !== 0"
            :class="remainingSplit < 0 ? 'bg-negative' : 'bg-warning'"
            class="text-white q-mb-md"
          >
            <div v-if="remainingSplit > 0">
              Remaining ${{ toDollars(toCents(Math.abs(remainingSplit))) }}
            </div>
            <div v-else>
              Over allocated ${{ toDollars(toCents(Math.abs(remainingSplit))) }} ({{
                toDollars(toCents(locTrnsx.amount + remainingSplit))
              }})
            </div>
          </q-banner>
        </div>
        <div>
          <q-btn flat color="primary" @click="addSplit" label="Add Split" />
        </div>
      </div>

      <!-- Moved Type toggle above Notes -->
      <q-row class="form-row">
        <q-col cols="auto" class="form-col-label">Type</q-col>
        <q-col class="text-right">
          <ToggleButton v-model="locTrnsx.isIncome" active-text="Income" inactive-text="Expense" />
        </q-col>
      </q-row>

      <div class="form-row">
        <q-input
          v-model="locTrnsx.notes"
          label="Notes"
          type="textarea"
          flat
          @focus="scrollToNoteField"
        />
      </div>

      <q-row class="rounded-borders bg-grey-2 q-mb-sm" justify="center">
        <q-col cols="auto" class="font-weight-bold" justify="center">Budget</q-col>
        <q-col class="q-pa-none q-ma-none text-right">
          <q-select
            v-model="locTrnsx.budgetMonth"
            :options="availableMonths"
            :rules="requiredField"
            flat
            dense
            :disable="availableMonths.length === 0"
          />
        </q-col>
      </q-row>

      <q-row v-if="availableMonths.length === 0" class="q-mt-sm">
        <q-col>
          <q-banner class="bg-warning text-white">
            No budgets available. Please create a budget in the Dashboard.
          </q-banner>
        </q-col>
      </q-row>

      <q-checkbox v-model="locTrnsx.recurring" label="Recurring" />
      <q-select
        v-if="locTrnsx.recurring"
        v-model="locTrnsx.recurringInterval"
        :options="intervals"
        label="Recurring Interval"
        dense
      />

      <!-- Imported Transaction Fields (Shown only if matched) -->
      <div
        v-if="locTrnsx.status && (locTrnsx.status == 'C' || locTrnsx.status == 'R')"
        class="q-mt-md"
      >
        <q-row class="form-row">
          <q-col class="form-col-label">Posted Date</q-col>
          <q-col class="form-col">
            <q-input v-model="locTrnsx.postedDate" type="date" flat dense disable />
          </q-col>
        </q-row>
        <q-row class="form-row">
          <q-col class="form-col-label q-pr-sm">Imported Merchant</q-col>
          <q-col class="form-col" style="min-width: 150px">
            <q-input v-model="locTrnsx.importedMerchant" flat dense disable />
          </q-col>
        </q-row>
        <q-row class="form-row">
          <q-col class="form-col-label">Account Source</q-col>
          <q-col class="form-col">
            <q-input v-model="locTrnsx.accountSource" flat dense disable />
          </q-col>
        </q-row>
        <q-row class="form-row">
          <q-col class="form-col-label">Account Number</q-col>
          <q-col class="form-col">
            <q-input v-model="locTrnsx.accountNumber" flat dense disable />
          </q-col>
        </q-row>
        <q-row class="form-row">
          <q-col class="form-col-label">Check Number</q-col>
          <q-col class="form-col">
            <q-input v-model="locTrnsx.checkNumber" flat dense disable />
          </q-col>
        </q-row>
        <q-row class="form-row">
          <q-col class="form-col-label">Status</q-col>
          <q-col class="form-col">
            <q-input v-model="locTrnsx.status" flat dense disable />
          </q-col>
        </q-row>
        <q-row class="form-row">
          <q-col>
            <q-btn
              flat
              color="warning"
              :loading="saving"
              @click="resetMatch"
              label="Reset Match"
            />
          </q-col>
        </q-row>
      </div>

      <div class="text-center">
        <q-btn
          v-if="locTrnsx.id"
          flat
          color="negative"
          :loading="saving"
          @click="deleteTransaction"
          label="Delete Transaction"
        />
      </div>
    </q-form>
  </div>
  <div v-else>Loading...</div>

  <q-notification v-model="snackbar" :color="snackbarColor" position="top" :timeout="3000">
    {{ snackbarText }}
    <template v-slot:actions>
      <q-btn flat label="Close" @click="snackbar = false" />
    </template>
  </q-notification>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue';
import { dataAccess } from '../dataAccess';
import { Budget, Transaction } from '../types';
import { toCents, toDollars, todayISO, currentMonthISO } from '../utils/helpers';
import CurrencyInput from './CurrencyInput.vue';
import ToggleButton from './ToggleButton.vue';
import { QForm } from 'quasar';
import { useMerchantStore } from '../store/merchants';
import { useBudgetStore } from '../store/budget';

const merchantStore = useMerchantStore();
const budgetStore = useBudgetStore();

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

const snackbar = ref(false);
const snackbarText = ref('');
const snackbarColor = ref('success');

const requiredField = [
  (value: string | null) => !!value || 'This field is required',
  (value: string | null) => value !== '' || 'This field is required',
];

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
        categories: props.initialTransaction.categories
          ? [...props.initialTransaction.categories]
          : [{ category: '', amount: 0 }],
      }
    : { ...defaultTransaction },
);

const isInitialized = ref(false);
const budget = ref<Budget | null>(null);
const transactions = ref<Transaction[]>([]);
const saving = ref(false);
const intervals = ref([
  'Daily',
  'Weekly',
  'Bi-Weekly',
  'Monthly',
  'Quarterly',
  'Bi-Annually',
  'Yearly',
]);
const isMobile = computed(() => window.innerWidth < 960);

const merchantNames = computed(() => merchantStore.merchantNames);

const availableMonths = computed(() => {
  return budgetStore.availableBudgetMonths;
});

const isLastMonth = computed(() => {
  return [...availableMonths.value].sort((a, b) => b.localeCompare(a))[0] === locTrnsx.budgetMonth;
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
      locTrnsx.budgetMonth = availableMonths.value[0];
    }
  }

  transactions.value = await dataAccess.getTransactions(props.budgetId);
  emit('update-transactions', transactions.value);

  if (budget.value?.merchants) {
    merchantStore.updateMerchantsFromCounts(
      Object.fromEntries(budget.value.merchants.map((m) => [m.name, m.usageCount])),
    );
  } else if (budget.value?.transactions) {
    merchantStore.updateMerchants(budget.value.transactions);
  }

  isInitialized.value = true;
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
    locTrnsx.categories[0].amount = locTrnsx.amount;
  }
}

async function resetMatch() {
  if (!locTrnsx.id) return;

  saving.value = true;
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

    const [, entityId, currentBudgetMonth] = props.budgetId.split('_');
    const targetBudgetMonth = locTrnsx.budgetMonth;
    const targetBudgetId =
      currentBudgetMonth === targetBudgetMonth
        ? props.budgetId
        : `${props.userId}_${entityId}_${targetBudgetMonth}`;

    const targetBudget = budgetStore.getBudget(targetBudgetId);
    if (targetBudget) {
      const savedTransaction = await dataAccess.saveTransaction(
        targetBudget,
        locTrnsx,
        !isLastMonth.value,
      );
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
      showSnackbar('Could not reset match, Budget does not exist!', 'error');
    }
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error resetting transaction match:', err.message);
    showSnackbar(`Error resetting transaction match: ${err.message}`, 'error');
  } finally {
    saving.value = false;
  }
}

async function save() {
  if (remainingSplit.value !== 0) return;
  if (!form.value) return;

  const valid = await form.value.validate();
  if (valid) {
    saving.value = true;
    try {
      if (locTrnsx.categories.length === 1 && locTrnsx.categories[0].amount === 0) {
        locTrnsx.categories[0].amount = locTrnsx.amount;
      }
      locTrnsx.userId = props.userId;
      if (!locTrnsx.status) {
        locTrnsx.status = 'U';
      }
      if (!budget.value) {
        throw new Error(`Budget ${props.budgetId} not found`);
      }

      const [, entityId, currentBudgetMonth] = props.budgetId.split('_');
      const targetBudgetMonth = locTrnsx.budgetMonth;
      const targetBudgetId =
        currentBudgetMonth === targetBudgetMonth
          ? props.budgetId
          : `${props.userId}_${entityId}_${targetBudgetMonth}`;

      let moved = false;
      if (currentBudgetMonth !== targetBudgetMonth && locTrnsx.id) {
        await dataAccess.deleteTransaction(budget.value, locTrnsx.id, !isLastMonth.value);
        moved = true;
      }

      const targetBudget = budgetStore.getBudget(targetBudgetId);
      if (targetBudget) {
        const savedTransaction = await dataAccess.saveTransaction(
          targetBudget,
          locTrnsx,
          !isLastMonth.value,
        );
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
      } else {
        showSnackbar('Could not save Transaction, Budget does not exist!', 'error');
      }
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error saving transaction:', err.message);
      showSnackbar(`Error saving transaction: ${err.message}`, 'error');
    } finally {
      saving.value = false;
    }
  } else {
    console.log('Form validation failed');
    showSnackbar('Form validation failed', 'error');
  }
}

async function deleteTransaction() {
  if (!locTrnsx.id) return;

  saving.value = true;
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
    saving.value = false;
  }
}

function showSnackbar(text: string, color = 'success') {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
}
</script>

<style scoped>
.rounded-borders {
  border-radius: 5px;
}

.mb-2 {
  margin-bottom: 8px;
}

.text-right .q-field__control,
.text-right .q-field__native,
.text-right .q-field__input {
  text-align: right !important;
}
</style>
