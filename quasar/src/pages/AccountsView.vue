<!-- src/views/AccountsView.vue -->
<template>
  <q-page padding>
    <h1 class="text-h4 q-mb-md">Accounts</h1>

    <!-- Loading Overlay -->
    <q-inner-loading :showing="loading">
      <q-spinner color="primary" size="50px" />
    </q-inner-loading>

    <!-- Family Prompt -->
    <q-banner v-if="!familyId" dense class="bg-warning text-black q-mb-md">
      Please create or join a family to manage accounts.
    </q-banner>

    <!-- Tabs -->
    <q-tabs
      v-model="tab"
      dense
      class="bg-primary text-white q-mb-md"
      active-color="accent"
      indicator-color="accent"
      :disable="!familyId"
    >
      <q-tab name="bank" label="Bank Accounts" />
      <q-tab name="credit" label="Credit Cards" />
      <q-tab name="investment" label="Investments" />
      <q-tab name="property" label="Properties" />
      <q-tab name="loan" label="Loans" />
      <q-tab name="net-worth" label="Snapshots" />
    </q-tabs>

    <!-- Tab Content -->
    <q-tab-panels v-model="tab" animated>
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
        <AccountList
          :accounts="propertyAccounts"
          type="Property"
          @add="openAccountDialog('Property')"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </q-tab-panel>
      <q-tab-panel name="loan">
        <AccountList
          :accounts="loanAccounts"
          type="Loan"
          @add="openAccountDialog('Loan')"
          @edit="editAccount"
          @delete="confirmDeleteAccount"
        />
      </q-tab-panel>
      <q-tab-panel name="net-worth">
        <q-card>
          <q-card-section>
            <div class="text-h6">Net Worth Over Time</div>
          </q-card-section>
          <q-card-section>
            <q-btn
              color="primary"
              label="Capture Snapshot"
              class="q-mr-sm q-mb-md"
              :disable="accounts.length === 0"
              @click="openSnapshotDialog"
            />
            <q-btn
              color="negative"
              label="Delete Selected"
              class="q-mb-md"
              :disable="selectedSnapshots.length === 0"
              :loading="deleting"
              @click="confirmBatchDeleteSnapshots"
            />
            <q-table
              :columns="snapshotHeaders"
              :rows="snapshotsWithSelection"
              row-key="id"
              :pagination="{ rowsPerPage: 10 }"
              class="shadow-2"
            >
              <template v-slot:header-cell-select>
                <q-th>
                  <q-checkbox v-model="selectAll" dense @update:model-value="toggleSelectAll" />
                </q-th>
              </template>
              <template v-slot:body-cell-select="props">
                <q-td>
                  <q-checkbox
                    v-model="selectedSnapshots"
                    :value="props.row.id"
                    dense
                    @update:model-value="updateSelectAll"
                  />
                </q-td>
              </template>
              <template v-slot:body-cell-date="props">
                <q-td>{{ formatTimestamp(props.row.date) }}</q-td>
              </template>
              <template v-slot:body-cell-netWorth="props">
                <q-td>{{ formatCurrency(props.row.netWorth) }}</q-td>
              </template>
              <template v-slot:body-cell-actions="props">
                <q-td>
                  <q-btn
                    icon="mdi-trash-can-outline"
                    color="negative"
                    flat
                    dense
                    @click="confirmDeleteSnapshot(props.row.id)"
                  />
                </q-td>
              </template>
            </q-table>
            <div class="q-mt-md">
              <p>Net worth trend chart coming soon!</p>
            </div>
          </q-card-section>
        </q-card>
      </q-tab-panel>
    </q-tab-panels>

    <!-- Account Dialog -->
    <q-dialog v-model="showAccountDialog" :maximized="isMobile" persistent>
      <q-card :style="{ width: isMobile ? '100%' : '600px', maxWidth: '600px' }">
        <q-card-section class="bg-primary text-white">
          <div class="text-h6">{{ editMode ? 'Edit' : 'Add' }} {{ accountType }} Account</div>
        </q-card-section>
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
    <q-dialog v-model="showSnapshotDialog" :maximized="isMobile" persistent>
      <q-card :style="{ width: isMobile ? '100%' : '800px', maxWidth: '800px' }">
        <q-card-section class="bg-primary text-white">
          <div class="text-h6">Capture Net Worth Snapshot</div>
        </q-card-section>
        <q-card-section>
          <q-form @submit="saveSnapshot">
            <q-input
              v-model="newSnapshot.date"
              label="Snapshot Date"
              type="date"
              outlined
              dense
              required
              class="q-mb-md"
            />
            <q-table
              :columns="[
                { name: 'name', label: 'Account', field: 'name', align: 'left' },
                { name: 'type', label: 'Type', field: 'type', align: 'left' },
                { name: 'value', label: 'Value', field: 'value', align: 'left' },
              ]"
              :rows="newSnapshot.accounts"
              row-key="accountId"
              hide-pagination
            >
              <template v-slot:body-cell-name="props">
                <q-td>{{ getAccountName(props.row.accountId) }}</q-td>
              </template>
              <template v-slot:body-cell-type="props">
                <q-td>{{ getAccountType(props.row.accountId) }}</q-td>
              </template>
              <template v-slot:body-cell-value="props">
                <q-td>
                  <q-input
                    v-model.number="props.row.value"
                    type="number"
                    outlined
                    dense
                    :prefix="getAccountCategory(props.row.accountId) === 'Liability' ? '-' : ''"
                  />
                </q-td>
              </template>
            </q-table>
            <div class="q-mt-md">
              <q-btn type="submit" color="primary" label="Save Snapshot" :loading="saving" />
              <q-btn
                flat
                color="grey"
                label="Cancel"
                class="q-ml-sm"
                @click="showSnapshotDialog = false"
              />
            </div>
          </q-form>
        </q-card-section>
      </q-card>
    </q-dialog>

    <!-- Delete Account Confirmation Dialog -->
    <q-dialog v-model="showDeleteAccountDialog" persistent>
      <q-card style="width: 400px; max-width: 400px">
        <q-card-section class="bg-negative text-white">
          <div class="text-h6">Delete Account</div>
        </q-card-section>
        <q-card-section> Are you sure you want to delete this account? </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" @click="showDeleteAccountDialog = false" />
          <q-btn flat label="Delete" color="negative" @click="executeDeleteAccount" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Delete Snapshot Confirmation Dialog -->
    <q-dialog v-model="showDeleteSnapshotDialog" persistent>
      <q-card style="width: 400px; max-width: 400px">
        <q-card-section class="bg-negative text-white">
          <div class="text-h6">Delete Snapshot</div>
        </q-card-section>
        <q-card-section> Are you sure you want to delete this snapshot? </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" @click="showDeleteSnapshotDialog = false" />
          <q-btn flat label="Delete" color="negative" @click="executeDeleteSnapshot" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Batch Delete Snapshots Confirmation Dialog -->
    <q-dialog v-model="showBatchDeleteSnapshotDialog" persistent>
      <q-card style="width: 400px; max-width: 400px">
        <q-card-section class="bg-negative text-white">
          <div class="text-h6">Delete Selected Snapshots</div>
        </q-card-section>
        <q-card-section>
          Are you sure you want to delete {{ selectedSnapshots.length }} selected snapshot(s)?
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" color="grey" @click="showBatchDeleteSnapshotDialog = false" />
          <q-btn flat label="Delete" color="negative" @click="executeBatchDeleteSnapshots" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Update Budget Transactions Confirmation Dialog -->
    <q-dialog v-model="showUpdateBudgetTransactionsDialog" persistent>
      <q-card style="width: 500px; max-width: 500px">
        <q-card-section class="bg-primary text-white">
          <div class="text-h6">Update Budget Transactions</div>
        </q-card-section>
        <q-card-section>
          The account name or number has changed. Would you like to update associated budget
          transactions that were matched to imported transactions? <br />
          <strong>{{ affectedImportedTransactionCount }}</strong> imported transactions will be
          updated. <br />
          <strong>{{ affectedBudgetTransactionCount }}</strong> budget transactions may be affected.
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="No" color="grey" @click="showUpdateBudgetTransactionsDialog = false" />
          <q-btn flat label="Yes" color="primary" @click="confirmUpdateBudgetTransactions" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Snackbar -->
    <q-notification
      v-model="snackbar"
      :color="snackbarColor"
      position="top"
      :timeout="3000"
      actions
      action-label="Close"
      @action="snackbar = false"
    >
      {{ snackbarText }}
    </q-notification>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { auth } from '../firebase';
