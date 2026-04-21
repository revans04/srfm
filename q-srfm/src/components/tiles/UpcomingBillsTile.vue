<template>
  <q-card class="dashboard-tile">
    <q-card-section class="row items-center justify-between q-px-md q-py-sm">
      <div class="text-subtitle2 q-mb-none">Upcoming Bills</div>
      <q-btn dense flat icon="event" color="primary" @click="$emit('open')">
        <q-tooltip>Open upcoming bills</q-tooltip>
      </q-btn>
    </q-card-section>
    <q-card-section class="q-pt-xs q-px-md q-pb-md">
      <div class="text-body2 text-grey-7">
        No upcoming bills found.
        <router-link to="/data" class="text-primary">Import bank data</router-link> to track bills.
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
defineEmits<{ (e: 'open'): void }>();
</script>

