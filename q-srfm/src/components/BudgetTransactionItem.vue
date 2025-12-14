<template>
  <q-item clickable @click="handleSelect">
    <div class="row q-py-sm full-width">
      <div class="col-auto date-circle column items-center justify-center text-caption text-uppercase q-pt-sm">
        <div class="date-month">{{ formatTransactionMonthShort(transaction.date) }}</div>
        <div class="date-day">{{ formatTransactionDay(transaction.date) }}</div>
      </div>
      <div class="col q-py-md q-pl-sm">
        <div class="text-body2">{{ transaction.merchant || 'Unnamed transaction' }}</div>
      </div>
      <div class="col-auto q-py-md text-right no-wrap">
        <div class="text-body2" :class="transaction.isIncome ? 'text-positive' : 'text-negative'">
          {{ transaction.isIncome ? '+' : '-' }}{{ formatTransactionAmount() }}
        </div>
      </div>
      <div class="col-auto q-py-md text-right no-wrap">
        <q-icon
          v-if="removable"
          name="delete"
          desnse
          class="transaction-delete"
          color="negative"
          size="20px"
          @click.stop="handleDelete"
        />
      </div>
    </div>
  </q-item>
</template>

<script setup lang="ts">
import type { Transaction } from '../types';
import { toDollars, toCents, formatCurrency } from '../utils/helpers';

const props = defineProps<{
  transaction: Transaction;
  categoryName?: string;
  removable?: boolean;
}>();
const emit = defineEmits<{
  (e: 'select', tx: Transaction): void;
  (e: 'delete', tx: Transaction): void;
}>();

function handleSelect() {
  emit('select', props.transaction);
}

function handleDelete() {
  emit('delete', props.transaction);
}

function formatTransactionMonthShort(dateStr?: string): string {
  if (!dateStr) return '--';
  const [yearStr, monthStr] = dateStr.split('-');
  const year = Number(yearStr);
  const month = Number(monthStr);
  const date = new Date(year, month - 1);
  if (Number.isNaN(date.getTime())) return '--';
  return date.toLocaleDateString('en-US', { month: 'short' });
}

function formatTransactionDay(dateStr?: string): string {
  if (!dateStr) return '--';
  const [yearStr, monthStr, dayStr] = dateStr.split('-');
  const year = Number(yearStr);
  const month = Number(monthStr);
  const day = Number(dayStr);
  const date = new Date(year, month - 1, day);
  if (Number.isNaN(date.getTime())) return '--';
  return date.toLocaleDateString('en-US', { day: '2-digit' });
}

function getCategoryAmount(): number {
  if (!props.categoryName) return 0;
  const split = props.transaction.categories?.find((s) => s.category === props.categoryName);
  if (!split) return 0;
  return Number(split.amount) || 0;
}

function formatTransactionAmount(): string {
  const amountSource = props.categoryName ? getCategoryAmount() : Number(props.transaction.amount || 0);
  const amount = Math.abs(Number(amountSource) || 0);
  return formatCurrency(toDollars(toCents(amount)));
}

</script>

<style scoped>
.date-circle {
  width: 54px;
  height: 54px;
  border-radius: 50%;
  border: 2px solid rgba(0, 0, 0, 0.15);
  background: rgba(0, 0, 0, 0.02);
  font-weight: 100;
  gap: 2px;
  text-align: center;
  padding: 0px;
}

.date-month,
.date-day {
  line-height: 1;
}

.date-month {
  font-size: 0.75rem;
}

.date-day {
  font-size: 1.1rem;
}

.transaction-delete {
  cursor: pointer;
}
</style>
