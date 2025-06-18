<!-- src/views/SetupWizard.vue -->
<template>
  <q-page fluid>
    <h1>Setup Wizard</h1>
    <div class="text-subtitle-1 mb-2">Step {{ currentStepIndex + 1 }} of {{ wizardSteps.length }}</div>
    <q-stepper v-model="currentStepValue" class="mt-4 wizard-stepper" hide-actions aria-label="Setup Wizard Steps">
      <template v-for="(step, index) in wizardSteps" :key="`item-${step.value}`">
        <q-stepper-item
          :value="step.value"
          :editable="isStepEditable(step.value)"
          :complete="isStepComplete(step.value)"
          :title="step.title"
          :icon="getStepIcon(step.value)"
          edit-icon="edit"
          error-icon="error"
        >
        </q-stepper-item>
      </template>

      <q-stepper-window class="mt-1">
        <template v-for="step in wizardSteps" :key="`window-${step.value}`">
          <q-stepper-window-item :value="step.value" class="wizard-step-content-wrapper">
            <template v-if="step.value === 'family'">
              <q-card flat class="wizard-step-card">
                <q-card-section class="text-h5">{{ step.title }}</q-card-section>
                <q-card-section>
                  <group-naming-form @family-created="handleFamilyCreated" />
                </q-card-section>
                <q-card-actions class="pa-4">
                  <q-space />
                </q-card-actions>
              </q-card>
            </template>

            <template v-else-if="step.value === 'entities'">
              <q-card flat class="wizard-step-card">
                <q-card-section class="text-h5">{{ step.title }}</q-card-section>
                <q-card-section>
                  An entity could be a family budget, rental property, or some kind of business. The reason you would want to add an entity would be to track
                  budgets and expenses related to that entity. You will also be able to run reports that are isolated to entities. Each entity will have monthly
                  budgets with expense categories. Each entity should also have some income source.<br /><br />
                  <q-list v-if="savedEntities.length > 0" class="mb-4 full-width">
                    <div class="row full-width align-center" >
                      <div class="col text-h6" >Saved Entities</div>
                      <div class="col text-right col-auto" >
                        <q-btn density="compact" variant="plain" icon="add" color="success" @click="initNewEntity" aria-label="Add New Entity"></q-btn>
                      </div>
                    </div>
                    <q-item
                      v-for="entity in savedEntities"
                      :key="entity.id"
                      @click="selectEntity(entity.id)"
                      :class="entity.id === selectedEntityId ? 'selected-entity' : ''"
                      class="entity-list-item"
                    >
                      <div>{{ entity.name }} ({{ entity.type }})</div>
                    </q-item>
                  </q-list>
                  <q-btn v-else color="primary" @click="initNewEntity" class="mb-4" aria-label="Add New Entity"> Add New Entity </q-btn>
                  <entity-form
                    v-if="selectedEntityId !== ''"
                    :key="selectedEntityId"
                    ref="entityFormRef"
                    :entity-id="selectedEntityId"
                    @cancel="unselectEntity"
                    @save="onEntitySaved"
                    @update:unsaved="updateUnsavedChanges"
                  />
                </q-card-section>
                <q-card-actions class="pa-4">
                  <q-btn color="secondary" variant="text" @click="navigateStep('back')"> Back </q-btn>
                  <q-space />
                  <q-btn color="primary" @click="navigateStep('next')" class="ml-2"> Next </q-btn>
                </q-card-actions>
              </q-card>
            </template>

            <template v-else-if="step.accountType">
              <q-card flat class="wizard-step-card">
                <q-card-section class="text-h5 d-flex justify-space-between align-center">
                  <span>{{ step.title }}</span>
                </q-card-section>
                <q-card-section>
                  <p v-if="step.accountType == AccountType.Bank" class="mb-4">
                    Add your bank accounts (savings or checking) here. Adding accounts here will allow you to track their value over time (via account
                    snapshots) and you'll be able to track transactions for those accounts for reconciliation purposes. If you would rather, you can also add
                    accounts via import from Data Management later.
                  </p>
                  <p v-else-if="step.accountType == AccountType.CreditCard" class="mb-4">
                    Add your credit card accounts here. Because your credit cards represent liabilities (loans), enter positive values and we'll treat them as
                    debts (with negative values) automatically. For instance, if you currently owe $1200 on a credit card, enter the value as 1200 (positive).
                    We'll display that as a negative value because it is an owed value.
                  </p>
                  <p v-else-if="step.accountType == AccountType.Investment" class="mb-4">
                    If you have any investment accounts (retirement, ESAs, annuities, etc.), add them here. We'll use these when calculating and tracking your
                    net worth.
                  </p>
                  <p v-else-if="step.accountType == AccountType.Property" class="mb-4">
                    This section is for any property that you might own (even if you have a loan or mortgage on the property). These properties could be real
                    estate, cars, jewelry, art, or other items which you would like to be included in your net worth.
                  </p>
                  <p v-else class="mb-4">Add your {{ step.accountType?.toLowerCase() }} accounts here. You can add as many as you need.</p>
                  <AccountList
                    :accounts="getAccountsByType(step.accountType!)"
                    :type="step.accountType!"
                    @add="openWizardAccountDialog(step.accountType!)"
                    @edit="editWizardAccount"
                    @delete="removeWizardAccount"
                  />
                </q-card-section>
                <q-card-actions class="pa-4">
                  <q-btn color="secondary" variant="text" @click="navigateStep('back')"> Back </q-btn>
                  <q-space />
                  <q-btn v-if="!isLastStep && step.accountType" color="secondary" variant="text" @click="navigateStep('next')" class="ml-2"> Skip </q-btn>
                  <q-btn v-if="!isLastStep" color="primary" @click="navigateStep('next')" class="ml-2"> Next </q-btn>
                  <q-btn v-else color="primary" @click="finishSetup" :loading="finishingSetup" class="ml-2"> Finish </q-btn>
                </q-card-actions>
              </q-card>
            </template>
          </q-stepper-window-item>
        </template>
      </q-stepper-window>
    </q-stepper>

    <!-- Entity Unsaved Changes Dialog -->
    <q-dialog v-model="showUnsavedEntityDialog" max-width="450px" persistent></q-dialog>

    <!-- Account Dialog -->
    <q-dialog v-model="showWizardAccountDialog" max-width="600px" persistent>
      <q-card>
        <q-card-section> {{ editMode ? "Edit" : "Add" }} {{ newWizardAccount.type }} Account </q-card-section>
        <q-card-section>
          <AccountForm
            :account-type="newWizardAccount.type as AccountType"
            :account="editMode ? newWizardAccount : undefined"
            :show-personal-option="false"
            @save="saveWizardAccount"
            @cancel="closeWizardAccountDialog"
          />
        </q-card-section>
      </q-card>
    </q-dialog>

    <q-snackbar v-model="snackbar.show" :color="snackbar.color" timeout="3000" location="top right"></q-snackbar>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick } from "vue";
