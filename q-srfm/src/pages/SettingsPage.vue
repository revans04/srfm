<!-- src/views/SettingsView.vue -->
<template>
  <q-page class="bg-grey-1 q-pa-lg">
    <h1 class="page-title">Settings</h1>

    <q-banner v-if="userEmail && !emailVerified" class="bg-warning text-white q-mb-lg">
      Your email ({{ userEmail }}) is not verified. Please check your inbox or resend the verification email.
      <template v-slot:action>
        <q-btn flat @click="sendVerificationEmail" :loading="resending">Resend</q-btn>
      </template>
    </q-banner>

    <q-tabs v-model="activeTab" color="primary" class="q-mt-md">
      <q-tab name="group" label="Manage Family/Group" />
      <q-tab name="entity" label="Manage Entities" />
      <q-tab name="manageTransactions" label="Manage Imports" />
      <q-tab name="manageBudgets" label="Manage Budgets" />
    </q-tabs>

    <q-tab-panels v-model="activeTab" class="bg-transparent">
      <!-- Group Management Tab -->
      <q-tab-panel name="group">
        <q-card class="q-mt-md">
          <q-card-section>Family/Group Information</q-card-section>
          <q-card-section>
            <div class="text-subtitle2 q-mb-sm">Members</div>
            <q-list separator bordered class="rounded-md q-mb-md">
              <q-item v-for="member in acceptedMembers" :key="member.uid">
                <q-item-section avatar>
                  <q-avatar color="primary" text-color="white" size="36px">
                    {{ member.email?.[0]?.toUpperCase() || '?' }}
                  </q-avatar>
                </q-item-section>
                <q-item-section>
                  <q-item-label :class="{ 'text-weight-bold': member.role === 'Admin' }">
                    {{ member.email }}
                  </q-item-label>
                  <q-item-label caption>
                    Last active: {{ formatDate(member.lastAccessed) || 'Never' }}
                  </q-item-label>
                </q-item-section>
                <q-item-section side>
                  <div class="row items-center q-gutter-sm">
                    <q-chip
                      :color="member.role === 'Admin' ? 'primary' : 'grey-4'"
                      :text-color="member.role === 'Admin' ? 'white' : 'dark'"
                      dense
                      size="sm"
                    >
                      {{ member.role === 'Admin' ? 'Owner' : 'Member' }}
                    </q-chip>
                    <q-btn
                      v-if="member.uid !== user.uid"
                      flat
                      round
                      dense
                      icon="person_remove"
                      color="negative"
                      size="sm"
                      @click="removeMember(member.uid)"
                      style="min-width: 44px; min-height: 44px;"
                    >
                      <q-tooltip>Remove member</q-tooltip>
                    </q-btn>
                  </div>
                </q-item-section>
              </q-item>
              <q-item v-for="invite in pendingInvites" :key="invite.token">
                <q-item-section avatar>
                  <q-avatar color="grey-4" text-color="grey-7" size="36px">
                    <q-icon name="mail_outline" />
                  </q-avatar>
                </q-item-section>
                <q-item-section>
                  <q-item-label>{{ invite.inviteeEmail }}</q-item-label>
                  <q-item-label caption>Invited {{ formatDate(invite.createdAt) }}</q-item-label>
                </q-item-section>
                <q-item-section side>
                  <q-chip color="warning" text-color="dark" dense size="sm">Pending</q-chip>
                </q-item-section>
              </q-item>
              <q-item v-if="!acceptedMembers.length && !pendingInvites.length">
                <q-item-section class="text-muted">No members yet. Invite someone below.</q-item-section>
              </q-item>
            </q-list>
            <div class="text-subtitle2 q-mb-sm">Invite New Member</div>
            <q-form @submit.prevent="inviteMember" class="row items-start q-gutter-sm">
              <q-input v-model="inviteEmail" label="Email address" type="email" required dense outlined class="col" aria-required="true"></q-input>
              <q-btn type="submit" color="primary" :loading="inviting" class="col-auto">Invite</q-btn>
            </q-form>
          </q-card-section>
        </q-card>

      </q-tab-panel>

      <!-- Entity Management Tab -->
      <q-tab-panel name="entity">
        <q-card class="q-mt-md">
          <q-card-section>Entities</q-card-section>
          <q-card-section>
            <div class="row q-gutter-sm q-mb-lg items-center">
              <q-btn color="primary" @click="openCreateEntityDialog">Add Entity</q-btn>
              <!-- Bookkeeper escape hatch: hops into the same one-page seed
                   form used for first-run onboarding, which atomically creates
                   the entity + an Income group + a starter budget for the
                   current month. Saves the back-and-forth of dialog → blank
                   budget → manually picking categories. -->
              <q-btn
                flat
                no-caps
                color="primary"
                icon="bolt"
                label="Quick setup with starter budget"
                :to="{ path: '/setup', query: { mode: 'add-entity' } }"
              >
                <q-tooltip>One-page form that creates the entity and seeds a starter budget in a single step.</q-tooltip>
              </q-btn>
            </div>
            <q-list>
              <q-item v-for="entity in entities" :key="entity.id">
                <div class="row dense">
                  <div class="col">
                    {{ entity.name }} ({{ entity.type }}) - Owner: {{ entity.members.find((m) => m.role === "Admin")?.email || "N/A" }}
                    <q-chip v-if="entity.templateBudget" color="positive" size="sm" class="q-ml-sm">Has Template</q-chip>
                  </div>
                  <div class="col col-auto">
                    <q-btn flat round dense color="primary" @click="openEditEntityDialog(entity)" icon="edit">
                      <q-tooltip>Edit entity</q-tooltip>
                    </q-btn>
                  </div>
                  <div class="col col-auto">
                    <q-btn flat round dense @click="confirmDeleteEntity(entity)" color="negative" icon="delete_outline">
                      <q-tooltip>Delete entity</q-tooltip>
                    </q-btn>
                  </div>
                </div>
              </q-item>
              <q-item v-if="!entities.length" class="text-grey-7">
                <q-item-section>
                  No entities yet.
                  <q-btn flat dense no-caps color="primary" label="Add your first entity →" class="q-ml-xs" @click="openCreateEntityDialog" />
                </q-item-section>
              </q-item>
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
                  class="shadow-1"
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
                        dense
                        flat
                        color="negative"
                        @click.stop="confirmDeleteTransactionDoc(props.row)"
                        title="Delete Transaction Document"
                        icon="delete_outline"
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
                  class="shadow-1"
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
                        dense
                        flat
                        color="negative"
                        @click.stop="confirmDeleteBudget(props.row)"
                        title="Delete Budget"
                        icon="delete_outline"
                      />
                    </q-td>
                  </template>
                </q-table>
                <div class="q-mt-lg">
                  <div class="text-subtitle1 q-mb-sm">Merge Budgets</div>
                  <div class="row q-col-gutter-md">
                    <div class="col-12 col-sm-4">
                      <q-select
                        v-model="targetBudgetId"
                        :options="budgetOptions"
                        option-label="label"
                        option-value="value"
                        emit-value
                        map-options
                        label="Budget to Keep"
                        dense
                        clearable
                        :disable="mergingBudgets"
                      />
                    </div>
                    <div class="col-12 col-sm-4">
                      <q-select
                        v-model="sourceBudgetId"
                        :options="budgetOptions"
                        option-label="label"
                        option-value="value"
                        emit-value
                        map-options
                        label="Budget to Merge"
                        dense
                        clearable
                        :disable="mergingBudgets"
                      />
                    </div>
                  </div>
                  <q-banner v-if="mergeValidationMessage" class="bg-warning text-dark q-pa-sm q-mt-sm" dense>
                    {{ mergeValidationMessage }}
                  </q-banner>
                  <q-btn
                    color="primary"
                    class="q-mt-md"
                    label="Merge Selected Budgets"
                    @click="mergeSelectedBudgets"
                    :disable="mergeDisabled"
                    :loading="mergingBudgets"
                  />
                </div>
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
          <q-btn color="grey" flat @click="showDeleteDialog = false">Cancel</q-btn>
          <q-btn color="negative" flat @click="deleteTransactionDoc">Delete</q-btn>
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
          <q-btn color="grey" flat @click="showDeleteBudgetDialog = false">Cancel</q-btn>
          <q-btn color="negative" flat @click="deleteBudget">Delete</q-btn>
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
          <q-btn color="grey" flat @click="showDeleteEntityDialog = false">Cancel</q-btn>
          <q-btn color="negative" flat @click="deleteEntity" :disabled="associatedBudgets.length > 0">Delete</q-btn>
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
import { ref, onMounted, onUnmounted, computed, watch } from "vue";
import { auth } from "../firebase/init";
import { dataAccess } from "../dataAccess";
import { Timestamp } from "firebase/firestore";
import type { Family, PendingInvite, Entity, BudgetInfo, ImportedTransactionDoc, Transaction, ImportedTransaction } from "../types";
import { useFamilyStore } from "../store/family";
import EntityForm from "../components/EntityForm.vue";
import { useQuasar, QSpinner } from 'quasar';
import { v4 as uuidv4 } from "uuid";
import { getImportedTransactionDate } from '../utils/helpers';

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
const targetBudgetId = ref<string | null>(null);
const sourceBudgetId = ref<string | null>(null);
const mergingBudgets = ref(false);
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

