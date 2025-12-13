<template>
  <q-page class="bg-grey-1 q-pa-lg">
    <div class="row items-center justify-between q-col-gutter-sm q-mb-md">
      <div>
        <div class="text-h4 q-mb-xs">Dashboard</div>
        <div class="text-subtitle2 text-grey-7">{{ budgetLabel }}</div>
      </div>
      <q-btn
        color="primary"
        unelevated
        rounded
        size="lg"
        class="q-px-lg"
        label="View Transactions"
        to="/transactions"
      />
    </div>

    <div class="row items-start q-col-gutter-md q-mb-lg">
      <div class="col-auto">
        <EntitySelector />
      </div>
      <div class="col q-gutter-md">
        <div v-for="nudge in nudges" :key="nudge" class="q-mb-sm">
          <q-banner dense class="dashboard-nudge" color="warning" text-color="white" icon="savings">
            {{ nudge }}
          </q-banner>
        </div>
      </div>
    </div>

    <section class="dashboard-summary q-mb-lg">
      <DashboardTiles
        :budget-id="budgetId"
        :family-id="familyId"
        :entity-id="entityId"
        @open-bills="onOpenBills"
        @create-goal="onCreateGoal"
      />
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
import { computed, onMounted, ref } from 'vue';
import DashboardTiles from '../components/DashboardTiles.vue';
import EntitySelector from '../components/EntitySelector.vue';
import SpendingByCategoryCard from '../components/charts/SpendingByCategoryCard.vue';
import IncomeVsExpensesCard from '../components/charts/IncomeVsExpensesCard.vue';
import GoalDialog from '../components/goals/GoalDialog.vue';
import { useFamilyStore } from '../store/family';
import { useAuthStore } from '../store/auth';
import { currentMonthISO } from '../utils/helpers';
import { useGoalNudges } from '../composables/useGoalNudges';
import { useGoals } from '../composables/useGoals';
import type { Goal } from '../types';
import { createBudgetForMonth } from '../utils/budget';

const familyStore = useFamilyStore();
const auth = useAuthStore();
const { getNudges } = useGoalNudges();
const { createGoal, loadGoals } = useGoals();
const nudges = ref<string[]>([]);
const goalDialog = ref(false);

onMounted(async () => {
  await familyStore.loadFamily();
  if (entityId.value) {
    nudges.value = await getNudges(entityId.value);
  }
  await ensureCurrentMonthBudget();
});

const familyId = computed(() => familyStore.family?.id || '');
const entityId = computed(() => familyStore.selectedEntityId || '');
const budgetId = ref<string>('');

async function ensureCurrentMonthBudget() {
  if (!familyId.value || !entityId.value) return;
  const family = await familyStore.getFamily?.();
  const ownerUid = family?.ownerUid || auth.user?.uid || '';
  if (!ownerUid) return;
  const b = await createBudgetForMonth(currentMonthISO(), familyId.value, ownerUid, entityId.value);
  if (b?.budgetId) budgetId.value = b.budgetId;
}

const currentEntityName = computed(() => {
  if (!entityId.value) return 'All Entities';
  const entity = familyStore.family?.entities?.find((e) => e.id === entityId.value);
  return entity?.name || 'All Entities';
});

function formatLongMonth(month: string) {
  const [year, monthNum] = month.split('-');
  const date = new Date(parseInt(year), parseInt(monthNum) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
}

const budgetLabel = computed(() => {
  const month = formatLongMonth(currentMonthISO());
  return `${month} - ${currentEntityName.value}`;
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
