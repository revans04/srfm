<!-- src/views/DataView.vue -->
<template>
  <q-page class="bg-grey-1 q-pa-lg">
    <h1 class="page-title q-mb-md">Data Management</h1>

    <!-- Loading handled via $q.loading -->

    <!-- Tabs -->
    <q-tabs v-model="activeTab" color="primary" class="bg-white q-mb-md rounded-sm">
      <q-tab name="import" label="Import" />
      <q-tab name="export" label="Export" />
    </q-tabs>

    <q-tab-panels v-model="activeTab">
      <!-- Import Tab -->
      <q-tab-panel name="import">
        <div class="row">
          <div class="col col-12">
            <q-card class="bg-white q-pa-md" rounded>
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
                    <q-banner class="bg-info text-white q-mb-lg">
                      No entities found. Please create a new entity or import entities before importing budgets or transactions.
                    </q-banner>
                    <q-btn color="primary" @click="openCreateEntityDialog" class="q-mr-lg">Create Entity</q-btn>
                    <q-btn color="secondary" @click="importType = 'entities'">Import Entities</q-btn>
                  </div>
                </div>

                <!-- File Input for Imports -->
                <q-banner v-if="importError" class="bg-negative text-white q-mb-lg">
                  {{ importError }}
                </q-banner>
                <q-banner v-if="importSuccess" class="bg-positive text-white q-mb-lg">
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
                      <q-banner v-if="dataLoaded && availableAccounts.length === 0" class="bg-warning text-white q-mb-lg">
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
                <div v-else-if="importType === 'everyDollarCsv'">
                  <div class="row q-col-gutter-md">
                    <div class="col-12 col-md-6">
                      <q-file
                        v-model="everyDollarBudgetCsvFile"
                        label="Upload EveryDollar Budgets CSV"
                        accept=".csv"
                        :disabled="importing"
                      ></q-file>
                    </div>
                    <div class="col-12 col-md-6">
                      <q-file
                        v-model="everyDollarTransactionsCsvFile"
                        label="Upload EveryDollar Transactions CSV"
                        accept=".csv"
                        :disabled="importing"
                      ></q-file>
                    </div>
                  </div>
                  <div class="row q-mt-md">
                    <div class="col">
                      <q-btn
                        color="primary"
                        label="Import EveryDollar CSVs"
                        @click="importEveryDollarCsv"
                        :disabled="
                          importing ||
                          !everyDollarBudgetCsvFile ||
                          !everyDollarTransactionsCsvFile ||
                          !selectedEntityId
                        "
                      />
                    </div>
                  </div>
                  <div v-if="everyDollarDiffs.length > 0" class="row q-mt-md">
                    <div class="col col-12">
                      <q-card class="bg-white q-pa-md" rounded>
                        <q-card-section>
                          <div class="text-h6">Review differences</div>
                          <div class="text-caption q-mb-sm">
                            This import is additive only. Nothing on existing budgets will be overwritten or deleted — check the items you want to add.
                          </div>
                          <div class="row q-gutter-md items-center q-mt-sm">
                            <div class="col-auto">
                              <q-input
                                v-model.number="everyDollarDuplicateWindowDays"
                                type="number"
                                label="Duplicate date window (days)"
                                dense
                                outlined
                                :min="0"
                                :disable="importing"
                                style="max-width: 220px"
                                @update:model-value="recomputeEveryDollarDiffDuplicates"
                              />
                            </div>
                          </div>
                        </q-card-section>

                        <q-card-section class="q-pt-none">
                          <q-expansion-item
                            v-for="diff in everyDollarDiffs"
                            :key="diff.month"
                            :label="diff.month"
                            :caption="diffSummaryCaption(diff)"
                            :default-opened="true"
                            header-class="text-weight-medium"
                            class="q-mb-sm"
                            style="border: 1px solid #e0e0e0; border-radius: 8px;"
                          >
                            <div class="q-pa-md">
                              <!-- Budget status banner -->
                              <q-banner
                                v-if="diff.existingBudgetId === null"
                                class="bg-info text-white q-mb-md"
                                dense
                              >
                                No budget exists for {{ diff.month }} yet.
                                <template #action>
                                  <q-checkbox
                                    v-if="everyDollarDiffSelection[diff.month]"
                                    v-model="everyDollarDiffSelection[diff.month].createBudget"
                                    label="Create budget from CSV"
                                    dense
                                  />
                                </template>
                              </q-banner>
                              <q-banner v-else class="q-mb-md bg-grey-2" dense>
                                Budget exists — existing categories, targets and carryover will be left alone.
                              </q-banner>

                              <!-- Budget-level informational diffs -->
                              <div v-if="diff.labelDiff || diff.incomeTargetDiff" class="q-mb-md">
                                <div class="text-subtitle2 q-mb-xs">Budget-level differences (informational only)</div>
                                <div v-if="diff.labelDiff" class="text-caption q-ml-sm">
                                  Label: existing <code>{{ diff.labelDiff.existing }}</code> · imported <code>{{ diff.labelDiff.imported }}</code>
                                </div>
                                <div v-if="diff.incomeTargetDiff" class="text-caption q-ml-sm">
                                  Income target: existing <code>{{ formatCurrency(diff.incomeTargetDiff.existing) }}</code> · imported <code>{{ formatCurrency(diff.incomeTargetDiff.imported) }}</code>
                                </div>
                              </div>

                              <!-- New categories -->
                              <div v-if="diff.newCategories.length > 0" class="q-mb-md">
                                <div class="text-subtitle2 q-mb-xs">
                                  New categories ({{ diff.newCategories.length }})
                                </div>
                                <div
                                  v-for="cat in diff.newCategories"
                                  :key="cat.key"
                                  class="row items-center q-mb-xs"
                                >
                                  <q-checkbox
                                    v-if="everyDollarDiffSelection[diff.month]"
                                    dense
                                    v-model="everyDollarDiffSelection[diff.month].addCategoryKeys[cat.key]"
                                  />
                                  <div class="q-ml-sm">
                                    <span class="text-weight-medium">{{ cat.category.name }}</span>
                                    <span class="text-caption text-grey-7 q-ml-sm">
                                      {{ cat.category.groupName || 'Uncategorized' }} ·
                                      target {{ formatCurrency(cat.category.target || 0) }}
                                      <span v-if="cat.category.isFund"> · fund</span>
                                      <span v-if="cat.category.carryover">
                                        · carryover {{ formatCurrency(cat.category.carryover) }}
                                      </span>
                                    </span>
                                  </div>
                                </div>
                              </div>

                              <!-- Changed categories (informational only) -->
                              <div v-if="diff.changedCategories.length > 0" class="q-mb-md">
                                <div class="text-subtitle2 q-mb-xs">
                                  Categories with differences ({{ diff.changedCategories.length }}) — informational only
                                </div>
                                <div class="text-caption text-grey-7 q-mb-xs">
                                  These categories already exist on the budget. Existing values will NOT be overwritten.
                                </div>
                                <div
                                  v-for="row in diff.changedCategories"
                                  :key="row.name"
                                  class="q-mb-xs q-pa-sm"
                                  style="border-left: 2px solid #fdd835; background: #fffde7;"
                                >
                                  <div class="text-weight-medium">{{ row.name }}</div>
                                  <div
                                    v-for="fd in row.differences"
                                    :key="fd.field"
                                    class="text-caption q-ml-sm"
                                  >
                                    {{ fd.field }}: existing
                                    <code>{{ formatDiffValue(fd.field, fd.existing) }}</code>
                                    · imported
                                    <code>{{ formatDiffValue(fd.field, fd.imported) }}</code>
                                  </div>
                                </div>
                              </div>

                              <!-- New transactions -->
                              <div class="q-mb-md">
                                <div class="text-subtitle2 q-mb-xs">
                                  New transactions ({{ diff.newTransactions.length }})
                                </div>
                                <div v-if="diff.newTransactions.length === 0" class="text-caption text-grey-7">
                                  No new transactions to import for this month.
                                </div>
                                <q-markup-table v-else dense flat bordered>
                                  <thead>
                                    <tr>
                                      <th class="text-left" style="width: 40px"></th>
                                      <th class="text-left">Date</th>
                                      <th class="text-left">Merchant</th>
                                      <th class="text-left">Categories</th>
                                      <th class="text-right">Amount</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    <tr v-for="row in diff.newTransactions" :key="row.key">
                                      <td>
                                        <q-checkbox
                                          v-if="everyDollarDiffSelection[diff.month]"
                                          dense
                                          v-model="everyDollarDiffSelection[diff.month].addTxKeys[row.key]"
                                        />
                                      </td>
                                      <td>{{ formatDate(row.transaction.date) }}</td>
                                      <td>{{ row.transaction.merchant }}</td>
                                      <td class="text-caption">{{ formatCategories(row.transaction.categories) }}</td>
                                      <td class="text-right">{{ formatCurrency(row.transaction.amount) }}</td>
                                    </tr>
                                  </tbody>
                                </q-markup-table>
                              </div>

                              <!-- Duplicate transactions (collapsible) -->
                              <div v-if="diff.duplicateTransactions.length > 0">
                                <q-btn
                                  flat
                                  dense
                                  size="sm"
                                  :label="`${everyDollarDiffSelection[diff.month]?.showDuplicates ? 'Hide' : 'Show'} duplicates (${diff.duplicateTransactions.length})`"
                                  icon="visibility"
                                  @click="toggleShowDuplicates(diff.month)"
                                />
                                <div v-if="everyDollarDiffSelection[diff.month]?.showDuplicates" class="q-mt-sm">
                                  <div class="text-caption text-grey-7 q-mb-xs">
                                    These CSV rows appear to match an existing transaction (date + amount + similar merchant). Check any you want to import anyway.
                                  </div>
                                  <q-markup-table dense flat bordered>
                                    <thead>
                                      <tr>
                                        <th class="text-left" style="width: 40px"></th>
                                        <th class="text-left">Date</th>
                                        <th class="text-left">Merchant</th>
                                        <th class="text-right">Amount</th>
                                        <th class="text-left">Matched existing</th>
                                      </tr>
                                    </thead>
                                    <tbody>
                                      <tr v-for="row in diff.duplicateTransactions" :key="row.key">
                                        <td>
                                          <q-checkbox
                                            v-if="everyDollarDiffSelection[diff.month]"
                                            dense
                                            v-model="everyDollarDiffSelection[diff.month].addTxKeys[row.key]"
                                          />
                                        </td>
                                        <td>{{ formatDate(row.transaction.date) }}</td>
                                        <td>{{ row.transaction.merchant }}</td>
                                        <td class="text-right">{{ formatCurrency(row.transaction.amount) }}</td>
                                        <td class="text-caption text-grey-8">{{ row.matchedExistingSummary }}</td>
                                      </tr>
                                    </tbody>
                                  </q-markup-table>
                                </div>
                              </div>
                            </div>
                          </q-expansion-item>
                        </q-card-section>

                        <q-card-actions align="right">
                          <q-btn
                            flat
                            label="Cancel"
                            @click="resetEveryDollarDiffState"
                            :disable="importing"
                          />
                          <q-btn
                            color="primary"
                            label="Apply selected changes"
                            @click="applyEveryDollarDiff"
                            :disable="importing || !hasAnyEveryDollarSelections"
                          />
                        </q-card-actions>
                      </q-card>
                    </div>
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
                      <q-banner v-if="previewErrors.length > 0" class="bg-negative text-white q-mt-lg">
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
                      Confirming will delete the existing budgets for these months before importing the new data. Do you want to
                      continue?
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
            <q-card class="bg-white q-pa-md" rounded>
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
import { ref, onMounted, computed, watch, reactive } from "vue";
import { useQuasar, QSpinner } from 'quasar';
import { auth } from "../firebase/init";
import { dataAccess } from "../dataAccess";
import Papa from "papaparse";
import type {
  Budget,
  Transaction,
  Account,
  Entity,
  ImportedTransaction,
  ImportedTransactionDoc,
  BudgetCategory,
  BudgetInfo,
} from "../types";
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
  formatDate,
} from "../utils/helpers";
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
const exporting = ref(false);
const importing = ref(false);
const activeTab = ref<string>("import");
const availableAccounts = ref<Account[]>([]);
const dataLoaded = ref(false);
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
  existingBudgetIds?: Set<string>;
  selectedBudgetIds?: Set<string>;
  everyDollarMonthToExistingId?: Map<string, string>;
} | null>(null);
const previewTab = ref("categories");
const importType = ref("bankTransactions");
const importTypes = [
  { label: "Entities", value: "entities" },
  { label: "Budget/Transactions", value: "budgetTransactions" },
  { label: "EveryDollar Budgets/Transactions CSV", value: "everyDollarCsv" },
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
const everyDollarBudgetCsvFile = ref<File | null>(null);
const everyDollarTransactionsCsvFile = ref<File | null>(null);

watch(importType, (val) => {
  if (val !== 'everyDollarCsv') {
    everyDollarBudgetCsvFile.value = null;
    everyDollarTransactionsCsvFile.value = null;
    pendingImportData.value = null;
    overwriteMonths.value = [];
    showOverwriteDialog.value = false;
    resetEveryDollarDiffState();
  }
});

// ---------------------------------------------------------------------------
// EveryDollar CSV import — types and diff state
// ---------------------------------------------------------------------------

interface EveryDollarMonthData {
  categories: Map<string, BudgetCategory>;
  transactions: Transaction[];
  incomeTarget: number;
  month: string;
}

interface EveryDollarCategoryFieldDiff {
  field: 'target' | 'carryover' | 'isFund' | 'group';
  existing: string | number | boolean | undefined;
  imported: string | number | boolean | undefined;
}

interface EveryDollarCategoryDiffRow {
  name: string;
  group: string;
  isFund: boolean;
  differences: EveryDollarCategoryFieldDiff[];
}

interface EveryDollarNewCategory {
  key: string;
  category: BudgetCategory;
}

interface EveryDollarTxPreviewRow {
  key: string;
  transaction: Transaction;
  matchedExistingId?: string;
  matchedExistingSummary?: string;
}

interface EveryDollarBudgetDiff {
  month: string;
  existingBudgetId: string | null;
  existingBudget: Budget | null;
  importedLabel: string;
  importedIncomeTarget: number;
  labelDiff: { existing: string; imported: string } | null;
  incomeTargetDiff: { existing: number; imported: number } | null;
  newCategories: EveryDollarNewCategory[];
  changedCategories: EveryDollarCategoryDiffRow[];
  newTransactions: EveryDollarTxPreviewRow[];
  duplicateTransactions: EveryDollarTxPreviewRow[];
}

interface EveryDollarDiffSelectionEntry {
  createBudget: boolean;
  addCategoryKeys: Record<string, boolean>;
  addTxKeys: Record<string, boolean>;
  showDuplicates: boolean;
  expanded: boolean;
}

const everyDollarDiffs = ref<EveryDollarBudgetDiff[]>([]);
const everyDollarDiffSelection = reactive<Record<string, EveryDollarDiffSelectionEntry>>({});
const everyDollarDuplicateWindowDays = ref(3);

function clearEveryDollarDiffSelection() {
  Object.keys(everyDollarDiffSelection).forEach((key) => {
    delete everyDollarDiffSelection[key];
  });
}

function resetEveryDollarDiffState() {
  everyDollarDiffs.value = [];
  clearEveryDollarDiffSelection();
}

function initializeDiffSelection(diffs: EveryDollarBudgetDiff[]) {
  clearEveryDollarDiffSelection();
  diffs.forEach((diff) => {
    const addCategoryKeys: Record<string, boolean> = {};
    diff.newCategories.forEach((c) => {
      addCategoryKeys[c.key] = true;
    });
    const addTxKeys: Record<string, boolean> = {};
    diff.newTransactions.forEach((t) => {
      addTxKeys[t.key] = true;
    });
    everyDollarDiffSelection[diff.month] = {
      createBudget: diff.existingBudgetId === null,
      addCategoryKeys,
      addTxKeys,
      showDuplicates: false,
      expanded: true,
    };
  });
}

function normalizeMerchant(name: string | undefined): string {
  if (!name) return '';
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, ' ')
    .trim();
}

