<template>
  <q-dialog v-model="model" :maximized="$q.screen.lt.sm">
    <q-card class="goal-dialog">
      <!-- Header: matches the visual language of GoalDetailsPanel hero -->
      <div class="goal-dialog__header">
        <q-avatar size="40px" class="goal-dialog__avatar" text-color="white" color="primary">
          <q-icon name="savings" size="22px" />
        </q-avatar>
        <div class="goal-dialog__title-group">
          <div class="goal-dialog__overline text-caption text-muted">Savings goal</div>
          <div class="goal-dialog__title text-h6">{{ isEditMode ? 'Edit goal' : 'New goal' }}</div>
        </div>
        <q-btn flat dense round icon="close" color="grey-7" v-close-popup>
          <q-tooltip>Cancel</q-tooltip>
        </q-btn>
      </div>

      <q-separator />

      <!-- Body: scrolls when content overflows; consistent vertical rhythm -->
      <q-card-section class="goal-dialog__body">
        <q-input
          v-model="form.name"
          label="Name"
          placeholder="What are you saving for?"
          dense
          outlined
          stack-label
          autofocus
        />

        <div class="goal-dialog__row">
          <CurrencyInput
            v-model.number="form.totalTarget"
            label="Total target"
            dense
            outlined
            stack-label
            class="goal-dialog__row-item"
          />
          <CurrencyInput
            v-model.number="form.monthlyTarget"
            label="Monthly target"
            dense
            outlined
            stack-label
            class="goal-dialog__row-item"
          />
        </div>

        <div class="goal-dialog__field">
          <CurrencyInput
            v-model.number="form.openingBalance"
            label="Already saved"
            dense
            outlined
            stack-label
            hide-bottom-space
          />
          <div class="goal-dialog__hint text-caption text-muted">
            Money you already have toward this goal — counts toward progress without affecting this month's budget.
          </div>
        </div>

        <q-input
          v-model="form.targetDate"
          label="Target date"
          type="date"
          dense
          outlined
          stack-label
        />

        <div class="goal-dialog__field">
          <div class="goal-dialog__editor-label text-caption text-muted">Notes</div>
          <q-editor
            v-model="form.notes"
            :toolbar="editorToolbar"
            min-height="120px"
            content-class="goal-dialog__editor-content"
            class="goal-dialog__editor"
            placeholder="Add details — vendor, trip, school, why this matters."
          />
        </div>
      </q-card-section>

      <q-separator />

      <q-card-actions class="goal-dialog__actions">
        <q-btn flat color="grey-8" label="Cancel" v-close-popup />
        <q-btn
          unelevated
          color="primary"
          :label="isEditMode ? 'Save changes' : 'Create goal'"
          :disable="!canSave"
          @click="onSave"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuasar } from 'quasar';
import CurrencyInput from '../CurrencyInput.vue';
import type { Goal } from '../../types';
import type { Timestamp } from 'firebase/firestore';

const props = defineProps<{ modelValue: boolean; goal?: Goal }>();
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void; (e: 'save', data: Partial<Goal>): void }>();

const $q = useQuasar();
const isEditMode = computed(() => Boolean(props.goal && props.goal.id));

// Focused toolbar — enough for narrative notes (vendor / trip / school /
// rationale) without overwhelming the dialog. Skip image upload (out of
// scope for this field; would need storage wiring) and color/alignment
// controls (rarely useful for short prose).
const editorToolbar = [
  ['bold', 'italic', 'underline', 'strike'],
  [
    {
      label: $q.lang.editor.formatting,
      icon: $q.iconSet.editor.formatting,
      list: 'no-icons',
      options: ['p', 'h4', 'h5', 'h6'],
    },
  ],
  ['unordered', 'ordered'],
  ['link'],
  ['undo', 'redo'],
];
const model = ref(props.modelValue);
watch(
  () => props.modelValue,
  (v) => (model.value = v),
);
watch(model, (v) => emit('update:modelValue', v));

