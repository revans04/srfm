<!-- src/views/SettingsView.vue -->
<template>
  <q-page>
    <h1>Settings</h1>

    <q-banner v-if="userEmail && !emailVerified" type="warning" class="mb-4">
      Your email ({{ userEmail }}) is not verified. Please check your inbox or resend the verification email.
      <template v-slot:actions>
        <q-btn variant="plain" @click="sendVerificationEmail" :loading="resending">Resend</q-btn>
      </template>
    </q-banner>

    <q-tabs v-model="activeTab" color="primary" class="mt-4">
      <q-tab value="group">Manage Family/Group</q-tab>
      <q-tab value="entity">Manage Entities</q-tab>
      <q-tab value="manageTransactions">Manage Imports</q-tab>
      <q-tab value="manageBudgets">Manage Budgets</q-tab>
    </q-tabs>

    <q-tab-panels v-model="activeTab">
      <!-- Group Management Tab -->
      <q-tab-panel name="group">
        <q-card class="mt-4">
          <q-card-section>Family/Group Information</q-card-section>
          <q-card-section>
            <q-list>
              <q-item v-for="member in acceptedMembers" :key="member.uid">
                {{ member.email }} ({{ member.role }}) - Last Accessed: {{ formatDate(member.lastAccessed) || "Never" }}
                <q-btn v-if="member.uid !== user.uid" @click="removeMember(member.uid)">Remove</q-btn>
              </q-item>
              <q-item v-for="invite in pendingInvites" :key="invite.token">
                {{ invite.inviteeEmail }} (Pending) - Invited: {{ formatDate(invite.createdAt) }}
              </q-item>
            </q-list>
            <q-form @submit.prevent="inviteMember">
              <q-input v-model="inviteEmail" label="Invite Email" type="email" required></q-input>
              <q-btn type="submit" :loading="inviting">Invite</q-btn>
            </q-form>
          </q-card-section>
        </q-card>
      </q-tab-panel>

      <!-- Entity Management Tab -->
      <q-tab-panel name="entity">
        <q-card class="mt-4">
          <q-card-section>Entities</q-card-section>
          <q-card-section>
            <q-btn color="primary" @click="openCreateEntityDialog" class="mb-4">Add Entity</q-btn>
            <q-list>
              <q-item v-for="entity in entities" :key="entity.id">
                <div class="row dense">
                  <div class="col">
                    {{ entity.name }} ({{ entity.type }}) - Owner: {{ entity.members.find((m) => m.role === "Admin")?.email || "N/A" }}
                    <q-chip v-if="entity.templateBudget" color="success" size="small" class="ml-2">Has Template</q-chip>
                  </div>
                  <div class="col col-auto">
                    <q-btn variant="plain" color="primary" icon @click="openEditEntityDialog(entity)">
                        <q-icon name="edit"></q-icon>
                    </q-btn>
                  </div>
                  <div class="col col-auto">
                    <q-btn variant="plain" icon @click="confirmDeleteEntity(entity)" color="error">
                        <q-icon name="delete_outline"></q-icon>
                    </q-btn>
                  </div>
                </div>
              </q-item>
              <q-item v-if="!entities.length"> No entities found. Create an entity to start managing budgets. </q-item>
            </q-list>
          </q-card-section>
        </q-card>
      </q-tab-panel>

      <!-- Manage Imported Transactions Tab -->
      <q-tab-panel name="manageTransactions">
        <div class="row">
          <div class="col col-12">
            <q-card>
              <q-card-section>Imported Transaction</q-card-section>
              <q-card-section>
                <q-table
                  :columns="transactionDocColumns"
                  :rows="importedTransactionDocs"
                  row-key="id"
                  :items-per-page="10"
                  class="elevation-1"
                  @click:row="viewTransactionDoc"
                >
                  <template v-slot:item.createdAt="{ item }">
                    {{ getDateRange(item) }}
                  </template>
                  <template v-slot:item.account="{ item }">
                    {{ getAccountInfo(item) }}
                  </template>
                  <template v-slot:item.actions="{ item }">
                    <q-btn
                      icon
                      density="compact"
                      variant="plain"
                      color="error"
                      @click.stop="confirmDeleteTransactionDoc(item)"
                      title="Delete Transaction Document"
                    >
                        <q-icon name="delete_outline"></q-icon>
                    </q-btn>
                  </template>
                </q-table>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </q-tab-panel>

      <!-- Manage Budgets Tab -->
      <q-tab-panel name="manageBudgets">
        <div class="row">
          <div class="col col-12">
            <q-card>
              <q-card-section>Monthly Budgets</q-card-section>
              <q-card-section>
                <q-table :headers="budgetHeaders" :rows="budgets" :items-per-page="10" class="elevation-1">
                  <template v-slot:item.entityName="{ item }">
                    {{ getEntityName(item.entityId) }}
                  </template>
                  <template v-slot:item.transactionCount="{ item }">
                    {{ item.transactions?.length || 0 }}
                  </template>
                  <template v-slot:item.actions="{ item }">
                    <q-btn icon density="compact" variant="plain" color="error" @click.stop="confirmDeleteBudget(item)" title="Delete Budget">
                        <q-icon name="delete_outline"></q-icon>
                    </q-btn>
                  </template>
                </q-table>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </q-tab-panel>
    </q-tab-panels>

    <!-- Transaction Document Details Dialog -->
    <q-dialog v-model="showTransactionDocDialog" max-width="1000px" @hide="closeTransactionDocDialog">
      <q-card style="min-width: 600px">
        <q-card-section class="bg-primary py-3">
          <span class="text-white">Imported Transactions</span>
        </q-card-section>
        <q-card-section>
          <q-table
            :headers="importedTransactionHeaders"
            :rows="selectedTransactionDoc?.importedTransactions || []"
            :items-per-page="10"
            class="elevation-1"
          >
            <template v-slot:item.accountId="{ item }">
              {{ item.accountId }}
            </template>
          </q-table>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Close" color="primary" @click="closeTransactionDocDialog" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Delete Transaction Doc Confirmation Dialog -->
    <q-dialog v-model="showDeleteDialog" max-width="400" @keyup.enter="deleteTransactionDoc">
      <q-card>
        <q-card-section class="bg-error py-3">
          <span class="text-white">Delete Transaction Document</span>
        </q-card-section>
        <q-card-section class="pt-4">
          Are you sure you want to delete the transaction document with ID "{{ transactionDocToDelete?.id }}" containing
          {{ transactionDocToDelete?.importedTransactions.length }} transactions? This action cannot be undone.
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteDialog = false">Cancel</q-btn>
          <q-btn color="error" variant="flat" @click="deleteTransactionDoc">Delete</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Delete Budget Confirmation Dialog -->
    <q-dialog v-model="showDeleteBudgetDialog" max-width="400" @keyup.enter="deleteBudget">
      <q-card>
        <q-card-section class="bg-error py-3">
          <span class="text-white">Delete Budget</span>
        </q-card-section>
        <q-card-section class="pt-4">
          Are you sure you want to delete the budget for "{{ budgetToDelete?.month }}" (ID: {{ budgetToDelete?.budgetId }}) containing
          {{ budgetToDelete?.transactions?.length || 0 }} transactions? This action cannot be undone.
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteBudgetDialog = false">Cancel</q-btn>
          <q-btn color="error" variant="flat" @click="deleteBudget">Delete</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Delete Entity Confirmation Dialog -->
    <q-dialog v-model="showDeleteEntityDialog" max-width="400" @keyup.enter="deleteEntity">
      <q-card>
        <q-card-section class="bg-error py-3">
          <span class="text-white">Delete Entity</span>
        </q-card-section>
        <q-card-section class="pt-4">
          Are you sure you want to delete the entity "{{ entityToDelete?.name }}" (ID: {{ entityToDelete?.id }})?
          <span v-if="associatedBudgets.length > 0"> This entity has {{ associatedBudgets.length }} associated budget(s), which must be deleted first. </span>
          <span v-else>This action cannot be undone.</span>
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteEntityDialog = false">Cancel</q-btn>
          <q-btn color="error" variant="flat" @click="deleteEntity" :disabled="associatedBudgets.length > 0">Delete</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Entity Form Dialog -->
    <q-dialog v-model="showEntityDialog" max-width="1000px" persistent>
      <entity-form
        v-if="selectedEntity"
        :key="selectedEntity.id"
        ref="entityFormRef"
        :entity-id="selectedEntity.id"
        @cancel="closeEntityForm"
        @save="handleEntitySave"
        @update:unsaved="closeEntityForm"
      />
    </q-dialog>

  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from "vue";