function merchantsLookSimilar(a: string, b: string): boolean {
  const na = normalizeMerchant(a);
  const nb = normalizeMerchant(b);
  if (!na || !nb) return na === nb;
  if (na === nb) return true;
  if (na.includes(nb) || nb.includes(na)) return true;
  // Token overlap: share at least one non-trivial token (len >= 4)
  const ta = new Set(na.split(' ').filter((t) => t.length >= 4));
  const tb = new Set(nb.split(' ').filter((t) => t.length >= 4));
  for (const t of ta) {
    if (tb.has(t)) return true;
  }
  return false;
}

function roundCents(n: number): number {
  return Math.round(Math.abs(n) * 100);
}

function transactionTotal(tx: Transaction): number {
  if (Array.isArray(tx.categories) && tx.categories.length > 0) {
    return tx.categories.reduce((sum, c) => sum + Math.abs(c.amount), 0);
  }
  return Math.abs(tx.amount);
}

function sameDay(a: string | undefined, b: string | undefined): boolean {
  if (!a || !b) return false;
  return a.slice(0, 10) === b.slice(0, 10);
}

function dateWithinDays(a: string | undefined, b: string | undefined, days: number): boolean {
  if (!a || !b) return false;
  const ta = new Date(a).getTime();
  const tb = new Date(b).getTime();
  if (Number.isNaN(ta) || Number.isNaN(tb)) return false;
  return Math.abs(ta - tb) <= days * 86400000;
}

