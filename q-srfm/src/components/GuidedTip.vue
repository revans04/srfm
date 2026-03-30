<template>
  <q-banner v-if="showTip" class="guided-tip q-mb-md" rounded>
    <template #avatar>
      <q-icon name="lightbulb" color="primary" />
    </template>
    <slot />
    <template #action>
      <q-btn flat dense label="Got it" color="primary" @click="dismiss" />
    </template>
  </q-banner>
</template>

<script setup lang="ts">
import { useGuidedTour } from '../composables/useGuidedTour';

const props = defineProps<{
  tipId: string;
}>();

const { showTip, dismiss } = useGuidedTour(props.tipId);
</script>

<style scoped>
.guided-tip {
  background: var(--color-surface-subtle);
  border: 1px solid var(--color-outline-soft);
}
</style>
