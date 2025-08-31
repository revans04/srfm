<!-- src/views/AccountsView.vue -->
<template>
  <q-page fluid>
    <h1>Accounts</h1>

    <!-- Loading handled via $q.loading -->

    <!-- Family Prompt -->
    <q-banner v-if="!familyId" type="warning" class="mb-4"> Please create or join a family to manage accounts. </q-banner>

    <!-- Tabs -->
    <q-tabs v-model="tab" color="primary" :disabled="!familyId">
      <q-tab name="bank" label="Bank Accounts" />
      <q-tab name="credit" label="Credit Cards" />
      <q-tab name="investment" label="Investments" />
      <q-tab name="property" label="Properties" />
      <q-tab name="loan" label="Loans" />
      <q-tab name="net-worth" label="Snapshots" />
    </q-tabs>

    <q-tab-panels v-model="tab">
      <q-tab-panel name="bank">
        <AccountList
          :accounts="bankAccounts"
          :imported-transactions="importedTransactions"
          type="Bank"
          @add="openAccountDialog(AccountType.Bank)"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </q-tab-panel>
      <q-tab-panel name="credit">
        <AccountList
          :accounts="creditCardAccounts"
          :imported-transactions="importedTransactions"
          type="CreditCard"
          @add="openAccountDialog(AccountType.CreditCard)"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </q-tab-panel>
      <q-tab-panel name="investment">
        <AccountList
          :accounts="investmentAccounts"
          type="Investment"
          @add="openAccountDialog(AccountType.Investment)"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </q-tab-panel>
      <q-tab-panel name="property">
        <AccountList :accounts="propertyAccounts" type="Property" @add="openAccountDialog(AccountType.Property)" @edit="editAccount" @delete="confirmDeleteAccount" />
      </q-tab-panel>
      <q-tab-panel name="loan">
        <AccountList :accounts="loanAccounts" type="Loan" @add="openAccountDialog(AccountType.Loan)" @edit="editAccount" @delete="confirmDeleteAccount" />
      </q-tab-panel>
      <q-tab-panel name="net-worth">
        <q-card>
          <q-card-section>Net Worth Over Time</q-card-section>
          <q-card-section>
            <q-btn
              color="primary"
              flat
              dense
              class="mb-4 mr-2"
              icon="photo_camera"
              @click="openSnapshotDialog"
              :disabled="accounts.length === 0"
            >
              Capture Snapshot
            </q-btn>
            <q-btn
              color="error"
              flat
              dense
              class="mb-4"
              icon="delete"
              @click="confirmBatchDeleteSnapshots"
              :disabled="selectedSnapshots.length === 0"
              :loading="deleting"
            >
              Delete Selected
            </q-btn>
              <q-table
                :columns="snapshotColumns"
                :rows="snapshotsWithSelection"
                class="elevation-1"
                :pagination="{ rowsPerPage: 10 }"
                row-key="id"
                @row-click="viewSnapshotDetails"
              >
                <template #header-cell-select="props">
                  <q-th :props="props">
                    <q-checkbox v-model="selectAll" @update:modelValue="toggleSelectAll" dense @click.stop />
                  </q-th>
                </template>
                <template #body-cell-select="props">
                  <q-td :props="props">
                    <q-checkbox
                      v-model="selectedSnapshots"
                      :value="props.row.id"
                      dense
                      @update:modelValue="updateSelectAll"
                      @click.stop
                    />
                  </q-td>
                </template>
                <template #body-cell-date="props">
                  <q-td :props="props">
                    {{ formatTimestamp(props.row.date) }}
                  </q-td>
                </template>
                <template #body-cell-netWorth="props">
                  <q-td :props="props">
                    {{ formatCurrency(props.row.netWorth) }}
                  </q-td>
                </template>
                <template #body-cell-actions="props">
                  <q-td :props="props">
                    <q-btn
                      flat
                      dense
                      color="error"
                      icon="delete"
                      @click.stop="confirmDeleteSnapshot(props.row.id)"
                    />
                  </q-td>
                </template>
              </q-table>
            <div class="mt-4">
              <p>Net worth trend chart coming soon!</p>
            </div>
          </q-card-section>
        </q-card>
      </q-tab-panel>
    </q-tab-panels>

    <!-- Account Dialog -->
    <q-dialog v-model="showAccountDialog" max-width="600px">
      <q-card>
          <q-card-section>{{ editMode ? "Edit" : "Add" }} {{ accountType }} Account</q-card-section>
          <q-card-section>
            <AccountForm
              :account-type="accountType"
              :account="editMode ? newAccount : undefined"
              :show-personal-option="true"
              @save="saveAccount"
              @cancel="closeAccountDialog"
            />
          </q-card-section>
        </q-card>
      </q-dialog>

    <!-- Snapshot Dialog -->
    <q-dialog v-model="showSnapshotDialog" max-width="800px">
      <q-card>
        <q-card-section>Capture Net Worth Snapshot</q-card-section>
        <q-card-section>
          <q-form @submit.prevent="saveSnapshot">
            <q-input v-model="newSnapshot.date" label="Snapshot Date" type="date" variant="outlined" density="compact" required></q-input>
            <q-table
              :columns="snapshotAccountColumns"
              :rows="newSnapshot.accounts"
              hide-bottom
              :pagination="{ rowsPerPage: 100 }"
            >
              <template #body-cell-name="props">
                <q-td :props="props">
                  {{ getAccountName(props.row.accountId) }}
                </q-td>
              </template>
              <template #body-cell-type="props">
                <q-td :props="props">
                  {{ getAccountType(props.row.accountId) }}
                </q-td>
              </template>
              <template #body-cell-value="props">
                <q-td :props="props">
                  <q-input
                    v-model.number="props.row.value"
                    type="number"
                    outlined
                    dense
                    :prefix="getAccountCategory(props.row.accountId) === 'Liability' ? '-' : ''"
                  ></q-input>
                </q-td>
              </template>
            </q-table>
            <q-btn type="submit" color="primary" :loading="saving" class="mt-4"> Save Snapshot </q-btn>
            <q-btn color="grey" variant="text" @click="showSnapshotDialog = false" class="ml-2"> Cancel </q-btn>
          </q-form>
        </q-card-section>
      </q-card>
    </q-dialog>

    <!-- Delete Account Confirmation Dialog -->
    <q-dialog v-model="showDeleteAccountDialog" max-width="400">
      <q-card>
        <q-card-section class="bg-error py-3">
          <span class="text-white">Delete Account</span>
        </q-card-section>
        <q-card-section class="pt-4"> Are you sure you want to delete this account? </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteAccountDialog = false"> Cancel </q-btn>
          <q-btn color="error" variant="flat" @click="executeDeleteAccount"> Delete </q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Delete Snapshot Confirmation Dialog -->
    <q-dialog v-model="showDeleteSnapshotDialog" max-width="400">
      <q-card>
        <q-card-section class="bg-error py-3">
          <span class="text-white">Delete Snapshot</span>
        </q-card-section>
        <q-card-section class="pt-4"> Are you sure you want to delete this snapshot? </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteSnapshotDialog = false"> Cancel </q-btn>
          <q-btn color="error" variant="flat" @click="executeDeleteSnapshot"> Delete </q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

      <!-- Batch Delete Snapshots Confirmation Dialog -->
      <q-dialog v-model="showBatchDeleteSnapshotDialog" max-width="400">
        <q-card>
        <q-card-section class="bg-error py-3">
          <span class="text-white">Delete Selected Snapshots</span>
        </q-card-section>
        <q-card-section class="pt-4"> Are you sure you want to delete {{ selectedSnapshots.length }} selected snapshot(s)? </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showBatchDeleteSnapshotDialog = false"> Cancel </q-btn>
          <q-btn color="error" variant="flat" @click="executeBatchDeleteSnapshots"> Delete </q-btn>
        </q-card-actions>
        </q-card>
      </q-dialog>

      <!-- Snapshot Details Dialog -->
      <q-dialog v-model="showSnapshotDetailsDialog" max-width="600px">
        <q-card>
          <q-card-section>
            Snapshot on {{ snapshotDetails ? formatTimestamp(snapshotDetails.date) : '' }}
          </q-card-section>
          <q-card-section>
            <q-table
              :columns="snapshotDetailColumns"
              :rows="snapshotDetails ? snapshotDetails.accounts : []"
              hide-bottom
              :pagination="{ rowsPerPage: 0 }"
              flat
            >
              <template #body-cell-value="props">
                <q-td :props="props">
                  {{ formatCurrency(props.row.value) }}
                </q-td>
              </template>
            </q-table>
            <div class="text-right mt-4">
              <strong>Net Worth: {{ snapshotDetails ? formatCurrency(snapshotDetails.netWorth) : '' }}</strong>
            </div>
          </q-card-section>
          <q-card-actions class="justify-end">
            <q-btn color="primary" variant="text" @click="showSnapshotDetailsDialog = false">Close</q-btn>
          </q-card-actions>
        </q-card>
      </q-dialog>

    <!-- Update Budget Transactions Confirmation Dialog -->
    <q-dialog v-model="showUpdateBudgetTransactionsDialog" max-width="500px">
      <q-card>
        <q-card-section>Update Budget Transactions</q-card-section>
        <q-card-section>
          The account name or number has changed. Would you like to update associated budget transactions that were matched to imported transactions? <br />
          <strong>{{ affectedImportedTransactionCount }}</strong> imported transactions will be updated. <br />
          <strong>{{ affectedBudgetTransactionCount }}</strong> budget transactions may be affected.
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showUpdateBudgetTransactionsDialog = false"> No </q-btn>
          <q-btn color="primary" @click="confirmUpdateBudgetTransactions"> Yes </q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Snackbar handled via $q.notify -->
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { auth } from "../firebase/init";
import { dataAccess } from "../dataAccess";
import { useFamilyStore } from "../store/family";
import AccountList from "../components/AccountList.vue";
import AccountForm from "../components/AccountForm.vue"; // Import AccountForm directly
import type { Account, Snapshot, ImportedTransaction } from "../types";
import { AccountType } from "../types";
import { formatCurrency, formatTimestamp, todayISO, timestampToMillis } from "../utils/helpers";
import { useQuasar, QSpinner } from 'quasar';
import { v4 as uuidv4 } from "uuid";
import { Timestamp } from "firebase/firestore";

