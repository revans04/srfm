<!-- src/pages/SettingsView.vue -->
<template>
  <q-page padding>
    <q-card flat>
      <q-card-section>
        <div class="text-h4">Settings</div>
      </q-card-section>

      <!-- Email Verification Alert -->
      <q-banner v-if="userEmail && !emailVerified" class="bg-warning text-white q-mb-md">
        Your email ({{ userEmail }}) is not verified. Please check your inbox or resend the verification email.
        <template v-slot:action>
          <q-btn flat label="Resend" :loading="resending" @click="sendVerificationEmail" />
        </template>
      </q-banner>

      <!-- Tabs -->
      <q-tabs
        v-model="activeTab"
        dense
        class="bg-primary text-white q-mt-md"
        active-color="white"
        indicator-color="white"
      >
        <q-tab name="group" label="Manage Family/Group" />
        <q-tab name="entity" label="Manage Entities" />
        <q-tab name="manageTransactions" label="Manage Imports" />
        <q-tab name="manageBudgets" label="Manage Budgets" />
      </q-tabs>

      <!-- Tab Content -->
      <q-tab-panels v-model="activeTab" animated>
        <!-- Group Management Tab -->
        <q-tab-panel name="group">
          <q-card flat bordered class="q-mt-md">
            <q-card-section>
              <div class="text-h6">Family/Group Information</div>
            </q-card-section>
            <q-card-section>
              <q-list>
                <q-item v-for="member in acceptedMembers" :key="member.uid">
                  <q-item-section>
                    {{ member.email }} ({{ member.role }}) - Last Accessed: {{ formatDate(member.lastAccessed) || 'Never' }}
                  </q-item-section>
                  <q-item-section side v-if="member.uid !== user.uid">
                    <q-btn flat color="negative" label="Remove" @click="removeMember(member.uid)" />
                  </q-item-section>
                </q-item>
                <q-item v-for="invite in pendingInvites" :key="invite.token">
                  <q-item-section>
                    {{ invite.inviteeEmail }} (Pending) - Invited: {{ formatDate(invite.createdAt) }}
                  </q-item-section>
                </q-item>
              </q-list>
              <q-form @submit="inviteMember">
                <q-input
                  v-model="inviteEmail"
                  label="Invite Email"
                  type="email"
                  outlined
                  dense
                  required
                  class="q-mb-md"
                />
                <q-btn type="submit" color="primary" label="Invite" :loading="inviting" />
              </q-form>
            </q-card-section>
          </q-card>
        </q-tab-panel>

        <!-- Entity Management Tab -->
        <q-tab-panel name="entity">
          <q-card flat bordered class="q-mt-md">
            <q-card-section>
              <div class="text-h6">Entities</div>
            </q-card-section>
            <q-card-section>
              <q-btn color="primary" label="Add Entity" @click="openCreateEntityDialog" class="q-mb-md" />
              <q-list>
                <q-item v-for="entity in entities" :key="entity.id">
                  <q-item-section>
                    {{ entity.name }} ({{ entity.type }}) - Owner:
                    {{ entity.members.find((m) => m.role === 'Admin')?.email || 'N/A' }}
                    <q-chip v-if="entity.templateBudget" color="positive" size="sm" class="q-ml-sm">
                      Has Template
                    </q-chip>
                  </q-item-section>
                  <q-item-section side>
                    <q-btn flat icon="mdi-pencil" color="primary" @click="openEditEntityDialog(entity)" />
                    <q-btn
                      flat
                      icon="mdi-trash-can-outline"
                      color="negative"
                      @click="confirmDeleteEntity(entity)"
                    />
                  </q-item-section>
                </q-item>
                <q-item v-if="!entities.length">
                  No entities found. Create an entity to start managing budgets.
                </q-item>
              </q-list>
            </q-card-section>
          </q-card>
        </q-tab-panel>

        <!-- Manage Imported Transactions Tab -->
        <q-tab-panel name="manageTransactions">
          <q-card flat bordered>
            <q-card-section>
              <div class="text-h6">Imported Transactions</div>
            </q-card-section>
            <q-card-section>
              <q-table
                :rows="importedTransactionDocs"
                :columns="transactionDocHeaders"
                row-key="id"
                :pagination="{ rowsPerPage: 10 }"
                class="elevation-1"
              >
                <template v-slot:body-cell-createdAt="props">
                  <q-td :props="props">
                    {{ getDateRange(props.row) }}
                  </q-td>
                </template>
                <template v-slot:body-cell-account="props">
                  <q-td :props="props">
                    {{ getAccountInfo(props.row) }}
                  </q-td>
                </template>
                <template v-slot:body-cell-actions="props">
                  <q-td :props="props">
                    <q-btn
                      flat
                      icon="mdi-trash-can-outline"
                      color="negative"
                      @click="confirmDeleteTransactionDoc(props.row)"
                      title="Delete Transaction Document"
                    />
                  </q-td>
                </template>
              </q-table>
            </q-card-section>
          </q-card>
        </q-tab-panel>

        <!-- Manage Budgets Tab -->
        <q-tab-panel name="manageBudgets">
          <q-card flat bordered>
            <q-card-section>
              <div class="text-h6">Monthly Budgets</div>
            </q-card-section>
            <q-card-section>
              <q-table
                :rows="budgets"
                :columns="budgetHeaders"
                row-key="budgetId"
                :pagination="{ rowsPerPage: 10 }"
                class="elevation-1"
              >
                <template v-slot:body-cell-entityName="props">
                  <q-td :props="props">
                    {{ getEntityName(props.row.entityId) }}
                  </q-td>
                </template>
                <template v-slot:body-cell-transactionCount="props">
                  <q-td :props="props">
                    {{ props.row.transactions?.length || 0 }}
                  </q-td>
                </template>
                <template v-slot:body-cell-actions="props">
                  <q-td :props="props">
                    <q-btn
                      flat
                      icon="mdi-trash-can-outline"
                      color="negative"
                      @click="confirmDeleteBudget(props.row)"
                      title="Delete Budget"
                    />
                  </q-td>
                </template>
              </q-table>
            </q-card-section>
          </q-card>
        </q-tab-panel>
      </q-tab-panels>

      <!-- Delete Transaction Doc Confirmation Dialog -->
      <q-dialog v-model="showDeleteDialog" max-width="400px">
        <q-card>
          <q-card-section class="bg-negative text-white">
            <div class="text-h6">Delete Transaction Document</div>
          </q-card-section>
          <q-card-section class="q-pt-md">
            Are you sure you want to delete the transaction document with ID "{{ transactionDocToDelete?.id }}"
            containing {{ transactionDocToDelete?.importedTransactions.length }} transactions? This action cannot be undone.
          </q-card-section>
          <q-card-actions align="right">
            <q-btn flat label="Cancel" color="grey" @click="showDeleteDialog = false" />
            <q-btn flat label="Delete" color="negative" @click="deleteTransactionDoc" />
          </q-card-actions>
        </q-card>
      </q-dialog>

      <!-- Delete Budget Confirmation Dialog -->
      <q-dialog v-model="showDeleteBudgetDialog" max-width="400px">
        <q-card>
          <q-card-section class="bg-negative text-white">
            <div class="text-h6">Delete Budget</div>
          </q-card-section>
          <q-card-section class="q-pt-md">
            Are you sure you want to delete the budget for "{{ budgetToDelete?.month }}" (ID:
            {{ budgetToDelete?.budgetId }}) containing {{ budgetToDelete?.transactions?.length || 0 }}
            transactions? This action cannot be undone.
          </q-card-section>
          <q-card-actions align="right">
            <q-btn flat label="Cancel" color="grey" @click="showDeleteBudgetDialog = false" />
            <q-btn flat label="Delete" color="negative" @click="deleteBudget" />
          </q-card-actions>
        </q-card>
      </q-dialog>

      <!-- Delete Entity Confirmation Dialog -->
      <q-dialog v-model="showDeleteEntityDialog" max-width="400px">
        <q-card>
          <q-card-section class="bg-negative text-white">
            <div class="text-h6">Delete Entity</div>
          </q-card-section>
          <q-card-section class="q-pt-md">
            Are you sure you want to delete the entity "{{ entityToDelete?.name }}" (ID: {{ entityToDelete?.id }})?
            <span v-if="associatedBudgets.length > 0">
              This entity has {{ associatedBudgets.length }} associated budget(s), which must be deleted first.
            </span>
            <span v-else>This action cannot be undone.</span>
          </q-card-section>
          <q-card-actions align="right">
            <q-btn flat label="Cancel" color="grey" @click="showDeleteEntityDialog = false" />
            <q-btn
              flat
              label="Delete"
              color="negative"
              @click="deleteEntity"
              :disable="associatedBudgets.length > 0"
            />
          </q-card-actions>
        </q-card>
      </q-dialog>

      <!-- Entity Form Dialog -->
      <q-dialog v-model="showEntityDialog" persistent max-width="1000px">
        <entity-form
          v-if="selectedEntity"
          :key="selectedEntity.id"
          :entity-id="selectedEntity.id"
          @cancel="closeEntityForm"
          @save="handleEntitySave"
          @update:unsaved="closeEntityForm"
        />
      </q-dialog>

      <!-- Snackbar -->
      <q-notification
        v-model="snackbar"
        :color="snackbarColor"
        position="top"
        :timeout="3000"
      >
        {{ snackbarText }}
        <template v-slot:action>
          <q-btn flat label="Close" @click="snackbar = false" />
        </template>
      </q-notification>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { auth } from '../firebase/index';
