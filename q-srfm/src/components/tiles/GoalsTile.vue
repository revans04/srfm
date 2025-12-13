<template>
  <q-card flat bordered class="dashboard-tile">
    <q-card-section class="row items-center justify-between q-px-md q-py-sm">
      <div class="text-subtitle2 q-mb-none">Goals</div>
      <q-btn dense flat icon="add" color="primary" @click="$emit('create')" title="Create Goal" />
    </q-card-section>
    <q-card-section class="q-pt-xs q-px-md q-pb-md">
      <div v-if="goals.length">
        <div
          v-for="g in goals.slice(0, 3)"
          :key="g.id"
          class="row items-center justify-between q-py-xs"
        >
          <div class="text-body1 text-grey-9">{{ g.name }}</div>
          <div class="text-body2">{{ formatCurrency(g.savedToDate || 0) }} / {{ formatCurrency(g.totalTarget) }}</div>
        </div>
        <div v-if="goals.length > 3" class="text-caption text-grey-6 q-mt-xs">
          +{{ goals.length - 3 }} more
        </div>
      </div>
      <div v-else class="text-body2 text-grey-7">No active goals</div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useGoals } from '../../composables/useGoals';

const props = defineProps<{ entityId: string }>();
defineEmits<{ (e: 'create'): void }>();

const { listGoals, loadGoals } = useGoals();

onMounted(() => {
  if (props.entityId) {
    void loadGoals(props.entityId);
  }
});

const goals = computed(() => (props.entityId ? listGoals(props.entityId) : []));

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(amount);
}
</script>

<style scoped>
.dashboard-tile {
  min-height: 150px;
  border-radius: 12px;
  background-color: #ffffff;
}
.text-subtitle2 {
  font-weight: 600;
  letter-spacing: 0.3px;
}
</style>
