<!-- src/views/SettingsView.vue -->
<template>
  <v-container>
    <h1>Settings</h1>

    <v-alert v-if="userEmail && !emailVerified" type="warning" class="mb-4">
      Your email ({{ userEmail }}) is not verified. Please check your inbox or resend the verification email.
      <template v-slot:actions>
        <v-btn variant="plain" @click="sendVerificationEmail" :loading="resending">Resend</v-btn>
      </template>
    </v-alert>

    <v-tabs v-model="activeTab" color="primary" class="mt-4">
      <v-tab value="group">Manage Family/Group</v-tab>
      <v-tab value="entity">Manage Entities</v-tab>
      <v-tab value="manageTransactions">Manage Imports</v-tab>
      <v-tab value="manageBudgets">Manage Budgets</v-tab>
    </v-tabs>

    <v-window v-model="activeTab">
      <!-- Group Management Tab -->
      <v-window-item value="group">
        <v-card class="mt-4">
          <v-card-title>Family/Group Information</v-card-title>
          <v-card-text>
            <v-list>
              <v-list-item v-for="member in acceptedMembers" :key="member.uid">
                {{ member.email }} ({{ member.role }}) - Last Accessed: {{ formatDate(member.lastAccessed) || "Never" }}
                <v-btn v-if="member.uid !== user.uid" @click="removeMember(member.uid)">Remove</v-btn>
              </v-list-item>
              <v-list-item v-for="invite in pendingInvites" :key="invite.token">
                {{ invite.inviteeEmail }} (Pending) - Invited: {{ formatDate(invite.createdAt) }}
              </v-list-item>
            </v-list>
            <v-form @submit.prevent="inviteMember">
              <v-text-field v-model="inviteEmail" label="Invite Email" type="email" required></v-text-field>
              <v-btn type="submit" :loading="inviting">Invite</v-btn>
            </v-form>
          </v-card-text>
        </v-card>
      </v-window-item>

      <!-- Entity Management Tab -->
      <v-window-item value="entity">
        <v-card class="mt-4">
          <v-card-title>Entities</v-card-title>
          <v-card-text>
            <v-btn color="primary" @click="openCreateEntityDialog" class="mb-4">Add Entity</v-btn>
            <v-list>
              <v-list-item v-for="entity in entities" :key="entity.id">
                <v-row :dense="true">
                  <v-col>
                    {{ entity.name }} ({{ entity.type }}) - Owner: {{ entity.members.find((m) => m.role === "Admin")?.email || "N/A" }}
                    <v-chip v-if="entity.templateBudget" color="success" size="small" class="ml-2">Has Template</v-chip>
                  </v-col>
                  <v-col cols="auto">
                    <v-btn variant="plain" color="primary" icon @click="openEditEntityDialog(entity)">
                      <v-icon>mdi-pencil</v-icon>
                    </v-btn>
                  </v-col>
                  <v-col cols="auto">
                    <v-btn variant="plain" icon @click="confirmDeleteEntity(entity)" color="error">
                      <v-icon>mdi-trash-can-outline</v-icon>
                    </v-btn>
                  </v-col>
                </v-row>
              </v-list-item>
              <v-list-item v-if="!entities.length"> No entities found. Create an entity to start managing budgets. </v-list-item>
            </v-list>
          </v-card-text>
        </v-card>
      </v-window-item>

      <!-- Manage Imported Transactions Tab -->
      <v-window-item value="manageTransactions">
        <v-row>
          <v-col cols="12">
            <v-card>
              <v-card-title>Imported Transaction</v-card-title>
              <v-card-text>
              <v-data-table
                :headers="transactionDocHeaders"
                :items="importedTransactionDocs"
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
                    <v-btn
                      icon
                      density="compact"
                      variant="plain"
                      color="error"
                      @click.stop="confirmDeleteTransactionDoc(item)"
                      title="Delete Transaction Document"
                    >
                      <v-icon>mdi-trash-can-outline</v-icon>
                    </v-btn>
                  </template>
              </v-data-table>
                <div class="mt-4">
                  <v-btn color="secondary" @click="validateImportedTransactions" :loading="validatingImports">
                    Validate Imported Transactions
                  </v-btn>
                </div>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>

      <!-- Manage Budgets Tab -->
      <v-window-item value="manageBudgets">
        <v-row>
          <v-col cols="12">
            <v-card>
              <v-card-title>Monthly Budgets</v-card-title>
              <v-card-text>
                <v-data-table :headers="budgetHeaders" :items="budgets" :items-per-page="10" class="elevation-1">
                  <template v-slot:item.entityName="{ item }">
                    {{ getEntityName(item.entityId) }}
                  </template>
                  <template v-slot:item.transactionCount="{ item }">
                    {{ item.transactions?.length || 0 }}
                  </template>
                  <template v-slot:item.actions="{ item }">
                    <v-btn icon density="compact" variant="plain" color="error" @click.stop="confirmDeleteBudget(item)" title="Delete Budget">
                      <v-icon>mdi-trash-can-outline</v-icon>
                    </v-btn>
                  </template>
                </v-data-table>
                <div class="mt-4">
                  <v-btn color="secondary" @click="validateBudgetTransactions" :loading="validatingBudgets">
                    Validate Budget Transactions
                  </v-btn>
                </div>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>
    </v-window>

    <!-- Transaction Document Details Dialog -->
    <v-dialog v-model="showTransactionDocDialog" max-width="1000px" @hide="closeTransactionDocDialog">
      <v-card style="min-width: 600px">
        <v-card-title class="bg-primary py-3">
          <span class="text-white">Imported Transactions</span>
        </v-card-title>
        <v-card-text>
          <v-data-table
            :headers="importedTransactionHeaders"
            :items="selectedTransactionDoc?.importedTransactions || []"
            :items-per-page="10"
            class="elevation-1"
          >
            <template v-slot:item.accountId="{ item }">
              {{ item.accountId }}
            </template>
          </v-data-table>
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" color="primary" @click="closeTransactionDocDialog">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Transaction Doc Confirmation Dialog -->
    <v-dialog v-model="showDeleteDialog" max-width="400" @keyup.enter="deleteTransactionDoc">
      <v-card>
        <v-card-title class="bg-error py-3">
          <span class="text-white">Delete Transaction Document</span>
        </v-card-title>
        <v-card-text class="pt-4">
          Are you sure you want to delete the transaction document with ID "{{ transactionDocToDelete?.id }}" containing
          {{ transactionDocToDelete?.importedTransactions.length }} transactions? This action cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="flat" @click="deleteTransactionDoc">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Budget Confirmation Dialog -->
    <v-dialog v-model="showDeleteBudgetDialog" max-width="400" @keyup.enter="deleteBudget">
      <v-card>
        <v-card-title class="bg-error py-3">
          <span class="text-white">Delete Budget</span>
        </v-card-title>
        <v-card-text class="pt-4">
          Are you sure you want to delete the budget for "{{ budgetToDelete?.month }}" (ID: {{ budgetToDelete?.budgetId }}) containing
          {{ budgetToDelete?.transactions?.length || 0 }} transactions? This action cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showDeleteBudgetDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="flat" @click="deleteBudget">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Entity Confirmation Dialog -->
    <v-dialog v-model="showDeleteEntityDialog" max-width="400" @keyup.enter="deleteEntity">
      <v-card>
        <v-card-title class="bg-error py-3">
          <span class="text-white">Delete Entity</span>
        </v-card-title>
        <v-card-text class="pt-4">
          Are you sure you want to delete the entity "{{ entityToDelete?.name }}" (ID: {{ entityToDelete?.id }})?
          <span v-if="associatedBudgets.length > 0"> This entity has {{ associatedBudgets.length }} associated budget(s), which must be deleted first. </span>
          <span v-else>This action cannot be undone.</span>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="grey" variant="text" @click="showDeleteEntityDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="flat" @click="deleteEntity" :disabled="associatedBudgets.length > 0">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Entity Form Dialog -->
    <v-dialog v-model="showEntityDialog" max-width="1000px" persistent>
      <entity-form
        v-if="selectedEntity"
        :key="selectedEntity.id"
        ref="entityFormRef"
        :entity-id="selectedEntity.id"
        @cancel="closeEntityForm"
        @save="handleEntitySave"
        @update:unsaved="closeEntityForm"
      />
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
import { ref, onMounted, onUnmounted, computed } from "vue";
import { auth } from "../firebase/index";
import { dataAccess } from "../dataAccess";
import { Timestamp } from "firebase/firestore";
import { Family, PendingInvite, Entity, Budget, ImportedTransactionDoc, Transaction, ImportedTransaction } from "../types";
import { useFamilyStore } from "../store/family";
import EntityForm from "../components/EntityForm.vue";
import { timestampToDate } from "../utils/helpers";
import { v4 as uuidv4 } from "uuid";