import { dataAccess } from '../dataAccess';
import { Timestamp } from 'firebase/firestore';
import { Family, PendingInvite, Entity, Budget, ImportedTransactionDoc } from '@/types';
import { useFamilyStore } from '../store/family';
import EntityForm from '../components/EntityForm.vue';
import { timestampToDate } from '@/utils/helpers';

const familyStore = useFamilyStore();
const inviteEmail = ref('');
const inviting = ref(false);
const resending = ref(false);
const snackbar = ref(false);
const snackbarText = ref('');
const snackbarColor = ref('success');
const userEmail = ref<string | null>(null);
const emailVerified = ref(false);
const user = ref(auth.currentUser);
const family = ref<Family | null>(null);
const acceptedMembers = ref<
  Array<{ uid: string; email: string; role: string; lastAccessed?: Timestamp }>
>([]);
const pendingInvites = ref<PendingInvite[]>([]);
const selectedEntity = ref<Entity | null>(null);
const activeTab = ref('group');
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

const entities = computed(() => family.value?.entities || []);

const transactionDocHeaders = [
  { title: 'Document ID', key: 'id' },
  { title: 'Transaction Count', key: 'importedTransactions.length' },
  { title: 'Account Info', key: 'account' },
  { title: 'Created At', key: 'createdAt' },
  { title: 'Actions', key: 'actions' },
];