import { useRouter } from "vue-router";
import { auth } from "../firebase/init";
import { dataAccess } from "../dataAccess";
import { useFamilyStore } from "../store/family";
import EntityForm from "../components/EntityForm.vue";
import GroupNamingForm from "../components/GroupNamingForm.vue";
import AccountList from "../components/AccountList.vue";
import AccountForm from "../components/AccountForm.vue"; // Import AccountForm directly
import { v4 as uuidv4 } from "uuid";
import { Timestamp } from "firebase/firestore";
import { AccountType, type Family, type Entity, type Account, EntityType } from "../types";

interface WizardStep {
  value: string;
  title: string;
  accountType?: AccountType;
}

const router = useRouter();
const familyStore = useFamilyStore();

const snackbar = ref({ show: false, text: "", color: "success" });

// Messages for consistency
const messages = {
  entityRequired: "Please create at least one entity before proceeding.",
  accountAdded: (type: string) => `${type} account added successfully.`,
  accountRemoved: (name: string) => `Account "${name}" removed.`,
  accountUpdated: (type: string) => `${type} account updated successfully.`,
  entityAdded: (name: string) => `Entity "${name}" added successfully.`,
};

// Stepper State
const initialSteps: WizardStep[] = [
  { value: "family", title: "Family Setup" },
  { value: "entities", title: "Entities" },
];
const accountTypeOrder = [AccountType.Bank, AccountType.CreditCard, AccountType.Investment, AccountType.Property, AccountType.Loan];
const accountSteps = computed<WizardStep[]>(() =>
  accountTypeOrder.map((type) => ({
    value: `account-${type.toLowerCase().replace(/\s+/g, "-")}`,
    title:
      type === AccountType.CreditCard
        ? "Credit Cards"
        : type === AccountType.Investment
        ? "Investments"
        : type === AccountType.Property
        ? "Properties"
        : type === AccountType.Loan
        ? "Loans"
        : type,
    accountType: type,
  }))
);
const wizardSteps = computed<WizardStep[]>(() => [...initialSteps, ...accountSteps.value]);
const currentStepValue = ref<string>(wizardSteps.value[0].value);
const highestStepReached = ref<string>(wizardSteps.value[0].value);

