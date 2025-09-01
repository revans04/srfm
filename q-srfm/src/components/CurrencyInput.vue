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
    density="compact"
  />
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { QInput } from 'quasar';

// Define props with TypeScript types
const props = defineProps<{
  modelValue: number;
}>();

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
    const cents = Math.round(props.modelValue * 100);
    const newCents = cents * 10 + parseInt(key);
    emit('update:modelValue', newCents / 100);
  } else if (key === 'Backspace') {
    event.preventDefault();
    const cents = Math.round(props.modelValue * 100);
    const newCents = Math.floor(cents / 10);
    emit('update:modelValue', newCents / 100);
  }
  // Ignore other keys
}
</script>
