<!-- src/views/SettingsView.vue -->
<template>
  <q-page>
    <h1>Settings</h1>

    <q-banner v-if="userEmail && !emailVerified" type="warning" class="q-mb-lg">
      Your email ({{ userEmail }}) is not verified. Please check your inbox or resend the verification email.
      <template v-slot:action>
        <q-btn variant="plain" @click="sendVerificationEmail" :loading="resending">Resend</q-btn>
      </template>
    </q-banner>

    <q-tabs v-model="activeTab" color="primary" class="q-mt-lg">
      <q-tab name="group" label="Manage Family/Group" />
      <q-tab name="entity" label="Manage Entities" />
      <q-tab name="manageTransactions" label="Manage Imports" />
      <q-tab name="manageBudgets" label="Manage Budgets" />
    </q-tabs>

    <q-tab-panels v-model="activeTab">
      <!-- Group Management Tab -->
      <q-tab-panel name="group">
        <q-card class="q-mt-lg">
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

        <q-card class="q-mt-lg">
          <q-card-section>Data Sync</q-card-section>
          <q-card-section>
            <div class="row items-center q-col-gutter-sm">
              <div class="col-auto">
                <q-btn color="primary" :loading="syncing" @click="syncNow">
                  <q-icon name="sync" class="q-mr-sm" /> Force Sync (Firestore → Supabase)
                </q-btn>
              </div>
            </div>
            <div class="text-caption q-mt-sm text-secondary">
              Runs a server-side sync that upserts budgets and transactions into Supabase.
            </div>

            <q-separator class="q-my-md" />

            <div class="row q-col-gutter-md items-end">
              <div class="col-12 col-sm-4">
                <q-input v-model="syncSinceDate" label="Since Date" placeholder="YYYY-MM-DD" dense readonly>
                  <template #append>
                    <q-icon name="event" class="cursor-pointer">
                      <q-popup-proxy cover transition-show="scale" transition-hide="scale">
                        <q-date v-model="syncSinceDate" mask="YYYY-MM-DD" />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
              </div>
              <div class="col-12 col-sm-3">
                <q-input v-model="syncSinceTime" label="Time (optional)" placeholder="HH:mm" dense readonly>
                  <template #append>
                    <q-icon name="schedule" class="cursor-pointer">
                      <q-popup-proxy cover transition-show="scale" transition-hide="scale">
                        <q-time v-model="syncSinceTime" mask="HH:mm" format24h />
                      </q-popup-proxy>
                    </q-icon>
                  </template>
                </q-input>
              </div>
              <div class="col-auto">
                <q-btn color="secondary" :disable="!syncSinceDate" :loading="syncingIncremental" @click="syncIncrementalNow">
                  <q-icon name="sync_alt" class="q-mr-sm" /> Incremental Sync
                </q-btn>
              </div>
            </div>
            <div class="text-caption q-mt-sm text-secondary">
              Runs an incremental sync for changes since the selected date/time.
            </div>

            <q-separator class="q-my-md" />

            <div class="row q-col-gutter-sm q-mt-sm">
              <div class="col-auto">
                <q-btn color="primary" :loading="syncingUsers" @click="syncUsersNow">
                  <q-icon name="person" class="q-mr-sm" /> Sync Users
                </q-btn>
              </div>
              <div class="col-auto">
                <q-btn color="primary" :loading="syncingFamilies" @click="syncFamiliesNow">
                  <q-icon name="groups" class="q-mr-sm" /> Sync Families
                </q-btn>
              </div>
              <div class="col-auto">
                <q-btn color="primary" :loading="syncingAccounts" @click="syncAccountsNow">
                  <q-icon name="account_balance" class="q-mr-sm" /> Sync Accounts
                </q-btn>
              </div>
              <div class="col-auto">
                <q-btn color="primary" :loading="syncingSnapshots" @click="syncSnapshotsNow">
                  <q-icon name="photo_camera" class="q-mr-sm" /> Sync Snapshots
                </q-btn>
              </div>
            </div>
            <div class="text-caption q-mt-sm text-secondary">
              Run targeted syncs for individual datasets.
            </div>
          </q-card-section>
        </q-card>
      </q-tab-panel>

      <!-- Entity Management Tab -->
      <q-tab-panel name="entity">
        <q-card class="q-mt-lg">
          <q-card-section>Entities</q-card-section>
          <q-card-section>
            <q-btn color="primary" @click="openCreateEntityDialog" class="q-mb-lg">Add Entity</q-btn>
            <q-list>
              <q-item v-for="entity in entities" :key="entity.id">
                <div class="row dense">
                  <div class="col">
                    {{ entity.name }} ({{ entity.type }}) - Owner: {{ entity.members.find((m) => m.role === "Admin")?.email || "N/A" }}
                    <q-chip v-if="entity.templateBudget" color="positive" size="small" class="q-ml-sm">Has Template</q-chip>
                  </div>
                  <div class="col col-auto">
                    <q-btn variant="plain" color="primary" @click="openEditEntityDialog(entity)" icon="edit" />
                  </div>
                  <div class="col col-auto">
                    <q-btn variant="plain" @click="confirmDeleteEntity(entity)" color="negative" icon="o_delete" />
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
                  :pagination="{ rowsPerPage: 10 }"
                  class="elevation-1"
                >
                  <template #body-cell-createdAt="props">
                    <q-td :props="props">
                      {{ getDateRange(props.row) }}
                    </q-td>
                  </template>
                  <template #body-cell-account="props">
                    <q-td :props="props">
                      {{ getAccountInfo(props.row) }}
                    </q-td>
                  </template>
                  <template #body-cell-actions="props">
                    <q-td :props="props">
                      <q-btn
                        density="compact"
                        variant="plain"
                        color="negative"
                        @click.stop="confirmDeleteTransactionDoc(props.row)"
                        title="Delete Transaction Document"
                        icon="o_delete"
                      />
                    </q-td>
                  </template>
                </q-table>
                <div class="q-mt-md">
                  <q-btn color="secondary" @click="validateImportedTransactions" :loading="validatingImports">
                    Validate Imported Transactions
                  </q-btn>
                </div>
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
                <q-table
                  :columns="budgetColumns"
                  :rows="budgets"
                  :pagination="{ rowsPerPage: 10 }"
                  class="elevation-1"
                >
                  <template #body-cell-entityName="props">
                    <q-td :props="props">
                      {{ getEntityName(props.row.entityId) }}
                    </q-td>
                  </template>
                  <template #body-cell-transactionCount="props">
                    <q-td :props="props">
                      {{ props.row.transactionCount ?? 0 }}
                    </q-td>
                  </template>
                  <template #body-cell-actions="props">
                    <q-td :props="props">
                      <q-btn
                        density="compact"
                        variant="plain"
                        color="negative"
                        @click.stop="confirmDeleteBudget(props.row)"
                        title="Delete Budget"
                        icon="o_delete"
                      />
                    </q-td>
                  </template>
                </q-table>
                <div class="q-mt-md">
                  <q-btn color="secondary" @click="validateBudgetTransactions" :loading="validatingBudgets">
                    Validate Budget Transactions
                  </q-btn>
                </div>

                <q-separator class="q-my-lg" />

                <div class="row q-col-gutter-md items-end">
                  <div class="col-12 col-sm-4">
                    <q-select
                      v-model="recalcEntityId"
                      :options="entityOptions"
                      option-label="label"
                      option-value="value"
                      emit-value
                      map-options
                      label="Entity"
                      dense
                    />
                  </div>
                  <div class="col-12 col-sm-4">
                    <q-select
                      v-model="recalcStartMonth"
                      :options="startMonthOptions"
                      label="Start From Budget Month"
                      dense
                      :disable="!recalcEntityId"
                    />
                  </div>
                  <div class="col-auto">
                    <q-btn
                      color="primary"
                      :disable="!recalcEntityId || !recalcStartMonth"
                      :loading="recalcLoading"
                      @click="runRecalcCarryover"
                    >
                      Recalculate Carryforward (from month → latest)
                    </q-btn>
                  </div>
                </div>
                <div class="text-caption q-mt-sm text-secondary">
                  Updates fund category carryover values month-by-month from the selected budget through the latest month.
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </q-tab-panel>
    </q-tab-panels>

    <!-- Delete Transaction Doc Confirmation Dialog -->
    <q-dialog v-model="showDeleteDialog" max-width="400" @keyup.enter="deleteTransactionDoc">
      <q-card>
        <q-card-section class="bg-negative q-py-md">
          <span class="text-white">Delete Transaction Document</span>
        </q-card-section>
        <q-card-section class="q-pt-lg">
          Are you sure you want to delete the transaction document with ID "{{ transactionDocToDelete?.id }}" containing
          {{ transactionDocToDelete?.importedTransactions.length }} transactions? This action cannot be undone.
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteDialog = false">Cancel</q-btn>
          <q-btn color="negative" variant="flat" @click="deleteTransactionDoc">Delete</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Delete Budget Confirmation Dialog -->
    <q-dialog v-model="showDeleteBudgetDialog" max-width="400" @keyup.enter="deleteBudget">
      <q-card>
        <q-card-section class="bg-negative q-py-md">
          <span class="text-white">Delete Budget</span>
        </q-card-section>
        <q-card-section class="q-pt-lg">
          Are you sure you want to delete the budget for "{{ budgetToDelete?.month }}" (ID: {{ budgetToDelete?.budgetId }}) containing
          {{ budgetToDelete?.transactionCount || 0 }} transactions? This action cannot be undone.
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteBudgetDialog = false">Cancel</q-btn>
          <q-btn color="negative" variant="flat" @click="deleteBudget">Delete</q-btn>
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Delete Entity Confirmation Dialog -->
    <q-dialog v-model="showDeleteEntityDialog" max-width="400" @keyup.enter="deleteEntity">
      <q-card>
        <q-card-section class="bg-negative q-py-md">
          <span class="text-white">Delete Entity</span>
        </q-card-section>
        <q-card-section class="q-pt-lg">
          Are you sure you want to delete the entity "{{ entityToDelete?.name }}" (ID: {{ entityToDelete?.id }})?
          <span v-if="associatedBudgets.length > 0"> This entity has {{ associatedBudgets.length }} associated budget(s), which must be deleted first. </span>
          <span v-else>This action cannot be undone.</span>
        </q-card-section>
        <q-card-actions>
          <q-space></q-space>
          <q-btn color="grey" variant="text" @click="showDeleteEntityDialog = false">Cancel</q-btn>
          <q-btn color="negative" variant="flat" @click="deleteEntity" :disabled="associatedBudgets.length > 0">Delete</q-btn>
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
      />
    </q-dialog>

  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from "vue";
