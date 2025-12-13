<template>
  <q-menu v-model="menuOpen" anchor="bottom right" self="top right" :close-on-content-click="false">
    <template #activator="{ props }">
      <q-btn
        v-bind="props"
        flat
        rounded
        color="primary"
        class="month-selector-btn"
        :label="formattedCurrentMonth"
        icon="expand_more"
        :disable="!entityId"
      />
    </template>
    <q-card class="month-selector-menu">
      <div class="row items-center q-px-sm q-py-xs border-bottom">
        <div class="col-auto">
          <q-btn flat dense icon="chevron_left" @click.stop="shiftMonths(-6)" />
        </div>
        <div class="col text-center text-caption text-grey-7">
          {{ displayYear }}
        </div>
        <div class="col-auto">
          <q-btn flat dense icon="chevron_right" @click.stop="shiftMonths(6)" />
        </div>
      </div>
      <div class="month-grid">
        <q-btn
          v-for="monthOption in displayedMonths"
          :key="monthOption.value"
          flat
          class="month-option"
          :class="{
            'month-option--selected': monthOption.value === modelValue,
            'month-option--existing': monthExists(monthOption.value),
          }"
          :label="monthOption.label"
          size="sm"
          @click="selectMonth(monthOption.value)"
        />
      </div>
    </q-card>
  </q-menu>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';

const props = defineProps<{
  modelValue: string;
  entityId?: string;
  existingMonths?: Set<string>;
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void;
  (e: 'select', month: string): void;
}>();

const menuOpen = ref(false);
const monthOffset = ref(0);

const formattedCurrentMonth = computed(() => {
  if (!props.modelValue) return '';
  const [year, monthNum] = props.modelValue.split('-');
  const date = new Date(parseInt(year, 10), parseInt(monthNum, 10) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
});

const displayedMonths = computed(() => {
  const options: { label: string; value: string }[] = [];
  const today = new Date();
  const startDate = new Date(today.getFullYear(), today.getMonth() + monthOffset.value, 1);
  for (let i = -4; i <= 7; i++) {
    const date = new Date(startDate.getFullYear(), startDate.getMonth() + i, 1);
    const year = date.getFullYear();
    const monthNum = date.getMonth();
    const label = date.toLocaleString('en-US', {
      month: 'short',
      year: 'numeric',
    });
    const value = `${year}-${(monthNum + 1).toString().padStart(2, '0')}`;
    options.push({ label, value });
  }
  return options;
});

const displayYear = computed(() => {
  const today = new Date();
  const startDate = new Date(today.getFullYear(), today.getMonth() + monthOffset.value, 1);
  return startDate.getFullYear();
});

const monthExists = (month: string) => {
  return props.existingMonths?.has(month) ?? false;
};

const shiftMonths = (delta: number) => {
  monthOffset.value += delta;
};

const selectMonth = (month: string) => {
  if (!props.entityId) return;
  emit('update:modelValue', month);
  emit('select', month);
  menuOpen.value = false;
};
</script>

<style scoped>
.month-selector-btn {
  text-transform: none;
}
.month-selector-menu {
  min-width: 240px;
  border-radius: 12px;
}
.month-option {
  border: 1px dashed rgba(15, 23, 42, 0.15);
  border-radius: 8px;
  min-height: 40px;
  font-size: 0.85rem;
  justify-content: center;
  text-transform: none;
}
.month-option--selected {
  background-color: var(--q-primary);
  color: #fff;
  font-weight: 600;
  border-color: transparent !important;
}
.month-option--existing {
  border-style: solid !important;
}
.month-option:not(.month-option--selected):hover {
  background: rgba(37, 99, 235, 0.08);
}
.border-bottom {
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
}
.month-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  padding: 12px;
}
</style>
