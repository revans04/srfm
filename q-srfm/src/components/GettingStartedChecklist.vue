<template>
  <q-expansion-item
    v-if="!tourStore.allComplete"
    dense
    header-class="getting-started-header"
    class="getting-started-card"
    default-opened
  >
    <template #header>
      <q-item-section>
        <q-item-label class="text-caption text-weight-bold">Getting Started</q-item-label>
      </q-item-section>
      <q-item-section side>
        <q-badge color="primary" outline class="text-caption">{{ tourStore.completedCount }}/{{ tourStore.totalCount }}</q-badge>
      </q-item-section>
    </template>
    <q-linear-progress
      :value="tourStore.completedCount / tourStore.totalCount"
      color="primary"
      track-color="grey-3"
      rounded
      size="4px"
      class="q-mx-md q-mb-sm"
    />
    <q-list dense class="q-px-sm q-pb-sm">
      <q-item v-for="item in tourStore.checklist" :key="item.id" dense class="q-py-none" style="min-height: 28px;">
        <q-item-section avatar style="min-width: 28px;">
          <q-icon
            :name="item.completed ? 'check_circle' : 'radio_button_unchecked'"
            :color="item.completed ? 'positive' : 'grey-5'"
            size="16px"
          />
        </q-item-section>
        <q-item-section :class="{ 'text-strike text-grey-5': item.completed }" class="text-caption">
          {{ item.label }}
        </q-item-section>
      </q-item>
    </q-list>
  </q-expansion-item>
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
  border-radius: var(--radius-sm);
  background: rgba(255, 255, 255, 0.1);
}

.getting-started-header {
  padding: 8px 12px;
  min-height: 36px;
}
</style>
