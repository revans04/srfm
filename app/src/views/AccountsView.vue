<!-- src/views/AccountsView.vue -->
<template>
  <v-container fluid>
    <h1>Accounts</h1>

    <!-- Loading Overlay -->
    <v-overlay :model-value="loading" class="align-center justify-center" scrim="#00000080">
      <v-progress-circular indeterminate color="primary" size="50" />
    </v-overlay>

    <!-- Family Prompt -->
    <v-alert v-if="!familyId" type="warning" class="mb-4"> Please create or join a family to manage accounts. </v-alert>

    <!-- Tabs -->
    <v-tabs v-model="tab" color="primary" :disabled="!familyId">
      <v-tab value="bank">Bank Accounts</v-tab>
      <v-tab value="credit">Credit Cards</v-tab>
      <v-tab value="investment">Investments</v-tab>
      <v-tab value="property">Properties</v-tab>
      <v-tab value="loan">Loans</v-tab>
      <v-tab value="net-worth">Snapshots</v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <v-window-item value="bank">
        <AccountList
          :accounts="bankAccounts"
          :imported-transactions="importedTransactions"
          type="Bank"
          @add="openAccountDialog('Bank')"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </v-window-item>
      <v-window-item value="credit">
        <AccountList
          :accounts="creditCardAccounts"
          :imported-transactions="importedTransactions"
          type="CreditCard"
          @add="openAccountDialog('CreditCard')"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </v-window-item>
      <v-window-item value="investment">
        <AccountList
          :accounts="investmentAccounts"
          type="Investment"
          @add="openAccountDialog('Investment')"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </v-window-item>
      <v-window-item value="property">
        <AccountList :accounts="propertyAccounts" type="Property" @add="openAccountDialog('Property')" @edit="editAccount" @delete="confirmDeleteAccount" />
      </v-window-item>
      <v-window-item value="loan">
        <AccountList :accounts="loanAccounts" type="Loan" @add="openAccountDialog('Loan')" @edit="editAccount" @delete="confirmDeleteAccount" />
      </v-window-item>
      <v-window-item value="net-worth">
        <v-card>
          <v-card-title>Net Worth Over Time</v-card-title>
          <v-card-text>
            <v-btn color="primary" class="mb-4 mr-2" @click="openSnapshotDialog" :disabled="accounts.length === 0"> Capture Snapshot </v-btn>
            <v-btn color="error" class="mb-4" @click="confirmBatchDeleteSnapshots" :disabled="selectedSnapshots.length === 0" :loading="deleting">
              Delete Selected
            </v-btn>
            <v-data-table
              :headers="snapshotHeaders"
              :items="snapshotsWithSelection"
              class="elevation-1"
              :items-per-page="10"
              @click:row="viewSnapshotDetails"
            >
              <template v-slot:header.select="{ column }">
                <v-checkbox
                  v-model="selectAll"
                  @update:modelValue="toggleSelectAll"
                  hide-details
                  density="compact"
                  @click.stop
                />
              </template>
              <template v-slot:item.select="{ item }">
                <v-checkbox
                  v-model="selectedSnapshots"
                  :value="item.id"
                  hide-details
                  density="compact"
                  @update:modelValue="updateSelectAll"
                  @click.stop
                />
              </template>
              <template v-slot:item.date="{ item }">
                {{ formatTimestamp(item.date) }}
              </template>
              <template v-slot:item.netWorth="{ item }">
                {{ formatCurrency(item.netWorth) }}
              </template>
              <template v-slot:item.actions="{ item }">
                <v-btn
                  icon
                  density="compact"
                  variant="plain"
                  color="error"
                  @click.stop="confirmDeleteSnapshot(item.id)"
                >
                  <v-icon>mdi-trash-can-outline</v-icon>
                </v-btn>
              </template>
            </v-data-table>
            <div class="mt-4">
              <p>Net worth trend chart coming soon!</p>
            </div>
          </v-card-text>
        </v-card>
      </v-window-item>
    </v-window>

    <!-- Account Dialog -->
    <v-dialog v-model="showAccountDialog" max-width="600px">
      <v-card>
        <v-card-title>{{ editMode ? "Edit" : "Add" }} {{ accountType }} Account</v-card-title>
        <v-card-text>
          <AccountForm
            :account-type="accountType"
            :account="editMode ? newAccount : undefined"
            :show-personal-option="true"
            @save="saveAccount"
            @cancel="closeAccountDialog"
          />
        </v-card-text>
      </v-card>
    </v-dialog>

    <!-- Snapshot Dialog -->
    <v-dialog v-model="showSnapshotDialog" max-width="800px">
      <v-card>
        <v-card-title>Capture Net Worth Snapshot</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="saveSnapshot">
            <v-text-field v-model="newSnapshot.date" label="Snapshot Date" type="date" variant="outlined" density="compact" required></v-text-field>
            <v-data-table
              :headers="[
                { title: 'Account', value: 'name' },
                { title: 'Type', value: 'type' },
                { title: 'Value', value: 'value' },
              ]"
              :items="newSnapshot.accounts"
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
                <v-text-field
                  v-model.number="item.value"
                  type="number"
                  variant="outlined"
                  density="compact"
                  :prefix="getAccountCategory(item.accountId) === 'Liability' ? '-' : ''"
                ></v-text-field>
              </template>
            </v-data-table>
            <v-btn type="submit" color="primary" :loading="saving" class="mt-4"> Save Snapshot </v-btn>
            <v-btn color="grey" variant="text" @click="showSnapshotDialog = false" class="ml-2"> Cancel </v-btn>
          </v-form>
        </v-card-text>
      </v-card>
    </v-dialog>

    <!-- Delete Account Confirmation Dialog -->
    <v-dialog v-model="showDeleteAccountDialog" max-width="400">
      <v-card>
        <v-card-title class="bg-error py-3">
          <span class="text-white">Delete Account</span>
        </v-card-title>
        <v-card-text class="pt-4"> Are you sure you want to delete this account? </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showDeleteAccountDialog = false"> Cancel </v-btn>
          <v-btn color="error" variant="flat" @click="executeDeleteAccount"> Delete </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Snapshot Confirmation Dialog -->
    <v-dialog v-model="showDeleteSnapshotDialog" max-width="400">
      <v-card>
        <v-card-title class="bg-error py-3">
          <span class="text-white">Delete Snapshot</span>
        </v-card-title>
        <v-card-text class="pt-4"> Are you sure you want to delete this snapshot? </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showDeleteSnapshotDialog = false"> Cancel </v-btn>
          <v-btn color="error" variant="flat" @click="executeDeleteSnapshot"> Delete </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Batch Delete Snapshots Confirmation Dialog -->
    <v-dialog v-model="showBatchDeleteSnapshotDialog" max-width="400">
      <v-card>
        <v-card-title class="bg-error py-3">
          <span class="text-white">Delete Selected Snapshots</span>
        </v-card-title>
        <v-card-text class="pt-4"> Are you sure you want to delete {{ selectedSnapshots.length }} selected snapshot(s)? </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showBatchDeleteSnapshotDialog = false"> Cancel </v-btn>
          <v-btn color="error" variant="flat" @click="executeBatchDeleteSnapshots"> Delete </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Snapshot Details Dialog -->
    <v-dialog v-model="showSnapshotDetailsDialog" max-width="600px">
      <v-card>
        <v-card-title>
          Snapshot on {{ snapshotDetails ? formatTimestamp(snapshotDetails.date) : '' }}
        </v-card-title>
        <v-card-text>
          <v-data-table
            :headers="[
              { title: 'Account', value: 'accountName' },
              { title: 'Type', value: 'type' },
              { title: 'Value', value: 'value' }
            ]"
            :items="snapshotDetails ? snapshotDetails.accounts : []"
            hide-default-footer
            items-per-page="0"
            height="575px"
          >
            <template v-slot:item.value="{ item }">
              {{ formatCurrency(item.value) }}
            </template>
          </v-data-table>
          <div class="text-right mt-4">
            <strong>Net Worth: {{ snapshotDetails ? formatCurrency(snapshotDetails.netWorth) : '' }}</strong>
          </div>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn color="primary" variant="text" @click="showSnapshotDetailsDialog = false">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Update Budget Transactions Confirmation Dialog -->
    <v-dialog v-model="showUpdateBudgetTransactionsDialog" max-width="500px">
      <v-card>
        <v-card-title>Update Budget Transactions</v-card-title>
        <v-card-text>
          The account name or number has changed. Would you like to update associated budget transactions that were matched to imported transactions? <br />
          <strong>{{ affectedImportedTransactionCount }}</strong> imported transactions will be updated. <br />
          <strong>{{ affectedBudgetTransactionCount }}</strong> budget transactions may be affected.
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showUpdateBudgetTransactionsDialog = false"> No </v-btn>
          <v-btn color="primary" @click="confirmUpdateBudgetTransactions"> Yes </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar" :color="snackbarColor" timeout="3000">
      {{ snackbarText }}
      <template v-slot:actions>
        <v-btn variant="text" @click="snackbar = false">Close</v-btn>
      </template>
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { auth } from "../firebase";
import { dataAccess } from "../dataAccess";
import { useFamilyStore } from "../store/family";
import AccountList from "../components/AccountList.vue";
import AccountForm from "../components/AccountForm.vue"; // Import AccountForm directly
import { Account, Snapshot, ImportedTransaction, Transaction } from "../types";
import { formatCurrency, formatTimestamp, todayISO, timestampToMillis } from "../utils/helpers";
import { v4 as uuidv4 } from "uuid";
import { Timestamp } from "firebase/firestore";

const familyStore = useFamilyStore();

const tab = ref("bank");
const loading = ref(false);
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
const showSnapshotDetailsDialog = ref(false);
const showUpdateBudgetTransactionsDialog = ref(false);
const snapshotDetails = ref<Snapshot | null>(null);
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
const snackbar = ref(false);
const snackbarText = ref("");
const snackbarColor = ref("success");

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

  loading.value = true;
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
    loading.value = false;
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

function viewSnapshotDetails(event: any, snapshotRow: any) {
  snapshotDetails.value = snapshotRow.item as Snapshot;
  console.log(snapshotRow.item as Snapshot);
  showSnapshotDetailsDialog.value = true;
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
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
}
</script>

<style scoped>
/* No specific styles needed */
</style>
