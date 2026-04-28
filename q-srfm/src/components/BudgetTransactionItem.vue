<template>
  <q-item clickable @click="handleSelect" class="tx-item">
    <q-item-section avatar class="tx-item__date-col">
      <div class="tx-date">
        <div class="tx-date__month">{{ formatTransactionMonthShort(transaction.date) }}</div>
        <div class="tx-date__day">{{ formatTransactionDay(transaction.date) }}</div>
      </div>
    </q-item-section>

    <q-item-section>
      <q-item-label class="tx-item__merchant">
        <q-icon v-if="isTransfer" name="swap_horiz" color="info" size="16px" class="q-mr-xs" />
        {{ transaction.merchant || 'Unnamed' }}
        <q-icon v-if="transaction.recurring" name="repeat" color="grey-5" size="14px" class="q-ml-xs">
          <q-tooltip>Recurring ({{ transaction.recurringInterval || 'Monthly' }})</q-tooltip>
        </q-icon>
      </q-item-label>
      <q-item-label v-if="isTransfer && counterpartCategory" caption class="tx-item__category tx-item__category--transfer">
        {{ transferDirection }}
      </q-item-label>
      <q-item-label v-else-if="categoryLabel" caption class="tx-item__category">{{ categoryLabel }}</q-item-label>
    </q-item-section>

    <q-item-section side class="tx-item__right">
      <span class="tx-item__amount" :class="isTransfer ? 'text-info' : (transaction.isIncome ? 'text-positive' : 'text-negative')">
        {{ isTransfer ? '' : (transaction.isIncome ? '+' : '-') }}{{ formatTransactionAmount() }}
      </span>
    </q-item-section>

    <q-item-section v-if="removable" side class="tx-item__action">
      <q-btn
        flat
        round
        dense
        icon="delete_outline"
        color="grey-5"
        size="xs"
        @click.stop="handleDelete"
        style="min-width: 36px; min-height: 36px;"
      >
        <q-tooltip>Mark as deleted — can be restored from Deleted tab</q-tooltip>
      </q-btn>
    </q-item-section>
  </q-item>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Transaction } from '../types';
import { toDollars, toCents, formatCurrency } from '../utils/helpers';

const props = defineProps<{
  transaction: Transaction;
  categoryName?: string;
  removable?: boolean;
  goal?: { name: string } | null;
}>();
const emit = defineEmits<{
  (e: 'select', tx: Transaction): void;
  (e: 'delete', tx: Transaction): void;
}>();

const isTransfer = computed(() => props.transaction.transactionType === 'transfer');

const counterpartCategory = computed(() => {
  if (!isTransfer.value || !props.categoryName) return '';
  const cats = props.transaction.categories;
  if (!cats || cats.length !== 2) return '';
  const other = cats.find((c) => c.category !== props.categoryName);
  return other?.category || '';
});

const transferDirection = computed(() => {
  if (!isTransfer.value || !props.categoryName) return '';
  const cats = props.transaction.categories;
  if (!cats || cats.length !== 2) return '';
  const thisSplit = cats.find((c) => c.category === props.categoryName);
  if (!thisSplit) return '';
  // Negative amount = money leaving this category (source)
  // Positive amount = money entering this category (destination)
  return thisSplit.amount < 0
    ? `→ ${counterpartCategory.value}`
    : `← ${counterpartCategory.value}`;
});

const categoryLabel = computed(() => {
  if (props.categoryName) return props.categoryName;
  const cats = props.transaction.categories;
  if (!cats || cats.length === 0) return '';
  if (cats.length === 1) return cats[0].category;
  return cats.map((c) => c.category).join(', ');
});

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

/**
 * Pick the right amount to display for this row.
 *
 * - When a categoryName is supplied (i.e. we're rendering inside a single
 *   category panel), use that category's split amount — the category-scoped
 *   value is what the user expects.
 * - Otherwise (whole-budget transactions list), fall back through:
 *     1. transaction.amount when set
 *     2. for transfers: the absolute of the largest single split (= the
 *        transfer amount, since the two sides cancel in a sum)
 *     3. for everything else: the sum of |split.amount| across categories
 *
 * Step 2 + 3 heal a pre-existing data inconsistency where a transaction was
 * persisted with amount=0 but populated splits — the row would otherwise
 * show $0 in the unmatched list while the category panel correctly showed
 * the split value.
 */
function deriveTransactionAmount(): number {
  const stored = Number(props.transaction.amount || 0);
  if (stored !== 0) return stored;
  const splits = props.transaction.categories || [];
  if (splits.length === 0) return 0;
  if (props.transaction.transactionType === 'transfer') {
    return splits.reduce((max, s) => Math.max(max, Math.abs(Number(s.amount) || 0)), 0);
  }
  return splits.reduce((sum, s) => sum + Math.abs(Number(s.amount) || 0), 0);
}

function formatTransactionAmount(): string {
  const amountSource = props.categoryName ? getCategoryAmount() : deriveTransactionAmount();
  const amount = Math.abs(Number(amountSource) || 0);
  return formatCurrency(toDollars(toCents(amount)));
}
</script>

<style scoped>
.tx-item {
  min-height: 48px;
  padding: 4px 8px;
}

.tx-item__date-col {
  min-width: 40px;
  padding-right: 8px;
}

.tx-date {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  border: 1.5px solid var(--color-outline-soft);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1px;
}

.tx-date__month {
  font-size: 0.6rem;
  font-weight: 500;
  text-transform: uppercase;
  line-height: 1;
  color: var(--color-text-muted);
}

.tx-date__day {
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1;
  color: var(--color-text-primary);
}

.tx-item__merchant {
  font-size: 0.85rem;
  font-weight: 500;
  line-height: 1.3;
}

.tx-item__category {
  font-size: 0.75rem;
  color: var(--q-primary);
}

.tx-item__category--transfer {
  color: var(--q-info);
}

.tx-item__right {
  padding-left: 4px;
}

.tx-item__amount {
  font-size: 0.85rem;
  font-weight: 600;
  white-space: nowrap;
}

.tx-item__action {
  padding-left: 0;
  min-width: 36px;
}
</style>