import { dataAccess } from '../dataAccess';
import { useFamilyStore } from '../store/family';
import AccountList from '../components/AccountList.vue';
import AccountForm from '../components/AccountForm.vue';
import { Account, Snapshot, ImportedTransaction, Transaction, AccountType } from '../types';
import { formatCurrency, formatTimestamp, todayISO } from '../utils/helpers';
import { v4 as uuidv4 } from 'uuid';
import { Timestamp } from 'firebase/firestore';
import { useQuasar } from 'quasar';

const $q = useQuasar();
const familyStore = useFamilyStore();

const tab = ref('bank');
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
const showUpdateBudgetTransactionsDialog = ref(false);
const accountType = ref<AccountType>(AccountType.Bank);
const editMode = ref(false);
const isPersonalAccount = ref(false);
const selectAll = ref(false);
const selectedSnapshots = ref<string[]>([]);
const originalAccountNumber = ref<string | undefined>('');
const originalAccountName = ref<string>('');
const affectedImportedTransactionCount = ref(0);
const affectedBudgetTransactionCount = ref(0);
const newAccount = ref<Account>({
  id: '',
  name: '',
  type: 'Bank',
  category: 'Asset',
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
const snackbarText = ref('');
const snackbarColor = ref('success');

const userId = computed(() => auth.currentUser?.uid || '');
const familyId = ref<string>('');
const isMobile = computed(() => $q.screen.lt.md);

const bankAccounts = computed(() =>
  accounts.value.filter((a) => a.type === 'Bank').sort((a, b) => a.name.localeCompare(b.name)),
);
const creditCardAccounts = computed(() =>
  accounts.value
    .filter((a) => a.type === 'CreditCard')
    .sort((a, b) => a.name.localeCompare(b.name)),
);
const investmentAccounts = computed(() =>
  accounts.value
    .filter((a) => a.type === 'Investment')
    .sort((a, b) => a.name.localeCompare(b.name)),
);
const propertyAccounts = computed(() =>
  accounts.value.filter((a) => a.type === 'Property').sort((a, b) => a.name.localeCompare(b.name)),
);
const loanAccounts = computed(() =>
  accounts.value.filter((a) => a.type === 'Loan').sort((a, b) => a.name.localeCompare(b.name)),
);

const snapshotHeaders = ref([
  { name: 'select', label: '', field: 'select', align: 'center', sortable: false },
  { name: 'date', label: 'Date', field: 'date', align: 'left', sortable: true },
  { name: 'netWorth', label: 'Net Worth', field: 'netWorth', align: 'left', sortable: true },
  { name: 'actions', label: 'Actions', field: 'actions', align: 'center', sortable: false },
]);

const snapshotsWithSelection = computed(() => {
  return snapshots.value.map((snapshot) => ({
    ...snapshot,
    selected: selectedSnapshots.value.includes(snapshot.id),
  }));
});

function updateSelectAll() {
  selectAll.value =
    snapshots.value.length > 0 && selectedSnapshots.value.length === snapshots.value.length;
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
    showSnackbar('Please log in to view accounts', 'negative');
    return;
  }

  loading.value = true;
  try {
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found. Please create or join a family.', 'negative');
      return;
    }
    familyId.value = family.id;
    await loadData();
  } catch (error: any) {
    showSnackbar(`Error loading data: ${error.message}`, 'negative');
    console.error('Error during onMounted:', error);
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
    console.error('Error loading data:', error);
    showSnackbar(`Error loading data: ${error.message}`, 'negative');
  }
}