const familyStore = useFamilyStore();
const $q = useQuasar();

const tab = ref("bank");
const saving = ref(false);
const deleting = ref(false);
const accounts = ref<Account[]>([]);
const importedTransactions = ref<ImportedTransaction[]>([]);
const snapshots = ref<Snapshot[]>([]);
const snapshotDetails = ref<Snapshot | null>(null);
const showAccountDialog = ref(false);
const showSnapshotDialog = ref(false);
const showDeleteAccountDialog = ref(false);
const showDeleteSnapshotDialog = ref(false);
const showBatchDeleteSnapshotDialog = ref(false);
const showSnapshotDetailsDialog = ref(false);
const showUpdateBudgetTransactionsDialog = ref(false);
const accountType = ref<AccountType>(AccountType.Bank);
const editMode = ref(false);
const isPersonalAccount = ref(false);
const selectAll = ref(false);
const selectedSnapshots = ref<string[]>([]);
const originalAccountNumber = ref<string | undefined>("");
const originalAccountName = ref<string>("");
const affectedImportedTransactionCount = ref(0);
const affectedBudgetTransactionCount = ref(0);
const newAccount = ref<Account>({
  id: uuidv4(),
  name: "",
  type: AccountType.Bank,
  category: "Asset",
  createdAt: Timestamp.now(),
  updatedAt: Timestamp.now(),
  details: {},
});

