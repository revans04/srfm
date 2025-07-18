<!-- src/components/TransactionForm.vue -->
<template>
  <div v-if="isInitialized && locTrnsx?.categories">
    <div>
      <v-row class="pa-0">
        <v-col cols="6"> <v-btn variant="text" type="submit" color="primary" :loading="loading" @click="save()">Save</v-btn></v-col>
        <v-col cols="6" class="text-right">
          <v-btn v-if="showCancel" @click="emit('cancel')" variant="text" type="submit" color="primary" :loading="loading">Cancel</v-btn></v-col
        >
      </v-row>
    </div>
    <v-form ref="form" @submit.prevent="save">
      <v-row class="form-row">
        <v-col class="form-col-label">Date</v-col>
        <v-col cols="auto" class="form-col">
          <v-text-field
            v-model="locTrnsx.date"
            type="date"
            :rules="requiredField"
            @input="updateBudgetMonth"
            variant="plain"
            density="compact"
            class="text-right"
          ></v-text-field>
        </v-col>
      </v-row>
      <v-row class="form-row">
        <v-col class="form-col-label q-pr-5">Merchant</v-col>
        <v-col cols="auto" class="form-col" style="min-width: 150px">
          <v-combobox
            v-model="locTrnsx.merchant"
            :items="merchantNames"
            :rules="requiredField"
            density="compact"
            variant="plain"
            menu-icon=""
            class="text-right"
            align-end
          ></v-combobox>
        </v-col>
      </v-row>
      <v-row class="form-row">
        <v-col cols="auto" class="form-col-label">Total</v-col>
        <v-col class="form-col">
          <Currency-Input v-model="locTrnsx.amount" class="text-right" variant="plain"></Currency-Input>
        </v-col>
      </v-row>

      <div class="form-row my-6 ms-n3 py-5">
        <div v-for="(split, index) in locTrnsx.categories" :key="index">
          <v-row no-gutters>
            <v-col no-gutters cols="auto" class="form-col-label pr-2" :class="locTrnsx.categories.length > 1 ? '' : 'pb-5'">
              <v-icon color="error" @click="removeSplit(index)">mdi-close</v-icon>
            </v-col>
            <v-col>
              <v-combobox
                v-model="split.category"
                :items="remainingCategories"
                label="Category"
                :rules="requiredField"
                density="compact"
                variant="plain"
                menu-icon=""
                required
              ></v-combobox>
            </v-col>
            <v-col v-if="locTrnsx.categories.length > 1" no-gutters cols="4" class="form-col">
              <Currency-Input v-model="split.amount" class="text-right" variant="plain"></Currency-Input>
            </v-col>
            <v-col v-if="locTrnsx.categories.length > 1" no-gutters cols="auto" class="form-col"> </v-col>
          </v-row>
        </div>
        <div v-if="locTrnsx.categories.length > 1 && remainingSplit !== 0">
          <v-alert v-if="remainingSplit !== 0" :type="remainingSplit < 0 ? 'error' : 'warning'" class="mb-4">
            <div v-if="remainingSplit > 0">Remaining ${{ toDollars(toCents(Math.abs(remainingSplit))) }}</div>
            <div v-else>Over allocated ${{ toDollars(toCents(Math.abs(remainingSplit))) }} ({{ toDollars(toCents(locTrnsx.amount + remainingSplit)) }})</div>
          </v-alert>
        </div>
        <div>
          <v-btn variant="plain" color="primary" @click="addSplit()">Add Split</v-btn>
        </div>
      </div>

      <!-- Moved Type toggle above Notes -->
      <v-row class="form-row">
        <v-col cols="auto" class="form-col-label">Type</v-col>
        <v-col class="text-right">
          <ToggleButton v-model="locTrnsx.isIncome" active-text="Income" inactive-text="Expense" />
        </v-col>
      </v-row>

      <div class="v-row form-row">
        <v-textarea v-model="locTrnsx.notes" label="Notes" variant="plain" @focus="scrollToNoteField"></v-textarea>
      </div>

      <v-row class="rounded-5 bg-light mb-2" justify="center">
        <v-col cols="auto" class="font-weight-bold" justify="center">Budget</v-col>
        <v-col class="pa-0 ma-0 text-right">
          <v-select
            v-model="locTrnsx.budgetMonth"
            :items="availableMonths"
            :rules="requiredField"
            variant="plain"
            density="compact"
            :disabled="availableMonths.length === 0"
          ></v-select>
        </v-col>
      </v-row>

      <v-row v-if="availableMonths.length === 0" class="mt-2">
        <v-col>
          <v-alert type="warning">No budgets available. Please create a budget in the Dashboard.</v-alert>
        </v-col>
      </v-row>

      <v-checkbox v-model="locTrnsx.recurring" label="Recurring"></v-checkbox>
      <v-select v-if="locTrnsx.recurring" v-model="locTrnsx.recurringInterval" :items="intervals" label="Recurring Interval"></v-select>

      <!-- Imported Transaction Fields (Shown only if matched) -->
      <div v-if="locTrnsx.status && (locTrnsx.status == 'C' || locTrnsx.status == 'R')" class="mt-4">
        <v-row class="form-row">
          <v-col class="form-col-label">Posted Date</v-col>
          <v-col class="form-col">
            <v-text-field v-model="locTrnsx.postedDate" type="date" variant="plain" density="compact" readonly></v-text-field>
          </v-col>
        </v-row>
        <v-row class="form-row">
          <v-col class="form-col-label q-pr-5">Imported Merchant</v-col>
          <v-col class="form-col" style="min-width: 150px">
            <v-text-field v-model="locTrnsx.importedMerchant" variant="plain" density="compact" readonly></v-text-field>
          </v-col>
        </v-row>
        <v-row class="form-row">
          <v-col class="form-col-label">Account Source</v-col>
          <v-col class="form-col">
            <v-text-field v-model="locTrnsx.accountSource" variant="plain" density="compact" readonly></v-text-field>
          </v-col>
        </v-row>
        <v-row class="form-row">
          <v-col class="form-col-label">Account Number</v-col>
          <v-col class="form-col">
            <v-text-field v-model="locTrnsx.accountNumber" variant="plain" density="compact" readonly></v-text-field>
          </v-col>
        </v-row>
        <v-row class="form-row">
          <v-col class="form-col-label">Check Number</v-col>
          <v-col class="form-col">
            <v-text-field v-model="locTrnsx.checkNumber" variant="plain" density="compact" readonly></v-text-field>
          </v-col>
        </v-row>
        <v-row class="form-row">
          <v-col class="form-col-label">Status</v-col>
          <v-col class="form-col">
            <v-text-field v-model="locTrnsx.status" variant="plain" density="compact" readonly></v-text-field>
          </v-col>
        </v-row>
        <v-row class="form-row">
          <v-col>
            <v-btn variant="text" color="warning" :loading="loading" @click="resetMatch">Reset Match</v-btn>
          </v-col>
        </v-row>
      </div>

      <div class="text-center">
        <v-btn v-if="locTrnsx.id" variant="text" color="error" :loading="loading" @click="deleteTransaction"> Delete Transaction </v-btn>
      </div>
    </v-form>
  </div>
  <div v-else>Loading...</div>

  <v-snackbar v-model="snackbar" :color="snackbarColor" :timeout="3000">
    {{ snackbarText }}
    <template v-slot:actions>
      <v-btn variant="text" @click="snackbar = false">Close</v-btn>
    </template>
  </v-snackbar>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, reactive } from "vue";
