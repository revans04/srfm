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
import { ref, computed } from 'vue';
import { QInput } from 'quasar';

// Define props with TypeScript types
const props = withDefaults(
  defineProps<{
    modelValue: number;
    /**
     * When true, allow the value to be negative. Pressing `-` (or NumPad
     * Subtract) toggles the sign of the current value. Default false —
     * most amount fields (transactions, targets) are sign-encoded elsewhere
     * (e.g. via the income/expense toggle), so they stay locked positive.
     * Use on fields like fund carryover where a negative value is meaningful
     * (overspent / in the hole).
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

// Computed property for displaying the value with two decimal places
const displayValue = computed(() => {
  if (!props.modelValue) return 0.0;
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
  if (selected && /^\d$/.test(key)) {
    emit('update:modelValue', parseInt(key) / 100);
  } else if (/^\d$/.test(key)) {
    event.preventDefault();
    const cents = Math.round(Math.abs(props.modelValue) * 100);
    const newCents = cents * 10 + parseInt(key);
    const signed = props.modelValue < 0 ? -newCents : newCents;
    emit('update:modelValue', signed / 100);
  } else if (key === 'Backspace') {
    event.preventDefault();
    const cents = Math.round(Math.abs(props.modelValue) * 100);
    const newCents = Math.floor(cents / 10);
    const signed = props.modelValue < 0 && newCents !== 0 ? -newCents : newCents;
    emit('update:modelValue', signed / 100);
  } else if (props.allowNegative && (key === '-' || key === 'Subtract')) {
    // Toggle the sign of the current value. Lets users negate by pressing `-`
    // anywhere in the field — cursor position is irrelevant since the value
    // is reformatted on every keypress.
    event.preventDefault();
    const next = props.modelValue === 0 ? 0 : -props.modelValue;
    emit('update:modelValue', next);
  }
  // Ignore other keys
}
</script>
