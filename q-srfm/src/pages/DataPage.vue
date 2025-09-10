<!-- src/views/DataView.vue -->
<template>
  <q-page class="bg-grey-2 q-pa-md" fluid>
    <h1 class="text-h5 q-mb-md">Data Management</h1>

    <!-- Loading handled via $q.loading -->

    <!-- Tabs -->
    <q-tabs v-model="activeTab" color="primary" class="bg-white q-mb-md" style="border-radius: 4px">
      <q-tab name="import" label="Import" />
      <q-tab name="export" label="Export" />
    </q-tabs>

    <q-tab-panels v-model="activeTab">
      <!-- Import Tab -->
      <q-tab-panel name="import">
        <div class="row">
          <div class="col col-12">
            <q-card flat bordered class="bg-white q-pa-md" rounded>
              <q-card-section>Import Data</q-card-section>
              <q-card-section>
                <q-select
                  v-model="importType"
                  :options="importTypes"
                  label="Select Import Type"
                  outlined
                  class="q-mb-lg"
                  emit-value
                  map-options
                ></q-select>

                <!-- Entity Selection or Creation -->
                <div class="row" v-if="entityOptions.length > 0 && importType !== 'bankTransactions' && importType !== 'accountsAndSnapshots'">
                  <div class="col col-12 col-md-6">
                    <q-select
                      v-model="selectedEntityId"
                      :options="entityOptions"
                      option-label="name"
                      option-value="id"
                      emit-value
                      map-options
                      label="Select Entity"
                      outlined
                      dense
                      clearable
                      :rules="importType !== 'entities' ? [(v) => !!v || 'Entity selection is required'] : []"
                      class="q-mb-lg"
                    ></q-select>
                  </div>
                  <div class="col col-12 col-md-6">
                    <q-btn color="primary" @click="openCreateEntityDialog" class="q-mt-sm">Create New Entity</q-btn>
                  </div>
                </div>
                <div class="row" v-else-if="importType !== 'bankTransactions' && importType !== 'accountsAndSnapshots'">
                  <div class="col col-12">
                    <q-banner type="info" class="q-mb-lg">
                      No entities found. Please create a new entity or import entities before importing budgets or transactions.
                    </q-banner>
                    <q-btn color="primary" @click="openCreateEntityDialog" class="q-mr-lg">Create Entity</q-btn>
                    <q-btn color="secondary" @click="importType = 'entities'">Import Entities</q-btn>
                  </div>
                </div>

                <!-- File Input for Imports -->
                <q-banner v-if="importError" type="negative" class="q-mb-lg">
                  {{ importError }}
                </q-banner>
                <q-banner v-if="importSuccess" type="positive" class="q-mb-lg">
                  {{ importSuccess }}
                </q-banner>
                <div v-if="importType === 'bankTransactions'">
                  <!-- Bank transaction import UI -->
                  <div class="row">
                    <div class="col col-12 col-md-6">
                      <q-select
                        v-model="selectedAccountId"
                        :options="formattedAccounts"
                        option-label="title"
                        option-value="value"
                        emit-value
                        map-options
                        label="Select Account for Transactions"
                        outlined
                        :rules="[(v) => !!v || 'Account selection is required']"
                        :disabled="importing || availableAccounts.length === 0"
                        class="q-mb-lg"
                      ></q-select>
                      <q-banner v-if="availableAccounts.length === 0" type="warning" class="q-mb-lg">
                        No bank or credit card accounts found. Please create an account in the Accounts section before importing transactions.
                      </q-banner>
                    </div>
                  </div>
                  <div class="row">
                    <div class="col col-12 col-md-6">
                      <q-file
                        v-model="bankTransactionsFile"
                        label="Upload Bank/Card Transactions CSV"
                        accept=".csv"
                        @update:model-value="handleBankTransactionsFileUpload"
                        :disabled="importing || !selectedAccountId"
                      ></q-file>
                    </div>
                  </div>
                  <!-- Field Mapping -->
                  <div v-if="csvHeaders.length > 0">
                    <div class="row">
                      <div class="col col-12">
                        <h3>Map CSV Columns to Fields</h3>
                        <q-select
                          v-model="amountFormat"
                          :options="amountFormatOptions"
                          label="How are Credits/Debits Represented?"
                          outlined
                          class="q-mb-lg"
                          emit-value
                          map-options
                        ></q-select>

                        <!-- Common Fields -->
                        <div class="row" v-for="(field, index) in commonBankTransactionFields" :key="index">
                          <div class="col col-12 col-md-6">
                            <q-select
                              v-model="fieldMapping[field.key]"
                              :options="csvHeaders"
                              :label="field.label"
                              outlined
                              clearable
                              placeholder="Select a column or type a value"
                            ></q-select>
                          </div>
                        </div>

                        <!-- Amount Fields Based on Format -->
                        <div v-if="amountFormat === 'separate'">
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-select
                                v-model="fieldMapping.creditAmount"
                                :options="csvHeaders"
                                label="Credit Amount"
                                outlined
                                clearable
                                placeholder="Select a column or type a value"
                              ></q-select>
                            </div>
                          </div>
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-select
                                v-model="fieldMapping.debitAmount"
                                :options="csvHeaders"
                                label="Debit Amount"
                                outlined
                                clearable
                                placeholder="Select a column or type a value"
                              ></q-select>
                            </div>
                          </div>
                        </div>
                        <div v-else-if="amountFormat === 'type'">
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-select
                                v-model="fieldMapping.transactionType"
                                :options="csvHeaders"
                                label="Transaction Type Column"
                                outlined
                                clearable
                                placeholder="Select a column or type a value"
                              ></q-select>
                            </div>
                          </div>
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-input
                                v-model="creditTypeValue"
                                label="Value for Credit"
                                outlined
                                placeholder="e.g., 'Credit' or 'CR'"
                              ></q-input>
                            </div>
                          </div>
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-input
                                v-model="debitTypeValue"
                                label="Value for Debit"
                                outlined
                                placeholder="e.g., 'Debit' or 'DR'"
                              ></q-input>
                            </div>
                          </div>
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-select
                                v-model="fieldMapping.amount"
                                :options="csvHeaders"
                                label="Amount"
                                outlined
                                clearable
                                placeholder="Select a column or type a value"
                              ></q-select>
                            </div>
                          </div>
                        </div>
                        <div v-else-if="amountFormat === 'single'">
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-select
                                v-model="fieldMapping.amount"
                                :options="csvHeaders"
                                label="Amount (Positive = Credit, Negative = Debit)"
                                outlined
                                clearable
                                placeholder="Select a column or type a value"
                              ></q-select>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div class="row">
                      <div class="col">
                        <q-btn color="primary" @click="previewBankTransactionsData" :disabled="importing || !isFieldMappingValid"> Preview Data </q-btn>
                      </div>
                    </div>
                  </div>
                </div>
                <div v-else-if="importType === 'everyDollarBudget'">
                  <q-file
                    v-model="everyDollarFile"
                    label="Upload EveryDollar Budget JSON"
                    accept=".json"
                    @update:model-value="handleEveryDollarFileUpload"
                    :disabled="importing"
                  ></q-file>
                  <div class="row q-mt-md">
                    <div class="col">
                      <q-btn color="primary" @click="previewEveryDollarBudget" :disabled="importing || !everyDollarFile"
                        >Preview Recommendations</q-btn
                      >
                    </div>
                  </div>
                  <div v-if="everyDollarRecommendations.length > 0" class="q-mt-lg">
                    <h3>Recommended Updates for {{ recommendedMonth }}</h3>
                    <q-table
                      :rows="everyDollarRecommendations"
                      :columns="everyDollarColumns"
                      row-key="id"
                      flat
                      bordered
                    ></q-table>
                  </div>
                </div>
                <div v-else-if="importType === 'entities'">
                  <q-file
                    v-model="entitiesFile"
                    label="Upload Entities CSV/JSON"
                    accept=".csv,.json"
                    @update:model-value="handleFileUpload"
                    :disabled="importing"
                  ></q-file>
                </div>
                <div v-else-if="importType === 'accountsAndSnapshots'">
                  <q-file
                    v-model="accountsSnapshotsFile"
                    label="Upload Accounts and Snapshots CSV"
                    accept=".csv"
                    @update:model-value="handleAccountsAndSnapshotsImport"
                    :disabled="importing"
                  ></q-file>
                </div>
                <div v-else>
                  <q-file
                    v-model="selectedFiles"
                    label="Upload Budget or Transactions CSV/JSON"
                    accept=".csv,.json"
                    multiple
                    @update:model-value="handleFileUpload"
                    :disabled="importing || (!selectedEntityId && importType !== 'entities')"
                  ></q-file>
                </div>

                <!-- Preview Dialog -->
                <q-dialog v-model="showPreview" max-width="1000px">
                  <q-card>
                    <q-card-section>Preview Data</q-card-section>
                    <q-card-section>
                      <q-tabs v-model="previewTab">
                        <q-tab name="entities" v-if="previewData.entities?.length > 0" label="Entities" />
                        <q-tab name="categories" v-if="previewData.categories?.length > 0" label="Categories" />
                        <q-tab name="transactions" v-if="previewData.transactions?.length > 0" label="Transactions" />
                        <q-tab name="bankTransactions" v-if="previewBankTransactions.length > 0" label="Bank Transactions" />
                        <q-tab name="accountsAndSnapshots" v-if="previewData.accountsAndSnapshots?.length > 0" label="Accounts/Snapshots" />
                      </q-tabs>
                      <q-tab-panels v-model="previewTab">
                        <q-tab-panel name="entities">
                          <q-table :columns="entityColumns" :rows="previewData.entities" :pagination="{ rowsPerPage: 5 }" class="q-mt-lg"></q-table>
                        </q-tab-panel>
                        <q-tab-panel name="categories">
                          <q-table :columns="categoryColumns" :rows="previewData.categories" :pagination="{ rowsPerPage: 5 }" class="q-mt-lg"></q-table>
                        </q-tab-panel>
                        <q-tab-panel name="transactions">
                          <q-table :columns="transactionColumns" :rows="previewData.transactions" :pagination="{ rowsPerPage: 5 }" class="q-mt-lg">
                            <template #body-cell-categories="slotProps">
                              <span>{{ formatCategories(slotProps.row.categories) }}</span>
                            </template>
                          </q-table>
                        </q-tab-panel>
                        <q-tab-panel name="bankTransactions">
                          <q-table
                            :columns="bankTransactionPreviewColumns"
                            :rows="previewBankTransactions"
                            :pagination="{ rowsPerPage: 5 }"
                            class="q-mt-lg"
                          ></q-table>
                        </q-tab-panel>
                        <q-tab-panel name="accountsAndSnapshots">
                          <q-table
                            :columns="accountsAndSnapshotsColumns"
                            :rows="previewData.accountsAndSnapshots"
                            :pagination="{ rowsPerPage: 5 }"
                            class="q-mt-lg"
                          ></q-table>
                        </q-tab-panel>
                      </q-tab-panels>
                      <q-banner v-if="previewErrors.length > 0" type="negative" class="q-mt-lg">
                        <ul>
                          <li v-for="(error, index) in previewErrors" :key="index">
                            {{ error }}
                          </li>
                        </ul>
                      </q-banner>
                    </q-card-section>
                    <q-card-actions>
                      <q-space></q-space>
                      <q-btn color="negative" @click="showPreview = false">Cancel</q-btn>
                      <q-btn color="primary" @click="confirmImport" :disabled="previewErrors.length > 0 || importRunning"> Import </q-btn>
                    </q-card-actions>
                  </q-card>
                </q-dialog>

                <!-- Overwrite Dialog -->
                <q-dialog v-model="showOverwriteDialog" max-width="500px">
                  <q-card>
                    <q-card-section>Overwrite Warning</q-card-section>
                    <q-card-section>
                      Budgets already exist for the following months:
                      <div class="q-px-xl">
                        <ul>
                          <li v-for="month in overwriteMonths" :key="month">
                            {{ month }}
                          </li>
                        </ul>
                      </div>
                      Do you want to overwrite them?
                    </q-card-section>
                    <q-card-actions>
                      <q-space></q-space>
                      <q-btn color="negative" @click="showOverwriteDialog = false">Cancel</q-btn>
                      <q-btn color="primary" @click="proceedWithImport">Overwrite</q-btn>
                    </q-card-actions>
                  </q-card>
                </q-dialog>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </q-tab-panel>

      <!-- Export Tab -->
      <q-tab-panel name="export">
        <div class="row">
          <div class="col col-12">
            <q-card flat bordered class="bg-white q-pa-md" rounded>
              <q-card-section>Export Data</q-card-section>
              <q-card-section>
                <q-btn color="primary" @click="exportDataToCSV" :loading="exporting">Export All Data</q-btn>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </q-tab-panel>
    </q-tab-panels>

    <!-- Snackbar handled via $q.notify -->
    <!-- Entity Form Dialog -->
    <q-dialog v-model="showEntityForm" max-width="1000px" persistent>
      <entity-form :entity-id="''" @save="handleEntitySave" @cancel="showEntityForm = false" />
    </q-dialog>

  </q-page>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unnecessary-type-assertion */
