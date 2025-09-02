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
        <div class="row items-center">
          <div class="col">
            <div class="text-subtitle1">{{ goal.name }}</div>
            <q-linear-progress
              :value="goal.totalTarget ? (goal.savedToDate || 0) / goal.totalTarget : 0"
              rounded
              color="primary"
              class="q-mt-xs"
            />
            <div class="text-caption">
              {{ formatCurrency(goal.savedToDate || 0) }} of {{ formatCurrency(goal.totalTarget) }}
            </div>
          </div>
          <div class="col-auto q-mr-sm">
            <q-badge color="primary" :label="formatCurrency(goal.monthlyTarget)" />
          </div>
          <div class="col-auto">
            <q-btn flat dense label="Contribute" @click="$emit('contribute', goal)" />
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
import { formatCurrency } from '../../utils/helpers';
import { useGoals } from '../../composables/useGoals';
import type { Goal } from '../../types';

const props = defineProps<{ entityId: string }>();
const goals = ref<Goal[]>([]);
const { listGoals, loadGoals } = useGoals();

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
</script>
