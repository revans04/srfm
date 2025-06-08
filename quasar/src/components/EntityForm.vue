<!-- src/components/EntityForm.vue -->
<template>
  <q-card dense>
    <q-card-section>
      {{ isEditing ? 'Edit Entity' : 'Create Entity' }}
    </q-card-section>
    <q-card-section>
      <q-form @submit="save">
        <!-- Entity Details -->
        <q-row dense>
          <q-col cols="12" sm="6">
            <q-input
              v-model="entityName"
              label="Entity Name"
              dense
              :rules="[(v) => !!v || 'Entity Name is required']"
            />
          </q-col>
          <q-col cols="12" sm="6">
            <q-select
              v-model="entityType"
              label="Entity Type"
              :options="entityTypeOptions"
              option-label="title"
              option-value="value"
              dense
              :rules="[(v) => !!v || 'Entity Type is required']"
            />
          </q-col>
          <q-col cols="12">
            <q-input v-model="entityEmail" label="Owner Email" dense disable />
          </q-col>
          <q-col cols="12">
            <q-select
              v-model="entityTaxFormIds"
              label="Applicable Tax Forms"
              :options="availableTaxForms"
              option-label="name"
              option-value="id"
              multiple
              use-chips
              clearable
              dense
              hint="Select tax forms applicable to this entity (federal and state)"
              bottom
            />
          </q-col>
        </q-row>
        <br />
        <q-row>
          <q-col cols="8" class="text-subtitle-1 font-weight-bold">
            Budget Template
            <div v-if="isFormValid">{{ getBudgetInfo }}</div>
          </q-col>
          <q-col cols="4" class="text-right">
            <q-btn color="primary" @click="importCategories" label="Add from Type" />
          </q-col>
          <q-col cols="12">
            <!-- Help Section -->
            <q-expansion-item label="Need help with creating a budget template?" class="q-mb-md">
              <q-card>
                <q-card-section class="q-px-md">
                  <strong>Create Your First Budget Template</strong>
                  A budget template helps you plan how to use your money each month. Follow these
                  simple steps:
                  <ol>
                    <li>
                      <strong>Add Your Income</strong>: List at least one source of money you
                      receive regularly, like your salary or freelance earnings. The Group should be
                      Income and the category can be something like Salary or the name of the income
                      source. Example: "Monthly Salary: $3,000."
                    </li>
                    <li>
                      <strong>List Spending and Saving Categories</strong>: Create categories for
                      where your money goes, such as rent, groceries, or savings. Group similar
                      categories together (e.g., "Housing" for rent and utilities, "Daily Needs" for
                      groceries and gas). <br /><strong>Tip</strong>: Click the "Add Categories from
                      Type" button to get sample categories and groups based on your entity type.
                      You can edit these to fit your needs.
                    </li>
                    <li>
                      <strong>Assign Your Income</strong>: Divide your total income among your
                      categories to cover all expenses and savings. Example: $1,000 for rent, $500
                      for groceries, $500 for savings, etc. Make sure the total matches your income
                      to avoid overspending.
                    </li>
                    <li>
                      <strong>Review and Save</strong>: Check that your categories cover all your
                      needs and goals. Save your template to start tracking your budget.
                    </li>
                  </ol>
                  <p>
                    <strong>Why It Matters</strong>: Categories and groups help you track where your
                    money goes, making it easier to save and plan.
                  </p>
                </q-card-section>
              </q-card>
            </q-expansion-item>
          </q-col>
        </q-row>
        <!-- Budget Template Categories -->
        <div class="text-subtitle-1 font-weight-bold"></div>
        <br />
        <q-list>
          <q-item v-for="(category, index) in budget.categories" :key="index">
            <q-row dense>
              <q-col cols="12" sm="3" class="q-px-sm">
                <q-input v-model="budget.categories[index].name" label="Category" dense required />
              </q-col>
              <q-col cols="12" sm="3" class="q-px-sm">
                <q-input v-model="budget.categories[index].group" label="Group" dense required />
              </q-col>
              <q-col cols="12" sm="3" class="q-px-sm">
                <Currency-Input
                  v-model.number="budget.categories[index].target"
                  label="Target"
                  class="text-right"
                  dense
                  required
                />
              </q-col>
              <q-col cols="12" sm="2" class="q-px-sm">
                <q-checkbox v-model="budget.categories[index].isFund" label="Is Fund?" dense />
              </q-col>
              <q-col cols="12" sm="1" class="q-px-sm">
                <q-btn icon="mdi-delete" flat color="negative" @click="removeCategory(index)" />
              </q-col>
            </q-row>
          </q-item>
        </q-list>

        <!-- Add New Category -->
        <q-form @submit="addCategory">
          <q-row dense>
            <q-col cols="12" sm="3" class="q-px-sm">
              <q-input v-model="newCategory.name" label="Category" dense required />
            </q-col>
            <q-col cols="12" sm="3" class="q-px-sm">
              <q-input v-model="newCategory.group" label="Group (e.g., Utilities)" dense />
            </q-col>
            <q-col cols="12" sm="2" class="q-px-sm">
              <Currency-Input
                v-model.number="newCategory.target"
                label="Target"
                class="text-right"
                dense
                required
              />
            </q-col>
            <q-col cols="12" sm="2" class="q-px-sm">
              <q-checkbox v-model="newCategory.isFund" label="Is Fund?" dense />
            </q-col>
            <q-col cols="12" sm="2" class="q-px-sm">
              <q-btn type="submit" color="primary" label="Add Category" />
            </q-col>
          </q-row>
        </q-form>
      </q-form>
    </q-card-section>
    <q-card-actions align="right">
      <q-space />
      <q-btn color="negative" @click="handleCancel" label="Cancel" />
      <q-btn
        color="primary"
        @click="save"
        :disable="!isFormValid"
        :loading="saving"
        label="Save Entity"
      />
    </q-card-actions>

    <!-- Confirmation Dialog for Importing Categories -->
    <q-dialog v-model="showImportDialog" persistent>
      <q-card>
        <q-card-section class="text-h6">Import Categories</q-card-section>
        <q-card-section>
          The template has no categories. Would you like to import categories from the latest budget
          for this entity?
        </q-card-section>
        <q-card-actions align="right">
          <q-space />
          <q-btn color="negative" @click="showImportDialog = false" label="No" v-close-popup />
          <q-btn
            color="primary"
            @click="importCategories"
            :loading="importing"
            label="Yes"
            v-close-popup
          />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Confirmation Dialog for Canceling with Unsaved Changes -->
    <q-dialog v-model="showCancelDialog" persistent>
      <q-card>
        <q-card-section class="text-h6">Unsaved Changes</q-card-section>
        <q-card-section>
          You have unsaved changes. Are you sure you want to cancel and discard them?
        </q-card-section>
        <q-card-actions align="right">
          <q-btn
            color="primary"
            @click="showCancelDialog = false"
            label="No, Keep Editing"
            v-close-popup
          />
          <q-btn color="negative" @click="confirmCancel" label="Yes, Discard" v-close-popup />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Notification for User Feedback -->
    <q-notification v-model="snackbar" :color="snackbarColor" position="top" :timeout="3000">
      {{ snackbarText }}
      <template v-slot:actions>
        <q-btn flat label="Close" @click="snackbar = false" />
      </template>
    </q-notification>
  </q-card>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { TemplateBudget, BudgetCategory, TaxForm, Entity, EntityType } from '@/types';