function openAccountDialog(type: 'Bank' | 'CreditCard' | 'Investment' | 'Property' | 'Loan') {
  accountType.value = type;
  editMode.value = false;
  isPersonalAccount.value = false;
  newAccount.value = {
    id: uuidv4(),
    name: '',
    type,
    category: type === 'CreditCard' || type === 'Loan' ? 'Liability' : 'Asset',
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

    let accountNumberChanged = false;
    let accountNameChanged = false;
    if (editMode.value) {
      accountNumberChanged = originalAccountNumber.value !== account.accountNumber;
      accountNameChanged = originalAccountName.value !== account.name;
    }

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
            value:
              account.category === 'Liability' && (account.balance || 0) > 0
                ? -(account.balance || 0)
                : account.balance || 0,
          },
        ],
        netWorth:
          account.category === 'Liability' && (account.balance || 0) > 0
            ? -(account.balance || 0)
            : account.balance || 0,
        createdAt: Timestamp.now(),
      };
      await dataAccess.saveSnapshot(familyId.value, snapshot);
    }

    const index = accounts.value.findIndex((a) => a.id === account.id);
    if (index >= 0) {
      accounts.value[index] = { ...account };
    } else {
      accounts.value.push({ ...account });
    }

    if (!editMode.value && snapshot) {
      snapshots.value.push(snapshot);
      snapshots.value.sort((a, b) =>
        a && a.date && b && b.date ? b.date.toMillis() - a.date.toMillis() : 1,
      );
    }

    if (editMode.value && (accountNumberChanged || accountNameChanged)) {
      const importedTxs = await dataAccess.getImportedTransactionsByAccountId(account.id);
      affectedImportedTransactionCount.value = importedTxs.length;

      if (affectedImportedTransactionCount.value > 0) {
        const updatedImportedTxs = importedTxs.map((tx) => ({
          ...tx,
          accountNumber: accountNumberChanged ? account.accountNumber : tx.accountNumber,
          accountSource: accountNameChanged
            ? account.institution || account.name
            : tx.accountSource,
        }));
        await dataAccess.updateImportedTransactions(updatedImportedTxs);

        updatedImportedTxs.forEach((updatedTx) => {
          const idx = importedTransactions.value.findIndex((it) => it.id === updatedTx.id);
          if (idx >= 0) {
            importedTransactions.value[idx] = updatedTx;
          }
        });

        const budgetTxsWithBudgetId = await dataAccess.getBudgetTransactionsMatchedToImported(
          account.id,
        );
        affectedBudgetTransactionCount.value = budgetTxsWithBudgetId.length;

        if (affectedBudgetTransactionCount.value > 0) {
          showUpdateBudgetTransactionsDialog.value = true;
          return;
        }
      }
    }

    showSnackbar(`${accountType.value} account saved successfully`, 'positive');
    closeAccountDialog();
  } catch (error: any) {
    showSnackbar(`Error saving account: ${error.message}`, 'negative');
    console.error('Error saving account:', error);
  } finally {
    saving.value = false;
  }
}

