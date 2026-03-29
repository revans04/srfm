import { defineStore } from 'pinia';
import { ref } from 'vue';
import { EntityType } from '../types';
import { dataAccess } from '../dataAccess';
import { useAuthStore } from './auth';
export const useFamilyStore = defineStore('family', () => {
    const auth = useAuthStore();
    const family = ref();
    const selectedEntityId = ref(''); // Track selected entity for filtering
    async function loadFamily(userId = '', options = {}) {
        try {
            if (family.value && !options.force)
                return family.value;
            if (!userId)
                userId = auth.user ? auth.user.uid : '';
            const f = await dataAccess.getUserFamily(userId);
            if (f) {
                family.value = f;
                // Set default entity (e.g., first "Family" type entity)
                if (f.entities?.length) {
                    const defaultEntity = f.entities.find((e) => e.type === EntityType.Family) || f.entities[0];
                    selectedEntityId.value = defaultEntity ? defaultEntity.id : '';
                }
                return f;
            }
        }
        catch (error) {
            const err = error;
            console.error('Error loading Family', err);
        }
        return null;
    }
    async function refreshAccounts(familyId) {
        try {
            const fid = familyId || family.value?.id;
            if (!fid)
                return [];
            const accounts = await dataAccess.getAccounts(fid);
            if (family.value) {
                family.value.accounts = accounts;
            }
            return accounts;
        }
        catch (error) {
            const err = error;
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
        }
        catch (error) {
            const err = error;
            console.error('Error getting Family', err);
        }
        return null;
    }
    async function createEntity(familyId, entity) {
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
        }
        catch (error) {
            const err = error;
            console.error('Error creating entity', err);
            throw err;
        }
    }
    async function updateEntity(familyId, entity) {
        try {
            await dataAccess.updateEntity(familyId, entity);
            if (family.value) {
                family.value.entities = family.value.entities?.map((e) => (e.id === entity.id ? entity : e)) || [];
            }
        }
        catch (error) {
            const err = error;
            console.error('Error updating entity', err);
            throw err;
        }
    }
    async function deleteEntity(familyId, entityId) {
        try {
            await dataAccess.deleteEntity(familyId, entityId);
            if (family.value) {
                family.value.entities = family.value.entities?.filter((e) => e.id !== entityId) || [];
                if (selectedEntityId.value === entityId) {
                    selectedEntityId.value = family.value.entities[0]?.id || '';
                }
            }
        }
        catch (error) {
            const err = error;
            console.error('Error deleting entity', err);
            throw err;
        }
    }
    async function addEntityMember(familyId, entityId, member) {
        try {
            await dataAccess.addEntityMember(familyId, entityId, member);
            if (family.value) {
                const entity = family.value.entities?.find((e) => e.id === entityId);
                if (entity) {
                    entity.members = entity.members || [];
                    if (!entity.members.some((m) => m.uid === member.uid)) {
                        entity.members.push(member);
                    }
                }
            }
        }
        catch (error) {
            const err = error;
            console.error('Error adding entity member', err);
            throw err;
        }
    }
    async function removeEntityMember(familyId, entityId, memberUid) {
        try {
            await dataAccess.removeEntityMember(familyId, entityId, memberUid);
            if (family.value) {
                const entity = family.value.entities?.find((e) => e.id === entityId);
                if (entity) {
                    entity.members = entity.members?.filter((m) => m.uid !== memberUid) || [];
                }
            }
        }
        catch (error) {
            const err = error;
            console.error('Error removing entity member', err);
            throw err;
        }
    }
    function selectEntity(entityId) {
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
    };
});