import { dataAccess } from "../dataAccess";
import { Budget, Transaction } from "../types";
import { toCents, toDollars, todayISO, currentMonthISO } from "../utils/helpers";
import CurrencyInput from "./CurrencyInput.vue";
import ToggleButton from "./ToggleButton.vue";
import { VForm } from "vuetify/components";
import { useMerchantStore } from "../store/merchants";
import { useBudgetStore } from "../store/budget";

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
  (e: "save", transaction: Transaction): void;
  (e: "cancel"): void;
  (e: "update-transactions", transactions: Transaction[]): void;
}>();

const snackbar = ref(false);
const snackbarText = ref("");
const snackbarColor = ref("success");

const requiredField = [(value: string | null) => !!value || "This field is required", (value: string | null) => value !== "" || "This field is required"];

const form = ref<InstanceType<typeof VForm> | null>(null);

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
const loading = ref(false);
const intervals = ref(["Daily", "Weekly", "Bi-Weekly", "Monthly", "Quarterly", "Bi-Annually", "Yearly"]);
const isMobile = computed(() => window.innerWidth < 960);

const merchantNames = computed(() => merchantStore.merchantNames);

const availableMonths = computed(() => {
  return budgetStore.availableBudgetMonths;
});

const isLastMonth = computed(() => {
  return availableMonths.value.sort((a, b) => b.localeCompare(a))[0] == locTrnsx.budgetMonth;
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
      locTrnsx.budgetMonth = availableMonths.value[0];
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
    locTrnsx.categories[0].amount = locTrnsx.amount;
  }
}

async function resetMatch() {
  if (!locTrnsx.id) return;

  loading.value = true;
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

    const [currentUserId, entityId, currentBudgetMonth] = props.budgetId.split("_");
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
  } catch (error: any) {
    console.error("Error resetting transaction match:", error.message);
    showSnackbar(`Error resetting transaction match: ${error.message}`, "error");
  } finally {
    loading.value = false;
  }
}

async function save() {
  if (remainingSplit.value !== 0) return;
  if (!form.value) return;

  const { valid } = await form.value.validate();
  if (valid) {
    loading.value = true;
    try {
      if (locTrnsx.categories.length === 1 && locTrnsx.categories[0].amount === 0) {
        locTrnsx.categories[0].amount = locTrnsx.amount;
      }
      locTrnsx.userId = props.userId;
      if (!locTrnsx.status) {
        locTrnsx.status = "U";
      }
      if (!budget.value) {
        throw new Error(`Budget ${props.budgetId} not found`);
      }

      const [currentUserId, entityId, currentBudgetMonth] = props.budgetId.split("_");
      const targetBudgetMonth = locTrnsx.budgetMonth;
      const targetBudgetId = currentBudgetMonth === targetBudgetMonth ? props.budgetId : `${props.userId}_${entityId}_${targetBudgetMonth}`;

      let moved = false;
      let targetBudget = budgetStore.getBudget(targetBudgetId);
      if (!targetBudget) {
        targetBudget = await dataAccess.getBudget(targetBudgetId);
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
    } catch (error: any) {
      console.error("Error saving transaction:", error.message);
      showSnackbar(`Error saving transaction: ${error.message}`, "error");
    } finally {
      loading.value = false;
    }
  } else {
    console.log("Form validation failed");
    showSnackbar("Form validation failed", "error");
  }
}

async function deleteTransaction() {
  if (!locTrnsx.id) return;

  loading.value = true;
  try {
    if (!budget.value) {
      throw new Error(`Budget ${props.budgetId} not found`);
    }
    await dataAccess.deleteTransaction(budget.value, locTrnsx.id, !isLastMonth.value);

    transactions.value = transactions.value.filter((t) => t.id !== locTrnsx.id);
    emit("update-transactions", transactions.value);

    emit("cancel");
  } catch (error: any) {
    console.error("Error deleting transaction:", error.message);
  } finally {
    loading.value = false;
  }
}

function showSnackbar(text: string, color = "success") {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
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
