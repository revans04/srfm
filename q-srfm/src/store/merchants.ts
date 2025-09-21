// src/store/merchants.ts
import type { Transaction } from '../types';
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

interface MerchantUsage {
  name: string;
  usageCount: number;
}

export const useMerchantStore = defineStore('merchants', () => {
  // State: List of merchants sorted by usage
  const merchants = ref<MerchantUsage[]>([]);

  // Action: Update merchants with a new list of transactions
  function updateMerchants(transactions: Transaction[]) {
    // Count usage of each merchant
    const merchantCounts: Record<string, number> = {};

    transactions.forEach((transaction) => {
      if (!transaction.deleted && transaction.merchant) {
        merchantCounts[transaction.merchant] = (merchantCounts[transaction.merchant] || 0) + 1;
      }
    });

    // Convert to array and sort by usage count (descending)
    merchants.value = Object.entries(merchantCounts)
      .map(([name, usageCount]) => ({
        name,
        usageCount,
      }))
      .sort((a, b) => b.usageCount - a.usageCount);
  }

  // Action: Update merchants from a precomputed counts object
  function updateMerchantsFromCounts(merchantCounts: Record<string, number>) {
    merchants.value = Object.entries(merchantCounts)
      .map(([name, usageCount]) => ({
        name,
        usageCount,
      }))
      .sort((a, b) => b.usageCount - a.usageCount);
  }

  function ensureMerchant(name: string) {
    const trimmed = name.trim();
    if (!trimmed) return;
    const exists = merchants.value.some((m) => m.name.localeCompare(trimmed, undefined, { sensitivity: 'accent' }) === 0);
    if (!exists) {
      merchants.value = [{ name: trimmed, usageCount: 0 }, ...merchants.value];
    }
  }

  // Getter: Get the list of merchant names for autocomplete
  const merchantNames = computed(() => merchants.value.map((m) => m.name));

  return {
    merchants,
    updateMerchants,
    updateMerchantsFromCounts,
    ensureMerchant,
    merchantNames,
  };
});
