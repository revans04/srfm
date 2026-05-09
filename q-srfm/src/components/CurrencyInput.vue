<template>
  <q-input
    ref="textField"
    :value="displayValue"
    :modelValue="displayValue"
    @focus="handleFocus"
    @keydown="handleKeydown"
    inputmode="numeric"
    type="text"
    v-bind="$attrs"
    dense
  />
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { QInput } from 'quasar';

// Define props with TypeScript types
const props = withDefaults(
  defineProps<{
    modelValue: number;
    /**
     * When true, allow the value to be negative. Pressing `-` (or NumPad
     * Subtract) toggles the sign — including at zero, where it sets a
     * "negative-intent" state so subsequent digits are entered as negative.
     * Default false — most amount fields (transactions, targets) are
     * sign-encoded elsewhere (e.g. via the income/expense toggle) and stay
     * locked positive. Use on fields like fund carryover where a negative
     * value is meaningful (overspent / in the hole).
     */
    allowNegative?: boolean;
  }>(),
  { allowNegative: false },
);

// Define emits with TypeScript types
const emit = defineEmits<{
  (e: 'update:modelValue', value: number): void;
}>();

// Reactive reference to the text field component
const textField = ref<InstanceType<typeof QInput> | null>(null);

// Tracks whether the user wants the next entered digits to be negative,
// even when the current numeric value is zero (where the sign cannot be
// represented in modelValue alone). Kept in sync with the actual sign of
// modelValue whenever it's non-zero, so external prop changes don't get
// stuck in stale negative-intent.
const isNegative = ref(props.modelValue < 0);
watch(
  () => props.modelValue,
  (v) => {
    if (v < 0) isNegative.value = true;
    else if (v > 0) isNegative.value = false;
    // v === 0: keep current intent so `-` then digits works.
  },
);

// Computed property for displaying the value with two decimal places.
// At zero we still surface the negative-intent as `-0.00` so the user
// gets visual confirmation after pressing `-` on an empty field.
const displayValue = computed(() => {
  if (!props.modelValue) return props.allowNegative && isNegative.value ? '-0.00' : '0.00';
  return props.modelValue.toFixed(2);
});

// Function to select the input text on focus
function handleFocus() {
  if (textField.value) textField.value.select();
}

// Function to handle keydown events for numeric input
function handleKeydown(event: KeyboardEvent) {
  const key = event.key;
  const selection = window.getSelection();
  const selected = selection !== null && selection.toString().length > 0;
  const sign = props.allowNegative && isNegative.value ? -1 : 1;
  if (selected && /^\d$/.test(key)) {
    emit('update:modelValue', (sign * parseInt(key)) / 100);
  } else if (/^\d$/.test(key)) {
    event.preventDefault();
    const cents = Math.round(Math.abs(props.modelValue) * 100);
    const newCents = cents * 10 + parseInt(key);
    emit('update:modelValue', (sign * newCents) / 100);
  } else if (key === 'Backspace') {
    event.preventDefault();
    const cents = Math.round(Math.abs(props.modelValue) * 100);
    const newCents = Math.floor(cents / 10);
    emit('update:modelValue', (sign * newCents) / 100);
  } else if (props.allowNegative && (key === '-' || key === 'Subtract')) {
    // Toggle negative intent. If the current value is non-zero, also flip
    // its sign so the displayed amount matches. At zero we just toggle
    // intent — the next digit will pick up the sign.
    event.preventDefault();
    isNegative.value = !isNegative.value;
    if (props.modelValue !== 0) {
      emit('update:modelValue', -props.modelValue);
    }
  }
  // Ignore other keys
}
</script>