import { ref, onMounted, computed, watch } from "vue";
import { useQuasar, QSpinner } from 'quasar';
import type { QTableColumn } from 'quasar';
import { auth } from "../firebase/init";
import { dataAccess } from "../dataAccess";
import Papa from "papaparse";
import type { Budget, Transaction, Account, Entity, ImportedTransaction, ImportedTransactionDoc } from "../types";
import type { EntityType } from "../types";
import { useRouter } from "vue-router";
import { v4 as uuidv4 } from "uuid";
import { useBudgetStore } from "../store/budget";
import { useFamilyStore } from "../store/family";
import EntityForm from "../components/EntityForm.vue";
import {
  timestampToDate,
  toBudgetMonth,
  stringToFirestoreTimestamp,
  parseAmount,
  todayISO,
  formatCurrency,
} from "../utils/helpers";
import JSZip from "jszip";
import { saveAs } from "file-saver";
import { Timestamp } from "firebase/firestore";
import { createBudgetForMonth } from "../utils/budget";

const familyStore = useFamilyStore();
const budgetStore = useBudgetStore();
const router = useRouter();
const $q = useQuasar();
const budgets = ref<Budget[]>([]);
const familyId = ref<string | null>(null);
const transactions = ref<Transaction[]>([]);
const exporting = ref(false);
const importing = ref(false);
const activeTab = ref<string>("import");
const availableAccounts = ref<Account[]>([]);
const selectedAccountId = ref<string>("");
const selectedEntityId = ref<string>("");
const importError = ref<string | null>(null);
const importSuccess = ref<string | null>(null);
const previewData = ref<{
  entities: any[];
  categories: any[];
  transactions: any[];
  accountsAndSnapshots: any[];
}>({
  entities: [],
  categories: [],
  transactions: [],
  accountsAndSnapshots: [],
});
const previewErrors = ref<string[]>([]);
const showPreview = ref(false);
const showOverwriteDialog = ref(false);
const overwriteMonths = ref<string[]>([]);
const pendingImportData = ref<{
  budgetsById: Map<string, Budget>;
  budgetIdMap: Map<string, string>;
  entitiesById?: Map<string, Entity>;
  accountsAndSnapshots?: any[];
} | null>(null);
const previewTab = ref("categories");
const importType = ref("bankTransactions");
const importTypes = [
  { label: "Entities", value: "entities" },
  { label: "Budget/Transactions", value: "budgetTransactions" },
  { label: "EveryDollar Budget JSON", value: "everyDollarBudget" },
  { label: "Bank/Card Transactions", value: "bankTransactions" },
  { label: "Accounts/Snapshots", value: "accountsAndSnapshots" },
];

watch(importType, async (val) => {
  if (val === "bankTransactions" && availableAccounts.value.length === 0 && familyId.value) {
    try {
      const accounts = await dataAccess.getAccounts(familyId.value);
      availableAccounts.value = accounts.filter(
        (account) => account.type === "Bank" || account.type === "CreditCard"
      );
      if (availableAccounts.value.length > 0 && !selectedAccountId.value) {
        selectedAccountId.value = availableAccounts.value[0].id;
      }
    } catch (err: any) {
      showSnackbar(`Error loading accounts: ${err.message}`, "negative");
    }
  }
});
const selectedFiles = ref<File[]>([]);
const bankTransactionsFile = ref<File | null>(null);
const entitiesFile = ref<File | null>(null);
const accountsSnapshotsFile = ref<File | null>(null);
const everyDollarFile = ref<File | null>(null);

interface EveryDollarRecommendation {
  id: string;
  group: string;
  item: string;
  budgeted: string;
  spent: string;
  recommended: string;
}

const everyDollarRecommendations = ref<EveryDollarRecommendation[]>([]);
const recommendedMonth = ref('');
const everyDollarColumns: QTableColumn[] = [
  { name: 'group', label: 'Group', field: 'group', align: 'left' as const },
  { name: 'item', label: 'Item', field: 'item', align: 'left' as const },
  { name: 'budgeted', label: 'Budgeted', field: 'budgeted', align: 'right' as const },
  { name: 'spent', label: 'Spent', field: 'spent', align: 'right' as const },
  { name: 'recommended', label: 'Recommended', field: 'recommended', align: 'right' as const },
];