async function confirmUpdateBudgetTransactions() {
  try {
    const budgetTxsWithBudgetId = await dataAccess.getBudgetTransactionsMatchedToImported(
      newAccount.value.id,
    );
    if (budgetTxsWithBudgetId.length > 0) {
      const updatedBudgetTxs = budgetTxsWithBudgetId.map((item) => ({
        budgetId: item.budgetId,
        transaction: {
          ...item.transaction,
          accountNumber: newAccount.value.accountNumber,
          accountSource: newAccount.value.institution || newAccount.value.name,
        },
      }));
      await dataAccess.updateBudgetTransactions(updatedBudgetTxs);
      showSnackbar(
        `${budgetTxsWithBudgetId.length} budget transactions updated successfully`,
        'positive',
      );
    }
  } catch (error: any) {
    showSnackbar(`Error updating budget transactions: ${error.message}`, 'negative');
    console.error('Error updating budget transactions:', error);
  } finally {
    showUpdateBudgetTransactionsDialog.value = false;
    closeAccountDialog();
  }
}

function closeAccountDialog() {
  showAccountDialog.value = false;
  originalAccountNumber.value = '';
  originalAccountName.value = '';
  affectedImportedTransactionCount.value = 0;
  affectedBudgetTransactionCount.value = 0;
  newAccount.value = {
    id: '',
    name: '',
    type: 'Bank',
    category: 'Asset',
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
    showSnackbar('Account deleted successfully', 'positive');
  } catch (error: any) {
    showSnackbar(`Error deleting account: ${error.message}`, 'negative');
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
    showSnackbar('Snapshot date is required', 'negative');
    return;
  }

  saving.value = true;
  try {
    const snapshot: Snapshot = {
      id: uuidv4(),
      date: Timestamp.fromDate(new Date(newSnapshot.value.date)),
      accounts: newSnapshot.value.accounts,
      netWorth: newSnapshot.value.accounts.reduce(
        (sum, a) =>
          sum +
          (getAccountCategory(a.accountId) === 'Liability' && a.value > 0 ? -a.value : a.value),
        0,
      ),
      createdAt: Timestamp.now(),
    };

    await dataAccess.saveSnapshot(familyId.value, snapshot);
    snapshots.value.push(snapshot);
    snapshots.value.sort((a, b) =>
      a && a.date && b && b.date ? b.date.toMillis() - a.date.toMillis() : 1,
    );

    showSnackbar('Snapshot saved successfully', 'positive');
    showSnapshotDialog.value = false;
  } catch (error: any) {
    showSnackbar(`Error saving snapshot: ${error.message}`, 'negative');
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
    showSnackbar('Snapshot deleted successfully', 'positive');
  } catch (error: any) {
    showSnackbar(`Error deleting snapshot: ${error.message}`, 'negative');
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
    showSnackbar(`${deletedCount} snapshot(s) deleted successfully`, 'positive');
  } catch (error: any) {
    showSnackbar(`Error deleting snapshots: ${error.message}`, 'negative');
  } finally {
    showBatchDeleteSnapshotDialog.value = false;
    deleting.value = false;
  }
}

function getAccountName(accountId: string) {
  const account = accounts.value.find((a) => a.id === accountId);
  return account ? account.name : 'Unknown';
}

function getAccountType(accountId: string) {
  const account = accounts.value.find((a) => a.id === accountId);
  return account ? account.type : 'Unknown';
}

function getAccountCategory(accountId: string) {
  const account = accounts.value.find((a) => a.id === accountId);
  return account ? account.category : 'Asset';
}

function showSnackbar(text: string, color = 'positive') {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
}
</script>

<style scoped>
/* Adjust styles for Quasar components */
.q-page {
  background-color: var(--q-light);
}
</style>