import { auth } from "../firebase/init";
import { dataAccess } from "../dataAccess";
import { Timestamp } from "firebase/firestore";
import { Family, PendingInvite, Entity, Budget, ImportedTransactionDoc } from "../types";
import { useFamilyStore } from "../store/family";
import EntityForm from "../components/EntityForm.vue";
import { timestampToDate } from "../utils/helpers";

const familyStore = useFamilyStore();
const inviteEmail = ref("");
const inviting = ref(false);
const resending = ref(false);
const userEmail = ref<string | null>(null);
const emailVerified = ref(false);
const user = ref(auth.currentUser);
const family = ref<Family | null>(null);
const acceptedMembers = ref<Array<{ uid: string; email: string; role: string; lastAccessed?: Timestamp }>>([]);
const pendingInvites = ref<PendingInvite[]>([]);
const selectedEntity = ref<Entity | null>(null);
const activeTab = ref("group");
const budgets = ref<Budget[]>([]);
const importedTransactionDocs = ref<ImportedTransactionDoc[]>([]);
const showDeleteDialog = ref(false);
const transactionDocToDelete = ref<ImportedTransactionDoc | null>(null);
const showDeleteBudgetDialog = ref(false);
const budgetToDelete = ref<Budget | null>(null);
const showDeleteEntityDialog = ref(false);
const showEntityDialog = ref(false);
const entityToDelete = ref<Entity | null>(null);
const associatedBudgets = ref<Budget[]>([]);