// Bank/Card Transactions Import
const previewBankTransactions = ref<any[]>([]);
const csvHeaders = ref<string[]>([]);
const rawCsvData = ref<any[]>([]);
const commonBankTransactionFields = ref([
  { key: "payee", label: "Payee", required: true },
  { key: "postedDate", label: "Posted Date", required: true },
  { key: "status", label: "Status (U, C, R)", required: false },
  { key: "checkNumber", label: "Check Number", required: false },
]);
const fieldMapping = ref<Record<string, string>>({});
const amountFormat = ref<"separate" | "type" | "single">("separate");
const amountFormatOptions = [
  { label: "Separate Credit/Debit Columns", value: "separate" },
  { label: "Transaction Type Column", value: "type" },
  { label: "Single Amount (Positive/Negative)", value: "single" },
];
const creditTypeValue = ref("Credit");
const debitTypeValue = ref("Debit");
const bankTransactionPreviewColumns = [
  { name: 'payee', label: 'Payee', field: 'payee' },
  { name: 'postedDate', label: 'Posted Date', field: 'postedDate' },
  { name: 'status', label: 'Status', field: 'status' },
  { name: 'creditAmount', label: 'Credit Amount', field: 'creditAmount' },
  { name: 'debitAmount', label: 'Debit Amount', field: 'debitAmount' },
  { name: 'checkNumber', label: 'Check Number', field: 'checkNumber' },
];

// Headers for Entity Preview
const entityColumns = [
  { name: 'name', label: 'Name', field: 'name' },
  { name: 'type', label: 'Type', field: 'type' },
  { name: 'ownerEmail', label: 'Owner Email', field: 'ownerEmail' },
];
const showEntityForm = ref(false);

// Headers for Accounts/Snapshots Preview
const accountsAndSnapshotsColumns = [
  { name: 'accountName', label: 'Account Name', field: 'accountName' },
  { name: 'type', label: 'Type', field: 'type' },
  { name: 'accountNumber', label: 'Account Number', field: 'accountNumber' },
  { name: 'institution', label: 'Institution', field: 'institution' },
  { name: 'date', label: 'Date', field: 'date' },
  { name: 'balance', label: 'Balance', field: 'balance' },
  { name: 'interestRate', label: 'Interest Rate', field: 'interestRate' },
  { name: 'appraisedValue', label: 'Appraised Value', field: 'appraisedValue' },
  { name: 'address', label: 'Address', field: 'address' },
];

// Headers for Budget and Transaction Previews
const categoryColumns = [
  { name: 'budgetid', label: 'BudgetId', field: 'budgetid' },
  { name: 'budgetmonth', label: 'Budget Month', field: 'budgetmonth' },
  { name: 'incomeTarget', label: 'Income Target', field: 'incomeTarget' },
  { name: 'category', label: 'Category', field: 'category' },
  { name: 'group', label: 'Group', field: 'group' },
  { name: 'isfund', label: 'IsFund', field: 'isfund' },
  { name: 'target', label: 'Target', field: 'target' },
  { name: 'carryover', label: 'Carryover', field: 'carryover' },
  { name: 'entityId', label: 'EntityId', field: 'entityId' },
  { name: 'entityName', label: 'EntityName', field: 'entityName' },
];
const transactionColumns = [
  { name: 'budgetid', label: 'BudgetId', field: 'budgetid' },
  { name: 'transactionid', label: 'TransactionId', field: 'transactionid' },
  { name: 'transactiondate', label: 'Transaction Date', field: 'transactiondate' },
  { name: 'categories', label: 'Categories', field: 'categories' },
  { name: 'merchant', label: 'Merchant', field: 'merchant' },
  { name: 'isincome', label: 'IsIncome', field: 'isincome' },
  { name: 'amount', label: 'Amount', field: 'amount' },
  { name: 'notes', label: 'Notes', field: 'notes' },
  { name: 'recurring', label: 'Recurring', field: 'recurring' },
  { name: 'recurringinterval', label: 'RecurringInterval', field: 'recurringinterval' },
  { name: 'entityId', label: 'EntityId', field: 'entityId' },
  { name: 'entityName', label: 'EntityName', field: 'entityName' },
];
const importRunning = ref(false);

const entityOptions = computed(() => {
  return (familyStore.family?.entities || []).map((entity: Entity) => ({
    id: entity.id,
    name: entity.name,
  }));
});

const formattedAccounts = computed(() => {
  const ret = availableAccounts.value.map((account) => ({
    title: `${account.name} (*${account.accountNumber ? account.accountNumber.slice(-4) : "N/A"})`,
    value: account.id,
  }));
  return ret;
});

const isFieldMappingValid = computed(() => {
  const requiredCommonFields = commonBankTransactionFields.value.every((field) => !field.required || !!fieldMapping.value[field.key]);
  if (amountFormat.value === "separate") {
    return requiredCommonFields && !!fieldMapping.value.creditAmount && !!fieldMapping.value.debitAmount;
  } else if (amountFormat.value === "type") {
    return requiredCommonFields && !!fieldMapping.value.transactionType && !!creditTypeValue.value && !!debitTypeValue.value && !!fieldMapping.value.amount;
  } else if (amountFormat.value === "single") {
    return requiredCommonFields && !!fieldMapping.value.amount;
  }
  return false;
});

onMounted(async () => {
  await loadAllData();
});

