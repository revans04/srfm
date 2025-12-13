<template>
  <q-page class="bg-grey-1 q-pa-lg">
    <div class="row items-center justify-between q-mb-md dashboard-header">
      <div class="col text-h4 q-mb-xs">Dashboard</div>
      <div class="col-auto">
        <EntitySelector />
      </div>
      <div class="row items-center q-col-gutter-sm">
        <div class="col-auto text-h6 text-primary">
          {{ budgetLabel }}
          <q-btn flat dense round icon="expand_more" size="sm"></q-btn>
          <MonthSelector v-model="selectedMonth" :entity-id="entityId" :existing-months="monthSet" @select="selectMonth" />
        </div>
      </div>
    </div>

    <div class="row items-start q-col-gutter-md q-mb-lg">
      <div class="col q-gutter-md">
        <div v-for="nudge in nudges" :key="nudge" class="q-mb-sm">
          <q-banner dense class="dashboard-nudge" color="warning" text-color="white" icon="savings">
            {{ nudge }}
          </q-banner>
        </div>
      </div>
    </div>

    <section class="dashboard-summary q-mb-lg">
      <DashboardTiles :budget-id="budgetId" :family-id="familyId" :entity-id="entityId" @open-bills="onOpenBills" @create-goal="onCreateGoal" />
    </section>

    <section class="row q-col-gutter-md">
      <div class="col-12 col-lg-6">
        <SpendingByCategoryCard :budget-id="budgetId" />
      </div>
      <div class="col-12 col-lg-6">
        <IncomeVsExpensesCard :entity-id="entityId" />
      </div>
    </section>

    <GoalDialog v-model="goalDialog" @save="saveGoal" />
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import DashboardTiles from '../components/DashboardTiles.vue';
import EntitySelector from '../components/EntitySelector.vue';
import MonthSelector from '../components/MonthSelector.vue';
import SpendingByCategoryCard from '../components/charts/SpendingByCategoryCard.vue';
import IncomeVsExpensesCard from '../components/charts/IncomeVsExpensesCard.vue';
import GoalDialog from '../components/goals/GoalDialog.vue';
import { useFamilyStore } from '../store/family';
import { useAuthStore } from '../store/auth';
import { useBudgetStore } from '../store/budget';
import { currentMonthISO } from '../utils/helpers';
import { useGoalNudges } from '../composables/useGoalNudges';
import { useGoals } from '../composables/useGoals';
import type { Goal } from '../types';
import { createBudgetForMonth } from '../utils/budget';

const familyStore = useFamilyStore();
const auth = useAuthStore();
const budgetStore = useBudgetStore();
const { getNudges } = useGoalNudges();
const { createGoal, loadGoals } = useGoals();
const nudges = ref<string[]>([]);
const goalDialog = ref(false);
const selectedMonth = ref(currentMonthISO());
const userId = computed(() => auth.user?.uid || '');

const familyId = computed(() => familyStore.family?.id || '');
const entityId = computed(() => familyStore.selectedEntityId || '');
const budgetId = ref<string>('');

const budgetsForEntity = computed(() => {
  const entity = entityId.value;
  if (!entity) return [];
  return Array.from(budgetStore.budgets.values()).filter((budget) => budget.entityId === entity);
});
const monthSet = computed(() => {
  return new Set(budgetsForEntity.value.map((budget) => budget.month));
});

onMounted(async () => {
  await familyStore.loadFamily();
  if (entityId.value) {
    nudges.value = await getNudges(entityId.value);
  }
});

watch(
  () => entityId.value,
  async (entity) => {
    if (!entity || !userId.value) return;
    await budgetStore.loadBudgets(userId.value, entity);
    await loadBudgetForMonth(selectedMonth.value);
  },
  { immediate: true },
);

async function loadBudgetForMonth(month: string) {
  if (!familyId.value || !entityId.value) return;
  const family = await familyStore.getFamily?.();
  const ownerUid = family?.ownerUid || auth.user?.uid || '';
  if (!ownerUid) return;
  try {
    const b = await createBudgetForMonth(month, familyId.value, ownerUid, entityId.value);
    if (b?.budgetId) {
      budgetId.value = b.budgetId;
      selectedMonth.value = month;
    }
  } catch (error: unknown) {
    console.error('Failed to load budget for selected month', error);
  }
}
async function selectMonth(month: string) {
  if (!entityId.value) return;
  await loadBudgetForMonth(month);
}

function formatLongMonth(month: string) {
  const [year, monthNum] = month.split('-');
  const date = new Date(parseInt(year), parseInt(monthNum) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
}

const budgetLabel = computed(() => {
  const month = formatLongMonth(selectedMonth.value);
  return `${month}`;
});

function onOpenBills() {}
function onCreateGoal() {
  goalDialog.value = true;
}

async function saveGoal(data: Partial<Goal>) {
  await createGoal(data);
  if (entityId.value) {
    await loadGoals(entityId.value);
  }
}
</script>

<style scoped>
.dashboard-nudge {
  border-radius: 10px;
}
</style>
