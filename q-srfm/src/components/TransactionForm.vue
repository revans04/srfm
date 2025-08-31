<!-- src/components/TransactionForm.vue -->
<template>
  <div v-if="isInitialized && locTrnsx?.categories">
      <div class="row pa-0" >
        <div class="col col-6">
          <q-btn variant="flat" type="submit" color="primary" :loading="isLoading" @click="save" label="Save" />
        </div>
        <div class="col text-right col-6" >
          <q-btn
            v-if="showCancel"
            @click="emit('cancel')"
            variant="flat"
            type="submit"
            color="primary"
            :loading="isLoading"
            label="Cancel"
          />
        </div>
      </div>
    <q-form ref="form" @submit.prevent="save">
      <div class="row form-row" >
        <div class="col form-col-label" >Date</div>
        <div class="col form-col col-auto" >
          <q-input
            v-model="locTrnsx.date"
            type="date"
            :rules="requiredField"
            @input="updateBudgetMonth"
            variant="plain"
            density="compact"
            class="text-right"
          ></q-input>
        </div>
      </div>
      <div class="row form-row" >
        <div class="col form-col-label q-pr-5" >Merchant</div>
        <div class="col form-col col-auto"  style="min-width: 150px">
          <q-select
            v-model="locTrnsx.merchant"
            :items="merchantNames"
            :rules="requiredField"
            density="compact"
            variant="plain"
            menu-icon=""
            class="text-right"
            align-end
          ></q-select>
        </div>
      </div>
      <div class="row form-row" >
        <div class="col form-col-label col-auto" >Total</div>
        <div class="col form-col" >
          <Currency-Input v-model="locTrnsx.amount" class="text-right" variant="plain"></Currency-Input>
        </div>
      </div>

      <div class="form-row my-6 ms-n3 py-5">
        <div v-for="(split, index) in locTrnsx.categories" :key="index">
          <div class="row no-gutters" >
            <div class="col form-col-label pr-2 col-auto" no-gutters  :class="locTrnsx.categories.length > 1 ? '' : 'pb-5'">
                <q-icon color="error" @click="removeSplit(index)" name="close"></q-icon>
            </div>
            <div class="col">
              <q-select
                v-model="split.category"
                :items="remainingCategories"
                label="Category"
                :rules="requiredField"
                density="compact"
                variant="plain"
                menu-icon=""
                required
              ></q-select>
            </div>
            <div class="col form-col" v-if="locTrnsx.categories.length > 1" no-gutters cols="4">
              <Currency-Input v-model="split.amount" class="text-right" variant="plain"></Currency-Input>
            </div>
            <div class="col form-col" v-if="locTrnsx.categories.length > 1" no-gutters cols="auto"> </div>
          </div>
        </div>
        <div v-if="locTrnsx.categories.length > 1 && remainingSplit !== 0">
          <q-banner v-if="remainingSplit !== 0" :type="remainingSplit < 0 ? 'error' : 'warning'" class="mb-4">
            <div v-if="remainingSplit > 0">Remaining ${{ toDollars(toCents(Math.abs(remainingSplit))) }}</div>
            <div v-else>Over allocated ${{ toDollars(toCents(Math.abs(remainingSplit))) }} ({{ toDollars(toCents(locTrnsx.amount + remainingSplit)) }})</div>
          </q-banner>
        </div>
        <div>
          <q-btn variant="plain" color="primary" @click="addSplit()">Add Split</q-btn>
        </div>
      </div>

      <!-- Moved Type toggle above Notes -->
      <div class="row form-row" >
        <div class="col form-col-label col-auto" >Type</div>
        <div class="col text-right" >
          <ToggleButton v-model="locTrnsx.isIncome" active-text="Income" inactive-text="Expense" />
        </div>
      </div>

      <div class="v-row form-row">
        <q-textarea v-model="locTrnsx.notes" label="Notes" variant="plain" @focus="scrollToNoteField"></q-textarea>
      </div>

      <div class="row rounded-5 bg-light mb-2 justify-center" >
        <div class="col font-weight-bold col-auto"  justify="center">Budget</div>
        <div class="col pa-0 ma-0 text-right" >
          <q-select
            v-model="locTrnsx.budgetMonth"
            :items="availableMonths"
            :rules="requiredField"
            variant="plain"
            density="compact"
            :disabled="availableMonths.length === 0"
          ></q-select>
        </div>
      </div>

      <div class="row mt-2" v-if="availableMonths.length === 0" >
        <div class="col">
          <q-banner type="warning">No budgets available. Please create a budget in the Dashboard.</q-banner>
        </div>
      </div>

      <q-checkbox v-model="locTrnsx.recurring" label="Recurring"></q-checkbox>
      <q-select v-if="locTrnsx.recurring" v-model="locTrnsx.recurringInterval" :items="intervals" label="Recurring Interval"></q-select>

      <!-- Imported Transaction Fields (Shown only if matched) -->
      <div
        v-if="locTrnsx.status && (locTrnsx.status === 'C' || locTrnsx.status === 'R')"
        class="mt-4"
      >
        <div class="row form-row" >
          <div class="col form-col-label" >Posted Date</div>
          <div class="col form-col" >
            <q-input v-model="locTrnsx.postedDate" type="date" variant="plain" density="compact" readonly></q-input>
          </div>
        </div>
        <div class="row form-row" >
          <div class="col form-col-label q-pr-5" >Imported Merchant</div>
          <div class="col form-col"  style="min-width: 150px">
            <q-input v-model="locTrnsx.importedMerchant" variant="plain" density="compact" readonly></q-input>
          </div>
        </div>
        <div class="row form-row" >
          <div class="col form-col-label" >Account Source</div>
          <div class="col form-col" >
            <q-input v-model="locTrnsx.accountSource" variant="plain" density="compact" readonly></q-input>
          </div>
        </div>
        <div class="row form-row" >
          <div class="col form-col-label" >Account Number</div>
          <div class="col form-col" >
            <q-input v-model="locTrnsx.accountNumber" variant="plain" density="compact" readonly></q-input>
          </div>
        </div>
        <div class="row form-row" >
          <div class="col form-col-label" >Check Number</div>
          <div class="col form-col" >
            <q-input v-model="locTrnsx.checkNumber" variant="plain" density="compact" readonly></q-input>
          </div>
        </div>
        <div class="row form-row" >
          <div class="col form-col-label" >Status</div>
          <div class="col form-col" >
            <q-input v-model="locTrnsx.status" variant="plain" density="compact" readonly></q-input>
          </div>
        </div>
        <div class="row form-row" >
          <div class="col">
            <q-btn variant="text" color="warning" :loading="isLoading" @click="resetMatch">Reset Match</q-btn>
          </div>
        </div>
      </div>

      <div class="text-center">
        <q-btn
          v-if="locTrnsx.id"
          variant="text"
          color="error"
          :loading="isLoading"
          @click="deleteTransaction"
          label="Delete Transaction"
        />
      </div>
    </q-form>
  </div>
  <div v-else>Loading...</div>

