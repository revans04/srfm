<!-- src/components/EntityForm.vue -->
<template>
  <v-card density="compact">
    <v-card-title>{{ isEditing ? "Edit Entity" : "Create Entity" }}</v-card-title>
    <v-card-text>
      <v-form @submit.prevent="save">
        <!-- Entity Details -->
        <v-row :dense="true">
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="entityName"
              label="Entity Name"
              required
              density="compact"
              :rules="[(v) => !!v || 'Entity Name is required']"
            ></v-text-field>
          </v-col>
          <v-col cols="12" sm="6">
            <v-select
              v-model="entityType"
              label="Entity Type"
              :items="entityTypeOptions"
              required
              density="compact"
              :rules="[(v) => !!v || 'Entity Type is required']"
            ></v-select>
          </v-col>
          <v-col cols="12">
            <v-text-field v-model="entityEmail" label="Owner Email" density="compact" disabled></v-text-field>
          </v-col>
          <v-col cols="12">
            <v-select
              v-model="entityTaxFormIds"
              label="Applicable Tax Forms"
              :items="availableTaxForms"
              item-title="name"
              item-value="id"
              multiple
              chips
              closable-chips
              density="compact"
              hint="Select tax forms applicable to this entity (federal and state)"
              persistent-hint
            ></v-select>
          </v-col>
        </v-row>
        <br />
        <v-row>
          <v-col cols="8" class="text-subtitle-1 font-weight-bold">
            Budget Template
            <div v-if="isFormValid">{{ getBudgetInfo }}</div>
          </v-col>
          <v-col cols="4" class="text-right">
            <v-btn color="primary" @click="importCategories">Add from Type</v-btn>
          </v-col>
          <v-col cols="12">
            <!-- Help Section -->
            <v-expansion-panels class="mb-4">
              <v-expansion-panel title="Need help with creating a budget template?">
                <v-expansion-panel-text class="px-4">
                  <strong>Create Your First Budget Template</strong>
                  A budget template helps you plan how to use your money each month. Follow these simple steps:
                  <ol>
                    <li>
                      <strong>Add Your Income</strong>: List at least one source of money you receive regularly, like your salary or freelance earnings. The
                      Group should be Income and the category can be something like Salary or the name of the income source.
                      Example: "Monthly Salary: $3,000."
                    </li>
                    <li>
                      <strong>List Spending and Saving Categories</strong>: Create categories for where your money goes, such as rent, groceries, or savings.
                      Group similar categories together (e.g., "Housing" for rent and utilities, "Daily Needs" for groceries and gas).
                      <br><strong>Tip</strong>:
                      Click the "Add Categories from Type" button to get sample categories and groups based on your entity type. You can edit these to fit your
                      needs.
                    </li>
                    <li>
                      <strong>Assign Your Income</strong>: Divide your total income among your categories to cover all expenses and savings. Example: $1,000 for
                      rent, $500 for groceries, $500 for savings, etc. Make sure the total matches your income to avoid overspending.
                    </li>
                    <li>
                      <strong>Review and Save</strong>: Check that your categories cover all your needs and goals. Save your template to start tracking your
                      budget.
                    </li>
                  </ol>
                  <p><strong>Why It Matters</strong>: Categories and groups help you track where your money goes, making it easier to save and plan.</p>
                </v-expansion-panel-text>
              </v-expansion-panel>
            </v-expansion-panels>
          </v-col>
        </v-row>
        <!-- Budget Template Categories -->
        <div class="text-subtitle-1 font-weight-bold"></div>
        <br />
        <v-list>
          <v-list-item v-for="(category, index) in budget.categories" :key="index">
            <v-row :dense="true">
              <v-col cols="12" sm="3" class="px-2">
                <v-text-field v-model="budget.categories[index].name" label="Category" required density="compact"></v-text-field>
              </v-col>
              <v-col cols="12" sm="3" class="px-2">
                <v-text-field v-model="budget.categories[index].group" label="Group" required density="compact"></v-text-field>
              </v-col>
              <v-col cols="12" sm="3" class="px-2">
                <Currency-Input v-model.number="budget.categories[index].target" label="Target" class="text-right" density="compact" required></Currency-Input>
              </v-col>
              <v-col cols="12" sm="2" class="px-2">
                <v-checkbox v-model="budget.categories[index].isFund" label="Is Fund?" density="compact"></v-checkbox>
              </v-col>
              <v-col cols="12" sm="1" class="px-2">
                <v-btn icon variant="plain" @click="removeCategory(index)" color="error">
                  <v-icon>mdi-delete</v-icon>
                </v-btn>
              </v-col>
            </v-row>
          </v-list-item>
        </v-list>

        <!-- Add New Category -->
        <v-form @submit.prevent="addCategory">
          <v-row :dense="true">
            <v-col cols="12" sm="3" class="px-2">
              <v-text-field v-model="newCategory.name" label="Category" required density="compact"></v-text-field>
            </v-col>
            <v-col cols="12" sm="3" class="px-2">
              <v-text-field v-model="newCategory.group" label="Group (e.g., Utilities)" density="compact"></v-text-field>
            </v-col>
            <v-col cols="12" sm="2" class="px-2">
              <Currency-Input v-model.number="newCategory.target" label="Target" class="text-right" density="compact" required></Currency-Input>
            </v-col>
            <v-col cols="12" sm="2" class="px-2">
              <v-checkbox v-model="newCategory.isFund" label="Is Fund?" density="compact"></v-checkbox>
            </v-col>
            <v-col cols="12" sm="2" class="px-2">
              <v-btn type="submit" color="primary">Add Category</v-btn>
            </v-col>
          </v-row>
        </v-form>
      </v-form>
    </v-card-text>
    <v-card-actions>
      <v-spacer></v-spacer>
      <v-btn color="error" @click="handleCancel">Cancel</v-btn>
      <v-btn color="primary" @click="save" :disabled="!isFormValid" :loading="saving">Save Entity</v-btn>
    </v-card-actions>

    <!-- Confirmation Dialog for Importing Categories -->
    <v-dialog v-model="showImportDialog" max-width="400px">
      <v-card>
        <v-card-title>Import Categories</v-card-title>
        <v-card-text>The template has no categories. Would you like to import categories from the latest budget for this entity?</v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="error" @click="showImportDialog = false">No</v-btn>
          <v-btn color="primary" @click="importCategories" :loading="importing">Yes</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Confirmation Dialog for Canceling with Unsaved Changes -->
    <v-dialog v-model="showCancelDialog" max-width="400px">
      <v-card>
        <v-card-title>Unsaved Changes</v-card-title>
        <v-card-text>You have unsaved changes. Are you sure you want to cancel and discard them?</v-card-text>
        <v-card-actions>
          <v-btn color="primary" @click="showCancelDialog = false">No, Keep Editing</v-btn>
          <v-btn color="error" @click="confirmCancel">Yes, Discard</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Snackbar for User Feedback -->
    <v-snackbar v-model="snackbar" :color="snackbarColor" timeout="3000">
      {{ snackbarText }}
      <template v-slot:actions>
        <v-btn variant="text" @click="snackbar = false">Close</v-btn>
      </template>
    </v-snackbar>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue";
