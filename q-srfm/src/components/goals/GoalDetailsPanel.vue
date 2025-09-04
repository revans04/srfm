<template>
  <q-page class="category-transactions text-black bg-grey-2 q-pa-md">
    <div class="row header items-center">
      <div class="col">
        <h2 class="category-title">{{ goal.name }}</h2>
      </div>
      <div class="col-auto">
        <q-btn flat dense icon="close" class="q-mt-sm" @click="$emit('close')" />
      </div>
    </div>
    <div class="row q-mt-sm">
      <div class="col">
        <div class="progress-section">
          <div class="progress-label">
            <span>{{ formatCurrency(saved) }}</span>
            saved of
            {{ formatCurrency(target) }}
          </div>
          <q-linear-progress
            :value="progress"
            height="10"
            color="primary"
            background-color="#e0e0e0"
            rounded
          />
        </div>
      </div>
    </div>
    <q-tabs v-model="tab" dense class="bg-grey-2 q-mt-md">
      <q-tab name="contribs" label="Contributions" />
      <q-tab name="spend" label="Goal Spend" />
    </q-tabs>
    <q-tab-panels v-model="tab" animated class="q-mt-md">
      <q-tab-panel name="contribs">
        <q-list dense>
          <q-item v-for="row in contribRows" :key="row.month">
            <q-item-section>{{ row.month }}</q-item-section>
            <q-item-section side>{{ formatCurrency(row.amount) }}</q-item-section>
          </q-item>
          <q-item v-if="contribRows.length === 0">
            <q-item-label>No contributions.</q-item-label>
          </q-item>
        </q-list>
      </q-tab-panel>
      <q-tab-panel name="spend">
        <q-list dense>
          <q-item v-for="row in spendRows" :key="row.txId">
            <q-item-section>{{ formatDate(row.txDate) }}</q-item-section>
            <q-item-section side>{{ formatCurrency(row.amount) }}</q-item-section>
          </q-item>
          <q-item v-if="spendRows.length === 0">
            <q-item-label>No goal spend.</q-item-label>
          </q-item>
        </q-list>
      </q-tab-panel>
    </q-tab-panels>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { formatCurrency, formatDate } from '../../utils/helpers';
import { useGoals } from '../../composables/useGoals';
import type { Goal } from '../../types';

const props = defineProps<{ goal: Goal }>();
defineEmits<{ (e: 'close'): void }>();

const tab = ref('contribs');

const { listContributions, listGoalSpends } = useGoals();

const contribRows = computed(() => listContributions(props.goal.id));
const spendRows = computed(() => listGoalSpends(props.goal.id));

const saved = computed(() => props.goal.savedToDate || 0);
const target = computed(() => props.goal.totalTarget || 0);
const progress = computed(() => (target.value ? saved.value / target.value : 0));
</script>

<style scoped>
.category-title {
  margin: 0;
}
.progress-section {
  margin-bottom: 8px;
}
.progress-label {
  font-size: 14px;
  margin-bottom: 4px;
}
</style>
