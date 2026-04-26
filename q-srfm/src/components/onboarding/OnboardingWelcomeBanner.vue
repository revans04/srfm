<!--
  Renders once on /budget when arriving via the post-seed redirect. Calm,
  non-celebratory copy per the design system rule ("calm, not cheerleading"
  — no emoji, no "Great job!"). Per-browser dismissal via localStorage; once
  dismissed it never resurfaces for that user on that machine.
-->
<template>
  <q-banner
    v-if="visible"
    rounded
    class="onboarding-welcome bg-grey-2 q-mb-md"
  >
    <template #avatar>
      <q-icon name="celebration" color="primary" size="28px" />
    </template>
    <div class="text-body1 text-weight-medium q-mb-xs">You're all set.</div>
    <div class="text-body2 text-grey-8">
      Your budget for {{ monthLabel }} is ready. Add a transaction to start
      tracking, or edit your category targets above.
    </div>
    <template #action>
      <q-btn flat label="Got it" color="primary" @click="dismiss" />
    </template>
  </q-banner>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';

const props = defineProps<{
  /** YYYY-MM string of the budget that was seeded; used in the body copy. */
  month?: string;
}>();

const STORAGE_KEY = 'srfm-onboarding-welcome-dismissed';
const visible = ref(false);

const monthLabel = computed(() => {
  if (!props.month) return 'this month';
  // Render as "April 2026". Falls back to the raw string if Date parsing fails.
  const [yearStr, monthStr] = props.month.split('-');
  const year = Number(yearStr);
  const month = Number(monthStr);
  if (!year || !month) return props.month;
  const d = new Date(year, month - 1, 1);
  return d.toLocaleString('en-US', { month: 'long', year: 'numeric' });
});

function dismiss() {
  visible.value = false;
  try {
    localStorage.setItem(STORAGE_KEY, '1');
  } catch {
    // localStorage disabled (private mode etc.) — banner just won't be sticky.
  }
}

onMounted(() => {
  try {
    visible.value = localStorage.getItem(STORAGE_KEY) !== '1';
  } catch {
    visible.value = true;
  }
});
</script>

<style scoped>
.onboarding-welcome {
  border: 1px solid var(--q-grey-3, #e2e8f0);
}
</style>