const budgetHeaders = [
  { title: 'Budget ID', key: 'budgetId' },
  { title: 'Month', key: 'month' },
  { title: 'Entity Name', key: 'entityName' },
  { title: 'Transaction Count', key: 'transactionCount' },
  { title: 'Actions', key: 'actions' },
];

onMounted(async () => {
  const currentUser = auth.currentUser;
  if (!currentUser) {
    showSnackbar('Please log in to view settings', 'error');
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
              role: 'Editor',
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
    showSnackbar(`Error loading data: ${error.message}`, 'error');
  }
}

function getAccountInfo(item: ImportedTransactionDoc): string {
  if (!item.importedTransactions || item.importedTransactions.length === 0) {
    return 'No transactions available';
  }

  const t = item.importedTransactions[0];
  return `${t.accountSource} (${t.accountNumber})`;
}

function getDateRange(item: ImportedTransactionDoc): string {
  if (!item.importedTransactions || item.importedTransactions.length === 0) {
    return 'No transactions available';
  }

  const validTransactions = item.importedTransactions.filter(
    (tx) => tx.postedDate && !isNaN(new Date(tx.postedDate).getTime())
  );

  if (validTransactions.length === 0) {
    return 'No valid dates available';
  }

  const dates = validTransactions.map((tx) => new Date(tx.postedDate));
  const beginDate = new Date(Math.min(...dates.map((d) => d.getTime())));
  const endDate = new Date(Math.max(...dates.map((d) => d.getTime())));

  const formatDate = (date: Date): string => {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const year = date.getFullYear();
    return `${month}/${day}/${year}`;
  };

  return `${formatDate(beginDate)} - ${formatDate(endDate)}`;
}

async function inviteMember() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar('Please log in to invite users', 'error');
    return;
  }

  const normalizedEmail = inviteEmail.value.toLowerCase().trim();
  if (!normalizedEmail) {
    showSnackbar('Please enter an email address', 'error');
    return;
  }

  if (normalizedEmail === user.email?.toLowerCase()) {
    showSnackbar('You cannot invite yourself', 'error');
    return;
  }

  inviting.value = true;
  try {
    await dataAccess.inviteUser({
      inviterUid: user.uid,
      inviterEmail: user.email || 'no-reply@budgetapp.com',
      inviteeEmail: normalizedEmail,
    });
    showSnackbar(`Invitation sent to ${normalizedEmail}`);
    inviteEmail.value = '';
    await loadAllData();
  } catch (error: any) {
    showSnackbar(`Error inviting user: ${error.message}`, 'error');
  } finally {
    inviting.value = false;
  }
}

