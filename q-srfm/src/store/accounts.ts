import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { Account } from '../types';
import { dataAccess } from '../dataAccess';

export const useAccountStore = defineStore('accounts', () => {
  const accountsByFamily = ref<Map<string, Account[]>>(new Map());

  async function fetchAccounts(familyId: string, force = false): Promise<Account[]> {
    if (!familyId) return [];
    if (!force && accountsByFamily.value.has(familyId)) {
      return accountsByFamily.value.get(familyId) ?? [];
    }
    const accounts = await dataAccess.getAccounts(familyId);
    accountsByFamily.value.set(familyId, accounts);
    return accounts;
  }

  function getCachedAccounts(familyId: string): Account[] {
    return accountsByFamily.value.get(familyId) ?? [];
  }

  function clearFamily(familyId: string) {
    accountsByFamily.value.delete(familyId);
  }

  return {
    fetchAccounts,
    getCachedAccounts,
    clearFamily,
  };
});