const familyStore = useFamilyStore();
const inviteEmail = ref("");
const inviting = ref(false);
const resending = ref(false);
const snackbar = ref(false);
const snackbarText = ref("");
const snackbarColor = ref("success");
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
const validatingBudgets = ref(false);
const validatingImports = ref(false);
const showTransactionDocDialog = ref(false);
const selectedTransactionDoc = ref<ImportedTransactionDoc | null>(null);

const entities = computed(() => family.value?.entities || []);

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

async function validateBudgetTransactions() {
  validatingBudgets.value = true;
  try {
    const currentUser = auth.currentUser;
    if (!currentUser) {
      showSnackbar('User not authenticated', 'error');
      return;
    }

    const budgetsList = await dataAccess.loadAccessibleBudgets(currentUser.uid);
    const seenBudgetIds = new Set<string>();
    const budgetUpdates: { budgetId: string; transaction: Transaction; oldId?: string }[] = [];

    budgetsList.forEach((b) => {
      b.transactions?.forEach((tx) => {
        if (!tx.id || seenBudgetIds.has(tx.id)) {
          const newId = uuidv4();
          budgetUpdates.push({ budgetId: b.budgetId || b.month, oldId: tx.id, transaction: { ...tx, id: newId } });
          seenBudgetIds.add(newId);
        } else {
          seenBudgetIds.add(tx.id);
        }
      });
    });

    if (budgetUpdates.length > 0) {
      await dataAccess.updateBudgetTransactions(budgetUpdates);
    }

    budgets.value = await dataAccess.loadAccessibleBudgets(currentUser.uid);

    showSnackbar(`Validation complete. Updated ${budgetUpdates.length} budget transactions`, 'success');
  } catch (error: any) {
    console.error('Error validating budgets:', error);
    showSnackbar(`Error validating budgets: ${error.message}`, 'error');
  } finally {
    validatingBudgets.value = false;
  }
}

