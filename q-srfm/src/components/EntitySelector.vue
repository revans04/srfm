<template>
  <h4 class="entity-label">
    {{ currentEntityName }}
    <q-icon size="xs" name="expand_more" style="color: var(--q-primary); vertical-align: middle; cursor: pointer" @click="menuOpen = !menuOpen">
      <q-menu v-model="menuOpen" :offset="[0, 4]">
        <q-list class="entity-menu" style="background-color: white">
          <q-item v-for="option in entityOptions" :key="option.id" clickable @click="selectEntity(option.id)">
            <q-item-section>{{ option.name }}</q-item-section>
          </q-item>
        </q-list>
      </q-menu>
    </q-icon>
  </h4>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useFamilyStore } from '../store/family';

const emit = defineEmits<{ (e: 'change', value: string): void }>();

const familyStore = useFamilyStore();

const menuOpen = ref(false);
const entities = computed(() => familyStore.family?.entities || []);

const entityOptions = computed(() => {
  const opts = entities.value.map((e) => ({ id: e.id, name: e.name }));
  return [{ id: '', name: 'All Entities' }, ...opts];
});

const currentEntityName = computed(() => {
  if (!familyStore.selectedEntityId) return 'All Entities';
  const entity = entities.value.find((e) => e.id === familyStore.selectedEntityId);
  console.log('Entity found:', entity);
  return entity?.name || 'All Entities';
});

function selectEntity(id: string) {
  familyStore.selectEntity(id);
  menuOpen.value = false;
  emit('change', id);
}

onMounted(() => {
  console.log('EntitySelector: entities', entities.value);
  console.log('EntitySelector: selectedEntityId', familyStore.selectedEntityId);
  console.log('EntitySelector: currentEntityName', currentEntityName.value);
});

watch(
  () => familyStore.selectedEntityId,
  () => {
    console.log('Selected Entity ID changed to:', familyStore.selectedEntityId, 'Name:', currentEntityName.value);
  },
);
</script>

<style scoped>
.entity-label {
  display: inline-flex;
  align-items: center;
  font-size: 1.5rem;
  font-weight: bold;
  color: var(--q-primary);
  margin: 0;
  padding: 4px;
}

.entity-menu {
  padding: 8px;
  min-width: 200px;
  background-color: white;
}
</style>