// Entity Step State
const selectedEntityId = ref("");
const entityFormRef = ref<InstanceType<typeof EntityForm> | null>(null);
const hasUnsavedEntityChanges = ref(false);
const savingEntity = ref(false);
const showUnsavedEntityDialog = ref(false);
const pendingNavigationArgs = ref<{
  direction: "next" | "back" | "select_entity";
  entityId?: string;
} | null>(null);

// Account Step State
const showWizardAccountDialog = ref(false);
const editMode = ref(false);
const accountsData = ref<Account[]>([]);
const newWizardAccount = ref<Partial<Account> & { type: AccountType | string }>({
  name: "",
  type: AccountType.Bank,
  accountNumber: "",
  balance: 0,
  tempId: uuidv4(),
});
const finishingSetup = ref(false);

// Global State
const family = ref<Family | null>(null);

// Computed Properties for Stepper
const currentStepIndex = computed(() => wizardSteps.value.findIndex((step) => step.value === currentStepValue.value));
const isLastStep = computed(() => currentStepIndex.value === wizardSteps.value.length - 1);

const isStepComplete = (stepValue: string): boolean => {
  const stepIdx = wizardSteps.value.findIndex((s) => s.value === stepValue);
  const highestReachedIdx = wizardSteps.value.findIndex((s) => s.value === highestStepReached.value);
  return stepIdx < highestReachedIdx;
};

const isStepEditable = (stepValue: string): boolean => {
  const stepIdx = wizardSteps.value.findIndex((s) => s.value === stepValue);
  const highestReachedIdx = wizardSteps.value.findIndex((s) => s.value === highestStepReached.value);
  return stepIdx <= highestReachedIdx || stepValue === currentStepValue.value;
};

// Step Icons
function getStepIcon(stepValue: string): string {
  const icons = {
    family: "group",
    entities: "domain",
    "account-bank": "account_balance",
    "account-creditcard": "credit_card",
    "account-investment": "show_chart",
    "account-property": "home",
    "account-loan": "handshake",
  };
  return icons[stepValue] || "circle";
}

watch(currentStepValue, (newValue) => {
  const newIndex = wizardSteps.value.findIndex((s) => s.value === newValue);
  const highestReachedIdx = wizardSteps.value.findIndex((s) => s.value === highestStepReached.value);
  if (newIndex > highestReachedIdx) {
    highestStepReached.value = newValue;
  }
});

// Lifecycle
onMounted(async () => {
  const user = auth.currentUser;
  if (!user) {
    showSnackbarMessage("Please log in to complete setup.", "error");
    router.push("/login");
    return;
  }
  if (familyStore.family) {
    family.value = familyStore.family;
  } else {
    try {
      const fam = await familyStore.getFamily();
      if (fam) {
        family.value = fam;
      }
    } catch (error: any) {
      console.error("Error loading family:", error);
    }
  }

  if (family.value) {
    try {
      accountsData.value = await dataAccess.getAccounts(family.value.id);
    } catch (error: any) {
      console.error("Error loading accounts:", error);
      showSnackbarMessage(`Error loading accounts: ${error.message}`, "error");
    }
  }
});

// Navigation
function handleFamilyCreated() {
  if (familyStore.family) {
    family.value = familyStore.family;
    navigateStep("next");
  } else {
    showSnackbarMessage("Family data not available. Please try again.", "error");
  }
}

function canNavigateFromEntities(): boolean {
  if (hasUnsavedEntityChanges.value && selectedEntityId.value !== "") {
    pendingNavigationArgs.value = { direction: "next" };
    showUnsavedEntityDialog.value = true;
    return false;
  }
  const currentEntities = family.value?.entities || [];
  if (currentEntities.length === 0) {
    showSnackbarMessage(messages.entityRequired, "warning");
    return false;
  }
  return true;
}

async function navigateStep(direction: "next" | "back") {
  const currentIndex = currentStepIndex.value;
  let targetStepValue: string | undefined;

  if (direction === "next") {
    if (currentIndex >= wizardSteps.value.length - 1) return;
    if (wizardSteps.value[currentIndex].value === "entities" && !canNavigateFromEntities()) return;
    targetStepValue = wizardSteps.value[currentIndex + 1].value;
  } else {
    if (currentIndex <= 0) return;
    targetStepValue = wizardSteps.value[currentIndex - 1].value;
  }

  if (targetStepValue) {
    currentStepValue.value = targetStepValue;
    if (wizardSteps.value[currentIndex].value === "entities" && direction === "next") {
      hasUnsavedEntityChanges.value = false;
    }
  }
}