async function validateImportedTransactions() {
  validatingImports.value = true;
  try {
    const importedDocs = await dataAccess.getImportedTransactionDocs();
    const seenImportedIds = new Set<string>();
    const importedUpdates: ImportedTransaction[] = [];

    importedDocs.forEach((doc) => {
      doc.importedTransactions.forEach((tx) => {
        if (!tx.id || seenImportedIds.has(tx.id)) {
          const newId = `${doc.id}-${uuidv4()}`;
          importedUpdates.push({ ...tx, id: newId });
          seenImportedIds.add(newId);
        } else {
          seenImportedIds.add(tx.id);
        }
      });
    });

    if (importedUpdates.length > 0) {
      await dataAccess.updateImportedTransactions(importedUpdates);
    }

    importedTransactionDocs.value = await dataAccess.getImportedTransactionDocs();

    showSnackbar(`Validation complete. Updated ${importedUpdates.length} imported transactions`, 'success');
  } catch (error: any) {
    console.error('Error validating imported transactions:', error);
    showSnackbar(`Error validating imported transactions: ${error.message}`, 'error');
  } finally {
    validatingImports.value = false;
  }
}

function formatDate(timestamp: Timestamp | null) {
  return timestamp ? timestampToDate(timestamp).toLocaleString() : "N/A";
}

function showSnackbar(text: string, color = "success") {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
}
</script>
