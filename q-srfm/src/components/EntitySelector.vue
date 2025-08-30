<template>
  <div class="entity-wrap">
    <h4 class="entity-label">
      {{ currentEntityName }}
      <q-btn flat dense round icon="expand_more" size="sm">
        <q-menu v-model="menuOpen" anchor="bottom left" self="top left" :offset="[0, 4]" @show="onMenuShow" @hide="onMenuHide">
          <q-list class="entity-menu" style="background-color: white">
            <q-item v-for="option in entityOptions" :key="option.id" clickable @click="selectEntity(option.id)">
              <q-item-section>{{ option.name }}</q-item-section>
            </q-item>
          </q-list>
        </q-menu>
      </q-btn>
    </h4>
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
  const opts = entities.value.map((e) => ({ id: e.id, name: e.name }));
  return [{ id: '', name: 'All Entities' }, ...opts];
});

const currentEntityName = computed(() => {
  if (!familyStore.selectedEntityId) return 'All Entities';
  const entity = entities.value.find((e) => e.id === familyStore.selectedEntityId);
  return entity?.name || 'All Entities';
});

function selectEntity(id: string) {
  familyStore.selectEntity(id);
  menuOpen.value = false;
  emit('change', id);
}

// Targeted logging for debugging selector open/close
const DBG = '[EntitySelector]';
// Btn+QMenu handles toggling; keep diagnostic hooks
function onMenuShow() {
  console.debug(DBG, 'menu show');
}
function onMenuHide() {
  console.debug(DBG, 'menu hide');
}
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
