<!-- src/views/DataView.vue -->
<template>
  <q-page fluid>
    <h1>Data Management</h1>

    <!-- Loading handled via $q.loading -->

    <!-- Tabs -->
    <q-tabs v-model="activeTab" color="primary" class="mb-4">
      <q-tab value="import">Import</q-tab>
      <q-tab value="export">Export</q-tab>
    </q-tabs>

    <q-tab-panels v-model="activeTab">
      <!-- Import Tab -->
      <q-tab-panel name="import">
        <div class="row">
          <div class="col col-12">
            <q-card>
              <q-card-section>Import Data</q-card-section>
              <q-card-section>
                <q-select v-model="importType" :items="importTypes" label="Select Import Type" variant="outlined" class="mb-4"></q-select>

                <!-- Entity Selection or Creation -->
                <div class="row" v-if="entityOptions.length > 0 && importType !== 'bankTransactions' && importType !== 'accountsAndSnapshots'">
                  <div class="col col-12 col-md-6">
                    <q-select
                      v-model="selectedEntityId"
                      :items="entityOptions"
                      item-title="name"
                      item-value="id"
                      label="Select Entity"
                      variant="outlined"
                      density="compact"
                      clearable
                      :rules="importType !== 'entities' ? [(v) => !!v || 'Entity selection is required'] : []"
                      class="mb-4"
                    ></q-select>
                  </div>
                  <div class="col col-12 col-md-6">
                    <q-btn color="primary" @click="openCreateEntityDialog" class="mt-2">Create New Entity</q-btn>
                  </div>
                </div>
                <div class="row" v-else-if="importType !== 'bankTransactions' && importType !== 'accountsAndSnapshots'">
                  <div class="col col-12">
                    <q-banner type="info" class="mb-4">
                      No entities found. Please create a new entity or import entities before importing budgets or transactions.
                    </q-banner>
                    <q-btn color="primary" @click="openCreateEntityDialog" class="mr-4">Create Entity</q-btn>
                    <q-btn color="secondary" @click="importType = 'entities'">Import Entities</q-btn>
                  </div>
                </div>

                <!-- File Input for Imports -->
                <div v-if="importType === 'bankTransactions'">
                  <!-- Bank transaction import UI -->
                  <div class="row">
                    <div class="col col-12 col-md-6">
                      <q-select
                        v-model="selectedAccountId"
                        :items="formattedAccounts"
                        item-title="title"
                        item-value="value"
                        label="Select Account for Transactions"
                        variant="outlined"
                        :rules="[(v) => !!v || 'Account selection is required']"
                        :disabled="importing || availableAccounts.length === 0"
                        class="mb-4"
                      ></q-select>
                      <q-banner v-if="availableAccounts.length === 0" type="warning" class="mb-4">
                        No bank or credit card accounts found. Please create an account in the Accounts section before importing transactions.
                      </q-banner>
                    </div>
                  </div>
                  <div class="row">
                    <div class="col col-12 col-md-6">
                      <q-file-input
                        label="Upload Bank/Card Transactions CSV"
                        accept=".csv"
                        @change="handleBankTransactionsFileUpload"
                        :disabled="importing || !selectedAccountId"
                      ></q-file-input>
                    </div>
                  </div>
                  <!-- Field Mapping -->
                  <div v-if="csvHeaders.length > 0">
                    <div class="row">
                      <div class="col col-12">
                        <h3>Map CSV Columns to Fields</h3>
                        <q-select
                          v-model="amountFormat"
                          :items="amountFormatOptions"
                          label="How are Credits/Debits Represented?"
                          variant="outlined"
                          class="mb-4"
                        ></q-select>

                        <!-- Common Fields -->
                        <div class="row" v-for="(field, index) in commonBankTransactionFields" :key="index">
                          <div class="col col-12 col-md-6">
                            <q-select
                              v-model="fieldMapping[field.key]"
                              :items="csvHeaders"
                              :label="field.label"
                              variant="outlined"
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
                                :items="csvHeaders"
                                label="Credit Amount"
                                variant="outlined"
                                clearable
                                placeholder="Select a column or type a value"
                              ></q-select>
                            </div>
                          </div>
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-select
                                v-model="fieldMapping.debitAmount"
                                :items="csvHeaders"
                                label="Debit Amount"
                                variant="outlined"
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
                                :items="csvHeaders"
                                label="Transaction Type Column"
                                variant="outlined"
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
                                variant="outlined"
                                placeholder="e.g., 'Credit' or 'CR'"
                              ></q-input>
                            </div>
                          </div>
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-input
                                v-model="debitTypeValue"
                                label="Value for Debit"
                                variant="outlined"
                                placeholder="e.g., 'Debit' or 'DR'"
                              ></q-input>
                            </div>
                          </div>
                          <div class="row">
                            <div class="col col-12 col-md-6">
                              <q-select
                                v-model="fieldMapping.amount"
                                :items="csvHeaders"
                                label="Amount"
                                variant="outlined"
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
                                :items="csvHeaders"
                                label="Amount (Positive = Credit, Negative = Debit)"
                                variant="outlined"
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
                <div v-else-if="importType === 'entities'">
                  <q-file-input label="Upload Entities CSV/JSON" accept=".csv,.json" @change="handleFileUpload" :disabled="importing"></q-file-input>
                </div>
                <div v-else-if="importType === 'accountsAndSnapshots'">
                  <q-file-input
                    label="Upload Accounts and Snapshots CSV"
                    accept=".csv"
                    @change="handleAccountsAndSnapshotsImport"
                    :disabled="importing"
                  ></q-file-input>
                </div>
                <div v-else>
                  <q-file-input
                    label="Upload Budget or Transactions CSV/JSON"
                    accept=".csv,.json"
                    multiple
                    @change="handleFileUpload"
                    :disabled="importing || (!selectedEntityId && importType !== 'entities')"
                  ></q-file-input>
                </div>

                <!-- Preview Dialog -->
                <q-dialog v-model="showPreview" max-width="1000px">
                  <q-card>
                    <q-card-section>Preview Data</q-card-section>
                    <q-card-section>
                      <q-tabs v-model="previewTab">
                        <q-tab value="entities" v-if="previewData.entities?.length > 0">Entities</q-tab>
                        <q-tab value="categories" v-if="previewData.categories?.length > 0">Categories</q-tab>
                        <q-tab value="transactions" v-if="previewData.transactions?.length > 0">Transactions</q-tab>
                        <q-tab value="bankTransactions" v-if="previewBankTransactions.length > 0">Bank Transactions</q-tab>
                        <q-tab value="accountsAndSnapshots" v-if="previewData.accountsAndSnapshots?.length > 0">Accounts/Snapshots</q-tab>
                      </q-tabs>
                      <q-tab-panels v-model="previewTab">
                        <q-tab-panel name="entities">
                          <q-table :headers="entityHeaders" :items="previewData.entities" :items-per-page="5" class="mt-4"></q-table>
                        </q-tab-panel>
                        <q-tab-panel name="categories">
                          <q-table :headers="categoryHeaders" :items="previewData.categories" :items-per-page="5" class="mt-4"></q-table>
                        </q-tab-panel>
                        <q-tab-panel name="transactions">
                          <q-table :headers="transactionHeaders" :items="previewData.transactions" :items-per-page="5" class="mt-4">
                            <template v-slot:item.categories="{ item }">
                              <span>{{ formatCategories(item.categories) }}</span>
                            </template>
                          </q-table>
                        </q-tab-panel>
                        <q-tab-panel name="bankTransactions">
                          <q-table
                            :headers="bankTransactionPreviewHeaders"
                            :items="previewBankTransactions"
                            :items-per-page="5"
                            class="mt-4"
                          ></q-table>
                        </q-tab-panel>
                        <q-tab-panel name="accountsAndSnapshots">
                          <q-table
                            :headers="accountsAndSnapshotsHeaders"
                            :items="previewData.accountsAndSnapshots"
                            :items-per-page="5"
                            class="mt-4"
                          ></q-table>
                        </q-tab-panel>
                      </q-tab-panels>
                      <q-banner v-if="previewErrors.length > 0" type="error" class="mt-4">
                        <ul>
                          <li v-for="(error, index) in previewErrors" :key="index">
                            {{ error }}
                          </li>
                        </ul>
                      </q-banner>
                    </q-card-section>
                    <q-card-actions>
                      <q-space></q-space>
                      <q-btn color="error" @click="showPreview = false">Cancel</q-btn>
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
                      <div class="px-8">
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
                      <q-btn color="error" @click="showOverwriteDialog = false">Cancel</q-btn>
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
            <q-card>
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
      <entity-form :initial-entity="null" @save="handleEntitySave" @cancel="showEntityForm = false" />
    </q-dialog>

  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useQuasar } from 'quasar';
