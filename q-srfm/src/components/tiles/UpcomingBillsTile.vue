<template>
  <q-card flat bordered class="dashboard-tile">
    <q-card-section class="row items-center justify-between q-px-md q-py-sm">
      <div class="text-subtitle2 q-mb-none">Upcoming Bills</div>
      <q-btn dense flat icon="event" color="primary" @click="$emit('open')" />
    </q-card-section>
    <q-card-section class="q-pt-xs q-px-md q-pb-md">
      <div class="text-body2 text-grey-7">
        Connect or import statements to see upcoming bills.
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
defineEmits<{ (e: 'open'): void }>();
</script>

<style scoped>
.dashboard-tile {
  min-height: 150px;
  border-radius: 12px;
  background-color: #ffffff;
}
.text-subtitle2 {
  font-weight: 600;
  letter-spacing: 0.3px;
}
</style>