async function removeMember(uid: string) {
  const user = auth.currentUser;
  if (!user || !family.value || !uid) {
    showSnackbar('Cannot remove member: invalid family or user data', 'error');
    return;
  }

  try {
    await dataAccess.removeFamilyMember(family.value.id, uid);
    showSnackbar('Member removed');
    await loadAllData();
  } catch (error: any) {
    showSnackbar(`Error removing member: ${error.message}`, 'error');
  }
}

async function sendVerificationEmail() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar('Please log in to resend verification email', 'error');
    return;
  }

  resending.value = true;
  try {
    await dataAccess.resendVerificationEmail();
    showSnackbar('Verification email sent. Please check your inbox.', 'success');
  } catch (error: any) {
    showSnackbar(`Error sending verification email: ${error.message}`, 'error');
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
  showSnackbar('Entity saved successfully', 'success');
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
      showSnackbar('Cannot delete entity: associated budgets exist. Delete budgets first.', 'error');
      return;
    }

    await familyStore.deleteEntity(family.value.id, entityToDelete.value.id);
    showSnackbar(`Entity "${entityToDelete.value.name}" deleted successfully`, 'success');
    await loadAllData();
  } catch (error: any) {
    console.error('Error deleting entity:', error);
    showSnackbar(`Error deleting entity: ${error.message}`, 'error');
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

async function deleteTransactionDoc() {
  if (!transactionDocToDelete.value) {
    showDeleteDialog.value = false;
    return;
  }

  try {
    const docId = transactionDocToDelete.value.id;
    await dataAccess.deleteImportedTransactionDoc(docId);
    importedTransactionDocs.value = importedTransactionDocs.value.filter((doc) => doc.id !== docId);
    showSnackbar(`Transaction document ${docId} deleted successfully`, 'success');
  } catch (error: any) {
    console.error('Error deleting transaction document:', error);
    showSnackbar(`Error deleting transaction document: ${error.message}`, 'error');
  } finally {
    showDeleteDialog.value = false;
    transactionDocToDelete.value = null;
  }
}

function getEntityName(entityId: string): string {
  const entity = family.value?.entities?.find((e) => e.id === entityId);
  return entity ? entity.name : 'Unknown';
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
    showSnackbar(`Budget ${budgetId} deleted successfully`, 'success');
  } catch (error: any) {
    console.error('Error deleting budget:', error);
    showSnackbar(`Error deleting budget: ${error.message}`, 'error');
  } finally {
    showDeleteBudgetDialog.value = false;
    budgetToDelete.value = null;
  }
}

function formatDate(timestamp: Timestamp | null) {
  return timestamp ? timestampToDate(timestamp).toLocaleString() : 'N/A';
}

function showSnackbar(text: string, color = 'success') {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
}
</script>
