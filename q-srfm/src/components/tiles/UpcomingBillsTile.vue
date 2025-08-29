<template>
  <q-card flat bordered class="tile">
    <q-card-section class="row items-center justify-between">
      <div class="col text-subtitle2">Upcoming Bills</div>
      <q-btn dense flat icon="event" @click="$emit('open')" />
    </q-card-section>
    <q-separator />
    <q-card-section>
      <div class="text-body2">Connect or import statements to see upcoming bills.</div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
defineEmits<{ (e: 'open'): void }>();
</script>

<style scoped>
.tile { min-height: 150px; }
</style>