function cancelPendingNavigation() {
  pendingNavigationArgs.value = null;
  showUnsavedEntityDialog.value = false;
}

// Entity Functions
const savedEntities = computed(() => {
  return family.value?.entities || [];
});

function selectEntity(id: string) {
  if (hasUnsavedEntityChanges.value && selectedEntityId.value !== "" && selectedEntityId.value !== id) {
    pendingNavigationArgs.value = { direction: "select_entity", entityId: id };
    showUnsavedEntityDialog.value = true;
  } else if (selectedEntityId.value !== id) {
    selectedEntityId.value = id;
    hasUnsavedEntityChanges.value = false;
  }
}

function unselectEntity() {
  selectedEntityId.value = "";
  hasUnsavedEntityChanges.value = false;
}

function initNewEntity() {
  if (hasUnsavedEntityChanges.value && selectedEntityId.value !== "") {
    pendingNavigationArgs.value = { direction: "select_entity", entityId: uuidv4() };
    showUnsavedEntityDialog.value = true;
  } else {
    selectedEntityId.value = uuidv4();
    hasUnsavedEntityChanges.value = false;
  }
}

function onEntitySaved() {
  hasUnsavedEntityChanges.value = false;
  const currentId = selectedEntityId.value;
  if (familyStore.family) {
    family.value = JSON.parse(JSON.stringify(familyStore.family));
    const saved = family.value.entities.find((e) => e.id === currentId);
    showSnackbarMessage(messages.entityAdded(saved?.name || "Entity"), "success");
  }

  selectedEntityId.value = "";

  if (pendingNavigationArgs.value && pendingNavigationArgs.value.direction !== "select_entity") {
    executePendingNavigation();
  } else if (pendingNavigationArgs.value && pendingNavigationArgs.value.direction === "select_entity" && pendingNavigationArgs.value.entityId === currentId) {
    pendingNavigationArgs.value = null;
  }
}

function updateUnsavedChanges(unsaved: boolean) {
  hasUnsavedEntityChanges.value = unsaved;
}

async function saveEntityAndProceed() {
  if (entityFormRef.value) {
    savingEntity.value = true;
    try {
      await entityFormRef.value.save();
    } catch (error) {
      showSnackbarMessage("Failed to save entity.", "error");
      cancelPendingNavigation();
    } finally {
      savingEntity.value = false;
    }
  }
}

function discardChangesAndProceed() {
  hasUnsavedEntityChanges.value = false;
  if (entityFormRef.value && typeof entityFormRef.value.resetFormInternal === "function") {
    entityFormRef.value.resetFormInternal();
  } else if (entityFormRef.value && typeof entityFormRef.value.resetForm === "function") {
    entityFormRef.value.resetForm();
  }
  showUnsavedEntityDialog.value = false;
  executePendingNavigation();
}

function executePendingNavigation() {
  if (!pendingNavigationArgs.value) return;
  const { direction, entityId } = pendingNavigationArgs.value;
  const currentPendingArgs = pendingNavigationArgs.value;
  pendingNavigationArgs.value = null;

  if (currentPendingArgs.direction === "next" || currentPendingArgs.direction === "back") {
    navigateStep(currentPendingArgs.direction);
  } else if (currentPendingArgs.direction === "select_entity" && currentPendingArgs.entityId) {
    selectedEntityId.value = currentPendingArgs.entityId;
    hasUnsavedEntityChanges.value = false;
  }
  showUnsavedEntityDialog.value = false;
}

// Account Step Functions
function getAccountsByType(type: AccountType | string | undefined): Account[] {
  if (!type) return [];
  return accountsData.value.filter((acc) => acc.type === type);
}

function openWizardAccountDialog(type: AccountType) {
  editMode.value = false;
  newWizardAccount.value = {
    name: "",
    type: type,
    accountNumber: "",
    balance: 0,
    tempId: uuidv4(),
    category: type === AccountType.CreditCard || type === AccountType.Loan ? "Liability" : "Asset",
  };
  showWizardAccountDialog.value = true;
}

function editWizardAccount(account: Account) {
  editMode.value = true;
  newWizardAccount.value = { ...account, type: account.type };
  showWizardAccountDialog.value = true;
}

function closeWizardAccountDialog() {
  showWizardAccountDialog.value = false;
  editMode.value = false;
}

