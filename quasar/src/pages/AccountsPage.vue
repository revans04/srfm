<!-- src/views/AccountsView.vue -->
<template>
  <q-page fluid>
    <h1>Accounts</h1>

    <!-- Loading handled via $q.loading -->

    <!-- Family Prompt -->
    <q-banner v-if="!familyId" type="warning" class="mb-4"> Please create or join a family to manage accounts. </q-banner>

    <!-- Tabs -->
    <q-tabs v-model="tab" color="primary" :disabled="!familyId">
      <q-tab value="bank">Bank Accounts</q-tab>
      <q-tab value="credit">Credit Cards</q-tab>
      <q-tab value="investment">Investments</q-tab>
      <q-tab value="property">Properties</q-tab>
      <q-tab value="loan">Loans</q-tab>
      <q-tab value="net-worth">Snapshots</q-tab>
    </q-tabs>

    <q-tab-panels v-model="tab">
      <q-tab-panel name="bank">
        <AccountList
          :accounts="bankAccounts"
          :imported-transactions="importedTransactions"
          type="Bank"
          @add="openAccountDialog('Bank')"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </q-tab-panel>
      <q-tab-panel name="credit">
        <AccountList
          :accounts="creditCardAccounts"
          :imported-transactions="importedTransactions"
          type="CreditCard"
          @add="openAccountDialog('CreditCard')"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </q-tab-panel>
      <q-tab-panel name="investment">
        <AccountList
          :accounts="investmentAccounts"
          type="Investment"
          @add="openAccountDialog('Investment')"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </q-tab-panel>
      <q-tab-panel name="property">
        <AccountList :accounts="propertyAccounts" type="Property" @add="openAccountDialog('Property')" @edit="editAccount" @delete="confirmDeleteAccount" />
      </q-tab-panel>
      <q-tab-panel name="loan">
        <AccountList :accounts="loanAccounts" type="Loan" @add="openAccountDialog('Loan')" @edit="editAccount" @delete="confirmDeleteAccount" />
      </q-tab-panel>
      <q-tab-panel name="net-worth">
        <q-card>
          <q-card-section>Net Worth Over Time</q-card-section>
          <q-card-section>
            <q-btn color="primary" class="mb-4 mr-2" @click="openSnapshotDialog" :disabled="accounts.length === 0"> Capture Snapshot </q-btn>
            <q-btn color="error" class="mb-4" @click="confirmBatchDeleteSnapshots" :disabled="selectedSnapshots.length === 0" :loading="deleting">
              Delete Selected
            </q-btn>
            <q-table :headers="snapshotHeaders" :rows="snapshotsWithSelection" class="elevation-1" :items-per-page="10">
              <template v-slot:header.select="{ column }">
                <q-checkbox v-model="selectAll" @update:modelValue="toggleSelectAll" hide-details density="compact" />
              </template>
              <template v-slot:item.select="{ item }">
                <q-checkbox v-model="selectedSnapshots" :value="item.id" hide-details density="compact" @update:modelValue="updateSelectAll" />
              </template>
              <template v-slot:item.date="{ item }">
                {{ formatTimestamp(item.date) }}
              </template>
              <template v-slot:item.netWorth="{ item }">
                {{ formatCurrency(item.netWorth) }}
              </template>
              <template v-slot:item.actions="{ item }">
                <q-btn icon density="compact" variant="plain" color="error" @click="confirmDeleteSnapshot(item.id)">
                    <q-icon name="delete_outline"></q-icon>
                </q-btn>
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
              :headers="[
                { title: 'Account', value: 'name' },
                { title: 'Type', value: 'type' },
                { title: 'Value', value: 'value' },
              ]"
              :rows="newSnapshot.accounts"
              hide-default-footer
              items-per-page="100"
            >
              <template v-slot:item.name="{ item }">
                {{ getAccountName(item.accountId) }}
              </template>
              <template v-slot:item.type="{ item }">
                {{ getAccountType(item.accountId) }}
              </template>
              <template v-slot:item.value="{ item }">
                <q-input
                  v-model.number="item.value"
                  type="number"
                  variant="outlined"
                  density="compact"
                  :prefix="getAccountCategory(item.accountId) === 'Liability' ? '-' : ''"
                ></q-input>
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
import { Account, Snapshot, ImportedTransaction, Transaction } from "../types";
import { formatCurrency, formatTimestamp, todayISO, timestampToMillis } from "../utils/helpers";
import { useQuasar } from 'quasar';
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
const showAccountDialog = ref(false);
const showSnapshotDialog = ref(false);
const showDeleteAccountDialog = ref(false);
const showDeleteSnapshotDialog = ref(false);
const showBatchDeleteSnapshotDialog = ref(false);
const showUpdateBudgetTransactionsDialog = ref(false);
const accountType = ref<"Bank" | "CreditCard" | "Investment" | "Property" | "Loan">("Bank");
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
  type: "Bank",
  category: "Asset",
  createdAt: Timestamp.now(),
  updatedAt: Timestamp.now(),
  details: {
    interestRate: undefined,
    appraisedValue: undefined,
    address: undefined,
    maturityDate: undefined,
  },
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