const showTransactionDocDialog = ref(false);
const selectedTransactionDoc = ref<ImportedTransactionDoc | null>(null);

const entities = computed(() => family.value?.entities || []);

const transactionDocColumns = [
  { name: 'id', label: 'Document ID', field: 'id', sortable: true, align: 'left' },
  {
    name: 'transactionCount',
    label: 'Transaction Count',
    field: (row: ImportedTransactionDoc) => row.importedTransactions.length,
    sortable: true,
    align: 'left'
  },
  {
    name: 'account',
    label: 'Account Info',
    field: (row: ImportedTransactionDoc) => getAccountInfo(row),
    sortable: true,
    align: 'left'
  },
  {
    name: 'createdAt',
    label: 'Created At',
    field: (row: ImportedTransactionDoc) => getDateRange(row),
    sortable: true,
    align: 'left'
  },
  { name: 'actions', label: 'Actions', field: 'actions', sortable: false, align: 'right' }
];

const transactionDocHeaders = [
  { title: "Document ID", value: "id", sortable: true },
  { title: "Transaction Count", value: "importedTransactions.length", sortable: true },
  { title: "Account Info", value: "account", sortable: true },
  { title: "Created At", value: "createdAt", sortable: true },
  { title: "Actions", value: "actions", sortable: false },
];

const importedTransactionHeaders = [
  { title: 'Posted Date', value: 'postedDate' },
  { title: 'Payee', value: 'payee' },
  { title: 'Credit Amount', value: 'creditAmount' },
  { title: 'Debit Amount', value: 'debitAmount' },
  { title: 'Account', value: 'accountId' },
  { title: 'Account Source', value: 'accountSource' },
  { title: 'Account #', value: 'accountNumber' },
  { title: 'Check Number', value: 'checkNumber' }
];


const budgetHeaders = [
  { title: "Budget ID", value: "budgetId", sortable: true },
  { title: "Month", value: "month", sortable: true },
  { title: "Entity Name", value: "entityName", sortable: true },
  { title: "Transaction Count", value: "transactionCount", sortable: true },
  { title: "Actions", value: "actions", sortable: false },
];