function saveWizardAccount(account: Account, isPersonal: boolean) {
  const accountToAdd: Account = {
    id: account.id || account.tempId || uuidv4(),
    familyId: "",
    name: account.name,
    type: account.type as AccountType,
    category: account.category,
    accountNumber: account.accountNumber || "",
    balance: account.balance || 0,
    institution: account.institution || "",
    createdAt: account.createdAt instanceof Timestamp ? account.createdAt : Timestamp.fromDate(new Date()),
    updatedAt: Timestamp.fromDate(new Date()),
    tempId: account.tempId,
    details: account.details || {},
  };

  if (editMode.value) {
    const index = accountsData.value.findIndex((acc) => (acc.tempId || acc.id) === (account.tempId || account.id));
    if (index >= 0) {
      accountsData.value[index] = accountToAdd;
    }
    showSnackbarMessage(messages.accountUpdated(accountToAdd.type), "success");
  } else {
    accountsData.value.push(accountToAdd);
    showSnackbarMessage(messages.accountAdded(accountToAdd.type), "success");
  }

  closeWizardAccountDialog();
}

function removeWizardAccount(account: Account) {
  const keyToRemove = account.tempId || account.id;
  accountsData.value = accountsData.value.filter((acc) => (acc.tempId || acc.id) !== keyToRemove);
  showSnackbarMessage(messages.accountRemoved(account.name), "info");
}

// Finish Setup
async function finishSetup() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbarMessage("User not authenticated.", "error");
    return;
  }
  if (!family.value || !family.value.id) {
    showSnackbarMessage("Family data missing. Cannot complete setup.", "error");
    return;
  }

  finishingSetup.value = true;
  try {
    for (const accData of accountsData.value) {
      const accountId = accData.id || uuidv4();
      const newAccountToSave: Account = {
        ...accData,
        id: accountId,
        familyId: family.value!.id,
        createdAt: accData.createdAt instanceof Timestamp ? accData.createdAt : Timestamp.fromDate(new Date()),
        updatedAt: Timestamp.fromDate(new Date()),
      };
      delete (newAccountToSave as any).tempId;
      await dataAccess.saveAccount(family.value!.id, newAccountToSave);
    }
    showSnackbarMessage("Setup completed successfully! Redirecting...", "success");
    setTimeout(() => router.push("/dashboard"), 1500);
  } catch (error: any) {
    showSnackbarMessage(`Error saving accounts: ${error.message || "Unknown error"}`, "error");
    console.error("Error in finishSetup:", error);
  } finally {
    finishingSetup.value = false;
  }
}

// Snackbar Helper
function showSnackbarMessage(keyOrText: string, color: string = "success", ...args: any[]) {
  const text = messages[keyOrText] ? messages[keyOrText](...args) : keyOrText;
  snackbar.value = { show: true, text, color };
}
</script>

<style scoped>
.entity-list-item {
  cursor: pointer;
  transition: background-color 0.2s;
  border-left: 4px solid transparent;
}
.entity-list-item:hover {
  background-color: rgba(var(--v-theme-on-surface), 0.04);
}
.selected-entity {
  background-color: rgba(var(--v-theme-primary), 0.1) !important;
  border-left-color: rgb(var(--v-theme-primary)) !important;
  color: rgb(var(--v-theme-primary)) !important;
}

/* Horizontal Scrollable Stepper */
.wizard-stepper .v-stepper-header {
  overflow-x: auto;
  white-space: nowrap;
  scrollbar-width: thin;
  -ms-overflow-style: auto;
}

.wizard-stepper .v-stepper-header::-webkit-scrollbar {
  height: 8px;
}

.wizard-stepper .v-stepper-header::-webkit-scrollbar-thumb {
  background-color: rgba(var(--v-theme-primary), 0.5);
  border-radius: 4px;
}

.wizard-stepper .v-stepper-header::-webkit-scrollbar-track {
  background: rgba(var(--v-theme-surface-variant), 0.2);
}

.wizard-stepper .v-stepper-header .v-stepper-item {
  display: inline-block;
  flex: 0 0 auto;
  min-width: 150px;
}

/* Stepper Item Styles */
.v-stepper-item--complete {
  background-color: rgba(var(--v-theme-success), 0.1);
}

.v-stepper-item--selected {
  background-color: rgba(var(--v-theme-primary), 0.05);
}

/* Content Styles */
.wizard-step-content-wrapper .v-card.wizard-step-card {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  box-shadow: none;
  margin-top: 16px;
  margin-bottom: 16px;
  padding: 16px;
}

.v-card-text {
  padding-bottom: 24px;
}

.v-list.py-0 {
  padding-top: 0 !important;
  padding-bottom: 0 !important;
}

.pl-0 {
  padding-left: 0 !important;
}
</style>
