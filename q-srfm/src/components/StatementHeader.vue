<template>
  <q-card class="panel-card statement-card">
    <div class="row items-center q-col-gutter-sm">
      <div class="col-auto text-subtitle2">Statement (placeholder)</div>
      <div class="col">
        <q-input dense outlined label="Beginning Balance" v-model="begin" type="number" class="q-mr-sm" />
      </div>
      <div class="col">
        <q-input dense outlined label="Ending Balance" v-model="end" type="number" />
      </div>
      <div class="col-auto">
        <q-linear-progress :value="0.5" color="positive" style="width: 160px" />
      </div>
    </div>
  </q-card>
</template>

<script setup lang="ts">
import { ref } from 'vue';
const begin = ref(0);
const end = ref(0);
</script>

<style scoped>
.statement-card {
  padding: 16px;
}
</style>