import { auth } from "../firebase/init";
import { dataAccess } from "../dataAccess";
import Papa from "papaparse";
import { Budget, Transaction, Account, Entity, ImportedTransaction, ImportedTransactionDoc, Snapshot } from "../types";
import { useRouter } from "vue-router";
import { v4 as uuidv4 } from "uuid";
import { useBudgetStore } from "../store/budget";
import { useFamilyStore } from "../store/family";
import EntityForm from "../components/EntityForm.vue";
import { timestampToDate, toBudgetMonth, stringToFirestoreTimestamp, parseAmount, adjustTransactionDate, todayISO } from "../utils/helpers";
import JSZip from "jszip";
import { saveAs } from "file-saver";
import { Timestamp } from "firebase/firestore";

const familyStore = useFamilyStore();
const budgetStore = useBudgetStore();
const router = useRouter();
const $q = useQuasar();
const budgets = ref<Budget[]>([]);
const familyId = ref<string | null>(null);
const transactions = ref<Transaction[]>([]);
const loadingData = ref(false);
const exporting = ref(false);
const importing = ref(false);
const loading = computed(() => loadingData.value || importing.value || exporting.value);
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
  { title: "Entities", value: "entities" },
  { title: "Budget/Transactions", value: "budgetTransactions" },
  { title: "Bank/Card Transactions", value: "bankTransactions" },
  { title: "Accounts/Snapshots", value: "accountsAndSnapshots" },
];
const selectedFiles = ref<File[]>([]);

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
  { title: "Separate Credit/Debit Columns", value: "separate" },
  { title: "Transaction Type Column", value: "type" },
  { title: "Single Amount (Positive/Negative)", value: "single" },
];
const creditTypeValue = ref("Credit");
const debitTypeValue = ref("Debit");
const bankTransactionPreviewHeaders = [
  { title: "Payee", value: "payee" },
  { title: "Posted Date", value: "postedDate" },
  { title: "Status", value: "status" },
  { title: "Credit Amount", value: "creditAmount" },
  { title: "Debit Amount", value: "debitAmount" },
  { title: "Check Number", value: "checkNumber" },
];

