<template>
  <div class="row q-col-gutter-md q-mb-md">
    <div class="col-12 col-md-6 col-lg-3">
      <BudgetProgressTile :budget-id="budgetId" />
    </div>
    <div class="col-12 col-md-6 col-lg-3">
      <NetWorthTile :family-id="familyId" />
    </div>
    <div class="col-12 col-md-6 col-lg-3">
      <UpcomingBillsTile @open="$emit('open-bills')" />
    </div>
    <div class="col-12 col-md-6 col-lg-3">
      <GoalsTile @create="$emit('create-goal')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import BudgetProgressTile from './tiles/BudgetProgressTile.vue';
import NetWorthTile from './tiles/NetWorthTile.vue';
import UpcomingBillsTile from './tiles/UpcomingBillsTile.vue';
import GoalsTile from './tiles/GoalsTile.vue';

defineProps<{ budgetId: string; familyId: string }>();
defineEmits<{ (e: 'open-bills'): void; (e: 'create-goal'): void }>();
</script>

