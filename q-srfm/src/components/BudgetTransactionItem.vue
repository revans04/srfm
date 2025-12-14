<template>
  <q-item clickable class="rounded-lg q-my-sm" @click="handleSelect">
    <div class="row items-center q-col-gutter-md q-py-sm q-px-sm">
      <div
        class="transaction-date-pill column items-center justify-center text-caption text-uppercase text-primary"
      >
        <div>{{ formatTransactionMonthShort(transaction.date) }}</div>
        <div class="text-body2">{{ formatTransactionDay(transaction.date) }}</div>
      </div>
      <div class="col">
        <div class="text-body1">{{ transaction.merchant || 'Unnamed transaction' }}</div>
        <div class="row items-center q-gutter-sm text-caption text-muted transaction-meta">
          <span>{{ formatTransactionCategories() }}</span>
          <span>•</span>
          <span>{{ formatTransactionDateLong() }}</span>
          <template v-if="goal">
            <span>•</span>
            <GoalFundingPill :goal="goal" class="goal-pill" />
          </template>
        </div>
      </div>
      <div class="col-auto text-right transaction-amount-wrapper">
        <div class="text-body1" :class="transaction.isIncome ? 'text-positive' : 'text-negative'">
          {{ transaction.isIncome ? '+' : '-' }}{{ formatTransactionAmount() }}
        </div>
      </div>
      <div v-if="removable" class="col-auto text-right transaction-actions">
        <q-btn flat dense round icon="delete" color="negative" @click.stop="handleDelete">
          <q-tooltip>Delete transaction</q-tooltip>
        </q-btn>
      </div>
    </div>
  </q-item>
</template>

<script setup lang="ts">
import type { Goal, Transaction } from '../types';
import { toDollars, toCents, formatCurrency, formatDateLong } from '../utils/helpers';
import GoalFundingPill from './transactions/GoalFundingPill.vue';

const props = defineProps<{
  transaction: Transaction;
  categoryName?: string;
  goal?: Goal | null;
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

function formatTransactionCategories(): string {
  if (!props.transaction.categories || props.transaction.categories.length === 0) {
    return 'Uncategorized';
  }
  return props.transaction.categories.map((c) => c.category).join(', ');
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

function formatTransactionDateLong(): string {
  if (!props.transaction.date) return '--';
  try {
    return formatDateLong(props.transaction.date);
  } catch (err) {
    console.warn('Unable to format transaction date', props.transaction.date, err);
    return '--';
  }
}
</script>

<style scoped>
.transaction-date-pill {
  width: 54px;
  height: 64px;
  border-radius: 10px;
  background: rgba(15, 23, 42, 0.12);
  font-weight: 600;
}

.transaction-meta {
  flex-wrap: wrap;
  gap: 4px;
}

.goal-pill {
  display: inline-flex;
  align-items: center;
}

.transaction-amount-wrapper {
  min-width: 90px;
}

.transaction-amount-wrapper .text-body1 {
  word-break: keep-all;
}

.transaction-actions {
  flex-shrink: 0;
}
</style>