</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from "vue";
import { useQuasar } from 'quasar';
import { dataAccess } from "../dataAccess";
import type { Budget, Transaction } from "../types";
import { toCents, toDollars, todayISO, currentMonthISO } from "../utils/helpers";
import CurrencyInput from "./CurrencyInput.vue";
import ToggleButton from "./ToggleButton.vue";
import { QForm } from "quasar";
import { useMerchantStore } from "../store/merchants";
import { useBudgetStore } from "../store/budget";

const merchantStore = useMerchantStore();
const budgetStore = useBudgetStore();
const $q = useQuasar();

const props = defineProps<{
  initialTransaction: Transaction;
  showCancel?: boolean;
  categoryOptions: string[];
  budgetId: string;
  userId: string;
  loading?: boolean;
}>();

const emit = defineEmits<{
  (e: "save", transaction: Transaction): void;
  (e: "cancel"): void;
  (e: "update-transactions", transactions: Transaction[]): void;
}>();


const requiredField = [(value: string | null) => !!value || "This field is required", (value: string | null) => value !== "" || "This field is required"];

const form = ref<InstanceType<typeof QForm> | null>(null);

const defaultTransaction: Transaction = {
  id: "",
  budgetMonth: currentMonthISO(),
  date: todayISO(),
  merchant: "",
  categories: [{ category: "", amount: 0 }],
  amount: 0,
  notes: "",
  recurring: false,
  recurringInterval: "Monthly",
  userId: props.userId,
  isIncome: false,
  taxMetadata: [],
};

const locTrnsx = reactive<Transaction>(
  props.initialTransaction
    ? {
        ...defaultTransaction,
        ...props.initialTransaction,
        categories: props.initialTransaction.categories ? [...props.initialTransaction.categories] : [{ category: "", amount: 0 }],
      }
    : { ...defaultTransaction }
);

const isInitialized = ref(false);
const budget = ref<Budget | null>(null);
const transactions = ref<Transaction[]>([]);
const isLoading = ref(false);
const intervals = ref(["Daily", "Weekly", "Bi-Weekly", "Monthly", "Quarterly", "Bi-Annually", "Yearly"]);
const isMobile = computed(() => $q.screen.lt.md);

