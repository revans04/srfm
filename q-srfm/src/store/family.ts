import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { Family, Entity } from '../types';
import { EntityType } from '../types';
import { dataAccess } from '../dataAccess';
import { useAuthStore } from './auth';

export const useFamilyStore = defineStore('family', () => {
  const auth = useAuthStore();
  const family = ref<Family>();
  const selectedEntityId = ref<string>(''); // Track selected entity for filtering

  async function loadFamily(userId: string = '') {
    try {
      if (family.value) return family.value;
      if (!userId) userId = auth.user ? auth.user.uid : '';
      const f = await dataAccess.getUserFamily(userId);
      if (f) {
        family.value = f;
        // Set default entity (e.g., first "Family" type entity)
        if (f.entities?.length) {
          const defaultEntity = f.entities.find((e: Entity) => e.type === EntityType.Family) || f.entities[0];
          selectedEntityId.value = defaultEntity ? defaultEntity.id : '';
        }
        return f;
      }
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error loading Family', err);
    }
    return null;
  }

  async function getFamily() {
    try {
      if (!family.value) {
        await loadFamily();
      }
      return family.value;
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error getting Family', err);
    }
    return null;
  }

  async function createEntity(familyId: string, entity: Entity) {
    try {
      const response = await dataAccess.createEntity(familyId, entity);
      if (family.value) {
        family.value.entities = family.value.entities || [];
        family.value.entities.push({ ...entity, id: response.entityId });
        if (!selectedEntityId.value) {
          selectedEntityId.value = response.entityId;
        }
      }
      return response.entityId;
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error creating entity', err);
      throw err;
    }
  }

  async function updateEntity(familyId: string, entity: Entity) {
    try {
      await dataAccess.updateEntity(familyId, entity);
      if (family.value) {
        family.value.entities = family.value.entities?.map((e: Entity) => (e.id === entity.id ? entity : e)) || [];
      }
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error updating entity', err);
      throw err;
    }
  }

  async function deleteEntity(familyId: string, entityId: string) {
    try {
      await dataAccess.deleteEntity(familyId, entityId);
      if (family.value) {
        family.value.entities = family.value.entities?.filter((e: Entity) => e.id !== entityId) || [];
        if (selectedEntityId.value === entityId) {
          selectedEntityId.value = family.value.entities[0]?.id || '';
        }
      }
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error deleting entity', err);
      throw err;
    }
  }

  async function addEntityMember(familyId: string, entityId: string, member: { uid: string; email: string; role: string }) {
    try {
      await dataAccess.addEntityMember(familyId, entityId, member);
      if (family.value) {
        const entity = family.value.entities?.find((e: Entity) => e.id === entityId);
        if (entity) {
          entity.members = entity.members || [];
          if (!entity.members.some((m) => m.uid === member.uid)) {
            entity.members.push(member);
          }
        }
      }
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error adding entity member', err);
      throw err;
    }
  }

  async function removeEntityMember(familyId: string, entityId: string, memberUid: string) {
    try {
      await dataAccess.removeEntityMember(familyId, entityId, memberUid);
      if (family.value) {
        const entity = family.value.entities?.find((e: Entity) => e.id === entityId);
        if (entity) {
          entity.members = entity.members?.filter((m) => m.uid !== memberUid) || [];
        }
      }
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error removing entity member', err);
      throw err;
    }
  }

  function selectEntity(entityId: string) {
    selectedEntityId.value = entityId;
  }

  return {
    family,
    selectedEntityId,
    loadFamily,
    getFamily,
    createEntity,
    updateEntity,
    deleteEntity,
    addEntityMember,
    removeEntityMember,
    selectEntity,
  };
});