import CurrencyInput from './CurrencyInput.vue';
import { useBudgetStore } from '../store/budget';
import { useFamilyStore } from '../store/family';
import { auth } from '../firebase/index';
import { DEFAULT_BUDGET_TEMPLATES } from '../constants/budgetTemplates';
import { DEFAULT_TAX_FORMS } from '../constants/taxForms';
import { v4 as uuidv4 } from 'uuid';
import { formatCurrency, formatEntityType } from '@/utils/helpers';
import { Timestamp } from 'firebase/firestore';

const props = defineProps<{
  entityId: string;
}>();

const emit = defineEmits<{
  (e: 'save'): void;
  (e: 'cancel'): void;
  (e: 'update:unsaved', value: boolean): void;
}>();

const familyStore = useFamilyStore();
const budgetStore = useBudgetStore();
const saving = ref(false);
const importing = ref(false);
const snackbar = ref(false);
const snackbarText = ref('');
const snackbarColor = ref('info');
const showImportDialog = ref(false);
const showCancelDialog = ref(false);
const isEditing = computed(() => props.entityId !== '');

const entityName = ref('Default');
const entityType = ref<EntityType>(EntityType.Family);
const entityEmail = ref(auth.currentUser?.email || '');
const entityTaxFormIds = ref<string[]>([]);
const budget = ref<TemplateBudget>({
  categories: [],
});
const initialEntity = ref<Entity>();

