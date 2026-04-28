<template>
  <q-card class="q-mt-md">
    <q-card-section>
      <div class="row items-center">
        <div class="col text-bold">Savings Goals</div>
        <div class="col-auto">
          <q-btn flat dense round icon="add" @click="$emit('add')">
            <q-tooltip>Add goal</q-tooltip>
          </q-btn>
        </div>
      </div>
      <div v-for="goal in goals" :key="goal.id" class="q-mt-sm">
        <div class="row items-center cursor-pointer" @click="$emit('view', goal)">
          <div class="col">
            <div class="row items-center justify-between">
              <div class="text-subtitle1">{{ goal.name }}</div>
              <div class="text-caption">{{ formatCurrency(goal.savedToDate || 0) }} of {{ formatCurrency(goal.totalTarget) }}</div>
            </div>
            <q-linear-progress
              :value="goal.totalTarget ? (goal.savedToDate || 0) / goal.totalTarget : 0"
              rounded
              color="primary"
              class="q-mt-xs"
            />
            <div class="row items-center justify-between text-caption q-mt-xs">
              <div>Monthly Goal {{ formatCurrency(goal.monthlyTarget) }}</div>
              <div>{{ formatCurrency(goal.totalTarget - (goal.savedToDate || 0)) }}</div>
            </div>
          </div>
          <div v-if="!isMobile" class="col-auto">
            <q-btn flat dense label="Contribute" @click.stop="$emit('contribute', goal)" />
          </div>
        </div>
      </div>
      <div v-if="goals.length === 0" class="text-body2 text-grey-7">
        No goals yet.
        <q-btn flat dense no-caps color="primary" label="Set your first goal →" class="q-ml-xs" @click="$emit('add')" />
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { useQuasar } from 'quasar';
import { formatCurrency } from '../../utils/helpers';
import { useGoals } from '../../composables/useGoals';

const props = defineProps<{ entityId: string }>();
const { listGoals, loadGoals } = useGoals();
const $q = useQuasar();
const isMobile = $q.platform.is.mobile;

// Drive the displayed list directly off the shared `useGoals` store via a
// computed so any write that touches that store (contribute, delete, edit,
// archive, etc. — anywhere in the app) re-renders this card without
// requiring its parent to manually call load(). Previously this was a local
// ref populated only on mount/entityId-change, which is why deleting a
// contribution from GoalDetailsPanel left the savings-goals card showing
// stale numbers until the user refreshed the page.
const goals = computed(() => listGoals(props.entityId));

onMounted(() => {
  if (props.entityId) void loadGoals(props.entityId);
});
watch(
  () => props.entityId,
  (id) => {
    if (id) void loadGoals(id);
  },
);
</script>
