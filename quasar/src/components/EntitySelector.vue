<template>
  <div>
    <template v-if="showMenu">
      <q-menu v-model="menuOpen" offset-y>
        <template #activator="{ props }">
          <div v-bind="props" class="entity-selector no-wrap">
            <h1>
              {{ currentEntityName }}
              <q-icon small>expand_more</q-icon>
            </h1>
          </div>
        </template>
        <q-list class="entity-menu">
          <q-item v-for="option in entityOptions" :key="option.id" clickable @click="selectEntity(option.id)">
            <q-item-section>{{ option.name }}</q-item-section>
          </q-item>
        </q-list>
      </q-menu>
    </template>
    <template v-else>
      <div class="entity-selector no-wrap">
        <h1>{{ currentEntityName }}</h1>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useFamilyStore } from '../store/family';

const emit = defineEmits<{ (e: 'change', value: string): void }>();

const familyStore = useFamilyStore();

const menuOpen = ref(false);
const entities = computed(() => familyStore.family?.entities || []);

const entityOptions = computed(() => {
  const opts = entities.value.map(e => ({ id: e.id, name: e.name }));
  return [{ id: '', name: 'All Entities' }, ...opts];
});

const currentEntityName = computed(() => {
  if (!familyStore.selectedEntityId) return 'All Entities';
  return entities.value.find(e => e.id === familyStore.selectedEntityId)?.name || 'All Entities';
});

const showMenu = computed(() => entities.value.length > 1);

function selectEntity(id: string) {
  familyStore.selectEntity(id);
  menuOpen.value = false;
  emit('change', id);
}
</script>

<style scoped>
.entity-selector {
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  font-size: 1.5rem;
  font-weight: bold;
  color: rgb(var(--v-theme-primary));
}
.entity-menu {
  padding: 8px;
  min-width: 200px;
}
</style>