import { TemplateBudget, BudgetCategory, TaxForm, Entity, EntityType } from "../types";
import CurrencyInput from "./CurrencyInput.vue";
import { useBudgetStore } from "../store/budget";
import { useFamilyStore } from "../store/family";
import { auth } from "../firebase/index";
import { DEFAULT_BUDGET_TEMPLATES } from "../constants/budgetTemplates";
import { DEFAULT_TAX_FORMS } from "../constants/taxForms";
import { v4 as uuidv4 } from "uuid";
import { formatCurrency, formatEntityType } from "../utils/helpers";
import { Timestamp } from "firebase/firestore";

const props = defineProps<{
  entityId: string;
}>();

const emit = defineEmits<{
  (e: "save"): void;
  (e: "cancel"): void;
  (e: "update:unsaved", value: boolean): void;
}>();

const familyStore = useFamilyStore();
const budgetStore = useBudgetStore();
const saving = ref(false);
const importing = ref(false);
const snackbar = ref(false);
const snackbarText = ref("");
const snackbarColor = ref("info");
const showImportDialog = ref(false);
const showCancelDialog = ref(false);
const isEditing = computed(() => props.entityId !== "");

const entityName = ref("Default");
const entityType = ref<EntityType>(EntityType.Family);
const entityEmail = ref(auth.currentUser?.email || "");
const entityTaxFormIds = ref<string[]>([]);
const budget = ref<TemplateBudget>({
  categories: [],
});
const initialEntity = ref<Entity>();

