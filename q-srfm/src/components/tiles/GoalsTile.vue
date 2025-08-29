<template>
  <q-card flat bordered class="tile">
    <q-card-section class="row items-center justify-between">
      <div class="col text-subtitle2">Goals</div>
      <q-btn dense flat icon="add" @click="$emit('create')" title="Create Goal" />
    </q-card-section>
    <q-separator />
    <q-card-section>
      <div class="text-body2">Coming soon: savings and debt goals.</div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
defineEmits<{ (e: 'create'): void }>();
</script>

<style scoped>
.tile { min-height: 150px; }
</style>