onMounted(async () => {
  const currentUser = auth.currentUser;
  if (!currentUser) {
    showSnackbar("Please log in to view settings", "error");
    return;
  }
  user.value = currentUser;
  userEmail.value = currentUser.email;
  emailVerified.value = currentUser.emailVerified;

  await loadAllData();
});

onUnmounted(() => {
  dataAccess.unsubscribeAll();
});

async function loadAllData() {
  const user = auth.currentUser;
  if (!user) return;

  try {
    family.value = await familyStore.loadFamily(user.uid);
    if (family.value && family.value.members) {
      acceptedMembers.value = (
        await Promise.all(
          family.value.members
            .filter((m) => m.uid && m.email)
            .map(async (m) => ({
              uid: m.uid,
              email: m.email,
              role: "Editor",
              lastAccessed: await dataAccess.getLastAccessed(m.uid),
            }))
        )
      ).filter((m) => m.uid !== undefined);
      pendingInvites.value = await dataAccess.getPendingInvites(user.uid);
    } else {
      acceptedMembers.value = [];
      pendingInvites.value = [];
    }

    // Load budgets and imported transaction docs
    budgets.value = await dataAccess.loadAccessibleBudgets(user.uid);
    importedTransactionDocs.value = await dataAccess.getImportedTransactionDocs();
  } catch (error: any) {
    showSnackbar(`Error loading data: ${error.message}`, "error");
  }
}

function getAccountInfo(item: ImportedTransactionDoc): string {
  if (!item.importedTransactions || item.importedTransactions.length === 0) {
    return "No transactions available";
  }

  const t = item.importedTransactions[0];
  return `${t.accountSource} (${t.accountNumber})`;
}

function getDateRange(item: ImportedTransactionDoc): string {
  if (!item.importedTransactions || item.importedTransactions.length === 0) {
    return "No transactions available";
  }

  const validTransactions = item.importedTransactions.filter((tx) => tx.postedDate && !isNaN(new Date(tx.postedDate).getTime()));

  if (validTransactions.length === 0) {
    return "No valid dates available";
  }

  const dates = validTransactions.map((tx) => new Date(tx.postedDate));
  const beginDate = new Date(Math.min(...dates.map((d) => d.getTime())));
  const endDate = new Date(Math.max(...dates.map((d) => d.getTime())));

  const formatDate = (date: Date): string => {
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    const year = date.getFullYear();
    return `${month}/${day}/${year}`;
  };

  return `${formatDate(beginDate)} - ${formatDate(endDate)}`;
}

async function inviteMember() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar("Please log in to invite users", "error");
    return;
  }

  const normalizedEmail = inviteEmail.value.toLowerCase().trim();
  if (!normalizedEmail) {
    showSnackbar("Please enter an email address", "error");
    return;
  }

  if (normalizedEmail === user.email?.toLowerCase()) {
    showSnackbar("You cannot invite yourself", "error");
    return;
  }

  inviting.value = true;
  try {
    await dataAccess.inviteUser({
      inviterUid: user.uid,
      inviterEmail: user.email || "no-reply@budgetapp.com",
      inviteeEmail: normalizedEmail,
    });
    showSnackbar(`Invitation sent to ${normalizedEmail}`);
    inviteEmail.value = "";
    await loadAllData();
  } catch (error: any) {
    showSnackbar(`Error inviting user: ${error.message}`, "error");
  } finally {
    inviting.value = false;
  }
}

async function removeMember(uid: string) {
  const user = auth.currentUser;
  if (!user || !family.value || !uid) {
    showSnackbar("Cannot remove member: invalid family or user data", "error");
    return;
  }

  try {
    await dataAccess.removeFamilyMember(family.value.id, uid);
    showSnackbar("Member removed");
    await loadAllData();
  } catch (error: any) {
    showSnackbar(`Error removing member: ${error.message}`, "error");
  }
}

async function sendVerificationEmail() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar("Please log in to resend verification email", "error");
    return;
  }

  resending.value = true;
  try {
    await dataAccess.resendVerificationEmail();
    showSnackbar("Verification email sent. Please check your inbox.", "success");
  } catch (error: any) {
    showSnackbar(`Error sending verification email: ${error.message}`, "error");
  } finally {
    resending.value = false;
  }
}