import { auth } from "../firebase/init";
import { dataAccess } from "../dataAccess";
import { Timestamp } from "firebase/firestore";
import type { Family, PendingInvite, Entity, BudgetInfo, ImportedTransactionDoc, Transaction, ImportedTransaction } from "../types";
import { useFamilyStore } from "../store/family";
import EntityForm from "../components/EntityForm.vue";
import { useQuasar, QSpinner } from 'quasar';
import { v4 as uuidv4 } from "uuid";

const familyStore = useFamilyStore();
const $q = useQuasar();
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
const budgets = ref<BudgetInfo[]>([]);
const importedTransactionDocs = ref<ImportedTransactionDoc[]>([]);
const showDeleteDialog = ref(false);
const transactionDocToDelete = ref<ImportedTransactionDoc | null>(null);
const showDeleteBudgetDialog = ref(false);
const budgetToDelete = ref<BudgetInfo | null>(null);
const showDeleteEntityDialog = ref(false);
const showEntityDialog = ref(false);
const entityToDelete = ref<Entity | null>(null);
const associatedBudgets = ref<BudgetInfo[]>([]);
const validatingBudgets = ref(false);
const validatingImports = ref(false);
const syncing = ref(false);
const syncingIncremental = ref(false);
const syncingUsers = ref(false);
const syncingFamilies = ref(false);
const syncingAccounts = ref(false);
const syncingSnapshots = ref(false);
const syncSinceDate = ref<string>(''); // format: YYYY-MM-DD
const syncSinceTime = ref<string>(''); // format: HH:mm (24h)

