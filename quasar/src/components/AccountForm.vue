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
      v-if="localAccount.type === AccountType.Property"
      v-model.number="localAccount.details.appraisedValue"
      label="Original Value"
      type="number"
      variant="outlined"
      density="compact"
    ></q-input>
    <q-input
      v-if="localAccount.type === AccountType.Property"
      v-model="localAccount.details.address"
      :label="localAccount.type === AccountType.Property ? 'Address' : 'Description'"
      variant="outlined"
      density="compact"
    ></q-input>
    <q-input
      v-if="localAccount.type === AccountType.Investment || localAccount.type === AccountType.Loan"
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
import { Account, AccountType } from "../types";
import { Timestamp } from "firebase/firestore";
import { v4 as uuidv4 } from "uuid";

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

type AccountWithDetails = Account & { details: NonNullable<Account["details"]> };

const localAccount = ref<AccountWithDetails>({
  id: uuidv4(),
  name: "",
  type: props.accountType,
  category: props.accountType === AccountType.CreditCard || props.accountType === AccountType.Loan ? "Liability" : "Asset",
  createdAt: Timestamp.now(),
  updatedAt: Timestamp.now(),
  details: {},
});

watch(
  () => props.account,
  (newAccount) => {
    if (newAccount) {
      localAccount.value = {
        ...newAccount,
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