const newSnapshot = ref<{
  date: string;
  accounts: Array<{
    accountId: string;
    accountName: string;
    type: string;
    value: number;
  }>;
}>({
  date: todayISO(),
  accounts: [],
});
const accountToDelete = ref<string | null>(null);
const snapshotToDelete = ref<string | null>(null);

const userId = computed(() => auth.currentUser?.uid || "");
const familyId = ref<string>("");

const bankAccounts = computed(() =>
  accounts.value
    .filter((acc) => acc.type === "Bank")
    .sort((a, b) => a.name.localeCompare(b.name))
);
const creditCardAccounts = computed(() =>
  accounts.value
    .filter((acc) => acc.type === "CreditCard")
    .sort((a, b) => a.name.localeCompare(b.name))
);
const investmentAccounts = computed(() =>
  accounts.value
    .filter((acc) => acc.type === "Investment")
    .sort((a, b) => a.name.localeCompare(b.name))
);
const propertyAccounts = computed(() =>
  accounts.value
    .filter((acc) => acc.type === "Property")
    .sort((a, b) => a.name.localeCompare(b.name))
);
const loanAccounts = computed(() =>
  accounts.value
    .filter((acc) => acc.type === "Loan")
    .sort((a, b) => a.name.localeCompare(b.name))
);