const budgetOptions = computed(() =>
  budgets.value
    .filter((budget): budget is BudgetInfo & { budgetId: string } => typeof budget.budgetId === "string" && budget.budgetId.length > 0)
    .map((budget) => ({
      label: formatBudgetOptionLabel(budget),
      value: budget.budgetId,
    }))
    .sort((a, b) => b.label.localeCompare(a.label))
);

const selectedTargetBudget = computed(() =>
  budgets.value.find((budget) => budget.budgetId === targetBudgetId.value) || null
);

const selectedSourceBudget = computed(() =>
  budgets.value.find((budget) => budget.budgetId === sourceBudgetId.value) || null
);

const mergeValidationMessage = computed(() => {
  if (!selectedTargetBudget.value || !selectedSourceBudget.value) {
    return "";
  }

  if (targetBudgetId.value === sourceBudgetId.value) {
    return "Choose two different budgets to merge.";
  }

  if (selectedTargetBudget.value.month !== selectedSourceBudget.value.month) {
    return "Budgets must belong to the same month before merging.";
  }

  if ((selectedTargetBudget.value.entityId || "") !== (selectedSourceBudget.value.entityId || "")) {
    return "Budgets must belong to the same entity before merging.";
  }

  return "";
});

const mergeDisabled = computed(() => {
  if (mergingBudgets.value) {
    return true;
  }
  if (!selectedTargetBudget.value || !selectedSourceBudget.value) {
    return true;
  }
  return !!mergeValidationMessage.value;
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

watch(
  () => budgets.value.map((budget) => budget.budgetId),
  () => {
    syncSelectedBudgetSelections();
  }
);

onUnmounted(() => {
  dataAccess.unsubscribeAll();
});

async function loadAllData() {
  const user = auth.currentUser;
  if (!user) return;

  try {
    family.value = await familyStore.loadFamily(user.uid);
    if (family.value && family.value.members) {
      const ownerUid = family.value.ownerUid;
      acceptedMembers.value = (
        await Promise.all(
          family.value.members
            .filter((m) => m.uid)
            .map(async (m) => {
              let email = m.email;
              if (!email) {
                try {
                  const userData = await dataAccess.getUser(m.uid);
                  email = userData?.email || null;
                } catch { /* ignore lookup failure */ }
              }
              return {
                uid: m.uid,
                email: email || m.uid,
                role: m.uid === ownerUid ? 'Admin' : (m.role || 'Member'),
                lastAccessed: await dataAccess.getLastAccessed(m.uid),
              };
            })
        )
      ).filter((m) => m.uid !== undefined);
      pendingInvites.value = await dataAccess.getPendingInvites(user.uid);
    } else {
      acceptedMembers.value = [];
      pendingInvites.value = [];
    }

    // Load budgets and imported transaction docs
    budgets.value = await dataAccess.loadAccessibleBudgets(user.uid);
    syncSelectedBudgetSelections();
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

  const validTransactions = item.importedTransactions.filter((tx) => {
    const dateStr = getImportedTransactionDate(tx);
    return Boolean(dateStr) && !isNaN(new Date(dateStr).getTime());
  });

  if (validTransactions.length === 0) {
    return "No valid dates available";
  }

  const dates = validTransactions.map((tx) => new Date(getImportedTransactionDate(tx)));
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

function formatBudgetOptionLabel(budget: BudgetInfo): string {
  const parts: string[] = [];
  if (budget.month) {
    parts.push(budget.month);
  }

  const entityId = budget.entityId;
  const entityName = entityId ? getEntityName(entityId) : "Family Budget";
  if (entityName && entityName !== "Unknown") {
    parts.push(entityName);
  }

  if (budget.label) {
    parts.push(budget.label);
  }

  const transactionCount = budget.transactionCount ?? budget.transactions?.length ?? 0;
  parts.push(`${transactionCount} ${transactionCount === 1 ? "txn" : "txns"}`);

  return parts.join(" • ");
}

function syncSelectedBudgetSelections() {
  const budgetIds = new Set(budgets.value.map((budget) => budget.budgetId));
  if (targetBudgetId.value && !budgetIds.has(targetBudgetId.value)) {
    targetBudgetId.value = null;
  }
  if (sourceBudgetId.value && !budgetIds.has(sourceBudgetId.value)) {
    sourceBudgetId.value = null;
  }
}

function confirmDeleteBudget(budget: BudgetInfo) {
  budgetToDelete.value = budget;
  showDeleteBudgetDialog.value = true;
}

async function mergeSelectedBudgets() {
  if (!selectedTargetBudget.value || !selectedSourceBudget.value) {
    showSnackbar("Select both budgets before merging", "negative");
    return;
  }

  if (mergeValidationMessage.value) {
    showSnackbar(mergeValidationMessage.value, "negative");
    return;
  }

  const targetId = selectedTargetBudget.value.budgetId;
  const sourceId = selectedSourceBudget.value.budgetId;
  const successLabel = formatBudgetOptionLabel(selectedTargetBudget.value);
  if (!targetId || !sourceId) {
    showSnackbar("Unable to determine selected budgets", "negative");
    return;
  }

  mergingBudgets.value = true;
  try {
    await dataAccess.mergeBudgets(targetId, sourceId);
    showSnackbar(`Budgets for ${successLabel} merged successfully`);
    await loadAllData();
    resetMergeSelection();
  } catch (error: unknown) {
    console.error("Error merging budgets:", error);
    showSnackbar(`Error merging budgets: ${errorMessage(error)}`, "negative");
  } finally {
    mergingBudgets.value = false;
  }
}

function resetMergeSelection() {
  targetBudgetId.value = null;
  sourceBudgetId.value = null;
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

// Member "Last active" / invite "Invited on" — mm/dd/yyyy per design system
// (no time). If we later need relative time ("2h ago"), add a dedicated helper
// rather than mixing formats here.
function formatDate(
  timestamp: Timestamp | { seconds: number; nanoseconds: number } | string | Date | null | undefined,
): string {
  if (!timestamp) return 'N/A';
  let d: Date | null = null;
  if (typeof timestamp === 'object') {
    if (timestamp instanceof Timestamp) {
      try {
        d = timestamp.toDate();
      } catch (err) {
        console.error('Failed to convert timestamp:', err);
      }
    } else if ('seconds' in timestamp && 'nanoseconds' in timestamp) {
      const ms = Number(timestamp.seconds) * 1000 + Number(timestamp.nanoseconds) / 1e6;
      d = new Date(ms);
    }
  }
  if (!d) d = new Date(timestamp as string | number | Date);
  if (!d || isNaN(d.getTime())) return 'N/A';
  return d.toLocaleDateString('en-US', {
    month: '2-digit',
    day: '2-digit',
    year: 'numeric',
  });
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
