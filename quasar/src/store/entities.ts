import { defineStore } from "pinia";
import { ref } from "vue";
import { Family, Entity } from "../types";
import { dataAccess } from "../dataAccess";
import { auth } from "@/firebase/index";

export const useFamilyStore = defineStore("family", () => {
  const family = ref<Family>();
  const selectedEntityId = ref<string>("");

  async function loadFamily(userId: string = "") {
    try {
      if (!userId) userId = auth.currentUser ? auth.currentUser.uid : "";
      const f = await dataAccess.getUserFamily(userId);
      if (f) {
        family.value = f;
        if (f.entities?.length) {
          const defaultEntity = f.entities.find(e => e.type === "Family") || f.entities[0];
          selectedEntityId.value = defaultEntity.id;
        }
        return f;
      }
    } catch (error: any) {
      console.error("Error loading Family", error);
    }
    return null;
  }

  async function getFamily() {
    try {
      if (!family.value) {
        await loadFamily();
      }
      return family.value;
    } catch (error: any) {
      console.error("Error getting Family", error);
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
    } catch (error: any) {
      console.error("Error creating entity", error);
      throw error;
    }
  }

  async function updateEntity(familyId: string, entity: Entity) {
    try {
      await dataAccess.updateEntity(familyId, entity);
      if (family.value) {
        family.value.entities = family.value.entities?.map(e =>
          e.id === entity.id ? entity : e
        ) || [];
      }
    } catch (error: any) {
      console.error("Error updating entity", error);
      throw error;
    }
  }

  async function deleteEntity(familyId: string, entityId: string) {
    try {
      await dataAccess.deleteEntity(familyId, entityId);
      if (family.value) {
        family.value.entities = family.value.entities?.filter(e => e.id !== entityId) || [];
        if (selectedEntityId.value === entityId) {
          selectedEntityId.value = family.value.entities[0]?.id || "";
        }
      }
    } catch (error: any) {
      console.error("Error deleting entity", error);
      throw error;
    }
  }

  async function addEntityMember(familyId: string, entityId: string, member: { uid: string; email: string; role: string }) {
    try {
      await dataAccess.addEntityMember(familyId, entityId, member);
      if (family.value) {
        const entity = family.value.entities?.find(e => e.id === entityId);
        if (entity) {
          entity.members = entity.members || [];
          if (!entity.members.some(m => m.uid === member.uid)) {
            entity.members.push(member);
          }
        }
      }
    } catch (error: any) {
      console.error("Error adding entity member", error);
      throw error;
    }
  }

  async function removeEntityMember(familyId: string, entityId: string, memberUid: string) {
    try {
      await dataAccess.removeEntityMember(familyId, entityId, memberUid);
      if (family.value) {
        const entity = family.value.entities?.find(e => e.id === entityId);
        if (entity) {
          entity.members = entity.members?.filter(m => m.uid !== memberUid) || [];
        }
      }
    } catch (error: any) {
      console.error("Error removing entity member", error);
      throw error;
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
