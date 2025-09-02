<template>
  <q-page padding>
    <div class="row">
      <div class="col-12">
        <EntitySelector class="q-mb-sm" />
        <div class="text-subtitle2 text-primary q-mb-md">
          {{ budgetLabel }}
        </div>
        <q-banner v-for="nudge in nudges" :key="nudge" class="q-mb-sm" color="warning" text-color="white" icon="savings">
          {{ nudge }}
        </q-banner>
        <DashboardTiles
          :budget-id="budgetId"
          :family-id="familyId"
          :entity-id="entityId"
          @open-bills="onOpenBills"
          @create-goal="onCreateGoal"
        />
      </div>
      <div class="col-12 q-mt-sm">
        <q-btn color="primary" class="full-width q-py-sm" rounded unelevated size="lg" label="View Transactions" to="/transactions" />
      </div>
      <div class="col-12 q-mt-md">
        <div class="row q-col-gutter-md">
          <div class="col-12 col-md-6">
            <SpendingByCategoryCard :budget-id="budgetId" />
          </div>
          <div class="col-12 col-md-6">
            <IncomeVsExpensesCard :entity-id="entityId" />
          </div>
        </div>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import DashboardTiles from '../components/DashboardTiles.vue';
import EntitySelector from '../components/EntitySelector.vue';
import SpendingByCategoryCard from '../components/charts/SpendingByCategoryCard.vue';
import IncomeVsExpensesCard from '../components/charts/IncomeVsExpensesCard.vue';
import { useFamilyStore } from '../store/family';
import { useAuthStore } from '../store/auth';
import { currentMonthISO } from '../utils/helpers';
import { useGoalNudges } from '../composables/useGoalNudges';

const familyStore = useFamilyStore();
const auth = useAuthStore();
const { getNudges } = useGoalNudges();
const nudges = ref<string[]>([]);

onMounted(async () => {
  await familyStore.loadFamily();
  if (entityId.value) {
    nudges.value = await getNudges(entityId.value);
  }
});

const familyId = computed(() => familyStore.family?.id || '');
const entityId = computed(() => familyStore.selectedEntityId || '');
const budgetId = computed(() => {
  const uid = auth.user?.uid;
  const eid = entityId.value;
  const month = currentMonthISO();
  return uid && eid ? `${uid}_${eid}_${month}` : '';
});

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
function onCreateGoal() {}
</script>