function findDuplicateExisting(
  candidate: Transaction,
  existing: Transaction[],
  windowDays: number,
): Transaction | null {
  const candidateCents = roundCents(transactionTotal(candidate));
  const candidateMerchant = candidate.merchant || '';
  for (const ex of existing) {
    if (ex.deleted) continue;
    if (roundCents(transactionTotal(ex)) !== candidateCents) continue;
    const dateMatch = windowDays <= 0
      ? sameDay(ex.date, candidate.date)
      : dateWithinDays(ex.date, candidate.date, windowDays);
    if (!dateMatch) continue;
    if (!merchantsLookSimilar(ex.merchant || '', candidateMerchant)) continue;
    return ex;
  }
  return null;
}

async function fetchBudgetsForAccessibleInfos(infos: BudgetInfo[]): Promise<Budget[]> {
  if (infos.length === 0) {
    return [];
  }

  const dedupedById = new Map<string, BudgetInfo>();
  infos.forEach((info) => {
    if (info?.budgetId) {
      dedupedById.set(info.budgetId, info);
    }
  });

  if (dedupedById.size === 0) {
    return [];
  }

  const loadedBudgets: Budget[] = [];

  const idsToFetch = Array.from(dedupedById.keys());
  const concurrency = 4;

  for (let i = 0; i < idsToFetch.length; i += concurrency) {
    const slice = idsToFetch.slice(i, i + concurrency);
    const results = await Promise.allSettled(
      slice.map(async (budgetId) => {
        try {
          const budget = await dataAccess.getBudget(budgetId);
          if (budget) {
            if (!budget.budgetId) budget.budgetId = budgetId;
            budgetStore.updateBudget(budgetId, budget);
            loadedBudgets.push(budget);
          }
        } catch (err) {
          console.error(`Failed to load budget ${budgetId}`, err);
        }
      }),
    );

    results.forEach((result) => {
      if (result.status === 'rejected') {
        console.error('Failed to load a budget in batch', result.reason);
      }
    });
  }

  return loadedBudgets;
}

// Bank/Card Transactions Import
const previewBankTransactions = ref<any[]>([]);
const csvHeaders = ref<string[]>([]);
const rawCsvData = ref<any[]>([]);
const commonBankTransactionFields = ref([
  { key: "payee", label: "Payee", required: true },
  { key: "transactionDate", label: "Transaction Date", required: false },
  { key: "postedDate", label: "Posted Date", required: false },
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
  { name: 'transactionDate', label: 'Transaction Date', field: 'transactionDate' },
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
  return availableAccounts.value
    .slice()
    .sort((a, b) => (a.name || '').localeCompare(b.name || '', undefined, { sensitivity: 'base' }))
    .map((account) => ({
      title: `${account.name} (*${account.accountNumber ? account.accountNumber.slice(-4) : "N/A"})`,
      value: account.id,
    }));
});

