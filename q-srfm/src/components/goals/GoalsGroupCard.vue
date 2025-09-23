<template>
  <q-card flat bordered class="q-mt-md">
    <q-card-section>
      <div class="row items-center">
        <div class="col text-bold">Savings Goals</div>
        <div class="col-auto">
          <q-btn flat dense round icon="add" @click="$emit('add')" />
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
          <div v-if="!isMobile" class="col-auto row items-center no-wrap">
            <q-btn flat dense label="Contribute" class="q-mr-sm" @click.stop="$emit('contribute', goal)" />
            <q-btn flat dense round icon="delete" color="negative" @click.stop="confirmArchive(goal)" />
          </div>
        </div>
      </div>
      <div v-if="goals.length === 0" class="text-caption text-grey">
        No goals yet.
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useQuasar } from 'quasar';
import { formatCurrency } from '../../utils/helpers';
import { useGoals } from '../../composables/useGoals';
import type { Goal } from '../../types';

const props = defineProps<{ entityId: string }>();
const goals = ref<Goal[]>([]);
const { listGoals, loadGoals, archiveGoal } = useGoals();
const $q = useQuasar();
const isMobile = $q.platform.is.mobile;

async function load() {
  if (!props.entityId) return;
  await loadGoals(props.entityId);
  goals.value = listGoals(props.entityId);
}

onMounted(() => {
  void load();
});
watch(
  () => props.entityId,
  () => {
    void load();
  },
);

function confirmArchive(goal: Goal) {
  $q.dialog({
    title: 'Delete Savings Goal',
    message: `Are you sure you want to delete the goal "${goal.name}"? This hides it from budgets but keeps historical contributions/spend.`,
    cancel: { label: 'Cancel' },
    persistent: true,
    ok: { label: 'Delete', color: 'negative' },
  }).onOk(() => {
    void (async () => {
      await archiveGoal(goal.id);
      await load();
    })();
  });
}
</script>