const snapshotColumns = [
  { name: 'select', label: '', field: 'select', sortable: false },
  { name: 'date', label: 'Date', field: 'date' },
  { name: 'netWorth', label: 'Net Worth', field: 'netWorth' },
  { name: 'actions', label: 'Actions', field: 'actions' },
];

const snapshotAccountColumns = [
  { name: 'name', label: 'Account', field: 'accountName' },
  { name: 'type', label: 'Type', field: 'type' },
  { name: 'value', label: 'Value', field: 'value' },
];

const snapshotDetailColumns = [
  { name: 'accountName', label: 'Account', field: 'accountName' },
  { name: 'type', label: 'Type', field: 'type' },
  { name: 'value', label: 'Value', field: 'value' },
];

const snapshotsWithSelection = computed(() => {
  return snapshots.value.map((snapshot) => ({
    ...snapshot,
    selected: selectedSnapshots.value.includes(snapshot.id),
  }));
});

function updateSelectAll() {
  selectAll.value = snapshots.value.length > 0 && selectedSnapshots.value.length === snapshots.value.length;
}

function toggleSelectAll(value: boolean) {
  if (value) {
    selectedSnapshots.value = snapshots.value.map((snapshot) => snapshot.id);
  } else {
    selectedSnapshots.value = [];
  }
  updateSelectAll();
}

function viewSnapshotDetails(_: unknown, row: Snapshot) {
  snapshotDetails.value = row;
  showSnapshotDetailsDialog.value = true;
}

onMounted(async () => {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar("Please log in to view accounts", "error");
    return;
  }

  $q.loading.show({
    message: 'Loading data...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });
  try {
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar("No family found. Please create or join a family.", "error");
      return;
    }
    familyId.value = family.id;
    await loadData();
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : JSON.stringify(error);
    showSnackbar(`Error loading data: ${msg}`, "error");
    console.error("Error during onMounted:", error);
  } finally {
    $q.loading.hide();
  }
});

async function loadData() {
  if (!familyId.value) return;
  try {
    accounts.value = await dataAccess.getAccounts(familyId.value);
    importedTransactions.value = await dataAccess.getImportedTransactions();
    snapshots.value = await dataAccess.getSnapshots(familyId.value);
  } catch (error: unknown) {
    console.error("Error loading data:", error);
    const msg = error instanceof Error ? error.message : JSON.stringify(error);
    showSnackbar(`Error loading data: ${msg}`, "error");
  }
}

function openAccountDialog(type: AccountType) {
  accountType.value = type;
  editMode.value = false;
  isPersonalAccount.value = false;
  newAccount.value = {
    id: uuidv4(),
    name: "",
    type,
    category: type === AccountType.CreditCard || type === AccountType.Loan ? "Liability" : "Asset",
    createdAt: Timestamp.now(),
    updatedAt: Timestamp.now(),
    details: {},
  };
  showAccountDialog.value = true;
}

function editAccount(account: Account) {
  accountType.value = account.type as AccountType;
  editMode.value = true;
  isPersonalAccount.value = !!account.userId;
  newAccount.value = { ...account, details: { ...(account.details || {}) } };
  originalAccountNumber.value = account.accountNumber;
  originalAccountName.value = account.name;
  showAccountDialog.value = true;
}