async function loadAllData() {
  $q.loading.show({
    message: 'Loading data...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });
  try {
    const user = auth.currentUser;
    if (!user) return;

    await familyStore.loadFamily(user.uid);

    const family = await familyStore.getFamily();
    if (!family) throw new Error("User has no family assigned");
    familyId.value = family.id;

    // Set selectedEntityId to the first entity if available
    const entities = family.entities || [];
    if (entities.length > 0 && !selectedEntityId.value) {
      selectedEntityId.value = entities[0].id;
    }

    // Load full budget details for export
    const accessibleBudgets = await dataAccess.loadAccessibleBudgets(user.uid);
    const fullBudgets = await Promise.all(
      accessibleBudgets.map((b) => dataAccess.getBudget(b.budgetId))
    );
    budgets.value = fullBudgets.filter((b): b is Budget => b !== null);

    transactions.value = budgets.value.flatMap((budget) => budget.transactions || []);

    const accounts = await dataAccess.getAccounts(familyId.value);
    availableAccounts.value = accounts.filter((account) => account.type === "Bank" || account.type === "CreditCard");
    if (availableAccounts.value.length > 0 && !selectedAccountId.value) {
      selectedAccountId.value = availableAccounts.value[0].id;
    }
  } catch (error: any) {
    console.error("Error loading data:", error);
    showSnackbar(`Error loading data: ${error.message}`, "negative");
  } finally {
    $q.loading.hide();
  }
}

function openCreateEntityDialog() {
  showEntityForm.value = true;
}

async function handleEntitySave() {
  const user = auth.currentUser;
  if (!user || !familyId.value) {
    showSnackbar("Cannot create entity: invalid user or family data", "negative");
    return;
  }

  try {
    await familyStore.loadFamily(user.uid); // Refresh family data
    const entities = familyStore.family?.entities || [];
    if (entities.length > 0) {
      selectedEntityId.value = entities[entities.length - 1].id; // Auto-select latest entity
    }
    showSnackbar("Entity created successfully", "success");
    showEntityForm.value = false;
  } catch (error: any) {
    showSnackbar(`Error creating entity: ${error.message}`, "negative");
  }
}

async function handleFileUpload(files: File | File[] | FileList | null) {
  selectedFiles.value = Array.isArray(files)
    ? files
    : files instanceof FileList
    ? Array.from(files)
    : files
    ? [files as File]
    : [];

  if (!selectedFiles.value.length) return;

  $q.loading.show({
    message: 'Importing data...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });
  importError.value = null;
  importSuccess.value = null;
  previewData.value = { entities: [], categories: [], transactions: [], accountsAndSnapshots: [] };
  previewErrors.value = [];

  try {
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

    if (importType.value === "entities") {
      await handleEntityImport();
    } else if (importType.value === "budgetTransactions") {
      if (!selectedEntityId.value) {
        throw new Error("Please select an entity before importing budgets or transactions");
      }
      await handleBudgetTransactionImport();
      console.log('handleBudgetTransactionImport done');
    }

    if (previewErrors.value.length > 0) {
      console.log('Validation errors found.', previewErrors.value);
      importError.value = "Validation errors found.";
    } else if (
      previewData.value.entities.length > 0 ||
      previewData.value.categories.length > 0 ||
      previewData.value.transactions.length > 0 ||
      previewData.value.accountsAndSnapshots.length > 0
    ) {
      showPreview.value = true;
      previewTab.value = previewData.value.entities.length > 0 ? "entities" : "categories";
    } else {
      console.log('VNo valid data found.');
      importError.value = "No valid data found.";
    }
  } catch (error: any) {
    console.error("Error parsing data:", error);
    importError.value = `Failed to parse data: ${error.message}`;
  } finally {
    $q.loading.hide();
  }
}

async function handleEntityImport() {
  const entitiesById = new Map<string, Entity>();

  for (const file of selectedFiles.value) {
    const text = await file.text();
    if (file.name.endsWith(".json")) {
      const jsonData = JSON.parse(text);
      if (Array.isArray(jsonData.entities)) {
        jsonData.entities.forEach((entity: any, index: number) => {
          if (!entity.name || !entity.type) {
            previewErrors.value.push(`File ${file.name}, Entity ${index + 1}: Name and type are required`);
            return;
          }
          const entityId = entity.id || uuidv4();
          const newEntity: Entity = {
            id: entityId,
            familyId: familyId.value || "",
            name: entity.name,
            type: entity.type as EntityType,
            ownerUid: auth.currentUser?.uid || "",
            members: entity.members || [{ uid: auth.currentUser?.uid || "", email: auth.currentUser?.email || "", role: "Admin" }],
            createdAt: Timestamp.fromDate(new Date()),
            updatedAt: Timestamp.fromDate(new Date()),
          };
          entitiesById.set(entityId, newEntity);
          previewData.value.entities.push({
            name: entity.name,
            type: entity.type,
            ownerEmail: auth.currentUser?.email || "",
          });
        });
      }
    } else if (file.name.endsWith(".csv")) {
      const result = Papa.parse(text, {
        header: true,
        skipEmptyLines: true,
        transformHeader: (header: string) => header.toLowerCase().replace(/\s+/g, ""),
      });
      if (result.errors.length > 0) {
        throw new Error(result.errors.map((e: any) => e.message).join("; "));
      }
      const data = result.data as any[];
      data.forEach((row, index) => {
        if (!row.name || !row.type) {
          previewErrors.value.push(`File ${file.name}, Row ${index + 1}: Name and type are required`);
          return;
        }
        const entityId = row.id || uuidv4();
        const newEntity: Entity = {
          id: entityId,
          familyId: familyId.value || "",
          name: row.name,
          type: row.type as EntityType,
          ownerUid: auth.currentUser?.uid || "",
          members: row.members ? JSON.parse(row.members) : [{ uid: auth.currentUser?.uid || "", email: auth.currentUser?.email || "", role: "Admin" }],
          createdAt: Timestamp.fromDate(new Date()),
          updatedAt: Timestamp.fromDate(new Date()),
        };
        entitiesById.set(entityId, newEntity);
        previewData.value.entities.push({
          name: row.name,
          type: row.type,
          ownerEmail: auth.currentUser?.email || "",
        });
      });
    }
  }

  pendingImportData.value = {
    budgetsById: new Map(),
    budgetIdMap: new Map(),
    entitiesById,
  };
}

async function handleBudgetTransactionImport() {
  const user = auth.currentUser;
  if (!user) throw new Error("User not authenticated");
  const budgetsById = new Map<string, Budget>();
  const monthToId = new Map<string, string>();
  const transactionMap = new Map<string, Transaction>();

  let budgetCSVData = null;
  let budgetCSVFile = null;
  let transactionCSVData = null;
  let transactionCSVFile = null;

  for (const file of selectedFiles.value) {
    if (!(file instanceof Blob)) {
      previewErrors.value.push(`File ${file.name || "unknown"} is not a valid file object`);
      continue;
    }

    const text = await file.text();
    let data: any[];

    if (file.name.endsWith(".json")) {
      const jsonData = JSON.parse(text);
      if (jsonData.familyId && jsonData.month) {
        const budgetId = uuidv4();
        budgetsById.set(budgetId, {
          budgetId: budgetId,
          budgetMonth: jsonData.month,
          month: jsonData.month,
          incomeTarget: jsonData.incomeTarget || 0,
          categories: jsonData.categories || [],
          transactions: jsonData.transactions || [],
          merchants: jsonData.merchants || [],
          familyId: jsonData.familyId,
          label: jsonData.label || `Imported Budget ${jsonData.month}`,
          entityId: selectedEntityId.value,
        });

        jsonData.categories.forEach((cat: any) => {
          previewData.value.categories.push({
            budgetid: budgetId,
            budgetmonth: jsonData.month,
            category: cat.name,
            group: cat.group || "",
            isfund: cat.isFund,
            target: cat.target || 0,
            carryover: cat.carryover || 0,
            entityId: selectedEntityId.value,
            entityName: familyStore.family?.entities?.find((e) => e.id === selectedEntityId.value)?.name || "",
          });
        });

        (jsonData.transactions || []).forEach((tx: any) => {
          const transactionId = tx.id || uuidv4();
          const categories = typeof tx.categories === "string" ? JSON.parse(tx.categories) : tx.categories;
          const transactionData: Transaction = {
            id: transactionId,
            userId: user.uid,
            budgetMonth: tx.budgetMonth || jsonData.month,
            budgetId: budgetId,
            date: tx.date,
            merchant: tx.merchant || "",
            categories: categories,
            amount: tx.amount,
            notes: tx.notes || "",
            recurring: tx.recurring || false,
            recurringInterval: tx.recurringInterval || "Monthly",
            isIncome: tx.isIncome || false,
            accountNumber: tx.accountNumber || "",
            accountSource: tx.accountSource || "",
            postedDate: tx.postedDate || "",
            status: tx.status || "U",
            entityId: selectedEntityId.value,
            taxMetadata: [],
          };
          transactionMap.set(transactionId, transactionData);
          previewData.value.transactions.push({
            budgetid: budgetId,
            transactionid: transactionId,
            transactiondate: tx.date,
            categories: JSON.stringify(categories),
            merchant: tx.merchant || "",
            isincome: tx.isIncome ? "true" : "false",
            amount: tx.amount,
            notes: tx.notes || "",
            recurring: tx.recurring ? "true" : "false",
            recurringinterval: tx.recurringInterval || "Monthly",
            entityId: selectedEntityId.value,
            entityName: familyStore.family?.entities?.find((e) => e.id === selectedEntityId.value)?.name || "",
          });
        });
      }
      continue;
    } else {
      const result = Papa.parse(text, {
        header: true,
        skipEmptyLines: true,
        transformHeader: (header: any) => header.toLowerCase().replace(/\s+/g, ""),
      });

      if (result.errors.length > 0) {
        throw new Error(result.errors.map((e: any) => e.message).join("; "));
      }

      data = result.data as any[];
      if (data.length === 0) {
        previewErrors.value.push(`File ${file.name} is empty`);
        continue;
      }

      const headers = Object.keys(data[0]);
      const isBudgetCsv = headers.includes("isfund") && headers.includes("target");
      const isTransactionCsv = headers.includes("transactiondate") && (headers.includes("income_expense") || headers.includes("isincome"));

      if (isBudgetCsv) {
        console.log(`budget found ${file.name}`);
        budgetCSVData = data;
        budgetCSVFile = file.name;
      } else if (isTransactionCsv) {
        console.log(`transactions found ${file.name}`);
        transactionCSVData = data;
        transactionCSVFile = file.name;
      }
    }
  }

  if (budgetCSVData) {
    console.log(`budget found`, budgetCSVData);
    budgetCSVData.forEach((row, index) => {
      if (!row.budgetmonth) {
        previewErrors.value.push(`File ${budgetCSVFile}, Row ${index + 1}: budgetMonth is required`);
        return;
      }
      const month = row.budgetmonth.length > 7 ? row.budgetmonth.slice(0, 7) : row.budgetmonth;
      let budgetId = monthToId.get(month);
      if (!budgetId) {
        budgetId = uuidv4();
        monthToId.set(month, budgetId);
      }
      if (!budgetsById.has(budgetId)) {
        budgetsById.set(budgetId, {
          budgetId: budgetId,
          budgetMonth: month,
          month: month,
          incomeTarget: parseFloat(row.incometarget) || 0,
          categories: [],
          transactions: [],
          familyId: familyId.value || "",
          label: `Imported Budget ${month}`,
          entityId: selectedEntityId.value,
          merchants: [],
        });
      }

      const budget = budgetsById.get(budgetId)!;
      if (!row.category) {
        previewErrors.value.push(`File ${budgetCSVFile}, Row ${index + 1}: Category is required`);
        return;
      }
      budget.categories.push({
        name: row.category,
        group: row.group || "",
        isFund: row.isfund === "true" || row.isfund === "1",
        target: parseFloat(row.target) || 0,
        carryover: parseFloat(row.carryover) || 0,
      });
    });
  }

  if (transactionCSVData) {
    console.log(`transactions found`, transactionCSVData);
    transactionCSVData.forEach((row, index) => {
      let month = row.budgetmonth;
      if (!month && row.budgetid) {
        const match = row.budgetid.match(/_(\d{4}-\d{2})$/);
        if (match) {
          month = match[1];
        }
      }
      if (!month) {
        try {
          month = toBudgetMonth(row.transactiondate);
        } catch {
          previewErrors.value.push(`File ${transactionCSVFile}, Row ${index + 1}: Invalid transaction date: ${row.transactiondate}`);
          return;
        }
      }
      if (!month) {
        previewErrors.value.push(`File ${transactionCSVFile}, Row ${index + 1}: budgetMonth is required and could not be derived`);
        return;
      }

      let budgetId = monthToId.get(month);
      if (!budgetId) {
        budgetId = uuidv4();
        monthToId.set(month, budgetId);
        budgetsById.set(budgetId, {
          budgetId: budgetId,
          budgetMonth: month,
          month: month,
          incomeTarget: 0,
          categories: [],
          transactions: [],
          merchants: [],
          familyId: familyStore.family?.id || '',
          label: `Imported Budget ${month}`,
          entityId: selectedEntityId.value,
        });
      }
      const amount = parseFloat(row.amount);
      if (isNaN(amount)) {
        previewErrors.value.push(`File ${transactionCSVFile}, Row ${index + 1}: Amount must be a number`);
        return;
      }

      if (!budgetsById.has(budgetId)) {
        console.log('no budget');
        budgetsById.set(budgetId, {
          budgetId: budgetId,
          budgetMonth: month,
          month: month,
          incomeTarget: 0,
          categories: [],
          transactions: [],
          familyId: familyId.value || "",
          label: `Imported Budget ${month}`,
          entityId: selectedEntityId.value,
          merchants: [],
        });
      }

      const transactionId = row.transactionid || uuidv4();
      let categories: { category: string; amount: number }[] = [];

      try {
        if (row.categories && typeof row.categories === "string") {
          if (row.categories.startsWith("[")) {
            categories = JSON.parse(row.categories);
            if (!Array.isArray(categories) || !categories.every((c) => c.category && typeof c.amount === "number")) {
              throw new Error("Invalid categories format");
            }
          } else if (row.issplit === "true" || row.issplit === "1") {
            categories = row.categories.split(";").map((catStr: string) => {
              const match = catStr.trim().match(/([^:]+):\s*(\d+\.?\d*)\s*(?:\(([^)]+)\))?/);
              if (!match) throw new Error(`Invalid category format: ${catStr}`);
              return {
                category: match[1].trim(),
                amount: parseFloat(match[2]),
              };
            });
          } else {
            categories = [{ category: row.categories || row.category || "", amount: amount }];
          }
        } else {
          categories = [{ category: row.category || "", amount: amount }];
        }
      } catch (e: any) {
        console.log(e);
        previewErrors.value.push(`File ${transactionCSVFile}, Row ${index + 1}: Invalid categories format - ${e.message}`);
        return;
      }

      const transactionData: Transaction = {
        id: transactionId,
        userId: user.uid,
        budgetMonth: month,
        budgetId: budgetId,
        date: row.transactiondate,
        merchant: row.merchant || "",
        categories: categories,
        amount: amount,
        notes: row.notes || "",
        recurring: row.recurring === "true" || row.recurring === "1",
        recurringInterval: row.recurringinterval || "Monthly",
        isIncome: row.isincome === "true" || row.isincome === "1" || row.income_expense?.toLowerCase() === "income",
        accountNumber: row.accountNumber || "",
        accountSource: row.accountSource || "",
        postedDate: row.postedDate || "",
        status: row.status || "U",
        entityId: selectedEntityId.value,
        taxMetadata: [],
      };

      transactionMap.set(transactionId, transactionData);
    });

    transactionMap.forEach((transaction) => {
      const budget = budgetsById.get(transaction.budgetId);
      if (budget) {
        budget.transactions.push(transaction);
      } else {
        console.warn(`No budget found for transaction with budgetId: ${transaction.budgetId}`);
      }
    });
  }

  previewData.value.categories = [];
  budgetsById.forEach((budget) => {
    const entityName = familyStore.family?.entities?.find((e) => e.id === budget.entityId)?.name || "";
    const budgetCategories = budget.categories.map((category) => ({
      budgetid: budget.budgetId,
      budgetmonth: budget.budgetMonth,
      incomeTarget: budget.incomeTarget || 0,
      category: category.name,
      group: category.group || "",
      isfund: (category as any).isFund ?? (category as any).isfund,
      target: category.target || 0,
      carryover: category.carryover || 0,
      entityId: budget.entityId,
      entityName,
    }));
    previewData.value.categories.push(...budgetCategories);
  });

  previewData.value.transactions = Array.from(transactionMap.values()).map((transaction) => ({
    budgetid: transaction.budgetId,
    transactionid: transaction.id,
    transactiondate: transaction.date,
    categories: JSON.stringify(transaction.categories),
    merchant: transaction.merchant,
    isincome: transaction.isIncome ? "true" : "false",
    amount: transaction.amount,
    notes: transaction.notes,
    recurring: transaction.recurring ? "true" : "false",
    recurringinterval: transaction.recurringInterval,
    entityId: transaction.entityId,
    entityName: familyStore.family?.entities?.find((e) => e.id === transaction.entityId)?.name || "",
  }));

  console.log('previewData.value', previewData.value);
}

async function handleAccountsAndSnapshotsImport(files: File | File[] | FileList | null) {
  const file = Array.isArray(files)
    ? files[0]
    : files instanceof FileList
    ? files[0]
    : files || null;

  if (!file) return;

  $q.loading.show({
    message: 'Importing data...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });
  importError.value = null;
  importSuccess.value = null;
  previewData.value.accountsAndSnapshots = [];
  previewErrors.value = [];

  try {
    const text = await file.text();
    const lines = text
      .split("\n")
      .map((line) => line.trim())
      .filter((line) => line);
    if (lines.length < 2) {
      previewErrors.value.push("CSV file is empty or invalid");
      return;
    }

    const headers = lines[0].split(",").map((h) => h.trim());
    const entries = lines.slice(1).map((line) => {
      const values = line.split(",").map((v) => v.trim());
      const entry: any = {};
      headers.forEach((h, i) => {
        entry[h] = values[i] || "";
      });
      if (entry && entry.date) {
        try {
          entry.date = stringToFirestoreTimestamp(entry.date);
        } catch {
          entry.date = "";
        }
      } else {
        entry.date = "";
      }

      return {
        accountName: entry.accountName || "",
        type: entry.type || "",
        accountNumber: entry.accountNumber || undefined,
        institution: entry.institution || undefined,
        date: entry.date,
        balance: parseFloat(entry.balance) || 0,
        interestRate: parseFloat(entry.interestRate) || undefined,
        appraisedValue: parseFloat(entry.appraisedValue) || undefined,
        address: entry.address || undefined,
      };
    });

    const validTypes = ["Bank", "CreditCard", "Investment", "Property", "Loan"];
    for (const entry of entries) {
      if (!entry.accountName) {
        previewErrors.value.push("All entries must have an account name");
        return;
      }
      if (!validTypes.includes(entry.type)) {
        previewErrors.value.push(`Invalid account type: ${entry.type}`);
        return;
      }
      if (!entry.date) {
        previewErrors.value.push(`Invalid date in entry: ${entry.date}`);
        return;
      }
    }

    previewData.value.accountsAndSnapshots = entries;
    pendingImportData.value = {
      budgetsById: new Map(),
      budgetIdMap: new Map(),
      entitiesById: new Map(),
      accountsAndSnapshots: entries,
    };

    if (previewErrors.value.length > 0) {
      importError.value = "Validation errors found.";
    } else if (previewData.value.accountsAndSnapshots.length > 0) {
      showPreview.value = true;
      previewTab.value = "accountsAndSnapshots";
    } else {
      importError.value = "No valid data found.";
    }
  } catch (error: any) {
    console.error("Error parsing accounts and snapshots:", error);
    importError.value = `Failed to parse data: ${error.message}`;
  } finally {
    $q.loading.hide();
  }
}

async function handleBankTransactionsFileUpload(files: File | File[] | FileList | null) {
  const file = Array.isArray(files)
    ? files[0]
    : files instanceof FileList
    ? files[0]
    : files || null;

  if (!file) return;

  $q.loading.show({
    message: 'Importing bank transactions...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  importError.value = null;
  importSuccess.value = null;
  previewBankTransactions.value = [];
  previewErrors.value = [];
  csvHeaders.value = [];
  rawCsvData.value = [];
  fieldMapping.value = {};

  try {
    const text = await file.text();
    const result = Papa.parse(text, { header: true, skipEmptyLines: true });

    if (result.errors.length > 0) {
      throw new Error(result.errors.map((e) => e.message).join("; "));
    }

    const data = result.data as any[];
    if (data.length === 0) {
      previewErrors.value.push(`File ${file.name} is empty`);
      return;
    }

    csvHeaders.value = Object.keys(data[0]);
    rawCsvData.value = data;
  } catch (error: any) {
    console.error("Error parsing bank transactions:", error);
    importError.value = `Failed to parse data: ${error.message}`;
  } finally {
    $q.loading.hide();
  }
}

function handleEveryDollarFileUpload(file: File | File[] | FileList | null) {
  everyDollarRecommendations.value = [];
  if (Array.isArray(file)) {
    everyDollarFile.value = file[0] || null;
  } else if (file instanceof FileList) {
    everyDollarFile.value = file[0] || null;
  } else {
    everyDollarFile.value = file as File | null;
  }
}

async function previewEveryDollarBudget() {
  importError.value = null;
  importSuccess.value = null;
  everyDollarRecommendations.value = [];
  if (!everyDollarFile.value) {
    importError.value = 'No file selected';
    return;
  }
  try {
    importing.value = true;
    const text = await everyDollarFile.value.text();
    const data = JSON.parse(text);
    recommendedMonth.value = data.date ? toBudgetMonth(data.date) : '';
    const recs: EveryDollarRecommendation[] = [];
    if (Array.isArray(data.groups)) {
      data.groups.forEach((group: any) => {
        if (group.type !== 'expense') return;
        const groupLabel = group.label || '';
        (group.budgetItems || []).forEach((item: any) => {
          const budgeted = typeof item.amountBudgeted === 'number' ? item.amountBudgeted : 0;
          const spent = Array.isArray(item.allocations)
            ? item.allocations.reduce(
                (sum: number, a: any) => sum + (typeof a.amount === 'number' ? a.amount : 0),
                0,
              )
            : 0;
          const spentAbs = Math.abs(spent);
          if (spentAbs > budgeted) {
            recs.push({
              id: item.id || uuidv4(),
              group: groupLabel,
              item: item.label || '',
              budgeted: formatCurrency(budgeted / 100),
              spent: formatCurrency(spentAbs / 100),
              recommended: formatCurrency(spentAbs / 100),
            });
          }
        });
      });
    }
    everyDollarRecommendations.value = recs;
    if (recs.length === 0) {
      importSuccess.value = 'No budget updates recommended.';
    }
  } catch (err: any) {
    console.error('Error parsing EveryDollar budget:', err);
    importError.value = `Failed to parse EveryDollar budget: ${err.message}`;
  } finally {
    importing.value = false;
  }
}

function previewBankTransactionsData() {
  previewBankTransactions.value = [];
  previewErrors.value = [];

  try {
    const selectedAccount = availableAccounts.value.find((account) => account.id === selectedAccountId.value);
    if (!selectedAccount) {
      previewErrors.value.push("Selected account not found.");
      return;
    }

    previewBankTransactions.value = rawCsvData.value.map((row, index) => {
      const mappedRow: any = {};

      commonBankTransactionFields.value.forEach((field) => {
        const mappedField = fieldMapping.value[field.key] || "";
        mappedRow[field.key] = csvHeaders.value.includes(mappedField) ? row[mappedField] || "" : mappedField || "";
      });

      if (mappedRow.postedDate) {
        const yyyymmddRegex = /^\d{4}-\d{2}-\d{2}$/;
        if (yyyymmddRegex.test(mappedRow.postedDate)) {
          const date = new Date(mappedRow.postedDate);
          if (isNaN(date.getTime())) {
            previewErrors.value.push(`Row ${index + 1}: Invalid posted date format, expected YYYY-MM-DD (e.g., 2023-12-31)`);
          }
        } else {
          const parts = mappedRow.postedDate.split("/");
          if (parts.length === 3) {
            // eslint-disable-next-line prefer-const
            let [month, day, year] = parts;
            year = year.length === 2 ? `20${year}` : year;
            mappedRow.postedDate = `${year}-${month.padStart(2, "0")}-${day.padStart(2, "0")}`;
            const date = new Date(mappedRow.postedDate);
            if (isNaN(date.getTime())) {
              previewErrors.value.push(`Row ${index + 1}: Invalid posted date format, expected MM/DD/YYYY (e.g., 12/31/2023) or YYYY-MM-DD (e.g., 2023-12-31)`);
            }
          } else {
            previewErrors.value.push(`Row ${index + 1}: Invalid posted date format, expected MM/DD/YYYY (e.g., 12/31/2023) or YYYY-MM-DD (e.g., 2023-12-31)`);
          }
        }
      } else {
        previewErrors.value.push(`Row ${index + 1}: Posted Date is required`);
      }

      let creditAmount = 0;
      let debitAmount = 0;

      if (amountFormat.value === "separate") {
        const creditField = fieldMapping.value.creditAmount || "";
        const debitField = fieldMapping.value.debitAmount || "";
        creditAmount = parseAmount(csvHeaders.value.includes(creditField) ? row[creditField] : creditField);
        debitAmount = parseAmount(csvHeaders.value.includes(debitField) ? row[debitField] : debitField);
      } else if (amountFormat.value === "type") {
        const typeField = fieldMapping.value.transactionType || "";
        const amountField = fieldMapping.value.amount || "";
        const transactionType = csvHeaders.value.includes(typeField) ? row[typeField] || "" : typeField || "";
        const amount = parseAmount(csvHeaders.value.includes(amountField) ? row[amountField] : amountField);

        if (transactionType === creditTypeValue.value) {
          creditAmount = Math.abs(amount);
        } else if (transactionType === debitTypeValue.value) {
          debitAmount = Math.abs(amount);
        } else {
          previewErrors.value.push(
            `Row ${index + 1}: Transaction type "${transactionType}" does not match "${creditTypeValue.value}" or "${debitTypeValue.value}"`
          );
          creditAmount = 0;
          debitAmount = 0;
        }
      } else if (amountFormat.value === "single") {
        const amountField = fieldMapping.value.amount || "";
        const amount = parseAmount(csvHeaders.value.includes(amountField) ? row[amountField] : amountField);
        if (amount >= 0) {
          creditAmount = amount;
        } else {
          debitAmount = Math.abs(amount);
        }
      }

      if (!mappedRow.postedDate) {
        previewErrors.value.push(`Row ${index + 1}: Posted Date is required`);
      }
      if (!mappedRow.payee) {
        previewErrors.value.push(`Row ${index + 1}: Payee is required`);
      }
      if (mappedRow.status && !["U", "C", "R"].includes(mappedRow.status)) {
        previewErrors.value.push(`Row ${index + 1}: Status must be U, C, or R`);
      }
      if (creditAmount !== 0 && debitAmount !== 0 && amountFormat.value !== "single") {
        previewErrors.value.push(`Row ${index + 1}: Both Credit and Debit amounts are non-zero, which is invalid`);
      }

      return {
        accountNumber: selectedAccount.accountNumber || "",
        accountId: selectedAccountId.value,
        accountSource: selectedAccount.institution || "",
        payee: mappedRow.payee || "",
        postedDate: mappedRow.postedDate || "",
        status: mappedRow.status || "U",
        creditAmount: isNaN(creditAmount) ? 0 : creditAmount,
        debitAmount: isNaN(debitAmount) ? 0 : debitAmount,
        checkNumber: mappedRow.checkNumber || "",
        entityId: selectedEntityId.value,
      };
    });

    if (previewBankTransactions.value.length > 0) {
      showPreview.value = true;
      previewTab.value = "bankTransactions";
    } else {
      importError.value = "No valid data found.";
    }

    if (previewErrors.value.length > 0) {
      importError.value = "Validation errors found. Please review the preview.";
    }
  } catch (error: any) {
    console.error("Error previewing bank transactions:", error);
    importError.value = `Failed to preview data: ${error.message}`;
  }
}

async function confirmImport() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar("User not authenticated", "negative");
    return;
  }
  if (!familyId.value) {
    showSnackbar("Cannot import without a Family/Org", "negative");
    return;
  }

  if (importType.value === "entities") {
    try {
      $q.loading.show({
        message: 'Importing entities...',
        spinner: QSpinner,
        spinnerColor: 'primary',
        spinnerSize: 50,
        customClass: 'q-ml-sm flex items-center justify-center',
      });
      const entitiesById = pendingImportData.value?.entitiesById || new Map();
      for (const entity of entitiesById.values()) {
        await familyStore.createEntity(familyId.value, entity);
      }
      await familyStore.loadFamily(user.uid); // Refresh family data
      if (entitiesById.size > 0) {
        selectedEntityId.value = entitiesById.keys().next().value; // Auto-select first imported entity
      }
      showSnackbar(`Imported ${entitiesById.size} entities`, "success");
      showPreview.value = false;
      previewData.value.entities = [];
    } catch (error: any) {
      console.error("Error importing entities:", error);
      showSnackbar(`Failed to import entities: ${error.message}`, "negative");
    } finally {
      $q.loading.hide();
      pendingImportData.value = null;
    }
  } else if (importType.value === "budgetTransactions") {
    const budgetsById = new Map<string, Budget>();
    const budgetIdMap = new Map<string, string>();
    const budgetIdToIncomeTarget = new Map<string, number>();

    try {
      importRunning.value = true;

      if (previewData.value.categories.length > 0) {
        const budgetIdToMonth = new Map<string, string>();
        previewData.value.categories.forEach((category) => {
          const originalBudgetId = category.budgetid;
          const fullMonth = category.budgetmonth;
          if (!fullMonth) {
            console.error(`Missing Budget Month for BudgetId ${originalBudgetId}`);
            return;
          }
          const month = fullMonth.slice(0, 7);
          if (!budgetIdToMonth.has(originalBudgetId)) {
            budgetIdToMonth.set(originalBudgetId, month);
            budgetIdToIncomeTarget.set(originalBudgetId, Number(category.incomeTarget) || 0);
          }
        });

        for (const [originalBudgetId, month] of budgetIdToMonth) {
          const firebaseBudgetId = uuidv4();
          budgetIdMap.set(originalBudgetId, firebaseBudgetId);
          // Create or update the budget
          const budget = await createBudgetForMonth(
            month,
            familyId.value!,
            user.uid,
            selectedEntityId.value,
          );
          budget.familyId = familyId.value!;
          budget.entityId = selectedEntityId.value;
          budget.incomeTarget = budgetIdToIncomeTarget.get(originalBudgetId) || 0;
          budget.categories = [];
          budgetsById.set(firebaseBudgetId, budget);
        }

        budgets.value = Array.from(budgetStore.budgets.values());

        previewData.value.categories.forEach((category) => {
          const originalBudgetId = category.budgetid;
          const firebaseBudgetId = budgetIdMap.get(originalBudgetId);
          if (!firebaseBudgetId) {
            console.error(`BudgetId ${originalBudgetId} not found in budgetIdMap`);
            return;
          }
          const budget = budgetsById.get(firebaseBudgetId)!;
          const existingCategory = budget.categories.find((c) => c.name === category.category);
          if (!existingCategory) {
            budget.categories.push({
              name: category.category,
              target: category.target,
              isFund: category.isfund === "true" || category.isfund === "1" || category.isfund === true,
              group: category.group || "",
              carryover: category.carryover || 0,
            });
          } else {
            existingCategory.target = category.target;
            existingCategory.isFund = category.isfund === "true" || category.isfund === "1" || category.isfund === true;
            existingCategory.group = category.group || "";
            existingCategory.carryover = category.carryover || 0;
          }
        });
      }

      if (previewData.value.transactions.length > 0) {
        previewData.value.transactions.forEach((txPreview) => {
          const originalBudgetId = txPreview.budgetid;
          const firebaseBudgetId = budgetIdMap.get(originalBudgetId);
          if (!firebaseBudgetId) {
            console.warn(`Transaction with BudgetId ${originalBudgetId} has no corresponding budget. Skipping.`);
            return;
          }

          const budget = budgetsById.get(firebaseBudgetId)!;
          const transactionId = txPreview.transactionid || uuidv4();

          let categories: { category: string; amount: number }[];
          try {
            categories = JSON.parse(txPreview.categories);
          } catch (e) {
            console.error(`Failed to parse categories for transaction ${transactionId}:`, e);
            previewErrors.value.push(`Transaction ${transactionId}: Invalid categories format`);
            return;
          }

          const transactionData: Transaction = {
            id: transactionId,
            userId: user.uid,
            familyId: familyId.value || "",
            budgetMonth: budget.month,
            budgetId: firebaseBudgetId,
            date: txPreview.transactiondate,
            merchant: txPreview.merchant || "",
            categories: categories,
            amount: txPreview.amount,
            notes: txPreview.notes || "",
            recurring: txPreview.recurring === "true" || txPreview.recurring === "1",
            recurringInterval: txPreview.recurringinterval || "Monthly",
            isIncome: txPreview.isincome === "true" || txPreview.isincome === "1",
            accountNumber: "",
            accountSource: "",
            postedDate: "",
            status: "U",
            entityId: selectedEntityId.value,
            taxMetadata: [],
          };

          budget.transactions.push(transactionData);
        });

        budgetsById.forEach((budget) => {
          const merchantCounts = budget.transactions
            .filter((t) => t.merchant && t.merchant.trim() !== "")
            .reduce((acc, t) => {
              acc[t.merchant] = (acc[t.merchant] || 0) + 1;
              return acc;
            }, {} as Record<string, number>);

          budget.merchants = Object.entries(merchantCounts)
            .map(([name, usageCount]) => ({ name, usageCount }))
            .sort((a, b) => b.usageCount - a.usageCount);
        });
      }

      const existingBudgets = await dataAccess.loadAccessibleBudgets(user.uid);
      const existingBudgetIds = new Set(existingBudgets.map((b) => b.budgetId));
      const newBudgetIds = new Set(budgetsById.keys());
      overwriteMonths.value = Array.from(newBudgetIds)
        .filter((id) => existingBudgetIds.has(id))
        .map((id) => budgetsById.get(id)!.month);

      if (overwriteMonths.value.length > 0) {
        pendingImportData.value = { budgetsById, budgetIdMap, entitiesById: new Map() };
        showOverwriteDialog.value = true;
      } else {
        pendingImportData.value = { budgetsById, budgetIdMap, entitiesById: new Map() };
        await proceedWithImport();
      }
    } catch (e: any) {
      console.error("Error during import:", e);
      showSnackbar(`Error during import: ${e.message}`, "negative");
    } finally {
      importRunning.value = false;
    }
  } else if (importType.value === "bankTransactions") {
    try {
      $q.loading.show({
        message: 'Importing transactions...',
        spinner: QSpinner,
        spinnerColor: 'primary',
        spinnerSize: 50,
        customClass: 'q-ml-sm flex items-center justify-center',
      });
      const accountId = selectedAccountId.value;
      if (!accountId) {
        showSnackbar("No account selected for import", "negative");
        return;
      }

      const selectedAccount = availableAccounts.value.find((account) => account.id === accountId);
      if (!selectedAccount) {
        showSnackbar("Selected account not found", "negative");
        return;
      }

      const existingDocs = await dataAccess.getImportedTransactionDocs();

      const newTransactions: ImportedTransaction[] = [];
      const duplicates: ImportedTransaction[] = [];

      previewBankTransactions.value.forEach((tx, index) => {
        const key = {
          accountNumber: selectedAccount.accountNumber || "",
          postedDate: tx.postedDate,
          payee: tx.payee,
          debitAmount: parseFloat(tx.debitAmount) || 0,
          creditAmount: parseFloat(tx.creditAmount) || 0,
        };

        const isDuplicate = dataAccess.findImportedTransactionByKey(existingDocs, key);
        const transaction: ImportedTransaction = {
          id: `${uuidv4()}-${index}`,
          accountId: accountId,
          accountNumber: selectedAccount.accountNumber || "",
          accountSource: selectedAccount.institution || "",
          payee: tx.payee,
          postedDate: tx.postedDate,
          status: tx.status,
          creditAmount: parseFloat(tx.creditAmount) || 0,
          debitAmount: parseFloat(tx.debitAmount) || 0,
          checkNumber: tx.checkNumber || "",
          matched: false,
          ignored: false,
          entityId: selectedEntityId.value,
        };

        if (isDuplicate) {
          duplicates.push(transaction);
        } else {
          newTransactions.push(transaction);
        }
      });

      if (newTransactions.length === 0) {
        showSnackbar("All transactions are duplicates and were skipped", "warning");
        showPreview.value = false;
        return;
      }

      const chunkSize = 400;
      const transactionChunks: ImportedTransaction[][] = [];
      for (let i = 0; i < newTransactions.length; i += chunkSize) {
        transactionChunks.push(newTransactions.slice(i, i + chunkSize));
      }

      let totalImported = 0;
      let totalBalanceChange = 0;
      const savedDocIds: string[] = [];

      for (let chunkIndex = 0; chunkIndex < transactionChunks.length; chunkIndex++) {
        const chunk = transactionChunks[chunkIndex];
        const newDocId = uuidv4();

        const chunkTransactions = chunk.map((tx, index) => ({
          ...tx,
          id: `${newDocId}-${index}`,
        }));

        const importedDoc: ImportedTransactionDoc = {
          id: newDocId,
          userId: user.uid,
          familyId: familyId.value || "",
          importedTransactions: chunkTransactions,
          createdAt: Timestamp.fromDate(new Date()),
        };

        const savedDocId = await dataAccess.saveImportedTransactions(importedDoc);
        savedDocIds.push(savedDocId);

        const chunkBalanceChange = chunkTransactions.reduce((total, tx) => {
          let change = 0;
          if (selectedAccount.type === "Bank") {
            change = (tx.creditAmount || 0) - (tx.debitAmount || 0);
          } else if (selectedAccount.type === "CreditCard") {
            change = (tx.creditAmount || 0) - (tx.debitAmount || 0);
          }
          return total + change;
        }, 0);

        totalImported += chunkTransactions.length;
        totalBalanceChange += chunkBalanceChange;
      }

      if (familyId.value) {
        const accountRef = await dataAccess.getAccount(familyId.value, accountId);
        if (!accountRef) {
          showSnackbar("Failed to fetch account for balance update", "negative");
        } else {
          const currentBalance = accountRef.balance || 0;
          const newBalance = currentBalance + totalBalanceChange;

          const updatedAccount: Account = {
            ...accountRef,
            balance: newBalance,
            updatedAt: Timestamp.fromDate(new Date()),
          };

          await dataAccess.saveAccount(familyId.value, updatedAccount);

          const accountIndex = availableAccounts.value.findIndex((acc) => acc.id === accountId);
          if (accountIndex !== -1) {
            availableAccounts.value[accountIndex] = { ...updatedAccount } as Account;
          }

          showSnackbar(
            `Imported ${totalImported} new bank/card transactions across ${savedDocIds.length} document(s) (IDs: ${savedDocIds.join(", ")}). Skipped ${
              duplicates.length
            } duplicates. Account balance updated to ${newBalance.toFixed(2)}.`,
            "success"
          );
        }
      }

      showPreview.value = false;
      previewBankTransactions.value = [];
      fieldMapping.value = {};
    } catch (error: any) {
      console.error("Error importing bank transactions:", error);
      showSnackbar(`Failed to import bank transactions: ${error.message}`, "negative");
    } finally {
      $q.loading.hide();
      importRunning.value = false;
      selectedAccountId.value = "";
      selectedEntityId.value = "";
    }
  } else if (importType.value === "accountsAndSnapshots") {
    try {
      $q.loading.show({
        message: 'Importing accounts...',
        spinner: QSpinner,
        spinnerColor: 'primary',
        spinnerSize: 50,
        customClass: 'q-ml-sm flex items-center justify-center',
      });
      const entries = pendingImportData.value?.accountsAndSnapshots || [];
      if (entries.length === 0) {
        showSnackbar("No accounts/snapshots data to import", "negative");
        return;
      }

      await dataAccess.importAccounts(familyId.value!, entries);
      await loadAllData();
      showSnackbar("Accounts and snapshots imported successfully", "success");
      showPreview.value = false;
      previewData.value.accountsAndSnapshots = [];
    } catch (error: any) {
      console.error("Error importing accounts and snapshots:", error);
      showSnackbar(`Failed to import accounts and snapshots: ${error.message}`, "negative");
    } finally {
      $q.loading.hide();
      pendingImportData.value = null;
    }
  }
}


async function proceedWithImport() {
  showPreview.value = false;
  showOverwriteDialog.value = false;
  $q.loading.show({
    message: 'Importing data...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });

  try {
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

    const budgetsById: Map<string, Budget> = pendingImportData.value?.budgetsById ?? new Map();
    pendingImportData.value = null;

    for (const [budgetId, budget] of budgetsById) {
      // Delete any existing budget to avoid merging old data
      try {
        await dataAccess.deleteBudget(budgetId);
      } catch {
        // Ignore if budget does not exist
      }

      // Recalculate merchants from imported transactions
      const merchantCounts = budget.transactions
        .filter((t) => t.merchant && t.merchant.trim() !== "")
        .reduce((acc, t) => {
          acc[t.merchant] = (acc[t.merchant] || 0) + 1;
          return acc;
        }, {} as Record<string, number>);
      budget.merchants = Object.entries(merchantCounts)
        .map(([name, usageCount]) => ({ name, usageCount }))
        .sort((a, b) => b.usageCount - a.usageCount);

      await dataAccess.saveBudget(budgetId, budget);
      budgetStore.updateBudget(budgetId, budget);
      showSnackbar(`Saved budget ${budget.month} with ${budget.transactions.length} transactions`);
    }

    const budgetMonths = Array.from(budgetsById.values()).map((budget) => budget.month);
    const mostRecentMonth = budgetMonths.sort((a, b) => {
      const dateA = new Date(a);
      const dateB = new Date(b);
      return dateB.getTime() - dateA.getTime();
    })[0];

    showSnackbar("Data imported successfully!");
    await loadAllData();

    if (mostRecentMonth) {
      await router.push({ path: "/", query: { month: mostRecentMonth } });
    } else {
      await router.push({ path: "/" });
    }
  } catch (error: any) {
    console.error("Error confirming import:", error);
    showSnackbar(`Failed to import data: ${error.message}`, "negative");
  } finally {
    $q.loading.hide();
    importRunning.value = false;
    previewData.value = { entities: [], categories: [], transactions: [], accountsAndSnapshots: [] };
    previewErrors.value = [];
    selectedEntityId.value = "";
  }
}

function formatCategories(categories: { category: string; amount: number }[]) {
  return JSON.stringify(categories);
}

async function exportDataToCSV() {
  $q.loading.show({
    message: 'Exporting data...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });
  try {
    const user = auth.currentUser;
    if (!user || !familyId.value) {
      showSnackbar("User not authenticated or no family selected", "negative");
      return;
    }

    const zip = new JSZip();

    const sanitizeFileName = (name: string) => {
      return name.replace(/[^a-zA-Z0-9_-]/g, "_").toLowerCase();
    };

    const entities = familyStore.family?.entities || [];
    for (const entity of entities) {
      const entityName = sanitizeFileName(entity.name);

      const entityBudgets = budgets.value.filter((budget) => budget.entityId === entity.id);
      const budgetCsv = Papa.unparse(
        entityBudgets.flatMap((budget) =>
          budget.categories.map((category) => ({
            budgetid: budget.budgetId ?? budget.month,
            budgetmonth: budget.month,
            category: category.name,
            group: category.group || "",
            isfund: category.isFund ? "true" : "false",
            target: category.target || 0,
            carryover: category.carryover || 0,
            entityId: budget.entityId || "",
            entityName: entity.name,
          }))
        )
      );
      if (entityBudgets.length > 0) {
        zip.file(`budgets_${entityName}.csv`, budgetCsv);
      }

      const entityTransactions = transactions.value.filter((t) => t.entityId === entity.id && !t.deleted);
      const transactionCsv = Papa.unparse(
        entityTransactions.map((transaction) => ({
          budgetid:
            budgets.value.find((b) => b.transactions.includes(transaction))?.budgetId ?? budgets.value.find((b) => b.transactions.includes(transaction))?.month,
          transactionid: transaction.id || "",
          transactiondate: transaction.date,
          categories: JSON.stringify(transaction.categories),
          merchant: transaction.merchant || "",
          isincome: transaction.isIncome ? "true" : "false",
          amount: transaction.amount,
          notes: transaction.notes || "",
          recurring: transaction.recurring ? "true" : "false",
          recurringinterval: transaction.recurringInterval || "Monthly",
          accountNumber: transaction.accountNumber || "",
          accountSource: transaction.accountSource || "",
          postedDate: transaction.postedDate || "",
          importedMerchant: transaction.importedMerchant || "",
          status: transaction.status || "U",
          checkNumber: transaction.checkNumber || "",
          entityId: transaction.entityId || "",
          entityName: entity.name,
        }))
      );
      if (entityTransactions.length > 0) {
        zip.file(`transactions_${entityName}.csv`, transactionCsv);
      }
    }

    const accounts = await dataAccess.getAccounts(familyId.value);
    const snapshots = await dataAccess.getSnapshots(familyId.value);
    const accountSnapshotRows = snapshots.flatMap((snapshot) =>
      snapshot.accounts.map((sa) => {
        const account = accounts.find((acc) => acc.id === sa.accountId);
        return {
          accountName: sa.accountName || account?.name || "",
          type: sa.type || account?.type || "",
          accountNumber: account?.accountNumber || "",
          institution: account?.institution || "",
          date: timestampToDate(snapshot.date).toISOString(),
          balance: sa.value || 0,
          interestRate: account?.details?.interestRate || "",
          appraisedValue: account?.details?.appraisedValue || "",
          address: account?.details?.address || "",
        };
      })
    );
    const accountsAndSnapshotsCsv = Papa.unparse(accountSnapshotRows);
    zip.file("accounts_and_snapshots.csv", accountsAndSnapshotsCsv);

    const importedTransactionDocsData = await dataAccess.getImportedTransactionDocs();
    importedTransactionDocsData.forEach((doc) => {
      const transactions = doc.importedTransactions.filter((tx) => !tx.deleted);
      if (transactions.length > 0) {
        const importedTransactionCsv = Papa.unparse(
          transactions.map((tx) => ({
            docId: doc.id,
            id: tx.id,
            accountId: tx.accountId,
            accountNumber: tx.accountNumber || "",
            accountSource: tx.accountSource || "",
            payee: tx.payee || "",
            postedDate: tx.postedDate || "",
            debitAmount: tx.debitAmount || 0,
            creditAmount: tx.creditAmount || 0,
            checkNumber: tx.checkNumber || "",
          // entityId may be tracked in UI; omit if undefined to match API
          ...(tx as any).entityId ? { entityId: (tx as any).entityId } : {},
        }))
      );
        zip.file(`imported_transactions_${doc.id}.csv`, importedTransactionCsv);
      }
    });

    const entityCsv = Papa.unparse(
      entities.map((entity) => ({
        id: entity.id,
        familyId: entity.familyId,
        name: entity.name,
        type: entity.type,
        ownerUid: entity.ownerUid || "",
        members: JSON.stringify(entity.members || []),
        createdAt: timestampToDate(entity.createdAt).toISOString(),
        updatedAt: timestampToDate(entity.updatedAt).toISOString(),
      }))
    );
    zip.file("entities.csv", entityCsv);

    const today = todayISO();
    const zipBlob = await zip.generateAsync({ type: "blob" });
    saveAs(zipBlob, `steady-rise-export-${today}.zip`);

    showSnackbar("Data exported successfully", "success");
  } catch (error: any) {
    console.error("Error exporting data:", error);
    showSnackbar(`Error exporting data: ${error.message}`, "negative");
  } finally {
    $q.loading.hide();
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