const entities = computed(() => family.value?.entities || []);

// Recalculate carryover controls
const recalcEntityId = ref<string | null>(null);
const recalcStartMonth = ref<string | null>(null);
const recalcLoading = ref(false);

const entityOptions = computed(() =>
  entities.value.map((e) => ({ label: e.name, value: e.id }))
);

const startMonthOptions = computed(() => {
  if (!recalcEntityId.value) return [] as string[];
  const months = budgets.value
    .filter((b) => b.entityId === recalcEntityId.value)
    .map((b) => b.month)
    .sort((a, b) => a.localeCompare(b));
  // Deduplicate while preserving order
  return Array.from(new Set(months));
});

function errorMessage(error: unknown): string {
  return error instanceof Error
    ? error.message
    : typeof error === 'string'
    ? error
    : JSON.stringify(error);
}

const transactionDocColumns = [
  { name: 'id', label: 'Document ID', field: 'id', sortable: true },
  {
    name: 'count',
    label: 'Transaction Count',
    field: (row: ImportedTransactionDoc) => row.importedTransactions?.length || 0,
    sortable: true,
  },
  { name: 'account', label: 'Account Info', field: 'account', sortable: false },
  { name: 'createdAt', label: 'Created At', field: 'createdAt', sortable: true },
  { name: 'actions', label: 'Actions', field: 'actions', sortable: false },
];

