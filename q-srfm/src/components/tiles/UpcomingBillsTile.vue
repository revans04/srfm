<template>
  <q-card flat bordered class="tile tile--negative text-white">
    <q-card-section class="row items-center justify-between">
      <div class="col text-subtitle2">Upcoming Bills</div>
      <q-btn dense flat round icon="event" color="white" @click="$emit('open')" />
    </q-card-section>
    <q-separator class="sep--dark" />
    <q-card-section>
      <div class="text-body2">Connect or import statements to see upcoming bills.</div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
defineEmits<{ (e: 'open'): void }>();
</script>

<style scoped>
.tile { min-height: 150px; border-radius: 12px; }
.tile--negative { background: var(--q-negative); }
.sep--dark { opacity: 0.2; }
.text-subtitle2 { font-weight: 600; letter-spacing: .3px; }
</style>
