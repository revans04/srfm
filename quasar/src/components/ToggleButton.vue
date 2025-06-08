<!-- src/components/ToggleButton.vue -->
<template>
  <div class="toggle-container" :class="$props.cssClass">
    <q-btn
      :class="{
        active: selectedOption === true,
        inactive: selectedOption !== true,
      }"
      @click="selectOption(true)"
      class="toggle-btn"
      :label="activeText"
      flat
      dense
    />
    <q-btn
      :class="{
        active: selectedOption === false,
        inactive: selectedOption !== false,
      }"
      @click="selectOption(false)"
      class="toggle-btn"
      :label="inactiveText"
      flat
      dense
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

// Define the props with TypeScript types
const props = withDefaults(
  defineProps<{
    activeText?: string;
    inactiveText?: string;
    modelValue?: boolean;
    cssClass?: string;
  }>(),
  {
    activeText: 'Active',
    inactiveText: 'Inactive',
    modelValue: true,
    cssClass: 'text-right',
  },
);

// Define the emit types for TypeScript
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
}>();

// Reactive state for the selected option
const selectedOption = ref<boolean>(props.modelValue);

// Function to handle option selection
const selectOption = (option: boolean) => {
  selectedOption.value = option;
  emit('update:modelValue', option); // Emit the selected option to the parent
};

// Watch for changes in modelValue prop to sync with parent
watch(
  () => props.modelValue,
  (newValue) => {
    selectedOption.value = newValue;
  },
);
</script>

<style scoped>
.toggle-container {
  display: flex;
  gap: 0; /* No gap between buttons */
}

.toggle-btn {
  width: 120px; /* Adjust width as needed */
  height: 40px; /* Adjust height as needed */
  border-radius: 0; /* Remove default border radius */
  text-transform: none; /* Prevent uppercase text */
  font-size: 16px;
  font-weight: bold;
}

/* Style for the active button */
.active {
  background-color: var(--q-primary); /* Quasar's primary color */
  color: white;
}

/* Style for the inactive button */
.inactive {
  background-color: #e0e0e0; /* Light gray for inactive */
  color: black;
}

/* Ensure the buttons are seamlessly connected */
.toggle-btn:first-child {
  border-top-left-radius: 10px; /* Rounded left side */
  border-bottom-left-radius: 10px;
}

.toggle-btn:last-child {
  border-top-right-radius: 10px; /* Rounded right side */
  border-bottom-right-radius: 10px;
}
</style>
