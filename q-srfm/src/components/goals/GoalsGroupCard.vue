<template>
  <q-card class="goals-card">
    <q-card-section class="goals-card__inner">
      <div class="goals-card__header">
        <div class="goals-card__title">Savings Goals</div>
        <q-btn
          flat
          dense
          round
          icon="add"
          color="primary"
          aria-label="Add goal"
          @click="$emit('add')"
        >
          <q-tooltip>Add goal</q-tooltip>
        </q-btn>
      </div>

      <div v-if="goals.length === 0" class="goals-empty">
        <q-avatar size="40px" class="goals-empty__avatar" text-color="white" color="primary">
          <q-icon name="savings" size="22px" />
        </q-avatar>
        <div class="goals-empty__text">
          <div class="goals-empty__title">No savings goals yet</div>
          <div class="goals-empty__subtitle">Set aside money for things you're working toward.</div>
        </div>
        <q-btn
          unelevated
          no-caps
          color="primary"
          icon="add"
          label="Add a goal"
          @click="$emit('add')"
        />
      </div>

      <ul v-else class="goal-list">
        <li
          v-for="goal in goals"
          :key="goal.id"
          class="goal-row"
          tabindex="0"
          role="button"
          :aria-label="`Open ${goal.name} goal`"
          @click="$emit('view', goal)"
          @keydown.enter.prevent="$emit('view', goal)"
          @keydown.space.prevent="$emit('view', goal)"
        >
          <q-avatar size="36px" class="goal-row__avatar" text-color="white" color="primary">
            <q-icon name="savings" size="18px" />
          </q-avatar>

          <div class="goal-row__body">
            <div class="goal-row__top">
              <div class="goal-row__name">{{ goal.name }}</div>
              <div class="goal-row__saved">
                <span class="goal-row__saved-amount">{{ formatCurrency(savedFor(goal)) }}</span>
                <span class="goal-row__saved-of"> of {{ formatCurrency(goal.totalTarget) }}</span>
              </div>
            </div>

            <q-linear-progress
              :value="progressFor(goal)"
              :color="progressColor(goal)"
              track-color="grey-3"
              rounded
              size="6px"
              class="goal-row__progress"
            />

            <div class="goal-row__meta">
              <span>Monthly target {{ formatCurrency(goal.monthlyTarget) }}</span>
              <span :class="remainingClass(goal)">{{ remainingLabel(goal) }}</span>
            </div>
          </div>

          <q-btn
            v-if="!isMobile"
            unelevated
            no-caps
            size="sm"
            icon="add"
            label="Contribute"
            class="btn-soft--primary goal-row__contribute"
            aria-label="Contribute to this goal"
            @click.stop="$emit('contribute', goal)"
          />
          <q-btn
            v-else
            flat
            dense
            round
            icon="add"
            color="primary"
            aria-label="Contribute to this goal"
            class="goal-row__contribute-mobile"
            @click.stop="$emit('contribute', goal)"
          >
            <q-tooltip>Contribute</q-tooltip>
          </q-btn>
        </li>
      </ul>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { useQuasar } from 'quasar';
import { formatCurrency } from '../../utils/helpers';
import { useGoals } from '../../composables/useGoals';
import type { Goal } from '../../types';

const props = defineProps<{ entityId: string }>();
defineEmits<{
  (e: 'add'): void;
  (e: 'view', goal: Goal): void;
  (e: 'contribute', goal: Goal): void;
}>();
const { listGoals, loadGoals } = useGoals();
const $q = useQuasar();
const isMobile = $q.platform.is.mobile;

// Drive the displayed list directly off the shared `useGoals` store via a
// computed so any write that touches that store (contribute, delete, edit,
// archive, etc. — anywhere in the app) re-renders this card without
// requiring its parent to manually call load(). Previously this was a local
// ref populated only on mount/entityId-change, which is why deleting a
// contribution from GoalDetailsPanel left the savings-goals card showing
// stale numbers until the user refreshed the page.
const goals = computed(() => listGoals(props.entityId));

function savedFor(goal: Goal): number {
  // Backend rollup already folds opening_balance into savedToDate; default
  // to 0 if the field hasn't loaded yet so the row renders without flicker.
  return goal.savedToDate || 0;
}

function progressFor(goal: Goal): number {
  if (!goal.totalTarget || goal.totalTarget <= 0) return savedFor(goal) > 0 ? 1 : 0;
  return Math.min(savedFor(goal) / goal.totalTarget, 1);
}

function progressColor(goal: Goal): string {
  // Goal reached → positive (green). Otherwise primary (brand blue).
  // Avoids the "100% blue" flatness when a goal is fully funded.
  if (goal.totalTarget > 0 && savedFor(goal) >= goal.totalTarget) return 'positive';
  return 'primary';
}

function remainingLabel(goal: Goal): string {
  const remaining = goal.totalTarget - savedFor(goal);
  if (remaining <= 0) return 'Goal reached';
  return `${formatCurrency(remaining)} to go`;
}

function remainingClass(goal: Goal): string {
  return goal.totalTarget > 0 && savedFor(goal) >= goal.totalTarget
    ? 'text-positive text-weight-medium'
    : 'text-muted';
}

onMounted(() => {
  if (props.entityId) void loadGoals(props.entityId);
});
watch(
  () => props.entityId,
  (id) => {
    if (id) void loadGoals(id);
  },
);
</script>

<style scoped>
.goals-card {
  margin-top: 16px;
}

.goals-card__inner {
  padding: 16px 20px;
}

.goals-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.goals-card__title {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

/* Empty state ------------------------------------------------------------ */
.goals-empty {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 12px 4px;
}

.goals-empty__avatar {
  flex-shrink: 0;
  box-shadow: 0 6px 16px rgba(15, 23, 42, 0.18);
}

.goals-empty__text {
  flex: 1;
  min-width: 0;
}

.goals-empty__title {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.goals-empty__subtitle {
  font-size: 0.8rem;
  color: var(--color-text-muted);
  line-height: 1.35;
  margin-top: 2px;
}

/* Goal list -------------------------------------------------------------- */
.goal-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
}

.goal-row {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 12px 8px;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: background-color 0.15s ease;
  outline: none;
}

.goal-row + .goal-row {
  border-top: 1px solid var(--color-divider);
}

.goal-row:hover,
.goal-row:focus-visible {
  background: rgba(37, 99, 235, 0.04);
}

.goal-row:focus-visible {
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.35);
}

.goal-row__avatar {
  flex-shrink: 0;
  box-shadow: 0 4px 10px rgba(15, 23, 42, 0.15);
}

.goal-row__body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.goal-row__top {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
}

.goal-row__name {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.goal-row__saved {
  font-size: 0.85rem;
  white-space: nowrap;
  flex-shrink: 0;
}

.goal-row__saved-amount {
  font-weight: 600;
  color: var(--color-text-primary);
}

.goal-row__saved-of {
  color: var(--color-text-muted);
}

.goal-row__progress {
  border-radius: 999px;
}

.goal-row__meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  font-size: 0.75rem;
  color: var(--color-text-muted);
}

.goal-row__contribute {
  flex-shrink: 0;
  /* btn-soft--primary already provides the soft blue. Tighten padding so it
     reads as a row-level shortcut, not a page-level CTA. */
  padding: 4px 12px;
  font-weight: 600;
}

.goal-row__contribute-mobile {
  flex-shrink: 0;
  /* Generous tap target for mobile (44×44 minimum per design rules) */
  min-width: 44px;
  min-height: 44px;
}
</style>
