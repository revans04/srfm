<template>
  <div>
    <v-menu v-if="showMenu" v-model="menuOpen" offset-y>
      <template #activator="{ props }">
        <div v-bind="props" class="entity-selector no-wrap">
          <h4 :class="isMobile ? 'text-white' : ''">
            {{ currentEntityName }}
            <v-icon small>mdi-chevron-down</v-icon>
          </h4>
        </div>
      </template>
      <v-list class="entity-menu">
        <v-list-item v-for="option in entityOptions" :key="option.id" @click="selectEntity(option.id)">
          <v-list-item-title>{{ option.name }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-menu>
    <div v-else class="entity-selector no-wrap">
      <h4>{{ currentEntityName }}</h4>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useFamilyStore } from '../store/family';

const emit = defineEmits<{ (e: 'change', value: string): void }>();

const isMobile = computed(() => window.innerWidth < 960);
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
