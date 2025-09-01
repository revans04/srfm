<template>
  <q-chip v-if="goal" dense color="primary" text-color="white" icon="savings">{{ goal.name }}</q-chip>
</template>

<script setup lang="ts">
import type { Goal } from '../../types';
const { goal } = defineProps<{ goal?: Goal }>();
</script>