const newCategory = ref<BudgetCategory>({
  name: "",
  target: 0,
  isFund: false,
  group: "",
});

const entityTypeOptions = Object.values(EntityType).map((value) => ({
  title: formatEntityType(value),
  value,
}));

const availableTaxForms = computed(() => {
  const forms: TaxForm[] = [...DEFAULT_TAX_FORMS];
  return forms.filter((form) => form.applicableEntityTypes.includes(entityType.value));
});

const isFormValid = computed(() => {
  let retValue = entityName.value !== null && entityType.value !== null;
  retValue = retValue && budget.value.categories.filter((f) => f.group.toLowerCase() == "income").length > 0;
  return retValue && budget.value.categories.every((cat) => cat.name && cat.group && cat.target >= 0);
});

const getBudgetInfo = computed(() => {
  const incomeTotal = budget.value.categories
    .filter((category) => category.group?.toLowerCase() === "income")
    .reduce((sum, category) => sum + (Number(category.target) || 0), 0);

  const nonIncomeTotal = budget.value.categories
    .filter((category) => category.group?.toLowerCase() !== "income")
    .reduce((sum, category) => sum + (Number(category.target) || 0), 0);
  return `Income: ${formatCurrency(incomeTotal)} Remaining: ${formatCurrency(incomeTotal - nonIncomeTotal)} `;
});

const hasUnsavedChanges = computed(() => {
  if (!initialEntity.value && isEditing.value) return false;

  const currentEntity: Partial<Entity> = {
    name: entityName.value,
    type: entityType.value,
    email: entityEmail.value,
    taxFormIds: entityTaxFormIds.value,
    templateBudget: { categories: budget.value.categories },
  };

  if (!isEditing.value) {
    return (
      entityName.value !== "Default" ||
      entityType.value !== EntityType.Family ||
      entityTaxFormIds.value.length > 0 ||
      budget.value.categories.length > 0
    );
  }

  return (
    currentEntity.name !== initialEntity.value.name ||
    currentEntity.type !== initialEntity.value.type ||
    JSON.stringify(currentEntity.taxFormIds) !== JSON.stringify(initialEntity.value.taxFormIds) ||
    JSON.stringify(currentEntity.templateBudget?.categories) !== JSON.stringify(initialEntity.value.templateBudget?.categories)
  );
});

watch(hasUnsavedChanges, (newValue) => {
  emit("update:unsaved", newValue);
});

onMounted(async () => {
  const userId = auth.currentUser?.uid;
  if (!userId) {
    showSnackbar("Please log in to load budgets.", "error");
    return;
  }
  await budgetStore.loadBudgets(userId, props.entityId);
  if (isEditing.value) {
    const entity = familyStore.family?.entities.find((e) => e.id === props.entityId);
    if (entity) {
      initialEntity.value = entity;
      let o = entity.members.find((m) => m.uid === initialEntity.value.ownerUid);
      entityName.value = initialEntity.value.name;
      entityType.value = initialEntity.value.type;
      entityEmail.value = o?.email ?? entityEmail.value;
      entityTaxFormIds.value = initialEntity.value.taxFormIds ?? [];
      budget.value.categories = initialEntity.value.templateBudget?.categories ?? [];
    }
  }
  if (isEditing.value && budget.value.categories.length === 0) {
    showImportDialog.value = true;
  }
});