const newCategory = ref<BudgetCategory>({
  name: '',
  target: 0,
  isFund: false,
  group: '',
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
  retValue =
    retValue && budget.value.categories.filter((f) => f.group.toLowerCase() == 'income').length > 0;
  return (
    retValue && budget.value.categories.every((cat) => cat.name && cat.group && cat.target >= 0)
  );
});

const getBudgetInfo = computed(() => {
  const incomeTotal = budget.value.categories
    .filter((category) => category.group?.toLowerCase() === 'income')
    .reduce((sum, category) => sum + (Number(category.target) || 0), 0);

  const nonIncomeTotal = budget.value.categories
    .filter((category) => category.group?.toLowerCase() !== 'income')
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
      entityName.value !== 'Default' ||
      entityType.value !== EntityType.Family ||
      entityTaxFormIds.value.length > 0 ||
      budget.value.categories.length > 0
    );
  }

  return (
    currentEntity.name !== initialEntity.value.name ||
    currentEntity.type !== initialEntity.value.type ||
    JSON.stringify(currentEntity.taxFormIds) !== JSON.stringify(initialEntity.value.taxFormIds) ||
    JSON.stringify(currentEntity.templateBudget?.categories) !==
      JSON.stringify(initialEntity.value.templateBudget?.categories)
  );
});

watch(hasUnsavedChanges, (newValue) => {
  emit('update:unsaved', newValue);
});

onMounted(async () => {
  const userId = auth.currentUser?.uid;
  if (!userId) {
    showSnackbar('Please log in to load budgets.', 'error');
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
    const entityBudgets = Array.from(budgetStore.budgets.values()).filter(
      (b) => b.entityId === props.entityId,
    );
    const latestBudget = entityBudgets.sort((a, b) => b.month.localeCompare(a.month))[0];
    if (latestBudget && latestBudget.categories.length > 0) {
      budget.value.categories = [...latestBudget.categories];
      showSnackbar('Categories imported successfully.', 'success');
    } else if (DEFAULT_BUDGET_TEMPLATES[entityType.value]) {
      const predefinedTemplate = DEFAULT_BUDGET_TEMPLATES[entityType.value];
      budget.value.categories = predefinedTemplate.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? 0 : 0,
      }));
      showSnackbar('Default categories imported for entity type.', 'success');
    } else {
      showSnackbar('No budgets or default categories found for this entity.', 'info');
    }
    showImportDialog.value = false;
  } catch (error: any) {
    console.error('Error importing categories:', error.message);
    showSnackbar(`Error importing categories: ${error.message}`, 'error');
  } finally {
    importing.value = false;
  }
}

function addCategory() {
  if (newCategory.value.name && newCategory.value.target >= 0 && newCategory.value.group) {
    budget.value.categories.push({ ...newCategory.value });
    newCategory.value = { name: '', target: 0, isFund: false, group: '' };
  }
}

function removeCategory(index: number) {
  budget.value.categories.splice(index, 1);
}

async function save() {
  if (!entityName.value || !entityType.value) {
    showSnackbar('Please provide Entity Name and Type', 'error');
    return;
  }

  saving.value = true;
  try {
    const family = await familyStore.getFamily();
    if (!family) throw new Error('No family found');

    const entity: Entity = {
      id: isEditing.value ? props.entityId : uuidv4(),
      familyId: family.id,
      name: entityName.value,
      type: entityType.value,
      ownerUid: auth.currentUser?.uid || '',
      members:
        isEditing.value && initialEntity.value
          ? initialEntity.value.members
          : [
              {
                uid: auth.currentUser?.uid || '',
                email: auth.currentUser?.email || '',
                role: 'Admin',
              },
            ],
      createdAt:
        isEditing.value && initialEntity.value
          ? initialEntity.value.createdAt
          : Timestamp.fromDate(new Date()),
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

    showSnackbar(`${isEditing.value ? 'Updated' : 'Created'} entity successfully`, 'success');
    emit('save');
  } catch (error: any) {
    console.error('Error saving entity:', error);
    showSnackbar(`Error saving entity: ${error.message}`, 'error');
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
  emit('cancel');
  resetForm();
}

function resetForm() {
  entityName.value = 'Default';
  entityType.value = EntityType.Family;
  entityEmail.value = auth.currentUser?.email || '';
  entityTaxFormIds.value = [];
  budget.value.categories = [];
  newCategory.value = { name: '', target: 0, isFund: false, group: '' };
}

function showSnackbar(text: string, color: string) {
  snackbarText.value = text;
  snackbarColor.value = color;
  snackbar.value = true;
}
</script>

<style scoped>
.rounded-borders {
  border-radius: 4px;
}
</style>
