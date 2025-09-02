<template>
  <q-card flat bordered class="tile tile--secondary text-white">
    <q-card-section class="row items-center justify-between">
      <div class="col text-subtitle2">Goals</div>
      <q-btn dense flat round icon="add" color="white" @click="$emit('create')" title="Create Goal" />
    </q-card-section>
    <q-separator class="sep--dark" />
    <q-card-section>
      <div v-if="goals.length">
        <div
          v-for="g in goals.slice(0, 3)"
          :key="g.id"
          class="row items-center justify-between q-mb-xs"
        >
          <div>{{ g.name }}</div>
          <div>{{ formatCurrency(g.savedToDate) }} / {{ formatCurrency(g.totalTarget) }}</div>
        </div>
        <div v-if="goals.length > 3" class="text-caption q-mt-xs">
          +{{ goals.length - 3 }} more
        </div>
      </div>
      <div v-else class="text-body2">No active goals</div>
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
.tile { min-height: 150px; border-radius: 12px; }
.tile--secondary { background: var(--q-secondary); }
.sep--dark { opacity: 0.2; }
.text-subtitle2 { font-weight: 600; letter-spacing: .3px; }
</style>