const budgetColumns = [
  { name: 'budgetId', label: 'Budget ID', field: 'budgetId', sortable: true },
  { name: 'month', label: 'Month', field: 'month', sortable: true },
  { name: 'entityName', label: 'Entity Name', field: 'entityId', sortable: false },
  { name: 'transactionCount', label: 'Transaction Count', field: 'transactionCount', sortable: true },
  { name: 'actions', label: 'Actions', field: 'actions', sortable: false },
];

onMounted(async () => {
  const currentUser = auth.currentUser;
  if (!currentUser) {
    showSnackbar("Please log in to view settings", "negative");
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

    // Seed defaults for recalc controls when obvious
    if (!recalcEntityId.value && budgets.value.length > 0) {
      // Prefer the entity with most budgets
      const counts = budgets.value.reduce((acc, b) => {
        const key = b.entityId || 'family';
        acc[key] = (acc[key] || 0) + 1;
        return acc;
      }, {} as Record<string, number>);
      const best = Object.entries(counts).sort((a, b) => b[1] - a[1])[0]?.[0];
      if (best && best !== 'family') recalcEntityId.value = best;
    }
  } catch (error: unknown) {
    showSnackbar(`Error loading data: ${errorMessage(error)}`, "negative");
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
    showSnackbar("Please log in to invite users", "negative");
    return;
  }

  const normalizedEmail = inviteEmail.value.toLowerCase().trim();
  if (!normalizedEmail) {
    showSnackbar("Please enter an email address", "negative");
    return;
  }

  if (normalizedEmail === user.email?.toLowerCase()) {
    showSnackbar("You cannot invite yourself", "negative");
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
  } catch (error: unknown) {
    showSnackbar(`Error inviting user: ${errorMessage(error)}`, "negative");
  } finally {
    inviting.value = false;
  }
}

async function removeMember(uid: string) {
  const user = auth.currentUser;
  if (!user || !family.value || !uid) {
    showSnackbar("Cannot remove member: invalid family or user data", "negative");
    return;
  }

  try {
    await dataAccess.removeFamilyMember(family.value.id, uid);
    showSnackbar("Member removed");
    await loadAllData();
  } catch (error: unknown) {
    showSnackbar(`Error removing member: ${errorMessage(error)}`, "negative");
  }
}

async function sendVerificationEmail() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar("Please log in to resend verification email", "negative");
    return;
  }

  resending.value = true;
  try {
    await dataAccess.resendVerificationEmail();
    showSnackbar("Verification email sent. Please check your inbox.", "success");
  } catch (error: unknown) {
    showSnackbar(`Error sending verification email: ${errorMessage(error)}`, "negative");
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
      showSnackbar("Cannot delete entity: associated budgets exist. Delete budgets first.", "negative");
      return;
    }

    await familyStore.deleteEntity(family.value.id, entityToDelete.value.id);
    showSnackbar(`Entity "${entityToDelete.value.name}" deleted successfully`, "success");
    await loadAllData();
  } catch (error: unknown) {
    console.error("Error deleting entity:", error);
    showSnackbar(`Error deleting entity: ${errorMessage(error)}`, "negative");
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
    showSnackbar(`Transaction document ${docId} deleted successfully`, "success");
  } catch (error: unknown) {
    console.error("Error deleting transaction document:", error);
    showSnackbar(`Error deleting transaction document: ${errorMessage(error)}`, "negative");
  } finally {
    showDeleteDialog.value = false;
    transactionDocToDelete.value = null;
  }
}

