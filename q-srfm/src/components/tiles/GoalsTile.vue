<template>
  <q-card flat bordered class="tile tile--secondary text-white">
    <q-card-section class="row items-center justify-between">
      <div class="col text-subtitle2">Goals</div>
      <q-btn dense flat round icon="add" color="white" @click="$emit('create')" title="Create Goal" />
    </q-card-section>
    <q-separator class="sep--dark" />
    <q-card-section>
      <div class="text-body2">Coming soon: savings and debt goals.</div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
defineEmits<{ (e: 'create'): void }>();
</script>

<style scoped>
.tile { min-height: 150px; border-radius: 12px; }
.tile--secondary { background: var(--q-secondary); }
.sep--dark { opacity: 0.2; }
.text-subtitle2 { font-weight: 600; letter-spacing: .3px; }
</style>
