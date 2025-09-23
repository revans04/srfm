<template>
  <q-card flat bordered class="budget-transaction-list column">
    <q-card-section class="row items-center no-wrap">
      <div class="col text-subtitle2 text-uppercase">Recent Transactions</div>
      <div class="col-auto q-ml-sm">
        <q-btn dense flat icon="refresh" @click="$emit('refresh')" title="Reload" />
      </div>
      <div class="col-auto">
        <q-btn dense flat icon="add" color="primary" @click="$emit('add')" title="Add Transaction" />
      </div>
    </q-card-section>
    <q-separator />
    <div class="q-pa-sm">
      <q-input
        dense
        v-model="search"
        debounce="200"
        placeholder="Search transactions"
        clearable
        prepend-icon="search"
      />
    </div>
    <q-scroll-area class="transactions-scroll">
      <q-list dense separator>
        <q-item
          v-for="tx in filteredTransactions"
          :key="tx.key"
          clickable
          class="transaction-row"
          @click="$emit('edit', tx.original)"
        >
          <q-item-section>
            <div class="text-caption text-grey-7">{{ tx.displayDate }}</div>
            <div class="text-body2 ellipsis">{{ tx.merchant || (tx.original.isIncome ? 'Income' : 'Transaction') }}</div>
          </q-item-section>
          <q-item-section side class="text-right">
            <div :class="tx.original.isIncome ? 'text-positive' : tx.signedAmount < 0 ? 'text-negative' : ''">
              {{ tx.amountDisplay }}
            </div>
          </q-item-section>
        </q-item>
        <q-item v-if="filteredTransactions.length === 0">
          <q-item-section class="text-caption text-grey">No transactions</q-item-section>
        </q-item>
      </q-list>
    </q-scroll-area>
  </q-card>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { Transaction } from '../types';
import { formatCurrency } from '../utils/helpers';

const props = defineProps<{
  transactions: Transaction[];
}>();

const search = ref('');

const preparedTransactions = computed(() => {
  return (props.transactions || [])
    .filter((tx) => !tx.deleted)
    .slice()
    .sort((a, b) => {
      const da = a.date ? new Date(a.date).getTime() : 0;
      const db = b.date ? new Date(b.date).getTime() : 0;
      return db - da;
    })
    .slice(0, 50)
    .map((tx, index) => {
      const signedAmount = tx.isIncome ? Math.abs(tx.amount) : -Math.abs(tx.amount || 0);
      const amountDisplay = formatCurrency(signedAmount);
      const displayDate = tx.date
        ? new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(new Date(tx.date))
        : '';
      return {
        original: tx,
        key: tx.id || `${tx.date || 'tx'}-${index}`,
        merchant: tx.merchant || '',
        amountDisplay,
        signedAmount,
        displayDate,
      };
    });
});

const filteredTransactions = computed(() => {
  const term = search.value.trim().toLowerCase();
  if (!term) return preparedTransactions.value;
  return preparedTransactions.value.filter((tx) =>
    tx.merchant.toLowerCase().includes(term) || tx.amountDisplay.toLowerCase().includes(term),
  );
});
</script>

<style scoped>
.budget-transaction-list {
  height: 100%;
  min-height: 400px;
}

.transactions-scroll {
  flex: 1;
  max-height: calc(100vh - 280px);
}

.transaction-row {
  transition: background-color 0.2s ease;
}

.transaction-row:hover {
  background-color: rgba(0, 0, 0, 0.04);
}
</style>
