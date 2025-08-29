<template>
  <q-card flat bordered class="tile">
    <q-card-section class="row items-center justify-between">
      <div class="col text-subtitle2">Net Worth</div>
      <q-btn dense flat icon="refresh" :loading="loading" @click="loadNetWorth" />
    </q-card-section>
    <q-separator />
    <q-card-section>
      <div v-if="loading" class="row items-center justify-center q-pa-md">
        <q-spinner size="24px" color="primary" />
      </div>
      <div v-else>
        <div v-if="value !== null" class="text-h5">{{ money(value) }}</div>
        <div v-else class="text-body2">
          No snapshots yet. Import accounts or capture a snapshot to see net worth.
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { dataAccess } from '../../dataAccess';

const props = defineProps<{ familyId: string }>();
const loading = ref(false);
const value = ref<number | null>(null);

function money(n: number) {
  const s = (Math.round(n * 100) / 100).toFixed(2);
  return `$${s}`;
}

async function loadNetWorth() {
  if (!props.familyId) return;
  loading.value = true;
  try {
    // Try snapshots first
    const snaps = await dataAccess.getSnapshots(props.familyId);
    if (snaps && snaps.length > 0) {
      const getEpoch = (d: string | { seconds: number; nanoseconds: number }) => {
        if (!d) return 0;
        if (typeof d === 'string') return Date.parse(d) || 0;
        return d.seconds * 1000 + Math.floor((d.nanoseconds ?? 0) / 1e6);
      };
      const latest = [...snaps].sort((a, b) => getEpoch((a as any).date) - getEpoch((b as any).date))[snaps.length - 1] as Record<string, unknown>;
      value.value = Number((latest.netWorth as number | undefined) ?? (latest.NetWorth as number | undefined) ?? 0) || 0;
      return;
    }
    // Fallback: compute from accounts
    const accounts = await dataAccess.getAccounts(props.familyId);
    if (accounts && accounts.length > 0) {
      const net = accounts.reduce((sum, a) => {
        const val = (a.balance ?? a.details?.appraisedValue ?? 0) as number;
        return sum + (a.category === 'Liability' ? -val : val);
      }, 0);
      value.value = net;
    } else {
      value.value = null;
    }
  } catch (e) {
    value.value = null;
  } finally {
    loading.value = false;
  }
}

watch(
  () => props.familyId,
  (val) => {
    if (val) void loadNetWorth();
  },
  { immediate: true },
);
</script>

<style scoped>
.tile { min-height: 150px; }
</style>