const isFieldMappingValid = computed(() => {
  const requiredCommonFields = commonBankTransactionFields.value.every((field) => !field.required || !!fieldMapping.value[field.key]);
  const hasDateMapping = Boolean(fieldMapping.value.postedDate || fieldMapping.value.transactionDate);
  if (!hasDateMapping) {
    return false;
  }
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
    const fullBudgets = await fetchBudgetsForAccessibleInfos(accessibleBudgets);
    budgets.value = fullBudgets;

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
    dataLoaded.value = true;
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
        groupName: row.group || "",
        isFund: row.isfund === "true" || row.isfund === "1",
        target: parseFloat(row.target) || 0,
        carryover: parseFloat(row.carryover) || 0,
      });
    });
  }

  const parseCurrencyField = (value: any): number | null => {
    const normalized = cleanCsvString(value);
    if (normalized === "") {
      return null;
    }
    const numeric = Number(normalized.replace(/[^0-9.-]/g, ""));
    if (Number.isNaN(numeric)) {
      return null;
    }
    return numeric;
  };

  const parseCurrencyFromRow = (row: Record<string, any>, fields: string[]): number | null => {
    for (const field of fields) {
      if (!Object.prototype.hasOwnProperty.call(row, field)) {
        continue;
      }
      const parsed = parseCurrencyField(row[field]);
      if (parsed === null) {
        continue;
      }
      if (field.toLowerCase().includes("cents")) {
        return parsed / 100;
      }
      return parsed;
    }
    return null;
  };

  const buildTransactionGroupKey = (row: Record<string, any>, index: number) => {
    const baseId = cleanCsvString(row.transactionid || row.transaction_id || row.id);
    if (baseId) {
      return baseId;
    }
    const datePart = cleanCsvString(row.transactiondate || row.transaction_date || row.date);
    const merchantPart = cleanCsvString(row.merchant || row.payee || "");
    const amountPart = parseCurrencyFromRow(row, [
      "transaction_amount_dollars",
      "transaction_amount_cents",
      "amount",
      "amount_cents",
    ]);
    return `${datePart}_${merchantPart}_${amountPart ?? index}`;
  };

  const extractRowSplits = (row: Record<string, any>) => {
    const splits: { category: string; amount: number }[] = [];
    if (row.categories && typeof row.categories === "string") {
      try {
        const parsed = JSON.parse(row.categories);
        if (Array.isArray(parsed)) {
          parsed.forEach((entry) => {
            if (entry && typeof entry.category === "string" && typeof entry.amount === "number") {
              splits.push({ category: entry.category, amount: Math.abs(entry.amount) });
            }
          });
          if (splits.length > 0) {
            return splits;
          }
        }
      } catch {
        // fall back to legacy parsing below
      }
    }

    const label = cleanCsvString(
      row.allocation_label ||
        row.category ||
        row.item ||
        row.group_label ||
        row.group ||
        row.name ||
        ""
    );
    if (!label) {
      return splits;
    }

    const amount = parseCurrencyFromRow(row, [
      "allocation_amount_dollars",
      "allocation_amount_cents",
      "amount",
      "amount_cents",
      "transaction_amount_dollars",
      "transaction_amount_cents",
    ]);
    if (amount === null) {
      return splits;
    }
    splits.push({ category: label, amount: Math.abs(amount) });
    return splits;
  };

  if (transactionCSVData) {
    const groupedTransactions = new Map<
      string,
      { base: Record<string, any>; rows: Record<string, any>[] }
    >();
    transactionCSVData.forEach((row, index) => {
      const key = buildTransactionGroupKey(row, index);
      let group = groupedTransactions.get(key);
      if (!group) {
        group = { base: row, rows: [] };
        groupedTransactions.set(key, group);
      }
      group.rows.push(row);
    });

    groupedTransactions.forEach((group, layerIndex) => {
      const base = group.base;
      let month = base.budgetmonth;
      if (!month && base.budgetid) {
        const match = base.budgetid.match(/_(\d{4}-\d{2})$/);
        if (match) {
          month = match[1];
        }
      }
      if (!month) {
        try {
          month = toBudgetMonth(base.transactiondate);
        } catch {
          previewErrors.value.push(
            `File ${transactionCSVFile}, Group ${layerIndex + 1}: Invalid transaction date: ${base.transactiondate}`
          );
        }
      }
      if (!month) {
        previewErrors.value.push(
          `File ${transactionCSVFile}, Group ${layerIndex + 1}: budgetMonth is required and could not be derived`
        );
        return;
      }

      let budgetId = monthToId.get(month);
      if (!budgetId) {
        budgetId = uuidv4();
        monthToId.set(month, budgetId);
        budgetsById.set(budgetId, {
          budgetId,
          budgetMonth: month,
          month,
          incomeTarget: 0,
          categories: [],
          transactions: [],
          merchants: [],
          familyId: familyStore.family?.id || "",
          label: `Imported Budget ${month}`,
          entityId: selectedEntityId.value,
        });
      }

      const categoryTotals = new Map<string, number>();
      group.rows.forEach((row) => {
        const splits = extractRowSplits(row);
        splits.forEach((split) => {
          const name = split.category.trim();
          if (!name) {
            return;
          }
          categoryTotals.set(name, (categoryTotals.get(name) || 0) + split.amount);
        });
      });

      const categories = Array.from(categoryTotals.entries()).map(([category, amount]) => ({
        category,
        amount,
      }));

      let totalAmount = parseCurrencyFromRow(base, [
        "transaction_amount_dollars",
        "transaction_amount_cents",
        "amount",
        "amount_cents",
      ]);
      if (totalAmount === null) {
        totalAmount = categories.reduce((sum, cat) => sum + cat.amount, 0);
      }

      if (categories.length === 0 && totalAmount !== null && totalAmount !== 0) {
        const fallbackCategory = cleanCsvString(
          base.category || base.categories || base.categoryid || base.item || "Uncategorized"
        );
        categories.push({
          category: fallbackCategory || "Uncategorized",
          amount: Math.abs(totalAmount),
        });
      }
      if (categories.length === 0) {
        previewErrors.value.push(
          `File ${transactionCSVFile}, Group ${layerIndex + 1}: Unable to determine categories`
        );
        return;
      }

      const transactionId = cleanCsvString(base.transactionid || base.transaction_id) || uuidv4();
      const transactionData: Transaction = {
        id: transactionId,
        userId: user.uid,
        budgetMonth: month,
        budgetId,
        date: base.transactiondate,
        merchant: base.merchant || base.payee || "",
        categories,
        amount: Math.abs(totalAmount ?? categories.reduce((sum, cat) => sum + cat.amount, 0)),
        notes: base.notes || "",
        recurring: base.recurring === "true" || base.recurring === "1",
        recurringInterval: base.recurringinterval || "Monthly",
        isIncome:
          (totalAmount ?? categories.reduce((sum, cat) => sum + cat.amount, 0)) > 0,
        accountNumber: base.accountNumber || "",
        accountSource: base.accountSource || "",
        postedDate: base.postedDate || "",
        status: base.status || "U",
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
      // CSV interop format keeps `group` as a column header (it's the
      // human-readable name).
      group: category.groupName || "",
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

function cleanCsvString(value: any, trim = true): string {
  if (value === null || value === undefined) {
    return '';
  }
  const normalized = String(value).replace(/\r/g, '');
  return trim ? normalized.trim() : normalized;
}

function parseEveryDollarCurrency(rawCents: any, rawDollars: any): number {
  const centsString = cleanCsvString(rawCents);
  if (centsString !== '') {
    const centsValue = Number(centsString.replace(/[^0-9.-]/g, ''));
    if (!Number.isNaN(centsValue)) {
      return centsValue / 100;
    }
  }
  const dollarsString = cleanCsvString(rawDollars);
  if (dollarsString !== '') {
    const dollarsValue = Number(dollarsString.replace(/[^0-9.-]/g, ''));
    if (!Number.isNaN(dollarsValue)) {
      return dollarsValue;
    }
  }
  return 0;
}

function parseBooleanish(value: any): boolean {
  if (typeof value === 'boolean') {
    return value;
  }
  if (typeof value === 'number') {
    return value === 1;
  }
  if (typeof value === 'string') {
    const normalized = cleanCsvString(value).toLowerCase();
    return ['true', '1', 'yes', 'y'].includes(normalized);
  }
  return false;
}

async function importEveryDollarCsv() {
  importError.value = null;
  importSuccess.value = null;

  if (!everyDollarBudgetCsvFile.value || !everyDollarTransactionsCsvFile.value) {
    importError.value = 'Please upload both EveryDollar budgets and transactions CSV files.';
    return;
  }

  const user = auth.currentUser;
  if (!user) {
    importError.value = 'User not authenticated';
    return;
  }
  if (!familyId.value) {
    showSnackbar('Cannot import without a Family/Org', 'negative');
    return;
  }
  if (!selectedEntityId.value) {
    importError.value = 'Entity selection is required';
    return;
  }

  importing.value = true;
  overwriteMonths.value = [];
  showOverwriteDialog.value = false;
  pendingImportData.value = null;

  try {
    const [budgetText, transactionText] = await Promise.all([
      everyDollarBudgetCsvFile.value.text(),
      everyDollarTransactionsCsvFile.value.text(),
    ]);

    const parseOptions = {
      header: true,
      skipEmptyLines: 'greedy' as const,
      transformHeader: (header: string) =>
        header
          .toLowerCase()
          .replace(/\r/g, '')
          .replace(/\s+/g, '_')
          .replace(/[^a-z0-9_]/g, '_')
          .replace(/__+/g, '_')
          .replace(/^_|_$/g, ''),
    };

    const parseCsv = (text: string) => {
      const result = Papa.parse(text, parseOptions);
      if (result.errors?.length) {
        throw new Error(result.errors.map((e) => e.message).join('; '));
      }
      const rows = (result.data as any[]).map((row) => {
        const normalized: Record<string, any> = {};
        Object.keys(row || {}).forEach((key) => {
          const value = row[key];
          normalized[key] = typeof value === 'string' ? value.replace(/\r/g, '') : value;
        });
        return normalized;
      });
      return rows.filter((row) =>
        Object.values(row).some((value) => cleanCsvString(value) !== '')
      );
    };

    const budgetRows = parseCsv(budgetText);
    const transactionRows = parseCsv(transactionText);

    if (budgetRows.length === 0) {
      throw new Error('The budgets CSV did not contain any rows.');
    }
    if (transactionRows.length === 0) {
      throw new Error('The transactions CSV did not contain any rows.');
    }

    const deriveBudgetKey = (row: Record<string, any>, month: string) => {
      const budgetKeyCandidates = [
        'budgetid',
        'budget_id',
        'budget',
        'budgetidentifier',
        'budget_reference_id',
        'budgetreferenceid',
      ];
      for (const key of budgetKeyCandidates) {
        if (Object.prototype.hasOwnProperty.call(row, key)) {
          const value = cleanCsvString(row[key]);
          if (value) {
            return value;
          }
        }
      }
      return month;
    };
    const budgetData = new Map<string, EveryDollarMonthData>();
    const budgetKeysWithBudgetCsvRows = new Set<string>();
    const ensureBudgetEntry = (budgetKey: string, month: string) => {
      let entry = budgetData.get(budgetKey);
      if (!entry) {
        entry = { categories: new Map(), transactions: [], incomeTarget: 0, month };
        budgetData.set(budgetKey, entry);
      } else if (!entry.month && month) {
        entry.month = month;
      }
      return entry;
    };

    budgetRows.forEach((row) => {
      const monthDate = cleanCsvString(row.budget_month_date || row.budgetmonth || row.month);
      const month = monthDate ? monthDate.slice(0, 7) : '';
      if (!month) {
        return;
      }

      const itemLabel = cleanCsvString(row.item_label || row.itemlabel || row.category);
      if (!itemLabel) {
        return;
      }

      const budgetKey = deriveBudgetKey(row, month);
      const entry = ensureBudgetEntry(budgetKey, month);
      budgetKeysWithBudgetCsvRows.add(budgetKey);
      const groupLabel = cleanCsvString(row.group_label || row.grouplabel || row.group);
      const itemKeyBase = cleanCsvString(row.item_id);
      const itemKey = itemKeyBase || `${groupLabel}::${itemLabel}`;

      const targetAmount = parseEveryDollarCurrency(
        row.amount_budgeted_cents,
        row.amount_budgeted_dollars,
      );
      const carryoverAmount = parseEveryDollarCurrency(
        row.carry_over_cents,
        row.carry_over_dollars,
      );

      const type = cleanCsvString(row.item_type).toLowerCase();
      const subType = cleanCsvString(row.item_sub_type).toLowerCase();
      const isFund = type === 'sinking_fund' || subType === 'sinking_fund';

      const category: BudgetCategory = {
        name: itemLabel,
        groupName: groupLabel,
        isFund,
        target: targetAmount,
      };
      if (carryoverAmount !== 0) {
        category.carryover = carryoverAmount;
      }
      if (parseBooleanish(row.is_favorite)) {
        category.favorite = true;
      }

      entry.categories.set(itemKey, category);

      if (cleanCsvString(row.group_type).toLowerCase() === 'income') {
        entry.incomeTarget += targetAmount;
      }
    });

    const groupedTransactions = new Map<
      string,
      { base: Record<string, any>; allocations: Array<{ label: string; amount: number }> }
    >();

    transactionRows.forEach((row) => {
      const txId = cleanCsvString(row.transaction_id) || uuidv4();
      let grouped = groupedTransactions.get(txId);
      if (!grouped) {
        grouped = { base: row, allocations: [] };
        groupedTransactions.set(txId, grouped);
      }
      if (!grouped.base) {
        grouped.base = row;
      }
      const allocationLabel = cleanCsvString(row.allocation_label);
      const allocationAmount = parseEveryDollarCurrency(
        row.allocation_amount_cents,
        row.allocation_amount_dollars,
      );
      if (allocationLabel || allocationAmount !== 0) {
        grouped.allocations.push({ label: allocationLabel || '', amount: allocationAmount });
      }
    });

    groupedTransactions.forEach((grouped, txId) => {
      const base = grouped.base || {};
      const deletedAt = cleanCsvString(base.deleted_at);
      if (deletedAt) {
        return;
      }

      const date = cleanCsvString(base.transaction_date);
      if (!date) {
        return;
      }

      let month = '';
      try {
        month = toBudgetMonth(date);
      } catch {
        console.warn('Skipping EveryDollar CSV transaction with invalid date', txId, date);
        return;
      }

      const budgetKey = deriveBudgetKey(base, month);
      if (!budgetKeysWithBudgetCsvRows.has(budgetKey)) {
        return;
      }
      const entry = budgetData.get(budgetKey);
      if (!entry) {
        return;
      }
      let totalAmount = grouped.allocations.reduce((sum, alloc) => sum + alloc.amount, 0);
      if (totalAmount === 0) {
        totalAmount = parseEveryDollarCurrency(
          base.transaction_amount_cents,
          base.transaction_amount_dollars,
        );
      }

      const categoriesMap = new Map<string, number>();
      grouped.allocations.forEach((alloc) => {
        const label = alloc.label || cleanCsvString(base.allocation_label) || 'Uncategorized';
        const amount = Math.abs(alloc.amount);
        if (amount === 0) {
          return;
        }
        categoriesMap.set(label, (categoriesMap.get(label) || 0) + amount);
      });

      if (categoriesMap.size === 0 && Math.abs(totalAmount) > 0) {
        const fallbackLabel = cleanCsvString(base.allocation_label) || 'Uncategorized';
        categoriesMap.set(fallbackLabel, Math.abs(totalAmount));
      }

      if (categoriesMap.size === 0) {
        return;
      }

      const transaction: Transaction = {
        id: txId,
        userId: user.uid,
        familyId: familyId.value!,
        budgetMonth: month,
        date,
        merchant: cleanCsvString(base.merchant),
        categories: Array.from(categoriesMap.entries()).map(([category, amount]) => ({
          category,
          amount,
        })),
        amount: Math.abs(totalAmount),
        notes: typeof base.note === 'string' ? base.note.replace(/\r/g, '') : '',
        recurring: false,
        recurringInterval: 'Monthly',
        isIncome: totalAmount >= 0,
        accountNumber: '',
        accountSource: '',
        transactionDate: date,
        postedDate: '',
        status: 'U',
        entityId: selectedEntityId.value,
        taxMetadata: [],
      };

      entry.transactions.push(transaction);
    });

    if (budgetData.size === 0) {
      throw new Error('No budget months could be determined from the provided CSV files.');
    }

    // ------------------------------------------------------------------
    // Compute a diff between the parsed CSV data and existing budgets.
    // The import is additive-only — never delete, never overwrite.
    // ------------------------------------------------------------------
    const existingInfos = await dataAccess.loadAccessibleBudgets(user.uid, selectedEntityId.value);
    const monthToExistingInfo = new Map<string, BudgetInfo>();
    existingInfos.forEach((info) => {
      if (info.month && info.budgetId) {
        monthToExistingInfo.set(info.month, info);
      }
    });

    // Make sure we have FULL existing budgets (with categories/transactions)
    // for any month that will be diffed against.
    const monthsToDiff: string[] = [];
    budgetKeysWithBudgetCsvRows.forEach((key) => {
      const data = budgetData.get(key);
      if (data?.month) monthsToDiff.push(data.month);
    });
    const fullExistingByMonth = new Map<string, Budget>();
    for (const month of monthsToDiff) {
      const info = monthToExistingInfo.get(month);
      if (!info?.budgetId) continue;
      let full = budgetStore.budgets.get(info.budgetId) || null;
      const needsFetch =
        !full ||
        !Array.isArray(full.categories) ||
        full.categories.length === 0 ||
        !Array.isArray(full.transactions);
      if (needsFetch) {
        try {
          const fetched = await dataAccess.getBudget(info.budgetId);
          if (fetched) {
            if (!fetched.budgetId) fetched.budgetId = info.budgetId;
            budgetStore.updateBudget(info.budgetId, fetched);
            full = fetched;
          }
        } catch (err) {
          console.warn(`Failed to load existing budget ${info.budgetId} for diff`, err);
        }
      }
      if (full) fullExistingByMonth.set(month, full);
    }

    const diffs: EveryDollarBudgetDiff[] = [];
    const sortedKeys = Array.from(budgetKeysWithBudgetCsvRows).sort((a, b) => {
      const ma = budgetData.get(a)?.month || '';
      const mb = budgetData.get(b)?.month || '';
      return ma.localeCompare(mb);
    });

    sortedKeys.forEach((key) => {
      const data = budgetData.get(key);
      if (!data) return;
      const month = data.month;
      if (!month) {
        console.warn('Skipping EveryDollar budget entry without a month for key', key);
        return;
      }

      const importedCategories = Array.from(data.categories.values()).sort((a, b) => {
        const groupCompare = (a.groupName || '').localeCompare(b.groupName || '', undefined, {
          sensitivity: 'base',
        });
        if (groupCompare !== 0) return groupCompare;
        return a.name.localeCompare(b.name, undefined, { sensitivity: 'base' });
      });

      const existing = fullExistingByMonth.get(month) || null;
      const existingInfo = monthToExistingInfo.get(month) || null;
      const existingCategoryByName = new Map<string, BudgetCategory>();
      (existing?.categories || []).forEach((cat) => {
        const name = (cat.name || '').trim();
        if (name) existingCategoryByName.set(name.toLowerCase(), cat);
      });

      const newCategories: EveryDollarNewCategory[] = [];
      const changedCategories: EveryDollarCategoryDiffRow[] = [];

      importedCategories.forEach((imported) => {
        const importedName = (imported.name || '').trim();
        if (!importedName) return;
        const match = existingCategoryByName.get(importedName.toLowerCase());
        if (!match) {
          newCategories.push({
            key: `cat::${month}::${importedName.toLowerCase()}`,
            category: { ...imported },
          });
          return;
        }
        // Existing — compare fields for informational display
        const diffs: EveryDollarCategoryFieldDiff[] = [];
        if (Number(match.target || 0) !== Number(imported.target || 0)) {
          diffs.push({ field: 'target', existing: match.target || 0, imported: imported.target || 0 });
        }
        const existingCarry = Number(match.carryover ?? 0);
        const importedCarry = Number(imported.carryover ?? 0);
        if (existingCarry !== importedCarry) {
          diffs.push({ field: 'carryover', existing: existingCarry, imported: importedCarry });
        }
        if (Boolean(match.isFund) !== Boolean(imported.isFund)) {
          diffs.push({ field: 'isFund', existing: Boolean(match.isFund), imported: Boolean(imported.isFund) });
        }
        if ((match.groupName || '') !== (imported.groupName || '')) {
          diffs.push({
            field: 'group',
            existing: match.groupName || '',
            imported: imported.groupName || '',
          });
        }
        if (diffs.length > 0) {
          changedCategories.push({
            name: importedName,
            group: imported.groupName || '',
            isFund: Boolean(imported.isFund),
            differences: diffs,
          });
        }
      });

      // Transactions: match against existing via (date, amount, similar merchant)
      const existingTxs = (existing?.transactions || []).filter((t) => !t.deleted);
      const newTransactions: EveryDollarTxPreviewRow[] = [];
      const duplicateTransactions: EveryDollarTxPreviewRow[] = [];

      data.transactions.forEach((tx) => {
        if (existing?.budgetId) tx.budgetId = existing.budgetId;
        tx.budgetMonth = month;
        const match = findDuplicateExisting(tx, existingTxs, everyDollarDuplicateWindowDays.value);
        const row: EveryDollarTxPreviewRow = {
          key: tx.id || uuidv4(),
          transaction: tx,
        };
        if (match) {
          row.matchedExistingId = match.id;
          row.matchedExistingSummary = `${match.date} · ${match.merchant || '(no merchant)'} · ${formatCurrency(transactionTotal(match))}`;
          duplicateTransactions.push(row);
        } else {
          newTransactions.push(row);
        }
      });

      const importedLabel = `Imported EveryDollar ${month}`;
      const labelDiff =
        existing && existing.label !== importedLabel
          ? { existing: existing.label, imported: importedLabel }
          : null;
      const incomeTargetDiff =
        existing && Number(existing.incomeTarget || 0) !== Number(data.incomeTarget || 0)
          ? { existing: Number(existing.incomeTarget || 0), imported: Number(data.incomeTarget || 0) }
          : null;

      diffs.push({
        month,
        existingBudgetId: existingInfo?.budgetId || null,
        existingBudget: existing,
        importedLabel,
        importedIncomeTarget: data.incomeTarget,
        labelDiff,
        incomeTargetDiff,
        newCategories,
        changedCategories,
        newTransactions,
        duplicateTransactions,
      });
    });

    if (diffs.length === 0) {
      throw new Error('No budget months could be determined from the provided CSV files.');
    }

    everyDollarDiffs.value = diffs;
    initializeDiffSelection(diffs);
    overwriteMonths.value = [];
    showOverwriteDialog.value = false;
  } catch (error: any) {
    console.error('Error importing EveryDollar CSV files:', error);
    importError.value = `Failed to import EveryDollar CSVs: ${error.message}`;
    resetEveryDollarDiffState();
  } finally {
    importing.value = false;
  }

}

// ---------------------------------------------------------------------------
// EveryDollar diff helpers (UI + apply)
// ---------------------------------------------------------------------------

function diffSummaryCaption(diff: EveryDollarBudgetDiff): string {
  const parts: string[] = [];
  if (diff.existingBudgetId === null) parts.push('new budget');
  parts.push(`${diff.newCategories.length} new categor${diff.newCategories.length === 1 ? 'y' : 'ies'}`);
  parts.push(`${diff.newTransactions.length} new tx`);
  if (diff.duplicateTransactions.length > 0) {
    parts.push(`${diff.duplicateTransactions.length} duplicate${diff.duplicateTransactions.length === 1 ? '' : 's'}`);
  }
  if (diff.changedCategories.length > 0) {
    parts.push(`${diff.changedCategories.length} cat differ${diff.changedCategories.length === 1 ? 's' : ''}`);
  }
  return parts.join(' · ');
}

function formatDiffValue(field: string, value: string | number | boolean | undefined): string {
  if (value === undefined || value === null) return '—';
  if (field === 'target' || field === 'carryover') {
    return formatCurrency(Number(value) || 0);
  }
  if (field === 'isFund') {
    return value ? 'fund' : 'not fund';
  }
  return String(value);
}

function toggleShowDuplicates(month: string) {
  const entry = everyDollarDiffSelection[month];
  if (entry) entry.showDuplicates = !entry.showDuplicates;
}

function recomputeEveryDollarDiffDuplicates() {
  const windowDays = Math.max(0, Number(everyDollarDuplicateWindowDays.value) || 0);
  everyDollarDiffs.value = everyDollarDiffs.value.map((diff) => {
    const existingTxs = (diff.existingBudget?.transactions || []).filter((t) => !t.deleted);
    const allParsed: EveryDollarTxPreviewRow[] = [
      ...diff.newTransactions,
      ...diff.duplicateTransactions,
    ];
    const newTransactions: EveryDollarTxPreviewRow[] = [];
    const duplicateTransactions: EveryDollarTxPreviewRow[] = [];
    allParsed.forEach((row) => {
      const match = findDuplicateExisting(row.transaction, existingTxs, windowDays);
      if (match) {
        duplicateTransactions.push({
          key: row.key,
          transaction: row.transaction,
          matchedExistingId: match.id,
          matchedExistingSummary: `${match.date} · ${match.merchant || '(no merchant)'} · ${formatCurrency(transactionTotal(match))}`,
        });
      } else {
        newTransactions.push({ key: row.key, transaction: row.transaction });
      }
    });
    return { ...diff, newTransactions, duplicateTransactions };
  });

  // Re-initialize selection so new transactions default to selected and
  // duplicates default to unselected, while preserving createBudget state.
  everyDollarDiffs.value.forEach((diff) => {
    const current = everyDollarDiffSelection[diff.month];
    const addTxKeys: Record<string, boolean> = {};
    diff.newTransactions.forEach((t) => {
      addTxKeys[t.key] = true;
    });
    // Honor any previously-checked duplicates
    diff.duplicateTransactions.forEach((t) => {
      addTxKeys[t.key] = current?.addTxKeys[t.key] ?? false;
    });
    if (current) {
      current.addTxKeys = addTxKeys;
    }
  });
}

const hasAnyEveryDollarSelections = computed(() => {
  for (const diff of everyDollarDiffs.value) {
    const selection = everyDollarDiffSelection[diff.month];
    if (!selection) continue;
    if (diff.existingBudgetId === null && selection.createBudget) return true;
    if (Object.values(selection.addCategoryKeys).some(Boolean)) return true;
    if (Object.values(selection.addTxKeys).some(Boolean)) return true;
  }
  return false;
});

async function applyEveryDollarDiff() {
  importError.value = null;
  importSuccess.value = null;

  const user = auth.currentUser;
  if (!user) {
    showSnackbar('User not authenticated', 'negative');
    return;
  }
  if (!familyId.value) {
    showSnackbar('Cannot import without a Family/Org', 'negative');
    return;
  }
  if (!selectedEntityId.value) {
    showSnackbar('Entity selection is required', 'negative');
    return;
  }

  importing.value = true;
  $q.loading.show({
    message: 'Applying changes...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });

  // Track which budgets were touched so we can recalculate carryover once at the end.
  const affectedByBudgetId = new Map<string, { month: string; categories: Set<string> }>();
  const recordImpact = (budgetId: string | undefined | null, month: string, categoryName: string) => {
    if (!budgetId) return;
    let entry = affectedByBudgetId.get(budgetId);
    if (!entry) {
      entry = { month, categories: new Set() };
      affectedByBudgetId.set(budgetId, entry);
    }
    entry.categories.add(categoryName);
  };

  let totalCategoriesAdded = 0;
  let totalTxAdded = 0;
  let totalBudgetsCreated = 0;

  try {
    // Sort diffs by month ascending so cascades run in order
    const orderedDiffs = [...everyDollarDiffs.value].sort((a, b) => a.month.localeCompare(b.month));

    for (const diff of orderedDiffs) {
      const selection = everyDollarDiffSelection[diff.month];
      if (!selection) continue;

      const selectedCategoryKeys = new Set(
        Object.entries(selection.addCategoryKeys)
          .filter(([, v]) => v)
          .map(([k]) => k),
      );
      const selectedTxKeys = new Set(
        Object.entries(selection.addTxKeys)
          .filter(([, v]) => v)
          .map(([k]) => k),
      );
      const hasNothing =
        selectedCategoryKeys.size === 0 &&
        selectedTxKeys.size === 0 &&
        !(diff.existingBudgetId === null && selection.createBudget);
      if (hasNothing) continue;

      let targetBudget: Budget | null = diff.existingBudget;
      let targetBudgetId: string | null = diff.existingBudgetId;

      // -----------------------------------------------------------------
      // Create budget from CSV when it doesn't exist yet
      // -----------------------------------------------------------------
      if (targetBudgetId === null) {
        if (!selection.createBudget) continue;
        const newBudgetId = uuidv4();
        const chosenCategories: BudgetCategory[] = [];
        diff.newCategories.forEach((c) => {
          if (selectedCategoryKeys.has(c.key)) {
            chosenCategories.push({ ...c.category });
          }
        });
        // Ensure Income category exists (common requirement)
        const hasIncome = chosenCategories.some((c) => (c.name || '').trim().toLowerCase() === 'income');
        if (!hasIncome) {
          chosenCategories.push({ name: 'Income', target: 0, isFund: false, groupName: 'Income' });
        }

        const newBudget: Budget = {
          budgetId: newBudgetId,
          familyId: familyId.value,
          entityId: selectedEntityId.value,
          label: diff.importedLabel,
          month: diff.month,
          budgetMonth: diff.month,
          incomeTarget: diff.importedIncomeTarget,
          categories: chosenCategories,
          transactions: [],
          merchants: [],
        };

        await dataAccess.saveBudget(newBudgetId, newBudget, { skipCarryoverRecalc: true });
        budgetStore.updateBudget(newBudgetId, newBudget);
        targetBudget = newBudget;
        targetBudgetId = newBudgetId;
        totalBudgetsCreated += 1;
        totalCategoriesAdded += chosenCategories.length;
        chosenCategories.forEach((c) => {
          if (c.isFund && c.name) recordImpact(newBudgetId, diff.month, c.name);
        });
      } else {
        // -----------------------------------------------------------------
        // Add new categories to existing budget (never replace existing)
        // -----------------------------------------------------------------
        if (!targetBudget) {
          try {
            const fetched = await dataAccess.getBudget(targetBudgetId);
            if (fetched) {
              if (!fetched.budgetId) fetched.budgetId = targetBudgetId;
              budgetStore.updateBudget(targetBudgetId, fetched);
              targetBudget = fetched;
            }
          } catch (err) {
            console.error(`Failed to reload budget ${targetBudgetId}`, err);
          }
        }
        if (!targetBudget) {
          showSnackbar(`Could not load budget for ${diff.month}`, 'negative');
          continue;
        }

        const categoriesToAdd: BudgetCategory[] = [];
        diff.newCategories.forEach((c) => {
          if (selectedCategoryKeys.has(c.key)) {
            categoriesToAdd.push({ ...c.category });
          }
        });

        if (categoriesToAdd.length > 0) {
          const updatedBudget: Budget = {
            ...targetBudget,
            categories: [...(targetBudget.categories || []), ...categoriesToAdd],
          };
          await dataAccess.saveBudget(targetBudgetId, updatedBudget, { skipCarryoverRecalc: true });
          budgetStore.updateBudget(targetBudgetId, updatedBudget);
          targetBudget = updatedBudget;
          totalCategoriesAdded += categoriesToAdd.length;
          categoriesToAdd.forEach((c) => {
            if (c.isFund && c.name) recordImpact(targetBudgetId!, diff.month, c.name);
          });
        }
      }

      // -----------------------------------------------------------------
      // Save selected transactions (from newTransactions + any opted-in duplicates)
      // -----------------------------------------------------------------
      const allPreviewRows = [...diff.newTransactions, ...diff.duplicateTransactions];
      const txsToSave: Transaction[] = [];
      allPreviewRows.forEach((row) => {
        if (!selectedTxKeys.has(row.key)) return;
        // Generate a fresh transaction id so we never collide with the CSV tx_id
        const tx: Transaction = {
          ...row.transaction,
          id: uuidv4(),
          budgetId: targetBudgetId || undefined,
          budgetMonth: diff.month,
          userId: user.uid,
          familyId: familyId.value,
          entityId: selectedEntityId.value,
          status: row.transaction.status || 'U',
        };
        // Recompute total from splits to be safe
        if (Array.isArray(tx.categories) && tx.categories.length > 0) {
          tx.amount = tx.categories.reduce((s, c) => s + Math.abs(c.amount), 0);
        }
        txsToSave.push(tx);
        // Fund impact tracking
        (tx.categories || []).forEach((c) => {
          const cname = (c.category || '').trim();
          if (!cname) return;
          const matchCat = targetBudget?.categories.find(
            (bc) => bc.name && bc.name.toLowerCase() === cname.toLowerCase(),
          );
          if (matchCat?.isFund && targetBudgetId) {
            recordImpact(targetBudgetId, diff.month, matchCat.name || cname);
          }
        });
      });

      if (txsToSave.length > 0 && targetBudgetId && targetBudget) {
        await dataAccess.batchSaveTransactions(targetBudgetId, targetBudget, txsToSave, {
          skipCarryoverRecalc: true,
        });
        totalTxAdded += txsToSave.length;
      }
    }

    // -------------------------------------------------------------------
    // Carryover recalculation: run once from the LAST affected budget
    // forward, so imported carryover on earlier months is preserved and
    // only future (existing) budgets beyond the import range get updated.
    // -------------------------------------------------------------------
    if (affectedByBudgetId.size > 0) {
      let latestBudgetId: string | null = null;
      let latestMonth = '';
      const allCategories = new Set<string>();
      affectedByBudgetId.forEach((value, budgetId) => {
        value.categories.forEach((name) => allCategories.add(name));
        if (value.month > latestMonth) {
          latestMonth = value.month;
          latestBudgetId = budgetId;
        }
      });
      if (latestBudgetId && allCategories.size > 0) {
        try {
          await dataAccess.recalculateCarryover(latestBudgetId, Array.from(allCategories));
        } catch (err) {
          console.error('Failed to recalculate carryover after EveryDollar diff apply', err);
          showSnackbar(
            'Changes applied, but carryover recalculation failed. Please refresh and verify fund balances.',
            'warning',
          );
        }
      }
    }

    const parts: string[] = [];
    if (totalBudgetsCreated > 0) parts.push(`${totalBudgetsCreated} budget${totalBudgetsCreated === 1 ? '' : 's'} created`);
    if (totalCategoriesAdded > 0) parts.push(`${totalCategoriesAdded} categor${totalCategoriesAdded === 1 ? 'y' : 'ies'} added`);
    if (totalTxAdded > 0) parts.push(`${totalTxAdded} transaction${totalTxAdded === 1 ? '' : 's'} added`);
    if (parts.length === 0) {
      showSnackbar('No changes selected.', 'info');
    } else {
      importSuccess.value = parts.join(', ');
      showSnackbar(importSuccess.value, 'success');
    }

    resetEveryDollarDiffState();
    everyDollarBudgetCsvFile.value = null;
    everyDollarTransactionsCsvFile.value = null;
    await loadAllData();
  } catch (err: any) {
    console.error('Error applying EveryDollar diff:', err);
    importError.value = `Failed to apply changes: ${err.message}`;
    showSnackbar(importError.value, 'negative');
  } finally {
    $q.loading.hide();
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

      const normalizeDate = (value: string, label: string, required: boolean): string => {
        const raw = (value || '').trim();
        if (!raw) {
          if (required) {
            previewErrors.value.push(`Row ${index + 1}: ${label} is required`);
          }
          return '';
        }
        const yyyymmddRegex = /^\d{4}-\d{2}-\d{2}$/;
        if (yyyymmddRegex.test(raw)) {
          const parsed = new Date(raw);
          if (isNaN(parsed.getTime())) {
            previewErrors.value.push(`Row ${index + 1}: Invalid ${label.toLowerCase()} format, expected YYYY-MM-DD (e.g., 2023-12-31)`);
            return '';
          }
          return raw;
        }

        const parts = raw.split('/');
        if (parts.length === 3) {
          const [month, day, yearPart] = parts;
          const year = yearPart.length === 2 ? `20${yearPart}` : yearPart;
          const iso = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
          const parsed = new Date(iso);
          if (isNaN(parsed.getTime())) {
            previewErrors.value.push(`Row ${index + 1}: Invalid ${label.toLowerCase()} format, expected MM/DD/YYYY (e.g., 12/31/2023) or YYYY-MM-DD (e.g., 2023-12-31)`);
            return '';
          }
          return iso;
        }

        previewErrors.value.push(`Row ${index + 1}: Invalid ${label.toLowerCase()} format, expected MM/DD/YYYY (e.g., 12/31/2023) or YYYY-MM-DD (e.g., 2023-12-31)`);
        return '';
      };

      const normalizedTransactionDate = normalizeDate(mappedRow.transactionDate || '', 'Transaction Date', false);
      const normalizedPostedDate = normalizeDate(mappedRow.postedDate || '', 'Posted Date', false);

      mappedRow.transactionDate = normalizedTransactionDate;
      mappedRow.postedDate = normalizedPostedDate;

      if (!mappedRow.transactionDate && !mappedRow.postedDate) {
        previewErrors.value.push(`Row ${index + 1}: Either Transaction Date or Posted Date is required`);
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
        transactionDate: mappedRow.transactionDate || "",
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
    const existingTargetBudgetIds = new Set<string>();

    try {
      importRunning.value = true;

      const existingInfos = await dataAccess.loadAccessibleBudgets(user.uid, selectedEntityId.value);
      const monthToExistingId = new Map<string, string>();
      existingInfos.forEach((info) => {
        if (info.month && info.budgetId) monthToExistingId.set(info.month, info.budgetId);
      });

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

        // Build target budgets (use existing budgetId when present, otherwise a new id)
        for (const [originalBudgetId, month] of budgetIdToMonth) {
          const existingId = monthToExistingId.get(month);
          const targetBudgetId = existingId || uuidv4();
          budgetIdMap.set(originalBudgetId, targetBudgetId);

          if (existingId) {
            existingTargetBudgetIds.add(existingId);
          }

          const budget: Budget = {
            budgetId: targetBudgetId,
            familyId: familyId.value!,
            entityId: selectedEntityId.value,
            label: `Imported Budget ${month}`,
            month,
            incomeTarget: budgetIdToIncomeTarget.get(originalBudgetId) || 0,
            categories: [],
            transactions: [],
            merchants: [],
          };
          budgetsById.set(targetBudgetId, budget);
        }

        budgets.value = Array.from(budgetStore.budgets.values());

        previewData.value.categories.forEach((category) => {
          const originalBudgetId = category.budgetid;
          const targetBudgetId = budgetIdMap.get(originalBudgetId);
          if (!targetBudgetId) {
            console.error(`BudgetId ${originalBudgetId} not found in budgetIdMap`);
            return;
          }
          const budget = budgetsById.get(targetBudgetId)!;
          const existingCategory = budget.categories.find((c) => c.name === category.category);
          if (!existingCategory) {
            budget.categories.push({
              name: category.category,
              target: category.target,
              isFund: category.isfund === "true" || category.isfund === "1" || category.isfund === true,
              groupName: category.group || "",
              carryover: category.carryover || 0,
            });
          } else {
            existingCategory.target = category.target;
            existingCategory.isFund = category.isfund === "true" || category.isfund === "1" || category.isfund === true;
            existingCategory.groupName = category.group || "";
            existingCategory.carryover = category.carryover || 0;
          }
        });
      }

      if (previewData.value.transactions.length > 0) {
        previewData.value.transactions.forEach((txPreview) => {
          const originalBudgetId = txPreview.budgetid;
          const targetBudgetId = budgetIdMap.get(originalBudgetId);
          if (!targetBudgetId) {
            console.warn(`Transaction with BudgetId ${originalBudgetId} has no corresponding budget. Skipping.`);
            return;
          }

          const budget = budgetsById.get(targetBudgetId)!;
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
            budgetId: targetBudgetId,
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

      // Determine which months will overwrite existing budgets
      const existingMonths = new Set(existingInfos.map((b) => b.month));
      const importMonths = new Set(Array.from(budgetsById.values()).map((b) => b.month));
      overwriteMonths.value = Array.from(importMonths).filter((m) => existingMonths.has(m));

      if (overwriteMonths.value.length > 0) {
        pendingImportData.value = {
          budgetsById,
          budgetIdMap,
          entitiesById: new Map(),
          existingBudgetIds: existingTargetBudgetIds,
        };
        showPreview.value = false; // ensure the overwrite dialog is visible
        showOverwriteDialog.value = true;
      } else {
        pendingImportData.value = {
          budgetsById,
          budgetIdMap,
          entitiesById: new Map(),
          existingBudgetIds: existingTargetBudgetIds,
        };
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
          transactionDate: tx.transactionDate || tx.postedDate,
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
          transactionDate: tx.transactionDate || "",
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

    const storedPending = pendingImportData.value;
    const budgetsById: Map<string, Budget> = storedPending?.budgetsById ?? new Map();
    const existingBudgetIds = new Set(storedPending?.existingBudgetIds ?? []);

    const preservedTransactionStatuses = new Map<string, 'U' | 'C' | 'R'>();
    const preservedTransactions = new Map<string, Transaction[]>();
    const collectExistingBudgetData = async (budgetId: string) => {
      if (!budgetId) return;
      let budget = budgetStore.budgets.get(budgetId);
      if (!budget) {
        try {
          budget = await dataAccess.getBudget(budgetId);
        } catch (err) {
          console.error(`Failed to load budget ${budgetId} for status preservation`, err);
          return;
        }
      }
      let txs = budget.transactions;
      if ((!txs || txs.length === 0) && budget.budgetId) {
        try {
          txs = await dataAccess.getTransactions(budget.budgetId);
        } catch {
          txs = [];
        }
      }
      const validTxs = (txs || []).filter((tx) => tx && !tx.deleted);
      preservedTransactions.set(budgetId, validTxs);
      validTxs.forEach((tx) => {
        if (!tx.id) return;
        const status = tx.status || 'U';
        if (status !== 'U') {
          preservedTransactionStatuses.set(tx.id, status);
        }
      });
    };

    if (existingBudgetIds.size > 0) {
      await Promise.all(Array.from(existingBudgetIds).map((budgetId) => collectExistingBudgetData(budgetId)));
    }

    pendingImportData.value = null;

    const selectedBudgetIds = storedPending?.selectedBudgetIds;
    const entries = Array.from(budgetsById.entries())
      .filter(([key]) => !selectedBudgetIds || selectedBudgetIds.has(key))
      .map(([key, budget]) => {
        const targetId = budget.budgetId || key;
        return { key, targetId, budget };
      });

    entries.forEach(({ budget }) => {
      budget.transactions = (budget.transactions || []).map((tx) => {
        if (!tx) return tx;
        const savedStatus = preservedTransactionStatuses.get(tx.id);
        if (savedStatus) {
          tx.status = savedStatus;
        } else if (!tx.status) {
          tx.status = 'U';
        }
        if (!tx.userId) {
          tx.userId = user.uid;
        }
        if (!tx.familyId) {
          tx.familyId = familyId.value || '';
        }
        return tx;
      });
    });

    // Deduplicate: keep existing transactions, only add imported ones that are new
    entries.forEach(({ targetId, budget }) => {
      const existingTxs = preservedTransactions.get(targetId);
      if (!existingTxs || existingTxs.length === 0) return;

      const txFingerprint = (tx: Transaction) => {
        const date = tx.date || '';
        const merchant = (tx.merchant || '').toLowerCase().trim();
        const amount = Math.round(Math.abs(tx.amount) * 100);
        return `${date}|${merchant}|${amount}`;
      };

      const existingFingerprints = new Set(existingTxs.map(txFingerprint));

      const newImportedTxs = (budget.transactions || []).filter(
        (tx) => !existingFingerprints.has(txFingerprint(tx)),
      );

      budget.transactions = [...existingTxs, ...newImportedTxs];
    });

    const shouldDeleteExisting = existingBudgetIds.size > 0 && overwriteMonths.value.length > 0;

    if (shouldDeleteExisting) {
      for (const { targetId } of entries) {
        if (!existingBudgetIds.has(targetId)) continue;
        try {
          await dataAccess.deleteBudget(targetId);
        } catch {
          // Ignore if budget does not exist
        }
      }
    }

    const parseBudgetMonth = (input: string) => {
      const [yearStr, monthStr] = input.split("-");
      const year = Number(yearStr);
      const month = Number(monthStr);
      if (Number.isFinite(year) && Number.isFinite(month)) {
        return new Date(Date.UTC(year, month - 1, 1));
      }
      const parsed = new Date(input);
      return isNaN(parsed.getTime()) ? new Date(0) : parsed;
    };

    const sortedEntries = entries.sort((a, b) => {
      const dateA = parseBudgetMonth(a.budget.month);
      const dateB = parseBudgetMonth(b.budget.month);
      return dateA.getTime() - dateB.getTime();
    });

    const fundCategories = new Set<string>();

    for (const { targetId, budget } of sortedEntries) {
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

      budget.categories
        ?.filter((category) => category.isFund && category.name)
        .forEach((category) => {
          const name = (category.name || "").trim();
          if (name) {
            fundCategories.add(name);
          }
        });

      const toSave = { ...budget, budgetId: targetId } as Budget;
      await dataAccess.saveBudget(targetId, toSave, { skipCarryoverRecalc: true });
      budgetStore.updateBudget(targetId, toSave);
      showSnackbar(`Saved budget ${toSave.month} with ${toSave.transactions.length} transactions`);
    }

    // Recalculate from the LAST imported budget forward so that imported
    // carryover values are preserved. Only future budgets beyond the import
    // range get recalculated.
    if (sortedEntries.length > 0 && fundCategories.size > 0) {
      const lastEntry = sortedEntries[sortedEntries.length - 1];
      try {
        await dataAccess.recalculateCarryover(lastEntry.targetId, Array.from(fundCategories));
      } catch (error) {
        console.error("Failed to recalculate carryover after import", error);
        showSnackbar("Budgets imported, but carryover recalculation failed. Please refresh and verify fund balances.", "warning");
      }
    }

    const budgetMonths = sortedEntries.map(({ budget }) => budget.month);
    const mostRecentMonth = budgetMonths.sort((a, b) => {
      const dateA = new Date(a);
      const dateB = new Date(b);
      return dateB.getTime() - dateA.getTime();
    })[0];

    showSnackbar("Data imported successfully");
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
    resetEveryDollarDiffState();
    previewData.value = { entities: [], categories: [], transactions: [], accountsAndSnapshots: [] };
    previewErrors.value = [];
    selectedEntityId.value = "";
    overwriteMonths.value = [];
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
            group: category.groupName || "",
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
