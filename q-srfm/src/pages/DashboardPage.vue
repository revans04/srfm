<template>
  <q-page padding>
    <div class="row">
      <div class="col-12">
        <DashboardTiles :budget-id="budgetId" :family-id="familyId" @open-bills="onOpenBills" @create-goal="onCreateGoal" />
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
            <IncomeVsExpensesCard :entity-id="familyId" />
          </div>
        </div>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import DashboardTiles from '../components/DashboardTiles.vue';
import SpendingByCategoryCard from '../components/charts/SpendingByCategoryCard.vue';
import IncomeVsExpensesCard from '../components/charts/IncomeVsExpensesCard.vue';
import { useFamilyStore } from '../store/family';
import { useAuthStore } from '../store/auth';
import { currentMonthISO } from '../utils/helpers';

const familyStore = useFamilyStore();
const auth = useAuthStore();

const familyId = computed(() => familyStore.selectedEntityId || '');
const budgetId = computed(() => {
  const uid = auth.user?.uid;
  const entityId = familyId.value;
  const month = currentMonthISO();
  return uid && entityId ? `${uid}_${entityId}_${month}` : '';
});

function onOpenBills() {}
function onCreateGoal() {}
</script>
