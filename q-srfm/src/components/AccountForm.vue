<!-- src/components/AccountForm.vue -->
<template>
  <q-form v-model="validForm" @submit.prevent="save">
    <q-input
      v-model="localAccount.name"
      label="Account Name"
      variant="outlined"
      density="compact"
      :rules="[(v: string | null) => !!v || 'Account name is required']"
      required
      aria-required="true"
    ></q-input>
    <q-input v-model="localAccount.institution" label="Institution" variant="outlined" density="compact"></q-input>
    <q-checkbox v-if="showPersonalOption" v-model="isPersonalAccount" label="Personal Account (not shared with family)" density="compact"></q-checkbox>
    <q-input
      v-if="localAccount.type === 'Bank' || localAccount.type === 'CreditCard'"
      v-model="localAccount.accountNumber"
      label="Account Number"
      variant="outlined"
      density="compact"
    ></q-input>
    <q-input
      v-if="localAccount.type === 'Loan' || localAccount.type === 'CreditCard'"
      v-model.number="localAccount.details.interestRate"
      label="Interest Rate (%)"
      type="number"
      step="0.01"
      variant="outlined"
      density="compact"
    ></q-input>
    <q-input
      v-if="localAccount.type === 'Property'"
      v-model.number="localAccount.details.appraisedValue"
      label="Original Value"
      type="number"
      variant="outlined"
      density="compact"
    ></q-input>
    <q-input
      v-if="localAccount.type === 'Property'"
      v-model="localAccount.details.address"
      :label="localAccount.type === 'Property' ? 'Address' : 'Description'"
      variant="outlined"
      density="compact"
    ></q-input>
    <q-input
      v-if="localAccount.type === 'Investment' || localAccount.type === 'Loan'"
      v-model="localAccount.details.maturityDate"
      label="Maturity Date"
      type="date"
      variant="outlined"
      density="compact"
    ></q-input>
    <q-input
      v-model.number="localAccount.balance"
      :label="localAccount.category === 'Liability' ? 'Current Balance (as positive #)' : 'Current Value'"
      type="number"
      variant="outlined"
      density="compact"
      hint="Enter the current balance or value as of today"
      :rules="[(v: number | null) => v !== null || 'Balance is required']"
    ></q-input>
    <div class="mt-4">
      <q-btn type="submit" color="primary" :loading="saving" :disabled="!validForm"> Save </q-btn>
      <q-btn color="grey" variant="text" @click="cancel" class="ml-2"> Cancel </q-btn>
    </div>
  </q-form>
</template>

<script setup lang="ts">
import { ref, watch, defineProps, defineEmits } from "vue";
import type { Account, AccountType } from "../types";
import { Timestamp } from "firebase/firestore";
import { v4 as uuidv4 } from "uuid";

type AccountTypeLiteral = Account["type"]; // "Bank" | "CreditCard" | ...

const props = defineProps<{
  // Accept either the string literal union or the enum value
  accountType: AccountType | AccountTypeLiteral;
  // Allow partial Account for callers that build drafts before persisting
  account?: Partial<Account>;
  showPersonalOption?: boolean;
}>();

const emit = defineEmits<{
  (e: "save", account: Account, isPersonal: boolean): void;
  (e: "cancel"): void;
}>();

const validForm = ref(false);
const saving = ref(false);
const isPersonalAccount = ref(false);

type AccountWithDetails = Account & { details: NonNullable<Account["details"]> };

function normalizeType(t: AccountType | AccountTypeLiteral): AccountTypeLiteral {
  // AccountType is a string enum, so a straight cast to the literal union is fine
  return t as unknown as AccountTypeLiteral;
}

const localAccount = ref<AccountWithDetails>({
  id: uuidv4(),
  name: "",
  type: normalizeType(props.accountType),
  category: normalizeType(props.accountType) === "CreditCard" || normalizeType(props.accountType) === "Loan" ? "Liability" : "Asset",
  createdAt: Timestamp.now(),
  updatedAt: Timestamp.now(),
  details: {},
});

watch(
  () => props.account,
  (newAccount) => {
    if (newAccount) {
      localAccount.value = {
        // Fill required fields while preserving provided values
        id: newAccount.id || localAccount.value.id,
        name: newAccount.name || "",
        type: normalizeType(newAccount.type ?? localAccount.value.type),
        category: newAccount.category ?? localAccount.value.category,
        accountNumber: newAccount.accountNumber,
        institution: newAccount.institution,
        createdAt: newAccount.createdAt ?? Timestamp.now(),
        updatedAt: newAccount.updatedAt ?? Timestamp.now(),
        balance: newAccount.balance,
        details: { ...(newAccount.details || {}) },
      };
      isPersonalAccount.value = !!newAccount.userId;
    }
  },
  { immediate: true }
);

watch(
  () => props.accountType,
  (newType) => {
    const t = normalizeType(newType);
    localAccount.value.type = t;
    localAccount.value.category = t === "CreditCard" || t === "Loan" ? "Liability" : "Asset";
  }
);

function save() {
  saving.value = true;
  localAccount.value.updatedAt = Timestamp.now();
  emit("save", localAccount.value, isPersonalAccount.value);
  saving.value = false;
}

function cancel() {
  emit("cancel");
}
</script>