function openCreateEntityDialog() {
  selectedEntity.value = null;
  showEntityDialog.value = true;
}

function openEditEntityDialog(entity: Entity) {
  selectedEntity.value = { ...entity };
  showEntityDialog.value = true;
}

async function handleEntitySave() {
  showEntityDialog.value = false;
  selectedEntity.value = null;
  await loadAllData();
  showSnackbar("Entity saved successfully", "success");
}

function closeEntityForm() {
  showEntityDialog.value = false;
  selectedEntity.value = null;
}

function confirmDeleteEntity(entity: Entity) {
  entityToDelete.value = entity;
  // Check for associated budgets
  associatedBudgets.value = budgets.value.filter((budget) => budget.entityId === entity.id);
  showDeleteEntityDialog.value = true;
}

async function deleteEntity() {
  if (!entityToDelete.value || !family.value || !user.value) {
    showDeleteEntityDialog.value = false;
    return;
  }

  try {
    // Prevent deletion if there are associated budgets
    if (associatedBudgets.value.length > 0) {
      showSnackbar("Cannot delete entity: associated budgets exist. Delete budgets first.", "error");
      return;
    }

    await familyStore.deleteEntity(family.value.id, entityToDelete.value.id);
    showSnackbar(`Entity "${entityToDelete.value.name}" deleted successfully`, "success");
    await loadAllData();
  } catch (error: any) {
    console.error("Error deleting entity:", error);
    showSnackbar(`Error deleting entity: ${error.message}`, "error");
  } finally {
    showDeleteEntityDialog.value = false;
    entityToDelete.value = null;
    associatedBudgets.value = [];
  }
}

function confirmDeleteTransactionDoc(doc: ImportedTransactionDoc) {
  transactionDocToDelete.value = doc;
  showDeleteDialog.value = true;
}

function viewTransactionDoc(event: any, data: any) {
  selectedTransactionDoc.value = data.item;
  showTransactionDocDialog.value = true;
}

function closeTransactionDocDialog() {
  showTransactionDocDialog.value = false;
  selectedTransactionDoc.value = null;
}

async function deleteTransactionDoc() {
  if (!transactionDocToDelete.value) {
    showDeleteDialog.value = false;
    return;
  }

  try {
    const docId = transactionDocToDelete.value.id;
    await dataAccess.deleteImportedTransactionDoc(docId);
    importedTransactionDocs.value = importedTransactionDocs.value.filter((doc) => doc.id !== docId);
    showSnackbar(`Transaction document ${docId} deleted successfully`, "success");
  } catch (error: any) {
    console.error("Error deleting transaction document:", error);
    showSnackbar(`Error deleting transaction document: ${error.message}`, "error");
  } finally {
    showDeleteDialog.value = false;
    transactionDocToDelete.value = null;
  }
}

function getEntityName(entityId: string): string {
  const entity = family.value?.entities?.find((e) => e.id === entityId);
  return entity ? entity.name : "Unknown";
}

function confirmDeleteBudget(budget: Budget) {
  budgetToDelete.value = budget;
  showDeleteBudgetDialog.value = true;
}

async function deleteBudget() {
  if (!budgetToDelete.value) {
    showDeleteBudgetDialog.value = false;
    return;
  }

  try {
    const budgetId = budgetToDelete.value.budgetId;
    await dataAccess.deleteBudget(budgetId);
    budgets.value = budgets.value.filter((budget) => budget.budgetId !== budgetId);
    showSnackbar(`Budget ${budgetId} deleted successfully`, "success");
  } catch (error: any) {
    console.error("Error deleting budget:", error);
    showSnackbar(`Error deleting budget: ${error.message}`, "error");
  } finally {
    showDeleteBudgetDialog.value = false;
    budgetToDelete.value = null;
  }
}

function formatDate(timestamp: Timestamp | null) {
  return timestamp ? timestampToDate(timestamp).toLocaleString() : "N/A";
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
