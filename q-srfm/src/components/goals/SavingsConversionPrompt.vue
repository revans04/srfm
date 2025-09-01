<template>
  <div>
    <q-banner
      v-if="show"
      class="bg-warning text-dark q-mb-md"
      rounded
    >
      <div class="row items-center no-wrap">
        <div class="col">
          <div class="text-body2">
            We detected legacy Savings categories. Convert them to the new Savings Goals feature.
          </div>
          <ul class="q-pl-md q-my-sm">
            <li
              v-for="c in categories"
              :key="c.name"
              class="row items-center q-gutter-xs"
            >
              <div class="col">{{ c.name }}</div>
              <div class="col-auto">
                <q-btn
                  flat
                  dense
                  size="sm"
                  color="primary"
                  label="Convert"
                  @click="emit('convert', c)"
                />
              </div>
            </li>
          </ul>
          <q-btn
            flat
            color="primary"
            size="sm"
            label="How to convert"
            @click="showGuide = true"
          />
        </div>
        <div class="col-auto">
          <q-btn
            dense
            flat
            round
            icon="close"
            @click="dismiss"
          />
        </div>
      </div>
    </q-banner>

    <q-dialog v-model="showGuide">
      <q-card style="max-width: 500px">
        <q-card-section class="text-h6">Convert to Savings Goals</q-card-section>
        <q-card-section class="text-body2">
          <ol class="q-pl-md">
            <li>Create a Savings Goal for each category listed.</li>
            <li>Match the goal's Total Target and Monthly Target to the category's values.</li>
            <li>Add a contribution equal to the amount you have already saved.</li>
            <li>Archive the old Savings category so it no longer affects your budget.</li>
            <li>Repeat for past budget months where the category appears.</li>
          </ol>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Close" v-close-popup />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { BudgetCategory } from 'src/types';

defineProps<{ categories: BudgetCategory[] }>();
const emit = defineEmits<{ (e: 'convert', category: BudgetCategory): void }>();

const showGuide = ref(false);
const show = ref(true);

function dismiss() {
  show.value = false;
}
</script>
