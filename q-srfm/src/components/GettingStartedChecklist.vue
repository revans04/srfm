<template>
  <q-card v-if="!tourStore.allComplete" flat bordered class="getting-started-card">
    <q-card-section>
      <div class="row items-center justify-between q-mb-sm">
        <div class="text-subtitle1 text-weight-medium">Getting Started</div>
        <q-badge color="primary" outline>{{ tourStore.completedCount }} of {{ tourStore.totalCount }}</q-badge>
      </div>
      <q-linear-progress
        :value="tourStore.completedCount / tourStore.totalCount"
        color="primary"
        track-color="grey-3"
        rounded
        size="8px"
        class="q-mb-md"
      />
      <q-list dense separator>
        <q-item v-for="item in tourStore.checklist" :key="item.id" dense>
          <q-item-section avatar>
            <q-icon
              :name="item.completed ? 'check_circle' : 'radio_button_unchecked'"
              :color="item.completed ? 'positive' : 'grey-5'"
              size="20px"
            />
          </q-item-section>
          <q-item-section :class="{ 'text-strike text-grey-5': item.completed }">
            {{ item.label }}
          </q-item-section>
        </q-item>
      </q-list>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useTourStore } from '../store/tour';

const tourStore = useTourStore();

onMounted(() => {
  if (!tourStore.initialized) {
    tourStore.loadTourState();
  }
});
</script>

<style scoped>
.getting-started-card {
  border-radius: var(--radius-md);
}
</style>