// Headers for Entity Preview
const entityHeaders = [
  { title: "Name", value: "name" },
  { title: "Type", value: "type" },
  { title: "Owner Email", value: "ownerEmail" },
];
const showEntityForm = ref(false);

// Headers for Accounts/Snapshots Preview
const accountsAndSnapshotsHeaders = [
  { title: "Account Name", value: "accountName" },
  { title: "Type", value: "type" },
  { title: "Account Number", value: "accountNumber" },
  { title: "Institution", value: "institution" },
  { title: "Date", value: "date" },
  { title: "Balance", value: "balance" },
  { title: "Interest Rate", value: "interestRate" },
  { title: "Appraised Value", value: "appraisedValue" },
  { title: "Address", value: "address" },
];

// Headers for Budget and Transaction Previews
const categoryHeaders = [
  { title: "BudgetId", value: "budgetid" },
  { title: "Budget Month", value: "budgetmonth" },
  { title: "Category", value: "category" },
  { title: "Group", value: "group" },
  { title: "IsFund", value: "isfund" },
  { title: "Target", value: "target" },
  { title: "Carryover", value: "carryover" },
  { title: "EntityId", value: "entityId" },
  { title: "EntityName", value: "entityName" },
];
const transactionHeaders = [
  { title: "BudgetId", value: "budgetid" },
  { title: "TransactionId", value: "transactionid" },
  { title: "Transaction Date", value: "transactiondate" },
  { title: "Categories", value: "categories" },
  { title: "Merchant", value: "merchant" },
  { title: "IsIncome", value: "isincome" },
  { title: "Amount", value: "amount" },
  { title: "Notes", value: "notes" },
  { title: "Recurring", value: "recurring" },
  { title: "RecurringInterval", value: "recurringinterval" },
  { title: "EntityId", value: "entityId" },
  { title: "EntityName", value: "entityName" },
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
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    const user = auth.currentUser;
    if (!user) return;

    await familyStore.loadFamily(user.uid);

    // Set selectedEntityId to the first entity if available
    const entities = familyStore.family?.entities || [];
    if (entities.length > 0 && !selectedEntityId.value) {
      selectedEntityId.value = entities[0].id;
    }

    // Load budgets directly from loadAccessibleBudgets
    budgets.value = await dataAccess.loadAccessibleBudgets(user.uid);

    transactions.value = budgets.value.flatMap((budget) => budget.transactions || []);

    if (budgets.value && budgets.value.length > 0) {
      familyId.value = budgets.value[0].familyId;
    } else {
      const family = await familyStore.getFamily();
      if (!family) throw new Error("User has no family assigned");
      familyId.value = family.id;
    }

    if (familyId.value) {
      const accounts = await dataAccess.getAccounts(familyId.value);
      availableAccounts.value = accounts.filter((account) => account.type === "Bank" || account.type === "CreditCard");
    }
  } catch (error: any) {
    console.error("Error loading data:", error);
    showSnackbar(`Error loading data: ${error.message}`, "error");
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
    showSnackbar("Cannot create entity: invalid user or family data", "error");
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
    showSnackbar(`Error creating entity: ${error.message}`, "error");
  }
}