async function importCategories() {
  importing.value = true;
  try {
    const entityBudgets = Array.from(budgetStore.budgets.values()).filter((b) => b.entityId === props.entityId);
    const latestBudget = entityBudgets.sort((a, b) => b.month.localeCompare(a.month))[0];
    if (latestBudget && latestBudget.categories.length > 0) {
      budget.value.categories = [...latestBudget.categories];
      showSnackbar("Categories imported successfully.", "success");
    } else if (DEFAULT_BUDGET_TEMPLATES[entityType.value]) {
      const predefinedTemplate = DEFAULT_BUDGET_TEMPLATES[entityType.value];
      budget.value.categories = predefinedTemplate.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? 0 : 0,
      }));
      showSnackbar("Default categories imported for entity type.", "success");
    } else {
      showSnackbar("No budgets or default categories found for this entity.", "info");
    }
    showImportDialog.value = false;
  } catch (error: any) {
    console.error("Error importing categories:", error.message);
    showSnackbar(`Error importing categories: ${error.message}`, "error");
  } finally {
    importing.value = false;
  }
}

function addCategory() {
  if (newCategory.value.name && newCategory.value.target >= 0 && newCategory.value.group) {
    budget.value.categories.push({ ...newCategory.value });
    newCategory.value = { name: "", target: 0, isFund: false, group: "" };
  }
}

function removeCategory(index: number) {
  budget.value.categories.splice(index, 1);
}

async function save() {
  if (!entityName.value || !entityType.value) {
    showSnackbar("Please provide Entity Name and Type", "error");
    return;
  }

  saving.value = true;
  try {
    const family = await familyStore.getFamily();
    if (!family) throw new Error("No family found");

    const entity: Entity = {
      id: isEditing.value ? props.entityId : uuidv4(),
      familyId: family.id,
      name: entityName.value,
      type: entityType.value,
      ownerUid: auth.currentUser?.uid || "",
      members: isEditing.value && initialEntity.value
        ? initialEntity.value.members
        : [{ uid: auth.currentUser?.uid || "", email: auth.currentUser?.email || "", role: "Admin" }],
      createdAt: isEditing.value && initialEntity.value ? initialEntity.value.createdAt : Timestamp.fromDate(new Date()),
      updatedAt: Timestamp.fromDate(new Date()),
      email: entityEmail.value,
      templateBudget: { categories: budget.value.categories },
      taxFormIds: entityTaxFormIds.value,
    };

    if (!entity.id) {
      entity.id = uuidv4();
      entity.createdAt = Timestamp.fromDate(new Date());
    }

    if (isEditing.value) {
      await familyStore.updateEntity(family.id, entity);
    } else {
      await familyStore.createEntity(family.id, entity);
    }

    showSnackbar(`${isEditing.value ? "Updated" : "Created"} entity successfully`, "success");
    emit("save");
  } catch (error: any) {
    console.error("Error saving entity:", error);
    showSnackbar(`Error saving entity: ${error.message}`, "error");
  } finally {
    saving.value = false;
  }
}

function handleCancel() {
  if (hasUnsavedChanges.value) {
    showCancelDialog.value = true;
  } else {
    confirmCancel();
  }
}

function confirmCancel() {
  showCancelDialog.value = false;
  emit("cancel");
  resetForm();
}

function resetForm() {
  entityName.value = "Default";
  entityType.value = EntityType.Family;
  entityEmail.value = auth.currentUser?.email || "";
  entityTaxFormIds.value = [];
  budget.value.categories = [];
  newCategory.value = { name: "", target: 0, isFund: false, group: "" };
}

function showSnackbar(text: string, color: string) {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
}
</script>