type GoalForm = Omit<Partial<Goal>, 'targetDate'> & { targetDate?: string };
// Initialize numeric fields to avoid undefined being passed to CurrencyInput
const form = ref<GoalForm>({ totalTarget: 0, monthlyTarget: 0, openingBalance: 0 });
function normalizeDate(val: unknown): string | undefined {
  if (!val) return undefined;
  let date: Date | undefined;
  if (val instanceof Date) {
    date = val;
  } else if (typeof val === 'string') {
    date = new Date(val);
  } else if (
    typeof val === 'object' &&
    val !== null &&
    'toDate' in val &&
    typeof (val as { toDate: () => Date }).toDate === 'function'
  ) {
    date = (val as { toDate: () => Date }).toDate();
  } else if (
    typeof val === 'object' &&
    val !== null &&
    'toISOString' in val &&
    typeof (val as { toISOString: () => string }).toISOString === 'function'
  ) {
    // Some libraries provide a toISOString method directly
    date = new Date((val as { toISOString: () => string }).toISOString());
  }
  return date && !isNaN(date.getTime()) ? date.toISOString().slice(0, 10) : undefined;
}

watch(
  () => props.goal,
  (g) => {
    if (g) {
      const { targetDate, totalTarget, monthlyTarget, openingBalance, ...rest } = g as Goal & {
        targetDate?: unknown;
      };
      form.value = {
        ...rest,
        totalTarget: totalTarget ?? 0,
        monthlyTarget: monthlyTarget ?? 0,
        openingBalance: openingBalance ?? 0,
        targetDate: normalizeDate(targetDate),
      };
    } else {
      form.value = { totalTarget: 0, monthlyTarget: 0, openingBalance: 0 };
    }
  },
  { immediate: true },
);

const canSave = computed(() => Boolean(form.value.name && form.value.name.trim().length > 0));

function onSave() {
  if (!canSave.value) return;
  const { targetDate, ...rest } = form.value;
  const payload: Partial<Goal> = { ...rest };
  if (targetDate) {
    payload.targetDate = new Date(targetDate) as unknown as Timestamp;
  }
  emit('save', payload);
  model.value = false;
}
</script>

<style scoped>
.goal-dialog {
  width: 100%;
  max-width: 480px;
  display: flex;
  flex-direction: column;
  /* Prevent the dialog from growing taller than the viewport so the body
     can scroll cleanly while header + actions stay pinned. */
  max-height: 90vh;
  border-radius: var(--radius-lg);
}

/* Mobile: dialog is maximized via :maximized so it fills the screen. Make
   the inner card flex to that height. */
@media (max-width: 599px) {
  .goal-dialog {
    max-width: none;
    max-height: none;
    height: 100%;
    border-radius: 0;
  }
}

.goal-dialog__header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 20px;
}

.goal-dialog__avatar {
  flex-shrink: 0;
  box-shadow: 0 6px 16px rgba(15, 23, 42, 0.2);
}

.goal-dialog__title-group {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.goal-dialog__overline {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.goal-dialog__title {
  font-size: 1.15rem;
  font-weight: 600;
  line-height: 1.2;
}

.goal-dialog__body {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 20px;
  /* Body scrolls when the form is taller than the dialog */
  overflow-y: auto;
}

.goal-dialog__row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

@media (max-width: 420px) {
  .goal-dialog__row {
    grid-template-columns: 1fr;
  }
}

.goal-dialog__field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.goal-dialog__hint {
  line-height: 1.35;
  padding: 0 2px;
}

.goal-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 12px 20px;
}

.goal-dialog__editor-label {
  /* Match the stack-label appearance of the other inputs above. */
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  margin-bottom: 4px;
  padding: 0 2px;
}

.goal-dialog__editor {
  width: 100%;
}

/* Tap targets: Quasar's default toolbar buttons are < 44px on mobile.
   Bumping them only on narrow viewports keeps desktop dense. WCAG / Apple
   guidance is 44×44 minimum for primary touch surfaces. */
@media (max-width: 599px) {
  .goal-dialog__editor :deep(.q-editor__toolbar) {
    flex-wrap: wrap;
    /* Allow the toolbar to scroll horizontally if it still overflows. */
    overflow-x: auto;
  }

  .goal-dialog__editor :deep(.q-editor__toolbar .q-btn) {
    min-width: 44px;
    min-height: 44px;
  }

  .goal-dialog__editor :deep(.q-editor__toolbar .q-btn-dropdown) {
    min-height: 44px;
  }
}

/* Content area: comfortable line-height + a little internal padding so the
   typing surface doesn't feel cramped. */
.goal-dialog__editor :deep(.goal-dialog__editor-content) {
  padding: 8px 10px;
  line-height: 1.45;
}

.goal-dialog__editor :deep(.goal-dialog__editor-content p) {
  margin: 0 0 8px;
}

.goal-dialog__editor :deep(.goal-dialog__editor-content p:last-child) {
  margin-bottom: 0;
}
</style>
