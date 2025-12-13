<template>
  <q-card flat bordered class="dashboard-tile">
    <q-card-section class="row items-center justify-between q-px-md q-py-sm">
      <div class="text-subtitle2 q-mb-none">Net Worth</div>
      <q-btn dense flat icon="refresh" color="primary" :loading="loading" @click="loadNetWorth" />
    </q-card-section>
    <q-card-section class="q-pt-xs q-px-md q-pb-md">
      <div v-if="loading" class="row items-center justify-center q-pa-md">
        <q-spinner size="24px" color="primary" />
      </div>
      <div v-else>
        <div v-if="value !== null" class="text-h5">{{ money(value) }}</div>
        <div v-else class="text-body2 text-grey-7">
          No snapshots yet. Import accounts or capture a snapshot to see net worth.
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { dataAccess } from '../../dataAccess';

interface Snapshot {
  date?: string | { seconds: number; nanoseconds: number };
  netWorth?: number;
  NetWorth?: number;
}

const props = defineProps<{ familyId: string }>();
const loading = ref(false);
const value = ref<number | null>(null);

function money(n: number) {
  const v = Math.round(n * 100) / 100;
  return `$${v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

async function loadNetWorth() {
  if (!props.familyId) return;
  loading.value = true;
  try {
    // Try snapshots first
    const snaps = (await dataAccess.getSnapshots(props.familyId)) as Snapshot[];
    if (snaps && snaps.length > 0) {
      const getEpoch = (d: string | { seconds: number; nanoseconds: number }) => {
        if (!d) return 0;
        if (typeof d === 'string') return Date.parse(d) || 0;
        return d.seconds * 1000 + Math.floor((d.nanoseconds ?? 0) / 1e6);
      };
      const latest = [...snaps].sort((a, b) => getEpoch(a.date) - getEpoch(b.date))[snaps.length - 1];
      value.value = Number(latest.netWorth ?? latest.NetWorth ?? 0) || 0;
      return;
    }
    // Fallback: compute from accounts
    const accounts = await dataAccess.getAccounts(props.familyId);
    if (accounts && accounts.length > 0) {
      const net = accounts.reduce((sum, a) => {
        const val = a.balance ?? a.details?.appraisedValue ?? 0;
        return sum + (a.category === 'Liability' ? -val : val);
      }, 0);
      value.value = net;
    } else {
      value.value = null;
    }
  } catch {
    value.value = null;
  } finally {
    loading.value = false;
  }
}

watch(
  () => props.familyId,
  () => {
    if (props.familyId) void loadNetWorth();
  },
  { immediate: true },
);
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