async function saveAccount(account: Account, isPersonal: boolean) {
  saving.value = true;
  try {
    if (isPersonal) {
      account.userId = userId.value;
    } else {
      delete account.userId;
    }

    // Detect changes in accountNumber or name (only in edit mode)
    let accountNumberChanged = false;
    let accountNameChanged = false;
    if (editMode.value) {
      accountNumberChanged = originalAccountNumber.value !== account.accountNumber;
      accountNameChanged = originalAccountName.value !== account.name;
    }

    // Save the account
    await dataAccess.saveAccount(familyId.value, account);

    let snapshot = null;
    if (!editMode.value) {
      snapshot = {
        id: uuidv4(),
        date: Timestamp.fromDate(new Date()),
        accounts: [
          {
            accountId: account.id,
            accountName: account.name,
            type: account.type,
            value: (() => { const bal = account.balance ?? 0; return account.category === 'Liability' && bal > 0 ? -bal : bal; })(),
          },
        ],
        netWorth: (() => { const bal = account.balance ?? 0; return account.category === 'Liability' && bal > 0 ? -bal : bal; })(),
        createdAt: Timestamp.now(),
      };
      await dataAccess.saveSnapshot(familyId.value, snapshot);
    }

    // Update the local accounts array
    const index = accounts.value.findIndex((a) => a.id === account.id);
    if (index >= 0) {
      accounts.value[index] = { ...account };
    } else {
      accounts.value.push({ ...account });
    }

    if (!editMode.value && snapshot) {
      snapshots.value.push(snapshot);
      snapshots.value.sort((a, b) => timestampToMillis(b.date) - timestampToMillis(a.date));
    }

    // Handle updates to ImportedTransactions and Budget Transactions if accountNumber or name changed
    if (editMode.value && (accountNumberChanged || accountNameChanged)) {
      // Fetch ImportedTransactions for this account
      const importedTxs = await dataAccess.getImportedTransactionsByAccountId(account.id);
      affectedImportedTransactionCount.value = importedTxs.length;

      if (affectedImportedTransactionCount.value > 0) {
        // Update ImportedTransactions
        const updatedImportedTxs = importedTxs.map((tx) => ({
          ...tx,
          ...(accountNumberChanged && account.accountNumber ? { accountNumber: account.accountNumber } : {}),
          ...(accountNameChanged ? { accountSource: account.institution || account.name } : {}),
        }));
        await dataAccess.updateImportedTransactions(updatedImportedTxs);

        // Update local importedTransactions
        updatedImportedTxs.forEach((updatedTx) => {
          const idx = importedTransactions.value.findIndex((it) => it.id === updatedTx.id);
          if (idx >= 0) {
            importedTransactions.value[idx] = updatedTx;
          }
        });

        // Fetch potentially affected Budget Transactions
        const budgetTxsWithBudgetId = await dataAccess.getBudgetTransactionsMatchedToImported(account.id);
        affectedBudgetTransactionCount.value = budgetTxsWithBudgetId.length;

        if (affectedBudgetTransactionCount.value > 0) {
          // Show confirmation dialog for updating Budget Transactions
          showUpdateBudgetTransactionsDialog.value = true;
          return; // Wait for user confirmation before proceeding
        }
      }
    }

    showSnackbar(`${accountType.value} account saved successfully`, "success");
    closeAccountDialog();
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : JSON.stringify(error);
    showSnackbar(`Error saving account: ${msg}`, "error");
    console.error("Error saving account:", error);
  } finally {
    saving.value = false;
  }
}

async function confirmUpdateBudgetTransactions() {
  try {
    // Fetch Budget Transactions again to ensure we have the latest data
    const budgetTxsWithBudgetId = await dataAccess.getBudgetTransactionsMatchedToImported(newAccount.value.id);
    if (budgetTxsWithBudgetId.length > 0) {
      // Update Budget Transactions
      const updatedBudgetTxs = budgetTxsWithBudgetId.map((item) => ({
        budgetId: item.budgetId,
        transaction: {
          ...item.transaction,
          ...(newAccount.value.accountNumber ? { accountNumber: newAccount.value.accountNumber } : {}),
          accountSource: newAccount.value.institution || newAccount.value.name,
        },
      }));
      await dataAccess.updateBudgetTransactions(updatedBudgetTxs);
      showSnackbar(`${budgetTxsWithBudgetId.length} budget transactions updated successfully`, "success");
    }
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : JSON.stringify(error);
    showSnackbar(`Error updating budget transactions: ${msg}`, "error");
    console.error("Error updating budget transactions:", error);
  } finally {
    showUpdateBudgetTransactionsDialog.value = false;
    closeAccountDialog();
  }
}

function closeAccountDialog() {
  showAccountDialog.value = false;
  originalAccountNumber.value = "";
  originalAccountName.value = "";
  affectedImportedTransactionCount.value = 0;
  affectedBudgetTransactionCount.value = 0;
  newAccount.value = {
    id: uuidv4(),
    name: "",
    type: AccountType.Bank,
    category: "Asset",
    createdAt: Timestamp.now(),
    updatedAt: Timestamp.now(),
    details: {},
  };
}

function confirmDeleteAccount(accountId: string) {
  accountToDelete.value = accountId;
  showDeleteAccountDialog.value = true;
}

