import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import type { Family, Entity, BudgetGroup, BudgetGroupKind } from '../types';
import { EntityType } from '../types';
import { dataAccess } from '../dataAccess';
import { useAuthStore } from './auth';

export const useFamilyStore = defineStore('family', () => {
  const auth = useAuthStore();
  const family = ref<Family>();
  const selectedEntityId = ref<string>(''); // Track selected entity for filtering

  // Group taxonomy is entity-scoped. Cached per entity to avoid refetching
  // on every BudgetPage navigation.
  const groupsByEntity = ref<Record<string, BudgetGroup[]>>({});

  const currentGroups = computed<BudgetGroup[]>(() => {
    const eid = selectedEntityId.value;
    if (!eid) return [];
    return groupsByEntity.value[eid] || [];
  });

  function setGroupsForEntity(entityId: string, groups: BudgetGroup[]) {
    if (!entityId) return;
    const sorted = [...groups].sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));
    groupsByEntity.value = { ...groupsByEntity.value, [entityId]: sorted };
  }

  async function loadGroups(entityId: string, options: { force?: boolean } = {}) {
    if (!entityId) return [];
    if (!options.force && groupsByEntity.value[entityId]) {
      return groupsByEntity.value[entityId];
    }
    const groups = await dataAccess.getGroups(entityId);
    setGroupsForEntity(entityId, groups);
    return groupsByEntity.value[entityId] || [];
  }

  async function createGroup(
    entityId: string,
    payload: { name: string; kind?: BudgetGroupKind; color?: string; icon?: string },
  ) {
    const created = await dataAccess.createGroup(entityId, payload);
    const next = [...(groupsByEntity.value[entityId] || []), created];
    setGroupsForEntity(entityId, next);
    return created;
  }

  async function renameGroup(entityId: string, groupId: string, name: string) {
    const updated = await dataAccess.updateGroup(entityId, groupId, { name });
    const list = (groupsByEntity.value[entityId] || []).map((g) => (g.id === groupId ? updated : g));
    setGroupsForEntity(entityId, list);
    return updated;
  }

  async function updateGroup(
    entityId: string,
    groupId: string,
    payload: Partial<Pick<BudgetGroup, 'name' | 'kind' | 'color' | 'icon' | 'collapsedDefault' | 'archived' | 'sortOrder'>>,
  ) {
    const updated = await dataAccess.updateGroup(entityId, groupId, payload);
    const list = (groupsByEntity.value[entityId] || []).map((g) => (g.id === groupId ? updated : g));
    setGroupsForEntity(entityId, list);
    return updated;
  }

  async function reorderGroups(entityId: string, groupIds: string[]) {
    await dataAccess.reorderGroups(entityId, groupIds);
    const orderMap = new Map<string, number>(groupIds.map((id, i) => [id, i]));
    const reordered = (groupsByEntity.value[entityId] || []).map((g) => {
      const next = orderMap.get(g.id);
      return { ...g, sortOrder: next ?? g.sortOrder };
    });
    setGroupsForEntity(entityId, reordered);
  }

  async function archiveGroup(entityId: string, groupId: string) {
    await updateGroup(entityId, groupId, { archived: true });
  }

  async function deleteGroup(entityId: string, groupId: string) {
    await dataAccess.deleteGroup(entityId, groupId);
    const list = (groupsByEntity.value[entityId] || []).filter((g) => g.id !== groupId);
    setGroupsForEntity(entityId, list);
  }

  function getGroup(entityId: string, groupId: string): BudgetGroup | undefined {
    return (groupsByEntity.value[entityId] || []).find((g) => g.id === groupId);
  }

  function getGroupByName(entityId: string, name: string): BudgetGroup | undefined {
    const needle = name?.trim().toLowerCase();
    if (!needle) return undefined;
    return (groupsByEntity.value[entityId] || []).find((g) => g.name.toLowerCase() === needle);
  }

  async function loadFamily(userId: string = '', options: { force?: boolean } = {}) {
    try {
      if (!userId) userId = auth.user ? auth.user.uid : '';

      // Defensive: if a previous user's family is still in memory (e.g.
      // persisted state survived an auth handoff that the persistence
      // plugin failed to catch), discard before returning the cache.
      // The persistence layer has its own UID-namespacing as the primary
      // protection — this is belt-and-suspenders so a missed auth event
      // can't leak data across sessions.
      if (family.value && !options.force) {
        const cached = family.value;
        const isOwner = !!userId && cached.ownerUid === userId;
        const isMember = !!userId && cached.members?.some((m) => m.uid === userId);
        if (isOwner || isMember) {
          return cached;
        }
        family.value = undefined;
        selectedEntityId.value = '';
        groupsByEntity.value = {};
      }

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

  async function refreshAccounts(familyId?: string) {
    try {
      const fid = familyId || family.value?.id;
      if (!fid) return [];
      const accounts = await dataAccess.getAccounts(fid);
      if (family.value) {
        family.value.accounts = accounts;
      }
      return accounts;
    } catch (error: unknown) {
      const err = error as Error;
      console.error('Error refreshing accounts', err);
      return [];
    }
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
    refreshAccounts,
    createEntity,
    updateEntity,
    deleteEntity,
    addEntityMember,
    removeEntityMember,
    selectEntity,
    // Groups
    currentGroups,
    groupsByEntity,
    setGroupsForEntity,
    loadGroups,
    createGroup,
    renameGroup,
    updateGroup,
    reorderGroups,
    archiveGroup,
    deleteGroup,
    getGroup,
    getGroupByName,
  };
});
