<template>
  <div>
    <q-banner v-if="show" class="bg-warning text-dark q-mb-md" rounded>
      <div class="row items-center no-wrap">
        <div class="col">
          <div class="text-body2">We detected legacy Savings categories. Convert them to the new Savings Goals feature.</div>
          <ul class="q-pl-md q-my-sm row">
            <div v-for="c in categories" :key="c.name" class="col-3 items-center q-gutter-xs">
              <div>{{ c.name }}</div>
            </div>
          </ul>
        </div>
        <div class="col-auto">
          <q-btn dense flat round icon="close" @click="dismiss" />
        </div>
      </div>
    </q-banner>

  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { BudgetCategory } from 'src/types';

defineProps<{ categories: BudgetCategory[] }>();

const show = ref(true);

function dismiss() {
  show.value = false;
}
</script>