async function executeDeleteAccount() {
  if (!accountToDelete.value) return;

  try {
    await dataAccess.deleteAccount(familyId.value, accountToDelete.value);
    accounts.value = accounts.value.filter((acc) => acc.id !== accountToDelete.value);
    showSnackbar("Account deleted successfully", "success");
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : JSON.stringify(error);
    showSnackbar(`Error deleting account: ${msg}`, "error");
  } finally {
    showDeleteAccountDialog.value = false;
    accountToDelete.value = null;
  }
}

function openSnapshotDialog() {
  const sortedAccounts = accounts.value.slice().sort((a, b) => {
    if (a.type === b.type) return a.name.localeCompare(b.name);
    return a.type.localeCompare(b.type);
  });

  newSnapshot.value = {
    date: todayISO(),
    accounts: sortedAccounts.map((account) => {
      const bal = account.balance ?? 0;
      return {
        accountId: account.id,
        accountName: account.name,
        type: account.type,
        value: bal,
      };
    }),
  };
  showSnapshotDialog.value = true;
}

async function saveSnapshot() {
  if (!newSnapshot.value.date) {
    showSnackbar("Snapshot date is required", "error");
    return;
  }

  saving.value = true;
  try {
    const snapshot: Snapshot = {
      id: uuidv4(),
      date: Timestamp.fromDate(new Date(newSnapshot.value.date)),
      accounts: newSnapshot.value.accounts,
      netWorth: newSnapshot.value.accounts.reduce((sum, a) => sum + (getAccountCategory(a.accountId) === "Liability" && a.value > 0 ? -a.value : a.value), 0),
      createdAt: Timestamp.now(),
    };

    await dataAccess.saveSnapshot(familyId.value, snapshot);
    snapshots.value.push(snapshot);
    snapshots.value.sort((a, b) => timestampToMillis(b.date) - timestampToMillis(a.date));

    showSnackbar("Snapshot saved successfully", "success");
    showSnapshotDialog.value = false;
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : JSON.stringify(error);
    showSnackbar(`Error saving snapshot: ${msg}`, "error");
    console.log(error);
  } finally {
    saving.value = false;
  }
}

function confirmDeleteSnapshot(snapshotId: string) {
  snapshotToDelete.value = snapshotId;
  showDeleteSnapshotDialog.value = true;
}

async function executeDeleteSnapshot() {
  if (!snapshotToDelete.value) return;

  try {
    await dataAccess.deleteSnapshot(familyId.value, snapshotToDelete.value);
    snapshots.value = snapshots.value.filter((s) => s.id !== snapshotToDelete.value);
    selectedSnapshots.value = selectedSnapshots.value.filter((id) => id !== snapshotToDelete.value);
    updateSelectAll();
    showSnackbar("Snapshot deleted successfully", "success");
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : JSON.stringify(error);
    showSnackbar(`Error deleting snapshot: ${msg}`, "error");
  } finally {
    showDeleteSnapshotDialog.value = false;
    snapshotToDelete.value = null;
  }
}

function confirmBatchDeleteSnapshots() {
  if (selectedSnapshots.value.length === 0) return;
  showBatchDeleteSnapshotDialog.value = true;
}

async function executeBatchDeleteSnapshots() {
  if (selectedSnapshots.value.length === 0) return;

  deleting.value = true;
  try {
    await dataAccess.batchDeleteSnapshots(familyId.value, selectedSnapshots.value);
    snapshots.value = snapshots.value.filter((s) => !selectedSnapshots.value.includes(s.id));
    const deletedCount = selectedSnapshots.value.length;
    selectedSnapshots.value = [];
    updateSelectAll();
    showSnackbar(`${deletedCount} snapshot(s) deleted successfully`, "success");
  } catch (error: unknown) {
    const msg = error instanceof Error ? error.message : JSON.stringify(error);
    showSnackbar(`Error deleting snapshots: ${msg}`, "error");
  } finally {
    showBatchDeleteSnapshotDialog.value = false;
    deleting.value = false;
  }
}

function getAccountName(accountId: string) {
  const account = accounts.value.find((a) => a.id === accountId);
  return account ? account.name : "Unknown";
}

function getAccountType(accountId: string) {
  const account = accounts.value.find((a) => a.id === accountId);
  return account ? account.type : "Unknown";
}

function getAccountCategory(accountId: string) {
  const account = accounts.value.find((a) => a.id === accountId);
  return account ? account.category : "Asset";
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

<style scoped>
/* No specific styles needed */
</style>