const bankAccounts = computed(() => accounts.value.filter((a) => a.type === "Bank").sort((a, b) => a.name.localeCompare(b.name)));
const creditCardAccounts = computed(() => accounts.value.filter((a) => a.type === "CreditCard").sort((a, b) => a.name.localeCompare(b.name)));
const investmentAccounts = computed(() => accounts.value.filter((a) => a.type === "Investment").sort((a, b) => a.name.localeCompare(b.name)));
const propertyAccounts = computed(() => accounts.value.filter((a) => a.type === "Property").sort((a, b) => a.name.localeCompare(b.name)));
const loanAccounts = computed(() => accounts.value.filter((a) => a.type === "Loan").sort((a, b) => a.name.localeCompare(b.name)));

const snapshotHeaders = ref([
  { title: "", value: "select", sortable: false },
  { title: "Date", value: "date" },
  { title: "Net Worth", value: "netWorth" },
  { title: "Actions", value: "actions" },
]);

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

onMounted(async () => {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar("Please log in to view accounts", "error");
    return;
  }

  $q.loading.show({
    message: 'Loading data...',
    spinner: 'QSpinner',
    spinnerColor: 'primary',
    spinnerSize: '50px',
    messageClass: 'q-ml-sm',
    boxClass: 'flex items-center justify-center',
  });
  try {
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar("No family found. Please create or join a family.", "error");
      return;
    }
    familyId.value = family.id;
    await loadData();
  } catch (error: any) {
    showSnackbar(`Error loading data: ${error.message}`, "error");
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
  } catch (error: any) {
    console.error("Error loading data:", error);
    showSnackbar(`Error loading data: ${error.message}`, "error");
  }
}

function openAccountDialog(type: "Bank" | "CreditCard" | "Investment" | "Property" | "Loan") {
  accountType.value = type;
  editMode.value = false;
  isPersonalAccount.value = false;
  newAccount.value = {
    id: uuidv4(),
    name: "",
    type,
    category: type === "CreditCard" || type === "Loan" ? "Liability" : "Asset",
    createdAt: Timestamp.now(),
    updatedAt: Timestamp.now(),
    details: {},
  };
  showAccountDialog.value = true;
}

function editAccount(account: Account) {
  accountType.value = account.type;
  editMode.value = true;
  isPersonalAccount.value = !!account.userId;
  newAccount.value = { ...account, details: { ...account.details } };
  originalAccountNumber.value = account.accountNumber;
  originalAccountName.value = account.name;
  showAccountDialog.value = true;
}

async function saveAccount(account: Account, isPersonal: boolean) {
  saving.value = true;
  try {
    account.userId = isPersonal ? userId.value : undefined;

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
            value: account.category === "Liability" && account.balance > 0 ? -account.balance : account.balance || 0,
          },
        ],
        netWorth: account.category === "Liability" && account.balance > 0 ? -account.balance : account.balance || 0,
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
      snapshots.value.sort((a, b) =>
        a && a.date && b && b.date ? timestampToMillis(b.date) - timestampToMillis(a.date) : 1
      );
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
          accountNumber: accountNumberChanged ? account.accountNumber : tx.accountNumber,
          accountSource: accountNameChanged ? account.institution || account.name : tx.accountSource,
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
  } catch (error: any) {
    showSnackbar(`Error saving account: ${error.message}`, "error");
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
          accountNumber: newAccount.value.accountNumber,
          accountSource: newAccount.value.institution || newAccount.value.name,
        },
      }));
      await dataAccess.updateBudgetTransactions(updatedBudgetTxs);
      showSnackbar(`${budgetTxsWithBudgetId.length} budget transactions updated successfully`, "success");
    }
  } catch (error: any) {
    showSnackbar(`Error updating budget transactions: ${error.message}`, "error");
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
    type: "Bank",
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
    accounts.value = accounts.value.filter((a) => a.id !== accountToDelete.value);
    showSnackbar("Account deleted successfully", "success");
  } catch (error: any) {
    showSnackbar(`Error deleting account: ${error.message}`, "error");
  } finally {
    showDeleteAccountDialog.value = false;
    accountToDelete.value = null;
  }
}

function openSnapshotDialog() {
  const a = accounts.value.sort((a, b) => {
    if (a.type == b.type) return a.name.localeCompare(b.name);
    return a.type.localeCompare(b.type);
  });

  newSnapshot.value = {
    date: todayISO(),
    accounts: a.map((a) => ({
      accountId: a.id,
      accountName: a.name,
      type: a.type,
      value: a.balance || 0,
    })),
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
    snapshots.value.sort((a, b) =>
      a && a.date && b && b.date ? timestampToMillis(b.date) - timestampToMillis(a.date) : 1
    );

    showSnackbar("Snapshot saved successfully", "success");
    showSnapshotDialog.value = false;
  } catch (error: any) {
    showSnackbar(`Error saving snapshot: ${error.message}`, "error");
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
  } catch (error: any) {
    showSnackbar(`Error deleting snapshot: ${error.message}`, "error");
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
  } catch (error: any) {
    showSnackbar(`Error deleting snapshots: ${error.message}`, "error");
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