async function handleFileUpload(event: Event) {
  const input = event.target as HTMLInputElement;
  selectedFiles.value = input.files ? Array.from(input.files) : [];

  if (!selectedFiles.value.length) return;

  $q.loading.show({
    message: 'Importing data...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
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
            type: entity.type,
            ownerUid: auth.currentUser?.uid || "",
            members: entity.members || [{ uid: auth.currentUser?.uid || "", email: auth.currentUser?.email || "", role: "Admin" }],
            createdAt: Timestamp.fromDate(new Date()),
            updatedAt: Timestamp.fromDate(new Date()),
            email: auth.currentUser?.email || "",
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
          type: row.type,
          ownerUid: auth.currentUser?.uid || "",
          members: row.members ? JSON.parse(row.members) : [{ uid: auth.currentUser?.uid || "", email: auth.currentUser?.email || "", role: "Admin" }],
          createdAt: Timestamp.fromDate(new Date()),
          updatedAt: Timestamp.fromDate(new Date()),
          email: auth.currentUser?.email || "",
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
        const budgetId = `${user.uid}_${selectedEntityId.value}_${jsonData.month}`;
        budgetsById.set(budgetId, {
          budgetId: budgetId,
          budgetMonth: jsonData.month,
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
      const originalBudgetId = row.budgetid;
      if (!row.budgetmonth) {
        previewErrors.value.push(`File ${budgetCSVFile}, Row ${index + 1}: budgetMonth is required`);
        return;
      }
      const month = row.budgetmonth.length > 7 ? row.budgetmonth.slice(0, 7) : row.budgetmonth;
      const budgetId = `${user.uid}_${selectedEntityId.value}_${month}`;
      if (!budgetsById.has(budgetId)) {
        budgetsById.set(budgetId, {
          budgetId: budgetId,
          budgetMonth: month,
          incomeTarget: parseFloat(row.incometarget) || 0,
          categories: [],
          transactions: [],
          familyId: familyId.value || "",
          label: `Imported Budget ${month}`,
          entityId: selectedEntityId.value,
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
        isfund: row.isfund === "true" || row.isfund === "1",
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
        } catch (e) {
          previewErrors.value.push(`File ${transactionCSVFile}, Row ${index + 1}: Invalid transaction date: ${row.transactiondate}`);
          return;
        }
      }
      if (!month) {
        previewErrors.value.push(`File ${transactionCSVFile}, Row ${index + 1}: budgetMonth is required and could not be derived`);
        return;
      }

      const originalBudgetId = row.budgetid;
      const budgetId = `${user.uid}_${selectedEntityId.value}_${month}`;
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
          incomeTarget: 0,
          categories: [],
          transactions: [],
          familyId: familyId.value || "",
          label: `Imported Budget ${month}`,
          entityId: selectedEntityId.value,
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
      category: category.name,
      group: category.group || "",
      isfund: category.isfund,
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

async function handleAccountsAndSnapshotsImport(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files ? input.files[0] : null;

  if (!file) return;

  $q.loading.show({
    message: 'Importing data...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
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
        } catch (e) {
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

async function handleBankTransactionsFileUpload(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files ? input.files[0] : null;

  if (!file) return;

  $q.loading.show({
    message: 'Importing bank transactions...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
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

async function previewBankTransactionsData() {
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
    showSnackbar("User not authenticated", "error");
    return;
  }
  if (!familyId.value) {
    showSnackbar("Cannot import without a Family/Org", "error");
    return;
  }

  if (importType.value === "entities") {
    try {
      $q.loading.show({
        message: 'Importing entities...',
        spinner: 'QSpinner',
        spinnerColor: 'primary',
        spinnerSize: '50px',
        messageClass: 'q-ml-sm',
        boxClass: 'flex items-center justify-center',
      });
      const entitiesById = pendingImportData.value?.entitiesById || new Map();
      for (const [entityId, entity] of entitiesById) {
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
      showSnackbar(`Failed to import entities: ${error.message}`, "error");
    } finally {
      $q.loading.hide();
      pendingImportData.value = null;
    }
  } else if (importType.value === "budgetTransactions") {
    const budgetsById = new Map<string, Budget>();
    const budgetIdMap = new Map<string, string>();

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
          }
        });

        for (const [originalBudgetId, month] of budgetIdToMonth) {
          const firebaseBudgetId = `${user.uid}_${selectedEntityId.value}_${month}`; // New ID format
          budgetIdMap.set(originalBudgetId, firebaseBudgetId);
          // Create or update the budget
          const budget = await createBudgetForMonth(month, familyId.value!, user.uid, selectedEntityId.value);
          budgetsById.set(firebaseBudgetId, budget);
        }

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
        await proceedWithImport(budgetsById, budgetIdMap);
      }
    } catch (e: any) {
      console.error("Error during import:", e);
      showSnackbar(`Error during import: ${e.message}`, "error");
    } finally {
      importRunning.value = false;
    }
  } else if (importType.value === "bankTransactions") {
    try {
      $q.loading.show({
        message: 'Importing transactions...',
        spinner: 'QSpinner',
        spinnerColor: 'primary',
        spinnerSize: '50px',
        messageClass: 'q-ml-sm',
        boxClass: 'flex items-center justify-center',
      });
      const accountId = selectedAccountId.value;
      if (!accountId) {
        showSnackbar("No account selected for import", "error");
        return;
      }

      const selectedAccount = availableAccounts.value.find((account) => account.id === accountId);
      if (!selectedAccount) {
        showSnackbar("Selected account not found", "error");
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
          showSnackbar("Failed to fetch account for balance update", "error");
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
      showSnackbar(`Failed to import bank transactions: ${error.message}`, "error");
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
        spinner: 'QSpinner',
        spinnerColor: 'primary',
        spinnerSize: '50px',
        messageClass: 'q-ml-sm',
        boxClass: 'flex items-center justify-center',
      });
      const entries = pendingImportData.value?.accountsAndSnapshots || [];
      if (entries.length === 0) {
        showSnackbar("No accounts/snapshots data to import", "error");
        return;
      }

      await dataAccess.importAccounts(familyId.value!, entries);
      await loadAllData();
      showSnackbar("Accounts and snapshots imported successfully", "success");
      showPreview.value = false;
      previewData.value.accountsAndSnapshots = [];
    } catch (error: any) {
      console.error("Error importing accounts and snapshots:", error);
      showSnackbar(`Failed to import accounts and snapshots: ${error.message}`, "error");
    } finally {
      $q.loading.hide();
      pendingImportData.value = null;
    }
  }
}

async function createBudgetForMonth(month: string, familyId: string, ownerUid: string, entityId: string): Promise<Budget> {
  const budgetId = `${ownerUid}_${entityId}_${month}`;
  const existingBudget = await dataAccess.getBudget(budgetId);
  if (existingBudget) {
    return existingBudget;
  }

  const availableBudgets = budgets.value.sort((a, b) => a.month.localeCompare(b.month));
  let sourceBudget: Budget | undefined;

  const previousBudgets = availableBudgets.filter((b) => b.month < month && b.entityId === entityId);
  if (previousBudgets.length > 0) {
    sourceBudget = previousBudgets[previousBudgets.length - 1];
  } else {
    const futureBudgets = availableBudgets.filter((b) => b.month > month && b.entityId === entityId);
    if (futureBudgets.length > 0) {
      sourceBudget = futureBudgets[0];
    }
  }

  if (!sourceBudget) {
    const defaultBudget: Budget = {
      familyId: familyId,
      entityId: entityId,
      month: month,
      incomeTarget: 0,
      categories: [],
      transactions: [],
      label: `Default Budget for ${month}`,
      merchants: [],
      budgetId: budgetId,
    };
    await dataAccess.saveBudget(budgetId, defaultBudget);
    budgetStore.updateBudget(budgetId, defaultBudget);
    budgets.value.push(defaultBudget);
    return defaultBudget;
  }

  const [newYear, newMonthNum] = month.split("-").map(Number);
  const [sourceYear, sourceMonthNum] = sourceBudget.month.split("-").map(Number);
  const isFutureMonth = newYear > sourceYear || (newYear === sourceYear && newMonthNum > sourceMonthNum);

  let newCarryover: Record<string, number> = {};
  if (isFutureMonth) {
    newCarryover = await dataAccess.calculateCarryOver(sourceBudget);
  }

  const newBudget: Budget = {
    familyId: familyId,
    entityId: entityId,
    month: month,
    incomeTarget: sourceBudget.incomeTarget,
    categories: sourceBudget.categories.map((cat) => ({
      ...cat,
      carryover: cat.isFund ? newCarryover[cat.name] || 0 : 0,
    })),
    label: "",
    merchants: sourceBudget.merchants || [],
    transactions: [],
    budgetId: budgetId,
  };

  const recurringTransactions: Transaction[] = [];
  if (sourceBudget.transactions) {
    const recurringGroups = sourceBudget.transactions.reduce((groups, trx) => {
      if (!trx.deleted && trx.recurring) {
        const key = `${trx.merchant}-${trx.amount}-${trx.recurringInterval}-${trx.userId}-${trx.isIncome}`;
        if (!groups[key]) {
          groups[key] = [];
        }
        groups[key].push(trx);
      }
      return groups;
    }, {} as Record<string, Transaction[]>);

    Object.values(recurringGroups).forEach((group) => {
      const firstInstance = group.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())[0];
      if (firstInstance.recurringInterval === "Monthly") {
        const newDate = adjustTransactionDate(firstInstance.date, month, "Monthly");
        recurringTransactions.push({
          ...firstInstance,
          id: uuidv4(),
          date: newDate,
          budgetMonth: month,
          entityId: entityId,
        });
      }
    });
  }

  newBudget.transactions = recurringTransactions;
  await dataAccess.saveBudget(budgetId, newBudget);
  budgetStore.updateBudget(budgetId, newBudget);
  budgets.value.push(newBudget);
  return newBudget;
}

async function proceedWithImport(budgetsById: Map<string, Budget> = new Map(), budgetIdMap: Map<string, string> = new Map()) {
  showPreview.value = false;
  showOverwriteDialog.value = false;
  $q.loading.show({
    message: 'Importing data...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });

  try {
    const user = auth.currentUser;
    if (!user) throw new Error("User not authenticated");

    if (pendingImportData.value) {
      budgetsById = pendingImportData.value.budgetsById;
      budgetIdMap = pendingImportData.value.budgetIdMap;
      pendingImportData.value = null;
    }

    for (const [budgetId, budget] of budgetsById) {
      await dataAccess.saveBudget(budgetId, budget);
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
    showSnackbar(`Failed to import data: ${error.message}`, "error");
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
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    const user = auth.currentUser;
    if (!user || !familyId.value) {
      showSnackbar("User not authenticated or no family selected", "error");
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
            entityId: tx.entityId || "",
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
    showSnackbar(`Error exporting data: ${error.message}`, "error");
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
