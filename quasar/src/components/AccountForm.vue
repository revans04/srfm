<!-- src/components/AccountForm.vue -->
<template>
  <q-form v-model="validForm" @submit="save">
    <q-input
      v-model="localAccount.name"
      label="Account Name"
      outlined
      dense
      :rules="[(v) => !!v || 'Account name is required']"
      required
      aria-required="true"
    />
    <q-input v-model="localAccount.institution" label="Institution" outlined dense />
    <q-checkbox
      v-if="showPersonalOption"
      v-model="isPersonalAccount"
      label="Personal Account (not shared with family)"
      dense
    />
    <q-input
      v-if="localAccount.type === 'Bank' || localAccount.type === 'CreditCard'"
      v-model="localAccount.accountNumber"
      label="Account Number"
      outlined
      dense
    />
    <q-input
      v-if="localAccount.type === 'Loan' || localAccount.type === 'CreditCard'"
      v-model.number="localAccount.details.interestRate"
      label="Interest Rate (%)"
      type="number"
      step="0.01"
      outlined
      dense
    />
    <q-input
      v-if="localAccount.type === AccountType.Property"
      v-model.number="localAccount.details.appraisedValue"
      label="Original Value"
      type="number"
      outlined
      dense
    />
    <q-input
      v-if="localAccount.type === AccountType.Property"
      v-model="localAccount.details.address"
      :label="localAccount.type === AccountType.Property ? 'Address' : 'Description'"
      outlined
      dense
    />
    <q-input
      v-if="localAccount.type === AccountType.Investment || localAccount.type === AccountType.Loan"
      v-model="localAccount.details.maturityDate"
      label="Maturity Date"
      type="date"
      outlined
      dense
    />
    <q-input
      v-model.number="localAccount.balance"
      :label="localAccount.category === 'Liability' ? 'Current Balance (as positive #)' : 'Current Value'"
      type="number"
      outlined
      dense
      hint="Enter the current balance or value as of today"
      :rules="[(v) => v !== null || 'Balance is required']"
    />
    <div class="q-mt-md">
      <q-btn type="submit" color="primary" :loading="saving" :disable="!validForm" label="Save" />
      <q-btn color="grey" flat label="Cancel" @click="cancel" class="q-ml-sm" />
    </div>
  </q-form>
</template>

<script setup lang="ts">
import { ref, watch, defineProps, defineEmits } from "vue";
import type { Account } from "../types";
import { AccountType } from "../types";
import { Timestamp } from "firebase/firestore";

const props = defineProps<{
  accountType: AccountType;
  account?: Account;
  showPersonalOption?: boolean;
}>();

const emit = defineEmits<{
  (e: "save", account: Account, isPersonal: boolean): void;
  (e: "cancel"): void;
}>();

const validForm = ref(false);
const saving = ref(false);
const isPersonalAccount = ref(false);

const localAccount = ref<Account>({
  id: "",
  name: "",
  type: props.accountType,
  category: props.accountType === AccountType.CreditCard || props.accountType === AccountType.Loan ? "Liability" : "Asset",
  createdAt: Timestamp.now(),
  updatedAt: Timestamp.now(),
  details: {
    interestRate: undefined,
    appraisedValue: undefined,
    address: undefined,
    maturityDate: undefined,
  },
});

watch(
  () => props.account,
  (newAccount) => {
    if (newAccount) {
      localAccount.value = { ...newAccount, details: { ...newAccount.details } };
      isPersonalAccount.value = !!newAccount.userId;
    }
  },
  { immediate: true }
);

watch(
  () => props.accountType,
  (newType) => {
    localAccount.value.type = newType;
    localAccount.value.category = newType === AccountType.CreditCard || newType === AccountType.Loan ? "Liability" : "Asset";
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