const merchantNames = computed(() => merchantStore.merchantNames);

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
      return str === "Income" && !categoryNames.has(str);
    } else {
      return str !== "Income" && !categoryNames.has(str);
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
    showSnackbar("No budgets available. Please create a budget in the Dashboard.", "warning");
  } else {
    if (!locTrnsx.budgetMonth || !availableMonths.value.includes(locTrnsx.budgetMonth)) {
      locTrnsx.budgetMonth = availableMonths.value[0] || "";
    }
  }

  transactions.value = await dataAccess.getTransactions(props.budgetId);
  emit("update-transactions", transactions.value);

  if (budget.value?.merchants) {
    merchantStore.updateMerchantsFromCounts(Object.fromEntries(budget.value.merchants.map((m) => [m.name, m.usageCount])));
  } else if (budget.value?.transactions) {
    merchantStore.updateMerchants(budget.value.transactions);
  }

  isInitialized.value = true;
});

function scrollToNoteField(event: FocusEvent) {
  if (isMobile.value) {
    setTimeout(() => {
      (event.target as HTMLElement).scrollIntoView({ behavior: "smooth", block: "center" });
    }, 300); // Delay to account for keyboard appearance
  }
}

function updateBudgetMonth() {
  if (!locTrnsx.budgetMonth && locTrnsx.date) {
    const potentialMonth = locTrnsx.date.slice(0, 7);
    if (availableMonths.value.includes(potentialMonth)) {
      locTrnsx.budgetMonth = potentialMonth;
    } else {
      locTrnsx.budgetMonth = availableMonths.value[0] || "";
    }
  }
}

function addSplit() {
  locTrnsx.categories.push({ category: "", amount: 0 });
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
    locTrnsx.status = "U";
    locTrnsx.accountSource = "";
    locTrnsx.accountNumber = "";
    locTrnsx.postedDate = "";
    locTrnsx.checkNumber = "";
    locTrnsx.importedMerchant = "";

    // Save the updated transaction
    if (!budget.value) {
      throw new Error(`Budget ${props.budgetId} not found`);
    }

    const [, entityId, currentBudgetMonth] = props.budgetId.split("_");
    const targetBudgetMonth = locTrnsx.budgetMonth;
    const targetBudgetId = currentBudgetMonth === targetBudgetMonth ? props.budgetId : `${props.userId}_${entityId}_${targetBudgetMonth}`;

    const targetBudget = budgetStore.getBudget(targetBudgetId);
    if (targetBudget) {
      const savedTransaction = await dataAccess.saveTransaction(targetBudget, locTrnsx, !isLastMonth.value);
      const index = transactions.value.findIndex((t) => t.id === savedTransaction.id);
      if (index >= 0) {
        transactions.value[index] = savedTransaction;
      } else {
        transactions.value.push(savedTransaction);
      }
      emit("update-transactions", transactions.value);
      emit("save", savedTransaction);
      showSnackbar("Transaction match reset successfully", "success");
    } else {
      showSnackbar("Could not reset match, Budget does not exist!", "error");
    }
  } catch (error: unknown) {
    const err = error as Error;
    console.error("Error resetting transaction match:", err.message);
    showSnackbar(`Error resetting transaction match: ${err.message}`, "error");
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
        locTrnsx.status = "U";
      }
      if (!budget.value) {
        throw new Error(`Budget ${props.budgetId} not found`);
      }

      const [, entityId, currentBudgetMonth] = props.budgetId.split("_");
      const targetBudgetMonth = locTrnsx.budgetMonth;
      const targetBudgetId = currentBudgetMonth === targetBudgetMonth ? props.budgetId : `${props.userId}_${entityId}_${targetBudgetMonth}`;

      let moved = false;
      let targetBudget = budgetStore.getBudget(targetBudgetId);
      if (!targetBudget) {
        targetBudget = (await dataAccess.getBudget(targetBudgetId)) || undefined;
        if (targetBudget) {
          budgetStore.updateBudget(targetBudgetId, targetBudget);
        }
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
        emit("update-transactions", transactions.value);

        emit("save", savedTransaction);
        showSnackbar("Transaction saved successfully", "success");
      } else {
        showSnackbar("Could not save Transaction, Budget does not exist!", "error");
      }
    } catch (error: unknown) {
      const err = error as Error;
      console.error("Error saving transaction:", err.message);
      showSnackbar(`Error saving transaction: ${err.message}`, "error");
    } finally {
      isLoading.value = false;
    }
  } else {
    console.log("Form validation failed");
    showSnackbar("Form validation failed", "error");
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
    emit("update-transactions", transactions.value);

    emit("cancel");
  } catch (error: unknown) {
    const err = error as Error;
    console.error("Error deleting transaction:", err.message);
  } finally {
    isLoading.value = false;
  }
}

function showSnackbar(text: string, color = "success") {
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
.mb-2 {
  margin-bottom: 8px;
}

.text-right.v-text-field input,
.text-right .v-input__control .v-field__input,
.text-right .v-autocomplete .v-field__input .v-autocomplete__selection-text,
.text-right .v-select .v-field__input .v-select__selection-text,
.text-right .v-textarea .v-field__input {
  text-align: right !important;
}
</style>