function getEntityName(entityId: string): string {
  const entity = family.value?.entities?.find((e) => e.id === entityId);
  return entity ? entity.name : "Unknown";
}

function confirmDeleteBudget(budget: BudgetInfo) {
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
  } catch (error: unknown) {
    console.error("Error deleting budget:", error);
    showSnackbar(`Error deleting budget: ${errorMessage(error)}`, "negative");
  } finally {
    showDeleteBudgetDialog.value = false;
    budgetToDelete.value = null;
  }
}

async function validateBudgetTransactions() {
  $q.loading.show({
    message: 'Validating budget transactions...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });
  validatingBudgets.value = true;
  try {
    const currentUser = auth.currentUser;
    if (!currentUser) {
      showSnackbar('User not authenticated', 'negative');
      return;
    }

    const budgetsList = await dataAccess.loadAccessibleBudgets(currentUser.uid);
    const fullBudgets = await Promise.all(
      budgetsList.map((b) => dataAccess.getBudget(b.budgetId))
    );
    const seenBudgetIds = new Set<string>();
    const budgetUpdates: { budgetId: string; transaction: Transaction; oldId?: string }[] = [];

    fullBudgets.forEach((b) => {
      b?.transactions?.forEach((tx) => {
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
  } catch (error: unknown) {
    console.error('Error validating budgets:', error);
    showSnackbar(`Error validating budgets: ${errorMessage(error)}`, 'negative');
  } finally {
    $q.loading.hide();
    validatingBudgets.value = false;
  }
}

async function validateImportedTransactions() {
  $q.loading.show({
    message: 'Validating imported transactions...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });
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
  } catch (error: unknown) {
    console.error('Error validating imported transactions:', error);
    showSnackbar(`Error validating imported transactions: ${errorMessage(error)}`, 'negative');
  } finally {
    $q.loading.hide();
    validatingImports.value = false;
  }
}

function formatDate(
  timestamp: Timestamp | { seconds: number; nanoseconds: number } | string | Date | null | undefined,
): string {
  if (!timestamp) return 'N/A';
  if (typeof timestamp === 'object') {
    if (timestamp instanceof Timestamp) {
      try {
        return timestamp.toDate().toLocaleString();
      } catch (err) {
        console.error('Failed to convert timestamp:', err);
      }
    }
    if ('seconds' in timestamp && 'nanoseconds' in timestamp) {
      const ms = Number(timestamp.seconds) * 1000 + Number(timestamp.nanoseconds) / 1e6;
      return new Date(ms).toLocaleString();
    }
  }
  const d = new Date(timestamp as string | number | Date);
  return isNaN(d.getTime()) ? 'N/A' : d.toLocaleString();
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

function toErrorMessage(error: unknown): string {
  return error instanceof Error
    ? error.message
    : typeof error === 'string'
      ? error
      : JSON.stringify(error);
}

async function syncNow() {
  syncing.value = true;
  try {
    const msg = await dataAccess.syncFull();
    showSnackbar(msg || 'Sync completed successfully', 'positive');
  } catch (error: unknown) {
    console.error('Sync error:', error);
    const msg = toErrorMessage(error);
    showSnackbar(`Sync failed: ${msg}`, 'negative');
  } finally {
    syncing.value = false;
  }
}

async function syncIncrementalNow() {
  if (!syncSinceDate.value) {
    showSnackbar('Please select a date for incremental sync', 'warning');
    return;
  }
  try {
    syncingIncremental.value = true;
    const datePart = syncSinceDate.value;
    const timePart = syncSinceTime.value || '00:00';
    const local = new Date(`${datePart}T${timePart}:00`);
    const iso = local.toISOString();
    const serverMsg = await dataAccess.syncIncremental(iso);
    const label = serverMsg || `Incremental sync completed since ${datePart} ${syncSinceTime.value || '00:00'}`;
    showSnackbar(label, 'positive');
  } catch (error: unknown) {
    console.error('Incremental sync error:', error);
    const msg = toErrorMessage(error);
    showSnackbar(`Incremental sync failed: ${msg}`, 'negative');
  } finally {
    syncingIncremental.value = false;
  }
}

async function syncUsersNow() {
  syncingUsers.value = true;
  try {
    const msg = await dataAccess.syncUsers();
    showSnackbar(msg || 'User sync completed', 'positive');
  } catch (error: unknown) {
    console.error('User sync error:', error);
    const msg = toErrorMessage(error);
    showSnackbar(`User sync failed: ${msg}`, 'negative');
  } finally {
    syncingUsers.value = false;
  }
}

async function syncFamiliesNow() {
  syncingFamilies.value = true;
  try {
    const msg = await dataAccess.syncFamilies();
    showSnackbar(msg || 'Family sync completed', 'positive');
  } catch (error: unknown) {
    console.error('Family sync error:', error);
    const msg = toErrorMessage(error);
    showSnackbar(`Family sync failed: ${msg}`, 'negative');
  } finally {
    syncingFamilies.value = false;
  }
}

async function syncAccountsNow() {
  syncingAccounts.value = true;
  try {
    const msg = await dataAccess.syncAccounts();
    showSnackbar(msg || 'Account sync completed', 'positive');
  } catch (error: unknown) {
    console.error('Account sync error:', error);
    const msg = toErrorMessage(error);
    showSnackbar(`Account sync failed: ${msg}`, 'negative');
  } finally {
    syncingAccounts.value = false;
  }
}

async function syncSnapshotsNow() {
  syncingSnapshots.value = true;
  try {
    const msg = await dataAccess.syncSnapshots();
    showSnackbar(msg || 'Snapshot sync completed', 'positive');
  } catch (error: unknown) {
    console.error('Snapshot sync error:', error);
    const msg = toErrorMessage(error);
    showSnackbar(`Snapshot sync failed: ${msg}`, 'negative');
  } finally {
    syncingSnapshots.value = false;
  }
}

async function runRecalcCarryover() {
  if (!recalcEntityId.value || !recalcStartMonth.value) {
    showSnackbar('Select an entity and start month', 'warning');
    return;
  }
  try {
    recalcLoading.value = true;
    await dataAccess.recalculateCarryoverFrom(recalcEntityId.value, recalcStartMonth.value);
    showSnackbar('Carryforward recalculated successfully', 'positive');
    // Reload budgets so table reflects latest values
    await loadAllData();
  } catch (error: unknown) {
    const msg = toErrorMessage(error);
    showSnackbar(`Recalc failed: ${msg}`, 'negative');
  } finally {
    recalcLoading.value = false;
  }
}
</script>
