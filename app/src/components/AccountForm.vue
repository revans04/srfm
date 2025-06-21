<!-- src/components/AccountForm.vue -->
<template>
  <v-form v-model="validForm" @submit.prevent="save">
    <v-text-field
      v-model="localAccount.name"
      label="Account Name"
      variant="outlined"
      density="compact"
      :rules="[(v) => !!v || 'Account name is required']"
      required
      aria-required="true"
    ></v-text-field>
    <v-text-field v-model="localAccount.institution" label="Institution" variant="outlined" density="compact"></v-text-field>
    <v-checkbox v-if="showPersonalOption" v-model="isPersonalAccount" label="Personal Account (not shared with family)" density="compact"></v-checkbox>
    <v-text-field
      v-if="localAccount.type === 'Bank' || localAccount.type === 'CreditCard'"
      v-model="localAccount.accountNumber"
      label="Account Number"
      variant="outlined"
      density="compact"
    ></v-text-field>
    <v-text-field
      v-if="localAccount.type === 'Loan' || localAccount.type === 'CreditCard'"
      v-model.number="localAccount.details.interestRate"
      label="Interest Rate (%)"
      type="number"
      step="0.01"
      variant="outlined"
      density="compact"
    ></v-text-field>
    <v-text-field
      v-if="localAccount.type === AccountType.Property"
      v-model.number="localAccount.details.appraisedValue"
      label="Original Value"
      type="number"
      variant="outlined"
      density="compact"
    ></v-text-field>
    <v-text-field
      v-if="localAccount.type === AccountType.Property"
      v-model="localAccount.details.address"
      :label="localAccount.type === AccountType.Property ? 'Address' : 'Description'"
      variant="outlined"
      density="compact"
    ></v-text-field>
    <v-text-field
      v-if="localAccount.type === AccountType.Investment || localAccount.type === AccountType.Loan"
      v-model="localAccount.details.maturityDate"
      label="Maturity Date"
      type="date"
      variant="outlined"
      density="compact"
    ></v-text-field>
    <v-text-field
      v-model.number="localAccount.balance"
      :label="localAccount.category === 'Liability' ? 'Current Balance (as positive #)' : 'Current Value'"
      type="number"
      variant="outlined"
      density="compact"
      hint="Enter the current balance or value as of today"
      :rules="[(v) => v !== null || 'Balance is required']"
    ></v-text-field>
    <div class="mt-4">
      <v-btn type="submit" color="primary" :loading="saving" :disabled="!validForm"> Save </v-btn>
      <v-btn color="grey" variant="text" @click="cancel" class="ml-2"> Cancel </v-btn>
    </div>
  </v-form>
</template>

<script setup lang="ts">
import { ref, computed, watch, defineProps, defineEmits } from "vue";
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

const localAccount = ref<Account>({
  id: uuidv4(),
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
